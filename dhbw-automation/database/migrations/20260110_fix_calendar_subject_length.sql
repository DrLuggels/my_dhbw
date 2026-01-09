-- =========================================================================
-- DHBW Automation - Fix Calendar Subject Column Length
-- Migration: 20260110_fix_calendar_subject_length
-- Extends Subject column to accommodate longer Rapla event subjects
-- =========================================================================

USE dhbw_automation;

-- Add Subject column if it doesn't exist (for older schemas)
ALTER TABLE calendar_events 
ADD COLUMN IF NOT EXISTS Subject VARCHAR(500) NULL;

-- Extend Subject column to handle longer subjects from Rapla
ALTER TABLE calendar_events 
MODIFY COLUMN Subject VARCHAR(500) NULL;

-- Add EventType column if it doesn't exist
ALTER TABLE calendar_events 
ADD COLUMN IF NOT EXISTS EventType VARCHAR(100) NULL;

-- Add Professor column if it doesn't exist
ALTER TABLE calendar_events 
ADD COLUMN IF NOT EXISTS Professor VARCHAR(100) NULL;
