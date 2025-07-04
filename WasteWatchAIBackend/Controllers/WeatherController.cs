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
using WasteWatchAIBackend.Models;
using Microsoft.AspNetCore.Authorization;

namespace WasteWatchAIBackend.Controller
{
    [Authorize]
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

        [HttpPost("fetch")]
        public async Task<IActionResult> FetchAndStoreWeather([FromBody] WeatherRequest request)
        {
            string startDate = request.StartDate.ToString("yyyy-MM-dd");
            string endDate = request.EndDate.ToString("yyyy-MM-dd");

            string apiUrl = $"https://api.open-meteo.com/v1/forecast?latitude={request.Latitude}&longitude={request.Longitude}&start_date={startDate}&end_date={endDate}&daily=temperature_2m_max,temperature_2m_min,weather_code&timezone=auto";

            var response = await _httpClient.GetAsync(apiUrl);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new
                {
                    message = "Failed to fetch weather data",
                    status = response.StatusCode,
                    error,
                    url = apiUrl
                });
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonDocument.Parse(json).RootElement;

            if (!data.TryGetProperty("daily", out var dailyData))
                return BadRequest("Geen dagelijkse data gevonden in de API-respons.");

            var dates = dailyData.GetProperty("time").EnumerateArray().ToList();
            var maxTemps = dailyData.GetProperty("temperature_2m_max").EnumerateArray().ToList();
            var minTemps = dailyData.GetProperty("temperature_2m_min").EnumerateArray().ToList();
            var weatherCodes = dailyData.GetProperty("weather_code").EnumerateArray().ToList();

            var weatherList = new List<WeatherData>();

for (int i = 0; i < dates.Count; i++)
{
    DateTime timestamp = DateTime.Parse(dates[i].GetString()).ToUniversalTime();

    // Check if values are null
    var maxTempElem = maxTemps[i];
    var minTempElem = minTemps[i];
    if (maxTempElem.ValueKind == JsonValueKind.Null || minTempElem.ValueKind == JsonValueKind.Null)
        continue; // Or handle as you need (e.g., set a default, skip, or log)

    float avgTemp = (maxTempElem.GetSingle() + minTempElem.GetSingle()) / 2;

    bool exists = await _repository.WeatherExistsAsync(timestamp, request.Latitude, request.Longitude);
    if (exists)
        continue;

    var weather = new WeatherData
    {
        Timestamp = timestamp,
        Latitude = request.Latitude,
        Longitude = request.Longitude,
        Temperature = avgTemp,
        WeatherDescription = WeatherCodeToDescription(weatherCodes[i].GetInt32())
    };

    weatherList.Add(weather);
}


            foreach (var w in weatherList)
            {
                await _repository.SaveWeatherAsync(w);
            }

            return Ok(weatherList);
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
