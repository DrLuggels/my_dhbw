export interface GraphNode {
  id: string
  entityType: string
  entityId: number
  label: string
  type: string
  linkCount: number
  mastery?: number
  effectiveStrength?: number
  lastInteraction?: string
  decayRate?: number
}

export interface GraphEdge {
  from: string
  to: string
  linkType: string
}

export interface ClusterPoint {
  entityType: string
  entityId: number
  label: string
  x: number
  y: number
  category: string
}

export interface Tag {
  id: number
  name: string
  color: string
  assignmentCount: number
}

export interface RelatedItem {
  entityType: string
  entityId: number
  title: string
  linkType: string
  score: number
}

export interface SearchResult {
  entityType: string
  entityId: number
  title: string
  score: number
}

export interface PendingLink {
  id: number
  sourceTitle: string
  targetTitle: string
  linkType: string
  confidence: number
}

export interface NetworkStats {
  totalNodes: number
  totalLinks: number
  embeddingsCount: number
  pendingLinks: number
}

export interface NewLink {
  targetType: string
  targetId: number | null
  linkType: string
}

export const entityTypeOptions = [
  { title: 'Dokumente', value: 'Document' },
  { title: 'Wissensbasis', value: 'KnowledgeItem' },
  { title: 'Java-Docs Uebungen', value: 'JavaDocsExercise' },
  { title: 'Bilder', value: 'Image' },
  { title: 'Moodle Ressourcen', value: 'MoodleResource' }
]

export const linkTypeOptions = [
  { title: 'Verwandt', value: 'related' },
  { title: 'Voraussetzung', value: 'prerequisite' },
  { title: 'Erweiterung', value: 'extension' },
  { title: 'Beispiel', value: 'example' },
  { title: 'Abgeleitet von', value: 'derived_from' }
]

export const getNodeIcon = (type: string): string => {
  const icons: Record<string, string> = {
    'Document': 'mdi-file-document',
    'KnowledgeItem': 'mdi-lightbulb',
    'JavaDocsExercise': 'mdi-language-java',
    'Image': 'mdi-image',
    'MoodleResource': 'mdi-school'
  }
  return icons[type] || 'mdi-circle'
}

export const getNodeColor = (type: string, masteryMode: boolean = false, mastery?: number): string => {
  if (masteryMode && mastery !== undefined) {
    if (mastery < 0.3) return 'error'
    if (mastery < 0.6) return 'warning'
    return 'success'
  }

  const colors: Record<string, string> = {
    'Document': 'blue',
    'KnowledgeItem': 'orange',
    'JavaDocsExercise': 'green',
    'Image': 'purple',
    'MoodleResource': 'red'
  }
  return colors[type] || 'grey'
}

export const getScoreColor = (score: number): string => {
  if (score >= 0.9) return 'success'
  if (score >= 0.7) return 'info'
  if (score >= 0.5) return 'warning'
  return 'grey'
}

export const formatScore = (score: number): string => {
  return `${Math.round(score * 100)}%`
}

export const entityRoutes: Record<string, string> = {
  'Document': '/files',
  'KnowledgeItem': '/learning',
  'JavaDocsExercise': '/learning',
  'Image': '/files',
  'MoodleResource': '/learning'
}
