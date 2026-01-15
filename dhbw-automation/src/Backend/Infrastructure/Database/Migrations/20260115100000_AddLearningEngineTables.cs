using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace DHBWAutomation.Backend.Infrastructure.Database.Migrations
{
    /// <summary>
    /// Adds Learning Engine (DeepTutor-style) tables:
    /// - kg_entities: Knowledge Graph entities
    /// - kg_relationships: Relationships between entities
    /// - user_entity_performance: User performance per entity
    /// - unified_knowledge_entities: Unified knowledge entities
    /// - unified_knowledge_relationships: Unified relationships
    /// - unified_learning_priorities: Unified learning priorities
    /// </summary>
    public partial class AddLearningEngineTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ========== KG_ENTITIES TABLE ==========
            migrationBuilder.CreateTable(
                name: "kg_entities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    DocumentId = table.Column<int>(type: "int", nullable: true),
                    ChunkId = table.Column<int>(type: "int", nullable: true),
                    EntityType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    NormalizedName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Subject = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Topic = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    ConfidenceScore = table.Column<double>(type: "double", nullable: false, defaultValue: 1.0),
                    OccurrenceCount = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    ImportanceScore = table.Column<double>(type: "double", nullable: false, defaultValue: 0.5),
                    HasEmbedding = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    QdrantPointId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Metadata = table.Column<string>(type: "JSON", nullable: true),
                    IsVerified = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kg_entities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_kg_entities_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_kg_entities_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_kg_entities_document_chunks_ChunkId",
                        column: x => x.ChunkId,
                        principalTable: "document_chunks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(name: "IX_kg_entities_UserId_NormalizedName", table: "kg_entities", columns: new[] { "UserId", "NormalizedName" });
            migrationBuilder.CreateIndex(name: "IX_kg_entities_UserId_EntityType", table: "kg_entities", columns: new[] { "UserId", "EntityType" });
            migrationBuilder.CreateIndex(name: "IX_kg_entities_DocumentId_ChunkId", table: "kg_entities", columns: new[] { "DocumentId", "ChunkId" });
            migrationBuilder.CreateIndex(name: "IX_kg_entities_Subject", table: "kg_entities", column: "Subject");
            migrationBuilder.CreateIndex(name: "IX_kg_entities_Topic", table: "kg_entities", column: "Topic");
            migrationBuilder.CreateIndex(name: "IX_kg_entities_HasEmbedding", table: "kg_entities", column: "HasEmbedding");
            migrationBuilder.CreateIndex(name: "IX_kg_entities_IsActive", table: "kg_entities", column: "IsActive");
            migrationBuilder.CreateIndex(name: "IX_kg_entities_ImportanceScore", table: "kg_entities", column: "ImportanceScore");

            // ========== KG_RELATIONSHIPS TABLE ==========
            migrationBuilder.CreateTable(
                name: "kg_relationships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    SourceEntityId = table.Column<int>(type: "int", nullable: false),
                    TargetEntityId = table.Column<int>(type: "int", nullable: false),
                    RelationshipType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Strength = table.Column<double>(type: "double", nullable: false, defaultValue: 1.0),
                    ExtractedFromChunkId = table.Column<int>(type: "int", nullable: true),
                    ExtractedFromDocumentId = table.Column<int>(type: "int", nullable: true),
                    Evidence = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    IsAutoExtracted = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    IsVerified = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    IsBidirectional = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kg_relationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_kg_relationships_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_kg_relationships_kg_entities_SourceEntityId",
                        column: x => x.SourceEntityId,
                        principalTable: "kg_entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_kg_relationships_kg_entities_TargetEntityId",
                        column: x => x.TargetEntityId,
                        principalTable: "kg_entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_kg_relationships_document_chunks_ExtractedFromChunkId",
                        column: x => x.ExtractedFromChunkId,
                        principalTable: "document_chunks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_kg_relationships_documents_ExtractedFromDocumentId",
                        column: x => x.ExtractedFromDocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(name: "IX_kg_relationships_SourceEntityId_TargetEntityId_RelationshipType", table: "kg_relationships", columns: new[] { "SourceEntityId", "TargetEntityId", "RelationshipType" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_kg_relationships_RelationshipType", table: "kg_relationships", column: "RelationshipType");
            migrationBuilder.CreateIndex(name: "IX_kg_relationships_ExtractedFromChunkId", table: "kg_relationships", column: "ExtractedFromChunkId");
            migrationBuilder.CreateIndex(name: "IX_kg_relationships_IsActive", table: "kg_relationships", column: "IsActive");
            migrationBuilder.CreateIndex(name: "IX_kg_relationships_Strength", table: "kg_relationships", column: "Strength");

            // ========== USER_ENTITY_PERFORMANCE TABLE ==========
            migrationBuilder.CreateTable(
                name: "user_entity_performance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    QuestionType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    BloomLevel = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Attempts = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Correct = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastAttempt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    MasteryScore = table.Column<double>(type: "double", nullable: false, defaultValue: 0.0),
                    NextReview = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Stability = table.Column<double>(type: "double", nullable: false, defaultValue: 0.0),
                    Difficulty = table.Column<double>(type: "double", nullable: false, defaultValue: 0.5),
                    ElapsedDays = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ScheduledDays = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Reps = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Lapses = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    State = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    AverageResponseTime = table.Column<double>(type: "double", nullable: true),
                    CurrentStreak = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    BestStreak = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_entity_performance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_entity_performance_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_entity_performance_kg_entities_EntityId",
                        column: x => x.EntityId,
                        principalTable: "kg_entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_user_entity_performance_UserId_EntityId_QuestionType_BloomLevel", table: "user_entity_performance", columns: new[] { "UserId", "EntityId", "QuestionType", "BloomLevel" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_user_entity_performance_UserId_NextReview", table: "user_entity_performance", columns: new[] { "UserId", "NextReview" });
            migrationBuilder.CreateIndex(name: "IX_user_entity_performance_MasteryScore", table: "user_entity_performance", column: "MasteryScore");
            migrationBuilder.CreateIndex(name: "IX_user_entity_performance_State", table: "user_entity_performance", column: "State");

            // ========== UNIFIED_KNOWLEDGE_ENTITIES TABLE ==========
            migrationBuilder.CreateTable(
                name: "unified_knowledge_entities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    EntityType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    NormalizedName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Subject = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Topic = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Subtopic = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    ConfidenceScore = table.Column<double>(type: "double", nullable: false, defaultValue: 1.0),
                    ImportanceScore = table.Column<double>(type: "double", nullable: false, defaultValue: 0.5),
                    OccurrenceCount = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    SourceDocumentId = table.Column<int>(type: "int", nullable: true),
                    SourceChunkId = table.Column<int>(type: "int", nullable: true),
                    Metadata = table.Column<string>(type: "JSON", nullable: true),
                    IsVerified = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    HasEmbedding = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    QdrantPointId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Stability = table.Column<double>(type: "double", nullable: false, defaultValue: 0.0),
                    Difficulty = table.Column<double>(type: "double", nullable: false, defaultValue: 0.5),
                    ElapsedDays = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ScheduledDays = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Reps = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Lapses = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    FsrsState = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    NextReview = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    BaseStrength = table.Column<double>(type: "double", nullable: false, defaultValue: 1.0),
                    DecayRate = table.Column<double>(type: "double", nullable: false, defaultValue: 0.05),
                    LastInteraction = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EasyCorrect = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    EasyTotal = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MediumCorrect = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MediumTotal = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    HardCorrect = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    HardTotal = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CurrentBloomLevel = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    BloomPerformanceJson = table.Column<string>(type: "JSON", nullable: true),
                    TotalAttempts = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TotalCorrect = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    AverageResponseTimeSeconds = table.Column<double>(type: "double", nullable: false, defaultValue: 0.0),
                    CurrentStreak = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    BestStreak = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MasteryScore = table.Column<double>(type: "double", nullable: false, defaultValue: 0.0),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unified_knowledge_entities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_unified_knowledge_entities_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_unified_knowledge_entities_documents_SourceDocumentId",
                        column: x => x.SourceDocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_unified_knowledge_entities_document_chunks_SourceChunkId",
                        column: x => x.SourceChunkId,
                        principalTable: "document_chunks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(name: "IX_unified_knowledge_entities_UserId_NormalizedName_Subject", table: "unified_knowledge_entities", columns: new[] { "UserId", "NormalizedName", "Subject" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_unified_knowledge_entities_UserId_Subject", table: "unified_knowledge_entities", columns: new[] { "UserId", "Subject" });
            migrationBuilder.CreateIndex(name: "IX_unified_knowledge_entities_UserId_EntityType", table: "unified_knowledge_entities", columns: new[] { "UserId", "EntityType" });
            migrationBuilder.CreateIndex(name: "IX_unified_knowledge_entities_MasteryScore", table: "unified_knowledge_entities", column: "MasteryScore");
            migrationBuilder.CreateIndex(name: "IX_unified_knowledge_entities_NextReview", table: "unified_knowledge_entities", column: "NextReview");
            migrationBuilder.CreateIndex(name: "IX_unified_knowledge_entities_FsrsState", table: "unified_knowledge_entities", column: "FsrsState");
            migrationBuilder.CreateIndex(name: "IX_unified_knowledge_entities_LastInteraction", table: "unified_knowledge_entities", column: "LastInteraction");
            migrationBuilder.CreateIndex(name: "IX_unified_knowledge_entities_HasEmbedding", table: "unified_knowledge_entities", column: "HasEmbedding");
            migrationBuilder.CreateIndex(name: "IX_unified_knowledge_entities_IsActive", table: "unified_knowledge_entities", column: "IsActive");
            migrationBuilder.CreateIndex(name: "IX_unified_knowledge_entities_ImportanceScore", table: "unified_knowledge_entities", column: "ImportanceScore");

            // ========== UNIFIED_KNOWLEDGE_RELATIONSHIPS TABLE ==========
            migrationBuilder.CreateTable(
                name: "unified_knowledge_relationships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    SourceEntityId = table.Column<int>(type: "int", nullable: false),
                    TargetEntityId = table.Column<int>(type: "int", nullable: false),
                    RelationshipType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Evidence = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    ExtractedFromChunkId = table.Column<int>(type: "int", nullable: true),
                    ExtractedFromDocumentId = table.Column<int>(type: "int", nullable: true),
                    IsAutoExtracted = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    IsVerified = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    IsBidirectional = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    InitialStrength = table.Column<double>(type: "double", nullable: false, defaultValue: 1.0),
                    DecayRate = table.Column<double>(type: "double", nullable: false, defaultValue: 0.03),
                    LastReinforced = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ReinforcementCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    WeakeningCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    RequiredMasteryLevel = table.Column<double>(type: "double", nullable: false, defaultValue: 0.6),
                    IsStrict = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    ConfidenceScore = table.Column<double>(type: "double", nullable: false, defaultValue: 1.0),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unified_knowledge_relationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_unified_knowledge_relationships_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_unified_knowledge_relationships_unified_knowledge_entities_SourceEntityId",
                        column: x => x.SourceEntityId,
                        principalTable: "unified_knowledge_entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_unified_knowledge_relationships_unified_knowledge_entities_TargetEntityId",
                        column: x => x.TargetEntityId,
                        principalTable: "unified_knowledge_entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_unified_knowledge_relationships_document_chunks_ExtractedFromChunkId",
                        column: x => x.ExtractedFromChunkId,
                        principalTable: "document_chunks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_unified_knowledge_relationships_documents_ExtractedFromDocumentId",
                        column: x => x.ExtractedFromDocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(name: "IX_unified_knowledge_relationships_UserId_SourceEntityId_TargetEntityId_RelationshipType", table: "unified_knowledge_relationships", columns: new[] { "UserId", "SourceEntityId", "TargetEntityId", "RelationshipType" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_unified_knowledge_relationships_RelationshipType", table: "unified_knowledge_relationships", column: "RelationshipType");
            migrationBuilder.CreateIndex(name: "IX_unified_knowledge_relationships_IsActive", table: "unified_knowledge_relationships", column: "IsActive");
            migrationBuilder.CreateIndex(name: "IX_unified_knowledge_relationships_ExtractedFromChunkId", table: "unified_knowledge_relationships", column: "ExtractedFromChunkId");

            // ========== UNIFIED_LEARNING_PRIORITIES TABLE ==========
            migrationBuilder.CreateTable(
                name: "unified_learning_priorities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    UnifiedEntityId = table.Column<int>(type: "int", nullable: true),
                    MoodleAssignmentId = table.Column<int>(type: "int", nullable: true),
                    CalendarEventId = table.Column<int>(type: "int", nullable: true),
                    DeadlineUrgency = table.Column<double>(type: "double", nullable: false, defaultValue: 0.0),
                    TopicRelevance = table.Column<double>(type: "double", nullable: false, defaultValue: 0.0),
                    MasteryGap = table.Column<double>(type: "double", nullable: false, defaultValue: 0.0),
                    DecayAmount = table.Column<double>(type: "double", nullable: false, defaultValue: 0.0),
                    CompositeScore = table.Column<double>(type: "double", nullable: false, defaultValue: 0.0),
                    CurrentBloomLevel = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    TargetBloomLevel = table.Column<int>(type: "int", nullable: false, defaultValue: 3),
                    BloomGap = table.Column<double>(type: "double", nullable: false, defaultValue: 0.0),
                    IsBlocked = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    BlockReason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    BlockingPrerequisitesJson = table.Column<string>(type: "JSON", nullable: true),
                    Subject = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Topic = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    EntityName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    Deadline = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RelatedEventName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    CalculatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    Rank = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unified_learning_priorities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_unified_learning_priorities_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_unified_learning_priorities_unified_knowledge_entities_UnifiedEntityId",
                        column: x => x.UnifiedEntityId,
                        principalTable: "unified_knowledge_entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_unified_learning_priorities_moodle_assignments_MoodleAssignmentId",
                        column: x => x.MoodleAssignmentId,
                        principalTable: "moodle_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_unified_learning_priorities_calendar_events_CalendarEventId",
                        column: x => x.CalendarEventId,
                        principalTable: "calendar_events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(name: "IX_unified_learning_priorities_UserId_CompositeScore", table: "unified_learning_priorities", columns: new[] { "UserId", "CompositeScore" });
            migrationBuilder.CreateIndex(name: "IX_unified_learning_priorities_UserId_Rank", table: "unified_learning_priorities", columns: new[] { "UserId", "Rank" });
            migrationBuilder.CreateIndex(name: "IX_unified_learning_priorities_Deadline", table: "unified_learning_priorities", column: "Deadline");
            migrationBuilder.CreateIndex(name: "IX_unified_learning_priorities_IsActive", table: "unified_learning_priorities", column: "IsActive");
            migrationBuilder.CreateIndex(name: "IX_unified_learning_priorities_IsBlocked", table: "unified_learning_priorities", column: "IsBlocked");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop tables in reverse order of dependencies
            migrationBuilder.DropTable(name: "unified_learning_priorities");
            migrationBuilder.DropTable(name: "unified_knowledge_relationships");
            migrationBuilder.DropTable(name: "unified_knowledge_entities");
            migrationBuilder.DropTable(name: "user_entity_performance");
            migrationBuilder.DropTable(name: "kg_relationships");
            migrationBuilder.DropTable(name: "kg_entities");
        }
    }
}
