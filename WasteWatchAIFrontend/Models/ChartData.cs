using static WasteWatchAIFrontend.Components.Pages.Analyse;
using System.Text.Json.Serialization;

namespace WasteWatchAIFrontend.Models
{
    public class ChartData
    {
        [JsonPropertyName("temperature_data")]
        public TemperatureData TemperatureData { get; set; } = new();

        [JsonPropertyName("weather_distribution")]
        public WeatherDistribution WeatherDistribution { get; set; } = new();

        [JsonPropertyName("correlation_scatter")]
        public CorrelationScatter CorrelationScatter { get; set; } = new();
    }
}
