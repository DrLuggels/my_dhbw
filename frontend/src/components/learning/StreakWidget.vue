<script setup lang="ts">
import type { Streak } from '@/types/learning'

defineProps<{
  streak: Streak
}>()
</script>

<template>
  <v-card elevation="1" rounded="lg" class="pa-4">
    <div class="d-flex align-center ga-6">
      <div class="text-center">
        <div class="text-h4 font-weight-bold text-primary">{{ streak.current_streak }}</div>
        <div class="text-caption text-medium-emphasis">Tage Streak</div>
      </div>

      <v-divider vertical />

      <div>
        <div class="text-body-2">
          Längster Streak: <strong>{{ streak.longest_streak }} Tage</strong>
        </div>
        <div class="text-body-2">
          Aktive Tage: <strong>{{ streak.total_active_days }}</strong>
        </div>
        <div class="text-body-2">
          Multiplikator: <strong>{{ streak.multiplier.toFixed(2) }}x</strong>
        </div>
      </div>
    </div>
  </v-card>
</template>
