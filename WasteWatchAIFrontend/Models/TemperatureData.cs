using System.Text.Json.Serialization;

namespace WasteWatchAIFrontend.Models
{
    public class TemperatureData
    {
        [JsonPropertyName("labels")]
        public List<string> Labels { get; set; } = new();

        [JsonPropertyName("temperature")]
        public List<double> Temperature { get; set; } = new();

        [JsonPropertyName("trash_count")]
        public List<int> TrashCount { get; set; } = new();
    }
}
