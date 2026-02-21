
// Models/Notification.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace EduPlatform.API.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [Required]
        public string Message { get; set; }

        [MaxLength(500)]
        public string Body { get; set; }

        [MaxLength(50)]
        public string Type { get; set; }

        [MaxLength(50)]
        public string Target { get; set; }

        [MaxLength(50)]
        public string RecipientType { get; set; }

        public string RecipientId { get; set; }

        [MaxLength(100)]
        public string CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; }

        public DateTime? ReadAt { get; set; }
    }
}