using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using WasteWatchAIBackend.Data;
using WasteWatchAIBackend.Models;

[ApiController]
[Route("api/[controller]")]
public class PredictionsController : ControllerBase
{
    private readonly WasteWatchDbContext _context;
    private readonly HttpClient _httpClient;

    public PredictionsController(WasteWatchDbContext context, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _httpClient = httpClientFactory.CreateClient();
    }

    [HttpPost]
    public async Task<IActionResult> MakePrediction([FromBody] PredictionInputDto input)
    {
        bool alreadyExists = await _context.PredictionResults
        .AnyAsync(p =>
            p.Timestamp.Date == input.Datum.Date &&
            Math.Abs(p.Temp - input.Temperatuur) < 0.01 &&
            p.Weather == input.Weersverwachting);

        if (alreadyExists)
        {
            return Conflict("Deze voorspelling bestaat al in de database.");
        }

        var payload = new
        {
            datum = input.Datum.ToString("yyyy-MM-dd"),
            temperatuur = input.Temperatuur,
            weersverwachting = input.Weersverwachting,
        };

        var fastApiUrl = "http://fastapi:8000/api/prediction/predict";
        var response = await _httpClient.PostAsJsonAsync(fastApiUrl, payload);

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode, "FastAPI error: " + await response.Content.ReadAsStringAsync());
        }

        var resultJson = await response.Content.ReadFromJsonAsync<PredictionResponseDto>();

        if (resultJson == null)
        {
            return BadRequest("Invalid response from FastAPI");
        }

        var predictionResult = new PredictionResult
        {
            Id = Guid.NewGuid(),
            Timestamp = input.Datum,
            Latitude = resultJson.Latitude,
            Longitude = resultJson.Longitude,
            Weather = input.Weersverwachting,
            Temp = input.Temperatuur,
            Predictions = resultJson.Predictions.Select(p => new CategoryPrediction
            {
                Category = p.Key,
                PredictedValue = p.Value,
                ConfidenceScore = resultJson.Confidence_Scores?.GetValueOrDefault(p.Key) ?? 0f,
                ModelUsed = resultJson.Model_Used_Per_Category?.GetValueOrDefault(p.Key) ?? "unknown"
            }).ToList()
        };

        _context.PredictionResults.Add(predictionResult);
        await _context.SaveChangesAsync();

        return Ok(predictionResult);
    }


}
