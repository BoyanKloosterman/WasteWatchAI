namespace WasteWatchAIFrontend.Models
{
    public class LocationChartData
    {
        public string LocationName { get; set; } = string.Empty;
        public Dictionary<string, int> TypeCounts { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
