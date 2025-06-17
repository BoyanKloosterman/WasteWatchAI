using Microsoft.VisualStudio.TestTools.UnitTesting;
using WasteWatchAIBackend.Repository;
using WasteWatchAIBackend.Data;
using WasteWatchAIBackend.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;

namespace WasteWatchAIBackend.Tests
{
    [TestClass]
    public class WeatherRepositoryTests
    {
        private WasteWatchDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<WasteWatchDbContext>()
                .UseInMemoryDatabase(databaseName: $"WeatherRepoTestDb_{System.Guid.NewGuid()}")
                .Options;
            var context = new WasteWatchDbContext(options);

            // Clear existing data to ensure a clean state
            context.WeatherData.RemoveRange(context.WeatherData);
            context.SaveChanges();

            return context;
        }



        [TestMethod]
        public async Task SaveWeatherAsync_AddsWeatherData()
        {
            // Arrange
            var context = GetDbContext();
            var repo = new WeatherRepository(context);
            var data = new WeatherData
            {
                Id = System.Guid.NewGuid(),
                Timestamp = System.DateTime.UtcNow,
                Latitude = 1,
                Longitude = 2,
                Temperature = 20,
                WeatherDescription = "Helder"
            };

            // Act
            await repo.SaveWeatherAsync(data);

            // Assert
            Assert.AreEqual(1, context.WeatherData.Count());
        }

        [TestMethod]
        public async Task GetAllAsync_ReturnsAllWeatherData()
        {
            // Arrange
            var context = GetDbContext();
            context.WeatherData.Add(new WeatherData
            {
                Id = System.Guid.NewGuid(),
                Timestamp = System.DateTime.UtcNow,
                Latitude = 1,
                Longitude = 2,
                Temperature = 20,
                WeatherDescription = "Helder"
            });
            context.SaveChanges();
            var repo = new WeatherRepository(context);

            // Act
            var result = await repo.GetAllAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
        }
    }
}