-- Knowledge Base System für periodische Grundlagen-Reviews
-- Created: 2026-01-09

-- Create knowledge_base_items table
CREATE TABLE IF NOT EXISTS knowledge_base_items (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id INTEGER NOT NULL,
    subject VARCHAR(100) NOT NULL,
    topic VARCHAR(200) NOT NULL,
    category VARCHAR(50) NOT NULL DEFAULT 'grundlagen', -- 'grundlagen', 'advanced', 'important'
    importance VARCHAR(50) NOT NULL DEFAULT 'medium', -- 'low', 'medium', 'high'
    last_tested_date DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    test_count INTEGER NOT NULL DEFAULT 0,
    average_score REAL NOT NULL DEFAULT 0.0,
    last_score REAL NOT NULL DEFAULT 0.0,
    next_review_date DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_active BOOLEAN NOT NULL DEFAULT 1,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS idx_knowledge_base_user_subject_topic 
    ON knowledge_base_items(user_id, subject, topic);

CREATE INDEX IF NOT EXISTS idx_knowledge_base_user_next_review 
    ON knowledge_base_items(user_id, next_review_date);

CREATE INDEX IF NOT EXISTS idx_knowledge_base_user_active_review 
    ON knowledge_base_items(user_id, is_active, next_review_date);

CREATE INDEX IF NOT EXISTS idx_knowledge_base_user_last_tested 
    ON knowledge_base_items(user_id, last_tested_date);

-- Add knowledge base fields to generated_exercises table
ALTER TABLE generated_exercises 
    ADD COLUMN knowledge_base_item_id INTEGER DEFAULT NULL;

ALTER TABLE generated_exercises 
    ADD COLUMN is_periodic_review BOOLEAN NOT NULL DEFAULT 0;

-- Foreign key constraint for knowledge base item
CREATE INDEX IF NOT EXISTS idx_generated_exercises_knowledge_base_item 
    ON generated_exercises(knowledge_base_item_id);

-- Trigger for updating updated_at timestamp
CREATE TRIGGER IF NOT EXISTS update_knowledge_base_items_timestamp 
    AFTER UPDATE ON knowledge_base_items
    FOR EACH ROW
BEGIN
    UPDATE knowledge_base_items 
    SET updated_at = CURRENT_TIMESTAMP 
    WHERE id = NEW.id;
END;

-- Initial Knowledge Base Items (Beispiel-Daten für DHBW Informatik)
INSERT INTO knowledge_base_items 
    (user_id, subject, topic, category, importance, last_tested_date, next_review_date) 
VALUES
    -- Programmierung Grundlagen
    (1, 'Programmierung', 'Datentypen und Variablen', 'grundlagen', 'high', datetime('now', '-60 days'), datetime('now')),
    (1, 'Programmierung', 'Kontrollstrukturen (if/else/while)', 'grundlagen', 'high', datetime('now', '-60 days'), datetime('now')),
    (1, 'Programmierung', 'Funktionen und Parameter', 'grundlagen', 'high', datetime('now', '-60 days'), datetime('now')),
    (1, 'Programmierung', 'Arrays und Listen', 'grundlagen', 'high', datetime('now', '-60 days'), datetime('now')),
    (1, 'Programmierung', 'Objektorientierung Basics', 'grundlagen', 'high', datetime('now', '-60 days'), datetime('now')),
    
    -- Datenbanken Grundlagen
    (1, 'Datenbanken', 'SQL SELECT Basics', 'grundlagen', 'high', datetime('now', '-60 days'), datetime('now')),
    (1, 'Datenbanken', 'SQL JOINs', 'grundlagen', 'high', datetime('now', '-60 days'), datetime('now')),
    (1, 'Datenbanken', 'Normalisierung', 'grundlagen', 'medium', datetime('now', '-60 days'), datetime('now')),
    (1, 'Datenbanken', 'Primär- und Fremdschlüssel', 'grundlagen', 'high', datetime('now', '-60 days'), datetime('now')),
    
    -- Algorithmen Grundlagen
    (1, 'Algorithmen', 'Sortieralgorithmen', 'grundlagen', 'medium', datetime('now', '-60 days'), datetime('now')),
    (1, 'Algorithmen', 'Suchalgorithmen', 'grundlagen', 'medium', datetime('now', '-60 days'), datetime('now')),
    (1, 'Algorithmen', 'Big-O Notation', 'grundlagen', 'high', datetime('now', '-60 days'), datetime('now')),
    
    -- Mathematik Grundlagen
    (1, 'Mathematik', 'Lineare Algebra Basics', 'grundlagen', 'medium', datetime('now', '-60 days'), datetime('now')),
    (1, 'Mathematik', 'Statistik Grundlagen', 'grundlagen', 'medium', datetime('now', '-60 days'), datetime('now')),
    (1, 'Mathematik', 'Wahrscheinlichkeitsrechnung', 'grundlagen', 'medium', datetime('now', '-60 days'), datetime('now')),
    
    -- Web Development Grundlagen
    (1, 'Web Development', 'HTTP Basics', 'grundlagen', 'high', datetime('now', '-60 days'), datetime('now')),
    (1, 'Web Development', 'REST APIs', 'grundlagen', 'high', datetime('now', '-60 days'), datetime('now')),
    (1, 'Web Development', 'HTML/CSS Basics', 'grundlagen', 'medium', datetime('now', '-60 days'), datetime('now')),
    (1, 'Web Development', 'JavaScript Basics', 'grundlagen', 'high', datetime('now', '-60 days'), datetime('now')),
    
    -- Software Engineering Grundlagen
    (1, 'Software Engineering', 'Clean Code Prinzipien', 'grundlagen', 'high', datetime('now', '-60 days'), datetime('now')),
    (1, 'Software Engineering', 'Design Patterns', 'advanced', 'medium', datetime('now', '-60 days'), datetime('now')),
    (1, 'Software Engineering', 'SOLID Prinzipien', 'advanced', 'medium', datetime('now', '-60 days'), datetime('now')),
    (1, 'Software Engineering', 'Git Basics', 'grundlagen', 'high', datetime('now', '-60 days'), datetime('now'))
ON CONFLICT DO NOTHING;

-- Commit transaction
COMMIT;
