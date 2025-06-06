using Microsoft.EntityFrameworkCore;
using WasteWatchAIBackend.Models;

namespace WasteWatchAIBackend.Data
{
    public class WasteWatchDbContext : DbContext
    {
        public WasteWatchDbContext(DbContextOptions<WasteWatchDbContext> options)
            : base(options) { }

        public DbSet<WeatherData> WeatherData { get; set; }
        public DbSet<PredictionResult> PredictionResults { get; set; }
        public DbSet<TrashItem> TrashItems { get; set; }

        public DbSet<DummyTrashItem> DummyTrashItems { get; set; }

    }
}
