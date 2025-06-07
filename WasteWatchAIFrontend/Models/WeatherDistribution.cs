namespace WasteWatchAIFrontend.Models
{
    public class WeatherDistribution
    {
        [JsonPropertyName("labels")]
        public List<string> Labels { get; set; } = new();

        [JsonPropertyName("values")]
        public List<int> Values { get; set; } = new();
    }
}
