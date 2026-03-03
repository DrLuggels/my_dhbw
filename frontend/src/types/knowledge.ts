export type EntityType =
  | 'concept' | 'definition' | 'formula' | 'person' | 'date'
  | 'example' | 'theorem' | 'method' | 'term' | 'algorithm'
  | 'data_structure' | 'principle'

export interface Entity {
  id: number
  name: string
  description: string | null
  entity_type: EntityType
  subject: string | null
  topic: string | null
  subtopic: string | null
  importance: number
  confidence: number
  mastery_score: number
  bloom_level: number
  total_attempts: number
  correct_attempts: number
  next_review: string | null
  created_at: string
}

export interface Relationship {
  id: number
  source_entity_id: number
  target_entity_id: number
  relationship_type: string
  strength: number
  evidence: string | null
  is_prerequisite: boolean
}

export interface GraphNode {
  id: number
  label: string
  type: string
  mastery: number
  bloom: number
  subject: string | null
  topic: string | null
}

export interface GraphEdge {
  id: number
  source: number
  target: number
  label: string
  strength: number
  is_prerequisite: boolean
}

export interface GraphData {
  nodes: GraphNode[]
  edges: GraphEdge[]
}

export interface SearchResult {
  id: number
  name: string
  description: string | null
  entity_type: string
  topic: string | null
  mastery_score: number
  bloom_level: number
  similarity: number
}
