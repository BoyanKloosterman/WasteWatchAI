using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WasteWatchAIBackend.Models;

namespace WasteWatchAIBackend.Data
{
    public class WasteWatchDbContext : IdentityDbContext<IdentityUser>
    {
        public WasteWatchDbContext(DbContextOptions<WasteWatchDbContext> options)
            : base(options)
        {

        }
        //public DbSet<Weather> Weather { get; set; }
        public DbSet<WeatherData> WeatherData { get; set; }
        public DbSet<TrashItem> TrashItems { get; set; }
        public DbSet<DummyTrashItem> DummyTrashItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<IdentityUser>().ToTable("AspNetUsers", "auth");
            modelBuilder.Entity<IdentityRole>().ToTable("AspNetRoles", "auth");
            modelBuilder.Entity<IdentityUserRole<string>>().ToTable("AspNetUserRoles", "auth");
            modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("AspNetUserClaims", "auth");
            modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("AspNetRoleClaims", "auth");
            modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("AspNetUserLogins", "auth");
            modelBuilder.Entity<IdentityUserToken<string>>().ToTable("AspNetUserTokens", "auth");

        }
    }
}
