using Microsoft.EntityFrameworkCore;

namespace WasteWatchAIBackend.Data
{
    public class WasteWatchDbContext : DbContext
    {
        public WasteWatchDbContext(DbContextOptions<WasteWatchDbContext> options)
            : base(options)
        {
        }
    }
}
