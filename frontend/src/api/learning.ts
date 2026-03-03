import type { ApiResponse } from '@/types/api'
import type { AnswerRequest, Exercise, LearningPriority, LearningStats, Streak } from '@/types/learning'
import { client } from './client'

export const learningApi = {
  next: () =>
    client.get<ApiResponse<Exercise>>('/api/learning/next'),

  generate: (entityId: number, difficulty = 'medium', bloomLevel?: number) =>
    client.post<ApiResponse<Exercise>>('/api/learning/exercise', {
      entity_id: entityId, difficulty, bloom_level: bloomLevel,
    }),

  answer: (exerciseId: number, data: AnswerRequest) =>
    client.post<ApiResponse<Exercise>>(`/api/learning/exercise/${exerciseId}/answer`, data),

  session: (count = 10) =>
    client.post<ApiResponse<Exercise[]>>('/api/learning/session', { count }),

  stats: () =>
    client.get<ApiResponse<LearningStats>>('/api/learning/stats'),

  streak: () =>
    client.get<ApiResponse<Streak>>('/api/learning/streak'),

  priorities: (limit = 20) =>
    client.get<ApiResponse<LearningPriority[]>>('/api/learning/priorities', { params: { limit } }),

  recalculate: () =>
    client.post<ApiResponse<{ calculated: number }>>('/api/learning/priorities/recalculate'),

  due: () =>
    client.get<ApiResponse<Exercise[]>>('/api/learning/due'),
}
