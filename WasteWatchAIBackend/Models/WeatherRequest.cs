namespace WasteWatchAIBackend.Models
{
    public class WeatherRequest
    {
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
