using EduPlatform.API.Data;
using EduPlatform.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EduPlatform.Services
{
    public class ActivityLogService
    {
        private readonly ApplicationDbContext _context;

        public ActivityLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogActivityAsync(string userId, string userName, string action, string details)
        {
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

        public async Task<List<Activity>> GetRecentActivitiesAsync(int count = 50)
        {
            return await _context.Activities
                .OrderByDescending(a => a.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task LogUserActivityAsync(string userId, string action, int? CourseId = null, int? lessonId = null, string details = "")
        {
            var userActivity = new UserActivity
            {
                UserId = userId,
                Action = action,
                CourseId = CourseId,
                LessonId = lessonId,
                Details = details,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserActivities.Add(userActivity);
            await _context.SaveChangesAsync();
        }
    }
}