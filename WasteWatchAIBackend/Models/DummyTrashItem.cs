using System;
using System.ComponentModel.DataAnnotations;

namespace WasteWatchAIBackend.Models
{
    public class DummyTrashItem
    {
        [Key]
        public Guid Id { get; set; }
        public string LitterType { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
