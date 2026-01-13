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

    // NEW: Knowledge Network System DbSets
    public DbSet<NextcloudCredential> NextcloudCredentials { get; set; } = null!;
    public DbSet<NextcloudFile> NextcloudFiles { get; set; } = null!;
    public DbSet<MoodleResource> MoodleResources { get; set; } = null!;
    public DbSet<MoodleAssignment> MoodleAssignments { get; set; } = null!;
    public DbSet<MoodleCourse> MoodleCourses { get; set; } = null!;
    public DbSet<MoodleCalendarEvent> MoodleCalendarEvents { get; set; } = null!;
    public DbSet<JavaDocsExercise> JavaDocsExercises { get; set; } = null!;
    public DbSet<DocumentImage> DocumentImages { get; set; } = null!;
    public DbSet<KnowledgeLink> KnowledgeLinks { get; set; } = null!;
    public DbSet<ContentTag> ContentTags { get; set; } = null!;
    public DbSet<ContentTagAssignment> ContentTagAssignments { get; set; } = null!;
    public DbSet<QdrantEmbedding> QdrantEmbeddings { get; set; } = null!;
    public DbSet<DocumentChunk> DocumentChunks { get; set; } = null!;

    // AKGLS (Adaptive Knowledge Graph Learning System) DbSets
    public DbSet<UserKnowledgeNode> UserKnowledgeNodes { get; set; } = null!;
    public DbSet<UserKnowledgeEdge> UserKnowledgeEdges { get; set; } = null!;
    public DbSet<LearningPriority> LearningPriorities { get; set; } = null!;
    public DbSet<UserDecayProfile> UserDecayProfiles { get; set; } = null!;
    public DbSet<LearningStreak> LearningStreaks { get; set; } = null!;
    public DbSet<ExamSimulation> ExamSimulations { get; set; } = null!;
    public DbSet<PrerequisiteChain> PrerequisiteChains { get; set; } = null!;

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

        // === Knowledge Network System Configurations ===

        // NextcloudCredential Configuration
        modelBuilder.Entity<NextcloudCredential>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.IsActive });
            entity.HasIndex(e => e.LastSyncAt);

            entity.HasOne(e => e.User)
                .WithMany(u => u.NextcloudCredentials)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Files)
                .WithOne(f => f.Credential)
                .HasForeignKey(f => f.CredentialId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // NextcloudFile Configuration
        modelBuilder.Entity<NextcloudFile>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.RemotePath }).IsUnique();
            entity.HasIndex(e => new { e.CredentialId, e.IsDownloaded });
            entity.HasIndex(e => e.ETag);
            entity.HasIndex(e => e.LocalDocumentId);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.LocalDocument)
                .WithMany()
                .HasForeignKey(e => e.LocalDocumentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // MoodleResource Configuration
        modelBuilder.Entity<MoodleResource>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.CourseId });
            entity.HasIndex(e => new { e.UserId, e.MoodleResourceId }).IsUnique();
            entity.HasIndex(e => e.ResourceType);
            entity.HasIndex(e => e.IsDownloaded);
            entity.HasIndex(e => e.LocalDocumentId);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.LocalDocument)
                .WithMany()
                .HasForeignKey(e => e.LocalDocumentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // MoodleAssignment Configuration
        modelBuilder.Entity<MoodleAssignment>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.CourseId });
            entity.HasIndex(e => new { e.UserId, e.MoodleAssignmentId }).IsUnique();
            entity.HasIndex(e => e.DueDate);
            entity.HasIndex(e => new { e.UserId, e.IsSubmitted });
            entity.HasIndex(e => e.CalendarEventId);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CalendarEvent)
                .WithMany()
                .HasForeignKey(e => e.CalendarEventId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Todo)
                .WithMany()
                .HasForeignKey(e => e.TodoId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // MoodleCourse Configuration
        modelBuilder.Entity<MoodleCourse>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.MoodleCourseId }).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.LastSynced);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // MoodleCalendarEvent Configuration
        modelBuilder.Entity<MoodleCalendarEvent>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.MoodleEventId }).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.TimeStart);
            entity.HasIndex(e => e.EventType);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CalendarEvent)
                .WithMany()
                .HasForeignKey(e => e.CalendarEventId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // JavaDocsExercise Configuration
        modelBuilder.Entity<JavaDocsExercise>(entity =>
        {
            entity.HasIndex(e => e.FilePath).IsUnique();
            entity.HasIndex(e => e.Topic);
            entity.HasIndex(e => e.Difficulty);
            entity.HasIndex(e => e.ExerciseType);
            entity.HasIndex(e => e.HasEmbedding);
            entity.HasIndex(e => e.GitCommitHash);
        });

        // DocumentImage Configuration
        modelBuilder.Entity<DocumentImage>(entity =>
        {
            entity.HasIndex(e => e.DocumentId);
            entity.HasIndex(e => new { e.DocumentId, e.PageNumber, e.ImageIndex }).IsUnique();
            entity.HasIndex(e => e.IsProcessed);
            entity.HasIndex(e => e.ImageType);
            entity.HasIndex(e => e.HasEmbedding);

            entity.HasOne(e => e.Document)
                .WithMany(d => d.Images)
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // DocumentChunk Configuration
        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.HasIndex(e => e.DocumentId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.DocumentId, e.ChunkIndex }).IsUnique();
            entity.HasIndex(e => e.TopicLabel);
            entity.HasIndex(e => e.HasEmbedding);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.HasBeenLinked, e.HasEmbedding });
            entity.HasIndex(e => new { e.HasEventLinks, e.HasKnowledgeLinks, e.HasExerciseLinks });

            entity.HasOne(e => e.Document)
                .WithMany(d => d.Chunks)
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // KnowledgeLink Configuration
        modelBuilder.Entity<KnowledgeLink>(entity =>
        {
            entity.HasIndex(e => new { e.SourceType, e.SourceId });
            entity.HasIndex(e => new { e.TargetType, e.TargetId });
            entity.HasIndex(e => new { e.SourceType, e.SourceId, e.TargetType, e.TargetId }).IsUnique();
            entity.HasIndex(e => e.LinkType);
            entity.HasIndex(e => e.IsAutoGenerated);
            entity.HasIndex(e => new { e.UserId, e.IsRejected });

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ContentTag Configuration
        modelBuilder.Entity<ContentTag>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.Name }).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.SortOrder });
            entity.HasIndex(e => e.IsSystem);

            entity.HasOne(e => e.User)
                .WithMany(u => u.ContentTags)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Assignments)
                .WithOne(a => a.Tag)
                .HasForeignKey(a => a.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ContentTagAssignment Configuration
        modelBuilder.Entity<ContentTagAssignment>(entity =>
        {
            entity.HasIndex(e => new { e.TagId, e.EntityType, e.EntityId }).IsUnique();
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.HasIndex(e => e.IsAutoAssigned);

            entity.HasOne(e => e.AssignedByUser)
                .WithMany()
                .HasForeignKey(e => e.AssignedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // QdrantEmbedding Configuration
        modelBuilder.Entity<QdrantEmbedding>(entity =>
        {
            entity.HasIndex(e => new { e.EntityType, e.EntityId }).IsUnique();
            entity.HasIndex(e => e.QdrantPointId).IsUnique();
            entity.HasIndex(e => e.CollectionName);
            entity.HasIndex(e => e.UserId);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // === AKGLS (Adaptive Knowledge Graph Learning System) Configurations ===

        // UserKnowledgeNode Configuration
        modelBuilder.Entity<UserKnowledgeNode>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.Subject, e.Topic }).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.Subject });
            entity.HasIndex(e => e.MasteryLevel);
            entity.HasIndex(e => e.LastInteraction);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserKnowledgeEdge Configuration
        modelBuilder.Entity<UserKnowledgeEdge>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.SourceNodeId, e.TargetNodeId }).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.EdgeType);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.SourceNode)
                .WithMany()
                .HasForeignKey(e => e.SourceNodeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.TargetNode)
                .WithMany()
                .HasForeignKey(e => e.TargetNodeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // LearningPriority Configuration
        modelBuilder.Entity<LearningPriority>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.CompositeScore });
            entity.HasIndex(e => e.Deadline);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.KnowledgeNode)
                .WithMany()
                .HasForeignKey(e => e.UserKnowledgeNodeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.MoodleAssignment)
                .WithMany()
                .HasForeignKey(e => e.MoodleAssignmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // UserDecayProfile Configuration
        modelBuilder.Entity<UserDecayProfile>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.Subject }).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // LearningStreak Configuration
        modelBuilder.Entity<LearningStreak>(entity =>
        {
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.LastActivityDate);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ExamSimulation Configuration
        modelBuilder.Entity<ExamSimulation>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.Subject });
            entity.HasIndex(e => e.StartedAt);
            entity.HasIndex(e => e.CompletedAt);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.MoodleAssignment)
                .WithMany()
                .HasForeignKey(e => e.MoodleAssignmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // PrerequisiteChain Configuration
        modelBuilder.Entity<PrerequisiteChain>(entity =>
        {
            entity.HasIndex(e => new { e.PrerequisiteNodeId, e.DependentNodeId }).IsUnique();

            entity.HasOne(e => e.PrerequisiteNode)
                .WithMany()
                .HasForeignKey(e => e.PrerequisiteNodeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.DependentNode)
                .WithMany()
                .HasForeignKey(e => e.DependentNodeId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
