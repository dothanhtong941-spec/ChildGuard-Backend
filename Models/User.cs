using Microsoft.AspNetCore.Identity;

namespace nhdphuong_1787_api.Models
{
    public class User : IdentityUser
    {
        public string? FullName { get; set; }
        public string? Initials { get; set; }
        public string? Role { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual ICollection<ChildProfile> Children { get; set; } = new List<ChildProfile>();
    }
}
