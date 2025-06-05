namespace WasteWatchAIBackend.Models
{
    public class WeatherData
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public float Temperature { get; set; }
        public string WeatherDescription { get; set; }
    }
}
