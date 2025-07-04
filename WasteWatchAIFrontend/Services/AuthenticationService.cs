using System.Threading.Tasks;
using WasteWatchAIFrontend.Models;
using WasteWatchAIFrontend.ApiClient;

namespace WasteWatchAIFrontend.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly Authorization _authorization;

        public AuthenticationService(Authorization authorization)
        {
            _authorization = authorization;
        }

        public async Task<bool> LoginAsync(LoginRequest loginModel)
        {
            return await _authorization.LoginAsync(loginModel.Email, loginModel.Password);
        }

        public async Task<(bool Success, string? ErrorMessage)> RegisterAsync(RegisterRequest registerModel)
        {
            return await _authorization.RegisterAsync(registerModel.Email, registerModel.Password, registerModel.ConfirmPassword);
        }
    }
}
