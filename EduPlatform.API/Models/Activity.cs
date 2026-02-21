// Models/Activity.cs
namespace EduPlatform.API.Models
{
    public class Activity
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Action { get; set; }
        public string Details { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } // "success", "error", "pending"

        // Navigation Property
        public ApplicationUser User { get; set; }
    }
}
