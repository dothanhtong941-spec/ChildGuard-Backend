using System.Text.Json.Serialization;

namespace nhdphuong_1787_api.Models
{
    public class ScreenTimeSchedule
    {
        public int Id { get; set; }
        public int ChildProfileId { get; set; }

        [JsonIgnore]
        public virtual ChildProfile? ChildProfile { get; set; }

        public string ScheduleTitle { get; set; } = "Khung giờ học bài tối";
        public string DayOfWeek { get; set; } = "Thứ 2 - Thứ 6";
        public TimeSpan AllowedStartTime { get; set; } = new TimeSpan(19, 0, 0);
        public TimeSpan AllowedEndTime { get; set; } = new TimeSpan(21, 0, 0);
        public int MaxDailyHours { get; set; } = 2;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
