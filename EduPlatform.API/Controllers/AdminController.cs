// Controllers/AdminController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using EduPlatform.API.Data;
using EduPlatform.API.Models;


namespace EduPlatform.API.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AdminController> _logger;
        private readonly BunnyTokenService _bunny;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<AdminController> logger,
           BunnyTokenService bunny)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _bunny = bunny;
        }


        // ==============================
        // رفع فيديو وربطه بالدرس
        // ==============================
        [HttpPost("{lessonId}/upload-video")]
        public async Task<IActionResult> UploadVideo(int lessonId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            // 1️⃣ رفع الفيديو على Bunny
            string videoId;
            try
            {
                videoId = await _bunny.UploadVideoAsync(file);
            }
            catch (Exception ex)
            {
                return BadRequest($"Upload failed: {ex.Message}");
            }

            // 2️⃣ تحديث الداتابيز وربط الفيديو بالدرس
            var lesson = await _context.Lessons.FindAsync(lessonId);
            if (lesson == null)
                return NotFound("Lesson not found");

            lesson.BunnyVideoId = videoId;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Video uploaded successfully",
                VideoId = videoId
            });
        }

        // GET: api/admin/stats
        [HttpGet("stats")]
        public async Task<ActionResult<AdminStats>> GetAdminStats()
        {
            try
            {
                var stats = new AdminStats
                {
                    TotalCourse = await _context.Courses.CountAsync(),
                    TotalUsers = await _context.Users.CountAsync(),
                    TotalSales = await _context.Purchases.CountAsync(),
                    TotalRevenue = await _context.Purchases.SumAsync(p => p.AmountPaid),
                    ActiveUsers = await _context.Users.CountAsync(u => u.IsActive),
                    PendingUsers = await _context.Users.CountAsync(u => !u.EmailConfirmed),
                    NewUsersMonth = await _context.Users
                        .CountAsync(u => u.CreatedAt >= DateTime.UtcNow.AddMonths(-1)),
                    AvgRating = await _context.Courses
                        .Where(c => c.Rating > 0)
                        .AverageAsync(c => c.Rating) ?? 0
                };

                // حساب نمو المبيعات الشهري
                var lastMonthSales = await _context.Purchases
                    .CountAsync(p => p.PurchaseDate >= DateTime.UtcNow.AddMonths(-2)
                                  && p.PurchaseDate < DateTime.UtcNow.AddMonths(-1));

                var thisMonthSales = await _context.Purchases
                    .CountAsync(p => p.PurchaseDate >= DateTime.UtcNow.AddMonths(-1));

                stats.MonthlyGrowth = lastMonthSales > 0
                    ? (int)((thisMonthSales - lastMonthSales) / (double)lastMonthSales * 100)
                    : 100;

                // حساب معدل التحويل (الزيارات إلى مبيعات)
                var totalVisitors = await _context.Activities
                    .CountAsync(a => a.Action.Contains("Visit"));
                stats.ConversionRate = totalVisitors > 0
                    ? Math.Round((stats.TotalSales / (decimal)totalVisitors) * 100, 2)
                    : 0;

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin stats");
                return StatusCode(500, new { Message = "حدث خطأ في تحميل الإحصائيات" });
            }
        }

        // GET: api/admin/activities
        [HttpGet("activities")]
        public async Task<ActionResult<IEnumerable<ActivityDto>>> GetActivities()
        {
            try
            {
                var activities = await _context.Activities
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(50)
                    .Select(a => new ActivityDto
                    {
                        Date = a.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                        Action = a.Action,
                        User = a.UserName,
                        Status = a.Status
                    })
                    .ToListAsync();

                return Ok(activities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting activities");
                return StatusCode(500, new { Message = "حدث خطأ في تحميل النشاطات" });
            }
        }

        // POST: api/admin/activities/log
        [HttpPost("activities/log")]
        public async Task<IActionResult> LogActivity([FromBody] ActivityLogDto logDto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = User.FindFirst(ClaimTypes.Name)?.Value;

                var activity = new Activity
                {
                    UserId = userId,
                    UserName = userName ?? "System",
                    Action = logDto.Action,
                    Details = logDto.Details,
                    CreatedAt = DateTime.UtcNow,
                    Status = "success"
                };

                _context.Activities.Add(activity);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "تم تسجيل النشاط بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging activity");
                return StatusCode(500, new { Message = "حدث خطأ في تسجيل النشاط" });
            }
        }

        // GET: api/admin/users
        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<UserListDto>>> GetUsers()
        {
            try
            {
                var users = await _context.Users
                    .Select(u => new UserListDto
                    {
                        Id = u.Id,
                        FullName = u.FullName,
                        UserName = u.UserName,
                        Email = u.Email,
                        IsActive = u.IsActive,
                        EmailConfirmed = u.EmailConfirmed,
                        CreatedAt = u.CreatedAt,
                        Role = _context.UserRoles
                            .Where(ur => ur.UserId == u.Id)
                            .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                            .FirstOrDefault() ?? "User"
                    })
                    .OrderByDescending(u => u.CreatedAt)
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users list");
                return StatusCode(500, new { Message = "حدث خطأ في تحميل قائمة المستخدمين" });
            }
        }

        // GET: api/admin/users/summary (يبقى كما هو للإحصائيات فقط)
        [HttpGet("users/summary")]
        public async Task<ActionResult<UserSummaryDto>> GetUsersSummary()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var weekStart = today.AddDays(-6);
                var monthStart = today.AddMonths(-1);

                var summary = new UserSummaryDto
                {
                    TotalUsers = await _context.Users.CountAsync(),
                    ActiveUsers = await _context.Users.CountAsync(u => u.IsActive && u.EmailConfirmed),
                    NewUsersToday = await _context.Users.CountAsync(u => u.CreatedAt >= today),
                    NewUsersWeek = await _context.Users.CountAsync(u => u.CreatedAt >= weekStart),
                    NewUsersMonth = await _context.Users.CountAsync(u => u.CreatedAt >= monthStart),
                    PendingVerification = await _context.Users.CountAsync(u => !u.EmailConfirmed)
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users summary");
                return StatusCode(500, new { Message = "حدث خطأ في تحميل ملخص المستخدمين" });
            }
        }

        // GET: api/admin/Course/summary
        [HttpGet("Courses/summary")]
        public async Task<ActionResult<CourseummaryDto>> GetCourseSummary()
        {
            try
            {
                var summary = new CourseummaryDto
                {
                    TotalCourse = await _context.Courses.CountAsync(),
                    ActiveCourse = await _context.Courses.CountAsync(c => c.IsActive),
                    FreeCourse = await _context.Courses.CountAsync(c => c.Price == 0),
                    PaidCourse = await _context.Courses.CountAsync(c => c.Price > 0),
                    AveragePrice = await _context.Courses
                        .Where(c => c.Price > 0)
                        .AverageAsync(c => c.Price),
                    TopRatedCourse = await _context.Courses
                        .Where(c => c.Rating > 0)
                        .OrderByDescending(c => c.Rating)
                        .Take(5)
                        .Select(c => new CourseInfoDto
                        {
                            Id = c.Id,
                            Title = c.Title,
                            University = c.University,
                            Rating = c.Rating ?? 0,
                            Price = c.Price,
                            EnrollmentCount = c.EnrollmentCount
                        })
                        .ToListAsync()
                };

                // توزيع الكورسات حسب الجامعة
                summary.UniversityDistribution = await _context.Courses
                    .GroupBy(c => c.University)
                    .Select(g => new
                    {
                        University = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(g => g.Count)
                    .Take(10)
                    .ToDictionaryAsync(g => g.University, g => g.Count);

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Course summary");
                return StatusCode(500, new { Message = "حدث خطأ في تحميل ملخص الكورسات" });
            }
        }

        // GET: api/admin/sales/summary
        [HttpGet("sales/summary")]
        public async Task<ActionResult<IEnumerable<SalesSummaryDto>>> GetSalesSummary([FromQuery] string period = "month")
        {
            try
            {
                var now = DateTime.UtcNow;
                DateTime startDate;

                switch (period.ToLower())
                {
                    case "day":
                        startDate = now.Date;
                        break;
                    case "week":
                        startDate = now.AddDays(-6).Date;
                        break;
                    case "month":
                        startDate = now.AddMonths(-1);
                        break;
                    case "year":
                        startDate = now.AddYears(-1);
                        break;
                    default:
                        startDate = now.AddMonths(-1);
                        break;
                }

                var sales = await _context.Purchases
                    .Where(p => p.PurchaseDate >= startDate && p.PurchaseDate <= now)
                    .ToListAsync();

                var summary = new SalesSummaryDto
                {
                    Period = period,
                    TotalSales = sales.Count,
                    TotalRevenue = sales.Sum(p => p.AmountPaid),
                    AverageOrderValue = sales.Any() ? sales.Average(p => p.AmountPaid) : 0,
                    TopSellingCourse = await GetTopSellingCourse(startDate, now)
                };

                // بيانات المبيعات اليومية للأسبوع الأخير
                if (period == "week")
                {
                    summary.DailySales = await GetDailySales(now.AddDays(-6), now);
                }
                // بيانات المبيعات الشهرية للسنة الأخيرة
                else if (period == "year")
                {
                    summary.MonthlySales = await GetMonthlySales(now.AddYears(-1), now);
                }

                // إرجاع مصفوفة تحتوي على كائن واحد
                return Ok(new List<SalesSummaryDto> { summary });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales summary");
                return StatusCode(500, new { Message = "حدث خطأ في تحميل ملخص المبيعات" });
            }
        }

        
        // POST: api/admin/notifications
        [HttpPost("notifications")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SendNotification([FromBody] NotificationDto notificationDto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = User.FindFirst(ClaimTypes.Name)?.Value;

                // تسجيل الإشعار في قاعدة البيانات
                var notification = new EduPlatform.API.Models.Notification
                {
                    Title = notificationDto.Title,
                    Message = notificationDto.Message,
                    Body = notificationDto.Body ?? notificationDto.Message, // للتأكد من وجود Body
                    Type = notificationDto.Type ?? "info",
                    Target = notificationDto.Target ?? notificationDto.RecipientType ?? "all",
                    RecipientType = notificationDto.RecipientType,
                    RecipientId = notificationDto.RecipientId,
                    CreatedBy = userName ?? userId,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // تسجيل النشاط
                await LogActivity(new ActivityLogDto
                {
                    Action = "إرسال إشعار",
                    Details = $"تم إرسال إشعار: {notificationDto.Title} إلى {notificationDto.Target}"
                });

                return Ok(new
                {
                    Message = "تم إرسال الإشعار بنجاح",
                    NotificationId = notification.Id,
                    Notification = notification
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification");
                return StatusCode(500, new { Message = "حدث خطأ في إرسال الإشعار" });
            }
        }

        // GET: api/admin/notifications
        [HttpGet("notifications")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetNotifications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var notifications = await _context.Notifications
                    .OrderByDescending(n => n.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(n => new NotificationDto
                    {
                        Id = n.Id,
                        Title = n.Title,
                        Body = n.Body ?? n.Message,
                        Message = n.Message,
                        Target = n.Target ?? n.RecipientType ?? "الكل",
                        RecipientType = n.RecipientType,
                        CreatedAt = n.CreatedAt,
                        IsRead = n.IsRead,
                        Type = n.Type
                    })
                    .ToListAsync();

                // إرجاع المصفوفة مباشرة (بدون Pagination)
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications");
                return StatusCode(500, new { Message = "حدث خطأ في تحميل الإشعارات" });
            }
        }

        // PUT: api/admin/notifications/{id}/read
        [HttpPut("notifications/{id}/read")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(id);
                if (notification == null)
                {
                    return NotFound(new { Message = "الإشعار غير موجود" });
                }

                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { Message = "تم تحديد الإشعار كمقروء" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                return StatusCode(500, new { Message = "حدث خطأ في تحديث حالة الإشعار" });
            }
        }

        // وظائف مساعدة
        private async Task<Dictionary<string, int>> GetMonthlyUserDistribution()
        {
            var distribution = new Dictionary<string, int>();
            var now = DateTime.UtcNow;

            for (int i = 11; i >= 0; i--)
            {
                var month = now.AddMonths(-i);
                var monthStart = new DateTime(month.Year, month.Month, 1);
                var monthEnd = monthStart.AddMonths(1);

                var count = await _context.Users
                    .CountAsync(u => u.CreatedAt >= monthStart && u.CreatedAt < monthEnd);

                var monthName = month.ToString("MMM", new System.Globalization.CultureInfo("ar-SA"));
                distribution.Add(monthName, count);
            }

            return distribution;
        }

        private async Task<List<TopSellingCourseDto>> GetTopSellingCourse(DateTime startDate, DateTime endDate)
        {
            return await _context.Purchases
                .Where(p => p.PurchaseDate >= startDate && p.PurchaseDate <= endDate)
                .GroupBy(p => p.CourseId)
                .Select(g => new TopSellingCourseDto
                {
                    CourseId = g.Key,
                    CourseTitle = g.First().Courses.Title,
                    SalesCount = g.Count(),
                    Revenue = g.Sum(p => p.AmountPaid)
                })
                .OrderByDescending(c => c.SalesCount)
                .Take(10)
                .ToListAsync();
        }

        private async Task<Dictionary<string, decimal>> GetDailySales(DateTime startDate, DateTime endDate)
        {
            var dailySales = new Dictionary<string, decimal>();

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var nextDay = date.AddDays(1);
                var sales = await _context.Purchases
                    .Where(p => p.PurchaseDate >= date && p.PurchaseDate < nextDay)
                    .SumAsync(p => p.AmountPaid);

                var dayName = date.ToString("ddd", new System.Globalization.CultureInfo("ar-SA"));
                dailySales.Add(dayName, sales);
            }

            return dailySales;
        }

        private async Task<Dictionary<string, decimal>> GetMonthlySales(DateTime startDate, DateTime endDate)
        {
            var monthlySales = new Dictionary<string, decimal>();

            for (var date = startDate; date <= endDate; date = date.AddMonths(1))
            {
                var nextMonth = date.AddMonths(1);
                var sales = await _context.Purchases
                    .Where(p => p.PurchaseDate >= date && p.PurchaseDate < nextMonth)
                    .SumAsync(p => p.AmountPaid);

                var monthName = date.ToString("MMM", new System.Globalization.CultureInfo("ar-SA"));
                monthlySales.Add(monthName, sales);
            }

            return monthlySales;
        }


        
    }
    // نماذج البيانات
    public class AdminStats
    {
        public int TotalCourse { get; set; }
        public int TotalUsers { get; set; }
        public int TotalSales { get; set; }
        public decimal TotalRevenue { get; set; }
        public int ActiveUsers { get; set; }
        public int PendingUsers { get; set; }
        public int MonthlyGrowth { get; set; }
        public decimal AvgRating { get; set; }
        public int NewUsersMonth { get; set; }
        public decimal ConversionRate { get; set; }
    }

    public class ActivityDto
    {
        public string Date { get; set; }
        public string Action { get; set; }
        public string User { get; set; }
        public string Status { get; set; }
    }

    public class ActivityLogDto
    {
        public string Action { get; set; }
        public string Details { get; set; }
    }

    public class UserSummaryDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int NewUsersToday { get; set; }
        public int NewUsersWeek { get; set; }
        public int NewUsersMonth { get; set; }
        public int PendingVerification { get; set; }
        public Dictionary<string, int> MonthlyDistribution { get; set; }
    }

    public class CourseummaryDto
    {
        public int TotalCourse { get; set; }
        public int ActiveCourse { get; set; }
        public int FreeCourse { get; set; }
        public int PaidCourse { get; set; }
        public decimal AveragePrice { get; set; }
        public List<CourseInfoDto> TopRatedCourse { get; set; }
        public Dictionary<string, int> UniversityDistribution { get; set; }
    }

    public class CourseInfoDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string University { get; set; }
        public decimal Rating { get; set; }
        public decimal Price { get; set; }
        public int EnrollmentCount { get; set; }
    }

    public class SalesSummaryDto
    {
        public string Period { get; set; }
        public int TotalSales { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }
        public List<TopSellingCourseDto> TopSellingCourse { get; set; }
        public Dictionary<string, decimal> DailySales { get; set; }
        public Dictionary<string, decimal> MonthlySales { get; set; }
    }

    public class TopSellingCourseDto
    {
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }
        public int SalesCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class NotificationDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Body { get; set; } // الاسم المستخدم في Frontend
        public string Type { get; set; }
        public string Target { get; set; } // المستهدفين (الكل، طلاب، مسؤولين)
        public string RecipientType { get; set; }
        public string RecipientId { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
    }

    public class Notification
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Body { get; set; } // إضافة هذا الحقل
        public string Type { get; set; }
        public string Target { get; set; } // إضافة هذا الحقل
        public string RecipientType { get; set; }
        public string RecipientId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
    }
    public class UserListDto
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
        public bool EmailConfirmed { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Role { get; set; }
    }
}