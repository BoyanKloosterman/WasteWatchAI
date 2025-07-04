using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using WasteWatchAIBackend.Models;

namespace WasteWatchAIBackend.Data
{
    public class WasteWatchDbContext : IdentityDbContext
    {
        public WasteWatchDbContext(DbContextOptions<WasteWatchDbContext> options)
            : base(options)
        {

        }
        public DbSet<WeatherData> WeatherData { get; set; }
        public DbSet<TrashItem> TrashItems { get; set; }
        public DbSet<DummyTrashItem> DummyTrashItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
