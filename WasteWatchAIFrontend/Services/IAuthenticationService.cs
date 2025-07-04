using System.Threading.Tasks;
using WasteWatchAIFrontend.Models;

namespace WasteWatchAIFrontend.Services
{
    public interface IAuthenticationService
    {
        Task<bool> LoginAsync(LoginRequest loginModel);
        Task<(bool Success, string? ErrorMessage)> RegisterAsync(RegisterRequest registerModel);
    }
}
