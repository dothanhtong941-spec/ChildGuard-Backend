using System.Text.Json.Serialization;

namespace nhdphuong_1787_api.Models
{
    public class AppRule
    {
        public int Id { get; set; }
        public int ChildProfileId { get; set; }

        [JsonIgnore]
        public virtual ChildProfile? ChildProfile { get; set; }

        public string AppName { get; set; } = string.Empty;
        public string PackageNameOrDomain { get; set; } = string.Empty;
        public string Category { get; set; } = "Entertainment"; // SocialMedia, Game, Entertainment, Education
        public string Status { get; set; } = "Limited"; // Blocked, Limited, Allowed
        public int DailyLimitMinutes { get; set; } = 45;
        public int UsedMinutesToday { get; set; } = 15;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
