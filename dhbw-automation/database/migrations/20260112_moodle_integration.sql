-- =========================================================================
-- DHBW Automation - Moodle Integration
-- Migration: 20260112_moodle_integration
-- Creates Moodle-related tables and extends users table
-- =========================================================================

USE dhbw_automation;

-- -------------------------------------------------------------------------
-- 1. Extend Users Table with Moodle fields
-- -------------------------------------------------------------------------

ALTER TABLE users
ADD COLUMN IF NOT EXISTS MoodleSyncEnabled TINYINT(1) NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS MoodleUserId INT NULL,
ADD COLUMN IF NOT EXISTS MoodleUsername VARCHAR(100) NULL,
ADD COLUMN IF NOT EXISTS MoodlePassword VARCHAR(500) NULL,
ADD COLUMN IF NOT EXISTS MoodleLastSync DATETIME(6) NULL,
ADD COLUMN IF NOT EXISTS MoodleLastSyncError VARCHAR(1000) NULL;

-- Index for Moodle sync queries
CREATE INDEX IF NOT EXISTS IX_Users_MoodleSyncEnabled ON users(MoodleSyncEnabled);

-- -------------------------------------------------------------------------
-- 2. Moodle Courses Table
-- -------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS moodle_courses (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,

    -- Moodle-specific IDs
    MoodleCourseId INT NOT NULL,

    -- Course details
    Shortname VARCHAR(100) NOT NULL,
    Fullname VARCHAR(500) NOT NULL,
    Summary TEXT NULL,
    Format VARCHAR(50) NULL,

    -- Course dates
    StartDate DATETIME(6) NULL,
    EndDate DATETIME(6) NULL,

    -- Status
    Visible TINYINT(1) NOT NULL DEFAULT 1,
    Progress INT NULL,  -- Completion percentage if available

    -- Sync tracking
    LastSynced DATETIME(6) NULL,

    -- Timestamps
    CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    UpdatedAt DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),

    -- Constraints
    CONSTRAINT FK_MoodleCourses_Users FOREIGN KEY (UserId)
        REFERENCES users(Id) ON DELETE CASCADE,

    -- Unique constraint: one course per user
    UNIQUE KEY UK_MoodleCourses_User_Course (UserId, MoodleCourseId),

    -- Indexes
    INDEX IX_MoodleCourses_UserId (UserId),
    INDEX IX_MoodleCourses_MoodleCourseId (MoodleCourseId),
    INDEX IX_MoodleCourses_LastSynced (LastSynced)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -------------------------------------------------------------------------
-- 3. Moodle Assignments Table
-- -------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS moodle_assignments (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,

    -- Course reference
    CourseId INT NOT NULL,
    CourseName VARCHAR(300) NULL,

    -- Moodle-specific IDs
    MoodleAssignmentId INT NOT NULL,

    -- Assignment details
    Title VARCHAR(500) NOT NULL,
    Description TEXT NULL,

    -- Dates
    DueDate DATETIME(6) NULL,
    CutoffDate DATETIME(6) NULL,
    AllowSubmissionsFrom DATETIME(6) NULL,

    -- Grading
    MaxGrade INT NOT NULL DEFAULT 100,
    Grade DOUBLE NULL,
    GradingStatus VARCHAR(50) NULL,

    -- Submission status
    IsSubmitted TINYINT(1) NOT NULL DEFAULT 0,
    SubmittedAt DATETIME(6) NULL,
    SubmissionStatus VARCHAR(50) NULL,

    -- Links to other entities
    CalendarEventId INT NULL,
    TodoId INT NULL,

    -- Sync tracking
    SyncedAt DATETIME(6) NULL,

    -- Timestamps
    CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    UpdatedAt DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),

    -- Constraints
    CONSTRAINT FK_MoodleAssignments_Users FOREIGN KEY (UserId)
        REFERENCES users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_MoodleAssignments_CalendarEvents FOREIGN KEY (CalendarEventId)
        REFERENCES calendar_events(Id) ON DELETE SET NULL,
    CONSTRAINT FK_MoodleAssignments_Todos FOREIGN KEY (TodoId)
        REFERENCES todos(Id) ON DELETE SET NULL,

    -- Unique constraint
    UNIQUE KEY UK_MoodleAssignments_User_Assignment (UserId, MoodleAssignmentId),

    -- Indexes
    INDEX IX_MoodleAssignments_UserId (UserId),
    INDEX IX_MoodleAssignments_CourseId (CourseId),
    INDEX IX_MoodleAssignments_DueDate (DueDate),
    INDEX IX_MoodleAssignments_IsSubmitted (IsSubmitted),
    INDEX IX_MoodleAssignments_SyncedAt (SyncedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -------------------------------------------------------------------------
-- 4. Moodle Resources Table
-- -------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS moodle_resources (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,

    -- Course reference
    CourseId INT NOT NULL,
    CourseName VARCHAR(300) NULL,

    -- Section info
    SectionNumber INT NOT NULL DEFAULT 0,
    SectionName VARCHAR(300) NULL,

    -- Resource type: file, url, page, folder, label
    ResourceType VARCHAR(50) NOT NULL,

    -- Moodle-specific IDs
    MoodleResourceId INT NOT NULL,

    -- Resource details
    Title VARCHAR(500) NOT NULL,
    Description TEXT NULL,

    -- File information
    DownloadUrl VARCHAR(1000) NULL,
    ExternalUrl VARCHAR(1000) NULL,
    FileType VARCHAR(100) NULL,
    FileSize BIGINT NULL,
    MimeType VARCHAR(200) NULL,

    -- Download tracking
    IsDownloaded TINYINT(1) NOT NULL DEFAULT 0,
    LocalDocumentId INT NULL,

    -- Sync tracking
    LastCheckedAt DATETIME(6) NULL,
    SyncedAt DATETIME(6) NULL,
    FileHash VARCHAR(64) NULL,  -- For detecting changes

    -- Timestamps
    CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    UpdatedAt DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),

    -- Constraints
    CONSTRAINT FK_MoodleResources_Users FOREIGN KEY (UserId)
        REFERENCES users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_MoodleResources_Documents FOREIGN KEY (LocalDocumentId)
        REFERENCES documents(Id) ON DELETE SET NULL,

    -- Unique constraint
    UNIQUE KEY UK_MoodleResources_User_Resource (UserId, MoodleResourceId),

    -- Indexes
    INDEX IX_MoodleResources_UserId (UserId),
    INDEX IX_MoodleResources_CourseId (CourseId),
    INDEX IX_MoodleResources_ResourceType (ResourceType),
    INDEX IX_MoodleResources_IsDownloaded (IsDownloaded),
    INDEX IX_MoodleResources_SyncedAt (SyncedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -------------------------------------------------------------------------
-- 5. Moodle Calendar Events Table (for Moodle-specific events)
-- -------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS moodle_calendar_events (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,

    -- Moodle-specific IDs
    MoodleEventId INT NOT NULL,

    -- Course reference (optional, can be site-wide events)
    CourseId INT NULL,
    CourseName VARCHAR(300) NULL,

    -- Event details
    Name VARCHAR(500) NOT NULL,
    Description TEXT NULL,
    EventType VARCHAR(50) NULL,  -- due, course, user, etc.
    ModuleName VARCHAR(100) NULL,  -- assign, quiz, etc.

    -- Timing
    TimeStart DATETIME(6) NOT NULL,
    TimeDuration INT NOT NULL DEFAULT 0,  -- Duration in seconds

    -- Link to local calendar event
    CalendarEventId INT NULL,

    -- Sync tracking
    SyncedAt DATETIME(6) NULL,

    -- Timestamps
    CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    UpdatedAt DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),

    -- Constraints
    CONSTRAINT FK_MoodleCalendarEvents_Users FOREIGN KEY (UserId)
        REFERENCES users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_MoodleCalendarEvents_CalendarEvents FOREIGN KEY (CalendarEventId)
        REFERENCES calendar_events(Id) ON DELETE SET NULL,

    -- Unique constraint
    UNIQUE KEY UK_MoodleCalendarEvents_User_Event (UserId, MoodleEventId),

    -- Indexes
    INDEX IX_MoodleCalendarEvents_UserId (UserId),
    INDEX IX_MoodleCalendarEvents_TimeStart (TimeStart),
    INDEX IX_MoodleCalendarEvents_EventType (EventType)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
