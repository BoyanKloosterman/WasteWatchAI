using Microsoft.AspNetCore.Identity;
using System.Data;
using WasteWatchAIBackend.Interface;

namespace WasteWatchAIBackend.Repositories
{
    public class IdentityRepository : IIdentityRepository
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IDbConnection _db;

        public IdentityRepository(UserManager<IdentityUser> userManager, IDbConnection db)
        {
            _userManager = userManager;
            _db = db;
        }

        public async Task<(bool Succeeded, string UserId, IEnumerable<string> Errors)> CreateUserAsync(string email, string password)
        {
            var user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true // Je kunt dit op false zetten als je email verificatie wilt
            };

            var result = await _userManager.CreateAsync(user, password);
            return (result.Succeeded, user.Id, result.Errors.Select(e => e.Description));
        }

        public async Task<bool> AddToRoleAsync(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var result = await _userManager.AddToRoleAsync(user, role);
            return result.Succeeded;
        }
    }
}
