-- =========================================================================
-- E-Mail-Integration für DHBW Automation
-- Migration: 20260108_AddEmailIntegration
-- =========================================================================

USE dhbw_automation;

-- -------------------------------------------------------------------------
-- 1. Erweitere Users-Tabelle um E-Mail-Sync Felder
-- -------------------------------------------------------------------------

ALTER TABLE users
ADD COLUMN EmailSyncEnabled TINYINT(1) NOT NULL DEFAULT 0 AFTER LastLoginAt,
ADD COLUMN EmailSyncAddress VARCHAR(200) NULL AFTER EmailSyncEnabled,
ADD COLUMN EmailSyncPassword VARCHAR(500) NULL AFTER EmailSyncAddress,
ADD COLUMN EmailImapHost VARCHAR(200) NULL AFTER EmailSyncPassword,
ADD COLUMN EmailImapPort INT NOT NULL DEFAULT 993 AFTER EmailImapHost,
ADD COLUMN EmailSmtpHost VARCHAR(200) NULL AFTER EmailImapPort,
ADD COLUMN EmailSmtpPort INT NOT NULL DEFAULT 587 AFTER EmailSmtpHost,
ADD COLUMN EmailSyncIntervalMinutes INT NOT NULL DEFAULT 1 AFTER EmailSmtpPort,
ADD COLUMN LastEmailSync DATETIME(6) NULL AFTER EmailSyncIntervalMinutes;

-- -------------------------------------------------------------------------
-- 2. Erstelle Emails-Tabelle
-- -------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS Emails (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    MessageId VARCHAR(500) NOT NULL,
    Subject VARCHAR(500) NOT NULL,
    FromAddress VARCHAR(500) NOT NULL,
    FromName VARCHAR(500) NOT NULL DEFAULT '',
    ToAddresses TEXT NOT NULL,
    CcAddresses TEXT NOT NULL DEFAULT '',
    BodyText TEXT NOT NULL,
    BodyHtml TEXT NULL,
    ReceivedAt DATETIME(6) NOT NULL,
    FetchedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    IsRead TINYINT(1) NOT NULL DEFAULT 0,
    IsImportant TINYINT(1) NOT NULL DEFAULT 0,
    HasAttachments TINYINT(1) NOT NULL DEFAULT 0,
    Folder VARCHAR(100) NOT NULL DEFAULT 'INBOX',
    
    -- KI-Analyse Felder
    IsProcessed TINYINT(1) NOT NULL DEFAULT 0,
    ProcessedAt DATETIME(6) NULL,
    Summary TEXT NULL,
    Category VARCHAR(50) NULL,
    IsAppointment TINYINT(1) NOT NULL DEFAULT 0,
    RequiresUserAction TINYINT(1) NOT NULL DEFAULT 0,
    SuggestedAction VARCHAR(50) NULL,
    Priority INT NOT NULL DEFAULT 2,
    ExtractedData TEXT NULL,
    
    -- Verknüpfungen
    RelatedCalendarEventId INT NULL,
    ActionStatus VARCHAR(50) NOT NULL DEFAULT 'pending',
    ActionTakenAt DATETIME(6) NULL,
    
    -- Foreign Keys
    CONSTRAINT FK_Emails_Users FOREIGN KEY (UserId) 
        REFERENCES users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Emails_CalendarEvents FOREIGN KEY (RelatedCalendarEventId) 
        REFERENCES CalendarEvents(Id) ON DELETE SET NULL,
    
    -- Indexes
    INDEX IX_Emails_UserId (UserId),
    UNIQUE INDEX IX_Emails_MessageId (MessageId),
    INDEX IX_Emails_UserId_ReceivedAt (UserId, ReceivedAt),
    INDEX IX_Emails_UserId_IsRead (UserId, IsRead),
    INDEX IX_Emails_UserId_RequiresUserAction (UserId, RequiresUserAction),
    INDEX IX_Emails_IsProcessed (IsProcessed),
    INDEX IX_Emails_Category (Category),
    INDEX IX_Emails_ActionStatus (ActionStatus)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -------------------------------------------------------------------------
-- 3. Erstelle EmailAttachments-Tabelle
-- -------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS EmailAttachments (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    EmailId INT NOT NULL,
    FileName VARCHAR(500) NOT NULL,
    ContentType VARCHAR(200) NOT NULL,
    FileSize BIGINT NOT NULL,
    ContentId VARCHAR(500) NULL,
    IsInline TINYINT(1) NOT NULL DEFAULT 0,
    DownloadedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    
    -- Document-Verknüpfung
    RelatedDocumentId INT NULL,
    IsProcessed TINYINT(1) NOT NULL DEFAULT 0,
    ProcessedAt DATETIME(6) NULL,
    
    -- Foreign Keys
    CONSTRAINT FK_EmailAttachments_Emails FOREIGN KEY (EmailId) 
        REFERENCES Emails(Id) ON DELETE CASCADE,
    CONSTRAINT FK_EmailAttachments_Documents FOREIGN KEY (RelatedDocumentId) 
        REFERENCES Documents(Id) ON DELETE SET NULL,
    
    -- Indexes
    INDEX IX_EmailAttachments_EmailId (EmailId),
    INDEX IX_EmailAttachments_RelatedDocumentId (RelatedDocumentId),
    INDEX IX_EmailAttachments_IsProcessed (IsProcessed)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -------------------------------------------------------------------------
-- Migration erfolgreich!
-- -------------------------------------------------------------------------

SELECT 'E-Mail-Integration Migration erfolgreich ausgeführt!' AS Status;
