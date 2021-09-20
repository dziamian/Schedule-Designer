using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ScheduleDesigner.Authentication
{
    public class UsosAuthenticationService
    {
        private readonly string _usosUrl;
        private readonly HttpClient _client;

        public UsosAuthenticationService(string usosUrl)
        {
            _client = new HttpClient();
            _usosUrl = usosUrl;
        }

        public async Task<string> GetUserId(string token)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_usosUrl}/services/users/user")
            {
                Content = new StringContent("{\"fields\": \"id\", \"format\": \"json\"}", Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Authorization", token);
            return await SendUserRequestAsync(request);
        }

        private async Task<string> SendUserRequestAsync(HttpRequestMessage request)
        {
            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var userInfo = JsonSerializer.Deserialize<UserInfo>(json);
            return userInfo.Id;
        }
    }

    public class UserInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
    }
}
