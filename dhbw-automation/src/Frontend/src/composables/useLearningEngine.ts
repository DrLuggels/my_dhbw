import { ref, computed } from 'vue'
import api from '@/services/api'
import type {
  KgEntity,
  KnowledgeGraph,
  LearningDocumentResult,
  LearningQuestion,
  AnswerSubmission,
  AnswerFeedback,
  WeakArea,
  MasteryStats,
  QuestionGenerationRequest,
  ProcessingOptions
} from '@/types/learningEngine'

export function useLearningEngine() {
  // State
  const knowledgeGraph = ref<KnowledgeGraph | null>(null)
  const questions = ref<LearningQuestion[]>([])
  const currentQuestion = ref<LearningQuestion | null>(null)
  const weakAreas = ref<WeakArea[]>([])
  const dueForReview = ref<KgEntity[]>([])
  const masteryStats = ref<MasteryStats | null>(null)
  const processingResults = ref<LearningDocumentResult[]>([])

  // Loading states
  const loadingGraph = ref(false)
  const loadingQuestions = ref(false)
  const loadingWeakAreas = ref(false)
  const loadingStats = ref(false)
  const processingDocument = ref(false)
  const submittingAnswer = ref(false)

  // Error state
  const error = ref<string | null>(null)

  // Computed
  const hasKnowledgeGraph = computed(() =>
    knowledgeGraph.value !== null &&
    (knowledgeGraph.value.entities.length > 0 || knowledgeGraph.value.relationships.length > 0)
  )

  const questionCount = computed(() => questions.value.length)
  const currentQuestionIndex = computed(() => {
    if (!currentQuestion.value) return -1
    return questions.value.findIndex(q => q.id === currentQuestion.value?.id)
  })

  // === Document Processing ===

  const processDocument = async (documentId: number, options?: ProcessingOptions): Promise<LearningDocumentResult | null> => {
    processingDocument.value = true
    error.value = null
    try {
      const response = await api.post(`/learning-engine/process-document/${documentId}`, options || {})
      if (response.data.success) {
        const result = response.data.data as LearningDocumentResult
        processingResults.value.push(result)
        return result
      } else {
        error.value = response.data.message || 'Fehler bei der Dokumentverarbeitung'
        return null
      }
    } catch (err: unknown) {
      const errorMsg = err instanceof Error ? err.message : 'Fehler bei der Dokumentverarbeitung'
      error.value = errorMsg
      console.error('Error processing document:', err)
      return null
    } finally {
      processingDocument.value = false
    }
  }

  const processDocumentsBatch = async (
    documentIds: number[],
    options?: ProcessingOptions
  ): Promise<LearningDocumentResult[]> => {
    processingDocument.value = true
    error.value = null
    try {
      const response = await api.post('/learning-engine/process-documents', {
        documentIds,
        options
      })
      if (response.data.success) {
        const results = response.data.data.results as LearningDocumentResult[]
        processingResults.value.push(...results)
        return results
      } else {
        error.value = response.data.message || 'Fehler bei der Batch-Verarbeitung'
        return []
      }
    } catch (err: unknown) {
      const errorMsg = err instanceof Error ? err.message : 'Fehler bei der Batch-Verarbeitung'
      error.value = errorMsg
      console.error('Error processing documents batch:', err)
      return []
    } finally {
      processingDocument.value = false
    }
  }

  // === Knowledge Graph ===

  const loadKnowledgeGraph = async (options?: {
    subject?: string
    topic?: string
    entityType?: string
    limit?: number
  }): Promise<void> => {
    loadingGraph.value = true
    error.value = null
    try {
      const params = new URLSearchParams()
      if (options?.subject) params.append('subject', options.subject)
      if (options?.topic) params.append('topic', options.topic)
      if (options?.entityType) params.append('entityType', options.entityType)
      if (options?.limit) params.append('limit', options.limit.toString())

      const queryString = params.toString()
      const url = `/learning-engine/knowledge-graph${queryString ? `?${queryString}` : ''}`

      const response = await api.get(url)
      if (response.data.success) {
        knowledgeGraph.value = response.data.data
      } else {
        error.value = response.data.message || 'Fehler beim Laden des Wissensgraphen'
      }
    } catch (err: unknown) {
      const errorMsg = err instanceof Error ? err.message : 'Fehler beim Laden des Wissensgraphen'
      error.value = errorMsg
      console.error('Error loading knowledge graph:', err)
    } finally {
      loadingGraph.value = false
    }
  }

  const loadDocumentKnowledgeGraph = async (documentId: number): Promise<void> => {
    loadingGraph.value = true
    error.value = null
    try {
      const response = await api.get(`/learning-engine/knowledge-graph/document/${documentId}`)
      if (response.data.success) {
        knowledgeGraph.value = response.data.data
      } else {
        error.value = response.data.message || 'Fehler beim Laden des Dokumentgraphen'
      }
    } catch (err: unknown) {
      const errorMsg = err instanceof Error ? err.message : 'Fehler beim Laden des Dokumentgraphen'
      error.value = errorMsg
      console.error('Error loading document knowledge graph:', err)
    } finally {
      loadingGraph.value = false
    }
  }

  const getRelatedEntities = async (entityId: number, depth: number = 1): Promise<KgEntity[]> => {
    try {
      const response = await api.get(`/learning-engine/entities/${entityId}/related?depth=${depth}`)
      if (response.data.success) {
        return response.data.data
      }
      return []
    } catch (err) {
      console.error('Error getting related entities:', err)
      return []
    }
  }

  const searchEntities = async (query: string, entityType?: string, limit: number = 20): Promise<KgEntity[]> => {
    try {
      const params = new URLSearchParams({ query, limit: limit.toString() })
      if (entityType) params.append('entityType', entityType)

      const response = await api.get(`/learning-engine/entities/search?${params.toString()}`)
      if (response.data.success) {
        return response.data.data
      }
      return []
    } catch (err) {
      console.error('Error searching entities:', err)
      return []
    }
  }

  const mergeEntities = async (primaryId: number, duplicateIds: number[]): Promise<boolean> => {
    try {
      const response = await api.post(`/learning-engine/entities/${primaryId}/merge`, {
        duplicateIds
      })
      return response.data.success
    } catch (err) {
      console.error('Error merging entities:', err)
      return false
    }
  }

  // === Question Generation ===

  const generateQuestions = async (request: QuestionGenerationRequest): Promise<LearningQuestion[]> => {
    loadingQuestions.value = true
    error.value = null
    try {
      const response = await api.post('/learning-engine/generate-questions', request)
      if (response.data.success) {
        const newQuestions = response.data.data as LearningQuestion[]
        questions.value = newQuestions
        if (newQuestions.length > 0) {
          currentQuestion.value = newQuestions[0]
        }
        return newQuestions
      } else {
        error.value = response.data.message || 'Fehler bei der Fragengenerierung'
        return []
      }
    } catch (err: unknown) {
      const errorMsg = err instanceof Error ? err.message : 'Fehler bei der Fragengenerierung'
      error.value = errorMsg
      console.error('Error generating questions:', err)
      return []
    } finally {
      loadingQuestions.value = false
    }
  }

  const generateEntityQuestions = async (
    entityId: number,
    count: number = 5,
    questionType?: string,
    bloomLevel?: number
  ): Promise<LearningQuestion[]> => {
    loadingQuestions.value = true
    error.value = null
    try {
      const params = new URLSearchParams({ count: count.toString() })
      if (questionType) params.append('questionType', questionType)
      if (bloomLevel) params.append('bloomLevel', bloomLevel.toString())

      const response = await api.post(
        `/learning-engine/entities/${entityId}/generate-questions?${params.toString()}`
      )
      if (response.data.success) {
        const newQuestions = response.data.data as LearningQuestion[]
        questions.value = newQuestions
        if (newQuestions.length > 0) {
          currentQuestion.value = newQuestions[0]
        }
        return newQuestions
      } else {
        error.value = response.data.message || 'Fehler bei der Fragengenerierung'
        return []
      }
    } catch (err: unknown) {
      const errorMsg = err instanceof Error ? err.message : 'Fehler bei der Fragengenerierung'
      error.value = errorMsg
      console.error('Error generating entity questions:', err)
      return []
    } finally {
      loadingQuestions.value = false
    }
  }

  // === Answer Submission ===

  const submitAnswer = async (submission: AnswerSubmission): Promise<AnswerFeedback | null> => {
    submittingAnswer.value = true
    error.value = null
    try {
      const response = await api.post('/learning-engine/submit-answer', submission)
      if (response.data.success) {
        return response.data.data as AnswerFeedback
      } else {
        error.value = response.data.message || 'Fehler beim Speichern der Antwort'
        return null
      }
    } catch (err: unknown) {
      const errorMsg = err instanceof Error ? err.message : 'Fehler beim Speichern der Antwort'
      error.value = errorMsg
      console.error('Error submitting answer:', err)
      return null
    } finally {
      submittingAnswer.value = false
    }
  }

  const nextQuestion = (): LearningQuestion | null => {
    const currentIndex = currentQuestionIndex.value
    if (currentIndex < questions.value.length - 1) {
      currentQuestion.value = questions.value[currentIndex + 1]
      return currentQuestion.value
    }
    return null
  }

  const previousQuestion = (): LearningQuestion | null => {
    const currentIndex = currentQuestionIndex.value
    if (currentIndex > 0) {
      currentQuestion.value = questions.value[currentIndex - 1]
      return currentQuestion.value
    }
    return null
  }

  // === Performance Tracking ===

  const loadWeakAreas = async (limit: number = 10): Promise<void> => {
    loadingWeakAreas.value = true
    error.value = null
    try {
      const response = await api.get(`/learning-engine/weak-areas?limit=${limit}`)
      if (response.data.success) {
        weakAreas.value = response.data.data
      } else {
        error.value = response.data.message || 'Fehler beim Laden der Schwachstellen'
      }
    } catch (err: unknown) {
      const errorMsg = err instanceof Error ? err.message : 'Fehler beim Laden der Schwachstellen'
      error.value = errorMsg
      console.error('Error loading weak areas:', err)
    } finally {
      loadingWeakAreas.value = false
    }
  }

  const loadDueForReview = async (limit: number = 10): Promise<void> => {
    try {
      const response = await api.get(`/learning-engine/due-for-review?limit=${limit}`)
      if (response.data.success) {
        dueForReview.value = response.data.data
      }
    } catch (err) {
      console.error('Error loading due for review:', err)
    }
  }

  const loadMasteryStats = async (subject?: string): Promise<void> => {
    loadingStats.value = true
    error.value = null
    try {
      const url = subject
        ? `/learning-engine/mastery-stats?subject=${encodeURIComponent(subject)}`
        : '/learning-engine/mastery-stats'

      const response = await api.get(url)
      if (response.data.success) {
        masteryStats.value = response.data.data
      } else {
        error.value = response.data.message || 'Fehler beim Laden der Statistiken'
      }
    } catch (err: unknown) {
      const errorMsg = err instanceof Error ? err.message : 'Fehler beim Laden der Statistiken'
      error.value = errorMsg
      console.error('Error loading mastery stats:', err)
    } finally {
      loadingStats.value = false
    }
  }

  // === Utility ===

  const clearError = () => {
    error.value = null
  }

  const resetQuestions = () => {
    questions.value = []
    currentQuestion.value = null
  }

  const clearProcessingResults = () => {
    processingResults.value = []
  }

  return {
    // State
    knowledgeGraph,
    questions,
    currentQuestion,
    weakAreas,
    dueForReview,
    masteryStats,
    processingResults,
    error,

    // Loading states
    loadingGraph,
    loadingQuestions,
    loadingWeakAreas,
    loadingStats,
    processingDocument,
    submittingAnswer,

    // Computed
    hasKnowledgeGraph,
    questionCount,
    currentQuestionIndex,

    // Document Processing
    processDocument,
    processDocumentsBatch,

    // Knowledge Graph
    loadKnowledgeGraph,
    loadDocumentKnowledgeGraph,
    getRelatedEntities,
    searchEntities,
    mergeEntities,

    // Questions
    generateQuestions,
    generateEntityQuestions,
    submitAnswer,
    nextQuestion,
    previousQuestion,
    resetQuestions,

    // Performance
    loadWeakAreas,
    loadDueForReview,
    loadMasteryStats,

    // Utility
    clearError,
    clearProcessingResults
  }
}
