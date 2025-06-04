using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WasteWatchAIBackend.Data;
using WasteWatchAIBackend.Interface;
using WasteWatchAIBackend.Model;

namespace WasteWatchAIBackend.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherController : ControllerBase
    {
        private readonly IWeatherRepository _repository;
        private readonly HttpClient _httpClient;

        public WeatherController(IWeatherRepository repository, IHttpClientFactory httpClientFactory)
        {
            _repository = repository;
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpPost("fetch-latest")]
        public async Task<IActionResult> FetchAndStoreLatestWeather()
        {
            float latitude = 51.57f;
            float longitude = 4.76f;

            string apiUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,weather_code";

            var response = await _httpClient.GetAsync(apiUrl);
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, "Failed to fetch weather data");

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonDocument.Parse(json).RootElement;

            var current = data.GetProperty("current");

            var weather = new Weather
            {
                Timestamp = DateTime.UtcNow,
                Latitude = latitude,
                Longitude = longitude,
                temperatuur = current.GetProperty("temperature_2m").GetSingle(),
                weerOmschrijving = WeatherCodeToDescription(current.GetProperty("weather_code").GetInt32())
            };

            await _repository.SaveWeatherAsync(weather);

            return Ok(weather);
        }


        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _repository.GetAllAsync();
            return Ok(data);
        }
        private string WeatherCodeToDescription(int code)
        {
            return code switch
            {
                0 => "Helder",
                1 or 2 or 3 => "Gedeeltelijk bewolkt",
                45 or 48 => "Mist",
                51 or 53 or 55 => "Lichte motregen",
                61 or 63 or 65 => "Regen",
                71 or 73 or 75 => "Sneeuw",
                95 => "Onweer",
                _ => "Onbekend"
            };
        }

    }
}
