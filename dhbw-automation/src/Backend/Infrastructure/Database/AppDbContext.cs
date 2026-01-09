using Microsoft.EntityFrameworkCore;
using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.Infrastructure.Database;

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

    // NEW: AI-System DbSets
    public DbSet<UserInteraction> UserInteractions { get; set; } = null!;
    public DbSet<Todo> Todos { get; set; } = null!;
    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<LearningDeficit> LearningDeficits { get; set; } = null!;
    public DbSet<GeneratedExercise> GeneratedExercises { get; set; } = null!;

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
            entity.HasIndex(e => e.DocumentCategory);
            entity.HasIndex(e => e.IsArchived);

            entity.HasOne(e => e.RelatedProject)
                .WithMany(p => p.Documents)
                .HasForeignKey(e => e.RelatedProjectId)
                .OnDelete(DeleteBehavior.SetNull);
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

        // UserInteraction Configuration
        modelBuilder.Entity<UserInteraction>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.Status, e.CreatedAt });
            entity.HasIndex(e => e.SnoozeUntil);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.RelatedDocument)
                .WithMany()
                .HasForeignKey(e => e.RelatedDocumentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.RelatedEvent)
                .WithMany()
                .HasForeignKey(e => e.RelatedEventId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.RelatedTodo)
                .WithMany()
                .HasForeignKey(e => e.RelatedTodoId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Todo Configuration
        modelBuilder.Entity<Todo>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.Status });
            entity.HasIndex(e => new { e.UserId, e.DueDate });
            entity.HasIndex(e => e.Category);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.RelatedDocument)
                .WithMany()
                .HasForeignKey(e => e.RelatedDocumentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.RelatedEvent)
                .WithMany()
                .HasForeignKey(e => e.RelatedEventId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.RelatedProject)
                .WithMany(p => p.Todos)
                .HasForeignKey(e => e.RelatedProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Project Configuration
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.Status });
            entity.HasIndex(e => e.Priority);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // LearningDeficit Configuration
        modelBuilder.Entity<LearningDeficit>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.Subject, e.Topic });
            entity.HasIndex(e => e.NeedsTutoring);
            entity.HasIndex(e => e.Severity);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // GeneratedExercise Configuration
        modelBuilder.Entity<GeneratedExercise>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.Subject });
            entity.HasIndex(e => e.NextReviewDate);
            entity.HasIndex(e => new { e.UserId, e.IsCorrect });

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Deficit)
                .WithMany()
                .HasForeignKey(e => e.DeficitId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
