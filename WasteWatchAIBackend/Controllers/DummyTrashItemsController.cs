using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WasteWatchAIBackend.Models;
using WasteWatchAIBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace WasteWatchAIBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DummyTrashItemsController : ControllerBase
    {
        private readonly WasteWatchDbContext _context;

        public DummyTrashItemsController(WasteWatchDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DummyTrashItem>>> GetDummyTrashItems()
        {
            return await _context.DummyTrashItems.ToListAsync();
        }
    }
}
