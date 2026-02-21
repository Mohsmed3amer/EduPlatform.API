// Models/AdminStats.cs
namespace EduPlatform.API.Models
{
    public class AdminStats
    {
        public int TotalCourse { get; set; }
        public int TotalUsers { get; set; }
        public int TotalSales { get; set; }
        public decimal TotalRevenue { get; set; }
        public int ActiveUsers { get; set; }
        public int PendingUsers { get; set; }
        public int MonthlyGrowth { get; set; } // نسبة مئوية
        public decimal AvgRating { get; set; }
        public int NewUsersMonth { get; set; }
        public decimal ConversionRate { get; set; }
    }

    public class SalesReport
    {
        public string Period { get; set; } // "daily", "weekly", "monthly", "yearly"
        public List<SalesData> Data { get; set; }
    }

    public class SalesData
    {
        public string Label { get; set; }
        public int SalesCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class UserReport
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int NewUsersToday { get; set; }
        public int NewUsersWeek { get; set; }
        public int NewUsersMonth { get; set; }
        public List<UserGrowthData> GrowthData { get; set; }
    }

    public class UserGrowthData
    {
        public string Date { get; set; }
        public int NewUsers { get; set; }
    }
}