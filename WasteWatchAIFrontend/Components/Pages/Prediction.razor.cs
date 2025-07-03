using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Net.Http;
using WasteWatchAIFrontend.Models;
using WasteWatchAIFrontend.Services;

namespace WasteWatchAIFrontend.Components.Pages
{
    public partial class Prediction : ComponentBase
    {
        [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        private PredictionResponse predictionResult = new();

        private static readonly Dictionary<string, string> WasteTypeTranslations = new()
        {
            { "Plastic", "Plastic" },
            { "Paper", "Papier" },
            { "Organic", "Organisch" },
            { "Glass", "Glas" }
        };

        private DateTime ?selectedDate = DateTime.UtcNow.AddDays(1);
        private DateTime minDate = DateTime.Today.AddDays(1);      
        private DateTime maxDate = DateTime.Today.AddDays(14);

        private bool isLoading = false;
        private bool useDummyData = true;
        private bool chartsNeedUpdate = false;

        private string GetConfidenceColorClass(float confidence)
        {
            if (confidence >= 0.75f) return "bg-success";
            if (confidence >= 0.5f) return "bg-warning";
            return "bg-danger";
        }

        private async Task ToggleDataMode()
        {
            isLoading = true;
            StateHasChanged();

            await Task.Yield();

            isLoading = false;
            StateHasChanged();
        }

        private async Task ChangeDateAndPredict(int days)
        {
            if (selectedDate.HasValue)
            {
                var newDate = selectedDate.Value.AddDays(days);
                if (newDate >= minDate && newDate <= maxDate)
                {
                    selectedDate = newDate;
                    await HandlePrediction();
                }
            }
        }

        private async Task OnDateInputChangedAndPredict(ChangeEventArgs e)
        {
            if (DateTime.TryParse(e.Value?.ToString(), out var newDate))
            {
                if (newDate >= minDate && newDate <= maxDate)
                {
                    selectedDate = newDate;
                    await HandlePrediction();
                }
            }
        }

        private async Task HandlePrediction()
        {
            if (selectedDate is null) return;

            isLoading = true;
            StateHasChanged();

            try
            {
                var httpClient = HttpClientFactory.CreateClient("FastAPI");

                var payload = new PredictionRequest
                {
                    Datum = selectedDate.Value.ToString("yyyy-MM-dd"),
                    Latitude = 51.589,
                    Longitude = 4.775
                };

                HttpResponseMessage response;
                if (useDummyData)
                {
                    response = await httpClient.PostAsJsonAsync("api/prediction/predict/dummy", payload);
                }
                else
                {
                    response = await httpClient.PostAsJsonAsync("api/prediction/predict/trash", payload);
                }


                if (response.IsSuccessStatusCode)
                {
                    predictionResult = await response.Content.ReadFromJsonAsync<PredictionResponse>();
                    StateHasChanged();
                    await InitializePredictionChart();
                }
                else
                {
                    predictionResult = new PredictionResponse();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Prediction error: {ex.Message}");
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!isLoading && (firstRender || chartsNeedUpdate))
            {
                await JS.InvokeVoidAsync("eval", @"
                var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle=""tooltip""]'));
                tooltipTriggerList.map(function (tooltipTriggerEl) {
                    return new bootstrap.Tooltip(tooltipTriggerEl);
                });
            ");

                await InitializeCharts();
                chartsNeedUpdate = false;
            }
        }

        private async Task InitializeCharts()
        {
            // Check if we're showing prediction results or initializing an empty chart
            await InitializePredictionChart();
            await HandlePrediction();
        }
        
        private async Task InitializePredictionChart()
        {
            try
            {
                // Define the expected waste types with English API keys and Dutch display labels
                var wasteTypeMappings = new Dictionary<string, (string DisplayName, string Color)>
                {
                    { "Plastic", ("Plastic", "#e74c3c") },      // Red
                    { "Paper", ("Papier", "#3498db") },         // Blue 
                    { "Organic", ("Organisch", "#2ecc71") },    // Green
                    { "Glass", ("Glas", "#f39c12") }            // Orange
                };

                // Log the actual keys present in the API response
                if (predictionResult?.Predictions != null)
                {
                    Console.WriteLine($"Available prediction keys: {string.Join(", ", predictionResult.Predictions.Keys)}");
                    Console.WriteLine($"Available confidence keys: {string.Join(", ", predictionResult.Confidence_Scores.Keys)}");
                }

                // Create arrays for chart data
                var labels = new List<string>();
                var values = new List<int>();
                var backgroundColors = new List<string>();
                var borderColors = new List<string>();

                // Ensure all waste types are included with values from API or defaults
                foreach (var wasteType in wasteTypeMappings.Keys)
                {
                    // Display name from mapping
                    labels.Add(wasteTypeMappings[wasteType].DisplayName);

                    // Value from API or default to 0
                    int value = 0;
                    if (predictionResult?.Predictions != null &&
                        predictionResult.Predictions.TryGetValue(wasteType, out var apiValue))
                    {
                        value = apiValue;
                    }
                    values.Add(value);

                    // Get confidence score for opacity
                    float confidence = 0.5f; // Default 50% confidence
                    if (predictionResult?.Confidence_Scores != null &&
                        predictionResult.Confidence_Scores.TryGetValue(wasteType, out var apiConfidence))
                    {
                        confidence = apiConfidence;
                    }

                    // Calculate opacity based on confidence
                    float opacity = 0.3f + (confidence * 0.7f); // Min 30% opacity, max 100%

                    // Add colors with opacity for background, solid for border
                    borderColors.Add(wasteTypeMappings[wasteType].Color);
                    backgroundColors.Add(ConvertHexToRgba(wasteTypeMappings[wasteType].Color, opacity));
                }

                // Create chart data
                var chartData = new
                {
                    labels = labels.ToArray(),
                    datasets = new[]
                    {
                new {
                    label = predictionResult?.Date != null
                        ? $"Voorspelde afvalitems voor {predictionResult.Date}"
                        : "Voorspelde afvalitems",
                    data = values.ToArray(),
                    backgroundColor = backgroundColors.ToArray(),
                    borderColor = borderColors.ToArray(),
                    borderWidth = 1
                }
            }
                };

                // Log what we're sending to the chart
                Console.WriteLine($"Chart data labels: {string.Join(", ", labels)}");
                Console.WriteLine($"Chart data values: {string.Join(", ", values)}");

                // Initialize the chart
                await JS.InvokeVoidAsync("initializePredictionChart", chartData);

                Console.WriteLine("Chart initialization complete");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in initializePredictionChart: {ex.Message}");
            }
        }

        // Helper method to convert hex color to RGBA with opacity
        private string ConvertHexToRgba(string hex, float opacity)
        {
            if (hex.StartsWith("#"))
                hex = hex.Substring(1);

            if (hex.Length != 6)
                return $"rgba(119, 119, 119, {opacity})"; // Fallback gray color

            int r = Convert.ToInt32(hex.Substring(0, 2), 16);
            int g = Convert.ToInt32(hex.Substring(2, 2), 16);
            int b = Convert.ToInt32(hex.Substring(4, 2), 16);

            return $"rgba({r}, {g}, {b}, {opacity})";
        }
    }
}