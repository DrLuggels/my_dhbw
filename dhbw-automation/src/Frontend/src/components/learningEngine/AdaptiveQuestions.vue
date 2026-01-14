<template>
  <v-card>
    <v-card-title class="d-flex align-center">
      <v-icon start>mdi-head-question</v-icon>
      Adaptive Fragen
      <v-spacer />
      <v-chip v-if="questions.length > 0" size="small" color="primary">
        {{ currentIndex + 1 }} / {{ questions.length }}
      </v-chip>
    </v-card-title>

    <v-card-text>
      <!-- No questions state -->
      <div v-if="!loading && questions.length === 0" class="text-center py-6">
        <v-icon size="64" color="primary" class="mb-4">mdi-brain</v-icon>
        <h3 class="text-h6 mb-2">Lernfragen generieren</h3>
        <p class="text-body-2 text-medium-emphasis mb-6">
          Generiere adaptive Fragen basierend auf deinem Wissensgraphen.
          Die Fragen passen sich deinem Lernfortschritt an.
        </p>

        <!-- Generation Form -->
        <v-card variant="outlined" class="pa-4 text-left mx-auto" max-width="500">
          <v-select
            v-model="generationMode"
            :items="generationModes"
            item-title="label"
            item-value="value"
            label="Fragenquelle"
            variant="outlined"
            density="compact"
            class="mb-3"
          />

          <v-text-field
            v-if="generationMode === 'subject'"
            v-model="filterSubject"
            label="Fach"
            placeholder="z.B. Informatik"
            variant="outlined"
            density="compact"
            class="mb-3"
          />

          <v-text-field
            v-if="generationMode === 'topic'"
            v-model="filterTopic"
            label="Thema"
            placeholder="z.B. Algorithmen"
            variant="outlined"
            density="compact"
            class="mb-3"
          />

          <v-slider
            v-model="questionCount"
            :min="3"
            :max="20"
            :step="1"
            label="Anzahl Fragen"
            thumb-label
            class="mb-3"
          />

          <v-select
            v-model="difficulty"
            :items="difficultyOptions"
            item-title="label"
            item-value="value"
            label="Schwierigkeit"
            variant="outlined"
            density="compact"
            class="mb-3"
          />

          <v-select
            v-model="selectedQuestionTypes"
            :items="questionTypeOptions"
            item-title="label"
            item-value="value"
            label="Fragentypen"
            variant="outlined"
            density="compact"
            multiple
            chips
            closable-chips
          />
        </v-card>

        <v-btn
          color="primary"
          size="large"
          class="mt-6"
          :loading="loading"
          @click="generateNewQuestions"
        >
          <v-icon start>mdi-auto-fix</v-icon>
          Fragen generieren
        </v-btn>
      </div>

      <!-- Loading state -->
      <div v-else-if="loading" class="text-center py-8">
        <v-progress-circular indeterminate color="primary" size="48" />
        <p class="mt-4 text-body-1">Generiere Fragen...</p>
      </div>

      <!-- Question display -->
      <div v-else-if="currentQuestion">
        <!-- Question header -->
        <div class="d-flex align-center mb-4">
          <v-chip
            :color="getBloomLevelColor(currentQuestion.bloomLevel)"
            size="small"
            variant="tonal"
          >
            {{ getBloomLevelName(currentQuestion.bloomLevel) }}
          </v-chip>
          <v-chip
            size="small"
            variant="outlined"
            class="ml-2"
          >
            {{ getQuestionTypeLabel(currentQuestion.questionType) }}
          </v-chip>
          <v-spacer />
          <v-chip
            v-if="currentQuestion.entityName"
            size="small"
            color="info"
            variant="tonal"
          >
            {{ currentQuestion.entityName }}
          </v-chip>
        </div>

        <!-- Question text -->
        <v-card variant="tonal" color="primary" class="pa-4 mb-4">
          <p class="text-body-1 font-weight-medium">{{ currentQuestion.question }}</p>
        </v-card>

        <!-- Answer input based on question type -->
        <div class="mb-4">
          <!-- Multiple Choice -->
          <v-radio-group
            v-if="currentQuestion.questionType === 'mc' && currentQuestion.options"
            v-model="userAnswer"
            :disabled="showFeedback"
          >
            <v-radio
              v-for="(option, i) in currentQuestion.options"
              :key="i"
              :label="option"
              :value="option"
              :color="getOptionColor(option)"
            />
          </v-radio-group>

          <!-- True/False -->
          <v-radio-group
            v-else-if="currentQuestion.questionType === 'true_false'"
            v-model="userAnswer"
            :disabled="showFeedback"
            inline
          >
            <v-radio label="Wahr" value="Wahr" :color="getOptionColor('Wahr')" />
            <v-radio label="Falsch" value="Falsch" :color="getOptionColor('Falsch')" />
          </v-radio-group>

          <!-- Fill blank / Short answer -->
          <v-text-field
            v-else
            v-model="userAnswer"
            label="Deine Antwort"
            variant="outlined"
            :disabled="showFeedback"
            @keyup.enter="submitCurrentAnswer"
          />
        </div>

        <!-- Hint button -->
        <v-btn
          v-if="currentQuestion.hint && !showHint && !showFeedback"
          variant="text"
          color="info"
          size="small"
          class="mb-4"
          @click="showHint = true"
        >
          <v-icon start>mdi-lightbulb-outline</v-icon>
          Hinweis anzeigen
        </v-btn>

        <!-- Hint display -->
        <v-alert
          v-if="showHint && currentQuestion.hint"
          type="info"
          variant="tonal"
          class="mb-4"
        >
          {{ currentQuestion.hint }}
        </v-alert>

        <!-- Feedback display -->
        <v-expand-transition>
          <v-alert
            v-if="showFeedback && feedback"
            :type="feedback.isCorrect ? 'success' : 'error'"
            class="mb-4"
          >
            <div class="d-flex align-center mb-2">
              <strong>{{ feedback.isCorrect ? 'Richtig!' : 'Leider falsch' }}</strong>
              <v-spacer />
              <span class="text-caption">
                Beherrschung: {{ formatPercent(feedback.newMasteryScore) }}
                <span :class="feedback.masteryChange >= 0 ? 'text-success' : 'text-error'">
                  ({{ feedback.masteryChange >= 0 ? '+' : '' }}{{ formatPercent(feedback.masteryChange) }})
                </span>
              </span>
            </div>
            <p v-if="!feedback.isCorrect">
              <strong>Richtige Antwort:</strong> {{ feedback.correctAnswer }}
            </p>
            <p v-if="feedback.explanation" class="mt-2">
              {{ feedback.explanation }}
            </p>
            <p v-if="feedback.feedback" class="mt-2 font-italic">
              {{ feedback.feedback }}
            </p>
            <div v-if="feedback.relatedTopicsToStudy && feedback.relatedTopicsToStudy.length > 0" class="mt-2">
              <strong>Empfohlene Themen:</strong>
              <v-chip
                v-for="topic in feedback.relatedTopicsToStudy"
                :key="topic"
                size="x-small"
                class="ml-1"
              >
                {{ topic }}
              </v-chip>
            </div>
          </v-alert>
        </v-expand-transition>

        <!-- Action buttons -->
        <div class="d-flex gap-2">
          <v-btn
            v-if="!showFeedback"
            color="primary"
            :disabled="!userAnswer"
            :loading="submitting"
            @click="submitCurrentAnswer"
          >
            Antwort prüfen
          </v-btn>
          <v-btn
            v-else
            color="primary"
            @click="nextQuestionClick"
          >
            {{ currentIndex < questions.length - 1 ? 'Nächste Frage' : 'Abschließen' }}
          </v-btn>
          <v-btn
            variant="text"
            @click="resetSession"
          >
            Neue Fragen
          </v-btn>
        </div>

        <!-- Progress -->
        <v-progress-linear
          :model-value="progressPercent"
          color="primary"
          height="4"
          class="mt-4"
          rounded
        />
      </div>

      <!-- Session complete -->
      <div v-else-if="sessionComplete" class="text-center py-6">
        <v-icon size="64" color="success" class="mb-4">mdi-check-circle</v-icon>
        <h3 class="text-h6 mb-2">Sitzung abgeschlossen!</h3>

        <v-card variant="tonal" color="primary" class="pa-4 mx-auto mb-6" max-width="400">
          <div class="d-flex justify-space-around">
            <div class="text-center">
              <div class="text-h5">{{ sessionStats.correct }}</div>
              <div class="text-caption text-success">Richtig</div>
            </div>
            <div class="text-center">
              <div class="text-h5">{{ sessionStats.incorrect }}</div>
              <div class="text-caption text-error">Falsch</div>
            </div>
            <div class="text-center">
              <div class="text-h5">{{ sessionStats.accuracy }}%</div>
              <div class="text-caption">Genauigkeit</div>
            </div>
          </div>
        </v-card>

        <v-btn color="primary" @click="resetSession">
          <v-icon start>mdi-refresh</v-icon>
          Neue Fragen generieren
        </v-btn>
      </div>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import type {
  LearningQuestion,
  AnswerFeedback,
  QuestionGenerationRequest
} from '@/types/learningEngine'
import { bloomLevels, questionTypes } from '@/types/learningEngine'

const props = defineProps<{
  questions: LearningQuestion[]
  loading?: boolean
  submitting?: boolean
}>()

const emit = defineEmits<{
  generate: [request: QuestionGenerationRequest]
  submit: [questionId: string, answer: string, entityId?: number]
  next: []
  reset: []
}>()

// State
const currentIndex = ref(0)
const userAnswer = ref('')
const showHint = ref(false)
const showFeedback = ref(false)
const feedback = ref<AnswerFeedback | null>(null)
const sessionComplete = ref(false)
const sessionStats = ref({ correct: 0, incorrect: 0, accuracy: 0 })

// Generation options
const generationMode = ref<'adaptive' | 'subject' | 'topic' | 'weak_areas'>('adaptive')
const filterSubject = ref('')
const filterTopic = ref('')
const questionCount = ref(10)
const difficulty = ref<'easy' | 'medium' | 'hard' | 'adaptive'>('adaptive')
const selectedQuestionTypes = ref<string[]>(['mc', 'true_false', 'short_answer'])

const generationModes = [
  { value: 'adaptive', label: 'Adaptiv (basierend auf Lernfortschritt)' },
  { value: 'weak_areas', label: 'Schwachstellen gezielt üben' },
  { value: 'subject', label: 'Nach Fach' },
  { value: 'topic', label: 'Nach Thema' }
]

const difficultyOptions = [
  { value: 'adaptive', label: 'Adaptiv' },
  { value: 'easy', label: 'Einfach' },
  { value: 'medium', label: 'Mittel' },
  { value: 'hard', label: 'Schwer' }
]

const questionTypeOptions = questionTypes.map(t => ({
  value: t.value,
  label: t.label
}))

const currentQuestion = computed(() => props.questions[currentIndex.value] || null)

const progressPercent = computed(() => {
  if (props.questions.length === 0) return 0
  return ((currentIndex.value + 1) / props.questions.length) * 100
})

const generateNewQuestions = () => {
  const request: QuestionGenerationRequest = {
    count: questionCount.value,
    difficulty: difficulty.value,
    questionTypes: selectedQuestionTypes.value
  }

  if (generationMode.value === 'subject' && filterSubject.value) {
    request.subject = filterSubject.value
  }
  if (generationMode.value === 'topic' && filterTopic.value) {
    request.topic = filterTopic.value
  }

  emit('generate', request)
}

const submitCurrentAnswer = () => {
  if (!currentQuestion.value || !userAnswer.value) return
  emit('submit', currentQuestion.value.id, userAnswer.value, currentQuestion.value.entityId)
}

const setFeedback = (fb: AnswerFeedback) => {
  feedback.value = fb
  showFeedback.value = true

  if (fb.isCorrect) {
    sessionStats.value.correct++
  } else {
    sessionStats.value.incorrect++
  }

  const total = sessionStats.value.correct + sessionStats.value.incorrect
  sessionStats.value.accuracy = Math.round((sessionStats.value.correct / total) * 100)
}

const nextQuestionClick = () => {
  if (currentIndex.value < props.questions.length - 1) {
    currentIndex.value++
    userAnswer.value = ''
    showHint.value = false
    showFeedback.value = false
    feedback.value = null
  } else {
    sessionComplete.value = true
  }
}

const resetSession = () => {
  currentIndex.value = 0
  userAnswer.value = ''
  showHint.value = false
  showFeedback.value = false
  feedback.value = null
  sessionComplete.value = false
  sessionStats.value = { correct: 0, incorrect: 0, accuracy: 0 }
  emit('reset')
}

const getBloomLevelName = (level: number): string => {
  const info = bloomLevels.find(b => b.level === level)
  return info?.name || `Level ${level}`
}

const getBloomLevelColor = (level: number): string => {
  const colors = ['grey', 'green', 'blue', 'orange', 'purple', 'red']
  return colors[Math.min(level, colors.length - 1)] || 'grey'
}

const getQuestionTypeLabel = (type: string): string => {
  const info = questionTypes.find(t => t.value === type)
  return info?.label || type
}

const getOptionColor = (option: string): string | undefined => {
  if (!showFeedback.value || !feedback.value) return undefined
  if (option === feedback.value.correctAnswer) return 'success'
  if (option === userAnswer.value && !feedback.value.isCorrect) return 'error'
  return undefined
}

const formatPercent = (value: number): string => {
  return `${Math.round(value * 100)}%`
}

// Expose setFeedback for parent component
defineExpose({ setFeedback })
</script>
