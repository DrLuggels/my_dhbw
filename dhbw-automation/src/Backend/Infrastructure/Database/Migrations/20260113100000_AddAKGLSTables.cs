using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace DHBWAutomation.Backend.Infrastructure.Database.Migrations
{
    /// <summary>
    /// Adds AKGLS (Adaptive Knowledge Graph Learning System) tables:
    /// - user_knowledge_nodes: Individual knowledge nodes per user/topic
    /// - user_knowledge_edges: Connections between knowledge nodes
    /// - user_decay_profiles: Personalized decay rates per user/subject
    /// - learning_streaks: Daily learning streaks for gamification
    /// - learning_priorities: Calculated learning priorities per node
    /// - exam_simulations: Timed exam simulations
    /// - prerequisite_chains: Prerequisite relationships between nodes
    /// </summary>
    public partial class AddAKGLSTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ========== USER_KNOWLEDGE_NODES TABLE ==========
            migrationBuilder.CreateTable(
                name: "user_knowledge_nodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Subject = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Topic = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Subtopic = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    MasteryLevel = table.Column<double>(type: "double", nullable: false, defaultValue: 0.0),
                    BaseStrength = table.Column<double>(type: "double", nullable: false, defaultValue: 0.5),
                    DecayRate = table.Column<double>(type: "double", nullable: false, defaultValue: 0.05),
                    LastInteraction = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TotalExercises = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CorrectExercises = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    EasyTotal = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    EasyCorrect = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MediumTotal = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MediumCorrect = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    HardTotal = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    HardCorrect = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_knowledge_nodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_knowledge_nodes_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_user_knowledge_nodes_UserId_Subject_Topic", table: "user_knowledge_nodes", columns: new[] { "UserId", "Subject", "Topic" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_user_knowledge_nodes_UserId_Subject", table: "user_knowledge_nodes", columns: new[] { "UserId", "Subject" });
            migrationBuilder.CreateIndex(name: "IX_user_knowledge_nodes_MasteryLevel", table: "user_knowledge_nodes", column: "MasteryLevel");
            migrationBuilder.CreateIndex(name: "IX_user_knowledge_nodes_LastInteraction", table: "user_knowledge_nodes", column: "LastInteraction");

            // ========== USER_KNOWLEDGE_EDGES TABLE ==========
            migrationBuilder.CreateTable(
                name: "user_knowledge_edges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    SourceNodeId = table.Column<int>(type: "int", nullable: false),
                    TargetNodeId = table.Column<int>(type: "int", nullable: false),
                    EdgeType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Strength = table.Column<double>(type: "double", nullable: false, defaultValue: 0.5),
                    DecayRate = table.Column<double>(type: "double", nullable: false, defaultValue: 0.03),
                    LastReinforced = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ReinforcementCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    WeakeningCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsBidirectional = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_knowledge_edges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_knowledge_edges_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_knowledge_edges_user_knowledge_nodes_SourceNodeId",
                        column: x => x.SourceNodeId,
                        principalTable: "user_knowledge_nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_knowledge_edges_user_knowledge_nodes_TargetNodeId",
                        column: x => x.TargetNodeId,
                        principalTable: "user_knowledge_nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_user_knowledge_edges_UserId_SourceNodeId_TargetNodeId", table: "user_knowledge_edges", columns: new[] { "UserId", "SourceNodeId", "TargetNodeId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_user_knowledge_edges_UserId", table: "user_knowledge_edges", column: "UserId");
            migrationBuilder.CreateIndex(name: "IX_user_knowledge_edges_EdgeType", table: "user_knowledge_edges", column: "EdgeType");
            migrationBuilder.CreateIndex(name: "IX_user_knowledge_edges_SourceNodeId", table: "user_knowledge_edges", column: "SourceNodeId");
            migrationBuilder.CreateIndex(name: "IX_user_knowledge_edges_TargetNodeId", table: "user_knowledge_edges", column: "TargetNodeId");

            // ========== USER_DECAY_PROFILES TABLE ==========
            migrationBuilder.CreateTable(
                name: "user_decay_profiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Subject = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    LearnedDecayRate = table.Column<double>(type: "double", nullable: false, defaultValue: 0.05),
                    Confidence = table.Column<double>(type: "double", nullable: false, defaultValue: 0.0),
                    PerformanceHistory = table.Column<string>(type: "JSON", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_decay_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_decay_profiles_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_user_decay_profiles_UserId_Subject", table: "user_decay_profiles", columns: new[] { "UserId", "Subject" }, unique: true);

            // ========== LEARNING_STREAKS TABLE ==========
            migrationBuilder.CreateTable(
                name: "learning_streaks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CurrentStreak = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LongestStreak = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastActivityDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    StreakStartDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TotalDaysActive = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TotalExercisesCompleted = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    FreezesAvailable = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    FreezesUsedThisWeek = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastFreezeUsed = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    WeekStartDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_streaks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_learning_streaks_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_learning_streaks_UserId", table: "learning_streaks", column: "UserId", unique: true);
            migrationBuilder.CreateIndex(name: "IX_learning_streaks_LastActivityDate", table: "learning_streaks", column: "LastActivityDate");

            // ========== LEARNING_PRIORITIES TABLE ==========
            migrationBuilder.CreateTable(
                name: "learning_priorities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    UserKnowledgeNodeId = table.Column<int>(type: "int", nullable: false),
                    MoodleAssignmentId = table.Column<int>(type: "int", nullable: true),
                    CalendarEventId = table.Column<int>(type: "int", nullable: true),
                    Deadline = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeadlineUrgencyScore = table.Column<double>(type: "double", nullable: false, defaultValue: 0.0),
                    TopicRelevanceScore = table.Column<double>(type: "double", nullable: false, defaultValue: 0.0),
                    MasteryGapScore = table.Column<double>(type: "double", nullable: false, defaultValue: 0.0),
                    DecayAmountScore = table.Column<double>(type: "double", nullable: false, defaultValue: 0.0),
                    CompositeScore = table.Column<double>(type: "double", nullable: false, defaultValue: 0.0),
                    Reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    LastCalculated = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_priorities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_learning_priorities_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_learning_priorities_user_knowledge_nodes_UserKnowledgeNodeId",
                        column: x => x.UserKnowledgeNodeId,
                        principalTable: "user_knowledge_nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_learning_priorities_moodle_assignments_MoodleAssignmentId",
                        column: x => x.MoodleAssignmentId,
                        principalTable: "moodle_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_learning_priorities_calendar_events_CalendarEventId",
                        column: x => x.CalendarEventId,
                        principalTable: "calendar_events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(name: "IX_learning_priorities_UserId_CompositeScore", table: "learning_priorities", columns: new[] { "UserId", "CompositeScore" });
            migrationBuilder.CreateIndex(name: "IX_learning_priorities_Deadline", table: "learning_priorities", column: "Deadline");
            migrationBuilder.CreateIndex(name: "IX_learning_priorities_UserKnowledgeNodeId", table: "learning_priorities", column: "UserKnowledgeNodeId");
            migrationBuilder.CreateIndex(name: "IX_learning_priorities_MoodleAssignmentId", table: "learning_priorities", column: "MoodleAssignmentId");
            migrationBuilder.CreateIndex(name: "IX_learning_priorities_CalendarEventId", table: "learning_priorities", column: "CalendarEventId");

            // ========== EXAM_SIMULATIONS TABLE ==========
            migrationBuilder.CreateTable(
                name: "exam_simulations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Subject = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    MoodleAssignmentId = table.Column<int>(type: "int", nullable: true),
                    TotalQuestions = table.Column<int>(type: "int", nullable: false, defaultValue: 20),
                    EasyQuestions = table.Column<int>(type: "int", nullable: false, defaultValue: 4),
                    MediumQuestions = table.Column<int>(type: "int", nullable: false, defaultValue: 8),
                    HardQuestions = table.Column<int>(type: "int", nullable: false, defaultValue: 8),
                    TimeLimitMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 60),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    CurrentQuestionIndex = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CorrectAnswers = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    SkippedQuestions = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    QuestionIds = table.Column<string>(type: "JSON", nullable: true),
                    UserAnswers = table.Column<string>(type: "JSON", nullable: true),
                    Score = table.Column<double>(type: "double", nullable: true),
                    AiFeedback = table.Column<string>(type: "TEXT", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exam_simulations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_exam_simulations_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_exam_simulations_moodle_assignments_MoodleAssignmentId",
                        column: x => x.MoodleAssignmentId,
                        principalTable: "moodle_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(name: "IX_exam_simulations_UserId_Subject", table: "exam_simulations", columns: new[] { "UserId", "Subject" });
            migrationBuilder.CreateIndex(name: "IX_exam_simulations_StartedAt", table: "exam_simulations", column: "StartedAt");
            migrationBuilder.CreateIndex(name: "IX_exam_simulations_CompletedAt", table: "exam_simulations", column: "CompletedAt");
            migrationBuilder.CreateIndex(name: "IX_exam_simulations_MoodleAssignmentId", table: "exam_simulations", column: "MoodleAssignmentId");

            // ========== PREREQUISITE_CHAINS TABLE ==========
            migrationBuilder.CreateTable(
                name: "prerequisite_chains",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PrerequisiteNodeId = table.Column<int>(type: "int", nullable: false),
                    DependentNodeId = table.Column<int>(type: "int", nullable: false),
                    RequiredMasteryLevel = table.Column<double>(type: "double", nullable: false, defaultValue: 0.6),
                    IsStrict = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    IsAutoGenerated = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    Confidence = table.Column<double>(type: "double", nullable: false, defaultValue: 1.0),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prerequisite_chains", x => x.Id);
                    table.ForeignKey(
                        name: "FK_prerequisite_chains_user_knowledge_nodes_PrerequisiteNodeId",
                        column: x => x.PrerequisiteNodeId,
                        principalTable: "user_knowledge_nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_prerequisite_chains_user_knowledge_nodes_DependentNodeId",
                        column: x => x.DependentNodeId,
                        principalTable: "user_knowledge_nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_prerequisite_chains_PrerequisiteNodeId_DependentNodeId", table: "prerequisite_chains", columns: new[] { "PrerequisiteNodeId", "DependentNodeId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_prerequisite_chains_PrerequisiteNodeId", table: "prerequisite_chains", column: "PrerequisiteNodeId");
            migrationBuilder.CreateIndex(name: "IX_prerequisite_chains_DependentNodeId", table: "prerequisite_chains", column: "DependentNodeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop tables in reverse order of dependencies
            migrationBuilder.DropTable(name: "prerequisite_chains");
            migrationBuilder.DropTable(name: "exam_simulations");
            migrationBuilder.DropTable(name: "learning_priorities");
            migrationBuilder.DropTable(name: "learning_streaks");
            migrationBuilder.DropTable(name: "user_decay_profiles");
            migrationBuilder.DropTable(name: "user_knowledge_edges");
            migrationBuilder.DropTable(name: "user_knowledge_nodes");
        }
    }
}
