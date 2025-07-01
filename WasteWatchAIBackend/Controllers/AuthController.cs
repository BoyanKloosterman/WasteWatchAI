using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WasteWatchAIBackend.Interface;

namespace WasteWatchAIBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IIdentityRepository _identityRepository;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IIdentityRepository identityRepository,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<AuthController> logger)
        {
            _identityRepository = identityRepository;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] AuthRegisterRequest request)
        {
            try
            {
                _logger.LogInformation("Registration attempt for email: {Email}", request?.Email ?? "null");
                
                if (request == null)
                {
                    return BadRequest("Request is required");
                }
                
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model validation failed for registration");
                    return BadRequest(ModelState);
                }

                var result = await _identityRepository.CreateUserAsync(request.Email, request.Password);
                _logger.LogInformation("Identity creation result: {Succeeded}", result.Succeeded);

                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(request.Email);
                    if (user != null)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        _logger.LogInformation("User registered and signed in successfully: {Email}", request.Email);
                        
                        var response = new AuthResponse
                        {
                            Success = true,
                            Token = "dummy-token", // You might want to implement JWT token generation
                            Email = request.Email,
                            Message = "Registration successful"
                        };
                        
                        _logger.LogInformation("Returning success response: {@Response}", response);
                        return Ok(response);
                    }
                    else
                    {
                        _logger.LogError("User creation succeeded but user not found in database");
                    }
                }
                else
                {
                    _logger.LogWarning("Registration failed for {Email}. Errors: {Errors}", 
                        request?.Email ?? "unknown", string.Join(", ", result.Errors));
                }

                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Registration failed",
                    Errors = result.Errors.ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Message = "Internal server error during registration"
                });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AuthLoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return Unauthorized(new AuthResponse
                    {
                        Success = false,
                        Message = "Invalid credentials"
                    });
                }

                var result = await _signInManager.PasswordSignInAsync(user, request.Password, false, false);

                if (result.Succeeded)
                {
                    return Ok(new AuthResponse
                    {
                        Success = true,
                        Token = "dummy-token", // You might want to implement JWT token generation
                        Email = request.Email,
                        Message = "Login successful"
                    });
                }

                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = "Invalid credentials"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Message = "Internal server error during login"
                });
            }
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            _logger.LogInformation("Test endpoint called");
            return Ok(new { Message = "Auth API is working", Timestamp = DateTime.UtcNow });
        }
    }

    // Models for the controller
    public class AuthRegisterRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class AuthLoginRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
    }
}
