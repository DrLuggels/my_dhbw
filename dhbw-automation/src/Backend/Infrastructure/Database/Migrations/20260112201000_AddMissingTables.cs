using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace DHBWAutomation.Backend.Infrastructure.Database.Migrations
{
    /// <summary>
    /// Adds missing tables that were not in the initial database setup
    /// </summary>
    public partial class AddMissingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ========== COURSES TABLE ==========
            migrationBuilder.CreateTable(
                name: "courses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CourseName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    CourseCode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    Professor = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Semester = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    MoodleUrl = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    MoodleId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    StartDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AdditionalInfo = table.Column<string>(type: "JSON", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_courses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_courses_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_courses_UserId", table: "courses", column: "UserId");
            migrationBuilder.CreateIndex(name: "IX_courses_MoodleId", table: "courses", column: "MoodleId");
            migrationBuilder.CreateIndex(name: "IX_courses_IsActive", table: "courses", column: "IsActive");

            // ========== PROJECTS TABLE ==========
            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Priority = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Interest = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Importance = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    WeeklyMinutes = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_projects_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_projects_UserId_Status", table: "projects", columns: new[] { "UserId", "Status" });
            migrationBuilder.CreateIndex(name: "IX_projects_Priority", table: "projects", column: "Priority");

            // ========== KNOWLEDGE_BASE_ITEMS TABLE ==========
            migrationBuilder.CreateTable(
                name: "KnowledgeBaseItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Subject = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Topic = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Subtopic = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    Category = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Importance = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    LastTestedDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TestCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    AverageScore = table.Column<double>(type: "double", nullable: false, defaultValue: 0.0),
                    LastScore = table.Column<double>(type: "double", nullable: false, defaultValue: 0.0),
                    NextReviewDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Notes = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    HasEmbedding = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    QdrantPointId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    SourceType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    SourceId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeBaseItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeBaseItems_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_KnowledgeBaseItems_UserId_Subject_Topic", table: "KnowledgeBaseItems", columns: new[] { "UserId", "Subject", "Topic" });
            migrationBuilder.CreateIndex(name: "IX_KnowledgeBaseItems_UserId_NextReviewDate", table: "KnowledgeBaseItems", columns: new[] { "UserId", "NextReviewDate" });
            migrationBuilder.CreateIndex(name: "IX_KnowledgeBaseItems_UserId_IsActive_NextReviewDate", table: "KnowledgeBaseItems", columns: new[] { "UserId", "IsActive", "NextReviewDate" });
            migrationBuilder.CreateIndex(name: "IX_KnowledgeBaseItems_Category", table: "KnowledgeBaseItems", column: "Category");
            migrationBuilder.CreateIndex(name: "IX_KnowledgeBaseItems_Importance", table: "KnowledgeBaseItems", column: "Importance");

            // ========== INTERACTIVE_EXERCISES TABLE ==========
            migrationBuilder.CreateTable(
                name: "interactive_exercises",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    DeficitId = table.Column<int>(type: "int", nullable: true),
                    KnowledgeBaseItemId = table.Column<int>(type: "int", nullable: true),
                    Subject = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Topic = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Difficulty = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    ExerciseContent = table.Column<string>(type: "TEXT", nullable: false),
                    StepProgress = table.Column<string>(type: "TEXT", nullable: false),
                    CompletedSteps = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TotalSteps = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Score = table.Column<double>(type: "double", nullable: false, defaultValue: 0),
                    TimeSpentSeconds = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    NextReviewDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ReviewCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    EaseFactor = table.Column<double>(type: "double", nullable: false, defaultValue: 2.5)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interactive_exercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_interactive_exercises_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_interactive_exercises_learning_deficits_DeficitId",
                        column: x => x.DeficitId,
                        principalTable: "learning_deficits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_interactive_exercises_KnowledgeBaseItems_KnowledgeBaseItemId",
                        column: x => x.KnowledgeBaseItemId,
                        principalTable: "KnowledgeBaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(name: "IX_interactive_exercises_UserId_Subject", table: "interactive_exercises", columns: new[] { "UserId", "Subject" });
            migrationBuilder.CreateIndex(name: "IX_interactive_exercises_NextReviewDate", table: "interactive_exercises", column: "NextReviewDate");
            migrationBuilder.CreateIndex(name: "IX_interactive_exercises_UserId_CompletedAt", table: "interactive_exercises", columns: new[] { "UserId", "CompletedAt" });
            migrationBuilder.CreateIndex(name: "IX_interactive_exercises_Difficulty", table: "interactive_exercises", column: "Difficulty");
            migrationBuilder.CreateIndex(name: "IX_interactive_exercises_DeficitId", table: "interactive_exercises", column: "DeficitId");
            migrationBuilder.CreateIndex(name: "IX_interactive_exercises_KnowledgeBaseItemId", table: "interactive_exercises", column: "KnowledgeBaseItemId");

            // ========== NEXTCLOUD_CREDENTIALS TABLE ==========
            migrationBuilder.CreateTable(
                name: "nextcloud_credentials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    NextcloudUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    Username = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    EncryptedPassword = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    SyncIntervalMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 60),
                    SyncFolders = table.Column<string>(type: "JSON", nullable: true),
                    LastSyncAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastSyncError = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nextcloud_credentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_nextcloud_credentials_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_nextcloud_credentials_UserId_IsActive", table: "nextcloud_credentials", columns: new[] { "UserId", "IsActive" });
            migrationBuilder.CreateIndex(name: "IX_nextcloud_credentials_LastSyncAt", table: "nextcloud_credentials", column: "LastSyncAt");

            // ========== NEXTCLOUD_FILES TABLE ==========
            migrationBuilder.CreateTable(
                name: "nextcloud_files",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CredentialId = table.Column<int>(type: "int", nullable: false),
                    RemotePath = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false),
                    FileName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    FileType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    ETag = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    RemoteModifiedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LocalSyncedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LocalDocumentId = table.Column<int>(type: "int", nullable: true),
                    IsDownloaded = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    IsProcessed = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nextcloud_files", x => x.Id);
                    table.ForeignKey(
                        name: "FK_nextcloud_files_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_nextcloud_files_nextcloud_credentials_CredentialId",
                        column: x => x.CredentialId,
                        principalTable: "nextcloud_credentials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_nextcloud_files_documents_LocalDocumentId",
                        column: x => x.LocalDocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(name: "IX_nextcloud_files_UserId_RemotePath", table: "nextcloud_files", columns: new[] { "UserId", "RemotePath" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_nextcloud_files_CredentialId_IsDownloaded", table: "nextcloud_files", columns: new[] { "CredentialId", "IsDownloaded" });
            migrationBuilder.CreateIndex(name: "IX_nextcloud_files_ETag", table: "nextcloud_files", column: "ETag");
            migrationBuilder.CreateIndex(name: "IX_nextcloud_files_LocalDocumentId", table: "nextcloud_files", column: "LocalDocumentId");

            // ========== MOODLE_COURSES TABLE ==========
            migrationBuilder.CreateTable(
                name: "moodle_courses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    MoodleCourseId = table.Column<int>(type: "int", nullable: false),
                    Shortname = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Fullname = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    Summary = table.Column<string>(type: "TEXT", nullable: true),
                    Format = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Visible = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    Progress = table.Column<int>(type: "int", nullable: true),
                    LastSynced = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moodle_courses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_moodle_courses_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_moodle_courses_UserId_MoodleCourseId", table: "moodle_courses", columns: new[] { "UserId", "MoodleCourseId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_moodle_courses_UserId", table: "moodle_courses", column: "UserId");
            migrationBuilder.CreateIndex(name: "IX_moodle_courses_LastSynced", table: "moodle_courses", column: "LastSynced");

            // ========== MOODLE_RESOURCES TABLE ==========
            migrationBuilder.CreateTable(
                name: "moodle_resources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    CourseName = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true),
                    ResourceType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    MoodleResourceId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    DownloadUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    ExternalUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    FileType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    SectionNumber = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    SectionName = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true),
                    LocalDocumentId = table.Column<int>(type: "int", nullable: true),
                    IsDownloaded = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    LastCheckedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    SyncedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moodle_resources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_moodle_resources_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_moodle_resources_documents_LocalDocumentId",
                        column: x => x.LocalDocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(name: "IX_moodle_resources_UserId_CourseId", table: "moodle_resources", columns: new[] { "UserId", "CourseId" });
            migrationBuilder.CreateIndex(name: "IX_moodle_resources_UserId_MoodleResourceId", table: "moodle_resources", columns: new[] { "UserId", "MoodleResourceId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_moodle_resources_ResourceType", table: "moodle_resources", column: "ResourceType");
            migrationBuilder.CreateIndex(name: "IX_moodle_resources_IsDownloaded", table: "moodle_resources", column: "IsDownloaded");
            migrationBuilder.CreateIndex(name: "IX_moodle_resources_LocalDocumentId", table: "moodle_resources", column: "LocalDocumentId");

            // ========== MOODLE_ASSIGNMENTS TABLE ==========
            migrationBuilder.CreateTable(
                name: "moodle_assignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    CourseName = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true),
                    MoodleAssignmentId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CutoffDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AllowSubmissionsFrom = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    MaxGrade = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                    IsSubmitted = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    SubmissionStatus = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    Grade = table.Column<double>(type: "double", nullable: true),
                    GradingStatus = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    CalendarEventId = table.Column<int>(type: "int", nullable: true),
                    TodoId = table.Column<int>(type: "int", nullable: true),
                    SyncedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moodle_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_moodle_assignments_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_moodle_assignments_calendar_events_CalendarEventId",
                        column: x => x.CalendarEventId,
                        principalTable: "calendar_events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_moodle_assignments_todos_TodoId",
                        column: x => x.TodoId,
                        principalTable: "todos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(name: "IX_moodle_assignments_UserId_CourseId", table: "moodle_assignments", columns: new[] { "UserId", "CourseId" });
            migrationBuilder.CreateIndex(name: "IX_moodle_assignments_UserId_MoodleAssignmentId", table: "moodle_assignments", columns: new[] { "UserId", "MoodleAssignmentId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_moodle_assignments_DueDate", table: "moodle_assignments", column: "DueDate");
            migrationBuilder.CreateIndex(name: "IX_moodle_assignments_UserId_IsSubmitted", table: "moodle_assignments", columns: new[] { "UserId", "IsSubmitted" });
            migrationBuilder.CreateIndex(name: "IX_moodle_assignments_CalendarEventId", table: "moodle_assignments", column: "CalendarEventId");
            migrationBuilder.CreateIndex(name: "IX_moodle_assignments_TodoId", table: "moodle_assignments", column: "TodoId");

            // ========== MOODLE_CALENDAR_EVENTS TABLE ==========
            migrationBuilder.CreateTable(
                name: "moodle_calendar_events",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    MoodleEventId = table.Column<int>(type: "int", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: true),
                    CourseName = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true),
                    Name = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    EventType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    ModuleName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    TimeStart = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TimeDuration = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CalendarEventId = table.Column<int>(type: "int", nullable: true),
                    SyncedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moodle_calendar_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_moodle_calendar_events_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_moodle_calendar_events_calendar_events_CalendarEventId",
                        column: x => x.CalendarEventId,
                        principalTable: "calendar_events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(name: "IX_moodle_calendar_events_UserId_MoodleEventId", table: "moodle_calendar_events", columns: new[] { "UserId", "MoodleEventId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_moodle_calendar_events_UserId", table: "moodle_calendar_events", column: "UserId");
            migrationBuilder.CreateIndex(name: "IX_moodle_calendar_events_TimeStart", table: "moodle_calendar_events", column: "TimeStart");
            migrationBuilder.CreateIndex(name: "IX_moodle_calendar_events_EventType", table: "moodle_calendar_events", column: "EventType");
            migrationBuilder.CreateIndex(name: "IX_moodle_calendar_events_CalendarEventId", table: "moodle_calendar_events", column: "CalendarEventId");

            // ========== DOCUMENT_IMAGES TABLE ==========
            migrationBuilder.CreateTable(
                name: "document_images",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    PageNumber = table.Column<int>(type: "int", nullable: false),
                    ImageIndex = table.Column<int>(type: "int", nullable: false),
                    StoragePath = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    ImageFormat = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    GeminiDescription = table.Column<string>(type: "TEXT", nullable: true),
                    ExtractedText = table.Column<string>(type: "TEXT", nullable: true),
                    DetectedObjects = table.Column<string>(type: "JSON", nullable: true),
                    ImageType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    RelevanceScore = table.Column<double>(type: "double", nullable: false, defaultValue: 0.5),
                    IsProcessed = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    HasEmbedding = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    QdrantPointId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_images", x => x.Id);
                    table.ForeignKey(
                        name: "FK_document_images_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_document_images_DocumentId", table: "document_images", column: "DocumentId");
            migrationBuilder.CreateIndex(name: "IX_document_images_DocumentId_PageNumber_ImageIndex", table: "document_images", columns: new[] { "DocumentId", "PageNumber", "ImageIndex" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_document_images_IsProcessed", table: "document_images", column: "IsProcessed");
            migrationBuilder.CreateIndex(name: "IX_document_images_ImageType", table: "document_images", column: "ImageType");
            migrationBuilder.CreateIndex(name: "IX_document_images_HasEmbedding", table: "document_images", column: "HasEmbedding");

            // ========== ADD FK FOR DOCUMENTS -> PROJECTS ==========
            // Add RelatedProjectId column if it doesn't exist
            migrationBuilder.AddColumn<int>(
                name: "RelatedProjectId",
                table: "documents",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_documents_RelatedProjectId",
                table: "documents",
                column: "RelatedProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_documents_projects_RelatedProjectId",
                table: "documents",
                column: "RelatedProjectId",
                principalTable: "projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // ========== ADD FK FOR TODOS -> PROJECTS ==========
            migrationBuilder.AddColumn<int>(
                name: "RelatedProjectId",
                table: "todos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_todos_RelatedProjectId",
                table: "todos",
                column: "RelatedProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_todos_projects_RelatedProjectId",
                table: "todos",
                column: "RelatedProjectId",
                principalTable: "projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // ========== ADD FK FOR GENERATED_EXERCISES -> KNOWLEDGE_BASE_ITEMS ==========
            migrationBuilder.AddColumn<int>(
                name: "KnowledgeBaseItemId",
                table: "generated_exercises",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_generated_exercises_KnowledgeBaseItemId",
                table: "generated_exercises",
                column: "KnowledgeBaseItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_generated_exercises_KnowledgeBaseItems_KnowledgeBaseItemId",
                table: "generated_exercises",
                column: "KnowledgeBaseItemId",
                principalTable: "KnowledgeBaseItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop foreign keys first
            migrationBuilder.DropForeignKey(name: "FK_generated_exercises_KnowledgeBaseItems_KnowledgeBaseItemId", table: "generated_exercises");
            migrationBuilder.DropForeignKey(name: "FK_todos_projects_RelatedProjectId", table: "todos");
            migrationBuilder.DropForeignKey(name: "FK_documents_projects_RelatedProjectId", table: "documents");

            // Drop indexes and columns
            migrationBuilder.DropIndex(name: "IX_generated_exercises_KnowledgeBaseItemId", table: "generated_exercises");
            migrationBuilder.DropColumn(name: "KnowledgeBaseItemId", table: "generated_exercises");

            migrationBuilder.DropIndex(name: "IX_todos_RelatedProjectId", table: "todos");
            migrationBuilder.DropColumn(name: "RelatedProjectId", table: "todos");

            migrationBuilder.DropIndex(name: "IX_documents_RelatedProjectId", table: "documents");
            migrationBuilder.DropColumn(name: "RelatedProjectId", table: "documents");

            // Drop tables in reverse order
            migrationBuilder.DropTable(name: "document_images");
            migrationBuilder.DropTable(name: "moodle_calendar_events");
            migrationBuilder.DropTable(name: "moodle_assignments");
            migrationBuilder.DropTable(name: "moodle_resources");
            migrationBuilder.DropTable(name: "moodle_courses");
            migrationBuilder.DropTable(name: "nextcloud_files");
            migrationBuilder.DropTable(name: "nextcloud_credentials");
            migrationBuilder.DropTable(name: "interactive_exercises");
            migrationBuilder.DropTable(name: "KnowledgeBaseItems");
            migrationBuilder.DropTable(name: "projects");
            migrationBuilder.DropTable(name: "courses");
        }
    }
}
