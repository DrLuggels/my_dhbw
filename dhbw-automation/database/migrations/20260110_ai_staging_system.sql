-- Migration: AI Staging System für validierte Datenqualität
-- Datum: 2026-01-10
-- Beschreibung: Fügt Staging-Tabellen hinzu, damit AI bei unklaren Daten Rückfragen stellen kann

-- StagedEntities Tabelle: Hält AI-extrahierte Entitäten bis zur User-Bestätigung
CREATE TABLE IF NOT EXISTS StagedEntities (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    SourceDocumentId INT NULL,
    EntityType VARCHAR(50) NOT NULL COMMENT 'todo, meeting, project, learning_deficit',
    EntityData TEXT NOT NULL COMMENT 'JSON-serialisierte Entitätsdaten',
    ConfidenceScore INT NOT NULL COMMENT '0-100: AI Confidence Score',
    Status VARCHAR(50) NOT NULL DEFAULT 'pending_review' COMMENT 'pending_review, confirmed, modified, rejected',
    Priority VARCHAR(20) NOT NULL DEFAULT 'medium' COMMENT 'low, medium, high, urgent',
    IsPromoted BOOLEAN NOT NULL DEFAULT FALSE COMMENT 'Wurde bereits in Produktiv-DB übertragen?',
    PromotedEntityId INT NULL COMMENT 'ID in Produktiv-Tabelle nach Bestätigung',
    UserNotes TEXT NULL COMMENT 'Notizen/Korrekturen des Users',
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ReviewedAt DATETIME NULL,
    PromotedAt DATETIME NULL,

    -- Foreign Keys
    CONSTRAINT FK_StagedEntities_Users FOREIGN KEY (UserId)
        REFERENCES users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_StagedEntities_Documents FOREIGN KEY (SourceDocumentId)
        REFERENCES documents(Id) ON DELETE SET NULL,

    -- Indexes für Performance
    INDEX IX_StagedEntities_User_Status_Priority (UserId, Status, Priority),
    INDEX IX_StagedEntities_User_EntityType_Status (UserId, EntityType, Status),
    INDEX IX_StagedEntities_ConfidenceScore (ConfidenceScore),
    INDEX IX_StagedEntities_Status_CreatedAt (Status, CreatedAt),
    INDEX IX_StagedEntities_Promoted (IsPromoted, PromotedAt),

    -- Constraints
    CONSTRAINT CHK_StagedEntities_ConfidenceScore CHECK (ConfidenceScore BETWEEN 0 AND 100),
    CONSTRAINT CHK_StagedEntities_Status CHECK (Status IN ('pending_review', 'confirmed', 'modified', 'rejected')),
    CONSTRAINT CHK_StagedEntities_Priority CHECK (Priority IN ('low', 'medium', 'high', 'urgent')),
    CONSTRAINT CHK_StagedEntities_EntityType CHECK (EntityType IN ('todo', 'meeting', 'project', 'learning_deficit', 'reminder'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- AIQuestions Tabelle: Rückfragen der AI zu unklaren Feldern
CREATE TABLE IF NOT EXISTS AIQuestions (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    StagedEntityId INT NOT NULL,
    FieldName VARCHAR(100) NOT NULL COMMENT 'Feld-Name in EntityData, das unklar ist',
    QuestionText VARCHAR(500) NOT NULL COMMENT 'Die Frage an den User',
    SuggestedAnswers TEXT NULL COMMENT 'JSON-Array von vorgeschlagenen Antworten',
    Priority VARCHAR(20) NOT NULL DEFAULT 'medium' COMMENT 'critical, high, medium, low',
    IsAnswered BOOLEAN NOT NULL DEFAULT FALSE,
    UserAnswer TEXT NULL COMMENT 'Antwort des Users',
    AnswerType VARCHAR(20) NOT NULL DEFAULT 'text' COMMENT 'text, date, time, datetime, choice, number',
    ValidationPattern VARCHAR(255) NULL COMMENT 'Regex für Validierung',
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    AnsweredAt DATETIME NULL,

    -- Foreign Keys
    CONSTRAINT FK_AIQuestions_StagedEntities FOREIGN KEY (StagedEntityId)
        REFERENCES StagedEntities(Id) ON DELETE CASCADE,

    -- Indexes
    INDEX IX_AIQuestions_StagedEntity_IsAnswered (StagedEntityId, IsAnswered),
    INDEX IX_AIQuestions_StagedEntity_Priority (StagedEntityId, Priority),
    INDEX IX_AIQuestions_AnswerType (AnswerType),

    -- Constraints
    CONSTRAINT CHK_AIQuestions_Priority CHECK (Priority IN ('critical', 'high', 'medium', 'low')),
    CONSTRAINT CHK_AIQuestions_AnswerType CHECK (AnswerType IN ('text', 'date', 'time', 'datetime', 'choice', 'number'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Beispiel-Daten zum Testen (optional, kann entfernt werden nach Test)
-- HINWEIS: Nur für Development, NICHT für Production ausführen!

/*
-- Beispiel: Staged Todo mit unklaren Feldern
INSERT INTO StagedEntities (UserId, EntityType, EntityData, ConfidenceScore, Status, Priority) VALUES
(1, 'todo', '{"title":"Mathehausaufgabe machen","description":"Aufgabe 3.5","priority":"high"}', 75, 'pending_review', 'high');

SET @staged_id = LAST_INSERT_ID();

-- Frage: Wann ist die Deadline?
INSERT INTO AIQuestions (StagedEntityId, FieldName, QuestionText, SuggestedAnswers, Priority, AnswerType) VALUES
(@staged_id, 'dueDate', 'Wann möchtest du diese Aufgabe erledigen?',
 '["Heute Abend","Morgen","Nächste Woche","In 2 Wochen"]', 'high', 'choice');

-- Frage: Welches Fach?
INSERT INTO AIQuestions (StagedEntityId, FieldName, QuestionText, SuggestedAnswers, Priority, AnswerType) VALUES
(@staged_id, 'category', 'Zu welchem Fach gehört diese Aufgabe?',
 '["Mathematik","Informatik","BWL","Sonstiges"]', 'medium', 'choice');
*/

-- Erfolgsmeldung
SELECT 'AI Staging System Migration erfolgreich!' AS Status,
       '2 neue Tabellen: StagedEntities, AIQuestions' AS Details;
