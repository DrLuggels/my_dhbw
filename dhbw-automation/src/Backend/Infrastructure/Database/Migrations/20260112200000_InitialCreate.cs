using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace DHBWAutomation.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ========== USERS TABLE ==========
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Email = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    FirstName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    MatriculationNumber = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    Course = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    EmailVerified = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    // Email Integration
                    EmailSyncEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    EmailSyncAddress = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    EmailSyncPassword = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    EmailImapHost = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    EmailImapPort = table.Column<int>(type: "int", nullable: false, defaultValue: 993),
                    EmailSmtpHost = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    EmailSmtpPort = table.Column<int>(type: "int", nullable: false, defaultValue: 587),
                    EmailSyncIntervalMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    LastEmailSync = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    // API Keys
                    OpenAiApiKey = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    AnthropicApiKey = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    GeminiApiKey = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    // Moodle Integration
                    MoodleSyncEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    MoodleUserId = table.Column<int>(type: "int", nullable: true),
                    MoodleUsername = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    MoodlePassword = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    MoodleToken = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    MoodleLastSync = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    MoodleLastSyncError = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_MatriculationNumber",
                table: "users",
                column: "MatriculationNumber");

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

            migrationBuilder.CreateIndex(
                name: "IX_projects_UserId_Status",
                table: "projects",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_projects_Priority",
                table: "projects",
                column: "Priority");

            // ========== DOCUMENTS TABLE ==========
            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    FileType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Category = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Subject = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    Summary = table.Column<string>(type: "TEXT", nullable: true),
                    ExtractedText = table.Column<string>(type: "TEXT", nullable: true),
                    Tags = table.Column<string>(type: "JSON", nullable: true),
                    Metadata = table.Column<string>(type: "JSON", nullable: true),
                    IsProcessed = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Source = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DocumentCategory = table.Column<int>(type: "int", nullable: false),
                    IsTemporary = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    IsArchived = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    ArchivedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DetectedErrors = table.Column<string>(type: "JSON", nullable: true),
                    CorrectedText = table.Column<string>(type: "TEXT", nullable: true),
                    ErrorCount = table.Column<int>(type: "int", nullable: true),
                    RelatedProjectId = table.Column<int>(type: "int", nullable: true),
                    HasEmbedding = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    QdrantPointId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    ImageCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ImagesProcessed = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    ChunkCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsChunked = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    ChunkedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_documents_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_documents_projects_RelatedProjectId",
                        column: x => x.RelatedProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_documents_UserId_Category",
                table: "documents",
                columns: new[] { "UserId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_documents_CreatedAt",
                table: "documents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_documents_IsProcessed",
                table: "documents",
                column: "IsProcessed");

            migrationBuilder.CreateIndex(
                name: "IX_documents_DocumentCategory",
                table: "documents",
                column: "DocumentCategory");

            migrationBuilder.CreateIndex(
                name: "IX_documents_IsArchived",
                table: "documents",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_documents_RelatedProjectId",
                table: "documents",
                column: "RelatedProjectId");

            // ========== CALENDAR_EVENTS TABLE ==========
            migrationBuilder.CreateTable(
                name: "calendar_events",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Location = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    StartTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsAllDay = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    EventType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Subject = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Professor = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    ExternalId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    Source = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_calendar_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_calendar_events_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_calendar_events_UserId_StartTime",
                table: "calendar_events",
                columns: new[] { "UserId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_calendar_events_ExternalId",
                table: "calendar_events",
                column: "ExternalId");

            migrationBuilder.CreateIndex(
                name: "IX_calendar_events_Source",
                table: "calendar_events",
                column: "Source");

            // ========== REMINDERS TABLE ==========
            migrationBuilder.CreateTable(
                name: "reminders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Priority = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    RelatedEventId = table.Column<int>(type: "int", nullable: true),
                    RelatedDocumentId = table.Column<int>(type: "int", nullable: true),
                    IsRecurring = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    RecurrencePattern = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    NotificationSent = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    NotifiedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reminders_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_reminders_calendar_events_RelatedEventId",
                        column: x => x.RelatedEventId,
                        principalTable: "calendar_events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_reminders_documents_RelatedDocumentId",
                        column: x => x.RelatedDocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_reminders_UserId_DueDate",
                table: "reminders",
                columns: new[] { "UserId", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_reminders_Status",
                table: "reminders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_reminders_Status_NotificationSent",
                table: "reminders",
                columns: new[] { "Status", "NotificationSent" });

            migrationBuilder.CreateIndex(
                name: "IX_reminders_RelatedEventId",
                table: "reminders",
                column: "RelatedEventId");

            migrationBuilder.CreateIndex(
                name: "IX_reminders_RelatedDocumentId",
                table: "reminders",
                column: "RelatedDocumentId");

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

            migrationBuilder.CreateIndex(
                name: "IX_courses_UserId",
                table: "courses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_courses_MoodleId",
                table: "courses",
                column: "MoodleId");

            migrationBuilder.CreateIndex(
                name: "IX_courses_IsActive",
                table: "courses",
                column: "IsActive");

            // ========== TODO_LISTS TABLE ==========
            migrationBuilder.CreateTable(
                name: "todo_lists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Icon = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "varchar(7)", maxLength: 7, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsDefault = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    IsArchiveList = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_todo_lists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_todo_lists_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_todo_lists_UserId_Name",
                table: "todo_lists",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_todo_lists_UserId_SortOrder",
                table: "todo_lists",
                columns: new[] { "UserId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_todo_lists_IsDefault",
                table: "todo_lists",
                column: "IsDefault");

            // ========== TODOS TABLE ==========
            migrationBuilder.CreateTable(
                name: "todos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Category = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EstimatedMinutes = table.Column<int>(type: "int", nullable: true),
                    ListId = table.Column<int>(type: "int", nullable: true),
                    ArchivedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AutoDeleteAfterDays = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                    LastReminderSent = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ReminderCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ParentTodoId = table.Column<int>(type: "int", nullable: true),
                    RelatedKeywords = table.Column<string>(type: "TEXT", nullable: true),
                    RelatedDocumentId = table.Column<int>(type: "int", nullable: true),
                    RelatedEventId = table.Column<int>(type: "int", nullable: true),
                    RelatedProjectId = table.Column<int>(type: "int", nullable: true),
                    ExtractedFrom = table.Column<string>(type: "TEXT", nullable: true),
                    AiSuggestion = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_todos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_todos_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_todos_todo_lists_ListId",
                        column: x => x.ListId,
                        principalTable: "todo_lists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_todos_todos_ParentTodoId",
                        column: x => x.ParentTodoId,
                        principalTable: "todos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_todos_documents_RelatedDocumentId",
                        column: x => x.RelatedDocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_todos_calendar_events_RelatedEventId",
                        column: x => x.RelatedEventId,
                        principalTable: "calendar_events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_todos_projects_RelatedProjectId",
                        column: x => x.RelatedProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_todos_UserId_Status",
                table: "todos",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_todos_UserId_DueDate",
                table: "todos",
                columns: new[] { "UserId", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_todos_Category",
                table: "todos",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_todos_ListId",
                table: "todos",
                column: "ListId");

            migrationBuilder.CreateIndex(
                name: "IX_todos_ArchivedAt",
                table: "todos",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_todos_Status_ArchivedAt_CompletedAt",
                table: "todos",
                columns: new[] { "Status", "ArchivedAt", "CompletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_todos_ParentTodoId",
                table: "todos",
                column: "ParentTodoId");

            migrationBuilder.CreateIndex(
                name: "IX_todos_RelatedDocumentId",
                table: "todos",
                column: "RelatedDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_todos_RelatedEventId",
                table: "todos",
                column: "RelatedEventId");

            migrationBuilder.CreateIndex(
                name: "IX_todos_RelatedProjectId",
                table: "todos",
                column: "RelatedProjectId");

            // ========== USER_INTERACTIONS TABLE ==========
            migrationBuilder.CreateTable(
                name: "user_interactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    InteractionType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Context = table.Column<string>(type: "TEXT", nullable: false),
                    Question = table.Column<string>(type: "TEXT", nullable: false),
                    SuggestedOptions = table.Column<string>(type: "JSON", nullable: true),
                    UserResponse = table.Column<string>(type: "TEXT", nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    SnoozeUntil = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RelatedDocumentId = table.Column<int>(type: "int", nullable: true),
                    RelatedEventId = table.Column<int>(type: "int", nullable: true),
                    RelatedTodoId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_interactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_interactions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_interactions_documents_RelatedDocumentId",
                        column: x => x.RelatedDocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_user_interactions_calendar_events_RelatedEventId",
                        column: x => x.RelatedEventId,
                        principalTable: "calendar_events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_user_interactions_todos_RelatedTodoId",
                        column: x => x.RelatedTodoId,
                        principalTable: "todos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_interactions_UserId_Status_CreatedAt",
                table: "user_interactions",
                columns: new[] { "UserId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_user_interactions_SnoozeUntil",
                table: "user_interactions",
                column: "SnoozeUntil");

            migrationBuilder.CreateIndex(
                name: "IX_user_interactions_RelatedDocumentId",
                table: "user_interactions",
                column: "RelatedDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_user_interactions_RelatedEventId",
                table: "user_interactions",
                column: "RelatedEventId");

            migrationBuilder.CreateIndex(
                name: "IX_user_interactions_RelatedTodoId",
                table: "user_interactions",
                column: "RelatedTodoId");

            // ========== LEARNING_DEFICITS TABLE ==========
            migrationBuilder.CreateTable(
                name: "learning_deficits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Subject = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Topic = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Subtopic = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    ErrorType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    ErrorDescription = table.Column<string>(type: "TEXT", nullable: false),
                    OccurrenceCount = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    FirstOccurrence = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastOccurrence = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Severity = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    NeedsTutoring = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    RelatedDocumentIds = table.Column<string>(type: "JSON", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_deficits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_learning_deficits_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_learning_deficits_UserId_Subject_Topic",
                table: "learning_deficits",
                columns: new[] { "UserId", "Subject", "Topic" });

            migrationBuilder.CreateIndex(
                name: "IX_learning_deficits_NeedsTutoring",
                table: "learning_deficits",
                column: "NeedsTutoring");

            migrationBuilder.CreateIndex(
                name: "IX_learning_deficits_Severity",
                table: "learning_deficits",
                column: "Severity");

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

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBaseItems_UserId_Subject_Topic",
                table: "KnowledgeBaseItems",
                columns: new[] { "UserId", "Subject", "Topic" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBaseItems_UserId_NextReviewDate",
                table: "KnowledgeBaseItems",
                columns: new[] { "UserId", "NextReviewDate" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBaseItems_UserId_IsActive_NextReviewDate",
                table: "KnowledgeBaseItems",
                columns: new[] { "UserId", "IsActive", "NextReviewDate" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBaseItems_Category",
                table: "KnowledgeBaseItems",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBaseItems_Importance",
                table: "KnowledgeBaseItems",
                column: "Importance");

            // ========== GENERATED_EXERCISES TABLE ==========
            migrationBuilder.CreateTable(
                name: "generated_exercises",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    DeficitId = table.Column<int>(type: "int", nullable: true),
                    Subject = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Topic = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    ExerciseType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Question = table.Column<string>(type: "TEXT", nullable: false),
                    HelpText = table.Column<string>(type: "TEXT", nullable: true),
                    CorrectAnswer = table.Column<string>(type: "TEXT", nullable: false),
                    Explanation = table.Column<string>(type: "TEXT", nullable: true),
                    Difficulty = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    ExerciseMode = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    TimeLimitSeconds = table.Column<int>(type: "int", nullable: true),
                    SubQuestions = table.Column<string>(type: "TEXT", nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MaxAttempts = table.Column<int>(type: "int", nullable: true),
                    UserAnswer = table.Column<string>(type: "TEXT", nullable: true),
                    IsCorrect = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    AnsweredAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    KnowledgeBaseItemId = table.Column<int>(type: "int", nullable: true),
                    IsPeriodicReview = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    NextReviewDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ReviewCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    EaseFactor = table.Column<double>(type: "double", nullable: false, defaultValue: 2.5),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_generated_exercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_generated_exercises_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_generated_exercises_learning_deficits_DeficitId",
                        column: x => x.DeficitId,
                        principalTable: "learning_deficits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_generated_exercises_KnowledgeBaseItems_KnowledgeBaseItemId",
                        column: x => x.KnowledgeBaseItemId,
                        principalTable: "KnowledgeBaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_generated_exercises_UserId_Subject",
                table: "generated_exercises",
                columns: new[] { "UserId", "Subject" });

            migrationBuilder.CreateIndex(
                name: "IX_generated_exercises_NextReviewDate",
                table: "generated_exercises",
                column: "NextReviewDate");

            migrationBuilder.CreateIndex(
                name: "IX_generated_exercises_UserId_IsCorrect",
                table: "generated_exercises",
                columns: new[] { "UserId", "IsCorrect" });

            migrationBuilder.CreateIndex(
                name: "IX_generated_exercises_DeficitId",
                table: "generated_exercises",
                column: "DeficitId");

            migrationBuilder.CreateIndex(
                name: "IX_generated_exercises_KnowledgeBaseItemId",
                table: "generated_exercises",
                column: "KnowledgeBaseItemId");

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

            migrationBuilder.CreateIndex(
                name: "IX_interactive_exercises_UserId_Subject",
                table: "interactive_exercises",
                columns: new[] { "UserId", "Subject" });

            migrationBuilder.CreateIndex(
                name: "IX_interactive_exercises_NextReviewDate",
                table: "interactive_exercises",
                column: "NextReviewDate");

            migrationBuilder.CreateIndex(
                name: "IX_interactive_exercises_UserId_CompletedAt",
                table: "interactive_exercises",
                columns: new[] { "UserId", "CompletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_interactive_exercises_Difficulty",
                table: "interactive_exercises",
                column: "Difficulty");

            migrationBuilder.CreateIndex(
                name: "IX_interactive_exercises_DeficitId",
                table: "interactive_exercises",
                column: "DeficitId");

            migrationBuilder.CreateIndex(
                name: "IX_interactive_exercises_KnowledgeBaseItemId",
                table: "interactive_exercises",
                column: "KnowledgeBaseItemId");

            // ========== EMAILS TABLE ==========
            migrationBuilder.CreateTable(
                name: "Emails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    MessageId = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    Subject = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    FromAddress = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    FromName = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    ToAddresses = table.Column<string>(type: "longtext", nullable: false),
                    CcAddresses = table.Column<string>(type: "longtext", nullable: false),
                    BodyText = table.Column<string>(type: "longtext", nullable: false),
                    BodyHtml = table.Column<string>(type: "longtext", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FetchedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsRead = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    IsImportant = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    HasAttachments = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    Folder = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    IsProcessed = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Summary = table.Column<string>(type: "longtext", nullable: true),
                    Category = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    IsAppointment = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    RequiresUserAction = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    SuggestedAction = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    ConfidenceScore = table.Column<double>(type: "double", nullable: true),
                    ExtractedDates = table.Column<string>(type: "longtext", nullable: true),
                    AnalysisResultJson = table.Column<string>(type: "longtext", nullable: true),
                    ActionStatus = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    ActionTakenAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RelatedCalendarEventId = table.Column<int>(type: "int", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 2),
                    ExtractedData = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Emails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Emails_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Emails_calendar_events_RelatedCalendarEventId",
                        column: x => x.RelatedCalendarEventId,
                        principalTable: "calendar_events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Emails_UserId_MessageId",
                table: "Emails",
                columns: new[] { "UserId", "MessageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Emails_UserId_IsRead_ReceivedAt",
                table: "Emails",
                columns: new[] { "UserId", "IsRead", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Emails_UserId_IsProcessed",
                table: "Emails",
                columns: new[] { "UserId", "IsProcessed" });

            migrationBuilder.CreateIndex(
                name: "IX_Emails_UserId_RequiresUserAction_ActionStatus",
                table: "Emails",
                columns: new[] { "UserId", "RequiresUserAction", "ActionStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Emails_Folder",
                table: "Emails",
                column: "Folder");

            migrationBuilder.CreateIndex(
                name: "IX_Emails_Category",
                table: "Emails",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Emails_RelatedCalendarEventId",
                table: "Emails",
                column: "RelatedCalendarEventId");

            // ========== EMAIL_ATTACHMENTS TABLE ==========
            migrationBuilder.CreateTable(
                name: "EmailAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmailId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    ContentId = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    IsInline = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    DownloadedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RelatedDocumentId = table.Column<int>(type: "int", nullable: true),
                    IsProcessed = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailAttachments_Emails_EmailId",
                        column: x => x.EmailId,
                        principalTable: "Emails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmailAttachments_documents_RelatedDocumentId",
                        column: x => x.RelatedDocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailAttachments_EmailId",
                table: "EmailAttachments",
                column: "EmailId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAttachments_RelatedDocumentId",
                table: "EmailAttachments",
                column: "RelatedDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAttachments_EmailId_IsProcessed",
                table: "EmailAttachments",
                columns: new[] { "EmailId", "IsProcessed" });

            // ========== STAGED_ENTITIES TABLE ==========
            migrationBuilder.CreateTable(
                name: "StagedEntities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    SourceDocumentId = table.Column<int>(type: "int", nullable: true),
                    EntityType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    EntityData = table.Column<string>(type: "longtext", nullable: false),
                    ConfidenceScore = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    IsPromoted = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    PromotedEntityId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    PromotedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UserNotes = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StagedEntities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StagedEntities_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StagedEntities_documents_SourceDocumentId",
                        column: x => x.SourceDocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StagedEntities_UserId_Status_Priority",
                table: "StagedEntities",
                columns: new[] { "UserId", "Status", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_StagedEntities_UserId_EntityType_Status",
                table: "StagedEntities",
                columns: new[] { "UserId", "EntityType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StagedEntities_ConfidenceScore",
                table: "StagedEntities",
                column: "ConfidenceScore");

            migrationBuilder.CreateIndex(
                name: "IX_StagedEntities_Status_CreatedAt",
                table: "StagedEntities",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StagedEntities_IsPromoted_PromotedAt",
                table: "StagedEntities",
                columns: new[] { "IsPromoted", "PromotedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StagedEntities_SourceDocumentId",
                table: "StagedEntities",
                column: "SourceDocumentId");

            // ========== AI_QUESTIONS TABLE ==========
            migrationBuilder.CreateTable(
                name: "AIQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StagedEntityId = table.Column<int>(type: "int", nullable: false),
                    FieldName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    QuestionText = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    SuggestedAnswers = table.Column<string>(type: "longtext", nullable: true),
                    Priority = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    IsAnswered = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    UserAnswer = table.Column<string>(type: "longtext", nullable: true),
                    AnswerType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    ValidationPattern = table.Column<string>(type: "longtext", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AnsweredAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIQuestions_StagedEntities_StagedEntityId",
                        column: x => x.StagedEntityId,
                        principalTable: "StagedEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AIQuestions_StagedEntityId_IsAnswered",
                table: "AIQuestions",
                columns: new[] { "StagedEntityId", "IsAnswered" });

            migrationBuilder.CreateIndex(
                name: "IX_AIQuestions_StagedEntityId_Priority",
                table: "AIQuestions",
                columns: new[] { "StagedEntityId", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_AIQuestions_AnswerType",
                table: "AIQuestions",
                column: "AnswerType");

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

            migrationBuilder.CreateIndex(
                name: "IX_nextcloud_credentials_UserId_IsActive",
                table: "nextcloud_credentials",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_nextcloud_credentials_LastSyncAt",
                table: "nextcloud_credentials",
                column: "LastSyncAt");

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

            migrationBuilder.CreateIndex(
                name: "IX_nextcloud_files_UserId_RemotePath",
                table: "nextcloud_files",
                columns: new[] { "UserId", "RemotePath" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_nextcloud_files_CredentialId_IsDownloaded",
                table: "nextcloud_files",
                columns: new[] { "CredentialId", "IsDownloaded" });

            migrationBuilder.CreateIndex(
                name: "IX_nextcloud_files_ETag",
                table: "nextcloud_files",
                column: "ETag");

            migrationBuilder.CreateIndex(
                name: "IX_nextcloud_files_LocalDocumentId",
                table: "nextcloud_files",
                column: "LocalDocumentId");

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

            migrationBuilder.CreateIndex(
                name: "IX_moodle_courses_UserId_MoodleCourseId",
                table: "moodle_courses",
                columns: new[] { "UserId", "MoodleCourseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_moodle_courses_UserId",
                table: "moodle_courses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_moodle_courses_LastSynced",
                table: "moodle_courses",
                column: "LastSynced");

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

            migrationBuilder.CreateIndex(
                name: "IX_moodle_resources_UserId_CourseId",
                table: "moodle_resources",
                columns: new[] { "UserId", "CourseId" });

            migrationBuilder.CreateIndex(
                name: "IX_moodle_resources_UserId_MoodleResourceId",
                table: "moodle_resources",
                columns: new[] { "UserId", "MoodleResourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_moodle_resources_ResourceType",
                table: "moodle_resources",
                column: "ResourceType");

            migrationBuilder.CreateIndex(
                name: "IX_moodle_resources_IsDownloaded",
                table: "moodle_resources",
                column: "IsDownloaded");

            migrationBuilder.CreateIndex(
                name: "IX_moodle_resources_LocalDocumentId",
                table: "moodle_resources",
                column: "LocalDocumentId");

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

            migrationBuilder.CreateIndex(
                name: "IX_moodle_assignments_UserId_CourseId",
                table: "moodle_assignments",
                columns: new[] { "UserId", "CourseId" });

            migrationBuilder.CreateIndex(
                name: "IX_moodle_assignments_UserId_MoodleAssignmentId",
                table: "moodle_assignments",
                columns: new[] { "UserId", "MoodleAssignmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_moodle_assignments_DueDate",
                table: "moodle_assignments",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_moodle_assignments_UserId_IsSubmitted",
                table: "moodle_assignments",
                columns: new[] { "UserId", "IsSubmitted" });

            migrationBuilder.CreateIndex(
                name: "IX_moodle_assignments_CalendarEventId",
                table: "moodle_assignments",
                column: "CalendarEventId");

            migrationBuilder.CreateIndex(
                name: "IX_moodle_assignments_TodoId",
                table: "moodle_assignments",
                column: "TodoId");

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

            migrationBuilder.CreateIndex(
                name: "IX_moodle_calendar_events_UserId_MoodleEventId",
                table: "moodle_calendar_events",
                columns: new[] { "UserId", "MoodleEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_moodle_calendar_events_UserId",
                table: "moodle_calendar_events",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_moodle_calendar_events_TimeStart",
                table: "moodle_calendar_events",
                column: "TimeStart");

            migrationBuilder.CreateIndex(
                name: "IX_moodle_calendar_events_EventType",
                table: "moodle_calendar_events",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_moodle_calendar_events_CalendarEventId",
                table: "moodle_calendar_events",
                column: "CalendarEventId");

            // ========== JAVA_DOCS_EXERCISES TABLE ==========
            migrationBuilder.CreateTable(
                name: "java_docs_exercises",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FilePath = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    Title = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    Topic = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Subtopic = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    Difficulty = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    ExerciseType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    RawMdxContent = table.Column<string>(type: "LONGTEXT", nullable: false),
                    ParsedContent = table.Column<string>(type: "LONGTEXT", nullable: true),
                    CodeSnippets = table.Column<string>(type: "JSON", nullable: true),
                    SolutionCode = table.Column<string>(type: "LONGTEXT", nullable: true),
                    Tags = table.Column<string>(type: "JSON", nullable: true),
                    Frontmatter = table.Column<string>(type: "JSON", nullable: true),
                    GitCommitHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    HasEmbedding = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    QdrantPointId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    PracticeCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    AverageScore = table.Column<double>(type: "double", nullable: false, defaultValue: 0.0),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_java_docs_exercises", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_java_docs_exercises_FilePath",
                table: "java_docs_exercises",
                column: "FilePath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_java_docs_exercises_Topic",
                table: "java_docs_exercises",
                column: "Topic");

            migrationBuilder.CreateIndex(
                name: "IX_java_docs_exercises_Difficulty",
                table: "java_docs_exercises",
                column: "Difficulty");

            migrationBuilder.CreateIndex(
                name: "IX_java_docs_exercises_ExerciseType",
                table: "java_docs_exercises",
                column: "ExerciseType");

            migrationBuilder.CreateIndex(
                name: "IX_java_docs_exercises_HasEmbedding",
                table: "java_docs_exercises",
                column: "HasEmbedding");

            migrationBuilder.CreateIndex(
                name: "IX_java_docs_exercises_GitCommitHash",
                table: "java_docs_exercises",
                column: "GitCommitHash");

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

            migrationBuilder.CreateIndex(
                name: "IX_document_images_DocumentId",
                table: "document_images",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_document_images_DocumentId_PageNumber_ImageIndex",
                table: "document_images",
                columns: new[] { "DocumentId", "PageNumber", "ImageIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_document_images_IsProcessed",
                table: "document_images",
                column: "IsProcessed");

            migrationBuilder.CreateIndex(
                name: "IX_document_images_ImageType",
                table: "document_images",
                column: "ImageType");

            migrationBuilder.CreateIndex(
                name: "IX_document_images_HasEmbedding",
                table: "document_images",
                column: "HasEmbedding");

            // ========== DOCUMENT_CHUNKS TABLE ==========
            migrationBuilder.CreateTable(
                name: "document_chunks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    ContentLength = table.Column<int>(type: "int", nullable: false),
                    ChunkIndex = table.Column<int>(type: "int", nullable: false),
                    TotalChunks = table.Column<int>(type: "int", nullable: false),
                    StartPosition = table.Column<int>(type: "int", nullable: false),
                    EndPosition = table.Column<int>(type: "int", nullable: false),
                    PageNumbers = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    TopicLabel = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    Summary = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    SectionHeading = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true),
                    ChunkType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    PreviousChunkSimilarity = table.Column<double>(type: "double", nullable: true),
                    HasEmbedding = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    QdrantPointId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    HasBeenLinked = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    HasEventLinks = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    HasKnowledgeLinks = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    HasExerciseLinks = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    LastLinkGenerationAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_chunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_document_chunks_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_document_chunks_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_document_chunks_DocumentId",
                table: "document_chunks",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_document_chunks_UserId",
                table: "document_chunks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_document_chunks_DocumentId_ChunkIndex",
                table: "document_chunks",
                columns: new[] { "DocumentId", "ChunkIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_document_chunks_TopicLabel",
                table: "document_chunks",
                column: "TopicLabel");

            migrationBuilder.CreateIndex(
                name: "IX_document_chunks_HasEmbedding",
                table: "document_chunks",
                column: "HasEmbedding");

            migrationBuilder.CreateIndex(
                name: "IX_document_chunks_Status",
                table: "document_chunks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_document_chunks_HasBeenLinked_HasEmbedding",
                table: "document_chunks",
                columns: new[] { "HasBeenLinked", "HasEmbedding" });

            migrationBuilder.CreateIndex(
                name: "IX_document_chunks_HasEventLinks_HasKnowledgeLinks_HasExerciseLinks",
                table: "document_chunks",
                columns: new[] { "HasEventLinks", "HasKnowledgeLinks", "HasExerciseLinks" });

            // ========== KNOWLEDGE_LINKS TABLE ==========
            migrationBuilder.CreateTable(
                name: "knowledge_links",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    SourceType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    SourceId = table.Column<int>(type: "int", nullable: false),
                    TargetType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    TargetId = table.Column<int>(type: "int", nullable: false),
                    LinkType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Strength = table.Column<double>(type: "double", nullable: false, defaultValue: 1.0),
                    IsAutoGenerated = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    VectorSimilarity = table.Column<double>(type: "double", nullable: true),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    IsBidirectional = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    IsConfirmed = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    IsRejected = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_knowledge_links", x => x.Id);
                    table.ForeignKey(
                        name: "FK_knowledge_links_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_links_SourceType_SourceId",
                table: "knowledge_links",
                columns: new[] { "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_links_TargetType_TargetId",
                table: "knowledge_links",
                columns: new[] { "TargetType", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_links_SourceType_SourceId_TargetType_TargetId",
                table: "knowledge_links",
                columns: new[] { "SourceType", "SourceId", "TargetType", "TargetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_links_LinkType",
                table: "knowledge_links",
                column: "LinkType");

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_links_IsAutoGenerated",
                table: "knowledge_links",
                column: "IsAutoGenerated");

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_links_UserId_IsRejected",
                table: "knowledge_links",
                columns: new[] { "UserId", "IsRejected" });

            // ========== CONTENT_TAGS TABLE ==========
            migrationBuilder.CreateTable(
                name: "content_tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Color = table.Column<string>(type: "varchar(7)", maxLength: 7, nullable: true),
                    Icon = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsSystem = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_content_tags_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_content_tags_UserId_Name",
                table: "content_tags",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_content_tags_UserId_SortOrder",
                table: "content_tags",
                columns: new[] { "UserId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_content_tags_IsSystem",
                table: "content_tags",
                column: "IsSystem");

            // ========== CONTENT_TAG_ASSIGNMENTS TABLE ==========
            migrationBuilder.CreateTable(
                name: "content_tag_assignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TagId = table.Column<int>(type: "int", nullable: false),
                    EntityType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AssignedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsAutoAssigned = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_tag_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_content_tag_assignments_content_tags_TagId",
                        column: x => x.TagId,
                        principalTable: "content_tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_content_tag_assignments_users_AssignedByUserId",
                        column: x => x.AssignedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_content_tag_assignments_TagId_EntityType_EntityId",
                table: "content_tag_assignments",
                columns: new[] { "TagId", "EntityType", "EntityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_content_tag_assignments_EntityType_EntityId",
                table: "content_tag_assignments",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_content_tag_assignments_IsAutoAssigned",
                table: "content_tag_assignments",
                column: "IsAutoAssigned");

            migrationBuilder.CreateIndex(
                name: "IX_content_tag_assignments_AssignedByUserId",
                table: "content_tag_assignments",
                column: "AssignedByUserId");

            // ========== QDRANT_EMBEDDINGS TABLE ==========
            migrationBuilder.CreateTable(
                name: "qdrant_embeddings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    EntityType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    QdrantPointId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    CollectionName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    EmbeddingModel = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    VectorDimension = table.Column<int>(type: "int", nullable: false, defaultValue: 1536),
                    EmbeddedTextPreview = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    FullTextLength = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_qdrant_embeddings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_qdrant_embeddings_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_qdrant_embeddings_EntityType_EntityId",
                table: "qdrant_embeddings",
                columns: new[] { "EntityType", "EntityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_qdrant_embeddings_QdrantPointId",
                table: "qdrant_embeddings",
                column: "QdrantPointId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_qdrant_embeddings_CollectionName",
                table: "qdrant_embeddings",
                column: "CollectionName");

            migrationBuilder.CreateIndex(
                name: "IX_qdrant_embeddings_UserId",
                table: "qdrant_embeddings",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop tables in reverse order (respecting foreign key dependencies)
            migrationBuilder.DropTable(name: "qdrant_embeddings");
            migrationBuilder.DropTable(name: "content_tag_assignments");
            migrationBuilder.DropTable(name: "content_tags");
            migrationBuilder.DropTable(name: "knowledge_links");
            migrationBuilder.DropTable(name: "document_chunks");
            migrationBuilder.DropTable(name: "document_images");
            migrationBuilder.DropTable(name: "java_docs_exercises");
            migrationBuilder.DropTable(name: "moodle_calendar_events");
            migrationBuilder.DropTable(name: "moodle_assignments");
            migrationBuilder.DropTable(name: "moodle_resources");
            migrationBuilder.DropTable(name: "moodle_courses");
            migrationBuilder.DropTable(name: "nextcloud_files");
            migrationBuilder.DropTable(name: "nextcloud_credentials");
            migrationBuilder.DropTable(name: "AIQuestions");
            migrationBuilder.DropTable(name: "StagedEntities");
            migrationBuilder.DropTable(name: "EmailAttachments");
            migrationBuilder.DropTable(name: "Emails");
            migrationBuilder.DropTable(name: "interactive_exercises");
            migrationBuilder.DropTable(name: "generated_exercises");
            migrationBuilder.DropTable(name: "KnowledgeBaseItems");
            migrationBuilder.DropTable(name: "learning_deficits");
            migrationBuilder.DropTable(name: "user_interactions");
            migrationBuilder.DropTable(name: "todos");
            migrationBuilder.DropTable(name: "todo_lists");
            migrationBuilder.DropTable(name: "courses");
            migrationBuilder.DropTable(name: "reminders");
            migrationBuilder.DropTable(name: "calendar_events");
            migrationBuilder.DropTable(name: "documents");
            migrationBuilder.DropTable(name: "projects");
            migrationBuilder.DropTable(name: "users");
        }
    }
}
