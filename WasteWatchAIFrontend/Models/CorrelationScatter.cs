namespace WasteWatchAIFrontend.Models
{

    public class CorrelationScatter
    {
        [JsonPropertyName("temperature")]
        public List<double> Temperature { get; set; } = new();

        [JsonPropertyName("trash_count")]
        public List<int> TrashCount { get; set; } = new();
    }
}
