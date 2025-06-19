using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WasteWatchAIBackend.Models;
using WasteWatchAIBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace WasteWatchAIBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TrashItemsController : ControllerBase
    {
        private readonly WasteWatchDbContext _context;

        public TrashItemsController(WasteWatchDbContext context)
        {
            _context = context;
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

    }
}
