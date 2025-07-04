public interface IAuthenticationService
    {
        /// <summary>
        /// Returns the user name of the authenticated user
        /// </summary>
        /// <returns></returns>
        string? GetCurrentAuthenticatedUserId();
    }

// This service is no longer needed with JWT authentication and direct claims access.