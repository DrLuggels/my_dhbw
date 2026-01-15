<template>
  <v-card class="exercise-player" :class="{ 'mobile': isMobile }">
    <!-- Progress Header -->
    <v-card-title class="d-flex align-center pa-3 pa-md-4">
      <v-icon class="mr-2" color="primary">mdi-school</v-icon>
      <span class="text-truncate">{{ exercise?.subject }} - {{ exercise?.topic }}</span>
      <v-spacer />
      <v-chip size="small" :color="difficultyColor" variant="flat">
        {{ exercise?.difficulty }}
      </v-chip>
    </v-card-title>

    <!-- Step Progress Bar -->
    <v-progress-linear
      :model-value="progressPercent"
      color="primary"
      height="8"
      class="mb-0"
    />

    <!-- Step Indicators (Desktop) -->
    <div v-if="!isMobile" class="step-indicators pa-3">
      <v-chip
        v-for="(step, index) in steps"
        :key="step.id"
        :color="getStepColor(index)"
        :variant="currentStepIndex === index ? 'flat' : 'outlined'"
        size="small"
        class="mr-2"
        @click="canNavigateToStep(index) && goToStep(index)"
      >
        {{ index + 1 }}. {{ step.title }}
      </v-chip>
    </div>

    <!-- Mobile Step Indicator -->
    <div v-else class="text-center pa-2 text-caption">
      Schritt {{ currentStepIndex + 1 }} von {{ steps.length }}
    </div>

    <v-divider />

    <!-- Current Step Content -->
    <v-card-text class="pa-3 pa-md-4">
      <div v-if="currentStep">
        <!-- Step Title -->
        <h3 class="text-h6 mb-3">{{ currentStep.title }}</h3>

        <!-- Instruction -->
        <div class="instruction-text mb-4" v-html="currentStep.instruction" />

        <!-- Dynamic Component -->
        <component
          :is="getComponentForType(currentStep.component.type)"
          :config="currentStep.component"
          :disabled="isSubmitting || stepCompleted"
          v-model="currentAnswer"
          @change="handleAnswerChange"
        />

        <!-- Hints -->
        <v-expand-transition>
          <div v-if="showHints && currentStep.hints?.length" class="mt-4">
            <v-alert
              v-for="(hint, idx) in visibleHints"
              :key="idx"
              type="info"
              variant="tonal"
              density="compact"
              class="mb-2"
            >
              <template #prepend>
                <v-icon>mdi-lightbulb</v-icon>
              </template>
              <span v-html="hint.content" />
            </v-alert>
          </div>
        </v-expand-transition>

        <!-- Feedback -->
        <v-expand-transition>
          <div v-if="feedback" class="mt-4">
            <v-alert
              :type="feedback.isCorrect ? 'success' : 'error'"
              variant="tonal"
              :icon="feedback.isCorrect ? 'mdi-check-circle' : 'mdi-close-circle'"
            >
              <div class="font-weight-medium">{{ feedback.message }}</div>
              <div v-if="feedback.explanation" class="mt-2 text-body-2" v-html="feedback.explanation" />
            </v-alert>
          </div>
        </v-expand-transition>
      </div>
    </v-card-text>

    <v-divider />

    <!-- Actions -->
    <v-card-actions class="pa-3 pa-md-4 flex-wrap ga-2">
      <!-- Hint Button -->
      <v-btn
        v-if="currentStep?.hints?.length && !stepCompleted"
        variant="text"
        size="small"
        :disabled="hintsUsed >= currentStep.hints.length"
        @click="requestHint"
      >
        <v-icon start>mdi-lightbulb-outline</v-icon>
        Tipp ({{ hintsUsed }}/{{ currentStep?.hints?.length || 0 }})
      </v-btn>

      <v-spacer />

      <!-- Navigation -->
      <v-btn
        v-if="currentStepIndex > 0"
        variant="outlined"
        size="small"
        @click="previousStep"
      >
        <v-icon start>mdi-arrow-left</v-icon>
        <span class="d-none d-sm-inline">Zuruck</span>
      </v-btn>

      <!-- Submit / Next -->
      <v-btn
        v-if="!stepCompleted"
        color="primary"
        :loading="isSubmitting"
        :disabled="!canSubmit"
        @click="submitStep"
      >
        Prufen
        <v-icon end>mdi-check</v-icon>
      </v-btn>

      <v-btn
        v-else-if="currentStepIndex < steps.length - 1"
        color="primary"
        @click="nextStep"
      >
        <span class="d-none d-sm-inline">Weiter</span>
        <v-icon end>mdi-arrow-right</v-icon>
      </v-btn>

      <v-btn
        v-else
        color="success"
        @click="completeExercise"
      >
        <v-icon start>mdi-flag-checkered</v-icon>
        Abschliessen
      </v-btn>
    </v-card-actions>

    <!-- Completion Dialog -->
    <v-dialog v-model="showCompletionDialog" max-width="400">
      <v-card>
        <v-card-title class="text-center pt-6">
          <v-icon size="64" color="success">mdi-trophy</v-icon>
        </v-card-title>
        <v-card-text class="text-center">
          <h2 class="text-h5 mb-2">Geschafft!</h2>
          <p class="text-body-1 mb-4">Du hast die Aufgabe abgeschlossen.</p>
          <v-chip color="primary" size="large">
            {{ Math.round(exercise?.score || 0) }}% Punkte
          </v-chip>
        </v-card-text>
        <v-card-actions class="justify-center pb-4">
          <v-btn color="primary" variant="flat" @click="$emit('complete', exercise)">
            Weiter
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-card>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useDisplay } from 'vuetify'
import MultipleChoice from './MultipleChoice.vue'
import DragDrop from './DragDrop.vue'
import FillInBlank from './FillInBlank.vue'
import TextInput from './TextInput.vue'
import api from '@/services/api'

interface ExerciseData {
  id: number
  subject: string
  topic: string
  difficulty: string
  exerciseContent: string
  stepProgress: string
  completedSteps: number
  totalSteps: number
  score: number
}

interface Props {
  exercise?: ExerciseData
  exerciseId?: number
}

const props = defineProps<Props>()
const emit = defineEmits(['complete', 'progress', 'close'])

const { mobile: isMobile } = useDisplay()

// State
const exercise = ref<any>(null)
const steps = ref<any[]>([])
const currentStepIndex = ref(0)
const currentAnswer = ref<any>(null)
const stepProgress = ref<Record<string, any>>({})
const feedback = ref<any>(null)
const isSubmitting = ref(false)
const showHints = ref(false)
const hintsUsed = ref(0)
const showCompletionDialog = ref(false)

// Computed
const currentStep = computed(() => steps.value[currentStepIndex.value])

const stepCompleted = computed(() => {
  if (!currentStep.value) return false
  return stepProgress.value[currentStep.value.id]?.completed || false
})

const progressPercent = computed(() => {
  if (!steps.value.length) return 0
  const completed = Object.values(stepProgress.value).filter((p: any) => p.completed).length
  return (completed / steps.value.length) * 100
})

const difficultyColor = computed(() => {
  switch (exercise.value?.difficulty) {
    case 'easy': return 'success'
    case 'medium': return 'warning'
    case 'hard': return 'error'
    default: return 'grey'
  }
})

const canSubmit = computed(() => {
  if (!currentAnswer.value) return false
  if (typeof currentAnswer.value === 'string') return currentAnswer.value.trim().length > 0
  if (Array.isArray(currentAnswer.value)) return currentAnswer.value.length > 0
  if (typeof currentAnswer.value === 'object') return Object.keys(currentAnswer.value).length > 0
  return true
})

const visibleHints = computed(() => {
  if (!currentStep.value?.hints) return []
  return currentStep.value.hints.slice(0, hintsUsed.value)
})

// Methods
function getComponentForType(type: string) {
  const components: Record<string, any> = {
    'multiple_choice': MultipleChoice,
    'drag_drop': DragDrop,
    'fill_blank': FillInBlank,
    'text_input': TextInput,
    'slider_range': TextInput, // Fallback
    'code_editor': TextInput, // Fallback
  }
  return components[type] || TextInput
}

function getStepColor(index: number) {
  const progress = stepProgress.value[steps.value[index]?.id]
  if (progress?.completed) return 'success'
  if (index === currentStepIndex.value) return 'primary'
  return 'grey'
}

function canNavigateToStep(index: number) {
  // Can always go back
  if (index < currentStepIndex.value) return true
  // Can only go forward if current is completed
  if (index === currentStepIndex.value + 1 && stepCompleted.value) return true
  return false
}

function goToStep(index: number) {
  if (canNavigateToStep(index)) {
    currentStepIndex.value = index
    resetStepState()
  }
}

function previousStep() {
  if (currentStepIndex.value > 0) {
    currentStepIndex.value--
    resetStepState()
  }
}

function nextStep() {
  if (currentStepIndex.value < steps.value.length - 1) {
    currentStepIndex.value++
    resetStepState()
  }
}

function resetStepState() {
  feedback.value = null
  showHints.value = false
  hintsUsed.value = stepProgress.value[currentStep.value?.id]?.hintsUsed || 0
  currentAnswer.value = stepProgress.value[currentStep.value?.id]?.userAnswer || null
}

function handleAnswerChange(value: any) {
  currentAnswer.value = value
  feedback.value = null
}

function requestHint() {
  if (currentStep.value?.hints && hintsUsed.value < currentStep.value.hints.length) {
    hintsUsed.value++
    showHints.value = true
  }
}

const exerciseId = computed(() => props.exercise?.id || props.exerciseId)

async function submitStep() {
  if (!currentStep.value || isSubmitting.value || !exerciseId.value) return

  isSubmitting.value = true
  feedback.value = null

  try {
    const response = await api.post(
      `/exercises/interactive/${exerciseId.value}/steps/${currentStep.value.id}/submit`,
      currentAnswer.value
    )

    const result = response.data.data

    // Update feedback
    feedback.value = {
      isCorrect: result.validation.isCorrect,
      message: result.validation.feedback,
      explanation: result.validation.explanation
    }

    // Update progress
    stepProgress.value[currentStep.value.id] = {
      completed: result.validation.isCorrect,
      score: result.validation.score,
      userAnswer: currentAnswer.value,
      hintsUsed: hintsUsed.value
    }

    // Update exercise data
    exercise.value = {
      ...exercise.value,
      score: result.exercise.Score,
      completedSteps: result.exercise.CompletedSteps
    }

    emit('progress', {
      stepId: currentStep.value.id,
      isCorrect: result.validation.isCorrect,
      score: result.validation.score
    })

  } catch (error) {
    console.error('Error submitting step:', error)
    feedback.value = {
      isCorrect: false,
      message: 'Fehler beim Prufen. Bitte versuche es erneut.'
    }
  } finally {
    isSubmitting.value = false
  }
}

async function completeExercise() {
  if (!exerciseId.value) return

  try {
    await api.post(`/exercises/interactive/${exerciseId.value}/complete`)
    showCompletionDialog.value = true
  } catch (error) {
    console.error('Error completing exercise:', error)
  }
}

function parseExerciseData(data: ExerciseData) {
  exercise.value = data

  // Parse exercise content (handle both PascalCase and camelCase)
  try {
    const content = JSON.parse(data.exerciseContent)
    steps.value = content.Steps || content.steps || []
  } catch {
    steps.value = []
  }

  // Parse step progress
  try {
    if (data.stepProgress) {
      const progress = JSON.parse(data.stepProgress)
      stepProgress.value = progress.Steps || progress.steps || {}
    }
  } catch {
    stepProgress.value = {}
  }

  // Find first incomplete step
  const firstIncomplete = steps.value.findIndex(
    (s: any) => !stepProgress.value[s.id]?.completed
  )
  currentStepIndex.value = firstIncomplete >= 0 ? firstIncomplete : 0
}

async function loadExercise() {
  // If exercise data is provided directly, use it
  if (props.exercise) {
    parseExerciseData(props.exercise)
    return
  }

  // Otherwise load from API
  if (!props.exerciseId) return

  try {
    const response = await api.get(`/exercises/interactive/${props.exerciseId}`)
    parseExerciseData(response.data.data)
  } catch (error) {
    console.error('Error loading exercise:', error)
  }
}

onMounted(() => {
  loadExercise()
})

// Watch for prop changes
watch(() => props.exercise, (newExercise) => {
  if (newExercise) parseExerciseData(newExercise)
}, { immediate: false })

watch(() => props.exerciseId, () => {
  if (props.exerciseId && !props.exercise) loadExercise()
})
</script>

<style scoped>
.exercise-player {
  max-width: 800px;
  margin: 0 auto;
}

.exercise-player.mobile {
  border-radius: 0;
}

.step-indicators {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
}

.instruction-text {
  font-size: 1rem;
  line-height: 1.6;
}

.instruction-text :deep(code) {
  background: rgba(var(--v-theme-primary), 0.1);
  padding: 2px 6px;
  border-radius: 4px;
  font-family: monospace;
}

.instruction-text :deep(pre) {
  background: rgba(0, 0, 0, 0.05);
  padding: 12px;
  border-radius: 8px;
  overflow-x: auto;
}
</style>
