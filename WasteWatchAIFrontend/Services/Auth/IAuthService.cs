using WasteWatchAIFrontend.Models.Auth;

namespace WasteWatchAIFrontend.Services.Auth
{
    public interface IAuthService
    {
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task LogoutAsync();
        Task<UserInfo> GetCurrentUserAsync();
        Task<bool> IsAuthenticatedAsync();
        string? GetToken();
    }
}
