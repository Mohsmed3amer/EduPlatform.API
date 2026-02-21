// Models/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace EduPlatform.API.Models
{
    public class ApplicationUser : IdentityUser
    {
        // خصائص المستخدم الأساسية (تأكد من عدم تكرارها)
        public string FullName { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }

        // العلاقات - تأكد من عدم تكرارها
        public ICollection<Activity> Activities { get; set; }
        public ICollection<Wishlist> Wishlists { get; set; }
        public ICollection<UserActivity> UserActivities { get; set; }
        public ICollection<Enrollment> Enrollments { get; set; }
        public ICollection<Purchase> Purchases { get; set; }
    }
}