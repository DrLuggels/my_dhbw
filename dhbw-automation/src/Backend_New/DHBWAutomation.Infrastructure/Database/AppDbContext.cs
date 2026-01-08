using Microsoft.EntityFrameworkCore;
using DHBWAutomation.Core.Models;

namespace DHBWAutomation.Infrastructure.Database;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Document> Documents { get; set; } = null!;
    public DbSet<CalendarEvent> CalendarEvents { get; set; } = null!;
    public DbSet<Reminder> Reminders { get; set; } = null!;
    public DbSet<CourseInfo> Courses { get; set; } = null!;
    public DbSet<Email> Emails { get; set; } = null!;
    public DbSet<EmailAttachment> EmailAttachments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.MatriculationNumber);
            
            entity.HasMany(e => e.Documents)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.CalendarEvents)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Reminders)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Document Configuration
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.Category });
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.IsProcessed);
        });

        // CalendarEvent Configuration
        modelBuilder.Entity<CalendarEvent>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.StartTime });
            entity.HasIndex(e => e.ExternalId);
            entity.HasIndex(e => e.Source);
        });

        // Reminder Configuration
        modelBuilder.Entity<Reminder>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.DueDate });
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.Status, e.NotificationSent });
            
            entity.HasOne(e => e.RelatedEvent)
                .WithMany()
                .HasForeignKey(e => e.RelatedEventId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.RelatedDocument)
                .WithMany()
                .HasForeignKey(e => e.RelatedDocumentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // CourseInfo Configuration
        modelBuilder.Entity<CourseInfo>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.MoodleId);
            entity.HasIndex(e => e.IsActive);
        });

        // Email Configuration
        modelBuilder.Entity<Email>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.MessageId).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.ReceivedAt });
            entity.HasIndex(e => new { e.UserId, e.IsRead });
            entity.HasIndex(e => new { e.UserId, e.RequiresUserAction });
            entity.HasIndex(e => e.IsProcessed);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.ActionStatus);

            entity.HasMany(e => e.Attachments)
                .WithOne(e => e.Email)
                .HasForeignKey(e => e.EmailId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.RelatedCalendarEvent)
                .WithMany()
                .HasForeignKey(e => e.RelatedCalendarEventId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // EmailAttachment Configuration
        modelBuilder.Entity<EmailAttachment>(entity =>
        {
            entity.HasIndex(e => e.EmailId);
            entity.HasIndex(e => e.RelatedDocumentId);
            entity.HasIndex(e => e.IsProcessed);

            entity.HasOne(e => e.RelatedDocument)
                .WithMany()
                .HasForeignKey(e => e.RelatedDocumentId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
