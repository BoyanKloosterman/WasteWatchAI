namespace WasteWatchAIFrontend.Models
{
    public class TrashItem
    {
        public Guid Id { get; set; }
        public string LitterType { get; set; } = string.Empty;
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
