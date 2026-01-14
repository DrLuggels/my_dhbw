<template>
  <v-row>
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
            :model-value="completionRate"
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
            @click="$emit('practiceNow')"
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
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { LearningStats } from '@/types/learning'

const props = defineProps<{
  stats: LearningStats
}>()

defineEmits<{
  'practiceNow': []
}>()

const completionRate = computed(() => {
  if (props.stats.totalExercises === 0) return 0
  return (props.stats.completedExercises / props.stats.totalExercises) * 100
})
</script>
