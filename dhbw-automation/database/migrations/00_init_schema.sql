-- =========================================================================
-- DHBW Automation - Initial Schema
-- Migration: 00_init_schema
-- =========================================================================

USE dhbw_automation;

-- -------------------------------------------------------------------------
-- 1. Users Table
-- -------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS users (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Email VARCHAR(200) NOT NULL UNIQUE,
    PasswordHash VARCHAR(500) NOT NULL,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    LastLoginAt DATETIME(6) NULL,
    
    INDEX IX_Users_Email (Email)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -------------------------------------------------------------------------
-- 2. CalendarEvents Table
-- -------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS CalendarEvents (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    Title VARCHAR(500) NOT NULL,
    Description TEXT NULL,
    Location VARCHAR(500) NULL,
    StartTime DATETIME(6) NOT NULL,
    EndTime DATETIME(6) NOT NULL,
    IsAllDay TINYINT(1) NOT NULL DEFAULT 0,
    Source VARCHAR(50) NOT NULL,
    ExternalId VARCHAR(200) NULL,
    Notes TEXT NULL,
    CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    UpdatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    
    CONSTRAINT FK_CalendarEvents_Users FOREIGN KEY (UserId) 
        REFERENCES users(Id) ON DELETE CASCADE,
    
    INDEX IX_CalendarEvents_UserId (UserId),
    INDEX IX_CalendarEvents_StartTime (StartTime),
    INDEX IX_CalendarEvents_UserId_StartTime (UserId, StartTime),
    INDEX IX_CalendarEvents_Source (Source)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -------------------------------------------------------------------------
-- 3. Documents Table
-- -------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS Documents (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    FileName VARCHAR(500) NOT NULL,
    OriginalFileName VARCHAR(500) NOT NULL,
    FileSize BIGINT NOT NULL,
    MimeType VARCHAR(200) NOT NULL,
    Category VARCHAR(100) NOT NULL,
    StoragePath VARCHAR(1000) NOT NULL,
    UploadedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    
    CONSTRAINT FK_Documents_Users FOREIGN KEY (UserId) 
        REFERENCES users(Id) ON DELETE CASCADE,
    
    INDEX IX_Documents_UserId (UserId),
    INDEX IX_Documents_Category (Category),
    INDEX IX_Documents_UploadedAt (UploadedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -------------------------------------------------------------------------
-- 4. Reminders Table
-- -------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS Reminders (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    CalendarEventId INT NULL,
    Title VARCHAR(500) NOT NULL,
    Description TEXT NULL,
    DueDate DATETIME(6) NOT NULL,
    IsCompleted TINYINT(1) NOT NULL DEFAULT 0,
    CompletedAt DATETIME(6) NULL,
    Priority INT NOT NULL DEFAULT 2,
    CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    
    CONSTRAINT FK_Reminders_Users FOREIGN KEY (UserId) 
        REFERENCES users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Reminders_CalendarEvents FOREIGN KEY (CalendarEventId) 
        REFERENCES CalendarEvents(Id) ON DELETE SET NULL,
    
    INDEX IX_Reminders_UserId (UserId),
    INDEX IX_Reminders_DueDate (DueDate),
    INDEX IX_Reminders_IsCompleted (IsCompleted)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -------------------------------------------------------------------------
-- 5. GoogleCalendarTokens Table
-- -------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS GoogleCalendarTokens (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL UNIQUE,
    AccessToken TEXT NOT NULL,
    RefreshToken TEXT NOT NULL,
    ExpiresAt DATETIME(6) NOT NULL,
    CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    UpdatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    
    CONSTRAINT FK_GoogleCalendarTokens_Users FOREIGN KEY (UserId) 
        REFERENCES users(Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -------------------------------------------------------------------------
-- Create test user (optional, for development)
-- Password: Test123! (bcrypt hash)
-- -------------------------------------------------------------------------

-- INSERT INTO users (Email, PasswordHash, FirstName, LastName) 
-- VALUES ('test@dhbw.de', '$2a$11$xE3r8VvTzOBLKwH6.KvuC.qH7UvVpQxJzJzR7tBnmwD0k4qVvXLmK', 'Test', 'User');
