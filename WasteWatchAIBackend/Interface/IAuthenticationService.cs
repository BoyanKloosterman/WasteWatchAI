namespace WasteWatchAIBackend.Interface
{
    public interface IAuthenticationService
    {
        Task<string?> GetUserIdAsync();
        Task<bool> IsUserAuthenticatedAsync();
        Task<string?> GetUserEmailAsync();
    }
}
