using WasteWatchAIFrontend.Services;

namespace WasteWatchAIFrontend.Services
{
    public interface ITokenInitializationService
    {
        Task InitializeAsync();
    }

    public class TokenInitializationService : ITokenInitializationService
    {
        private readonly IAuthenticationService _authService;
        private readonly ILogger<TokenInitializationService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public TokenInitializationService(
            IAuthenticationService authService,
            ILogger<TokenInitializationService> logger,
            IServiceProvider serviceProvider)
        {
            _authService = authService;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("Starting token initialization");

            try
            {
                // Check if user has a stored token
                var token = await _authService.GetTokenAsync();
                var isAuthenticated = await _authService.IsAuthenticatedAsync();

                _logger.LogInformation("Token initialization check - Has token: {HasToken}, Is authenticated: {IsAuthenticated}", 
                    !string.IsNullOrEmpty(token), isAuthenticated);

                if (!string.IsNullOrEmpty(token))
                {
                    _logger.LogDebug("Found stored token with length: {TokenLength}", token.Length);

                    // Try to get user info to validate token
                    var userInfo = await _authService.GetUserInfoAsync();
                    if (userInfo != null)
                    {
                        _logger.LogInformation("User info loaded for: {Email}", userInfo.Email);
                    }
                    else
                    {
                        _logger.LogWarning("No user info found despite having a token");
                    }
                }
                else
                {
                    _logger.LogInformation("No stored token found - user needs to login");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token initialization");
            }

            _logger.LogInformation("Token initialization completed");
        }
    }
}
