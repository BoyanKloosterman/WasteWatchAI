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

        public async Task<bool> RegisterAsync(RegisterRequest registerModel)
        {
            // Username is not in RegisterRequest, so use Email as username
            return await _authorization.RegisterAsync(registerModel.Email, registerModel.Email, registerModel.Password);
        }
    }
}
