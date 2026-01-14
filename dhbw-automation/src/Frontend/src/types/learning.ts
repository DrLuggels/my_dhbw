export interface LearningDeficit {
  id: number
  userId: number
  subject: string
  topic: string
  subtopic?: string
  errorType: string
  errorDescription: string
  occurrenceCount: number
  firstOccurrence: string
  lastOccurrence: string
  severity: string
  needsTutoring: boolean
  relatedDocumentIds: string
  createdAt: string
  resolvedAt?: string
}

export interface Exercise {
  id: number
  userId: number
  deficitId?: number
  subject: string
  topic: string
  exerciseType: string
  question: string
  helpText?: string
  correctAnswer: string
  explanation?: string
  difficulty: string
  userAnswer?: string
  isCorrect?: boolean
  answeredAt?: string
  nextReviewDate: string
  reviewCount: number
  easeFactor: number
  createdAt: string
  // UI state
  userInput?: string
  showHelp?: boolean
  answered?: boolean
}

export interface LearningStats {
  totalDeficits: number
  activeDeficits: number
  resolvedDeficits: number
  highSeverityDeficits: number
  totalExercises: number
  completedExercises: number
  pendingExercises: number
  dueExercises: number
  averageEaseFactor: number
}

export interface InteractiveExerciseData {
  id: number
  subject: string
  topic: string
  difficulty: string
  exerciseContent: string
  stepProgress: string
  completedSteps: number
  totalSteps: number
  score: number
  nextReviewDate: string
}

export const exerciseModeItems = [
  { value: 'learning', text: 'Lernen', icon: 'mdi-school' },
  { value: 'exam_prep', text: 'KA-Vorbereitung', icon: 'mdi-file-document' },
  { value: 'exam_simulation', text: 'Prüfungssimulation', icon: 'mdi-timer' }
]

export const difficultyItems = [
  { value: 'easy', text: 'Leicht - Einführung & Grundlagen' },
  { value: 'medium', text: 'Mittel - Anwendung & Vertiefung' },
  { value: 'hard', text: 'Schwer - Komplexe Szenarien' }
]

export const getDeficitSeverityColor = (severity: string): string => {
  const colors: Record<string, string> = {
    critical: 'error',
    high: 'warning',
    medium: 'info',
    low: 'success'
  }
  return colors[severity] || 'default'
}

export const getSeverityLabel = (severity: string): string => {
  const labels: Record<string, string> = {
    critical: 'Kritisch',
    high: 'Hoch',
    medium: 'Mittel',
    low: 'Niedrig'
  }
  return labels[severity] || severity
}

export const getDifficultyColor = (difficulty: string): string => {
  const colors: Record<string, string> = {
    easy: 'success',
    medium: 'warning',
    hard: 'error'
  }
  return colors[difficulty] || 'default'
}

export const formatLearningDate = (date: string): string => {
  return new Date(date).toLocaleDateString('de-DE', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}
