using EduPlatform.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EduPlatform.API.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    // الجداول الحالية
    public DbSet<Courses> Courses { get; set; }
    public DbSet<Lesson> Lessons { get; set; }
    public DbSet<Purchase> Purchases { get; set; }

    // الجداول الجديدة للداشبورد والإدارة
    public DbSet<Activity> Activities { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Discount> Discounts { get; set; }
    public DbSet<SystemSettings> SystemSettings { get; set; }

    // الجداول الجديدة لإدارة المستخدمين
    public DbSet<Wishlist> Wishlists { get; set; }
    public DbSet<UserActivity> UserActivities { get; set; }
    public DbSet<Enrollment> Enrollments { get; set; }

    // جداول العلاقات many-to-many
    public DbSet<CourseDiscount> CourseDiscounts { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ========== تكوين جداول الداشبورد ==========

        // تكوين جدول Activities
        builder.Entity<Activity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UserName).HasMaxLength(100);
            entity.Property(e => e.Details).HasMaxLength(500);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired(false);
        });

        // تكوين جدول Notifications
        builder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Type).HasMaxLength(20);
            entity.Property(e => e.RecipientType).HasMaxLength(20);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.ReadAt).IsRequired(false);

            entity.Property(e => e.RecipientId)
                  .HasColumnName("UserId")
                  .HasMaxLength(450)
                  .IsRequired(false);
        });

        // تكوين جدول Discounts
        builder.Entity<Discount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Percent).IsRequired();
            entity.Property(e => e.StartDate).IsRequired();
            entity.Property(e => e.EndDate).IsRequired();
            entity.Property(e => e.MinAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MaxUses).IsRequired(false);
            entity.Property(e => e.UsedCount).HasDefaultValue(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("active");

            entity.Ignore(e => e.CourseIds);
            entity.Ignore(e => e.CourseNames);

            entity.HasMany(d => d.CourseDiscounts)
                  .WithOne(cd => cd.Discount)
                  .HasForeignKey(cd => cd.DiscountId);
        });

        // تكوين جدول SystemSettings
        builder.Entity<SystemSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SiteName).HasMaxLength(100);
            entity.Property(e => e.SupportEmail).HasMaxLength(100);
            entity.Property(e => e.SupportPhone).HasMaxLength(20);
            entity.Property(e => e.WelcomeMessage).HasMaxLength(500);
            entity.Property(e => e.EnableRegistration).HasDefaultValue(true);
            entity.Property(e => e.EnableCoursePurchase).HasDefaultValue(true);
            entity.Property(e => e.MaintenanceMode).HasDefaultValue(false);
            entity.Property(e => e.Currency).HasMaxLength(10).HasDefaultValue("SAR");
            entity.Property(e => e.EnableEmailNotifications).HasDefaultValue(true);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);

            entity.HasIndex(e => e.UpdatedAt).IsDescending();
        });

        // ========== تكوين جداول إدارة المستخدمين ==========

        // تكوين جدول Wishlists
        builder.Entity<Wishlist>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AddedAt).HasDefaultValueSql("GETDATE()");

            entity.HasIndex(e => new { e.UserId, e.CourseId }).IsUnique();

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Courses)
                  .WithMany(c => c.Wishlists)
                  .HasForeignKey(e => e.CourseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // تكوين جدول UserActivities
        builder.Entity<UserActivity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Details).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasIndex(e => new { e.UserId, e.CreatedAt });

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Courses)
                  .WithMany(c => c.UserActivities)
                  .HasForeignKey(e => e.CourseId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired(false);

            entity.HasOne(e => e.Lesson)
                  .WithMany()
                  .HasForeignKey(e => e.LessonId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired(false);
        });

        // تكوين جدول Enrollments
        builder.Entity<Enrollment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EnrollmentDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.IsCompleted).HasDefaultValue(false);
            entity.Property(e => e.CompletedDate).IsRequired(false);
            entity.Property(e => e.ProgressPercentage).HasColumnType("decimal(5,2)");

            entity.HasIndex(e => new { e.UserId, e.CourseId }).IsUnique();

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Courses)
                  .WithMany(c => c.Enrollments)
                  .HasForeignKey(e => e.CourseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ========== تكوين العلاقات many-to-many ==========

        // تكوين جدول CourseDiscounts
        builder.Entity<CourseDiscount>(entity =>
        {
            entity.HasKey(e => new { e.CourseId, e.DiscountId });

            entity.HasOne(e => e.Courses)
                  .WithMany(c => c.CourseDiscounts)
                  .HasForeignKey(e => e.CourseId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Discount)
                  .WithMany(d => d.CourseDiscounts)
                  .HasForeignKey(e => e.DiscountId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ========== تكوين الجداول الحالية ==========

        // تكوين جدول Courses
        builder.Entity<Courses>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            entity.Property(e => e.University).HasMaxLength(100);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.Page).HasMaxLength(50).HasDefaultValue("page-1");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedAt).IsRequired(false);
            entity.Property(e => e.Rating).HasColumnType("decimal(3,2)");
            entity.Property(e => e.EnrollmentCount).HasDefaultValue(0);

            entity.HasIndex(e => e.Title);
            entity.HasIndex(e => e.University);
            entity.HasIndex(e => e.Price);
            entity.HasIndex(e => e.CreatedAt).IsDescending();
            entity.HasIndex(e => e.IsActive);
        });

        // تكوين جدول Lessons
        builder.Entity<Lesson>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.BunnyVideoId).HasMaxLength(100);

            entity.HasOne(e => e.Courses)
                  .WithMany(c => c.Lesson)
                  .HasForeignKey(e => e.CourseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // تكوين جدول Purchases
        builder.Entity<Purchase>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AmountPaid).HasColumnType("decimal(18,2)");
            entity.Property(e => e.PurchaseDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.TransactionId).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("completed");

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CourseId);
            entity.HasIndex(e => e.PurchaseDate).IsDescending();
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Courses)
                  .WithMany(c => c.Purchases)
                  .HasForeignKey(e => e.CourseId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ========== تحسينات على نموذج المستخدم ==========

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.FullName);
        });

        // ========== بيانات أولية - بدون قيم ديناميكية ==========

        // إعدادات النظام الافتراضية - بقيمة تاريخ ثابتة
        builder.Entity<SystemSettings>().HasData(
            new SystemSettings
            {
                Id = 1,
                SiteName = "منصتي التعليمية",
                SupportEmail = "support@eduiplatform.com",
                SupportPhone = "+966500000000",
                WelcomeMessage = "مرحباً بكم في منصتنا التعليمية",
                EnableRegistration = true,
                EnableCoursePurchase = true,
                MaintenanceMode = false,
                Currency = "SAR",
                EnableEmailNotifications = true,
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedBy = "System"
            }
        );

        // أدوار النظام - بدون Guid.NewGuid()
        builder.Entity<IdentityRole>().HasData(
            new IdentityRole
            {
                Id = "1",
                Name = "Admin",
                NormalizedName = "ADMIN",
                ConcurrencyStamp = "00000000-0000-0000-0000-000000000001"
            },
            new IdentityRole
            {
                Id = "2",
                Name = "User",
                NormalizedName = "USER",
                ConcurrencyStamp = "00000000-0000-0000-0000-000000000002"
            },
            new IdentityRole
            {
                Id = "3",
                Name = "Instructor",
                NormalizedName = "INSTRUCTOR",
                ConcurrencyStamp = "00000000-0000-0000-0000-000000000003"
            }
        );
    }
}