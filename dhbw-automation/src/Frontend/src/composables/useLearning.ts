import { ref } from 'vue'
import api from '@/services/api'
import type { LearningStats, LearningDeficit, Exercise, InteractiveExerciseData } from '@/types/learning'

export function useLearning() {
  const stats = ref<LearningStats>({
    totalDeficits: 0, activeDeficits: 0, resolvedDeficits: 0, highSeverityDeficits: 0,
    totalExercises: 0, completedExercises: 0, pendingExercises: 0, dueExercises: 0,
    averageEaseFactor: 2.5
  })
  const deficits = ref<LearningDeficit[]>([])
  const dueExercises = ref<Exercise[]>([])
  const resolvedDeficits = ref<LearningDeficit[]>([])
  const loadingDeficits = ref(false)
  const loadingExercises = ref(false)
  const loadingResolved = ref(false)

  const loadStats = async (userId: number) => {
    try {
      const response = await api.get(`/learning/stats/${userId}`)
      if (response.data.success) stats.value = response.data.data
    } catch (error) { console.error('Error loading stats:', error) }
  }

  const loadDeficits = async (userId: number) => {
    loadingDeficits.value = true
    try {
      const response = await api.get(`/learning/deficits/${userId}`)
      if (response.data.success) deficits.value = response.data.data
    } catch (error) { console.error('Error loading deficits:', error); throw error }
    finally { loadingDeficits.value = false }
  }

  const loadExercises = async (userId: number) => {
    loadingExercises.value = true
    try {
      const response = await api.get(`/learning/exercises/due/${userId}`)
      if (response.data.success) {
        dueExercises.value = response.data.data.map((ex: Exercise) => ({
          ...ex, userInput: '', showHelp: false, answered: false
        }))
      }
    } catch (error) { console.error('Error loading exercises:', error); throw error }
    finally { loadingExercises.value = false }
  }

  const loadResolvedDeficits = async (_userId: number) => {
    loadingResolved.value = true
    try { resolvedDeficits.value = [] }
    catch (error) { console.error('Error loading resolved:', error) }
    finally { loadingResolved.value = false }
  }

  const scheduleTutoring = async (deficitId: number, userId: number) => {
    const response = await api.post(`/learning/schedule-tutoring/${deficitId}?userId=${userId}`)
    return response.data
  }

  const resolveDeficit = async (deficitId: number, userId: number) => {
    const response = await api.patch(`/learning/deficits/${deficitId}/resolve?userId=${userId}`)
    return response.data
  }

  const submitAnswer = async (exerciseId: number, userId: number, answer: string) => {
    const response = await api.post(`/learning/exercises/${exerciseId}/answer`, {
      userId, answer, isCorrect: false
    })
    return response.data
  }

  return {
    stats, deficits, dueExercises, resolvedDeficits,
    loadingDeficits, loadingExercises, loadingResolved,
    loadStats, loadDeficits, loadExercises, loadResolvedDeficits,
    scheduleTutoring, resolveDeficit, submitAnswer
  }
}

export function useInteractiveExercises() {
  const currentExercise = ref<InteractiveExerciseData | null>(null)
  const loading = ref(false)

  const generate = async (userId: number, subject: string, topic: string, difficulty: string) => {
    loading.value = true
    try {
      const response = await api.post('/exercises/interactive/generate', {
        userId, subject, topic, difficulty
      })
      if (response.data.success) {
        currentExercise.value = response.data.data
        return response.data.data
      }
    } catch (error) { console.error('Error generating:', error); throw error }
    finally { loading.value = false }
  }

  const reset = () => { currentExercise.value = null }

  return { currentExercise, loading, generate, reset }
}
