using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WasteWatchAIBackend.Models;
using WasteWatchAIBackend.Data;
using Microsoft.EntityFrameworkCore;
using WasteWatchAIBackend.Interface;

namespace WasteWatchAIBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize] // Beveilig de hele controller
    public class TrashItemsController : ControllerBase
    {
        private readonly WasteWatchDbContext _context;
        private readonly IAuthenticationService _authService;

        public TrashItemsController(WasteWatchDbContext context, IAuthenticationService authService)
        {
            _context = context;
            _authService = authService;
        }

        [HttpGet("trash")]
        public async Task<ActionResult<IEnumerable<TrashItem>>> GetTrashItems()
        {
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
                // Optioneel: voeg de gebruiker ID toe aan het trash item
                var userId = await _authService.GetUserIdAsync();
                if (!string.IsNullOrEmpty(userId))
                {
                    // Als je een UserId property hebt in je TrashItem model:
                    // trashItem.UserId = userId;
                }

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
