<script setup lang="ts">
import { useLearningStore } from '@/stores/learning'
import type { Exercise } from '@/types/learning'
import { ref } from 'vue'
import FillInBlank from './FillInBlank.vue'
import FreeText from './FreeText.vue'
import MultipleChoice from './MultipleChoice.vue'

const props = defineProps<{
  exercise: Exercise
}>()

const emit = defineEmits<{
  answered: []
  next: []
}>()

const learning = useLearningStore()
const answered = ref(false)
const result = ref<Exercise | null>(null)

async function onAnswer(answer: string) {
  const rating = 3 // Default "Good" - user can adjust
  result.value = await learning.submitAnswer(props.exercise.id, answer, rating)
  answered.value = true
}

function onNext() {
  answered.value = false
  result.value = null
  emit('next')
}
</script>

<template>
  <v-card elevation="1" rounded="lg" class="pa-6">
    <div class="d-flex align-center mb-4">
      <v-chip size="small" color="primary" class="mr-2">
        Bloom {{ exercise.bloom_level }}
      </v-chip>
      <v-chip size="small" class="mr-2">{{ exercise.difficulty }}</v-chip>
      <v-chip size="small" variant="outlined">{{ exercise.exercise_type }}</v-chip>
    </div>

    <template v-if="!answered">
      <MultipleChoice
        v-if="exercise.exercise_type === 'multiple_choice'"
        :question="exercise.question"
        :options="exercise.options_json?.options ?? []"
        @answer="onAnswer"
      />
      <FillInBlank
        v-else-if="exercise.exercise_type === 'fill_in_blank'"
        :question="exercise.question"
        @answer="onAnswer"
      />
      <FreeText
        v-else
        :question="exercise.question"
        @answer="onAnswer"
      />
    </template>

    <template v-else-if="result">
      <v-alert
        :type="result.is_correct ? 'success' : 'error'"
        variant="tonal"
        class="mb-4"
      >
        {{ result.is_correct ? 'Richtig!' : 'Leider falsch' }}
      </v-alert>

      <div v-if="result.correct_answer" class="mb-2">
        <strong>Korrekte Antwort:</strong> {{ result.correct_answer }}
      </div>
      <div v-if="result.explanation" class="text-body-2 text-medium-emphasis mb-4">
        {{ result.explanation }}
      </div>

      <v-btn color="primary" @click="onNext">Nächste Übung</v-btn>
    </template>
  </v-card>
</template>
