namespace WasteWatchAIBackend.Models
{
    public class CategoryPrediction
    {
        public Guid Id { get; set; } = Guid.NewGuid(); // unieke ID
        public string Category { get; set; }
        public int PredictedValue { get; set; }
        public float ConfidenceScore { get; set; }
        public string ModelUsed { get; set; }

        // Foreign key if using EF Core
        public Guid PredictionResultId { get; set; }
        public PredictionResult PredictionResult { get; set; }
    }
}
