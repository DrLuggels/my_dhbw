// OmniLearning Types - Omnifunktionales Lernsystem

export interface OmniEntity {
  id: number
  name: string
  entityType: string
  subject: string
  topic: string
  subtopic?: string
  description?: string
  masteryScore: number
  currentBloomLevel: number
  importanceScore: number
  lastInteraction: string
  nextReviewDate: string
  totalAttempts: number
  totalCorrect: number
  isActive: boolean
  hasEmbedding: boolean
  tags: string[]
}

export interface OmniRelationship {
  id: number
  sourceEntityId: number
  targetEntityId: number
  relationshipType: string
  currentStrength: number
  isPrerequisite: boolean
  createdAt: string
}

export interface OmniExercise {
  id: number
  entityIds: number[]
  subject: string
  topic: string
  difficulty: string
  bloomLevel: number
  bloomLevelName: string
  componentType: string
  question: string
  config: any
  correctAnswer: string
  explanation?: string
  hint?: string
  createdAt: string
}

export interface ExerciseSubmissionResult {
  isCorrect: boolean
  score: number
  feedback: string
  explanation?: string
  nextReviewDate: string
  newMasteryScore: number
  newBloomLevel: number
  fsrsUpdate?: {
    nextInterval: number
    newDifficulty: number
    newStability: number
  }
}

export interface Priority {
  entityId?: number
  entityName?: string
  subject?: string
  topic?: string
  compositeScore: number
  rank: number
  isBlocked: boolean
  blockReason?: string
  recommendedAction?: string
  deadline?: string
}

export interface WeakArea {
  entityId: number
  entityName: string
  subject: string
  topic: string
  masteryScore: number
  failureRate: number
  lastAttempt: string
  suggestedAction: string
}

export interface OverdueItem {
  entityId: number
  entityName: string
  subject: string
  topic: string
  daysPastDue: number
  masteryScore: number
  decayedScore: number
}

export interface MasteryStats {
  totalEntities: number
  masteredEntities: number
  learningEntities: number
  newEntities: number
  totalExercises: number
  correctAnswers: number
  overallMastery: number
  overallSuccessRate: number
  bySubject: Record<string, SubjectStats>
  byBloomLevel: Record<number, number>
}

export interface SubjectStats {
  subject: string
  entityCount: number
  averageMastery: number
  exerciseCount: number
  successRate: number
}

export interface LearningStreak {
  currentStreak: number
  bestStreak: number
  lastActivityDate?: string
  totalActiveDays: number
  recentActivityDates: string[]
}

export interface DifficultyDistribution {
  easyTotal: number
  easyCorrect: number
  easySuccessRate: number
  mediumTotal: number
  mediumCorrect: number
  mediumSuccessRate: number
  hardTotal: number
  hardCorrect: number
  hardSuccessRate: number
  followsTwentyFortyForty: boolean
  recommendedDifficulty: string
  distributionAdvice: string
}

export interface BloomProgression {
  currentLevel: number
  currentLevelName: string
  targetLevel: number
  targetLevelName: string
  canAdvance: boolean
  progressAdvice: string
  levelStats: Record<number, BloomLevelStats>
}

export interface BloomLevelStats {
  level: number
  name: string
  attempts: number
  correct: number
  successRate: number
  isMastered: boolean
}

export interface KnowledgeGraph {
  nodes: GraphNode[]
  edges: GraphEdge[]
  metadata: GraphMetadata
}

export interface GraphNode {
  id: number
  label: string
  entityType: string
  subject: string
  topic: string
  masteryScore: number
  size: number
  color: string
  x: number
  y: number
}

export interface GraphEdge {
  id: number
  source: number
  target: number
  relationshipType: string
  strength: number
  isPrerequisite: boolean
}

export interface GraphMetadata {
  totalNodes: number
  totalEdges: number
  subjectCount: number
  averageMastery: number
  subjects: string[]
}

export interface ClusterVisualization {
  clusters: ClusterInfo[]
  points: ClusterPoint[]
}

export interface ClusterInfo {
  id: string
  label: string
  centerX: number
  centerY: number
  entityCount: number
  averageMastery: number
}

export interface ClusterPoint {
  entityId: number
  label: string
  x: number
  y: number
  clusterId: string
  masteryScore: number
}

// UI Helper Functions
export const bloomLevelNames: Record<number, string> = {
  1: 'Erinnern',
  2: 'Verstehen',
  3: 'Anwenden',
  4: 'Analysieren',
  5: 'Bewerten',
  6: 'Erschaffen'
}

export const getBloomLevelName = (level: number): string => {
  return bloomLevelNames[level] || `Level ${level}`
}

export const getBloomLevelColor = (level: number): string => {
  const colors: Record<number, string> = {
    1: '#4CAF50', // Gruen - Erinnern
    2: '#8BC34A', // Hell-Gruen - Verstehen
    3: '#FFEB3B', // Gelb - Anwenden
    4: '#FF9800', // Orange - Analysieren
    5: '#FF5722', // Rot-Orange - Bewerten
    6: '#9C27B0'  // Lila - Erschaffen
  }
  return colors[level] || '#9E9E9E'
}

export const getMasteryColor = (score: number): string => {
  if (score >= 0.8) return '#4CAF50' // Gruen - Gemeistert
  if (score >= 0.5) return '#FFC107' // Gelb - In Arbeit
  if (score >= 0.3) return '#FF9800' // Orange - Anfaenger
  return '#F44336' // Rot - Neu/Schwach
}

export const getMasteryLabel = (score: number): string => {
  if (score >= 0.8) return 'Gemeistert'
  if (score >= 0.5) return 'Fortgeschritten'
  if (score >= 0.3) return 'Lernend'
  return 'Neu'
}

export const getDifficultyLabel = (difficulty: string): string => {
  const labels: Record<string, string> = {
    easy: 'Leicht',
    medium: 'Mittel',
    hard: 'Schwer'
  }
  return labels[difficulty] || difficulty
}

export const getDifficultyColor = (difficulty: string): string => {
  const colors: Record<string, string> = {
    easy: 'success',
    medium: 'warning',
    hard: 'error'
  }
  return colors[difficulty] || 'default'
}

export const formatDate = (date: string): string => {
  return new Date(date).toLocaleDateString('de-DE', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric'
  })
}

export const formatDateTime = (date: string): string => {
  return new Date(date).toLocaleDateString('de-DE', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

export const getRelativeTime = (date: string): string => {
  const now = new Date()
  const target = new Date(date)
  const diffMs = target.getTime() - now.getTime()
  const diffDays = Math.ceil(diffMs / (1000 * 60 * 60 * 24))

  if (diffDays < 0) return `${Math.abs(diffDays)} Tage ueberfaellig`
  if (diffDays === 0) return 'Heute faellig'
  if (diffDays === 1) return 'Morgen faellig'
  if (diffDays <= 7) return `In ${diffDays} Tagen`
  return formatDate(date)
}

export const exerciseTypes = [
  { value: 'multiple_choice', text: 'Multiple Choice', icon: 'mdi-checkbox-marked-circle' },
  { value: 'fill_blank', text: 'Lueckentext', icon: 'mdi-form-textbox' },
  { value: 'drag_drop', text: 'Drag & Drop', icon: 'mdi-drag' },
  { value: 'slider', text: 'Schieberegler', icon: 'mdi-tune' },
  { value: 'code_editor', text: 'Code-Editor', icon: 'mdi-code-braces' },
  { value: 'text_input', text: 'Freitext', icon: 'mdi-text' }
]

export const difficultyItems = [
  { value: 'easy', text: 'Leicht (20%)', color: 'success' },
  { value: 'medium', text: 'Mittel (40%)', color: 'warning' },
  { value: 'hard', text: 'Schwer (40%)', color: 'error' }
]

export const sessionTypes = [
  { value: 'review', text: 'Wiederholung', icon: 'mdi-refresh', description: 'Faellige Themen wiederholen' },
  { value: 'weakness', text: 'Schwachstellen', icon: 'mdi-alert', description: 'Schwache Bereiche staerken' },
  { value: 'mixed', text: 'Gemischt', icon: 'mdi-shuffle', description: 'Optimale Mischung' },
  { value: 'bloom_progression', text: 'Bloom-Fortschritt', icon: 'mdi-stairs', description: 'Naechstes Level erreichen' }
]
