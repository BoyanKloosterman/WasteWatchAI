using WasteWatchAIFrontend.Models;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace WasteWatchAIFrontend.Services
{
    public interface IAuthenticationService
    {
        event Action? AuthenticationStateChanged;
        Task<bool> LoginAsync(LoginRequest loginRequest);
        Task<bool> RegisterAsync(RegisterRequest registerRequest);
        Task LogoutAsync();
        Task<bool> IsAuthenticatedAsync();
        Task<UserInfo?> GetUserInfoAsync();
        Task<string?> GetTokenAsync();
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly HttpClient _httpClient;
        private readonly ProtectedLocalStorage _protectedStorage;
        private readonly ILogger<AuthenticationService> _logger;
        private const string TOKEN_KEY = "access_token";
        private const string USER_INFO_KEY = "user_info";

        public event Action? AuthenticationStateChanged;

        public AuthenticationService(
            IHttpClientFactory httpClientFactory,
            ProtectedLocalStorage protectedStorage,
            ILogger<AuthenticationService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("WasteWatchAPI");
            _protectedStorage = protectedStorage;
            _logger = logger;
        }

        public async Task<bool> LoginAsync(LoginRequest loginRequest)
        {
            try
            {
                var json = JsonSerializer.Serialize(loginRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/account/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();

                    // Parse JSON response als dynamic object
                    using (var document = JsonDocument.Parse(responseContent))
                    {
                        var root = document.RootElement;

                        // Probeer de access token te verkrijgen
                        if (root.TryGetProperty("accessToken", out var accessTokenElement) &&
                            !string.IsNullOrEmpty(accessTokenElement.GetString()))
                        {
                            var accessToken = accessTokenElement.GetString()!;

                            // Token opslaan
                            await _protectedStorage.SetAsync(TOKEN_KEY, accessToken);

                            // User info opslaan
                            var userInfo = new UserInfo
                            {
                                Email = loginRequest.Email,
                                IsAuthenticated = true
                            };
                            await _protectedStorage.SetAsync(USER_INFO_KEY, JsonSerializer.Serialize(userInfo));

                            // Default authorization header instellen
                            _httpClient.DefaultRequestHeaders.Authorization =
                                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                            // Notify components that authentication state has changed
                            AuthenticationStateChanged?.Invoke();

                            return true;
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Login failed with status: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
            }

            return false;
        }

        public async Task<bool> RegisterAsync(RegisterRequest registerRequest)
        {
            try
            {
                var json = JsonSerializer.Serialize(registerRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/account/register", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("User registered successfully");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Registration failed with status: {StatusCode}, Error: {Error}",
                        response.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
            }

            return false;
        }

        public async Task LogoutAsync()
        {
            try
            {
                // Logout API call (optioneel)
                var token = await GetTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                    await _httpClient.PostAsync("account/logout", null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout API call");
            }
            finally
            {
                // Lokale data verwijderen
                await _protectedStorage.DeleteAsync(TOKEN_KEY);
                await _protectedStorage.DeleteAsync(USER_INFO_KEY);

                // Authorization header verwijderen
                _httpClient.DefaultRequestHeaders.Authorization = null;

                // Notify components that authentication state has changed
                AuthenticationStateChanged?.Invoke();
            }
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            try
            {
                var result = await _protectedStorage.GetAsync<string>(TOKEN_KEY);
                return result.Success && !string.IsNullOrEmpty(result.Value);
            }
            catch
            {
                return false;
            }
        }

        public async Task<UserInfo?> GetUserInfoAsync()
        {
            try
            {
                var result = await _protectedStorage.GetAsync<string>(USER_INFO_KEY);
                if (result.Success && !string.IsNullOrEmpty(result.Value))
                {
                    return JsonSerializer.Deserialize<UserInfo>(result.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user info");
            }

            return null;
        }

        public async Task<string?> GetTokenAsync()
        {
            try
            {
                var result = await _protectedStorage.GetAsync<string>(TOKEN_KEY);
                return result.Success ? result.Value : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
