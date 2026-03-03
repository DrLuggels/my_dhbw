<template>
  <v-container :class="{ 'pa-0': isMobile && showExercisePlayer }">
    <!-- Header (hidden during exercise) -->
    <div v-if="!showExercisePlayer" class="d-flex justify-space-between align-center mb-4 mb-md-6">
      <div class="d-flex align-center">
        <v-btn icon variant="text" @click="$router.back()" class="mr-2 mr-md-3">
          <v-icon>mdi-arrow-left</v-icon>
        </v-btn>
        <h1 :class="isMobile ? 'text-h5' : 'text-h3'">
          <v-icon left color="primary">mdi-school</v-icon>
          Lernbereich
        </h1>
        <StreakWidget compact variant="text" :show-action="false" class="ml-4 d-none d-sm-flex" />
      </div>

      <v-btn-toggle v-model="exerciseMode" mandatory density="compact" class="d-none d-sm-flex">
        <v-btn v-for="item in exerciseModeItems" :key="item.value" :value="item.value" size="small">
          <v-icon start>{{ item.icon }}</v-icon>
          {{ item.text }}
        </v-btn>
      </v-btn-toggle>
    </div>

    <!-- Mobile Mode Selector -->
    <v-select
      v-if="isMobile && !showExercisePlayer"
      v-model="exerciseMode"
      :items="exerciseModeItems"
      item-title="text"
      item-value="value"
      density="compact"
      variant="outlined"
      class="mb-4"
      hide-details
    />

    <!-- Priority Recommendations -->
    <v-row v-if="!showExercisePlayer" class="mb-4">
      <v-col cols="12" md="8">
        <PriorityCard :max-items="3" :show-details="false" @learn="onPriorityLearn" />
      </v-col>
      <v-col cols="12" md="4">
        <DifficultyDistribution variant="outlined" />
      </v-col>
    </v-row>

    <!-- Statistics Cards -->
    <LearningStatsCards
      v-if="!showExercisePlayer"
      :stats="stats"
      @practice-now="activeTab = 'exercises'"
    />

    <!-- Tabs -->
    <v-card v-if="!showExercisePlayer" class="mt-6">
      <v-tabs v-model="activeTab" bg-color="primary">
        <v-tab value="deficits">
          <v-icon left>mdi-alert-circle</v-icon>
          Defizite ({{ stats.activeDeficits }})
        </v-tab>
        <v-tab value="exercises">
          <v-icon left>mdi-checkbox-marked-circle</v-icon>
          Übungen ({{ stats.dueExercises }})
        </v-tab>
        <v-tab value="resolved">
          <v-icon left>mdi-check-all</v-icon>
          Behoben ({{ stats.resolvedDeficits }})
        </v-tab>
        <v-tab value="interactive">
          <v-icon left>mdi-star</v-icon>
          Interaktiv
        </v-tab>
        <v-tab value="engine">
          <v-icon left>mdi-brain</v-icon>
          Lern-Engine
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

        <v-tabs-window-item value="interactive">
          <InteractiveTab
            :current-exercise="currentInteractiveExercise"
            :loading="loadingInteractive"
            v-model:subject="interactiveSubject"
            v-model:topic="interactiveTopic"
            v-model:difficulty="interactiveDifficulty"
            @generate="generateInteractiveExercise"
            @complete="onInteractiveComplete"
            @close="currentInteractiveExercise = null"
          />
        </v-tabs-window-item>

        <v-tabs-window-item value="engine">
          <LearningEngineTab />
        </v-tabs-window-item>
      </v-tabs-window>
    </v-card>

    <!-- Fullscreen Interactive Exercise Player (Mobile) -->
    <v-dialog v-model="showExercisePlayer" fullscreen transition="dialog-bottom-transition">
      <v-card v-if="currentInteractiveExercise">
        <v-toolbar color="primary" density="compact">
          <v-btn icon @click="showExercisePlayer = false">
            <v-icon>mdi-close</v-icon>
          </v-btn>
          <v-toolbar-title>{{ currentInteractiveExercise.topic }}</v-toolbar-title>
        </v-toolbar>
        <InteractiveExercisePlayer
          :exercise="currentInteractiveExercise"
          @complete="onInteractiveComplete"
          @close="closeExercisePlayer"
        />
      </v-card>
    </v-dialog>

    <v-snackbar v-model="snackbar.show" :color="snackbar.color" :timeout="4000">
      {{ snackbar.message }}
    </v-snackbar>
  </v-container>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useDisplay } from 'vuetify'
import { useAuthStore } from '@/stores/auth'
import { useLearning, useInteractiveExercises } from '@/composables/useLearning'
import { exerciseModeItems } from '@/types/learning'
import type { Exercise } from '@/types/learning'
import { InteractiveExercisePlayer } from '@/components/exercises'
import {
  StreakWidget, PriorityCard, DifficultyDistribution,
  DeficitsTab, ExercisesTab, ResolvedTab, InteractiveTab, LearningStatsCards
} from '@/components/learning'
import { LearningEngineTab } from '@/components/learningEngine'

const { mobile } = useDisplay()
const isMobile = computed(() => mobile.value)
const authStore = useAuthStore()

const {
  stats, deficits, dueExercises, resolvedDeficits,
  loadingDeficits, loadingExercises, loadingResolved,
  loadStats, loadDeficits, loadExercises, loadResolvedDeficits,
  scheduleTutoring, resolveDeficit, submitAnswer
} = useLearning()

const { currentExercise: currentInteractiveExercise, loading: loadingInteractive, generate, reset } = useInteractiveExercises()

const exerciseMode = ref<'learning' | 'exam_prep' | 'exam_simulation'>('learning')
const activeTab = ref('deficits')
const showExercisePlayer = ref(false)

const interactiveSubject = ref('')
const interactiveTopic = ref('')
const interactiveDifficulty = ref<'easy' | 'medium' | 'hard'>('easy')

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

const generateInteractiveExercise = async () => {
  if (!authStore.user?.id || !interactiveSubject.value || !interactiveTopic.value) return
  try {
    await generate(authStore.user.id, interactiveSubject.value, interactiveTopic.value, interactiveDifficulty.value)
    if (isMobile.value) showExercisePlayer.value = true
    showMessage('Interaktive Übung generiert!')
  } catch (error: any) {
    showMessage(error.response?.data?.message || 'Fehler beim Generieren der Übung', 'error')
  }
}

const onInteractiveComplete = async (result: { score: number; stepResults: any[] }) => {
  showMessage(`Übung abgeschlossen! Score: ${Math.round(result.score * 100)}%`)
  reset()
  showExercisePlayer.value = false
  if (authStore.user?.id) await loadStats(authStore.user.id)
}

const closeExercisePlayer = () => {
  showExercisePlayer.value = false
  reset()
}

const onPriorityLearn = (priority: { topic: string; subject: string }) => {
  interactiveSubject.value = priority.subject
  interactiveTopic.value = priority.topic
  activeTab.value = 'interactive'
  generateInteractiveExercise()
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
