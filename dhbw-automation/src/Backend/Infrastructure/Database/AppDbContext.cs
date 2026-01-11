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
    public DbSet<TodoList> TodoLists { get; set; } = null!;
    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<LearningDeficit> LearningDeficits { get; set; } = null!;
    public DbSet<GeneratedExercise> GeneratedExercises { get; set; } = null!;
    public DbSet<KnowledgeBaseItem> KnowledgeBaseItems { get; set; } = null!;
    public DbSet<InteractiveExercise> InteractiveExercises { get; set; } = null!;

    // NEW: Email-System DbSets
    public DbSet<Email> Emails { get; set; } = null!;
    public DbSet<EmailAttachment> EmailAttachments { get; set; } = null!;

    // NEW: AI Staging System DbSets (für validierte Datenqualität)
    public DbSet<StagedEntity> StagedEntities { get; set; } = null!;
    public DbSet<AIQuestion> AIQuestions { get; set; } = null!;

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

        // TodoList Configuration
        modelBuilder.Entity<TodoList>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.Name }).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.SortOrder });
            entity.HasIndex(e => e.IsDefault);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Todos)
                .WithOne(t => t.List)
                .HasForeignKey(t => t.ListId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Todo Configuration
        modelBuilder.Entity<Todo>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.Status });
            entity.HasIndex(e => new { e.UserId, e.DueDate });
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.ListId);
            entity.HasIndex(e => e.ArchivedAt);
            entity.HasIndex(e => new { e.Status, e.ArchivedAt, e.CompletedAt });

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.List)
                .WithMany(l => l.Todos)
                .HasForeignKey(e => e.ListId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.ParentTodo)
                .WithMany()
                .HasForeignKey(e => e.ParentTodoId)
                .OnDelete(DeleteBehavior.SetNull);

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

        // KnowledgeBaseItem Configuration
        modelBuilder.Entity<KnowledgeBaseItem>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.Subject, e.Topic });
            entity.HasIndex(e => new { e.UserId, e.NextReviewDate });
            entity.HasIndex(e => new { e.UserId, e.IsActive, e.NextReviewDate });
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.Importance);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // InteractiveExercise Configuration (Brilliant-style exercises)
        modelBuilder.Entity<InteractiveExercise>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.Subject });
            entity.HasIndex(e => e.NextReviewDate);
            entity.HasIndex(e => new { e.UserId, e.CompletedAt });
            entity.HasIndex(e => e.Difficulty);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Deficit)
                .WithMany()
                .HasForeignKey(e => e.DeficitId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.KnowledgeBaseItem)
                .WithMany()
                .HasForeignKey(e => e.KnowledgeBaseItemId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Email Configuration
        modelBuilder.Entity<Email>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.MessageId }).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.IsRead, e.ReceivedAt });
            entity.HasIndex(e => new { e.UserId, e.IsProcessed });
            entity.HasIndex(e => new { e.UserId, e.RequiresUserAction, e.ActionStatus });
            entity.HasIndex(e => e.Folder);
            entity.HasIndex(e => e.Category);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Emails)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.RelatedCalendarEvent)
                .WithMany()
                .HasForeignKey(e => e.RelatedCalendarEventId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Attachments)
                .WithOne(a => a.Email)
                .HasForeignKey(a => a.EmailId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // EmailAttachment Configuration
        modelBuilder.Entity<EmailAttachment>(entity =>
        {
            entity.HasIndex(e => e.EmailId);
            entity.HasIndex(e => e.RelatedDocumentId);
            entity.HasIndex(e => new { e.EmailId, e.IsProcessed });

            entity.HasOne(e => e.Email)
                .WithMany(email => email.Attachments)
                .HasForeignKey(e => e.EmailId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.RelatedDocument)
                .WithMany()
                .HasForeignKey(e => e.RelatedDocumentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // StagedEntity Configuration - AI Staging System
        modelBuilder.Entity<StagedEntity>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.Status, e.Priority });
            entity.HasIndex(e => new { e.UserId, e.EntityType, e.Status });
            entity.HasIndex(e => e.ConfidenceScore);
            entity.HasIndex(e => new { e.Status, e.CreatedAt });
            entity.HasIndex(e => new { e.IsPromoted, e.PromotedAt });

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.SourceDocument)
                .WithMany()
                .HasForeignKey(e => e.SourceDocumentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Questions)
                .WithOne(q => q.StagedEntity)
                .HasForeignKey(q => q.StagedEntityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AIQuestion Configuration
        modelBuilder.Entity<AIQuestion>(entity =>
        {
            entity.HasIndex(e => new { e.StagedEntityId, e.IsAnswered });
            entity.HasIndex(e => new { e.StagedEntityId, e.Priority });
            entity.HasIndex(e => e.AnswerType);

            entity.HasOne(e => e.StagedEntity)
                .WithMany(s => s.Questions)
                .HasForeignKey(e => e.StagedEntityId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
