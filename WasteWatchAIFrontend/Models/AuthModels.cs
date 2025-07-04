using System.ComponentModel.DataAnnotations;

namespace WasteWatchAIFrontend.Models
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Email is verplicht")]
        [EmailAddress(ErrorMessage = "Ongeldig email adres")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Wachtwoord is verplicht")]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        [Required(ErrorMessage = "Email is verplicht")]
        [EmailAddress(ErrorMessage = "Ongeldig email adres")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Wachtwoord is verplicht")]
        [MinLength(6, ErrorMessage = "Wachtwoord moet minimaal 6 karakters lang zijn")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$",
            ErrorMessage = "Wachtwoord moet minimaal 1 kleine letter, 1 hoofdletter en 1 cijfer bevatten")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bevestig je wachtwoord")]
        [Compare("Password", ErrorMessage = "Wachtwoorden komen niet overeen")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; } // Dit is meestal in seconden vanaf nu
        public string TokenType { get; set; } = "Bearer";
    }

    public class UserInfo
    {
        public string Email { get; set; } = string.Empty;
        public bool IsAuthenticated { get; set; }
        public List<string> Claims { get; set; } = new();
    }
}