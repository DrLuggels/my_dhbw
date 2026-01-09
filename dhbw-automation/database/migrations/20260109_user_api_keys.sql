-- =========================================================================
-- DHBW Automation - User API Keys
-- Migration: 20260109_user_api_keys
-- Fügt benutzerspezifische AI API-Keys zur users-Tabelle hinzu
-- =========================================================================

USE dhbw_automation;

-- -------------------------------------------------------------------------
-- Add API Key columns to users table
-- -------------------------------------------------------------------------

ALTER TABLE users
ADD COLUMN OpenAiApiKey VARCHAR(500) NULL COMMENT 'Encrypted OpenAI API Key',
ADD COLUMN AnthropicApiKey VARCHAR(500) NULL COMMENT 'Encrypted Anthropic API Key',
ADD COLUMN GeminiApiKey VARCHAR(500) NULL COMMENT 'Encrypted Google Gemini API Key';

-- -------------------------------------------------------------------------
-- Create index for faster lookups
-- -------------------------------------------------------------------------

CREATE INDEX IX_Users_ApiKeys ON users(Id, OpenAiApiKey(100), AnthropicApiKey(100), GeminiApiKey(100));

-- =========================================================================
-- Migration Complete
-- =========================================================================

SELECT 'Migration 20260109_user_api_keys completed successfully' AS Status;
