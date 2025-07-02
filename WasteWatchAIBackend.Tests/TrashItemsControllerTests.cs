using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using WasteWatchAIBackend.Controllers;
using WasteWatchAIBackend.Models;
using WasteWatchAIBackend.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace WasteWatchAIBackend.Tests
{
    [TestClass]
    public class TrashItemsControllerTests
    {
        private WasteWatchDbContext GetDbContextWithData()
        {
            var options = new DbContextOptionsBuilder<WasteWatchDbContext>()
                .UseInMemoryDatabase(databaseName: $"TrashItemsTestDb_{System.Guid.NewGuid()}")
                .Options;
            var context = new WasteWatchDbContext(options);

            // Add test data
            context.TrashItems.Add(new TrashItem { Id = System.Guid.NewGuid(), LitterType = "Plastic", Latitude = 1, Longitude = 2, Timestamp = System.DateTime.UtcNow });
            context.DummyTrashItems.Add(new DummyTrashItem { Id = System.Guid.NewGuid(), LitterType = "Paper", Latitude = 3, Longitude = 4, Timestamp = System.DateTime.UtcNow });
            context.SaveChanges();
            return context;
        }


        //[TestMethod]
        //public async Task GetTrashItems_ReturnsAllTrashItems()
        //{
        //    // Arrange
        //    var context = GetDbContextWithData();
        //    var controller = new TrashItemsController(context);

        //    // Act
        //    var result = await controller.GetTrashItems();

        //    // Assert
        //    Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
        //    var items = (result.Result as OkObjectResult).Value as IEnumerable<TrashItem>;
        //    Assert.IsNotNull(items);
        //    Assert.AreEqual(1, items.Count());
        //}

        //[TestMethod]
        //public async Task GetDummyTrashItems_ReturnsAllDummyTrashItems()
        //{
        //    // Arrange
        //    var context = GetDbContextWithData();
        //    var controller = new TrashItemsController(context);

        //    // Act
        //    var result = await controller.GetDummyTrashItems();

        //    // Assert
        //    Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
        //    var items = (result.Result as OkObjectResult).Value as IEnumerable<DummyTrashItem>;
        //    Assert.IsNotNull(items);
        //    Assert.AreEqual(1, items.Count());
        //}
    }
}