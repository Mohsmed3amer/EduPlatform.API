// Models/Enrollment.cs
namespace EduPlatform.API.Models
{
    public class Enrollment
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int CourseId { get; set; }
        public DateTime EnrollmentDate { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedDate { get; set; }
        public decimal? ProgressPercentage { get; set; }

        // Navigation Properties
        public ApplicationUser User { get; set; }
        public Courses Courses { get; set; }
    }
}

