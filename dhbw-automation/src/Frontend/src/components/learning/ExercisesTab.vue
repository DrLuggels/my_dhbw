<template>
  <v-card-text>
    <div v-if="loading" class="text-center py-8">
      <v-progress-circular indeterminate color="primary" />
    </div>

    <v-alert v-else-if="exercises.length === 0" type="info" variant="tonal">
      Keine fälligen Übungen. Schau später wieder vorbei!
    </v-alert>

    <div v-else>
      <v-card
        v-for="exercise in exercises"
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
          <v-chip size="small" :color="getDifficultyColor(exercise.difficulty)">
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
            @click="$emit('submitAnswer', exercise)"
            :disabled="!exercise.userInput"
          >
            <v-icon left>mdi-check</v-icon>
            Antwort prüfen
          </v-btn>
        </v-card-actions>
      </v-card>
    </div>
  </v-card-text>
</template>

<script setup lang="ts">
import type { Exercise } from '@/types/learning'
import { getDifficultyColor } from '@/types/learning'

defineProps<{
  exercises: Exercise[]
  loading: boolean
}>()

defineEmits<{
  'submitAnswer': [exercise: Exercise]
}>()
</script>
