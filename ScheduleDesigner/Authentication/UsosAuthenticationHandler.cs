using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScheduleDesigner.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace ScheduleDesigner.Authentication
{
    /// <summary>
    /// Opcje użyte do konfiguracji obsługi procesu uwierzytelniania.
    /// </summary>
    public class UsosAuthenticationOptions : AuthenticationSchemeOptions { }
    
    /// <summary>
    /// Klasa obsługi procesu uwierzytelniania w systemie.
    /// </summary>
    public class UsosAuthenticationHandler : AuthenticationHandler<UsosAuthenticationOptions>
    {
        /// <summary>
        /// Instancja serwisu zapewniającego poprawną komunikację z zewnętrznym systemem USOS
        /// </summary>
        private readonly UsosAuthenticationService _usosService;

        /// <summary>
        /// Konstruktor obsługi uwierzytelniania wykorzystujący wstrzykiwanie zależności.
        /// </summary>
        /// <param name="options">Wstrzyknięte opcje użyte do konfiguracji obsługi procesu uwierzytelniania</param>
        /// <param name="logger">Wstrzyknięty loger zdarzeń</param>
        /// <param name="encoder">Wstrzyknięty koder adresów URL</param>
        /// <param name="clock">Wstrzyknięty zegar systemowy</param>
        /// <param name="usosService">Wstrzyknięta instancja serwisu do komunikacji z systemem USOS</param>
        public UsosAuthenticationHandler(
            IOptionsMonitor<UsosAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            UsosAuthenticationService usosService
        )
            :base(options, logger, encoder, clock)
        {
            _usosService = usosService;
        }

        /// <summary>
        /// Funkcja obsługująca proces uwierzytelniania.
        /// </summary>
        /// <returns>Asynchroniczna operacja przechowująca rezultat procesu uwierzytelniania</returns>
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            try
            {
                if (!Request.Headers.ContainsKey("AccessToken") || !Request.Headers.ContainsKey("AccessTokenSecret"))
                {
                    return AuthenticateResult.NoResult();
                }

                var accessToken = Request.Headers["AccessToken"];
                var accessTokenSecret = Request.Headers["AccessTokenSecret"];

                var userId = await _usosService.GetUserId(accessToken, accessTokenSecret);
                if (userId != -1)
                {
                    return Validate(userId);
                }
                
                var oauth = _usosService.GetOAuthRequest(
                    accessToken,
                    accessTokenSecret
                );
                var userInfo = await _usosService.GetUserId(oauth);
                userId = int.Parse(userInfo.Id);
                await _usosService.UpdateCredentials(userId, accessToken, accessTokenSecret);
                return Validate(userId);

            }
            catch (Exception e)
            {
                return AuthenticateResult.Fail("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Funkcja weryfikująca użytkownika i nadająca mu odpowiednie roszczenia.
        /// </summary>
        /// <param name="userId">Identyfikator użytkownika</param>
        /// <returns>Rezultat procesu uwierzytelniania</returns>
        private AuthenticateResult Validate(int userId)
        {
            var user = _usosService.GetUserFromDb(userId);

            var claims = new List<Claim>
            {
                new Claim("user_id", userId.ToString())
            };

            if (user != null && user.IsStudent)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Student"));
                bool isRepresentative = false;
                foreach (var group in user.Groups)
                {
                    if (group.IsRepresentative)
                    {
                        claims.Add(new Claim("representative_group_id", group.GroupId.ToString()));
                        isRepresentative = true;
                    }
                }
                if (isRepresentative)
                {
                    claims.Add(new Claim(ClaimTypes.Role, "Representative"));
                }
            }
            if (user != null && user.IsCoordinator) claims.Add(new Claim(ClaimTypes.Role, "Coordinator"));
            if (user != null && user.IsStaff)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Staff"));
                if (user != null && user.IsAdmin)
                {
                    claims.Add(new Claim(ClaimTypes.Role, "Administrator"));
                }
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new System.Security.Principal.GenericPrincipal(identity, null);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }
    }
}
