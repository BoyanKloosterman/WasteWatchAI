using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WasteWatchAIBackend.Data;
using WasteWatchAIBackend.Models;

namespace WasteWatchAIBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TrashItemsController : ControllerBase
    {
        private readonly WasteWatchDbContext _context;

        public TrashItemsController(WasteWatchDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TrashItem>>> GetTrashItems()
        {
            return await _context.TrashItems.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TrashItem>> GetTrashItem(Guid id)
        {
            var item = await _context.TrashItems.FindAsync(id);
            if (item == null) return NotFound();
            return item;
        }

        [HttpPost]
        public async Task<ActionResult<TrashItem>> PostTrashItem(TrashItem item)
        {
            item.Id = Guid.NewGuid();
            item.Timestamp = DateTime.UtcNow;
            _context.TrashItems.Add(item);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTrashItem), new { id = item.Id }, item);
        }

    }
}
