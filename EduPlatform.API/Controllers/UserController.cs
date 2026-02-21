// Controllers/UserController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduPlatform.API.Data;
using EduPlatform.API.Models;
using EduPlatform.API.DTOs;
using System.Security.Claims;

namespace EduPlatform.API.Controllers
{
    [Route("api/users")]
    [ApiController]
    [Authorize] // جميع المسارات تتطلب مصادقة
    public class UserController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserController> _logger;

        public UserController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            ILogger<UserController> logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        // GET: api/users/profile
        [HttpGet("profile")]
        public async Task<ActionResult<UserProfileDto>> GetUserProfile()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "غير مصرح" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { Message = "المستخدم غير موجود" });
                }

                var roles = await _userManager.GetRolesAsync(user);

                var profile = new UserProfileDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    UserName = user.UserName,
                    PhoneNumber = user.PhoneNumber,
                    IsActive = user.IsActive,
                    EmailConfirmed = user.EmailConfirmed,
                    CreatedAt = user.CreatedAt,
                    LastLogin = user.LastLogin,
                    Roles = roles.ToList()
                };

                // الحصول على الكورسات المشتراة
                profile.PurchasedCourse = await GetUserPurchasedCourse(userId);
                profile.TotalSpent = await GetUserTotalSpent(userId);

                return Ok(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile");
                return StatusCode(500, new { Message = "حدث خطأ في تحميل الملف الشخصي" });
            }
        }

        // PUT: api/users/profile
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateUserProfile([FromBody] UpdateProfileDto dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "غير مصرح" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { Message = "المستخدم غير موجود" });
                }

                // تحديث البيانات
                if (!string.IsNullOrEmpty(dto.FullName))
                {
                    user.FullName = dto.FullName;
                }

                if (!string.IsNullOrEmpty(dto.PhoneNumber))
                {
                    user.PhoneNumber = dto.PhoneNumber;
                }

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(new { Errors = result.Errors });
                }

                return Ok(new { Message = "تم تحديث الملف الشخصي بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                return StatusCode(500, new { Message = "حدث خطأ في تحديث الملف الشخصي" });
            }
        }

        // POST: api/users/change-password
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "غير مصرح" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { Message = "المستخدم غير موجود" });
                }

                var result = await _userManager.ChangePasswordAsync(
                    user, dto.CurrentPassword, dto.NewPassword);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description);
                    return BadRequest(new { Errors = errors });
                }

                // تسجيل النشاط
                await LogActivity("تغيير كلمة المرور", "تم تغيير كلمة المرور بنجاح");

                return Ok(new { Message = "تم تغيير كلمة المرور بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return StatusCode(500, new { Message = "حدث خطأ في تغيير كلمة المرور" });
            }
        }

        // GET: api/users/Course
        [HttpGet("Course")]
        public async Task<ActionResult<UserCourseDto>> GetUserCourse()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "غير مصرح" });
                }

                var userCourse = new UserCourseDto
                {
                    PurchasedCourse = await GetUserPurchasedCourse(userId),
                    EnrolledCourse = await GetUserEnrolledCourse(userId),
                    Wishlist = await GetUserWishlist(userId),
                    RecentlyViewed = await GetRecentlyViewedCourse(userId)
                };

                return Ok(userCourse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user Course");
                return StatusCode(500, new { Message = "حدث خطأ في تحميل الكورسات" });
            }
        }

        // GET: api/users/purchases
        [HttpGet("purchases")]
        public async Task<ActionResult<IEnumerable<PurchaseDto>>> GetUserPurchases(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "غير مصرح" });
                }

                var purchases = await _context.Purchases
                    .Where(p => p.UserId == userId)
                    .Include(p => p.Courses)
                    .OrderByDescending(p => p.PurchaseDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new PurchaseDto
                    {
                        Id = p.Id,
                        CourseId = p.CourseId,
                        CourseTitle = p.Courses.Title,
                        CourseImage = p.Courses.ImageUrl,
                        AmountPaid = p.AmountPaid,
                        PurchaseDate = p.PurchaseDate,
                        PaymentMethod = p.PaymentMethod,
                        TransactionId = p.TransactionId,
                        Status = p.Status
                    })
                    .ToListAsync();

                var totalCount = await _context.Purchases
                    .CountAsync(p => p.UserId == userId);

                return Ok(new
                {
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    Data = purchases
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user purchases");
                return StatusCode(500, new { Message = "حدث خطأ في تحميل المشتريات" });
            }
        }

        // GET: api/users/notifications
        [HttpGet("notifications")]
        public async Task<ActionResult<IEnumerable<UserNotificationDto>>> GetUserNotifications(
            [FromQuery] bool unreadOnly = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "غير مصرح" });
                }

                var query = _context.Notifications
                    .Where(n => n.RecipientType == "user" && n.RecipientId == userId ||
                                n.RecipientType == "all");

                if (unreadOnly)
                {
                    query = query.Where(n => !n.IsRead);
                }

                var notifications = await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(n => new UserNotificationDto
                    {
                        Id = n.Id,
                        Title = n.Title,
                        Message = n.Message,
                        Type = n.Type,
                        CreatedAt = n.CreatedAt,
                        IsRead = n.IsRead,
                        ReadAt = n.ReadAt
                    })
                    .ToListAsync();

                var totalCount = await query.CountAsync();

                return Ok(new
                {
                    TotalCount = totalCount,
                    UnreadCount = await query.CountAsync(n => !n.IsRead),
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    Data = notifications
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user notifications");
                return StatusCode(500, new { Message = "حدث خطأ في تحميل الإشعارات" });
            }
        }

        // PUT: api/users/notifications/{id}/read
        [HttpPut("notifications/{id}/read")]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "غير مصرح" });
                }

                var notification = await _context.Notifications.FindAsync(id);
                if (notification == null)
                {
                    return NotFound(new { Message = "الإشعار غير موجود" });
                }

                // التحقق من أن المستخدم هو المستلم
                if (notification.RecipientType == "user" && notification.RecipientId != userId)
                {
                    return Forbid();
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

        // GET: api/users/activities
        [HttpGet("activities")]
        public async Task<ActionResult<IEnumerable<UserActivityDto>>> GetUserActivities(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "غير مصرح" });
                }

                var activities = await _context.Activities
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => new UserActivityDto
                    {
                        Id = a.Id,
                        Action = a.Action,
                        Details = a.Details,
                        CreatedAt = a.CreatedAt,
                        Status = a.Status
                    })
                    .ToListAsync();

                var totalCount = await _context.Activities
                    .CountAsync(a => a.UserId == userId);

                return Ok(new
                {
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    Data = activities
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user activities");
                return StatusCode(500, new { Message = "حدث خطأ في تحميل النشاطات" });
            }
        }

        // POST: api/users/wishlist/{CourseId}
        [HttpPost("wishlist/{CourseId}")]
        public async Task<IActionResult> AddToWishlist(int CourseId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "غير مصرح" });
                }

                // التحقق من وجود الكورس
                var Course= await _context.Courses.FindAsync(CourseId);
                if (Course== null)
                {
                    return NotFound(new { Message = "الكورس غير موجود" });
                }

                // التحقق إذا كان الكورس موجود بالفعل في المفضلة
                var existing = await _context.Wishlists
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.CourseId == CourseId);

                if (existing != null)
                {
                    return BadRequest(new { Message = "الكورس موجود بالفعل في المفضلة" });
                }

                var wishlist = new Wishlist
                {
                    UserId = userId,
                    CourseId = CourseId,
                    AddedAt = DateTime.UtcNow
                };

                _context.Wishlists.Add(wishlist);
                await _context.SaveChangesAsync();

                // تسجيل النشاط
                await LogActivity("إضافة إلى المفضلة", $"تم إضافة كورس: {Course.Title} إلى المفضلة");

                return Ok(new { Message = "تمت الإضافة إلى المفضلة بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to wishlist");
                return StatusCode(500, new { Message = "حدث خطأ في الإضافة إلى المفضلة" });
            }
        }

        // DELETE: api/users/wishlist/{CourseId}
        [HttpDelete("wishlist/{CourseId}")]
        public async Task<IActionResult> RemoveFromWishlist(int CourseId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "غير مصرح" });
                }

                var wishlist = await _context.Wishlists
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.CourseId == CourseId);

                if (wishlist == null)
                {
                    return NotFound(new { Message = "الكورس غير موجود في المفضلة" });
                }

                _context.Wishlists.Remove(wishlist);
                await _context.SaveChangesAsync();

                // تسجيل النشاط
                await LogActivity("حذف من المفضلة", $"تم حذف كورس من المفضلة");

                return Ok(new { Message = "تم الحذف من المفضلة بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing from wishlist");
                return StatusCode(500, new { Message = "حدث خطأ في الحذف من المفضلة" });
            }
        }

        // وظائف مساعدة
        private async Task<List<CourseDto>> GetUserPurchasedCourse(string userId)
        {
            return await _context.Purchases
                .Where(p => p.UserId == userId && p.Status == "completed")
                .Include(p => p.Courses)
                .Select(p => new CourseDto
                {
                    Id = p.Courses.Id,
                    Title = p.Courses.Title,
                    Description = p.Courses.Description,
                    ImageUrl = p.Courses.ImageUrl,
                    University = p.Courses.University,
                    Price = p.Courses.Price,
                    PurchaseDate = p.PurchaseDate
                })
                .ToListAsync();
        }

        private async Task<List<CourseDto>> GetUserEnrolledCourse(string userId)
        {
            // يمكن تعديل هذه الدالة حسب منطق التسجيل في الكورسات
            return await _context.Courses
                .Where(c => c.Enrollments.Any(e => e.UserId == userId))
                .Select(c => new CourseDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    ImageUrl = c.ImageUrl,
                    University = c.University,
                    Price = c.Price,
                    EnrollmentDate = c.Enrollments
                        .Where(e => e.UserId == userId)
                        .Max(e => e.EnrollmentDate)
                })
                .ToListAsync();
        }

        private async Task<List<CourseDto>> GetUserWishlist(string userId)
        {
            return await _context.Wishlists
                .Where(w => w.UserId == userId)
                .Include(w => w.Courses)
                .Select(w => new CourseDto
                {
                    Id = w.Courses.Id,
                    Title = w.Courses.Title,
                    Description = w.Courses.Description,
                    ImageUrl = w.Courses.ImageUrl,
                    University = w.Courses.University,
                    Price = w.Courses.Price ,
                    AddedToWishlistAt = w.AddedAt
                })
                .ToListAsync();
        }

        private async Task<List<CourseDto>> GetRecentlyViewedCourse(string userId)
        {
            return await _context.UserActivities
                .Where(a => a.UserId == userId && a.Action == "ViewCourse")
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .Include(a => a.Courses)
                .Select(a => new CourseDto
                {
                    Id = a.Courses.Id,
                    Title = a.Courses.Title,
                    Description = a.Courses.Description,
                    ImageUrl = a.Courses.ImageUrl,
                    University = a.Courses.University,
                    Price = a.Courses.Price,
                    ViewedAt = a.CreatedAt
                })
                .ToListAsync();
        }

        private async Task<decimal> GetUserTotalSpent(string userId)
        {
            return await _context.Purchases
                .Where(p => p.UserId == userId && p.Status == "completed")
                .SumAsync(p => p.AmountPaid);
        }

        private async Task LogActivity(string action, string details)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;

            var activity = new Activity
            {
                UserId = userId,
                UserName = userName,
                Action = action,
                Details = details,
                CreatedAt = DateTime.UtcNow,
                Status = "success"
            };

            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();
        }
    }

    // نماذج البيانات DTOs
    public class UserProfileDto
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public bool EmailConfirmed { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public List<string> Roles { get; set; }
        public List<CourseDto> PurchasedCourse { get; set; }
        public decimal TotalSpent { get; set; }
    }

    public class UpdateProfileDto
    {
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }

    public class UserCourseDto
    {
        public List<CourseDto> PurchasedCourse { get; set; }
        public List<CourseDto> EnrolledCourse { get; set; }
        public List<CourseDto> Wishlist { get; set; }
        public List<CourseDto> RecentlyViewed { get; set; }
    }

    public class CourseDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string University { get; set; }
        public decimal Price { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public DateTime? EnrollmentDate { get; set; }
        public DateTime? AddedToWishlistAt { get; set; }
        public DateTime? ViewedAt { get; set; }
    }

    public class PurchaseDto
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }
        public string CourseImage { get; set; }
        public decimal AmountPaid { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string PaymentMethod { get; set; }
        public string TransactionId { get; set; }
        public string Status { get; set; }
    }

    public class UserNotificationDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
    }

    public class UserActivityDto
    {
        public int Id { get; set; }
        public string Action { get; set; }
        public string Details { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }
    }
}