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
    [Route("api/[controller]")]
    [ApiController]
    public class ProxyController : ControllerBase
    {
        private readonly IOptions<Consumer> _usosConsumer;
        private readonly HttpClient _client;

        public ProxyController(IOptions<ApplicationOptions> applicationInfo, IOptions<Consumer> usosConsumer)
        {
            _usosConsumer = usosConsumer;
            _client = new HttpClient
            {
                BaseAddress = new Uri(applicationInfo.Value.BaseUsosUrl)
            };
        }

        [HttpGet("Authorize")]
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
