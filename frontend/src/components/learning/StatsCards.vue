<script setup lang="ts">
import type { LearningStats } from '@/types/learning'

defineProps<{
  stats: LearningStats
}>()

function masteryColor(score: number): string {
  if (score < 0.4) return 'red'
  if (score < 0.7) return 'orange'
  return 'green'
}
</script>

<template>
  <v-row>
    <v-col cols="12" sm="6" md="3">
      <v-card elevation="1" rounded="lg" class="pa-4">
        <div class="text-subtitle-2 text-medium-emphasis">Entitäten</div>
        <div class="text-h5 font-weight-bold">{{ stats.total_entities }}</div>
        <div class="text-caption text-medium-emphasis">
          {{ stats.mastered_entities }} gemeistert
        </div>
      </v-card>
    </v-col>

    <v-col cols="12" sm="6" md="3">
      <v-card elevation="1" rounded="lg" class="pa-4">
        <div class="text-subtitle-2 text-medium-emphasis">Mastery</div>
        <div class="text-h5 font-weight-bold" :class="`text-${masteryColor(stats.average_mastery)}`">
          {{ (stats.average_mastery * 100).toFixed(0) }}%
        </div>
        <v-progress-linear
          :model-value="stats.average_mastery * 100"
          :color="masteryColor(stats.average_mastery)"
          class="mt-2"
          rounded
        />
      </v-card>
    </v-col>

    <v-col cols="12" sm="6" md="3">
      <v-card elevation="1" rounded="lg" class="pa-4">
        <div class="text-subtitle-2 text-medium-emphasis">Übungen</div>
        <div class="text-h5 font-weight-bold">{{ stats.answered_exercises }}</div>
        <div class="text-caption text-medium-emphasis">
          von {{ stats.total_exercises }} beantwortet
        </div>
      </v-card>
    </v-col>

    <v-col cols="12" sm="6" md="3">
      <v-card elevation="1" rounded="lg" class="pa-4">
        <div class="text-subtitle-2 text-medium-emphasis">Genauigkeit</div>
        <div class="text-h5 font-weight-bold">
          {{ (stats.accuracy * 100).toFixed(0) }}%
        </div>
        <div class="text-caption text-medium-emphasis">
          {{ stats.correct_exercises }} richtig
        </div>
      </v-card>
    </v-col>
  </v-row>
</template>
