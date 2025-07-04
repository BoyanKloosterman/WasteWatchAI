using System.Text;
using System.Text.Json;

namespace WasteWatchAIFrontend.ApiClient
{
    public class Authorization
    {
        private readonly HttpClient _http;

        public string? Token { get; private set; }

        public Authorization(HttpClient http)
        {
            _http = http;
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            var payload = new { email, password };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("http://localhost:8080/account/login", content);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(body);
                Token = doc.RootElement.GetProperty("token").GetString();
                return true;
            }

            return false;
        }

        public async Task<bool> RegisterAsync(string email, string username, string password)
        {
            var payload = new { email, username, password };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("http://localhost:8080/account/register", content);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(body);
                return true;
            }

            return false;
        }

        public void Logout()
        {
            Token = null;
        }

        public bool IsLoggedIn()
        {
            if (Token == null || Token.Trim() == "")
                return false;
            else
                return true;
        }
}
}
