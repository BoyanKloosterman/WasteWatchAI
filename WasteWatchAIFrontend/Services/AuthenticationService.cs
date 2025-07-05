using WasteWatchAIFrontend.Models;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Authentication;

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
        private readonly ProtectedSessionStorage _protectedStorage;
        private readonly ILogger<AuthenticationService> _logger;
        private const string TOKEN_KEY = "access_token";
        private const string USER_INFO_KEY = "user_info";

        // In-memory fallback for when JS interop is not available
        private static string? _tempToken;
        private static UserInfo? _tempUserInfo;

        public event Action? AuthenticationStateChanged;

        public AuthenticationService(
            IHttpClientFactory httpClientFactory,
            ProtectedSessionStorage protectedStorage,
            ILogger<AuthenticationService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("WasteWatchAPI");
            _protectedStorage = protectedStorage;
            _logger = logger;

            _logger.LogInformation("AuthenticationService initialized with base address: {BaseAddress}",
                _httpClient.BaseAddress?.ToString() ?? "NULL");
        }

        public async Task<bool> LoginAsync(LoginRequest loginRequest)
        {
            _logger.LogInformation("=== LOGIN ATTEMPT STARTED ===");
            _logger.LogInformation("Login attempt for email: {Email}", loginRequest.Email);
            _logger.LogInformation("HttpClient base address: {BaseAddress}", _httpClient.BaseAddress?.ToString() ?? "NULL");

            try
            {
                var json = JsonSerializer.Serialize(loginRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Serialized login request: {Json}", json);
                _logger.LogInformation("Making POST request to: {Url}", "/account/login");

                var response = await _httpClient.PostAsync("/account/login", content);

                _logger.LogInformation("Response status: {StatusCode} ({ReasonPhrase})",
                    response.StatusCode, response.ReasonPhrase);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Login response received successfully");
                    _logger.LogInformation("Response content length: {Length}", responseContent.Length);
                    _logger.LogInformation("Response content: {Content}", responseContent);

                    using (var document = JsonDocument.Parse(responseContent))
                    {
                        var root = document.RootElement;
                        _logger.LogInformation("JSON root element type: {Type}", root.ValueKind);

                        // Log all properties in the response
                        foreach (var property in root.EnumerateObject())
                        {
                            _logger.LogInformation("Response property: {Name} = {Value}",
                                property.Name, property.Value.ToString());
                        }

                        if (root.TryGetProperty("accessToken", out var accessTokenElement))
                        {
                            var accessToken = accessTokenElement.GetString();
                            _logger.LogInformation("Access token found: {HasValue}", !string.IsNullOrEmpty(accessToken));

                            if (!string.IsNullOrEmpty(accessToken))
                            {
                                _logger.LogInformation("Token length: {Length}", accessToken.Length);
                                _logger.LogInformation("Token starts with: {TokenStart}",
                                    accessToken.Length > 20 ? accessToken[..20] + "..." : accessToken);
                                _logger.LogInformation("Token ends with: {TokenEnd}",
                                    accessToken.Length > 20 ? "..." + accessToken[^10..] : accessToken);

                                // Store in memory first (for immediate use)
                                _tempToken = accessToken;
                                _tempUserInfo = new UserInfo
                                {
                                    Email = loginRequest.Email,
                                    IsAuthenticated = true
                                };

                                _logger.LogInformation("Token stored in memory successfully");
                                _logger.LogInformation("UserInfo created: Email={Email}, IsAuthenticated={IsAuth}",
                                    _tempUserInfo.Email, _tempUserInfo.IsAuthenticated);

                                // Try to store in session storage (may fail during prerendering)
                                try
                                {
                                    await _protectedStorage.SetAsync(TOKEN_KEY, accessToken);
                                    await _protectedStorage.SetAsync(USER_INFO_KEY, JsonSerializer.Serialize(_tempUserInfo));
                                    _logger.LogInformation("Token stored in session storage successfully");
                                }
                                catch (InvalidOperationException ex)
                                {
                                    _logger.LogInformation("Using in-memory storage (prerendering mode): {Message}", ex.Message);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Unexpected error storing token in session storage");
                                }

                                _logger.LogInformation("Invoking AuthenticationStateChanged event");
                                AuthenticationStateChanged?.Invoke();

                                _logger.LogInformation("=== LOGIN SUCCESSFUL ===");
                                return true;
                            }
                            else
                            {
                                _logger.LogError("Access token is null or empty");
                            }
                        }
                        else
                        {
                            _logger.LogError("No 'accessToken' property found in response");
                        }
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Login failed with status: {StatusCode} ({ReasonPhrase})",
                        response.StatusCode, response.ReasonPhrase);
                    _logger.LogError("Error response content: {ErrorContent}", errorContent);

                    // Log response headers
                    _logger.LogError("Response headers:");
                    foreach (var header in response.Headers)
                    {
                        _logger.LogError("  {Name}: {Value}", header.Key, string.Join(", ", header.Value));
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request exception during login");
                _logger.LogError("HttpRequestException details: {Message}", ex.Message);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Request timeout during login");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error during login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login");
            }

            _logger.LogError("=== LOGIN FAILED ===");
            return false;
        }

        public async Task<bool> RegisterAsync(RegisterRequest registerRequest)
        {
            _logger.LogInformation("=== REGISTRATION ATTEMPT STARTED ===");
            _logger.LogInformation("Registration attempt for email: {Email}", registerRequest.Email);

            try
            {
                var json = JsonSerializer.Serialize(registerRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Making POST request to: {Url}", "/account/register");

                var response = await _httpClient.PostAsync("/account/register", content);

                _logger.LogInformation("Registration response status: {StatusCode} ({ReasonPhrase})",
                    response.StatusCode, response.ReasonPhrase);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("=== REGISTRATION SUCCESSFUL ===");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Registration failed with status: {StatusCode}, Error: {Error}",
                        response.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
            }

            _logger.LogError("=== REGISTRATION FAILED ===");
            return false;
        }

        public async Task LogoutAsync()
        {
            _logger.LogInformation("=== LOGOUT STARTED ===");

            try
            {
                var token = await GetTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    _logger.LogInformation("Making logout API call with token");
                    await _httpClient.PostAsync("account/logout", null);
                    _logger.LogInformation("Logout API call completed");
                }
                else
                {
                    _logger.LogInformation("No token available for logout API call");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout API call");
            }
            finally
            {
                _logger.LogInformation("Clearing authentication data");

                // Clear memory
                _tempToken = null;
                _tempUserInfo = null;
                _logger.LogInformation("Cleared in-memory authentication data");

                // Clear session storage
                try
                {
                    await _protectedStorage.DeleteAsync(TOKEN_KEY);
                    await _protectedStorage.DeleteAsync(USER_INFO_KEY);
                    _logger.LogInformation("Cleared session storage authentication data");
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogInformation("Cannot clear session storage (prerendering mode): {Message}", ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error clearing session storage");
                }

                _logger.LogInformation("Invoking AuthenticationStateChanged event for logout");
                AuthenticationStateChanged?.Invoke();
            }

            _logger.LogInformation("=== LOGOUT COMPLETED ===");
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            _logger.LogInformation("=== CHECKING AUTHENTICATION STATUS ===");

            var token = await GetTokenAsync();
            var isAuthenticated = !string.IsNullOrEmpty(token);

            _logger.LogInformation("Authentication status: {IsAuthenticated}", isAuthenticated);
            _logger.LogInformation("Token available: {HasToken}", !string.IsNullOrEmpty(token));

            return isAuthenticated;
        }

        public async Task<UserInfo?> GetUserInfoAsync()
        {
            _logger.LogInformation("=== GETTING USER INFO ===");

            // Try in-memory first
            if (_tempUserInfo != null)
            {
                _logger.LogInformation("Retrieved user info from memory: {Email}", _tempUserInfo.Email);
                return _tempUserInfo;
            }

            // Try session storage
            try
            {
                var result = await _protectedStorage.GetAsync<string>(USER_INFO_KEY);
                if (result.Success && !string.IsNullOrEmpty(result.Value))
                {
                    var userInfo = JsonSerializer.Deserialize<UserInfo>(result.Value);
                    _logger.LogInformation("Retrieved user info from session storage: {Email}", userInfo?.Email);
                    return userInfo;
                }
                else
                {
                    _logger.LogInformation("No user info found in session storage");
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogInformation("Cannot access session storage for user info (prerendering mode): {Message}", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user info from session storage");
            }

            _logger.LogInformation("No user info available");
            return null;
        }

        public async Task<string?> GetTokenAsync()
        {
            _logger.LogInformation("=== GETTING TOKEN ===");

            // Try in-memory first
            if (!string.IsNullOrEmpty(_tempToken))
            {
                _logger.LogInformation("Retrieved token from memory (length: {Length})", _tempToken.Length);
                _logger.LogInformation("Token preview: {TokenStart}...{TokenEnd}",
                    _tempToken.Length > 20 ? _tempToken[..10] : _tempToken,
                    _tempToken.Length > 20 ? _tempToken[^10..] : "");
                return _tempToken;
            }

            // Try session storage
            try
            {
                var result = await _protectedStorage.GetAsync<string>(TOKEN_KEY);
                var token = result.Success ? result.Value : null;

                if (!string.IsNullOrEmpty(token))
                {
                    _logger.LogInformation("Retrieved token from session storage (length: {Length})", token.Length);
                    _logger.LogInformation("Token preview: {TokenStart}...{TokenEnd}",
                        token.Length > 20 ? token[..10] : token,
                        token.Length > 20 ? token[^10..] : "");
                }
                else
                {
                    _logger.LogInformation("No token found in session storage");
                }

                return token;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogInformation("Cannot access session storage for token (prerendering mode): {Message}", ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving token from session storage");
                return null;
            }
        }
    }
}