import { learningApi } from '@/api/learning'
import type { Exercise, LearningStats, Streak } from '@/types/learning'
import { defineStore } from 'pinia'
import { ref } from 'vue'

export const useLearningStore = defineStore('learning', () => {
  const currentExercise = ref<Exercise | null>(null)
  const session = ref<Exercise[]>([])
  const stats = ref<LearningStats | null>(null)
  const streak = ref<Streak | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function fetchNext() {
    loading.value = true
    error.value = null
    try {
      const { data } = await learningApi.next()
      currentExercise.value = data.data
    } catch {
      error.value = 'Keine Übungen verfügbar'
    } finally {
      loading.value = false
    }
  }

  async function submitAnswer(exerciseId: number, answer: string, rating: number) {
    loading.value = true
    try {
      const { data } = await learningApi.answer(exerciseId, { user_answer: answer, rating })
      currentExercise.value = data.data
      return data.data
    } catch {
      error.value = 'Antwort konnte nicht gesendet werden'
      return null
    } finally {
      loading.value = false
    }
  }

  async function startSession(count = 10) {
    loading.value = true
    error.value = null
    try {
      const { data } = await learningApi.session(count)
      session.value = data.data ?? []
    } catch {
      error.value = 'Session konnte nicht erstellt werden'
    } finally {
      loading.value = false
    }
  }

  async function fetchStats() {
    try {
      const { data } = await learningApi.stats()
      stats.value = data.data
    } catch {
      error.value = 'Statistiken nicht verfügbar'
    }
  }

  async function fetchStreak() {
    try {
      const { data } = await learningApi.streak()
      streak.value = data.data
    } catch {
      error.value = 'Streak nicht verfügbar'
    }
  }

  return {
    currentExercise, session, stats, streak, loading, error,
    fetchNext, submitAnswer, startSession, fetchStats, fetchStreak,
  }
})
