using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using WasteWatchAIBackend.Interface;

namespace WasteWatchAIBackend.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(
            IHttpContextAccessor httpContextAccessor, 
            UserManager<IdentityUser> userManager,
            ILogger<AuthenticationService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _logger = logger;
        }

        public Task<string?> GetUserIdAsync()
        {
            var context = _httpContextAccessor.HttpContext;
            _logger.LogDebug("Getting user ID from context. User authenticated: {IsAuthenticated}", 
                context?.User?.Identity?.IsAuthenticated);
            
            if (context?.User?.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                _logger.LogDebug("Retrieved user ID: {UserId}", userId);
                
                // Log all claims for debugging
                var claims = context.User.Claims.Select(c => new { c.Type, c.Value }).ToList();
                _logger.LogDebug("User claims: {@Claims}", claims);
                
                return Task.FromResult(userId);
            }
            
            _logger.LogDebug("User not authenticated, returning null");
            return Task.FromResult<string?>(null);
        }

        public Task<bool> IsUserAuthenticatedAsync()
        {
            var context = _httpContextAccessor.HttpContext;
            var isAuthenticated = context?.User?.Identity?.IsAuthenticated == true;
            
            _logger.LogDebug("Checking authentication status: {IsAuthenticated}", isAuthenticated);
            
            if (context?.Request?.Headers != null)
            {
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                _logger.LogDebug("Authorization header present: {HasAuthHeader}, Value: {AuthHeader}", 
                    !string.IsNullOrEmpty(authHeader), 
                    !string.IsNullOrEmpty(authHeader) ? "Bearer ***" : "None");
            }
            
            return Task.FromResult(isAuthenticated);
        }

        public async Task<string?> GetUserEmailAsync()
        {
            _logger.LogDebug("Getting user email");
            
            var userId = await GetUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogDebug("User ID is null or empty, cannot get email");
                return null;
            }

            var user = await _userManager.FindByIdAsync(userId);
            var email = user?.Email;
            
            _logger.LogDebug("Retrieved user email: {Email}", 
                !string.IsNullOrEmpty(email) ? email.Substring(0, Math.Min(3, email.Length)) + "***" : "None");
            
            return email;
        }
    }
}
