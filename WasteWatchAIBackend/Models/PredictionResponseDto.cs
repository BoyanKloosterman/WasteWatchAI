namespace WasteWatchAIBackend.Models
{
    public class PredictionResponseDto
    {
        public Dictionary<string, int> Predictions { get; set; } = new();
        public Dictionary<string, float> Confidence_Scores { get; set; } = new();
        public Dictionary<string, string> Model_Used_Per_Category { get; set; } = new();
        public float Latitude { get; set; }
        public float Longitude { get; set; }
    }
}
