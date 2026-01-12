-- Smart Reference System Migration
-- Enables linking notes to calendar events via natural language references
-- Author: Claude Code
-- Date: 2026-01-12

-- ===================================
-- Extend knowledge_links with reference text
-- ===================================

ALTER TABLE knowledge_links
ADD COLUMN IF NOT EXISTS reference_text VARCHAR(500) NULL
COMMENT 'Original natural language reference that created this link';

-- ===================================
-- Smart Reference Cache Table
-- Caches resolved temporal expressions for performance
-- ===================================

CREATE TABLE IF NOT EXISTS smart_reference_cache (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    expression_hash VARCHAR(64) NOT NULL COMMENT 'SHA256 hash of the expression',
    original_expression VARCHAR(500) NOT NULL,
    resolved_start DATETIME NULL,
    resolved_end DATETIME NULL,
    resolved_entity_type VARCHAR(50) NULL,
    resolved_entity_id INT NULL,
    confidence FLOAT DEFAULT 0.0,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    expires_at DATETIME NULL,

    UNIQUE KEY idx_user_expression (user_id, expression_hash),
    KEY idx_expires (expires_at),

    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Cache for resolved temporal expressions';

-- ===================================
-- Extend calendar_events for note embeddings
-- ===================================

ALTER TABLE calendar_events
ADD COLUMN IF NOT EXISTS notes_has_embedding BOOLEAN DEFAULT FALSE
COMMENT 'Whether the notes field has been embedded';

ALTER TABLE calendar_events
ADD COLUMN IF NOT EXISTS notes_qdrant_point_id VARCHAR(100) NULL
COMMENT 'Qdrant vector DB point ID for notes embedding';

-- ===================================
-- Index for faster professor/subject queries
-- ===================================

CREATE INDEX IF NOT EXISTS idx_calendar_professor ON calendar_events(user_id, professor);
CREATE INDEX IF NOT EXISTS idx_calendar_subject ON calendar_events(user_id, subject);

-- ===================================
-- View for smart reference statistics
-- ===================================

CREATE OR REPLACE VIEW v_smart_reference_stats AS
SELECT
    u.id AS user_id,
    u.email,
    COUNT(DISTINCT ce.professor) AS unique_professors,
    COUNT(DISTINCT ce.subject) AS unique_subjects,
    COUNT(ce.id) AS total_events,
    COUNT(CASE WHEN ce.notes IS NOT NULL AND ce.notes != '' THEN 1 END) AS events_with_notes,
    COUNT(CASE WHEN kl.link_type IN ('professor_reference', 'subject_reference', 'temporal_reference', 'professor_temporal_reference') THEN 1 END) AS smart_links_count
FROM users u
LEFT JOIN calendar_events ce ON u.id = ce.user_id
LEFT JOIN knowledge_links kl ON u.id = kl.user_id
GROUP BY u.id, u.email;

-- ===================================
-- Log entry for migration
-- ===================================

INSERT INTO migrations (name, executed_at)
VALUES ('20260112_smart_reference_system', NOW())
ON DUPLICATE KEY UPDATE executed_at = NOW();
