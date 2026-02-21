// Models/CourseDiscount.cs
namespace EduPlatform.API.Models
{
    public class CourseDiscount
    {
        public int CourseId { get; set; }
        public int DiscountId { get; set; }

        // Navigation Properties
        public Courses Courses { get; set; }
        public Discount Discount { get; set; }
    }
}