<template>
  <v-card-text>
    <div v-if="loading" class="text-center py-8">
      <v-progress-circular indeterminate color="primary" />
      <p class="mt-4 text-body-1">Generiere interaktive Übung...</p>
    </div>

    <div v-else-if="!currentExercise" class="text-center py-6">
      <v-icon size="64" color="primary" class="mb-4">mdi-star-shooting</v-icon>
      <h3 class="text-h6 mb-2">Interaktive Übungen</h3>
      <p class="text-body-2 text-medium-emphasis mb-6">
        Lerne Schritt für Schritt mit interaktiven Aufgaben im Brilliant-Stil.
        Perfekt für neue Konzepte und spielerisches Lernen!
      </p>

      <v-card variant="outlined" class="mb-4 pa-4 text-left" max-width="500" style="margin: 0 auto;">
        <v-text-field
          :model-value="subject"
          @update:model-value="$emit('update:subject', $event)"
          label="Fach"
          placeholder="z.B. Informatik, Mathe..."
          variant="outlined"
          density="compact"
          class="mb-3"
        />
        <v-text-field
          :model-value="topic"
          @update:model-value="$emit('update:topic', $event)"
          label="Thema"
          placeholder="z.B. Binäre Suche, Integrale..."
          variant="outlined"
          density="compact"
          class="mb-3"
        />
        <v-select
          :model-value="difficulty"
          @update:model-value="$emit('update:difficulty', $event)"
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
        @click="$emit('generate')"
        :disabled="!subject || !topic"
      >
        <v-icon start>mdi-auto-fix</v-icon>
        Interaktive Übung generieren
      </v-btn>
    </div>

    <div v-else>
      <InteractiveExercisePlayer
        :exercise="currentExercise"
        @complete="$emit('complete', $event)"
        @close="$emit('close')"
      />
    </div>
  </v-card-text>
</template>

<script setup lang="ts">
import type { InteractiveExerciseData } from '@/types/learning'
import { difficultyItems } from '@/types/learning'
import { InteractiveExercisePlayer } from '@/components/exercises'

defineProps<{
  currentExercise: InteractiveExerciseData | null
  loading: boolean
  subject: string
  topic: string
  difficulty: string
}>()

defineEmits<{
  'update:subject': [value: string]
  'update:topic': [value: string]
  'update:difficulty': [value: string]
  'generate': []
  'complete': [result: { score: number; stepResults: any[] }]
  'close': []
}>()
</script>
