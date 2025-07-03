using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using WasteWatchAIBackend.Interface;

namespace WasteWatchAIBackend.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<IdentityUser> _userManager;

        public AuthenticationService(IHttpContextAccessor httpContextAccessor, UserManager<IdentityUser> userManager)
        {
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        public Task<string?> GetUserIdAsync()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context?.User?.Identity?.IsAuthenticated == true)
            {
                return Task.FromResult(context.User.FindFirstValue(ClaimTypes.NameIdentifier));
            }
            return Task.FromResult<string?>(null);
        }

        public Task<bool> IsUserAuthenticatedAsync()
        {
            var context = _httpContextAccessor.HttpContext;
            return Task.FromResult(context?.User?.Identity?.IsAuthenticated == true);
        }

        public async Task<string?> GetUserEmailAsync()
        {
            var userId = await GetUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            var user = await _userManager.FindByIdAsync(userId);
            return user?.Email;
        }
    }
}
