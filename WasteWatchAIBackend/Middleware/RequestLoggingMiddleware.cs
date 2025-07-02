using System.Security.Claims;

namespace WasteWatchAIBackend.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;
        private static readonly HashSet<string> _staticFileExtensions = new HashSet<string>
        {
            ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".ico", ".svg", ".woff", ".woff2", ".ttf", ".eot"
        };

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var request = context.Request;
            
            // Skip logging for static files and health checks
            if (IsStaticFile(request.Path) || request.Path.StartsWithSegments("/health"))
            {
                await _next(context);
                return;
            }

            // Only log API requests and authentication-related requests
            bool shouldLog = request.Path.StartsWithSegments("/api") || 
                            request.Path.StartsWithSegments("/account") ||
                            context.User?.Identity?.IsAuthenticated == true;

            if (shouldLog)
            {
                var hasAuthHeader = request.Headers.ContainsKey("Authorization");
                var authHeaderValue = hasAuthHeader ? request.Headers["Authorization"].FirstOrDefault() : null;
                
                _logger.LogDebug("HTTP {Method} {Path} | Auth Header: {HasAuth} | User Authenticated: {IsAuthenticated}", 
                    request.Method, 
                    request.Path, 
                    hasAuthHeader,
                    context.User?.Identity?.IsAuthenticated == true);

                if (hasAuthHeader && !string.IsNullOrEmpty(authHeaderValue))
                {
                    var tokenPreview = authHeaderValue.StartsWith("Bearer ") && authHeaderValue.Length > 20 
                        ? "Bearer " + authHeaderValue.Substring(7, 10) + "..."
                        : "Bearer [token]";
                    _logger.LogDebug("Authorization header: {AuthHeader}", tokenPreview);
                }

                // Log user claims if authenticated
                if (context.User?.Identity?.IsAuthenticated == true)
                {
                    var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var email = context.User.FindFirstValue(ClaimTypes.Email);
                    
                    _logger.LogDebug("Authenticated user - ID: {UserId}, Email: {Email}", userId, email);
                }
            }

            await _next(context);
        }

        private static bool IsStaticFile(PathString path)
        {
            if (!path.HasValue) return false;
            
            var extension = Path.GetExtension(path.Value);
            return !string.IsNullOrEmpty(extension) && _staticFileExtensions.Contains(extension.ToLowerInvariant());
        }
    }

    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
}
