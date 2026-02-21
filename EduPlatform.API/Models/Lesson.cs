using EduPlatform.API.Models;

public class Lesson
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public int CourseId { get; set; }
    public int Order { get; set; }
    public string? Duration { get; set; }
    public string? BunnyVideoId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    public Courses Courses { get; set; }
}