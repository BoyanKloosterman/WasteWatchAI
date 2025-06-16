namespace WasteWatchAIBackend.Models
{
    public class PredictionInputDto
    {
        public DateTime Datum { get; set; }
        public float Temperatuur { get; set; }
        public string Weersverwachting { get; set; }
    }
}
