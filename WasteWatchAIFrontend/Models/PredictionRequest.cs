using System.Text.Json.Serialization;

namespace WasteWatchAIFrontend.Models
{
    public class PredictionRequest
    {
        [JsonPropertyName("date")]
        public string Datum { get; set; } = default!;  // ISO-formaat "yyyy-MM-dd"

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

    }
}
