
// Models/Purchase.cs
namespace EduPlatform.API.Models
{
    public class Purchase
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int CourseId { get; set; }
        public decimal AmountPaid { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string PaymentMethod { get; set; }
        public string TransactionId { get; set; }
        public string Status { get; set; } // "completed", "pending", "failed", "refunded"

        // Navigation Properties
        public ApplicationUser User { get; set; }
        public Courses Courses { get; set; }
    }
}

