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
        //public DbSet<Weather> Weather { get; set; }
        public DbSet<WeatherData> WeatherData { get; set; }
        public DbSet<PredictionResult> PredictionResults { get; set; }
        public DbSet<CategoryPrediction> CategoryPredictions { get; set; }
        public DbSet<TrashItem> TrashItems { get; set; }
        public DbSet<DummyTrashItem> DummyTrashItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PredictionResult>()
                .HasMany(p => p.Predictions)
                .WithOne(cp => cp.PredictionResult)
                .HasForeignKey(cp => cp.PredictionResultId);
        }
    }
}
