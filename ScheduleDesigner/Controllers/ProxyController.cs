using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ScheduleDesigner.Helpers;

namespace ScheduleDesigner.Controllers
{
    /// <summary>
    /// Kontroler API, który posiada rolę pośrednika pomiędzy klientem 
    /// a zewnętrznym systemem USOS w celu uniknięcia problemów wynikających z mechanizmu CORS.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ProxyController : ControllerBase
    {
        /// <summary>
        /// Instancja klasy przeznaczonej do wysyłania i odbierania żądań HTTP do/z USOS API.
        /// </summary>
        private readonly HttpClient _client;

        /// <summary>
        /// Konstruktor kontrolera wykorzystujący wstrzykiwanie zależności.
        /// </summary>
        /// <param name="applicationInfo">Wstrzyknięta instancja konfiguracji aplikacji</param>
        public ProxyController(IOptions<ApplicationOptions> applicationInfo)
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri(applicationInfo.Value.BaseUsosUrl)
            };
        }

        /// <summary>
        /// Wysyła żądanie do USOS API, aby zostać przekierowanym na stronę logowania do systemu USOS w celu zautoryzowania tokenu dostępu.
        /// </summary>
        /// <param name="oauth_token">Token dostępu do autoryzacji</param>
        /// <param name="interactivity">Poziom interaktywności z systemem</param>
        /// <returns>Odpowiedź systemu USOS na wysłane żądanie (powinno posiadać adres lokalizacji strony do logowania)</returns>
        /// <response code="200">Zwrócono odpowiedź systemu USOS</response>
        [HttpGet("Authorize")]
        [ProducesResponseType(200)]
        public async Task<ActionResult> Authorize([FromQuery] string oauth_token, [FromQuery] string interactivity) 
        {
            var queryStrings = new Dictionary<string, string>()
            {
                { "oauth_token", oauth_token },
                { "interactivity", interactivity }
            };
            var requestUri = QueryHelpers.AddQueryString("services/oauth/authorize", queryStrings);
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            var response = await _client.SendAsync(request);
            return Ok(response);
        }
    }
}
