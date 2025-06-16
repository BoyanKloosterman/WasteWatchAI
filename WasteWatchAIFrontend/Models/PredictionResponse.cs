namespace WasteWatchAIFrontend.Models
{
    public class PredictionResponse
    {
        public Dictionary<string, int> Predictions { get; set; } = new();
        public Dictionary<string, float> Confidence_Scores { get; set; } = new();
        public Dictionary<string, string> Model_Used_Per_Category { get; set; } = new();
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public string Date { get; set; } = default!;  // ISO-formaat "yyyy-MM-dd"
        public float Temperature { get; set; }
        public string weather_description { get; set; }
    }
}
