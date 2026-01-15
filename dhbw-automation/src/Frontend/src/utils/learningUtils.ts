/**
 * Zentrale Lern-Utilities
 *
 * Konsolidierte Helper-Funktionen fuer alle Lern-Views:
 * - LearningView (Defizit-Management)
 * - OmniLernenView (Session-basiertes Lernen)
 * - KnowledgeNetworkView (Wissens-Graph)
 */

// Re-export von omniLearning.ts fuer einheitlichen Import
export {
  // Bloom's Taxonomy
  bloomLevelNames,
  getBloomLevelName,
  getBloomLevelColor,

  // Mastery
  getMasteryColor,
  getMasteryLabel,

  // Difficulty
  getDifficultyLabel,
  getDifficultyColor,
  difficultyItems,

  // Date/Time
  formatDate,
  formatDateTime,
  getRelativeTime,

  // Exercise Types
  exerciseTypes,
  sessionTypes
} from '@/types/omniLearning'

// === Zusaetzliche konsolidierte Utilities ===

/**
 * Einheitliche Terminologie fuer Lern-Status
 * "Prioritaet" = "Defizit" = "Lernbedarf"
 */
export const getLearningStatusLabel = (status: string): string => {
  const labels: Record<string, string> = {
    new: 'Neu',
    learning: 'In Bearbeitung',
    reviewing: 'Zur Wiederholung',
    mastered: 'Gemeistert',
    blocked: 'Blockiert',
    overdue: 'Ueberfaellig'
  }
  return labels[status] || status
}

export const getLearningStatusColor = (status: string): string => {
  const colors: Record<string, string> = {
    new: 'grey',
    learning: 'info',
    reviewing: 'warning',
    mastered: 'success',
    blocked: 'error',
    overdue: 'error'
  }
  return colors[status] || 'default'
}

/**
 * Prioritaets-Score zu Label
 * Verwendet fuer Defizite, Priorities, Weak Areas
 */
export const getPriorityLabel = (score: number): string => {
  if (score >= 80) return 'Kritisch'
  if (score >= 60) return 'Hoch'
  if (score >= 40) return 'Mittel'
  return 'Niedrig'
}

export const getPriorityColor = (score: number): string => {
  if (score >= 80) return 'error'
  if (score >= 60) return 'warning'
  if (score >= 40) return 'info'
  return 'success'
}

/**
 * Berechnet Fortschritt als Prozent
 */
export const calculateProgress = (current: number, total: number): number => {
  if (total === 0) return 0
  return Math.round((current / total) * 100)
}

/**
 * Formatiert Erfolgsrate als Prozent-String
 */
export const formatSuccessRate = (rate: number): string => {
  return `${Math.round(rate * 100)}%`
}

/**
 * Berechnet Tage bis zur naechsten Wiederholung
 */
export const getDaysUntilReview = (nextReviewDate: string): number => {
  const now = new Date()
  const review = new Date(nextReviewDate)
  const diffMs = review.getTime() - now.getTime()
  return Math.ceil(diffMs / (1000 * 60 * 60 * 24))
}

/**
 * Gibt an ob ein Item zur Wiederholung faellig ist
 */
export const isDueForReview = (nextReviewDate: string): boolean => {
  return getDaysUntilReview(nextReviewDate) <= 0
}

/**
 * Sortiert Items nach Prioritaet (absteigend)
 */
export const sortByPriority = <T extends { compositeScore?: number; priority?: number }>(
  items: T[]
): T[] => {
  return [...items].sort((a, b) => {
    const scoreA = a.compositeScore ?? a.priority ?? 0
    const scoreB = b.compositeScore ?? b.priority ?? 0
    return scoreB - scoreA
  })
}

/**
 * Sortiert Items nach Mastery (aufsteigend - schwache zuerst)
 */
export const sortByMastery = <T extends { masteryScore: number }>(
  items: T[],
  ascending = true
): T[] => {
  return [...items].sort((a, b) => {
    return ascending
      ? a.masteryScore - b.masteryScore
      : b.masteryScore - a.masteryScore
  })
}

/**
 * Gruppiert Items nach Subject
 */
export const groupBySubject = <T extends { subject: string }>(
  items: T[]
): Record<string, T[]> => {
  return items.reduce((acc, item) => {
    if (!acc[item.subject]) {
      acc[item.subject] = []
    }
    acc[item.subject].push(item)
    return acc
  }, {} as Record<string, T[]>)
}

/**
 * Berechnet durchschnittliche Mastery fuer eine Gruppe
 */
export const calculateAverageMastery = <T extends { masteryScore: number }>(
  items: T[]
): number => {
  if (items.length === 0) return 0
  const sum = items.reduce((acc, item) => acc + item.masteryScore, 0)
  return sum / items.length
}

/**
 * 20/40/40 Regel - Berechnet empfohlene Schwierigkeit basierend auf bisheriger Verteilung
 */
export const getRecommendedDifficulty = (
  easyCount: number,
  mediumCount: number,
  hardCount: number
): 'easy' | 'medium' | 'hard' => {
  const total = easyCount + mediumCount + hardCount
  if (total === 0) return 'easy'

  const easyRatio = easyCount / total
  const mediumRatio = mediumCount / total
  const hardRatio = hardCount / total

  // Ziel: 20% easy, 40% medium, 40% hard
  if (easyRatio < 0.2) return 'easy'
  if (mediumRatio < 0.4) return 'medium'
  if (hardRatio < 0.4) return 'hard'

  return 'medium' // Default
}

/**
 * Erstellt Session-Konfiguration basierend auf Session-Typ
 */
export const getSessionConfig = (sessionType: string) => {
  const configs: Record<string, { focusOverdue: boolean; focusWeak: boolean; bloomProgression: boolean }> = {
    review: { focusOverdue: true, focusWeak: false, bloomProgression: false },
    weakness: { focusOverdue: false, focusWeak: true, bloomProgression: false },
    mixed: { focusOverdue: true, focusWeak: true, bloomProgression: false },
    bloom_progression: { focusOverdue: false, focusWeak: false, bloomProgression: true }
  }
  return configs[sessionType] || configs.mixed
}

// === Konstanten ===

export const MASTERY_THRESHOLDS = {
  mastered: 0.8,
  learning: 0.5,
  beginner: 0.3,
  new: 0
} as const

export const BLOOM_LEVELS = [1, 2, 3, 4, 5, 6] as const

export const DEFAULT_SESSION_SIZE = 5
export const MAX_SESSION_SIZE = 20
export const MIN_SESSION_SIZE = 1
