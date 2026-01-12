-- ============================================================================
-- Email System Migration
-- Created: 2026-01-09
-- Description: Adds email synchronization and AI-powered email processing
-- ============================================================================

-- Add Email sync columns to Users table
ALTER TABLE Users ADD COLUMN EmailSyncEnabled BOOLEAN NOT NULL DEFAULT 0;
ALTER TABLE Users ADD COLUMN EmailSyncAddress VARCHAR(255) NULL;
ALTER TABLE Users ADD COLUMN EmailSyncPassword TEXT NULL;
ALTER TABLE Users ADD COLUMN EmailImapHost VARCHAR(255) NULL;
ALTER TABLE Users ADD COLUMN EmailImapPort INT NULL DEFAULT 993;
ALTER TABLE Users ADD COLUMN EmailSmtpHost VARCHAR(255) NULL;
ALTER TABLE Users ADD COLUMN EmailSmtpPort INT NULL DEFAULT 587;
ALTER TABLE Users ADD COLUMN EmailSyncIntervalMinutes INT NULL DEFAULT 15;
ALTER TABLE Users ADD COLUMN LastEmailSync DATETIME NULL;
ALTER TABLE Users ADD COLUMN EmailSyncLastError TEXT NULL;
ALTER TABLE Users ADD COLUMN EmailSyncEnabled_At DATETIME NULL;

-- Create Emails table
CREATE TABLE IF NOT EXISTS Emails (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    UserId INT NOT NULL,
    MessageId VARCHAR(500) NOT NULL,
    Subject VARCHAR(1000) NULL,
    Sender VARCHAR(500) NULL,
    Recipients TEXT NULL,
    Body TEXT NULL,
    ReceivedDate DATETIME NOT NULL,
    IsRead BOOLEAN NOT NULL DEFAULT 0,
    IsProcessed BOOLEAN NOT NULL DEFAULT 0,
    
    -- AI Analysis fields
    Category VARCHAR(100) NULL,
    IsAppointment BOOLEAN NOT NULL DEFAULT 0,
    RequiresUserAction BOOLEAN NOT NULL DEFAULT 0,
    SuggestedAction VARCHAR(500) NULL,
    Priority INT NOT NULL DEFAULT 0,
    Summary TEXT NULL,
    ExtractedData JSON NULL,
    
    -- Action tracking
    ActionStatus VARCHAR(50) NULL,
    ActionTakenAt DATETIME NULL,
    RelatedCalendarEventId INT NULL,
    RelatedTodoId INT NULL,
    
    -- Metadata
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (RelatedCalendarEventId) REFERENCES CalendarEvents(Id) ON DELETE SET NULL,
    FOREIGN KEY (RelatedTodoId) REFERENCES Todos(Id) ON DELETE SET NULL,
    
    INDEX idx_user_received (UserId, ReceivedDate DESC),
    INDEX idx_is_read (IsRead),
    INDEX idx_is_processed (IsProcessed),
    INDEX idx_category (Category),
    UNIQUE INDEX idx_user_message (UserId, MessageId)
);

-- Create EmailAttachments table
CREATE TABLE IF NOT EXISTS EmailAttachments (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    EmailId INT NOT NULL,
    FileName VARCHAR(255) NOT NULL,
    ContentType VARCHAR(100) NULL,
    FileSize BIGINT NOT NULL,
    ContentId VARCHAR(255) NULL,
    IsInline BOOLEAN NOT NULL DEFAULT 0,
    RelatedDocumentId INT NULL,
    
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (EmailId) REFERENCES Emails(Id) ON DELETE CASCADE,
    FOREIGN KEY (RelatedDocumentId) REFERENCES Documents(Id) ON DELETE SET NULL,
    
    INDEX idx_email_id (EmailId)
);

