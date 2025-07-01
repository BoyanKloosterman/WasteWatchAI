namespace WasteWatchAIFrontend.Models.Auth
{
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
    }
}
