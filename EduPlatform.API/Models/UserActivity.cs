// Models/UserActivity.cs (للأنشطة الخاصة بالمستخدم)
namespace EduPlatform.API.Models
{
    public class UserActivity
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Action { get; set; } // "ViewCourse", "CompleteLesson", "TakeQuiz", etc.
        public int? CourseId { get; set; }
        public int? LessonId { get; set; }
        public string Details { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public ApplicationUser User { get; set; }
        public Courses Courses { get; set; }
        public Lesson Lesson { get; set; }
    }
}
