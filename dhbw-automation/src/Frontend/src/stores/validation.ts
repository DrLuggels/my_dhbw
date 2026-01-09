import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { validationService } from '@/services/validationService'
import type {
  StagedEntity,
  EntityWithParsedData,
  PendingEntitiesResponse,
  StagingStatistics,
  ParsedEntityData
} from '@/types/validation'

export const useValidationStore = defineStore('validation', () => {
  // State
  const pendingEntities = ref<StagedEntity[]>([])
  const currentEntity = ref<StagedEntity | null>(null)
  const statistics = ref<StagingStatistics | null>(null)
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  // Computed
  const pendingCount = computed(() => pendingEntities.value.length)

  const highPriorityCount = computed(() =>
    pendingEntities.value.filter(e => e.priority === 'high' || e.priority === 'urgent').length
  )

  const withQuestionsCount = computed(() =>
    pendingEntities.value.filter(e => e.questions.length > 0).length
  )

  const lowConfidenceCount = computed(() =>
    pendingEntities.value.filter(e => e.confidenceScore < 70).length
  )

  const entitiesWithParsedData = computed((): EntityWithParsedData[] => {
    return pendingEntities.value.map(entity => ({
      ...entity,
      parsedData: parseEntityData(entity)
    }))
  })

  // Helper: Parse entity data JSON
  function parseEntityData(entity: StagedEntity): ParsedEntityData {
    try {
      return JSON.parse(entity.entityData)
    } catch (e) {
      console.error('Failed to parse entity data:', e)
      return {} as ParsedEntityData
    }
  }

  // Actions
  async function fetchPendingEntities(status?: string) {
    isLoading.value = true
    error.value = null

    try {
      const response: PendingEntitiesResponse = await validationService.getPendingEntities(status)
      pendingEntities.value = response.entities
      return true
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Fehler beim Laden der Entitäten'
      console.error('Error fetching pending entities:', err)
      return false
    } finally {
      isLoading.value = false
    }
  }

  async function fetchEntity(id: number) {
    isLoading.value = true
    error.value = null

    try {
      const entity = await validationService.getStagedEntity(id)
      currentEntity.value = entity
      return entity
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Fehler beim Laden der Entität'
      console.error('Error fetching entity:', err)
      return null
    } finally {
      isLoading.value = false
    }
  }

  async function answerQuestions(id: number, answers: Record<string, string>) {
    isLoading.value = true
    error.value = null

    try {
      await validationService.answerQuestions(id, answers)

      // Update local state
      const entity = pendingEntities.value.find(e => e.id === id)
      if (entity) {
        entity.questions.forEach(q => {
          if (answers[q.fieldName]) {
            q.isAnswered = true
            q.userAnswer = answers[q.fieldName]
            q.answeredAt = new Date().toISOString()
          }
        })
      }

      return true
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Fehler beim Beantworten der Fragen'
      console.error('Error answering questions:', err)
      return false
    } finally {
      isLoading.value = false
    }
  }

  async function confirmEntity(id: number, userNotes?: string) {
    isLoading.value = true
    error.value = null

    try {
      const result = await validationService.confirmEntity(id, userNotes)

      // Remove from pending list
      pendingEntities.value = pendingEntities.value.filter(e => e.id !== id)

      if (currentEntity.value?.id === id) {
        currentEntity.value = null
      }

      return result.promotedEntityId
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Fehler beim Bestätigen der Entität'
      console.error('Error confirming entity:', err)
      return null
    } finally {
      isLoading.value = false
    }
  }

  async function rejectEntity(id: number, reason: string) {
    isLoading.value = true
    error.value = null

    try {
      await validationService.rejectEntity(id, reason)

      // Remove from pending list
      pendingEntities.value = pendingEntities.value.filter(e => e.id !== id)

      if (currentEntity.value?.id === id) {
        currentEntity.value = null
      }

      return true
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Fehler beim Ablehnen der Entität'
      console.error('Error rejecting entity:', err)
      return false
    } finally {
      isLoading.value = false
    }
  }

  async function modifyEntity(id: number, modifiedData: ParsedEntityData) {
    isLoading.value = true
    error.value = null

    try {
      const jsonData = JSON.stringify(modifiedData)
      await validationService.modifyEntity(id, jsonData)

      // Update local state
      const entity = pendingEntities.value.find(e => e.id === id)
      if (entity) {
        entity.entityData = jsonData
        entity.status = 'modified'
        entity.reviewedAt = new Date().toISOString()
      }

      return true
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Fehler beim Ändern der Entität'
      console.error('Error modifying entity:', err)
      return false
    } finally {
      isLoading.value = false
    }
  }

  async function fetchStatistics(days: number = 30) {
    isLoading.value = true
    error.value = null

    try {
      const stats = await validationService.getStatistics(days)
      statistics.value = stats
      return stats
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Fehler beim Laden der Statistiken'
      console.error('Error fetching statistics:', err)
      return null
    } finally {
      isLoading.value = false
    }
  }

  async function bulkConfirm(minConfidence: number = 95) {
    isLoading.value = true
    error.value = null

    try {
      const result = await validationService.bulkConfirm(minConfidence)

      // Refresh pending entities
      await fetchPendingEntities()

      return result
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Fehler bei der Bulk-Bestätigung'
      console.error('Error in bulk confirm:', err)
      return null
    } finally {
      isLoading.value = false
    }
  }

  function clearError() {
    error.value = null
  }

  // Auto-refresh on initial load
  fetchPendingEntities()

  return {
    // State
    pendingEntities,
    currentEntity,
    statistics,
    isLoading,
    error,

    // Computed
    pendingCount,
    highPriorityCount,
    withQuestionsCount,
    lowConfidenceCount,
    entitiesWithParsedData,

    // Actions
    fetchPendingEntities,
    fetchEntity,
    answerQuestions,
    confirmEntity,
    rejectEntity,
    modifyEntity,
    fetchStatistics,
    bulkConfirm,
    clearError
  }
})
