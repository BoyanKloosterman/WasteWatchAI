namespace WasteWatchAIBackend.Models
{
    public class PredictionResult
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public string Prediction { get; set; }
        public string FeatureWeather { get; set; }
        public float FeatureTemp { get; set; }
        public string FeatureLocationType { get; set; }
    }
}
