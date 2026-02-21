namespace EduPlatform.API.Models
// Models/AdminUser.cs

{
    public class AdminUser
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public int PurchasedCourse { get; set; }
        public decimal TotalSpent { get; set; }
        public bool IsActive { get; set; }
        public string Status { get; set; } // "active", "inactive", "suspended"
    }

    public class UpdateUserStatusDto
    {
        public string Status { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
    }

    public class ResetPasswordDto
    {
        public string NewPassword { get; set; }
    }
}