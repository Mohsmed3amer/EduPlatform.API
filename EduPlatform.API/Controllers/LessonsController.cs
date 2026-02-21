using EduPlatform.API.Data;
using EduPlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

//[Authorize]
[ApiController]
[Route("api/lessons")]
public class LessonsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly BunnyTokenService _bunny;

    public LessonsController(ApplicationDbContext context, BunnyTokenService bunny)
    {
        _context = context;
        _bunny = bunny;
    }

    // ✅ GET: api/lessons - عرض جميع الدروس (للمسؤول فقط)
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAllLessons()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var lessons = await _context.Lessons
                .Include(l => l.Courses)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new
                {
                    l.Id,
                    l.Title,
                    l.Description,
                    l.Duration,
                    l.Order,
                    l.BunnyVideoId,
                    l.CreatedAt,
                    CourseId = l.CourseId,
                    CourseTitle = l.Courses.Title,
                    VideoUrl = !string.IsNullOrEmpty(l.BunnyVideoId)
                        ? _bunny.GenerateVideoUrl(l.BunnyVideoId)
                        : null
                })
                .ToListAsync();

            return Ok(lessons);
        }
        catch (Exception)
        {
            return StatusCode(500, "حدث خطأ في تحميل الدروس");
        }
    }

    // ✅ GET: api/lessons/course/{courseId} - عرض دروس كورس معين
    [HttpGet("course/{courseId}")]
    public async Task<IActionResult> GetCourseLessons(int courseId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var lessons = await _context.Lessons
                .Where(l => l.CourseId == courseId)
                .OrderBy(l => l.Order)
                .Select(l => new
                {
                    l.Id,
                    l.Title,
                    l.Description,
                    l.Duration,
                    l.Order,
                    l.CreatedAt,
                    VideoUrl = !string.IsNullOrEmpty(l.BunnyVideoId)
                        ? _bunny.GenerateVideoUrl(l.BunnyVideoId)
                        : null
                })
                .ToListAsync();

            return Ok(lessons);
        }
        catch (Exception)
        {
            return StatusCode(500, "حدث خطأ في تحميل دروس الكورس");
        }
    }

    // ✅ GET: api/lessons/{lessonId} - عرض درس محدد (موجود بالفعل)
    [HttpGet("{lessonId}")]
    public async Task<IActionResult> WatchLesson(int lessonId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var lesson = await _context.Lessons
            .AsNoTracking()
            .Include(l => l.Courses)
            .FirstOrDefaultAsync(l => l.Id == lessonId);

        if (lesson == null)
            return NotFound("الدرس غير موجود");

        bool isAdmin = User.IsInRole("Admin");
        bool hasAccess = isAdmin || await _context.Purchases
    .AnyAsync(p => p.UserId == userId && p.CourseId == lesson.CourseId);

        if (!hasAccess)
            return Forbid();

        if (string.IsNullOrEmpty(lesson.BunnyVideoId))
            return BadRequest("الفيديو غير متاح");

        var secureUrl = _bunny.GenerateVideoUrl(lesson.BunnyVideoId);

        return Ok(new
        {
            lesson.Id,
            lesson.Title,
            lesson.Description,
            lesson.Duration,
            lesson.Order,
            lesson.CourseId,
            CourseTitle = lesson.Courses?.Title,
            VideoUrl = secureUrl,
            lesson.CreatedAt
        });
    }

    // ✅ POST: api/lessons - إضافة درس جديد (للمسؤول فقط)
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateLesson([FromForm] CreateLessonDto dto)
    {
        try
        {
            // التحقق من وجود الكورس
            var course = await _context.Courses.FindAsync(dto.CourseId);
            if (course == null)
                return NotFound("الكورس غير موجود");

            // التحقق من صحة البيانات
            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest("عنوان الدرس مطلوب");

            // تحديد الترتيب التلقائي
            int order = dto.Order ?? await _context.Lessons
                .Where(l => l.CourseId == dto.CourseId)
                .CountAsync() + 1;

            // إنشاء الدرس
            var lesson = new Lesson
            {
                Title = dto.Title,
                Description = dto.Description,
                CourseId = dto.CourseId,
                Order = order,
                Duration = dto.Duration,
                CreatedAt = DateTime.UtcNow
            };

            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "تم إضافة الدرس بنجاح",
                LessonId = lesson.Id,
                Title = lesson.Title,
                Order = lesson.Order
            });
        }
        catch (Exception)
        {
            return StatusCode(500, "حدث خطأ في إضافة الدرس");
        }
    }

    // ✅ PUT: api/lessons/{lessonId} - تحديث درس (للمسؤول فقط)
    [Authorize(Roles = "Admin")]
    [HttpPut("{lessonId}")]
    public async Task<IActionResult> UpdateLesson(int lessonId, [FromBody] UpdateLessonDto dto)
    {
        try
        {
            var lesson = await _context.Lessons.FindAsync(lessonId);
            if (lesson == null)
                return NotFound("الدرس غير موجود");

            // تحديث البيانات
            if (!string.IsNullOrWhiteSpace(dto.Title))
                lesson.Title = dto.Title;

            if (dto.Description != null)
                lesson.Description = dto.Description;

            if (!string.IsNullOrWhiteSpace(dto.Duration))
                lesson.Duration = dto.Duration;

            if (dto.Order.HasValue)
                lesson.Order = dto.Order.Value;

            lesson.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "تم تحديث الدرس بنجاح",
                LessonId = lesson.Id,
                Title = lesson.Title
            });
        }
        catch (Exception)
        {
            return StatusCode(500, "حدث خطأ في تحديث الدرس");
        }
    }

    // ✅ DELETE: api/lessons/{lessonId} - حذف درس (للمسؤول فقط)
    [Authorize(Roles = "Admin")]
    [HttpDelete("{lessonId}")]
    public async Task<IActionResult> DeleteLesson(int lessonId)
    {
        try
        {
            var lesson = await _context.Lessons.FindAsync(lessonId);
            if (lesson == null)
                return NotFound("الدرس غير موجود");

            _context.Lessons.Remove(lesson);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "تم حذف الدرس بنجاح" });
        }
        catch (Exception)
        {
            return StatusCode(500, "حدث خطأ في حذف الدرس");
        }
    }

    //// ✅ POST: api/lessons/{lessonId}/upload-video - رفع فيديو لدرس (للمسؤول فقط)
    //[Authorize(Roles = "Admin")]
    //[HttpPost("{lessonId}/upload-video")]
    //public async Task<IActionResult> UploadLessonVideo(int lessonId, IFormFile video)
    //{
    //    try
    //    {
    //        var lesson = await _context.Lessons.FindAsync(lessonId);
    //        if (lesson == null)
    //            return NotFound("الدرس غير موجود");

    //        if (video == null || video.Length == 0)
    //            return BadRequest("الرجاء اختيار ملف فيديو");

    //        // التحقق من نوع الملف
    //        var allowedTypes = new[] { "video/mp4", "video/webm", "video/ogg", "video/quicktime" };
    //        if (!allowedTypes.Contains(video.ContentType.ToLower()))
    //            return BadRequest("نوع الملف غير مدعوم. الأنواع المدعومة: MP4, WebM, Ogg");

    //        // التحقق من الحجم (500MB max)
    //        if (video.Length > 500 * 1024 * 1024)
    //            return BadRequest("حجم الملف كبير جداً. الحد الأقصى 500MB");

    //        // هنا يتم رفع الفيديو إلى Bunny (يتم تنفيذه في الخدمة)
    //        // هذا مجرد مثال - يجب تنفيذ الدالة في BunnyTokenService
    //        var videoId = Guid.NewGuid().ToString(); // مؤقت

    //        lesson.BunnyVideoId = videoId;
    //        lesson.UpdatedAt = DateTime.UtcNow;

    //        await _context.SaveChangesAsync();

    //        return Ok(new
    //        {
    //            Message = "تم رفع الفيديو بنجاح",
    //            VideoId = videoId,
    //            VideoUrl = _bunny.GenerateVideoUrl(videoId)
    //        });
    //    }
    //    catch (Exception)
    //    {
    //        return StatusCode(500, "حدث خطأ في رفع الفيديو");
    //    }
    //}

    // ✅ POST: api/lessons/reorder - إعادة ترتيب الدروس (للمسؤول فقط)
    [Authorize(Roles = "Admin")]
    [HttpPost("reorder")]
    public async Task<IActionResult> ReorderLessons([FromBody] List<LessonOrderDto> orders)
    {
        try
        {
            foreach (var item in orders)
            {
                var lesson = await _context.Lessons.FindAsync(item.LessonId);
                if (lesson != null)
                {
                    lesson.Order = item.Order;
                    lesson.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = "تم إعادة ترتيب الدروس بنجاح" });
        }
        catch (Exception)
        {
            return StatusCode(500, "حدث خطأ في إعادة ترتيب الدروس");
        }
    }
}

// ========== DTOs ==========

public class CreateLessonDto
{
    public string Title { get; set; }
    public string? Description { get; set; }
    public int CourseId { get; set; }
    public int? Order { get; set; }
    public string? Duration { get; set; }
}

public class UpdateLessonDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Duration { get; set; }
    public int? Order { get; set; }
}

public class LessonOrderDto
{
    public int LessonId { get; set; }
    public int Order { get; set; }
}