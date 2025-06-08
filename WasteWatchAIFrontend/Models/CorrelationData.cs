using System.Text.Json.Serialization;

namespace WasteWatchAIFrontend.Models
{
    public class CorrelationData
    {
        [JsonPropertyName("correlation_coefficient")]
        public double CorrelationCoefficient { get; set; }

        [JsonPropertyName("correlation_strength")]
        public string CorrelationStrength { get; set; } = string.Empty;

        [JsonPropertyName("sunny_weather_percentage")]
        public double SunnyWeatherPercentage { get; set; }

        [JsonPropertyName("rainy_weather_percentage")]
        public double RainyWeatherPercentage { get; set; }

        [JsonPropertyName("temperature_correlation")]
        public double TemperatureCorrelation { get; set; }

        [JsonPropertyName("insights")]
        public List<string> Insights { get; set; } = new();

        [JsonPropertyName("chart_data")]
        public ChartData ChartData { get; set; } = new();
    }
}
