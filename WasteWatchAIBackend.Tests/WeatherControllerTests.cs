using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WasteWatchAIBackend.Controller;
using WasteWatchAIBackend.Interface;
using WasteWatchAIBackend.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System;
using Moq.Protected;

namespace WasteWatchAIBackend.Tests
{
    [TestClass]
    public class WeatherControllerTests
    {
        [TestMethod]
        public async Task GetAll_ReturnsOkWithData()
        {
            // Arrange
            var mockRepo = new Mock<IWeatherRepository>();
            mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<WeatherData> { new WeatherData() });
            var mockFactory = new Mock<IHttpClientFactory>();
            var controller = new WeatherController(mockRepo.Object, mockFactory.Object);

            // Act
            var result = await controller.GetAll();

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okResult = result as OkObjectResult;
            Assert.IsInstanceOfType(okResult.Value, typeof(IEnumerable<WeatherData>));
        }

        [TestMethod]
        public async Task FetchAndStoreWeather_FailedApiCall_ReturnsError()
        {
            // Arrange
            var mockRepo = new Mock<IWeatherRepository>();
            var mockFactory = new Mock<IHttpClientFactory>();
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("error")
                });
            var client = new HttpClient(handler.Object);
            mockFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

            var controller = new WeatherController(mockRepo.Object, mockFactory.Object);

            // Act
            var result = await controller.FetchAndStoreWeather(new WeatherRequest
            {
                Latitude = 1,
                Longitude = 2,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow
            });

            // Assert
            Assert.IsInstanceOfType(result, typeof(ObjectResult));
            var objectResult = result as ObjectResult;
            Assert.AreEqual(400, objectResult.StatusCode);
        }
    }
}