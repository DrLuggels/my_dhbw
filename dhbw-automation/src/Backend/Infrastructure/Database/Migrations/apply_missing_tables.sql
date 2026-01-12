-- =====================================================
-- SQL Script to create missing tables
-- Run this directly on MariaDB to add missing tables
-- =====================================================

-- ========== COURSES TABLE ==========
CREATE TABLE IF NOT EXISTS `courses` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `UserId` int NOT NULL,
    `CourseName` varchar(100) NOT NULL,
    `CourseCode` varchar(50) DEFAULT NULL,
    `Professor` varchar(100) DEFAULT NULL,
    `Semester` varchar(50) DEFAULT NULL,
    `Description` TEXT DEFAULT NULL,
    `MoodleUrl` varchar(255) DEFAULT NULL,
    `MoodleId` varchar(100) DEFAULT NULL,
    `IsActive` tinyint(1) NOT NULL DEFAULT 1,
    `StartDate` datetime(6) DEFAULT NULL,
    `EndDate` datetime(6) DEFAULT NULL,
    `AdditionalInfo` JSON DEFAULT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) DEFAULT NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_courses_UserId` (`UserId`),
    KEY `IX_courses_MoodleId` (`MoodleId`),
    KEY `IX_courses_IsActive` (`IsActive`),
    CONSTRAINT `FK_courses_users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ========== PROJECTS TABLE ==========
CREATE TABLE IF NOT EXISTS `projects` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `UserId` int NOT NULL,
    `Name` varchar(200) NOT NULL,
    `Description` TEXT DEFAULT NULL,
    `Priority` varchar(50) NOT NULL DEFAULT 'medium',
    `Interest` varchar(50) NOT NULL DEFAULT 'medium',
    `Importance` varchar(50) NOT NULL DEFAULT 'medium',
    `WeeklyMinutes` int DEFAULT NULL,
    `Status` varchar(50) NOT NULL DEFAULT 'idea',
    `CreatedAt` datetime(6) NOT NULL,
    `StartedAt` datetime(6) DEFAULT NULL,
    `CompletedAt` datetime(6) DEFAULT NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_projects_UserId_Status` (`UserId`, `Status`),
    KEY `IX_projects_Priority` (`Priority`),
    CONSTRAINT `FK_projects_users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ========== KNOWLEDGE_BASE_ITEMS TABLE ==========
CREATE TABLE IF NOT EXISTS `KnowledgeBaseItems` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `UserId` int NOT NULL,
    `Subject` varchar(100) NOT NULL,
    `Topic` varchar(200) NOT NULL,
    `Subtopic` varchar(200) DEFAULT NULL,
    `Category` varchar(50) NOT NULL DEFAULT 'grundlagen',
    `Importance` varchar(20) NOT NULL DEFAULT 'medium',
    `LastTestedDate` datetime(6) NOT NULL,
    `TestCount` int NOT NULL DEFAULT 0,
    `AverageScore` double NOT NULL DEFAULT 0.0,
    `LastScore` double NOT NULL DEFAULT 0.0,
    `NextReviewDate` datetime(6) NOT NULL,
    `Notes` varchar(1000) DEFAULT NULL,
    `IsActive` tinyint(1) NOT NULL DEFAULT 1,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) DEFAULT NULL,
    `HasEmbedding` tinyint(1) NOT NULL DEFAULT 0,
    `QdrantPointId` varchar(100) DEFAULT NULL,
    `SourceType` varchar(50) DEFAULT NULL,
    `SourceId` int DEFAULT NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_KnowledgeBaseItems_UserId_Subject_Topic` (`UserId`, `Subject`, `Topic`),
    KEY `IX_KnowledgeBaseItems_UserId_NextReviewDate` (`UserId`, `NextReviewDate`),
    KEY `IX_KnowledgeBaseItems_UserId_IsActive_NextReviewDate` (`UserId`, `IsActive`, `NextReviewDate`),
    KEY `IX_KnowledgeBaseItems_Category` (`Category`),
    KEY `IX_KnowledgeBaseItems_Importance` (`Importance`),
    CONSTRAINT `FK_KnowledgeBaseItems_users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ========== INTERACTIVE_EXERCISES TABLE ==========
CREATE TABLE IF NOT EXISTS `interactive_exercises` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `UserId` int NOT NULL,
    `DeficitId` int DEFAULT NULL,
    `KnowledgeBaseItemId` int DEFAULT NULL,
    `Subject` varchar(100) NOT NULL,
    `Topic` varchar(200) NOT NULL,
    `Difficulty` varchar(50) NOT NULL DEFAULT 'medium',
    `ExerciseContent` TEXT NOT NULL,
    `StepProgress` TEXT NOT NULL,
    `CompletedSteps` int NOT NULL DEFAULT 0,
    `TotalSteps` int NOT NULL DEFAULT 0,
    `Score` double NOT NULL DEFAULT 0,
    `TimeSpentSeconds` int NOT NULL DEFAULT 0,
    `StartedAt` datetime(6) DEFAULT NULL,
    `CompletedAt` datetime(6) DEFAULT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `NextReviewDate` datetime(6) NOT NULL,
    `ReviewCount` int NOT NULL DEFAULT 0,
    `EaseFactor` double NOT NULL DEFAULT 2.5,
    PRIMARY KEY (`Id`),
    KEY `IX_interactive_exercises_UserId_Subject` (`UserId`, `Subject`),
    KEY `IX_interactive_exercises_NextReviewDate` (`NextReviewDate`),
    KEY `IX_interactive_exercises_UserId_CompletedAt` (`UserId`, `CompletedAt`),
    KEY `IX_interactive_exercises_Difficulty` (`Difficulty`),
    KEY `IX_interactive_exercises_DeficitId` (`DeficitId`),
    KEY `IX_interactive_exercises_KnowledgeBaseItemId` (`KnowledgeBaseItemId`),
    CONSTRAINT `FK_interactive_exercises_users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_interactive_exercises_learning_deficits_DeficitId` FOREIGN KEY (`DeficitId`) REFERENCES `learning_deficits` (`Id`) ON DELETE SET NULL,
    CONSTRAINT `FK_interactive_exercises_KnowledgeBaseItems_KnowledgeBaseItemId` FOREIGN KEY (`KnowledgeBaseItemId`) REFERENCES `KnowledgeBaseItems` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ========== NEXTCLOUD_CREDENTIALS TABLE ==========
CREATE TABLE IF NOT EXISTS `nextcloud_credentials` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `UserId` int NOT NULL,
    `NextcloudUrl` varchar(500) NOT NULL DEFAULT 'https://nextcloud.dhbw-ravensburg.de',
    `Username` varchar(200) NOT NULL,
    `EncryptedPassword` varchar(500) NOT NULL,
    `IsActive` tinyint(1) NOT NULL DEFAULT 1,
    `SyncIntervalMinutes` int NOT NULL DEFAULT 60,
    `SyncFolders` JSON DEFAULT NULL,
    `LastSyncAt` datetime(6) DEFAULT NULL,
    `LastSyncError` varchar(1000) DEFAULT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) DEFAULT NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_nextcloud_credentials_UserId_IsActive` (`UserId`, `IsActive`),
    KEY `IX_nextcloud_credentials_LastSyncAt` (`LastSyncAt`),
    CONSTRAINT `FK_nextcloud_credentials_users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ========== NEXTCLOUD_FILES TABLE ==========
CREATE TABLE IF NOT EXISTS `nextcloud_files` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `UserId` int NOT NULL,
    `CredentialId` int NOT NULL,
    `RemotePath` varchar(1000) NOT NULL,
    `FileName` varchar(255) NOT NULL,
    `FileType` varchar(100) NOT NULL,
    `FileSize` bigint NOT NULL,
    `ETag` varchar(64) DEFAULT NULL,
    `RemoteModifiedAt` datetime(6) NOT NULL,
    `LocalSyncedAt` datetime(6) DEFAULT NULL,
    `LocalDocumentId` int DEFAULT NULL,
    `IsDownloaded` tinyint(1) NOT NULL DEFAULT 0,
    `IsProcessed` tinyint(1) NOT NULL DEFAULT 0,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) DEFAULT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_nextcloud_files_UserId_RemotePath` (`UserId`, `RemotePath`(255)),
    KEY `IX_nextcloud_files_CredentialId_IsDownloaded` (`CredentialId`, `IsDownloaded`),
    KEY `IX_nextcloud_files_ETag` (`ETag`),
    KEY `IX_nextcloud_files_LocalDocumentId` (`LocalDocumentId`),
    CONSTRAINT `FK_nextcloud_files_users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_nextcloud_files_nextcloud_credentials_CredentialId` FOREIGN KEY (`CredentialId`) REFERENCES `nextcloud_credentials` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_nextcloud_files_documents_LocalDocumentId` FOREIGN KEY (`LocalDocumentId`) REFERENCES `documents` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ========== MOODLE_COURSES TABLE ==========
CREATE TABLE IF NOT EXISTS `moodle_courses` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `UserId` int NOT NULL,
    `MoodleCourseId` int NOT NULL,
    `Shortname` varchar(100) NOT NULL,
    `Fullname` varchar(500) NOT NULL,
    `Summary` TEXT DEFAULT NULL,
    `Format` varchar(50) DEFAULT NULL,
    `StartDate` datetime(6) DEFAULT NULL,
    `EndDate` datetime(6) DEFAULT NULL,
    `Visible` tinyint(1) NOT NULL DEFAULT 1,
    `Progress` int DEFAULT NULL,
    `LastSynced` datetime(6) DEFAULT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) DEFAULT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_moodle_courses_UserId_MoodleCourseId` (`UserId`, `MoodleCourseId`),
    KEY `IX_moodle_courses_UserId` (`UserId`),
    KEY `IX_moodle_courses_LastSynced` (`LastSynced`),
    CONSTRAINT `FK_moodle_courses_users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ========== MOODLE_RESOURCES TABLE ==========
CREATE TABLE IF NOT EXISTS `moodle_resources` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `UserId` int NOT NULL,
    `CourseId` int NOT NULL,
    `CourseName` varchar(300) DEFAULT NULL,
    `ResourceType` varchar(50) NOT NULL,
    `MoodleResourceId` int NOT NULL,
    `Title` varchar(500) NOT NULL,
    `Description` TEXT DEFAULT NULL,
    `DownloadUrl` varchar(1000) DEFAULT NULL,
    `ExternalUrl` varchar(1000) DEFAULT NULL,
    `FileType` varchar(100) DEFAULT NULL,
    `FileSize` bigint DEFAULT NULL,
    `SectionNumber` int NOT NULL DEFAULT 0,
    `SectionName` varchar(300) DEFAULT NULL,
    `LocalDocumentId` int DEFAULT NULL,
    `IsDownloaded` tinyint(1) NOT NULL DEFAULT 0,
    `LastCheckedAt` datetime(6) DEFAULT NULL,
    `SyncedAt` datetime(6) DEFAULT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) DEFAULT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_moodle_resources_UserId_MoodleResourceId` (`UserId`, `MoodleResourceId`),
    KEY `IX_moodle_resources_UserId_CourseId` (`UserId`, `CourseId`),
    KEY `IX_moodle_resources_ResourceType` (`ResourceType`),
    KEY `IX_moodle_resources_IsDownloaded` (`IsDownloaded`),
    KEY `IX_moodle_resources_LocalDocumentId` (`LocalDocumentId`),
    CONSTRAINT `FK_moodle_resources_users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_moodle_resources_documents_LocalDocumentId` FOREIGN KEY (`LocalDocumentId`) REFERENCES `documents` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ========== MOODLE_ASSIGNMENTS TABLE ==========
CREATE TABLE IF NOT EXISTS `moodle_assignments` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `UserId` int NOT NULL,
    `CourseId` int NOT NULL,
    `CourseName` varchar(300) DEFAULT NULL,
    `MoodleAssignmentId` int NOT NULL,
    `Title` varchar(500) NOT NULL,
    `Description` TEXT DEFAULT NULL,
    `DueDate` datetime(6) DEFAULT NULL,
    `CutoffDate` datetime(6) DEFAULT NULL,
    `AllowSubmissionsFrom` datetime(6) DEFAULT NULL,
    `MaxGrade` int NOT NULL DEFAULT 100,
    `IsSubmitted` tinyint(1) NOT NULL DEFAULT 0,
    `SubmittedAt` datetime(6) DEFAULT NULL,
    `SubmissionStatus` varchar(50) DEFAULT NULL,
    `Grade` double DEFAULT NULL,
    `GradingStatus` varchar(50) DEFAULT NULL,
    `CalendarEventId` int DEFAULT NULL,
    `TodoId` int DEFAULT NULL,
    `SyncedAt` datetime(6) DEFAULT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) DEFAULT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_moodle_assignments_UserId_MoodleAssignmentId` (`UserId`, `MoodleAssignmentId`),
    KEY `IX_moodle_assignments_UserId_CourseId` (`UserId`, `CourseId`),
    KEY `IX_moodle_assignments_DueDate` (`DueDate`),
    KEY `IX_moodle_assignments_UserId_IsSubmitted` (`UserId`, `IsSubmitted`),
    KEY `IX_moodle_assignments_CalendarEventId` (`CalendarEventId`),
    KEY `IX_moodle_assignments_TodoId` (`TodoId`),
    CONSTRAINT `FK_moodle_assignments_users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_moodle_assignments_calendar_events_CalendarEventId` FOREIGN KEY (`CalendarEventId`) REFERENCES `calendar_events` (`Id`) ON DELETE SET NULL,
    CONSTRAINT `FK_moodle_assignments_todos_TodoId` FOREIGN KEY (`TodoId`) REFERENCES `todos` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ========== MOODLE_CALENDAR_EVENTS TABLE ==========
CREATE TABLE IF NOT EXISTS `moodle_calendar_events` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `UserId` int NOT NULL,
    `MoodleEventId` int NOT NULL,
    `CourseId` int DEFAULT NULL,
    `CourseName` varchar(300) DEFAULT NULL,
    `Name` varchar(500) NOT NULL,
    `Description` TEXT DEFAULT NULL,
    `EventType` varchar(50) DEFAULT NULL,
    `ModuleName` varchar(100) DEFAULT NULL,
    `TimeStart` datetime(6) NOT NULL,
    `TimeDuration` int NOT NULL DEFAULT 0,
    `CalendarEventId` int DEFAULT NULL,
    `SyncedAt` datetime(6) DEFAULT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) DEFAULT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_moodle_calendar_events_UserId_MoodleEventId` (`UserId`, `MoodleEventId`),
    KEY `IX_moodle_calendar_events_UserId` (`UserId`),
    KEY `IX_moodle_calendar_events_TimeStart` (`TimeStart`),
    KEY `IX_moodle_calendar_events_EventType` (`EventType`),
    KEY `IX_moodle_calendar_events_CalendarEventId` (`CalendarEventId`),
    CONSTRAINT `FK_moodle_calendar_events_users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_moodle_calendar_events_calendar_events_CalendarEventId` FOREIGN KEY (`CalendarEventId`) REFERENCES `calendar_events` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ========== DOCUMENT_IMAGES TABLE ==========
CREATE TABLE IF NOT EXISTS `document_images` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `DocumentId` int NOT NULL,
    `PageNumber` int NOT NULL,
    `ImageIndex` int NOT NULL,
    `StoragePath` varchar(500) NOT NULL,
    `FileName` varchar(255) DEFAULT NULL,
    `ImageFormat` varchar(50) NOT NULL DEFAULT 'png',
    `Width` int NOT NULL,
    `Height` int NOT NULL,
    `FileSize` bigint NOT NULL,
    `GeminiDescription` TEXT DEFAULT NULL,
    `ExtractedText` TEXT DEFAULT NULL,
    `DetectedObjects` JSON DEFAULT NULL,
    `ImageType` varchar(50) DEFAULT NULL,
    `RelevanceScore` double NOT NULL DEFAULT 0.5,
    `IsProcessed` tinyint(1) NOT NULL DEFAULT 0,
    `ProcessedAt` datetime(6) DEFAULT NULL,
    `HasEmbedding` tinyint(1) NOT NULL DEFAULT 0,
    `QdrantPointId` varchar(100) DEFAULT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_document_images_DocumentId_PageNumber_ImageIndex` (`DocumentId`, `PageNumber`, `ImageIndex`),
    KEY `IX_document_images_DocumentId` (`DocumentId`),
    KEY `IX_document_images_IsProcessed` (`IsProcessed`),
    KEY `IX_document_images_ImageType` (`ImageType`),
    KEY `IX_document_images_HasEmbedding` (`HasEmbedding`),
    CONSTRAINT `FK_document_images_documents_DocumentId` FOREIGN KEY (`DocumentId`) REFERENCES `documents` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ========== ADD MISSING COLUMNS TO EXISTING TABLES ==========

-- Add RelatedProjectId to documents if not exists
SET @dbname = DATABASE();
SET @tablename = 'documents';
SET @columnname = 'RelatedProjectId';
SET @preparedStatement = (SELECT IF(
  (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = @tablename AND COLUMN_NAME = @columnname) > 0,
  'SELECT 1',
  'ALTER TABLE documents ADD COLUMN RelatedProjectId int DEFAULT NULL, ADD KEY IX_documents_RelatedProjectId (RelatedProjectId), ADD CONSTRAINT FK_documents_projects_RelatedProjectId FOREIGN KEY (RelatedProjectId) REFERENCES projects(Id) ON DELETE SET NULL'
));
PREPARE alterIfNotExists FROM @preparedStatement;
EXECUTE alterIfNotExists;
DEALLOCATE PREPARE alterIfNotExists;

-- Add RelatedProjectId to todos if not exists
SET @tablename = 'todos';
SET @columnname = 'RelatedProjectId';
SET @preparedStatement = (SELECT IF(
  (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = @tablename AND COLUMN_NAME = @columnname) > 0,
  'SELECT 1',
  'ALTER TABLE todos ADD COLUMN RelatedProjectId int DEFAULT NULL, ADD KEY IX_todos_RelatedProjectId (RelatedProjectId), ADD CONSTRAINT FK_todos_projects_RelatedProjectId FOREIGN KEY (RelatedProjectId) REFERENCES projects(Id) ON DELETE CASCADE'
));
PREPARE alterIfNotExists FROM @preparedStatement;
EXECUTE alterIfNotExists;
DEALLOCATE PREPARE alterIfNotExists;

-- Add KnowledgeBaseItemId to generated_exercises if not exists
SET @tablename = 'generated_exercises';
SET @columnname = 'KnowledgeBaseItemId';
SET @preparedStatement = (SELECT IF(
  (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = @tablename AND COLUMN_NAME = @columnname) > 0,
  'SELECT 1',
  'ALTER TABLE generated_exercises ADD COLUMN KnowledgeBaseItemId int DEFAULT NULL, ADD KEY IX_generated_exercises_KnowledgeBaseItemId (KnowledgeBaseItemId), ADD CONSTRAINT FK_generated_exercises_KnowledgeBaseItems_KnowledgeBaseItemId FOREIGN KEY (KnowledgeBaseItemId) REFERENCES KnowledgeBaseItems(Id) ON DELETE SET NULL'
));
PREPARE alterIfNotExists FROM @preparedStatement;
EXECUTE alterIfNotExists;
DEALLOCATE PREPARE alterIfNotExists;

-- Record migration in history
INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260112201000_AddMissingTables', '8.0.0')
ON DUPLICATE KEY UPDATE `ProductVersion` = '8.0.0';

SELECT 'Migration completed successfully!' AS Status;
