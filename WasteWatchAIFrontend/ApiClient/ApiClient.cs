using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WasteWatchAIFrontend.Models;

namespace WasteWatchAIFrontend.ApiClient
{
    public class ApiClient
    {
        private static string apiBaseUrl = "http://localhost:8080";

        private readonly Authorization authorization;
        private readonly HttpClient httpClient;

        public ApiClient(HttpClient http, Authorization auth)
        {
            httpClient = http;
            authorization = auth;
        }

        public async Task<TrashItem> ApiCall(string endpoint)
        {
            using HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", authorization.Token);

            try
            {
                HttpResponseMessage response = await client.GetAsync(apiBaseUrl + endpoint);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();

                // Deserialize into TrashItem object
                TrashItem? trash = JsonSerializer.Deserialize<TrashItem>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (trash == null)
                {
                    Console.WriteLine("Deserialization returned null.");
                    return new TrashItem();
                }

                Console.WriteLine($"ID: {trash.Id}, Type: {trash.LitterType}, Date: {trash.Timestamp}");
                return trash;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                return new TrashItem();
            }
        }

        public async Task<List<TrashItem>> GetAllTrash()
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authorization.Token);

            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(apiBaseUrl + "/api/trashitems/dummy");
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();

                // Deserialize JSON array into List<TrashItem>
                List<TrashItem> trashList = JsonSerializer.Deserialize<List<TrashItem>>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<TrashItem>();

                return trashList;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                return new List<TrashItem>();
            }
        }
    }
}
