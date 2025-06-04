using Microsoft.EntityFrameworkCore;
using WasteWatchAIBackend.Model;

namespace WasteWatchAIBackend.Data
{
    public class WasteWatchDbContext : DbContext
    {
        public WasteWatchDbContext(DbContextOptions<WasteWatchDbContext> options)
            : base(options)
        {

        }
        public DbSet<Weather> Weather { get; set; }
    }
}
