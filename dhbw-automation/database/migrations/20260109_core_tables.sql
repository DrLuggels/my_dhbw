-- =========================================================================
-- DHBW Automation - Core Tables
-- Migration: 20260109_core_tables
-- Creates user_interactions, todos, and learning_deficits tables
-- =========================================================================

USE dhbw_automation;

-- -------------------------------------------------------------------------
-- 1. Todos Table
-- -------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS todos (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    Title VARCHAR(200) NOT NULL,
    Description TEXT NULL,
    Category VARCHAR(50) NOT NULL DEFAULT 'general',
    Priority VARCHAR(50) NOT NULL DEFAULT 'medium',
    Status VARCHAR(50) NOT NULL DEFAULT 'pending',
    DueDate DATETIME(6) NULL,
    EstimatedMinutes INT NULL,
    RelatedDocumentId INT NULL,
    RelatedEventId INT NULL,
    RelatedProjectId INT NULL,
    ExtractedFrom TEXT NULL,
    AiSuggestion TEXT NULL,
    CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    CompletedAt DATETIME(6) NULL,
    
    CONSTRAINT FK_Todos_Users FOREIGN KEY (UserId) 
        REFERENCES users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Todos_Documents FOREIGN KEY (RelatedDocumentId) 
        REFERENCES documents(Id) ON DELETE SET NULL,
    CONSTRAINT FK_Todos_CalendarEvents FOREIGN KEY (RelatedEventId) 
        REFERENCES calendar_events(Id) ON DELETE SET NULL,
    
    INDEX IX_Todos_UserId (UserId),
    INDEX IX_Todos_Status (Status),
    INDEX IX_Todos_DueDate (DueDate),
    INDEX IX_Todos_Category (Category),
    INDEX IX_Todos_Priority (Priority)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -------------------------------------------------------------------------
-- 2. User Interactions Table
-- -------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS user_interactions (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    InteractionType VARCHAR(100) NOT NULL,
    Context TEXT NOT NULL,
    Question TEXT NOT NULL,
    SuggestedOptions JSON NULL,
    UserResponse TEXT NULL,
    RespondedAt DATETIME(6) NULL,
    Status VARCHAR(50) NOT NULL DEFAULT 'pending',
    SnoozeUntil DATETIME(6) NULL,
    RelatedDocumentId INT NULL,
    RelatedEventId INT NULL,
    RelatedTodoId INT NULL,
    CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    UpdatedAt DATETIME(6) NULL,
    
    CONSTRAINT FK_UserInteractions_Users FOREIGN KEY (UserId) 
        REFERENCES users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserInteractions_Documents FOREIGN KEY (RelatedDocumentId) 
        REFERENCES documents(Id) ON DELETE SET NULL,
    CONSTRAINT FK_UserInteractions_CalendarEvents FOREIGN KEY (RelatedEventId) 
        REFERENCES calendar_events(Id) ON DELETE SET NULL,
    CONSTRAINT FK_UserInteractions_Todos FOREIGN KEY (RelatedTodoId) 
        REFERENCES todos(Id) ON DELETE SET NULL,
    
    INDEX IX_UserInteractions_UserId (UserId),
    INDEX IX_UserInteractions_Status (Status),
    INDEX IX_UserInteractions_InteractionType (InteractionType),
    INDEX IX_UserInteractions_CreatedAt (CreatedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -------------------------------------------------------------------------
-- 3. Learning Deficits Table
-- -------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS learning_deficits (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    Subject VARCHAR(100) NOT NULL,
    Topic VARCHAR(200) NOT NULL,
    Subtopic VARCHAR(200) NULL,
    ErrorType VARCHAR(100) NOT NULL,
    ErrorDescription TEXT NOT NULL,
    OccurrenceCount INT NOT NULL DEFAULT 1,
    FirstOccurrence DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    LastOccurrence DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    Severity VARCHAR(50) NOT NULL DEFAULT 'low',
    NeedsTutoring TINYINT(1) NOT NULL DEFAULT 0,
    RelatedDocumentIds JSON NOT NULL DEFAULT ('[]'),
    CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    ResolvedAt DATETIME(6) NULL,
    
    CONSTRAINT FK_LearningDeficits_Users FOREIGN KEY (UserId) 
        REFERENCES users(Id) ON DELETE CASCADE,
    
    INDEX IX_LearningDeficits_UserId (UserId),
    INDEX IX_LearningDeficits_Subject (Subject),
    INDEX IX_LearningDeficits_Severity (Severity),
    INDEX IX_LearningDeficits_NeedsTutoring (NeedsTutoring),
    INDEX IX_LearningDeficits_ResolvedAt (ResolvedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
