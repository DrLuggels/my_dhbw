# TASK: Implement DeepTutor-Style Learning Engine in my_dhbw

## Context
This is a DHBW study automation system built with:
- Backend: .NET 8 Web API
- Frontend: Vue.js 3
- Databases: MariaDB + Qdrant (Vector DB) + Redis
- Storage: MinIO
- AI: OpenAI, Claude, Gemini via Multi-AI Gateway

## Goal
Implement a DeepTutor-inspired learning module that:
1. Processes uploaded documents into chunks with vector embeddings AND relationship mappings (knowledge graph)
2. Automatically generates adaptive exercises that train at the user's maximum difficulty level
3. Integrates with existing Qdrant vector store and AI gateway

## Required Features

### 1. Document Processing Pipeline
```
Upload → PDF/MD/TXT Parser → Chunking (semantic) → Embeddings → Qdrant
                                    ↓
                          Entity Extraction → Knowledge Graph (Neo4j or in MariaDB)
                                    ↓
                          Relationship Mapping (Concept A relates to Concept B)
```

- Chunk size: ~512 tokens with 50 token overlap
- Extract entities: concepts, definitions, formulas, people, dates
- Map relationships: "is-a", "part-of", "relates-to", "requires", "contradicts"

### 2. Knowledge Graph Schema (MariaDB or separate Neo4j)
```sql
-- Entities
CREATE TABLE kg_entities (
    id INT PRIMARY KEY AUTO_INCREMENT,
    document_id INT,
    chunk_id INT,
    entity_type ENUM('concept', 'definition', 'formula', 'person', 'date', 'example'),
    name VARCHAR(255),
    description TEXT,
    embedding_id VARCHAR(255) -- Reference to Qdrant
);

-- Relationships
CREATE TABLE kg_relationships (
    id INT PRIMARY KEY AUTO_INCREMENT,
    source_entity_id INT,
    target_entity_id INT,
    relationship_type ENUM('is_a', 'part_of', 'relates_to', 'requires', 'contradicts', 'example_of'),
    strength FLOAT DEFAULT 1.0,
    extracted_from_chunk INT
);
```

### 3. Adaptive Question Generator
Implement question types:
- **Multiple Choice** (4 options, 1 correct)
- **Fill-in-the-Blank** (cloze deletion)
- **True/False** with explanation requirement
- **Short Answer** (1-2 sentences)
- **Calculation/Application** (for formulas)
- **Connection Questions** ("How does X relate to Y?")

Difficulty scaling based on:
- User's past performance per topic (track in DB)
- Bloom's Taxonomy level (Remember → Understand → Apply → Analyze → Evaluate → Create)
- Knowledge graph depth (surface concepts vs. deep connections)

### 4. Dual-Loop Reasoning (for Tutor Mode)
```
Analysis Loop:
  1. InvestigateAgent: Parse question, identify required knowledge
  2. NoteAgent: Retrieve relevant chunks from Qdrant + traverse knowledge graph
  
Solve Loop:
  1. PlanAgent: Create solution strategy
  2. SolveAgent: Execute step-by-step with RAG context
  3. CheckAgent: Verify answer against source material
```

### 5. API Endpoints
```
POST /api/learning/process-document
  - Triggers chunking + embedding + KG extraction

GET /api/learning/knowledge-graph/{documentId}
  - Returns entity-relationship graph for visualization

POST /api/learning/generate-questions
  Body: { documentIds: [], count: 10, difficulty: "adaptive", types: ["mc", "fill", "short"] }
  - Returns generated questions with source citations

POST /api/learning/submit-answer
  Body: { questionId, userAnswer }
  - Returns feedback + updates user performance model

GET /api/learning/weak-areas/{userId}
  - Returns topics needing more practice based on performance

POST /api/learning/tutor-solve
  Body: { question, knowledgeBaseIds: [] }
  - Returns step-by-step solution with dual-loop reasoning
```

### 6. User Performance Tracking
```sql
CREATE TABLE user_performance (
    id INT PRIMARY KEY AUTO_INCREMENT,
    user_id INT,
    entity_id INT, -- Which concept
    question_type VARCHAR(50),
    bloom_level INT, -- 1-6
    attempts INT DEFAULT 0,
    correct INT DEFAULT 0,
    last_attempt DATETIME,
    mastery_score FLOAT DEFAULT 0.0, -- 0.0 to 1.0
    next_review DATETIME -- Spaced repetition
);
```

Mastery algorithm: Modified FSRS (Free Spaced Repetition Scheduler)

## Technical Constraints
- Use existing `AiGatewayService` for all LLM calls
- Use existing `QdrantService` for vector operations
- Follow existing project patterns (see /dhbw-automation folder structure)
- All new code in `/dhbw-automation/src/Learning/` namespace
- Vue components in `/dhbw-automation/frontend/src/views/Learning/`

## Implementation Order
1. Document processing pipeline + chunking
2. Entity extraction (use Claude for NER)
3. Knowledge graph storage + API
4. Basic question generation (MC + Fill-in-blank)
5. User performance tracking
6. Adaptive difficulty algorithm
7. Dual-loop tutor mode
8. Frontend components

## Example Prompts for AI Question Generation

### Multiple Choice Generation
```
Based on this content:
{chunk_content}

Generate a multiple choice question that tests understanding at Bloom level {level}.
The question should focus on: {entity_name}

Return JSON:
{
  "question": "...",
  "options": ["A) ...", "B) ...", "C) ...", "D) ..."],
  "correct": "A",
  "explanation": "...",
  "source_chunk_id": "...",
  "bloom_level": 3,
  "difficulty": 0.7
}
```

### Knowledge Graph Extraction
```
Extract entities and relationships from this text chunk:
{chunk_content}

Return JSON:
{
  "entities": [
    {"name": "...", "type": "concept|definition|formula|person|date", "description": "..."}
  ],
  "relationships": [
    {"source": "Entity A", "target": "Entity B", "type": "relates_to|requires|part_of|is_a", "evidence": "..."}
  ]
}
```

Start with the document processing pipeline and work incrementally. Ask clarifying questions if the existing codebase structure is unclear.