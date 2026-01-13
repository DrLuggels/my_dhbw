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
        <!-- Streak Widget (kompakt) -->
        <StreakWidget compact variant="text" :show-action="false" class="ml-4 d-none d-sm-flex" />
      </div>

      <!-- Exercise Mode Selector -->
      <v-btn-toggle v-model="exerciseMode" mandatory density="compact" class="d-none d-sm-flex">
        <v-btn value="learning" size="small">
          <v-icon start>mdi-school</v-icon>
          Lernen
        </v-btn>
        <v-btn value="exam_prep" size="small">
          <v-icon start>mdi-file-document</v-icon>
          KA-Prep
        </v-btn>
        <v-btn value="exam_simulation" size="small">
          <v-icon start>mdi-timer</v-icon>
          Simulation
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

    <!-- AKGLS Priority Recommendations (hidden during fullscreen exercise) -->
    <v-row v-if="!showExercisePlayer" class="mb-4">
      <v-col cols="12" md="8">
        <PriorityCard :max-items="3" :show-details="false" @learn="onPriorityLearn" />
      </v-col>
      <v-col cols="12" md="4">
        <DifficultyDistribution variant="outlined" />
      </v-col>
    </v-row>

    <!-- Statistics Cards (hidden during fullscreen exercise) -->
    <v-row v-if="!showExercisePlayer">
      <v-col cols="12" sm="6" md="3">
        <v-card color="primary" variant="tonal">
          <v-card-text class="text-center">
            <div class="text-h3 mb-2">{{ stats.totalDeficits }}</div>
            <div class="text-subtitle-1">Erkannte Defizite</div>
            <v-chip size="small" class="mt-2" color="warning">
              {{ stats.highSeverityDeficits }} kritisch
            </v-chip>
          </v-card-text>
        </v-card>
      </v-col>

      <v-col cols="12" sm="6" md="3">
        <v-card color="success" variant="tonal">
          <v-card-text class="text-center">
            <div class="text-h3 mb-2">{{ stats.completedExercises }}</div>
            <div class="text-subtitle-1">Gelöste Übungen</div>
            <v-progress-linear
              :model-value="exerciseCompletionRate"
              color="success"
              class="mt-2"
            />
          </v-card-text>
        </v-card>
      </v-col>

      <v-col cols="12" sm="6" md="3">
        <v-card color="warning" variant="tonal">
          <v-card-text class="text-center">
            <div class="text-h3 mb-2">{{ stats.dueExercises }}</div>
            <div class="text-subtitle-1">Fällige Übungen</div>
            <v-btn
              v-if="stats.dueExercises > 0"
              size="small"
              color="warning"
              class="mt-2"
              @click="activeTab = 'exercises'"
            >
              Jetzt üben
            </v-btn>
          </v-card-text>
        </v-card>
      </v-col>

      <v-col cols="12" sm="6" md="3">
        <v-card color="info" variant="tonal">
          <v-card-text class="text-center">
            <div class="text-h3 mb-2">{{ stats.averageEaseFactor.toFixed(1) }}</div>
            <div class="text-subtitle-1">Durchschnittlicher EF</div>
            <div class="text-caption mt-2">Spaced Repetition</div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <!-- Tabs (hidden during fullscreen exercise) -->
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
      </v-tabs>

      <v-tabs-window v-model="activeTab">
        <!-- Deficits Tab -->
        <v-tabs-window-item value="deficits">
          <v-card-text>
            <div v-if="loadingDeficits" class="text-center py-8">
              <v-progress-circular indeterminate color="primary" />
            </div>

            <v-alert
              v-else-if="deficits.length === 0"
              type="success"
              variant="tonal"
            >
              Keine aktiven Lerndefizite! Weiter so!
            </v-alert>

            <v-expansion-panels v-else>
              <v-expansion-panel
                v-for="deficit in deficits"
                :key="deficit.id"
              >
                <v-expansion-panel-title>
                  <div class="d-flex align-center w-100">
                    <v-chip
                      :color="getDeficitSeverityColor(deficit.severity)"
                      size="small"
                      class="mr-3"
                    >
                      {{ getSeverityLabel(deficit.severity) }}
                    </v-chip>
                    <strong>{{ deficit.subject }}</strong>
                    <v-icon class="mx-2">mdi-chevron-right</v-icon>
                    {{ deficit.topic }}
                    <v-spacer />
                    <v-chip size="small" variant="text">
                      {{ deficit.occurrenceCount }}x
                    </v-chip>
                  </div>
                </v-expansion-panel-title>

                <v-expansion-panel-text>
                  <v-divider class="mb-4" />

                  <div class="mb-4">
                    <v-chip size="small" class="mr-2">
                      <v-icon left size="small">mdi-alert</v-icon>
                      {{ deficit.errorType }}
                    </v-chip>
                    <v-chip size="small" class="mr-2">
                      <v-icon left size="small">mdi-calendar</v-icon>
                      Zuletzt: {{ formatDate(deficit.lastOccurrence) }}
                    </v-chip>
                  </div>

                  <p class="text-body-1 mb-4">
                    <strong>Fehlerbeschreibung:</strong><br />
                    {{ deficit.errorDescription }}
                  </p>

                  <v-alert
                    v-if="deficit.needsTutoring"
                    type="warning"
                    variant="tonal"
                    class="mb-4"
                  >
                    <strong>Empfehlung:</strong> Dieses Defizit tritt häufig auf. Eine Nachhilfesession wird empfohlen.
                  </v-alert>

                  <div class="d-flex gap-2">
                    <v-btn
                      v-if="deficit.needsTutoring"
                      color="primary"
                      @click="scheduleTutoring(deficit.id)"
                      :loading="schedulingId === deficit.id"
                    >
                      <v-icon left>mdi-calendar-plus</v-icon>
                      Nachhilfe planen (5 Übungen + Lernzeit)
                    </v-btn>

                    <v-btn
                      color="success"
                      variant="outlined"
                      @click="resolveDeficit(deficit.id)"
                      :loading="resolvingId === deficit.id"
                    >
                      <v-icon left>mdi-check</v-icon>
                      Als behoben markieren
                    </v-btn>
                  </div>
                </v-expansion-panel-text>
              </v-expansion-panel>
            </v-expansion-panels>
          </v-card-text>
        </v-tabs-window-item>

        <!-- Exercises Tab -->
        <v-tabs-window-item value="exercises">
          <v-card-text>
            <div v-if="loadingExercises" class="text-center py-8">
              <v-progress-circular indeterminate color="primary" />
            </div>

            <v-alert
              v-else-if="dueExercises.length === 0"
              type="info"
              variant="tonal"
            >
              Keine fälligen Übungen. Schau später wieder vorbei!
            </v-alert>

            <div v-else>
              <v-card
                v-for="exercise in dueExercises"
                :key="exercise.id"
                class="mb-4"
                variant="outlined"
              >
                <v-card-title class="d-flex align-center">
                  <v-chip size="small" color="info" class="mr-2">
                    {{ exercise.subject }}
                  </v-chip>
                  {{ exercise.topic }}
                  <v-spacer />
                  <v-chip
                    size="small"
                    :color="getDifficultyColor(exercise.difficulty)"
                  >
                    {{ exercise.difficulty }}
                  </v-chip>
                </v-card-title>

                <v-card-text>
                  <div class="text-body-1 mb-4" v-html="exercise.question"></div>

                  <v-text-field
                    v-model="exercise.userInput"
                    label="Deine Antwort"
                    variant="outlined"
                    density="comfortable"
                    :disabled="exercise.answered"
                  />

                  <v-expand-transition>
                    <v-alert
                      v-if="exercise.showHelp"
                      type="info"
                      variant="tonal"
                      class="mt-2"
                    >
                      <strong>Hilfe:</strong> <span v-html="exercise.helpText"></span>
                    </v-alert>
                  </v-expand-transition>

                  <v-expand-transition>
                    <v-alert
                      v-if="exercise.answered"
                      :type="exercise.isCorrect ? 'success' : 'error'"
                      variant="tonal"
                      class="mt-2"
                    >
                      <strong v-if="exercise.isCorrect">Richtig!</strong>
                      <strong v-else>Nicht ganz richtig</strong>
                      <div class="mt-2" v-html="exercise.explanation"></div>
                    </v-alert>
                  </v-expand-transition>
                </v-card-text>

                <v-card-actions>
                  <v-btn
                    v-if="!exercise.showHelp && !exercise.answered"
                    variant="text"
                    @click="exercise.showHelp = true"
                  >
                    <v-icon left>mdi-help-circle</v-icon>
                    Hilfe anzeigen
                  </v-btn>
                  <v-spacer />
                  <v-btn
                    v-if="!exercise.answered"
                    color="primary"
                    @click="submitAnswer(exercise)"
                    :disabled="!exercise.userInput"
                  >
                    <v-icon left>mdi-check</v-icon>
                    Antwort prüfen
                  </v-btn>
                </v-card-actions>
              </v-card>
            </div>
          </v-card-text>
        </v-tabs-window-item>

        <!-- Resolved Tab -->
        <v-tabs-window-item value="resolved">
          <v-card-text>
            <div v-if="loadingResolved" class="text-center py-8">
              <v-progress-circular indeterminate color="primary" />
            </div>

            <v-alert
              v-else-if="resolvedDeficits.length === 0"
              type="info"
              variant="tonal"
            >
              Noch keine behobenen Defizite.
            </v-alert>

            <v-list v-else>
              <v-list-item
                v-for="deficit in resolvedDeficits"
                :key="deficit.id"
              >
                <template v-slot:prepend>
                  <v-icon color="success">mdi-check-circle</v-icon>
                </template>

                <v-list-item-title>
                  {{ deficit.subject }} - {{ deficit.topic }}
                </v-list-item-title>

                <v-list-item-subtitle>
                  Behoben am: {{ formatDate(deficit.resolvedAt!) }}
                </v-list-item-subtitle>
              </v-list-item>
            </v-list>
          </v-card-text>
        </v-tabs-window-item>

        <!-- Interactive Exercises Tab -->
        <v-tabs-window-item value="interactive">
          <v-card-text>
            <div v-if="loadingInteractiveExercise" class="text-center py-8">
              <v-progress-circular indeterminate color="primary" />
              <p class="mt-4 text-body-1">Generiere interaktive Übung...</p>
            </div>

            <!-- Generate New Interactive Exercise -->
            <div v-else-if="!currentInteractiveExercise" class="text-center py-6">
              <v-icon size="64" color="primary" class="mb-4">mdi-star-shooting</v-icon>
              <h3 class="text-h6 mb-2">Interaktive Übungen</h3>
              <p class="text-body-2 text-medium-emphasis mb-6">
                Lerne Schritt für Schritt mit interaktiven Aufgaben im Brilliant-Stil.
                Perfekt für neue Konzepte und spielerisches Lernen!
              </p>

              <v-card variant="outlined" class="mb-4 pa-4 text-left" max-width="500" style="margin: 0 auto;">
                <v-text-field
                  v-model="interactiveSubject"
                  label="Fach"
                  placeholder="z.B. Informatik, Mathe..."
                  variant="outlined"
                  density="compact"
                  class="mb-3"
                />
                <v-text-field
                  v-model="interactiveTopic"
                  label="Thema"
                  placeholder="z.B. Binäre Suche, Integrale..."
                  variant="outlined"
                  density="compact"
                  class="mb-3"
                />
                <v-select
                  v-model="interactiveDifficulty"
                  :items="difficultyItems"
                  item-title="text"
                  item-value="value"
                  label="Schwierigkeit"
                  variant="outlined"
                  density="compact"
                />
              </v-card>

              <v-btn
                color="primary"
                size="large"
                @click="generateInteractiveExercise"
                :disabled="!interactiveSubject || !interactiveTopic"
              >
                <v-icon start>mdi-auto-fix</v-icon>
                Interaktive Übung generieren
              </v-btn>
            </div>

            <!-- Interactive Exercise Player -->
            <div v-else>
              <InteractiveExercisePlayer
                :exercise="currentInteractiveExercise"
                @complete="onInteractiveComplete"
                @close="currentInteractiveExercise = null"
              />
            </div>
          </v-card-text>
        </v-tabs-window-item>
      </v-tabs-window>
    </v-card>

    <!-- Fullscreen Interactive Exercise Player (Mobile) -->
    <v-dialog
      v-model="showExercisePlayer"
      fullscreen
      transition="dialog-bottom-transition"
    >
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
          @close="showExercisePlayer = false; currentInteractiveExercise = null"
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
import api from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { InteractiveExercisePlayer } from '@/components/exercises'
import { StreakWidget, PriorityCard, DifficultyDistribution } from '@/components/learning'

// Vuetify display for responsive design
const { mobile } = useDisplay()
const isMobile = computed(() => mobile.value)

// Exercise mode state
const exerciseMode = ref<'learning' | 'exam_prep' | 'exam_simulation'>('learning')
const exerciseModeItems = [
  { value: 'learning', text: 'Lernen', icon: 'mdi-school' },
  { value: 'exam_prep', text: 'KA-Vorbereitung', icon: 'mdi-file-document' },
  { value: 'exam_simulation', text: 'Prüfungssimulation', icon: 'mdi-timer' }
]

// Interactive exercise player state
const showExercisePlayer = ref(false)
const currentInteractiveExercise = ref<InteractiveExerciseData | null>(null)
const loadingInteractiveExercise = ref(false)

// Interface for interactive exercises
interface InteractiveExerciseData {
  id: number
  subject: string
  topic: string
  difficulty: string
  exerciseContent: string
  stepProgress: string
  completedSteps: number
  totalSteps: number
  score: number
  nextReviewDate: string
}

// Form inputs for generating interactive exercises
const interactiveSubject = ref('')
const interactiveTopic = ref('')
const interactiveDifficulty = ref<'easy' | 'medium' | 'hard'>('easy')
const difficultyItems = [
  { value: 'easy', text: 'Leicht - Einführung & Grundlagen' },
  { value: 'medium', text: 'Mittel - Anwendung & Vertiefung' },
  { value: 'hard', text: 'Schwer - Komplexe Szenarien' }
]

interface LearningDeficit {
  id: number
  userId: number
  subject: string
  topic: string
  subtopic?: string
  errorType: string
  errorDescription: string
  occurrenceCount: number
  firstOccurrence: string
  lastOccurrence: string
  severity: string
  needsTutoring: boolean
  relatedDocumentIds: string
  createdAt: string
  resolvedAt?: string
}

interface Exercise {
  id: number
  userId: number
  deficitId?: number
  subject: string
  topic: string
  exerciseType: string
  question: string
  helpText?: string
  correctAnswer: string
  explanation?: string
  difficulty: string
  userAnswer?: string
  isCorrect?: boolean
  answeredAt?: string
  nextReviewDate: string
  reviewCount: number
  easeFactor: number
  createdAt: string
  // UI state
  userInput?: string
  showHelp?: boolean
  answered?: boolean
}

interface Stats {
  totalDeficits: number
  activeDeficits: number
  resolvedDeficits: number
  highSeverityDeficits: number
  totalExercises: number
  completedExercises: number
  pendingExercises: number
  dueExercises: number
  averageEaseFactor: number
}

const authStore = useAuthStore()
const activeTab = ref('deficits')

const stats = ref<Stats>({
  totalDeficits: 0,
  activeDeficits: 0,
  resolvedDeficits: 0,
  highSeverityDeficits: 0,
  totalExercises: 0,
  completedExercises: 0,
  pendingExercises: 0,
  dueExercises: 0,
  averageEaseFactor: 2.5
})

const deficits = ref<LearningDeficit[]>([])
const dueExercises = ref<Exercise[]>([])
const resolvedDeficits = ref<LearningDeficit[]>([])

const loadingDeficits = ref(false)
const loadingExercises = ref(false)
const loadingResolved = ref(false)

const schedulingId = ref<number | null>(null)
const resolvingId = ref<number | null>(null)

const snackbar = ref({
  show: false,
  message: '',
  color: 'success'
})

const exerciseCompletionRate = computed(() => {
  if (stats.value.totalExercises === 0) return 0
  return (stats.value.completedExercises / stats.value.totalExercises) * 100
})

const loadStats = async () => {
  if (!authStore.user?.id) return

  try {
    const response = await api.get(`/learning/stats/${authStore.user.id}`)
    if (response.data.success) {
      stats.value = response.data.data
    }
  } catch (error) {
    console.error('Error loading stats:', error)
  }
}

const loadDeficits = async () => {
  if (!authStore.user?.id) return

  loadingDeficits.value = true
  try {
    const response = await api.get(`/learning/deficits/${authStore.user.id}`)
    if (response.data.success) {
      deficits.value = response.data.data
    }
  } catch (error) {
    console.error('Error loading deficits:', error)
    showMessage('Fehler beim Laden der Defizite', 'error')
  } finally {
    loadingDeficits.value = false
  }
}

const loadExercises = async () => {
  if (!authStore.user?.id) return

  loadingExercises.value = true
  try {
    const response = await api.get(`/learning/exercises/due/${authStore.user.id}`)
    if (response.data.success) {
      dueExercises.value = response.data.data.map((ex: Exercise) => ({
        ...ex,
        userInput: '',
        showHelp: false,
        answered: false
      }))
    }
  } catch (error) {
    console.error('Error loading exercises:', error)
    showMessage('Fehler beim Laden der Übungen', 'error')
  } finally {
    loadingExercises.value = false
  }
}

const loadResolvedDeficits = async () => {
  if (!authStore.user?.id) return

  loadingResolved.value = true
  try {
    // This would need a backend endpoint for resolved deficits
    // For now, we'll filter from deficits
    resolvedDeficits.value = []
  } catch (error) {
    console.error('Error loading resolved deficits:', error)
  } finally {
    loadingResolved.value = false
  }
}

const scheduleTutoring = async (deficitId: number) => {
  schedulingId.value = deficitId

  try {
    const response = await api.post(
      `/learning/schedule-tutoring/${deficitId}?userId=${authStore.user?.id}`
    )

    if (response.data.success) {
      showMessage(
        `${response.data.exercises} Übungen generiert und ${response.data.sessions} Lernzeiten eingeplant!`,
        'success'
      )

      await Promise.all([loadStats(), loadDeficits(), loadExercises()])
    }
  } catch (error: any) {
    console.error('Error scheduling tutoring:', error)
    showMessage(
      error.response?.data?.message || 'Fehler beim Planen der Nachhilfe',
      'error'
    )
  } finally {
    schedulingId.value = null
  }
}

const resolveDeficit = async (deficitId: number) => {
  if (!confirm('Dieses Defizit als behoben markieren?')) return

  resolvingId.value = deficitId

  try {
    const response = await api.patch(
      `/learning/deficits/${deficitId}/resolve?userId=${authStore.user?.id}`
    )

    if (response.data.success) {
      showMessage('Defizit als behoben markiert', 'success')
      await Promise.all([loadStats(), loadDeficits(), loadResolvedDeficits()])
    }
  } catch (error: any) {
    console.error('Error resolving deficit:', error)
    showMessage(
      error.response?.data?.message || 'Fehler beim Beheben des Defizits',
      'error'
    )
  } finally {
    resolvingId.value = null
  }
}

const submitAnswer = async (exercise: Exercise) => {
  if (!authStore.user?.id || !exercise.userInput) return

  try {
    const response = await api.post(`/learning/exercises/${exercise.id}/answer`, {
      userId: authStore.user.id,
      answer: exercise.userInput,
      isCorrect: false // Will be checked by backend
    })

    if (response.data.success) {
      exercise.answered = true
      exercise.isCorrect = response.data.data.isCorrect
      exercise.explanation = response.data.data.explanation

      if (exercise.isCorrect) {
        showMessage('Richtig! Weiter so! 🎉', 'success')
        // Remove from list after 2 seconds
        setTimeout(() => {
          dueExercises.value = dueExercises.value.filter(e => e.id !== exercise.id)
          loadStats()
        }, 2000)
      } else {
        showMessage('Nicht ganz richtig. Versuch es nochmal!', 'warning')
      }
    }
  } catch (error: any) {
    console.error('Error submitting answer:', error)
    showMessage(
      error.response?.data?.message || 'Fehler beim Prüfen der Antwort',
      'error'
    )
  }
}

const getDeficitSeverityColor = (severity: string) => {
  const colors: Record<string, string> = {
    critical: 'error',
    high: 'warning',
    medium: 'info',
    low: 'success'
  }
  return colors[severity] || 'default'
}

const getSeverityLabel = (severity: string) => {
  const labels: Record<string, string> = {
    critical: 'Kritisch',
    high: 'Hoch',
    medium: 'Mittel',
    low: 'Niedrig'
  }
  return labels[severity] || severity
}

const getDifficultyColor = (difficulty: string) => {
  const colors: Record<string, string> = {
    easy: 'success',
    medium: 'warning',
    hard: 'error'
  }
  return colors[difficulty] || 'default'
}

const formatDate = (date: string) => {
  return new Date(date).toLocaleDateString('de-DE', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

const showMessage = (message: string, color: string = 'success') => {
  snackbar.value = { show: true, message, color }
}

// Interactive Exercise Methods
const generateInteractiveExercise = async () => {
  if (!authStore.user?.id || !interactiveSubject.value || !interactiveTopic.value) return

  loadingInteractiveExercise.value = true

  try {
    const response = await api.post('/exercises/interactive/generate', {
      userId: authStore.user.id,
      subject: interactiveSubject.value,
      topic: interactiveTopic.value,
      difficulty: interactiveDifficulty.value
    })

    if (response.data.success) {
      currentInteractiveExercise.value = response.data.data

      // On mobile, show fullscreen
      if (isMobile.value) {
        showExercisePlayer.value = true
      }

      showMessage('Interaktive Übung generiert!', 'success')
    }
  } catch (error: any) {
    console.error('Error generating interactive exercise:', error)
    showMessage(
      error.response?.data?.message || 'Fehler beim Generieren der Übung',
      'error'
    )
  } finally {
    loadingInteractiveExercise.value = false
  }
}

const onInteractiveComplete = async (result: { score: number; stepResults: any[] }) => {
  showMessage(`Übung abgeschlossen! Score: ${Math.round(result.score * 100)}%`, 'success')

  // Reset state
  currentInteractiveExercise.value = null
  showExercisePlayer.value = false

  // Reload stats
  await loadStats()
}

// Handler for PriorityCard "learn" action
const onPriorityLearn = (priority: { topic: string; subject: string }) => {
  // Set the interactive exercise fields based on priority
  interactiveSubject.value = priority.subject
  interactiveTopic.value = priority.topic

  // Switch to interactive tab
  activeTab.value = 'interactive'

  // Auto-generate the exercise
  generateInteractiveExercise()
}

onMounted(() => {
  Promise.all([
    loadStats(),
    loadDeficits(),
    loadExercises(),
    loadResolvedDeficits()
  ])
})
</script>

<style scoped>
.gap-2 {
  gap: 0.5rem;
}
</style>
