namespace WasteWatchAIBackend.Models
{
    public class PredictionResult
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public string Weather { get; set; }
        public float Temp { get; set; }
        public float AvgConfidence { get; set; }

        public List<CategoryPrediction> Predictions { get; set; }
    }
}
