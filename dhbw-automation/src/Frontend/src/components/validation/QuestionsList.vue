<template>
  <v-list>
    <v-list-item
      v-for="question in sortedQuestions"
      :key="question.id"
      class="question-item mb-3"
    >
      <v-card variant="outlined" :color="getQuestionBorderColor(question)">
        <v-card-text>
          <div class="d-flex align-center mb-2">
            <v-chip
              :color="getPriorityColor(question.priority)"
              size="small"
              class="mr-2"
            >
              {{ question.priority }}
            </v-chip>
            <v-chip v-if="question.isAnswered" color="success" size="small">
              <v-icon start size="x-small">mdi-check</v-icon>
              Beantwortet
            </v-chip>
          </div>

          <div class="text-body-1 font-weight-medium mb-3">
            {{ question.questionText }}
          </div>

          <!-- Answer Input based on type -->
          <v-text-field
            v-if="question.answerType === 'text'"
            v-model="localAnswers[question.fieldName]"
            label="Antwort"
            variant="outlined"
            density="comfortable"
            :placeholder="question.isAnswered ? question.userAnswer : 'Ihre Antwort...'"
          />

          <v-text-field
            v-else-if="question.answerType === 'number'"
            v-model="localAnswers[question.fieldName]"
            label="Antwort"
            type="number"
            variant="outlined"
            density="comfortable"
          />

          <v-text-field
            v-else-if="question.answerType === 'date'"
            v-model="localAnswers[question.fieldName]"
            label="Datum"
            type="date"
            variant="outlined"
            density="comfortable"
          />

          <v-text-field
            v-else-if="question.answerType === 'time'"
            v-model="localAnswers[question.fieldName]"
            label="Uhrzeit"
            type="time"
            variant="outlined"
            density="comfortable"
          />

          <v-text-field
            v-else-if="question.answerType === 'datetime'"
            v-model="localAnswers[question.fieldName]"
            label="Datum und Uhrzeit"
            type="datetime-local"
            variant="outlined"
            density="comfortable"
          />

          <v-select
            v-else-if="question.answerType === 'choice' && suggestedAnswers(question)"
            v-model="localAnswers[question.fieldName]"
            :items="suggestedAnswers(question)"
            label="Wählen Sie eine Antwort"
            variant="outlined"
            density="comfortable"
          />

          <v-text-field
            v-else
            v-model="localAnswers[question.fieldName]"
            label="Antwort"
            variant="outlined"
            density="comfortable"
          />

          <!-- Suggested Answers as Chips (if available and not choice) -->
          <div
            v-if="question.answerType !== 'choice' && suggestedAnswers(question).length > 0"
            class="mt-2"
          >
            <div class="text-caption text-grey mb-1">Vorschläge:</div>
            <v-chip-group>
              <v-chip
                v-for="(suggestion, index) in suggestedAnswers(question)"
                :key="index"
                size="small"
                @click="selectSuggestion(question.fieldName, suggestion)"
                variant="outlined"
                clickable
              >
                {{ suggestion }}
              </v-chip>
            </v-chip-group>
          </div>
        </v-card-text>
      </v-card>
    </v-list-item>
  </v-list>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import type { AIQuestion } from '@/types/validation'

const props = defineProps<{
  questions: AIQuestion[]
  answers: Record<string, string>
}>()

const emit = defineEmits<{
  'update:answers': [value: Record<string, string>]
}>()

const localAnswers = ref<Record<string, string>>({ ...props.answers })

// Sort questions by priority
const sortedQuestions = computed(() => {
  const priorityOrder = { critical: 0, high: 1, medium: 2, low: 3 }
  return [...props.questions].sort((a, b) => {
    return priorityOrder[a.priority as keyof typeof priorityOrder] - priorityOrder[b.priority as keyof typeof priorityOrder]
  })
})

function suggestedAnswers(question: AIQuestion): string[] {
  if (!question.suggestedAnswers) return []
  try {
    return JSON.parse(question.suggestedAnswers)
  } catch (e) {
    return []
  }
}

function selectSuggestion(fieldName: string, suggestion: string) {
  localAnswers.value[fieldName] = suggestion
}

function getPriorityColor(priority: string): string {
  const colors: Record<string, string> = {
    critical: 'error',
    high: 'warning',
    medium: 'info',
    low: 'success'
  }
  return colors[priority] || 'grey'
}

function getQuestionBorderColor(question: AIQuestion): string {
  if (localAnswers.value[question.fieldName] || question.isAnswered) {
    return 'success'
  }
  if (question.priority === 'critical') {
    return 'error'
  }
  return ''
}

// Sync local answers with parent
watch(localAnswers, (newAnswers) => {
  emit('update:answers', newAnswers)
}, { deep: true })

// Update local answers when props change
watch(() => props.answers, (newAnswers) => {
  localAnswers.value = { ...newAnswers }
}, { deep: true })
</script>

<style scoped>
.question-item {
  list-style: none;
}
</style>
