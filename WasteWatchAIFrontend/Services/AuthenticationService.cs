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
            _logger.LogInformation("Starting login process for user: {Email}", loginRequest.Email);
            
            try
            {
                var json = JsonSerializer.Serialize(loginRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogDebug("Sending login request to /account/login");
                var response = await _httpClient.PostAsync("/account/login", content);
                _logger.LogDebug("Login response status: {StatusCode}", response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug("Login response content length: {Length}", responseContent.Length);
                    
                    // Parse JSON response als dynamic object
                    using (var document = JsonDocument.Parse(responseContent))
                    {
                        var root = document.RootElement;
                        
                        // Probeer de access token te verkrijgen
                        if (root.TryGetProperty("accessToken", out var accessTokenElement) && 
                            !string.IsNullOrEmpty(accessTokenElement.GetString()))
                        {
                            var accessToken = accessTokenElement.GetString()!;
                            _logger.LogInformation("Successfully received access token (length: {TokenLength})", accessToken.Length);
                            
                            // Token opslaan
                            await _protectedStorage.SetAsync(TOKEN_KEY, accessToken);
                            _logger.LogDebug("Access token stored in protected storage");
                            
                            // User info opslaan
                            var userInfo = new UserInfo
                            {
                                Email = loginRequest.Email,
                                IsAuthenticated = true
                            };
                            await _protectedStorage.SetAsync(USER_INFO_KEY, JsonSerializer.Serialize(userInfo));
                            _logger.LogDebug("User info stored in protected storage");

                            // Default authorization header instellen
                            _httpClient.DefaultRequestHeaders.Authorization = 
                                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                            _logger.LogDebug("Authorization header set on HttpClient");

                            // Notify components that authentication state has changed
                            AuthenticationStateChanged?.Invoke();
                            _logger.LogInformation("Login successful for user: {Email}", loginRequest.Email);

                            return true;
                        }
                        else
                        {
                            _logger.LogWarning("No access token found in login response");
                        }
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Login failed with status: {StatusCode}, Error: {Error}", 
                        response.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user: {Email}", loginRequest.Email);
            }

            _logger.LogWarning("Login failed for user: {Email}", loginRequest.Email);
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
            _logger.LogInformation("Starting logout process");
            
            try
            {
                // Logout API call (optioneel)
                var token = await GetTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    _logger.LogDebug("Token found, making logout API call");
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    
                    await _httpClient.PostAsync("account/logout", null);
                    _logger.LogDebug("Logout API call completed");
                }
                else
                {
                    _logger.LogDebug("No token found for logout API call");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout API call");
            }
            finally
            {
                // Lokale data verwijderen
                _logger.LogDebug("Clearing local storage");
                await _protectedStorage.DeleteAsync(TOKEN_KEY);
                await _protectedStorage.DeleteAsync(USER_INFO_KEY);
                
                // Authorization header verwijderen
                _httpClient.DefaultRequestHeaders.Authorization = null;
                _logger.LogDebug("Authorization header cleared from HttpClient");
                
                // Notify components that authentication state has changed
                AuthenticationStateChanged?.Invoke();
                _logger.LogInformation("Logout completed successfully");
            }
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            _logger.LogDebug("Checking authentication status");
            
            try
            {
                var result = await _protectedStorage.GetAsync<string>(TOKEN_KEY);
                var isAuthenticated = result.Success && !string.IsNullOrEmpty(result.Value);
                
                _logger.LogDebug("Authentication check result: {IsAuthenticated}, Token present: {HasToken}", 
                    isAuthenticated, result.Success && !string.IsNullOrEmpty(result.Value));
                
                if (isAuthenticated && !string.IsNullOrEmpty(result.Value))
                {
                    _logger.LogDebug("Token length: {TokenLength}", result.Value.Length);
                    
                    // Set authorization header if token exists but header is not set
                    if (_httpClient.DefaultRequestHeaders.Authorization == null)
                    {
                        _httpClient.DefaultRequestHeaders.Authorization = 
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result.Value);
                        _logger.LogDebug("Authorization header restored from stored token");
                    }
                }
                
                return isAuthenticated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking authentication status");
                return false;
            }
        }

        public async Task<UserInfo?> GetUserInfoAsync()
        {
            _logger.LogDebug("Getting user info from storage");
            
            try
            {
                var result = await _protectedStorage.GetAsync<string>(USER_INFO_KEY);
                if (result.Success && !string.IsNullOrEmpty(result.Value))
                {
                    var userInfo = JsonSerializer.Deserialize<UserInfo>(result.Value);
                    _logger.LogDebug("User info retrieved: {Email}, Authenticated: {IsAuthenticated}", 
                        userInfo?.Email, userInfo?.IsAuthenticated);
                    return userInfo;
                }
                else
                {
                    _logger.LogDebug("No user info found in storage");
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
            _logger.LogDebug("Getting token from storage");
            
            try
            {
                var result = await _protectedStorage.GetAsync<string>(TOKEN_KEY);
                var hasToken = result.Success && !string.IsNullOrEmpty(result.Value);
                
                _logger.LogDebug("Token retrieval result: Success={Success}, HasValue={HasValue}", 
                    result.Success, hasToken);
                
                if (hasToken)
                {
                    _logger.LogDebug("Token found with length: {TokenLength}", result.Value!.Length);
                }
                
                return hasToken ? result.Value : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting token from storage");
                return null;
            }
        }
    }
}
