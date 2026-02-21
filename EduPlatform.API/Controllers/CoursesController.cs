// CourseController.cs
using EduPlatform.API.Models;
using EduPlatform.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EduPlatform.API.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CourseController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CourseController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: api/Course (لجميع المستخدمين)
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetCourse()
        {
            var Courses = await _context.Courses
                .Where(c => c.IsActive)
                .OrderBy(c => c.Id)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.Description,
                    c.Price,
                    c.University,
                    c.ImageUrl,
                    c.Page,
                    CreatedAt = c.CreatedAt.ToString("yyyy-MM-dd")
                })
                .ToListAsync();

            return Ok(Courses);
        }

        // GET: api/Course/{id}
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCourse(int id)
        {
            var Courses= await _context.Courses
                .Where(c => c.Id == id && c.IsActive)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.Description,
                    c.Price,
                    c.University,
                    c.ImageUrl,
                    c.Page,
                    CreatedAt = c.CreatedAt.ToString("yyyy-MM-dd")
                })
                .FirstOrDefaultAsync();

            if (Courses== null)
                return NotFound("الكورس غير موجود");

            return Ok(Courses);
        }

        // GET: api/Course/page/{pageNumber}
        [AllowAnonymous]
        [HttpGet("page/{pageNumber}")]
        public async Task<IActionResult> GetCourseByPage(string pageNumber)
        {
            var Courses = await _context.Courses
                .Where(c => c.IsActive && c.Page == $"page-{pageNumber}")
                .OrderBy(c => c.Id)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.Description,
                    c.Price,
                    c.University,
                    c.ImageUrl,
                    c.Page
                })
                .ToListAsync();

            return Ok(Courses);
        }

        // GET: api/Course/university/{universityName}
        [AllowAnonymous]
        [HttpGet("university/{universityName}")]
        public async Task<IActionResult> GetCourseByUniversity(string universityName)
        {
            var Courses = await _context.Courses
                .Where(c => c.IsActive && c.University.Contains(universityName))
                .OrderBy(c => c.Id)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.Description,
                    c.Price,
                    c.University,
                    c.ImageUrl,
                    c.Page
                })
                .ToListAsync();

            return Ok(Courses);
        }

        [HttpPost("buy/{CourseId}")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> Buy(int CourseId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("المستخدم غير مسجل الدخول");

                var course = await _context.Courses
                    .FirstOrDefaultAsync(c => c.Id == CourseId && c.IsActive);

                if (course == null)
                    return NotFound("الكورس غير موجود أو غير نشط");

                var existingPurchase = await _context.Purchases
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.CourseId == CourseId);

                if (existingPurchase != null)
                    return BadRequest("لقد قمت بشراء هذا الكورس مسبقاً");

                var purchase = new Purchase
                {
                    UserId = userId,
                    CourseId = CourseId,
                    AmountPaid = course.Price,
                    PurchaseDate = DateTime.UtcNow,
                    PaymentMethod = "manual", // مؤقت لحد ما تربط Payment Gateway
                    TransactionId = Guid.NewGuid().ToString(),
                    Status = "completed"
                };


                _context.Purchases.Add(purchase);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "تم شراء الكورس بنجاح",
                    CourseTitle = course.Title,
                    purchaseDate = purchase.PurchaseDate.ToString("yyyy-MM-dd HH:mm")
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"حدث خطأ: {ex.Message}");
            }
        }


        // GET: api/Course/search?query={searchTerm}
        [AllowAnonymous]
        [HttpGet("search")]
        public async Task<IActionResult> SearchCourse([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("يرجى إدخال كلمة للبحث");

            var Courses = await _context.Courses
                .Where(c => c.IsActive &&
                           (c.Title.Contains(query) ||
                            c.Description.Contains(query) ||
                            c.University.Contains(query)))
                .OrderBy(c => c.Id)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.Description,
                    c.Price,
                    c.University,
                    c.ImageUrl,
                    c.Page
                })
                .Take(20) // Limit results
                .ToListAsync();

            return Ok(new
            {
                query,
                results = Courses,
                count = Courses.Count
            });
        }

        // POST: api/Course - إضافة كورس جديد (للمسؤولين فقط)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCourses([FromForm] CreateCourseDto dto)
        {
            try
            {
                // التحقق من البيانات
                if (string.IsNullOrWhiteSpace(dto.Title))
                    return BadRequest("عنوان الكورس مطلوب");

                if (string.IsNullOrWhiteSpace(dto.University))
                    return BadRequest("اسم الجامعة مطلوب");

                if (dto.Price <= 0)
                    return BadRequest("السعر يجب أن يكون أكبر من صفر");

                if (dto.ImageFile == null || dto.ImageFile.Length == 0)
                    return BadRequest("صورة الكورس مطلوبة");

                // تحقق من صحة ملف الصورة
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(dto.ImageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                    return BadRequest("نوع الملف غير مسموح به. المسموح: jpg, jpeg, png, gif");

                if (dto.ImageFile.Length > 5 * 1024 * 1024) // 5MB
                    return BadRequest("حجم الصورة يجب أن يكون أقل من 5 ميجابايت");

                // حفظ الصورة
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "Course");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.ImageFile.CopyToAsync(stream);
                }

                var imageUrl = $"/uploads/Course/{fileName}";

                // إنشاء الكورس
                var Courses= new Courses
                {
                    Title = dto.Title,
                    Description = dto.Description,
                    Price = dto.Price,
                    University = dto.University,
                    ImageUrl = imageUrl,
                    Page = !string.IsNullOrWhiteSpace(dto.Page) ? dto.Page : "page-1",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.Courses.Add(Courses);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "تم إضافة الكورس بنجاح",
                    Courses= new
                    {
                        Courses.Id,
                        Courses.Title,
                        Courses.Description,
                        Courses.Price,
                        Courses.University,
                        Courses.ImageUrl,
                        Courses.Page,
                        Courses.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"حدث خطأ: {ex.Message}");
            }
        }

        // PUT: api/Course/{id} - تحديث كورس (للمسؤولين فقط)
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCourse(int id, [FromBody] UpdateCourseDto dto)
        {
            try
            {
                // التحقق من البيانات
                if (string.IsNullOrWhiteSpace(dto.Title))
                    return BadRequest("عنوان الكورس مطلوب");

                if (string.IsNullOrWhiteSpace(dto.University))
                    return BadRequest("اسم الجامعة مطلوب");

                if (dto.Price <= 0)
                    return BadRequest("السعر يجب أن يكون أكبر من صفر");

                // البحث عن الكورس
                var Course= await _context.Courses.FindAsync(id);
                if (Course== null)
                    return NotFound("الكورس غير موجود");

                // تحديث البيانات
                Course.Title = dto.Title;
                Course.Description = dto.Description;
                Course.Price = dto.Price;
                Course.University = dto.University;
                Course.UpdatedAt = DateTime.Now;

                if (!string.IsNullOrWhiteSpace(dto.Page))
                    Course.Page = dto.Page;

                _context.Courses.Update(Course);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "تم تحديث الكورس بنجاح",
                    Course= new
                    {
                        Course.Id,
                        Course.Title,
                        Course.Description,
                        Course.Price,
                        Course.University,
                        Course.ImageUrl,
                        Course.Page
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"حدث خطأ: {ex.Message}");
            }
        }

        // DELETE: api/Course/{id} - حذف كورس (للمسؤولين فقط)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            try
            {
                // البحث عن الكورس
                var Course= await _context.Courses.FindAsync(id);
                if (Course== null)
                    return NotFound("الكورس غير موجود");

                // حذف الكورس (أو تعطيله)
                // إذا كنت تريد حذف فعلي:
                // _context.Course.Remove(Course);

                // إذا كنت تريد تعطيل الكورس فقط:
                Course.IsActive = false;
                Course.UpdatedAt = DateTime.Now;

                _context.Courses.Update(Course);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "تم حذف الكورس بنجاح",
                    CourseTitle = Course.Title
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"حدث خطأ: {ex.Message}");
            }
        }

        // PUT: api/Course/{id}/image - تحديث صورة الكورس (للمسؤولين فقط)
        [HttpPut("{id}/image")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCourseImage(int id, [FromForm] UpdateCourseImageDto dto)
        {
            try
            {
                if (dto.ImageFile == null || dto.ImageFile.Length == 0)
                    return BadRequest("صورة الكورس مطلوبة");

                // البحث عن الكورس
                var Courses= await _context.Courses.FindAsync(id);
                if (Courses== null)
                    return NotFound("الكورس غير موجود");

                // تحقق من صحة ملف الصورة
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(dto.ImageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                    return BadRequest("نوع الملف غير مسموح به. المسموح: jpg, jpeg, png, gif");

                if (dto.ImageFile.Length > 5 * 1024 * 1024) // 5MB
                    return BadRequest("حجم الصورة يجب أن يكون أقل من 5 ميجابايت");

                // حفظ الصورة الجديدة
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "Course");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.ImageFile.CopyToAsync(stream);
                }

                var newImageUrl = $"/uploads/Course/{fileName}";

                // حذف الصورة القديمة إذا كانت موجودة
                if (!string.IsNullOrEmpty(Courses.ImageUrl))
                {
                    var oldImagePath = Path.Combine(_environment.WebRootPath, Courses.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // تحديث رابط الصورة
                Courses.ImageUrl = newImageUrl;
                Courses.UpdatedAt = DateTime.Now;

                _context.Courses.Update(Courses);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "تم تحديث صورة الكورس بنجاح",
                    imageUrl = newImageUrl
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"حدث خطأ: {ex.Message}");
            }
        }
    }

    // DTOs
    public class CreateCourseDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string University { get; set; }
        public IFormFile ImageFile { get; set; }
        public string Page { get; set; } = "page-1";
    }

    public class UpdateCourseDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string University { get; set; }
        public string Page { get; set; }
    }

    public class UpdateCourseImageDto
    {
        public IFormFile ImageFile { get; set; }
    }
}