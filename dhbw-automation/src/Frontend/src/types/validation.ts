// Validation & Staging Types für TypeScript

export interface StagedEntity {
  id: number
  userId: number
  sourceDocumentId?: number
  entityType: 'todo' | 'meeting' | 'project' | 'learning_deficit' | 'reminder'
  entityData: string // JSON string
  confidenceScore: number // 0-100
  status: 'pending_review' | 'confirmed' | 'modified' | 'rejected'
  priority: 'low' | 'medium' | 'high' | 'urgent'
  isPromoted: boolean
  promotedEntityId?: number
  userNotes?: string
  createdAt: string
  reviewedAt?: string
  promotedAt?: string

  // Relations
  questions: AIQuestion[]
  sourceDocument?: {
    id: number
    fileName: string
    category: string
  }
}

export interface AIQuestion {
  id: number
  stagedEntityId: number
  fieldName: string // e.g., "meeting.suggestedDate", "todo.dueDate"
  questionText: string
  suggestedAnswers?: string // JSON array
  priority: 'critical' | 'high' | 'medium' | 'low'
  isAnswered: boolean
  userAnswer?: string
  answerType: 'text' | 'date' | 'time' | 'datetime' | 'choice' | 'number'
  validationPattern?: string
  createdAt: string
  answeredAt?: string
}

export interface PendingEntitiesResponse {
  count: number
  entities: StagedEntity[]
  summary: {
    highPriority: number
    withQuestions: number
    lowConfidence: number
  }
}

export interface AnswerQuestionsRequest {
  answers: Record<string, string> // fieldName -> answer
}

export interface ConfirmEntityRequest {
  userNotes?: string
}

export interface RejectEntityRequest {
  reason: string
}

export interface ModifyEntityRequest {
  modifiedData: string // JSON
}

export interface StagingStatistics {
  totalStaged: number
  totalConfirmed: number
  totalRejected: number
  totalModified: number
  averageConfidenceScore: number
  totalQuestions: number
  averageQuestionsPerEntity: number
  questionsByPriority: Record<string, number>
}

// Parsed entity data types
export interface ParsedTodo {
  title: string
  description?: string
  priority: 'low' | 'medium' | 'high' | 'urgent'
  category?: string
  suggestedDeadline?: string
  confidenceScore: number
}

export interface ParsedMeeting {
  personName: string
  purpose?: string
  suggestedDate?: string
  estimatedDurationMinutes: number
  confidenceScore: number
}

export interface ParsedProject {
  name: string
  description?: string
  estimatedPriority: 'low' | 'medium' | 'high'
  confidenceScore: number
}

export type ParsedEntityData = ParsedTodo | ParsedMeeting | ParsedProject

// UI Helper types
export interface EntityWithParsedData extends StagedEntity {
  parsedData: ParsedEntityData
}

export interface QuestionGroup {
  priority: 'critical' | 'high' | 'medium' | 'low'
  questions: AIQuestion[]
}
