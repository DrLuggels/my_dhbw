// Learning Engine Types (Knowledge Graph & Adaptive Questions)

export interface KgEntity {
  id: number
  entityType: string
  name: string
  description?: string
  subject?: string
  topic?: string
  confidenceScore: number
  importanceScore: number
  occurrenceCount: number
  isVerified: boolean
  documentId?: number
  documentName?: string
  chunkId?: number
  masteryScore?: number
  nextReview?: string
}

export interface KgRelationship {
  id: number
  sourceEntityId: number
  sourceEntityName: string
  targetEntityId: number
  targetEntityName: string
  relationshipType: string
  strength: number
  evidence?: string
  description?: string
  isVerified: boolean
}

export interface KnowledgeGraphStats {
  totalEntities: number
  totalRelationships: number
  entitiesByType: Record<string, number>
  relationshipsByType: Record<string, number>
  documentsCovered: number
  chunksCovered: number
}

export interface KnowledgeGraph {
  entities: KgEntity[]
  relationships: KgRelationship[]
  stats: KnowledgeGraphStats
}

export interface LearningDocumentResult {
  documentId: number
  documentName: string
  success: boolean
  errorMessage?: string
  chunksCreated: number
  entitiesExtracted: number
  relationshipsExtracted: number
  embeddingsGenerated: number
  warnings: string[]
  processingTime: string
}

export interface LearningQuestion {
  id: string
  questionType: string
  question: string
  options?: string[]
  correctAnswer: string
  explanation?: string
  bloomLevel: number
  difficulty: number
  entityId?: number
  entityName?: string
  sourceChunkId?: number
  sourceDocumentId?: number
  sourceDocumentName?: string
  hint?: string
  relatedConcepts?: string[]
}

export interface QuestionGenerationRequest {
  documentIds?: number[]
  entityIds?: number[]
  subject?: string
  topic?: string
  count: number
  difficulty: 'easy' | 'medium' | 'hard' | 'adaptive'
  questionTypes?: string[]
  minBloomLevel?: number
  maxBloomLevel?: number
}

export interface AnswerSubmission {
  questionId: string
  entityId?: number
  userAnswer: string
  responseTimeSeconds?: number
  questionType?: string
  bloomLevel?: number
}

export interface AnswerFeedback {
  isCorrect: boolean
  correctAnswer: string
  explanation?: string
  feedback?: string
  newMasteryScore: number
  masteryChange: number
  nextReview?: string
  relatedTopicsToStudy?: string[]
}

export interface WeakArea {
  entityId: number
  entityName: string
  entityType: string
  subject?: string
  topic?: string
  masteryScore: number
  attempts: number
  correct: number
  successRate: number
  reason: 'low_mastery' | 'overdue' | 'high_error_rate'
  priority: number
}

export interface MasteryStats {
  totalEntities: number
  masteredEntities: number
  learningEntities: number
  newEntities: number
  averageMastery: number
  totalAttempts: number
  totalCorrect: number
  overallSuccessRate: number
  bySubject: Record<string, SubjectMastery>
  byBloomLevel: Record<number, number>
  currentStreak: number
  bestStreak: number
}

export interface SubjectMastery {
  subject: string
  totalEntities: number
  masteredEntities: number
  averageMastery: number
  attempts: number
  correct: number
}

export interface ProcessingOptions {
  extractEntities: boolean
  extractRelationships: boolean
  generateEmbeddings: boolean
  useSemanticChunking: boolean
  targetChunkSize: number
  chunkOverlap: number
  entityConfidenceThreshold: number
  relationshipStrengthThreshold: number
}

// Entity type constants
export const entityTypes = [
  { value: 'concept', label: 'Konzept', icon: 'mdi-lightbulb', color: 'primary' },
  { value: 'definition', label: 'Definition', icon: 'mdi-book-open', color: 'info' },
  { value: 'formula', label: 'Formel', icon: 'mdi-function', color: 'warning' },
  { value: 'theorem', label: 'Theorem', icon: 'mdi-scale-balance', color: 'success' },
  { value: 'method', label: 'Methode', icon: 'mdi-cog', color: 'secondary' },
  { value: 'example', label: 'Beispiel', icon: 'mdi-file-document', color: 'accent' },
  { value: 'person', label: 'Person', icon: 'mdi-account', color: 'grey' },
  { value: 'algorithm', label: 'Algorithmus', icon: 'mdi-code-braces', color: 'deep-purple' }
]

export const relationshipTypes = [
  { value: 'is_a', label: 'ist ein', description: 'Klassifikation' },
  { value: 'part_of', label: 'Teil von', description: 'Komposition' },
  { value: 'relates_to', label: 'verwandt mit', description: 'Allgemeine Beziehung' },
  { value: 'requires', label: 'benötigt', description: 'Voraussetzung' },
  { value: 'example_of', label: 'Beispiel für', description: 'Illustration' },
  { value: 'uses', label: 'verwendet', description: 'Nutzung' },
  { value: 'extends', label: 'erweitert', description: 'Spezialisierung' }
]

export const bloomLevels = [
  { level: 1, name: 'Erinnern', description: 'Fakten abrufen, definieren' },
  { level: 2, name: 'Verstehen', description: 'Erklären, interpretieren' },
  { level: 3, name: 'Anwenden', description: 'In neuen Situationen anwenden' },
  { level: 4, name: 'Analysieren', description: 'Unterscheiden, vergleichen' },
  { level: 5, name: 'Bewerten', description: 'Begründen, kritisieren' },
  { level: 6, name: 'Erschaffen', description: 'Entwickeln, produzieren' }
]

export const questionTypes = [
  { value: 'mc', label: 'Multiple Choice', icon: 'mdi-checkbox-marked-circle' },
  { value: 'fill_blank', label: 'Lückentext', icon: 'mdi-form-textbox' },
  { value: 'true_false', label: 'Wahr/Falsch', icon: 'mdi-check-circle' },
  { value: 'short_answer', label: 'Kurzantwort', icon: 'mdi-text-short' },
  { value: 'connection', label: 'Verbindung', icon: 'mdi-relation-many-to-many' }
]

// Helper functions
export const getEntityTypeInfo = (type: string) => {
  return entityTypes.find(t => t.value === type) || { value: type, label: type, icon: 'mdi-help', color: 'grey' }
}

export const getRelationshipTypeInfo = (type: string) => {
  return relationshipTypes.find(t => t.value === type) || { value: type, label: type, description: '' }
}

export const getBloomLevelInfo = (level: number) => {
  return bloomLevels.find(b => b.level === level) || { level, name: `Level ${level}`, description: '' }
}

export const getMasteryColor = (score: number): string => {
  if (score >= 0.8) return 'success'
  if (score >= 0.5) return 'warning'
  if (score >= 0.3) return 'orange'
  return 'error'
}

export const getMasteryLabel = (score: number): string => {
  if (score >= 0.8) return 'Gemeistert'
  if (score >= 0.5) return 'Lernend'
  if (score >= 0.3) return 'Anfänger'
  return 'Neu'
}

export const formatMasteryPercent = (score: number): string => {
  return `${Math.round(score * 100)}%`
}
