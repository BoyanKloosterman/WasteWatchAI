using Microsoft.EntityFrameworkCore;
using WasteWatchAIBackend.Models;

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
