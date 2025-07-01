using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using WasteWatchAIFrontend.Services.Auth;

namespace WasteWatchAIFrontend.Services.Auth
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IAuthService _authService;

        public CustomAuthenticationStateProvider(IAuthService authService)
        {
            _authService = authService;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var isAuthenticated = await _authService.IsAuthenticatedAsync();
            Console.WriteLine($"GetAuthenticationStateAsync - IsAuthenticated: {isAuthenticated}");
            
            if (isAuthenticated)
            {
                var userInfo = await _authService.GetCurrentUserAsync();
                var token = _authService.GetToken();
                
                if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(userInfo.Email))
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, userInfo.Email),
                        new Claim(ClaimTypes.Email, userInfo.Email),
                        new Claim("IsAuthenticated", "true")
                    };
                    
                    // Only try to parse JWT if it's not a Data Protection token
                    if (!token.StartsWith("CfDJ8"))
                    {
                        try
                        {
                            var jwtHandler = new JwtSecurityTokenHandler();
                            if (jwtHandler.CanReadToken(token))
                            {
                                var jwtToken = jwtHandler.ReadJwtToken(token);
                                claims.AddRange(jwtToken.Claims);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error parsing JWT: {ex.Message}");
                        }
                    }
                    
                    var identity = new ClaimsIdentity(claims, "bearer");
                    var user = new ClaimsPrincipal(identity);
                    
                    Console.WriteLine($"User authenticated with email: {userInfo.Email}");
                    return new AuthenticationState(user);
                }
            }
            
            Console.WriteLine("User not authenticated");
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        public void NotifyUserAuthentication(string email)
        {
            Console.WriteLine($"NotifyUserAuthentication called for: {email}");
            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Email, email),
                new Claim("IsAuthenticated", "true")
            };
            
            // Get token from storage
            var token = _authService.GetToken();
            if (!string.IsNullOrEmpty(token) && !token.StartsWith("CfDJ8"))
            {
                try
                {
                    var jwtHandler = new JwtSecurityTokenHandler();
                    if (jwtHandler.CanReadToken(token))
                    {
                        var jwtToken = jwtHandler.ReadJwtToken(token);
                        claims.AddRange(jwtToken.Claims);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing JWT in NotifyUserAuthentication: {ex.Message}");
                }
            }
            
            var identity = new ClaimsIdentity(claims, "bearer");
            var user = new ClaimsPrincipal(identity);
            
            Console.WriteLine($"Notifying authentication state changed for: {email}");
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }

        public void NotifyUserLogout()
        {
            var identity = new ClaimsIdentity();
            var user = new ClaimsPrincipal(identity);
            
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }
    }
}
