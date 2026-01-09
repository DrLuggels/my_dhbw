import api from './api'
import type {
  StagedEntity,
  PendingEntitiesResponse,
  AnswerQuestionsRequest,
  ConfirmEntityRequest,
  RejectEntityRequest,
  ModifyEntityRequest,
  StagingStatistics
} from '@/types/validation'

export const validationService = {
  /**
   * Holt alle ausstehenden Staging-Entitäten für den aktuellen User
   * @param status Optional: Filter nach Status (pending_review, confirmed, rejected)
   */
  async getPendingEntities(status?: string): Promise<PendingEntitiesResponse> {
    const params = status ? { status } : {}
    const response = await api.get('/validation/pending', { params })
    return response.data
  },

  /**
   * Holt Details zu einer spezifischen Staging-Entität
   * @param id ID der Staging-Entität
   */
  async getStagedEntity(id: number): Promise<StagedEntity> {
    const response = await api.get(`/validation/${id}`)
    return response.data
  },

  /**
   * Beantwortet Fragen zu einer Staging-Entität
   * @param id ID der Staging-Entität
   * @param answers Dictionary mit Antworten (fieldName -> answer)
   */
  async answerQuestions(id: number, answers: Record<string, string>): Promise<void> {
    const request: AnswerQuestionsRequest = { answers }
    await api.post(`/validation/${id}/answer`, request)
  },

  /**
   * Bestätigt eine Staging-Entität und überträgt sie in die Produktiv-DB
   * @param id ID der Staging-Entität
   * @param userNotes Optional: Notizen des Users
   */
  async confirmEntity(id: number, userNotes?: string): Promise<{ promotedEntityId: number }> {
    const request: ConfirmEntityRequest = { userNotes }
    const response = await api.post(`/validation/${id}/confirm`, request)
    return response.data
  },

  /**
   * Lehnt eine Staging-Entität ab
   * @param id ID der Staging-Entität
   * @param reason Grund der Ablehnung
   */
  async rejectEntity(id: number, reason: string): Promise<void> {
    const request: RejectEntityRequest = { reason }
    await api.post(`/validation/${id}/reject`, request)
  },

  /**
   * Ändert die Daten einer Staging-Entität (User-Korrektur)
   * @param id ID der Staging-Entität
   * @param modifiedData Geänderte Daten als JSON-String
   */
  async modifyEntity(id: number, modifiedData: string): Promise<void> {
    const request: ModifyEntityRequest = { modifiedData }
    await api.put(`/validation/${id}`, request)
  },

  /**
   * Holt Statistiken über das Staging-System
   * @param days Zeitraum in Tagen (default: 30)
   */
  async getStatistics(days: number = 30): Promise<StagingStatistics> {
    const response = await api.get(`/validation/statistics?days=${days}`)
    return response.data
  },

  /**
   * Bulk-Bestätigung: Bestätigt alle Entitäten mit hohem Confidence Score
   * @param minConfidence Minimum Confidence Score (default: 95)
   */
  async bulkConfirm(minConfidence: number = 95): Promise<{ promotedCount: number; totalEligible: number }> {
    const response = await api.post(`/validation/bulk-confirm?minConfidence=${minConfidence}`)
    return response.data
  }
}
