namespace WasteWatchAIFrontend.Models.Auth
{
    public class UserInfo
    {
        public string Email { get; set; } = string.Empty;
        public bool IsAuthenticated { get; set; }
        public List<string> Claims { get; set; } = new();
    }
}
