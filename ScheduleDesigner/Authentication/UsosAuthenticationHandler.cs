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
    public class UsosAuthenticationOptions : AuthenticationSchemeOptions { }
    
    public class UsosAuthenticationHandler : AuthenticationHandler<UsosAuthenticationOptions>
    {
        private readonly UsosAuthenticationService _usosService;

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

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            try
            {
                if (Request.Headers.ContainsKey("AccessToken") && Request.Headers.ContainsKey("AccessTokenSecret"))
                {
                    var oauth = _usosService.GetOAuthRequest(
                        Request.Headers["AccessToken"], 
                        Request.Headers["AccessTokenSecret"]
                    );
                    var userInfo = await _usosService.GetUserId(oauth);
                    return ValidateToken(int.Parse(userInfo.Id));
                }

                return AuthenticateResult.NoResult();
            }
            catch (Exception e)
            {
                return AuthenticateResult.Fail(e.Message);
            }
        }

        private AuthenticateResult ValidateToken(int userId)
        {
            var user = _usosService.GetUserFromDb(userId);

            var claims = new List<Claim>
            {
                new Claim("user_id", userId.ToString())
            };

            if (user.Student != null)
            {
                claims.Add(new Claim("student", userId.ToString()));
                foreach (var group in user.Student.Groups) claims.Add(new Claim("representative", group.GroupId.ToString()));
            }
            if (user.Coordinator != null) claims.Add(new Claim("coordinator", userId.ToString()));
            if (user.Staff != null)
            {
                claims.Add(new Claim("staff", userId.ToString()));
                if (user.Staff.IsAdmin) claims.Add(new Claim("admin", userId.ToString()));
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new System.Security.Principal.GenericPrincipal(identity, null);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }
    }
}
