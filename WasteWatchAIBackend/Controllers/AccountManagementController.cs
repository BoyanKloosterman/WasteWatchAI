using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WasteWatchAIBackend.Controllers
{
    [Route("account/[controller]")]
    [ApiController]
    [Authorize]
    public class AccountManagementController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;

        public AccountManagementController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        /// <summary>
        /// Voegt een claim toe aan een gebruiker
        /// </summary>
        /// <param name="userId">Het ID van de gebruiker</param>
        /// <param name="claimType">Het type van de claim</param>
        /// <param name="claimValue">De waarde van de claim (optioneel)</param>
        /// <returns>Het resultaat van de operatie</returns>
        [HttpPost("{userId}/claims")]
        public async Task<IActionResult> AddClaimToUser(string userId, [FromBody] AddClaimRequest request)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("Gebruiker niet gevonden");
            }

            var claim = new Claim(request.ClaimType, request.ClaimValue ?? "");
            var result = await _userManager.AddClaimAsync(user, claim);

            if (result.Succeeded)
            {
                return Ok(new { Message = "Claim succesvol toegevoegd", UserId = userId, ClaimType = request.ClaimType, ClaimValue = request.ClaimValue });
            }

            return BadRequest(result.Errors);
        }

        /// <summary>
        /// Haalt alle claims op van een gebruiker
        /// </summary>
        /// <param name="userId">Het ID van de gebruiker</param>
        /// <returns>Lijst met claims</returns>
        [HttpGet("{userId}/claims")]
        public async Task<IActionResult> GetUserClaims(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("Gebruiker niet gevonden");
            }

            var claims = await _userManager.GetClaimsAsync(user);
            var claimsList = claims.Select(c => new { Type = c.Type, Value = c.Value }).ToList();

            return Ok(claimsList);
        }

        /// <summary>
        /// Verwijdert een claim van een gebruiker
        /// </summary>
        /// <param name="userId">Het ID van de gebruiker</param>
        /// <param name="claimType">Het type van de claim</param>
        /// <param name="claimValue">De waarde van de claim</param>
        /// <returns>Het resultaat van de operatie</returns>
        [HttpDelete("{userId}/claims")]
        public async Task<IActionResult> RemoveClaimFromUser(string userId, [FromQuery] string claimType, [FromQuery] string claimValue = "")
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("Gebruiker niet gevonden");
            }

            var claim = new Claim(claimType, claimValue);
            var result = await _userManager.RemoveClaimAsync(user, claim);

            if (result.Succeeded)
            {
                return Ok(new { Message = "Claim succesvol verwijderd" });
            }

            return BadRequest(result.Errors);
        }

        /// <summary>
        /// Haalt alle gebruikers op
        /// </summary>
        /// <returns>Lijst met gebruikers</returns>
        [HttpGet("users")]
        public IActionResult GetAllUsers()
        {
            var users = _userManager.Users.Select(u => new { 
                Id = u.Id, 
                Email = u.Email, 
                UserName = u.UserName,
                EmailConfirmed = u.EmailConfirmed
            }).ToList();

            return Ok(users);
        }

        /// <summary>
        /// Haalt de huidige ingelogde gebruiker op
        /// </summary>
        /// <returns>De huidige gebruiker</returns>
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("Gebruiker niet gevonden");
            }

            var claims = await _userManager.GetClaimsAsync(user);
            
            return Ok(new
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                EmailConfirmed = user.EmailConfirmed,
                Claims = claims.Select(c => new { Type = c.Type, Value = c.Value }).ToList()
            });
        }
    }

    public class AddClaimRequest
    {
        public string ClaimType { get; set; } = string.Empty;
        public string? ClaimValue { get; set; }
    }
}
