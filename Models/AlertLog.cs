using System.Text.Json.Serialization;

namespace nhdphuong_1787_api.Models
{
    public class AlertLog
    {
        public int Id { get; set; }
        public int ChildProfileId { get; set; }

        [JsonIgnore]
        public virtual ChildProfile? ChildProfile { get; set; }

        public string AlertType { get; set; } = "EYE_DISTANCE"; // EYE_DISTANCE, NOISE_DANGER, GEOFENCE_OUT
        public string Value { get; set; } = "18 cm";
        public string Severity { get; set; } = "Warning"; // Warning, Danger, Info
        public string Message { get; set; } = "Bé để máy quá gần mắt (<20cm)";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class StaffCatalogItem
    {
        public int Id { get; set; }
        public string AppName { get; set; } = string.Empty;
        public string PackageNameOrDomain { get; set; } = string.Empty;
        public string Category { get; set; } = "Game";
        public string RiskLevel { get; set; } = "High"; // Low, Medium, High
        public string Description { get; set; } = string.Empty;
        public string CreatedByStaff { get; set; } = "staff1787";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
