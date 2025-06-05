namespace WasteWatchAIBackend.Models
{
    public class Weather
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public float temperatuur { get; set; }
        public string weerOmschrijving { get; set; }
    }
}
