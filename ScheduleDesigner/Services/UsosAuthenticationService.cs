using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using OAuth;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using ScheduleDesigner.Authentication;
using ScheduleDesigner.Helpers;
using ScheduleDesigner.Repositories.UnitOfWork;

namespace ScheduleDesigner.Services
{
    /// <summary>
    /// Klasa serwisu zapewniającego poprawną komunikację z zewnętrznym systemem USOS.
    /// </summary>
    public class UsosAuthenticationService
    {
        /// <summary>
        /// Informacje o połączeniu z konkretną instalacją USOS API.
        /// </summary>
        public readonly ApplicationOptions ApplicationInfo;

        /// <summary>
        /// Informacje o kluczu projektu USOS API.
        /// </summary>
        public readonly Consumer UsosConsumer;

        /// <summary>
        /// Instancja klasy przeznaczonej do wysyłania i odbierania żądań HTTP do/z USOS API.
        /// </summary>
        private readonly HttpClient _client;

        /// <summary>
        /// Instancja klasy wzorca UoW.
        /// </summary>
        private readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// Konstruktor serwisu.
        /// </summary>
        /// <param name="applicationInfo">Instancja konfiguracji aplikacji</param>
        /// <param name="usosConsumer">Instancja konfiguracji klucza API</param>
        /// <param name="unitOfWork">Instancja klasy wzorca UoW</param>
        public UsosAuthenticationService(IOptions<ApplicationOptions> applicationInfo, IOptions<Consumer> usosConsumer, IUnitOfWork unitOfWork)
        {
            ApplicationInfo = applicationInfo.Value;
            UsosConsumer = usosConsumer.Value;

            _client = new HttpClient();
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Funkcja haszująca ciąg znaków podanych jako jej parametr.
        /// </summary>
        /// <param name="text">Ciąg znaków do zahaszowania</param>
        /// <returns>Zahaszowany ciąg znaków</returns>
        private string HashString(string text)
        {
            if (String.IsNullOrEmpty(text))
            {
                return String.Empty;
            }

            var sha256 = new System.Security.Cryptography.SHA256Managed();

            var textBytes = System.Text.Encoding.UTF8.GetBytes(text);
            var hashBytes = sha256.ComputeHash(textBytes);

            string hash = BitConverter.ToString(hashBytes).Replace("-", String.Empty);

            return hash;
        }

        /// <summary>
        /// Funkcja tworząca żądanie OAuth 1.0a.
        /// </summary>
        /// <param name="accessToken">Token dostępu</param>
        /// <param name="accessTokenSecret">Sekret tokenu dostępu</param>
        /// <returns>Utworzone żądanie OAuth 1.0a</returns>
        public OAuthRequest GetOAuthRequest(string accessToken, string accessTokenSecret)
        {
            OAuthRequest oauth = OAuthRequest.ForProtectedResource(
                "GET",
                UsosConsumer.Key,
                UsosConsumer.Secret,
                accessToken,
                accessTokenSecret
            );
            return oauth;
        }

        /// <summary>
        /// Funkcja zwracająca identyfikator użytkownika z systemu USOS.
        /// </summary>
        /// <param name="oauth">Utworzone żądanie OAuth 1.0a</param>
        /// <returns>Asynchroniczną operację przechowującą identyfikator użytkownika</returns>
        public async Task<UserInfo> GetUserId(OAuthRequest oauth)
        {
            var requestUrl = $"{ApplicationInfo.BaseUsosUrl}/services/users/user?fields=id&format=json";
            oauth.RequestUrl = requestUrl;
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            
            request.Headers.Add("Authorization", oauth.GetAuthorizationHeader());
            return await SendUserRequestAsync(request);
        }

        /// <summary>
        /// Funkcja zwracająca identyfikator użytkownika na podstawie tokenu dostępu i jego sekretu.
        /// </summary>
        /// <param name="accessToken">Token dostępu</param>
        /// <param name="accessTokenSecret">Sekret tokenu dostępu</param>
        /// <returns>Identyfikator użytkownika lub -1 w przypadku nieodnalezienia go</returns>
        public async Task<int> GetUserId(string accessToken, string accessTokenSecret)
        {
            var hashedAccessToken = HashString(accessToken);
            var hashedAccessTokenSecret = HashString(accessTokenSecret);

            var _authorization = await _unitOfWork.Authorizations
                .Get(e => e.AccessToken == hashedAccessToken && e.AccessTokenSecret == hashedAccessTokenSecret)
                .FirstOrDefaultAsync();

            if (_authorization == null || _authorization.InsertedDateTime.AddMinutes(30) < DateTime.Now)
            {
                return -1;
            }

            return _authorization.UserId;
        }

        /// <summary>
        /// Funkcja zwracająca zbiór informacji o użytkowniku z systemu USOS w postaci obiektu klasy <see cref="UserInfo"/>.
        /// </summary>
        /// <param name="oauth">Utworzone żądanie OAuth 1.0a</param>
        /// <param name="userId">Identyfikator użytkownika</param>
        /// <returns>Asynchroniczną operację przechowującą informacje o użytkowniku</returns>
        public async Task<UserInfo> GetUserInfo(OAuthRequest oauth, int? userId = null)
        {
            var requestUrl = $"{ApplicationInfo.BaseUsosUrl}/services/users/user?";
            if (userId != null) requestUrl += "user_id=" + userId + "&";
            requestUrl += "fields=id|first_name|last_name|student_status|student_number|staff_status|titles&format=json";
            oauth.RequestUrl = requestUrl;
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

            request.Headers.Add("Authorization", oauth.GetAuthorizationHeader());
            return await SendUserRequestAsync(request);
        }

        /// <summary>
        /// Funkcja wyszukująca użytkowników w systemie USOS i zwracająca ich w postaci obiektu klasy <see cref="UserSearch"/>.
        /// </summary>
        /// <param name="oauth">Utworzone żądanie OAuth 1.0a</param>
        /// <param name="query">Kryterium wyszukiwania</param>
        /// <param name="perPage">Liczba użytkowników na stronie</param>
        /// <param name="start">Liczba użytkowników do pominięcia</param>
        /// <returns>Asynchroniczną operację przechowującą informacje o odnalezionych użytkownikach</returns>
        public async Task<UserSearch> GetUserSearch(OAuthRequest oauth, string query, int perPage, int start)
        {
            var requestUrl = $"{ApplicationInfo.BaseUsosUrl}/services/users/search2" +
                $"?lang=pl&fields=items[user[id|first_name|last_name|student_status|student_number|staff_status|titles]]|next_page";
            if (query != null) requestUrl += $"&query={query}";
            requestUrl += $"&among=all&num={perPage}&start={start}&format=json";
            oauth.RequestUrl = requestUrl;
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

            request.Headers.Add("Authorization", oauth.GetAuthorizationHeader());
            return await SendSearchRequestAsync(request);
        }

        /// <summary>
        /// Wyszukanie użytkownika w bazie danych na podstawie jego identyfikatora.
        /// </summary>
        /// <param name="userId">ID użytkownika do odnalezienia</param>
        /// <returns>Odnalezionego użytkownika lub null w przypadku nieznalezienia</returns>
        public User GetUserFromDb(int userId)
        {
            var _user = _unitOfWork.Users
                .Get(e => e.UserId == userId)
                .Include(e => e.Groups);

            return _user.FirstOrDefault();
        }

        /// <summary>
        /// Utworzenie nowego użytkownika w bazie danych na podstawie informacji z systemu USOS.
        /// </summary>
        /// <param name="userInfo">Uzyskane informacje o użytkowniku</param>
        /// <returns>Asynchroniczną operację przechowującą nowo utworzonego użytkownika</returns>
        public async Task<User> CreateUser(UserInfo userInfo)
        {
            var userId = int.Parse(userInfo.Id);

            var isStudent = false;
            var studentNumber = "";
            if (userInfo.StudentStatus != null && userInfo.StudentStatus != 0)
            {
                isStudent = true;
                studentNumber = userInfo.StudentNumber;
            }
            
            var isCoordinator = false;
            if (userInfo.StaffStatus == 2)
            {
                isCoordinator = true;
            }

            var isStaff = false;
            var isAdmin = false;
            if (userInfo.StaffStatus == 1 || userInfo.StaffStatus == 2)
            {
                isStaff = true;
                isAdmin = userInfo.StaffStatus != 2 && !_unitOfWork.Users.GetAll().Any(e => e.IsStaff);
            }

            User user = new User
            {
                UserId = userId,
                FirstName = userInfo.FirstName,
                LastName = userInfo.LastName,
                AcademicNumber = isStudent ? studentNumber : null,
                TitleBefore = userInfo.Titles.Before,
                TitleAfter = userInfo.Titles.After,
                IsStudent = isStudent,
                IsCoordinator = isCoordinator,
                IsStaff = isStaff,
                IsAdmin = isAdmin
            };

            await _unitOfWork.Users.Add(user);
            return user;
        }

        /// <summary>
        /// Operacja zapisania tokenu dostępu do zewnętrznego systemu USOS.
        /// </summary>
        /// <param name="userId">Identyfikator użytkownika</param>
        /// <param name="accessToken">Token dostępu</param>
        /// <param name="accessTokenSecret">Sekret tokenu dostępu</param>
        /// <returns>Asynchroniczną operację przechowującą zapisany token dostępu w postaci obiektu klasy <see cref="Authorization"/></returns>
        public async Task<Authorization> CreateCredentials(int userId, string accessToken, string accessTokenSecret)
        {
            return await CreateCredentials(userId, accessToken, accessTokenSecret, DateTime.Now);
        }

        /// <summary>
        /// Operacja zapisania tokenu dostępu do zewnętrznego systemu USOS.
        /// </summary>
        /// <param name="userId">Identyfikator użytkownika</param>
        /// <param name="accessToken">Token dostępu</param>
        /// <param name="accessTokenSecret">Sekret tokenu dostępu</param>
        /// <param name="insertedDateTime">Data zapisania tokenu</param>
        /// <returns>Asynchroniczną operację przechowującą zapisany token dostępu w postaci obiektu klasy <see cref="Authorization"/></returns>
        public async Task<Authorization> CreateCredentials(int userId, string accessToken, string accessTokenSecret, DateTime insertedDateTime)
        {
            var _authorization = await _unitOfWork.Authorizations
                .Get(e => e.UserId == userId)
                .FirstOrDefaultAsync();

            if (_authorization == null)
            {
                var hashedAccessToken = HashString(accessToken);
                var hashedAccessTokenSecret = HashString(accessTokenSecret);

                var authorization = new Authorization
                {
                    UserId = userId,
                    AccessToken = hashedAccessToken,
                    AccessTokenSecret = hashedAccessTokenSecret,
                    InsertedDateTime = insertedDateTime
                };
                await _unitOfWork.Authorizations.Add(authorization);
                await _unitOfWork.CompleteAsync();

                return authorization;
            }

            return null;
        }

        /// <summary>
        /// Operacja nadpisania posiadanego tokenu dostępu do zewnętrznego systemu USOS.
        /// </summary>
        /// <param name="userId">Identyfikator użytkownika</param>
        /// <param name="accessToken">Nowy token dostępu</param>
        /// <param name="accessTokenSecret">Nowy sekret tokenu dostępu</param>
        /// <returns>Asynchroniczną operację przechowującą nowo zapisany token dostępu w postaci obiektu klasy <see cref="Authorization"/></returns>
        public async Task<Authorization> UpdateCredentials(int userId, string accessToken, string accessTokenSecret)
        {
            var _authorization = await _unitOfWork.Authorizations
                .Get(e => e.UserId == userId)
                .FirstOrDefaultAsync();

            if (_authorization == null || _authorization.InsertedDateTime.AddMinutes(30) > DateTime.Now)
            {
                return null;
            }

            var hashedAccessToken = HashString(accessToken);
            var hashedAccessTokenSecret = HashString(accessTokenSecret);

            _authorization.AccessToken = hashedAccessToken;
            _authorization.AccessTokenSecret = hashedAccessTokenSecret;
            _authorization.InsertedDateTime = DateTime.Now;

            _unitOfWork.Authorizations.Update(_authorization);
            await _unitOfWork.CompleteAsync();

            return _authorization;
        }

        /// <summary>
        /// Funkcja wysyłająca żądanie pobrania informacji o użytkowniku z systemu USOS.
        /// </summary>
        /// <param name="request">Reprezentacja żądania HTTP przeznaczonego do wysłania</param>
        /// <returns>Asynchroniczną operację przechowującą informacje o użytkowniku</returns>
        private async Task<UserInfo> SendUserRequestAsync(HttpRequestMessage request)
        {
            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var userInfo = JsonSerializer.Deserialize<UserInfo>(json);
            return userInfo;
        }

        /// <summary>
        /// Funkcja wysyłająca żądanie wyszukania użytkowników w systemie USOS.
        /// </summary>
        /// <param name="request">Reprezentacja żądania HTTP przeznaczonego do wysłania</param>
        /// <returns>Asynchroniczną operację przechowującą informacje o odnalezionych użytkownikach</returns>
        private async Task<UserSearch> SendSearchRequestAsync(HttpRequestMessage request)
        {
            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var userSearch = JsonSerializer.Deserialize<UserSearch>(json);
            return userSearch;
        }
    }

    /// <summary>
    /// Klasa reprezentująca odpowiedź USOS API na żądanie wyszukania użytkowników.
    /// </summary>
    [Serializable]
    public class UserSearch
    {
        /// <summary>
        /// Lista odnalezionych użytkowników.
        /// </summary>
        [JsonPropertyName("items")]
        public List<SearchItem> Items { get; set; }

        /// <summary>
        /// Zmienna przechowująca informację o tym czy istnieje kolejna strona odnalezionych wyników.
        /// </summary>
        [JsonPropertyName("next_page")]
        public bool NextPage { get; set; }
    }

    /// <summary>
    /// Reprezentacja odnalezionego pojedynczego użytkownika (itemu) w systemie USOS.
    /// </summary>
    [Serializable]
    public class SearchItem
    {
        /// <summary>
        /// Obiekt przechowujący informacje o odnalezionym użytkowniku.
        /// </summary>
        [JsonPropertyName("user")]
        public UserInfo User { get; set; }
    }

    /// <summary>
    /// Reprezentacja pobranych informacji o użytkowniku z systemu USOS.
    /// </summary>
    [Serializable]
    public class UserInfo
    {
        /// <summary>
        /// Identyfikator użytkownika w systemie USOS.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        /// <summary>
        /// Imię użytkownika.
        /// </summary>
        [JsonPropertyName("last_name")]
        public string LastName { get; set; }

        /// <summary>
        /// Nazwisko użytkownika.
        /// </summary>
        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }

        /// <summary>
        /// Posiadane tytuły naukowe przez użytkownika.
        /// </summary>
        [JsonPropertyName("titles")]
        public Titles Titles { get; set; }

        /// <summary>
        /// Informacja o tym czy użytkownik jest aktualnie studentem uczelni.
        /// </summary>
        [JsonPropertyName("student_status")]
        public int? StudentStatus { get; set; }

        /// <summary>
        /// Indeks przypisany studentowi.
        /// </summary>
        [JsonPropertyName("student_number")]
        public string StudentNumber { get; set; }

        /// <summary>
        /// Informacja o tym czy użytkownik jest aktualnie pracownikiem uczelni.
        /// </summary>
        [JsonPropertyName("staff_status")]
        public int? StaffStatus { get; set; }
    }

    /// <summary>
    /// Reprezentacja posiadanych tytułów naukowych przez użytkownika w systemie USOS.
    /// </summary>
    [Serializable]
    public class Titles
    {
        /// <summary>
        /// Tytuły naukowe wypisywane przed nazwiskiem.
        /// </summary>
        [JsonPropertyName("before")]
        public string Before { get; set; }
        
        /// <summary>
        /// Tytuły naukowe wypisywane po nazwisku.
        /// </summary>
        [JsonPropertyName("after")]
        public string After { get; set; }
    }
}
