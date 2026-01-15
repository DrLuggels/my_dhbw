<template>
  <v-container>
    <!-- Header -->
    <div class="d-flex justify-space-between align-center mb-4 mb-md-6">
      <div class="d-flex align-center">
        <v-btn icon variant="text" @click="$router.back()" class="mr-2 mr-md-3">
          <v-icon>mdi-arrow-left</v-icon>
        </v-btn>
        <h1 :class="isMobile ? 'text-h5' : 'text-h3'">
          <v-icon left color="primary">mdi-school</v-icon>
          Defizit-Management
        </h1>
        <StreakWidget compact variant="text" :show-action="false" class="ml-4 d-none d-sm-flex" />
      </div>

      <v-btn color="primary" variant="tonal" :to="{ name: 'omni-lernen' }">
        <v-icon start>mdi-brain</v-icon>
        <span class="d-none d-sm-inline">Omni-Lernen</span>
      </v-btn>
    </div>

    <!-- OmniLernen Promo Banner -->
    <v-alert
      type="info"
      variant="tonal"
      class="mb-4"
      closable
      v-model="showPromoBanner"
    >
      <div class="d-flex align-center justify-space-between flex-wrap">
        <div>
          <strong>Neu: Omni-Lernen</strong> - Adaptives Lernen mit KI, Spaced Repetition und Wissens-Graph.
        </div>
        <v-btn color="primary" variant="text" :to="{ name: 'omni-lernen' }" class="mt-2 mt-sm-0">
          Jetzt ausprobieren
          <v-icon end>mdi-arrow-right</v-icon>
        </v-btn>
      </div>
    </v-alert>

    <!-- Priority Recommendations -->
    <v-row class="mb-4">
      <v-col cols="12" md="8">
        <PriorityCard :max-items="3" :show-details="false" @learn="onPriorityLearn" />
      </v-col>
      <v-col cols="12" md="4">
        <DifficultyDistribution variant="outlined" />
      </v-col>
    </v-row>

    <!-- Statistics Cards -->
    <LearningStatsCards
      :stats="stats"
      @practice-now="activeTab = 'exercises'"
    />

    <!-- Tabs (simplified - only deficit management) -->
    <v-card class="mt-6">
      <v-tabs v-model="activeTab" bg-color="primary">
        <v-tab value="deficits">
          <v-icon left>mdi-alert-circle</v-icon>
          Defizite ({{ stats.activeDeficits }})
        </v-tab>
        <v-tab value="exercises">
          <v-icon left>mdi-checkbox-marked-circle</v-icon>
          Uebungen ({{ stats.dueExercises }})
        </v-tab>
        <v-tab value="resolved">
          <v-icon left>mdi-check-all</v-icon>
          Behoben ({{ stats.resolvedDeficits }})
        </v-tab>
      </v-tabs>

      <v-tabs-window v-model="activeTab">
        <v-tabs-window-item value="deficits">
          <DeficitsTab
            :deficits="deficits"
            :loading="loadingDeficits"
            :scheduling-id="schedulingId"
            :resolving-id="resolvingId"
            @schedule-tutoring="handleScheduleTutoring"
            @resolve="handleResolveDeficit"
          />
        </v-tabs-window-item>

        <v-tabs-window-item value="exercises">
          <ExercisesTab
            :exercises="dueExercises"
            :loading="loadingExercises"
            @submit-answer="handleSubmitAnswer"
          />
        </v-tabs-window-item>

        <v-tabs-window-item value="resolved">
          <ResolvedTab
            :deficits="resolvedDeficits"
            :loading="loadingResolved"
          />
        </v-tabs-window-item>
      </v-tabs-window>
    </v-card>

    <v-snackbar v-model="snackbar.show" :color="snackbar.color" :timeout="4000">
      {{ snackbar.message }}
    </v-snackbar>
  </v-container>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useDisplay } from 'vuetify'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useLearning } from '@/composables/useLearning'
import type { Exercise } from '@/types/learning'
import {
  StreakWidget, PriorityCard, DifficultyDistribution,
  DeficitsTab, ExercisesTab, ResolvedTab, LearningStatsCards
} from '@/components/learning'

const { mobile } = useDisplay()
const router = useRouter()
const isMobile = computed(() => mobile.value)
const authStore = useAuthStore()

const {
  stats, deficits, dueExercises, resolvedDeficits,
  loadingDeficits, loadingExercises, loadingResolved,
  loadStats, loadDeficits, loadExercises, loadResolvedDeficits,
  scheduleTutoring, resolveDeficit, submitAnswer
} = useLearning()

const activeTab = ref('deficits')
const showPromoBanner = ref(true)

const schedulingId = ref<number | null>(null)
const resolvingId = ref<number | null>(null)

const snackbar = ref({ show: false, message: '', color: 'success' })
const showMessage = (message: string, color = 'success') => {
  snackbar.value = { show: true, message, color }
}

const handleScheduleTutoring = async (deficitId: number) => {
  if (!authStore.user?.id) return
  schedulingId.value = deficitId
  try {
    const result = await scheduleTutoring(deficitId, authStore.user.id)
    if (result.success) {
      showMessage(`${result.exercises} Übungen generiert und ${result.sessions} Lernzeiten eingeplant!`)
      await reloadData()
    }
  } catch (error: any) {
    showMessage(error.response?.data?.message || 'Fehler beim Planen der Nachhilfe', 'error')
  } finally {
    schedulingId.value = null
  }
}

const handleResolveDeficit = async (deficitId: number) => {
  if (!authStore.user?.id) return
  resolvingId.value = deficitId
  try {
    const result = await resolveDeficit(deficitId, authStore.user.id)
    if (result.success) {
      showMessage('Defizit als behoben markiert')
      await reloadData()
    }
  } catch (error: any) {
    showMessage(error.response?.data?.message || 'Fehler beim Beheben des Defizits', 'error')
  } finally {
    resolvingId.value = null
  }
}

const handleSubmitAnswer = async (exercise: Exercise) => {
  if (!authStore.user?.id || !exercise.userInput) return
  try {
    const result = await submitAnswer(exercise.id, authStore.user.id, exercise.userInput)
    if (result.success) {
      exercise.answered = true
      exercise.isCorrect = result.data.isCorrect
      exercise.explanation = result.data.explanation
      if (exercise.isCorrect) {
        showMessage('Richtig! Weiter so!')
        setTimeout(() => {
          dueExercises.value = dueExercises.value.filter(e => e.id !== exercise.id)
          loadStats(authStore.user!.id)
        }, 2000)
      } else {
        showMessage('Nicht ganz richtig. Versuch es nochmal!', 'warning')
      }
    }
  } catch (error: any) {
    showMessage(error.response?.data?.message || 'Fehler beim Prüfen der Antwort', 'error')
  }
}

const onPriorityLearn = (priority: { topic: string; subject: string }) => {
  // Redirect to OmniLernen with pre-selected topic
  router.push({
    name: 'omni-lernen',
    query: { subject: priority.subject, topic: priority.topic }
  })
}

const reloadData = async () => {
  if (!authStore.user?.id) return
  await Promise.all([
    loadStats(authStore.user.id),
    loadDeficits(authStore.user.id),
    loadExercises(authStore.user.id),
    loadResolvedDeficits(authStore.user.id)
  ])
}

onMounted(() => {
  if (authStore.user?.id) reloadData()
})
</script>
