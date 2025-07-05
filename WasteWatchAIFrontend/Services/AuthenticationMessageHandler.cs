using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;

namespace WasteWatchAIFrontend.Services
{
    public class AuthenticationMessageHandler : DelegatingHandler
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AuthenticationMessageHandler> _logger;

        public AuthenticationMessageHandler(
            IServiceProvider serviceProvider,
            ILogger<AuthenticationMessageHandler> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Resolve the authentication service lazily to avoid circular dependency
            using var scope = _serviceProvider.CreateScope();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();

            // Get the token
            var token = await authService.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                _logger.LogInformation("Adding Bearer token to request: {Method} {Uri}",
                    request.Method, request.RequestUri);
                // Add the Bearer token to the Authorization header
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                _logger.LogInformation("Authorization header set: Bearer {TokenPreview}...",
                    token.Length > 20 ? token[..20] : token);
            }
            else
            {
                _logger.LogWarning("No token available for request: {Method} {Uri}",
                    request.Method, request.RequestUri);
            }

            // Continue with the request
            var response = await base.SendAsync(request, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Received 401 Unauthorized response for: {Method} {Uri}",
                    request.Method, request.RequestUri);
            }

            return response;
        }
    }
}