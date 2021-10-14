using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
                if (Request.Headers.ContainsKey("Authorization"))
                {
                    var token = Request.Headers["Authorization"];
                    var id = await _usosService.GetUserId(token);
                    return ValidateToken(id);
                }

                return AuthenticateResult.NoResult();
            }
            catch (Exception ex)
            {
                return AuthenticateResult.Fail(ex.Message);
            }
        }

        private AuthenticateResult ValidateToken(string userId)
        {
            var claims = new List<Claim>
            {
                new Claim("user_id", userId)
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new System.Security.Principal.GenericPrincipal(identity, null);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }
    }
}
