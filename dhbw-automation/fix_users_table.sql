-- Fix all database tables to match Entity Framework models

-- 1. CalendarEvents: Add EventType, Subject, Professor
ALTER TABLE CalendarEvents 
ADD COLUMN EventType VARCHAR(100) NULL AFTER IsAllDay,
ADD COLUMN Subject VARCHAR(100) NULL AFTER EventType,
ADD COLUMN Professor VARCHAR(100) NULL AFTER Subject;

-- 2. Documents: Rename/Add columns to match model
-- Model expects: FilePath, FileType, but DB has: StoragePath, Category
-- Keep both for now, add missing ones
ALTER TABLE Documents
ADD COLUMN FilePath VARCHAR(500) NULL AFTER FileName,
ADD COLUMN FileType VARCHAR(100) NULL AFTER FilePath;

-- Update existing data
UPDATE Documents SET FilePath = StoragePath WHERE FilePath IS NULL;
UPDATE Documents SET FileType = MimeType WHERE FileType IS NULL;

-- 3. Reminders: Add Status column (model expects Status, DB has IsCompleted)
ALTER TABLE Reminders
ADD COLUMN Status VARCHAR(50) NOT NULL DEFAULT 'pending' AFTER Priority;

-- Update Status based on IsCompleted
UPDATE Reminders SET Status = 'completed' WHERE IsCompleted = 1;
UPDATE Reminders SET Status = 'pending' WHERE IsCompleted = 0;

