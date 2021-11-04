using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using OAuth;
using ScheduleDesigner.Helpers;
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

namespace ScheduleDesigner.Services
{
    public class UsosAuthenticationService
    {
        public readonly ApplicationInfo ApplicationInfo;
        public readonly Consumer UsosConsumer;

        private readonly HttpClient _client;
        private readonly IUserRepo _userRepo;
        private readonly IStaffRepo _staffRepo;
        private readonly IAuthorizationRepo _authorizationRepo;

        public UsosAuthenticationService(IOptions<ApplicationInfo> applicationInfo, IOptions<Consumer> usosConsumer, 
            IUserRepo userRepo,
            IStaffRepo staffRepo,
            IAuthorizationRepo authorizationRepo)
        {
            ApplicationInfo = applicationInfo.Value;
            UsosConsumer = usosConsumer.Value;

            _client = new HttpClient();
            _userRepo = userRepo;
            _staffRepo = staffRepo;
            _authorizationRepo = authorizationRepo;
        }

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

        public async Task<UserInfo> GetUserId(OAuthRequest oauth)
        {
            var requestUrl = $"{ApplicationInfo.BaseUsosUrl}/services/users/user?fields=id&format=json";
            oauth.RequestUrl = requestUrl;
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            
            request.Headers.Add("Authorization", oauth.GetAuthorizationHeader());
            return await SendUserRequestAsync(request);
        }

        public async Task<int> GetUserId(string accessToken, string accessTokenSecret)
        {
            var _authorization = await _authorizationRepo
                .Get(e => e.AccessToken == accessToken && e.AccessTokenSecret == accessTokenSecret)
                .FirstOrDefaultAsync();

            if (_authorization == null || _authorization.InsertedDateTime.AddMinutes(30) < DateTime.Now)
            {
                return -1;
            }

            return _authorization.UserId;
        }

        public async Task<UserInfo> GetUserInfo(OAuthRequest oauth, int? userId = null)
        {
            var requestUrl = $"{ApplicationInfo.BaseUsosUrl}/services/users/user?";
            if (userId != null) requestUrl += "user_id=" + userId + "&";
            requestUrl += "fields=id|first_name|last_name|student_status|staff_status|titles&format=json";
            oauth.RequestUrl = requestUrl;
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

            request.Headers.Add("Authorization", oauth.GetAuthorizationHeader());
            return await SendUserRequestAsync(request);
        }

        public User GetUserFromDb(int userId)
        {
            var _user = _userRepo
                .Get(e => e.UserId == userId)
                .Include(e => e.Student).ThenInclude(e => e.Groups)
                .Include(e => e.Coordinator)
                .Include(e => e.Staff);

            return _user.FirstOrDefault();
        }

        public async Task<User> CreateUser(UserInfo userInfo)
        {
            var userId = int.Parse(userInfo.Id);

            Student student = null;
            if (userInfo.StudentStatus != null && userInfo.StudentStatus != 0)
            {
                student = new Student 
                {
                    UserId = userId,
                };
            }
            
            Coordinator coordinator = null;
            if (userInfo.StaffStatus == 2)
            {
                coordinator = new Coordinator 
                { 
                    TitleBefore = userInfo.Titles.Before,
                    TitleAfter = userInfo.Titles.After,
                    UserId = userId,
                };
            }

            Staff staff = null;
            if (userInfo.StaffStatus == 1)
            {
                staff = new Staff
                {
                    IsAdmin = !_staffRepo.GetAll().Any(),
                    UserId = userId,
                };
            }

            User user = new User
            {
                UserId = userId,
                FirstName = userInfo.FirstName,
                LastName = userInfo.LastName,
                Student = student,
                Coordinator = coordinator,
                Staff = staff
            };

            await _userRepo.Add(user);
            return user;
        }

        public async Task<Authorization> CreateCredentials(int userId, string accessToken, string accessTokenSecret)
        {
            var _authorization = await _authorizationRepo
                .Get(e => e.UserId == userId)
                .FirstOrDefaultAsync();

            if (_authorization == null)
            {
                var authorization = new Authorization
                {
                    UserId = userId,
                    AccessToken = accessToken,
                    AccessTokenSecret = accessTokenSecret,
                    InsertedDateTime = DateTime.Now
                };
                await _authorizationRepo.Add(authorization);
                await _authorizationRepo.SaveChanges();

                return authorization;
            }

            return null;
        }

        public async Task<Authorization> UpdateCredentials(int userId, string accessToken, string accessTokenSecret)
        {
            var _authorization = await _authorizationRepo
                .Get(e => e.UserId == userId)
                .FirstOrDefaultAsync();

            if (_authorization == null || _authorization.InsertedDateTime.AddMinutes(30) > DateTime.Now)
            {
                return null;
            }
            
            _authorization.AccessToken = accessToken;
            _authorization.AccessTokenSecret = accessTokenSecret;
            _authorization.InsertedDateTime = DateTime.Now;

            _authorizationRepo.Update(_authorization);
            await _authorizationRepo.SaveChanges();

            return _authorization;
        }

        private async Task<UserInfo> SendUserRequestAsync(HttpRequestMessage request)
        {
            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var userInfo = JsonSerializer.Deserialize<UserInfo>(json);
            return userInfo;
        }
    }

    [Serializable]
    public class UserInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("last_name")]
        public string LastName { get; set; }

        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }

        [JsonPropertyName("titles")]
        public Titles Titles { get; set; }

        [JsonPropertyName("student_status")]
        public int? StudentStatus { get; set; }

        [JsonPropertyName("staff_status")]
        public int? StaffStatus { get; set; }
    }

    [Serializable]
    public class Titles
    {
        [JsonPropertyName("before")]
        public string Before { get; set; }
        
        [JsonPropertyName("after")]
        public string After { get; set; }
    }
}
