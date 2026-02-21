namespace EduPlatform.API.Models
// Models/Discount.cs

{
    public class Discount
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public int Percent { get; set; }
        public List<int> CourseIds { get; set; } = new List<int>();
        public List<string> CourseNames { get; set; } = new List<string>();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? MaxUses { get; set; }
        public int UsedCount { get; set; }
        public decimal? MinAmount { get; set; }
        public bool IsActive { get; set; }
        public string Status { get; set; } // "active", "expired", "upcoming", "disabled"

        // Navigation property
        public ICollection<CourseDiscount> CourseDiscounts { get; set; }
    }
}