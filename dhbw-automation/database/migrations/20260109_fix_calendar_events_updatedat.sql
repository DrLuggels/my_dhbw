-- =========================================================================
-- DHBW Automation - Fix calendar_events UpdatedAt column
-- Migration: 20260109_fix_calendar_events_updatedat
-- =========================================================================

USE dhbw_automation;

-- Fix UpdatedAt column to have DEFAULT value
ALTER TABLE calendar_events 
MODIFY COLUMN UpdatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6);
