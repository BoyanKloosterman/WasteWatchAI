using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using WasteWatchAIFrontend.Models.Auth;

namespace WasteWatchAIFrontend.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ProtectedLocalStorage _localStorage;
        private const string TokenKey = "authToken";
        private const string UserKey = "userInfo";

        public AuthService(IHttpClientFactory httpClientFactory, ProtectedLocalStorage localStorage)
        {
            _httpClient = httpClientFactory.CreateClient("WasteWatchAPI");
            _localStorage = localStorage;
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/Auth/login", request);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var authResponse = JsonSerializer.Deserialize<AuthResponse>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (authResponse != null && authResponse.Success)
                    {
                        await _localStorage.SetAsync(TokenKey, authResponse.Token);
                        
                        var userInfo = new UserInfo
                        {
                            Email = authResponse.Email,
                            IsAuthenticated = true
                        };
                        
                        await _localStorage.SetAsync(UserKey, userInfo);
                        return authResponse;
                    }
                }

                return new AuthResponse 
                { 
                    Success = false, 
                    Message = "Invalid login credentials" 
                };
            }
            catch (Exception ex)
            {
                return new AuthResponse 
                { 
                    Success = false, 
                    Message = $"Login failed: {ex.Message}" 
                };
            }
        }        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/Auth/register", request);
                var content = await response.Content.ReadAsStringAsync();

                // Log the response for debugging
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Content: {content}");

                if (response.IsSuccessStatusCode)
                {
                    var authResponse = JsonSerializer.Deserialize<AuthResponse>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (authResponse != null && authResponse.Success)
                    {
                        await _localStorage.SetAsync(TokenKey, authResponse.Token);
                        
                        var userInfo = new UserInfo
                        {
                            Email = authResponse.Email,
                            IsAuthenticated = true
                        };
                        
                        await _localStorage.SetAsync(UserKey, userInfo);
                        return authResponse;
                    }
                }

                // Try to parse validation errors
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<Dictionary<string, string[]>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    var errors = new List<string>();
                    if (errorResponse != null)
                    {
                        foreach (var error in errorResponse.Values)
                        {
                            errors.AddRange(error);
                        }
                    }

                    return new AuthResponse 
                    { 
                        Success = false, 
                        Message = $"Registration failed. Status: {response.StatusCode}. Content: {content}",
                        Errors = errors
                    };
                }
                catch
                {
                    return new AuthResponse 
                    { 
                        Success = false, 
                        Message = $"Registration failed. Status: {response.StatusCode}. Content: {content}" 
                    };
                }
            }
            catch (Exception ex)
            {
                return new AuthResponse 
                { 
                    Success = false, 
                    Message = $"Registration failed: {ex.Message}" 
                };
            }
        }

        public async Task LogoutAsync()
        {
            await _localStorage.DeleteAsync(TokenKey);
            await _localStorage.DeleteAsync(UserKey);
        }

        public async Task<UserInfo> GetCurrentUserAsync()
        {
            try
            {
                var result = await _localStorage.GetAsync<UserInfo>(UserKey);
                return result.Success ? result.Value! : new UserInfo();
            }
            catch
            {
                return new UserInfo();
            }
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            try
            {
                var tokenResult = await _localStorage.GetAsync<string>(TokenKey);
                if (!tokenResult.Success || string.IsNullOrEmpty(tokenResult.Value))
                    return false;

                var token = tokenResult.Value;
                var jwtHandler = new JwtSecurityTokenHandler();
                
                if (!jwtHandler.CanReadToken(token))
                    return false;

                var jwtToken = jwtHandler.ReadJwtToken(token);
                return jwtToken.ValidTo > DateTime.UtcNow;
            }
            catch
            {
                return false;
            }
        }

        public string? GetToken()
        {
            try
            {
                var tokenResult = _localStorage.GetAsync<string>(TokenKey).Result;
                return tokenResult.Success ? tokenResult.Value : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
