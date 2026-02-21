// Models/Wishlist.cs
namespace EduPlatform.API.Models
{
    public class Wishlist
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int CourseId { get; set; }
        public DateTime AddedAt { get; set; }

        // Navigation Properties
        public ApplicationUser User { get; set; }
        public Courses Courses{ get; set; }
    }
}
