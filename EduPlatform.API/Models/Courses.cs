// Models/Course.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace EduPlatform.API.Models
{
    public class Courses
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الكورس مطلوب")]
        [StringLength(200)]
        public string Title { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [Required(ErrorMessage = "السعر مطلوب")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [StringLength(100)]
        public string University { get; set; }

        [StringLength(500)]
        public string ImageUrl { get; set; }

        [StringLength(50)]
        public string Page { get; set; } = "page-1";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // الخصائص الجديدة للداشبورد
        [Column(TypeName = "decimal(3,2)")]
        public decimal? Rating { get; set; }
        //public double? Rating { get; set; }
        public int EnrollmentCount { get; set; } = 0;

        // العلاقات الجديدة
        public ICollection<Wishlist> Wishlists { get; set; }
        public ICollection<UserActivity> UserActivities { get; set; }
        public ICollection<Enrollment> Enrollments { get; set; }
        public ICollection<Purchase> Purchases { get; set; }
        public ICollection<CourseDiscount> CourseDiscounts { get; set; }
        public ICollection<Lesson> Lesson { get; set; }
    }
}