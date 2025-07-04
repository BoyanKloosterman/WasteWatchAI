using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WasteWatchAIBackend.Models;
using WasteWatchAIBackend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace WasteWatchAIBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TrashItemsController : ControllerBase
    {
        private readonly WasteWatchDbContext _context;
        private readonly IAuthenticationService _authenticationService;

        public TrashItemsController(WasteWatchDbContext context, IAuthenticationService authenticationService)
        {
            _context = context;
            _authenticationService = authenticationService;
        }

          [HttpGet("trash")]
        public async Task<ActionResult<IEnumerable<TrashItem>>> GetTrashItems()
        {
            // Controleer of de gebruiker geauthenticeerd is via de authentication service
            var userId = _authenticationService.GetCurrentAuthenticatedUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User is not authenticated.");
            }
            
            var trashItems = await _context.TrashItems.ToListAsync();
            return Ok(trashItems);
        }

        [HttpGet("dummy")]
        public async Task<ActionResult<IEnumerable<DummyTrashItem>>> GetDummyTrashItems()
        {
            var dummyTrashItems = await _context.DummyTrashItems.ToListAsync();
            return Ok(dummyTrashItems);
        }
        
                // POST endpoint voor nieuwe TrashItems
        [HttpPost]
        public async Task<ActionResult<TrashItem>> CreateTrashItem(TrashItem trashItem)
        {
            try
            {
                _context.TrashItems.Add(trashItem);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetTrashItems), new { id = trashItem.Id }, trashItem);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating trash item: {ex.Message}");
            }
        }

        // POST endpoint voor nieuwe DummyTrashItems
        [HttpPost("dummy")]
        public async Task<ActionResult<DummyTrashItem>> CreateDummyTrashItem(DummyTrashItem dummyTrashItem)
        {
                        var userId = _authenticationService.GetCurrentAuthenticatedUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User is not authenticated.");
            }
            try
            {
                _context.DummyTrashItems.Add(dummyTrashItem);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetDummyTrashItems), new { id = dummyTrashItem.Id }, dummyTrashItem);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating dummy trash item: {ex.Message}");
            }
        }

    }
}
