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
                var response = await _httpClient.PostAsJsonAsync("account/login", request);
                var content = await response.Content.ReadAsStringAsync();

                // Log the response for debugging
                Console.WriteLine($"Login Response Status: {response.StatusCode}");
                Console.WriteLine($"Login Response Content: {content}");

                if (response.IsSuccessStatusCode)
                {
                    var authResponse = JsonSerializer.Deserialize<AuthResponse>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (authResponse != null && !string.IsNullOrEmpty(authResponse.AccessToken))
                    {
                        // Store token
                        await _localStorage.SetAsync(TokenKey, authResponse.AccessToken);
                        
                        // Store user info
                        var userInfo = new UserInfo
                        {
                            Email = request.Email,
                            IsAuthenticated = true
                        };
                        await _localStorage.SetAsync(UserKey, userInfo);
                        
                        Console.WriteLine($"Login successful - Token stored: {authResponse.AccessToken[..20]}...");
                        return authResponse;
                    }
                }

                return new AuthResponse
                {
                    Message = $"Invalid login credentials. Status: {response.StatusCode}. Content: {content}"
                };
            }
            catch (Exception ex)
            {
                return new AuthResponse
                {
                    Message = $"Login failed: {ex.Message}"
                };
            }
        }        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("account/register", request);
                var content = await response.Content.ReadAsStringAsync();

                // Log the response for debugging
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Content: {content}");

                if (response.IsSuccessStatusCode)
                {
                    // ASP.NET Core Identity registration returns 200 OK with empty body on success
                    // We need to login after successful registration to get the token
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        // Registration successful, now login to get token
                        var loginRequest = new LoginRequest
                        {
                            Email = request.Email,
                            Password = request.Password
                        };
                        
                        var loginResult = await LoginAsync(loginRequest);
                        if (loginResult != null && !string.IsNullOrEmpty(loginResult.AccessToken))
                        {
                            return new AuthResponse
                            {
                                AccessToken = loginResult.AccessToken,
                                TokenType = loginResult.TokenType,
                                ExpiresIn = loginResult.ExpiresIn,
                                RefreshToken = loginResult.RefreshToken,
                                Message = "Registration successful"
                            };
                        }
                        else
                        {
                            return new AuthResponse
                            {
                                Message = "Registration successful, but login failed. Please try logging in manually."
                            };
                        }
                    }
                    else
                    {
                        // Try to parse response if there is content
                        var authResponse = JsonSerializer.Deserialize<AuthResponse>(content, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (authResponse != null && !string.IsNullOrEmpty(authResponse.AccessToken))
                        {
                            var userInfo = new UserInfo
                            {
                                Email = request.Email,
                                IsAuthenticated = true
                            };
                            await _localStorage.SetAsync(UserKey, userInfo);
                            await _localStorage.SetAsync(TokenKey, authResponse.AccessToken);
                            return authResponse;
                        }
                    }
                }

                // Try to parse error message
                string errorMsg = $"Registration failed. Status: {response.StatusCode}. Content: {content}";
                return new AuthResponse
                {
                    Message = errorMsg
                };
            }
            catch (Exception ex)
            {
                return new AuthResponse
                {
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
                {
                    Console.WriteLine("No token found in storage");
                    return false;
                }

                var token = tokenResult.Value;
                Console.WriteLine($"Token found: {token[..20]}...");

                // Check if it's a Data Protection token (starts with CfDJ8)
                if (token.StartsWith("CfDJ8"))
                {
                    // For Data Protection tokens, we can't validate expiry client-side
                    // Just return true if we have a token - the server will validate it
                    Console.WriteLine("Data Protection token detected - assuming valid");
                    return true;
                }

                // Handle JWT tokens
                var jwtHandler = new JwtSecurityTokenHandler();
                if (!jwtHandler.CanReadToken(token))
                {
                    Console.WriteLine("Token is not a valid JWT");
                    return false;
                }

                var jwtToken = jwtHandler.ReadJwtToken(token);
                var isValid = jwtToken.ValidTo > DateTime.UtcNow;
                Console.WriteLine($"JWT token validation: {isValid}");
                return isValid;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error validating token: {ex.Message}");
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
