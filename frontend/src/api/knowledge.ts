import type { ApiResponse } from '@/types/api'
import type { Entity, GraphData, SearchResult } from '@/types/knowledge'
import { client } from './client'

export const knowledgeApi = {
  entities: (subject?: string, entityType?: string) =>
    client.get<ApiResponse<Entity[]>>('/api/knowledge/entities', {
      params: { subject, entity_type: entityType },
    }),

  entity: (id: number) =>
    client.get<ApiResponse<Entity>>(`/api/knowledge/entities/${id}`),

  graph: () =>
    client.get<ApiResponse<GraphData>>('/api/knowledge/graph'),

  search: (query: string, limit = 10) =>
    client.post<ApiResponse<SearchResult[]>>('/api/knowledge/search', { query, limit }),

  weakAreas: (limit = 20) =>
    client.get<ApiResponse<Entity[]>>('/api/knowledge/weak-areas', { params: { limit } }),

  extract: (documentId: number) =>
    client.post<ApiResponse<{ entities_created: number }>>(`/api/knowledge/extract/${documentId}`),
}
