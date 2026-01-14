<template>
  <v-card-text>
    <div v-if="loading" class="text-center py-8">
      <v-progress-circular indeterminate color="primary" />
    </div>

    <v-alert v-else-if="deficits.length === 0" type="info" variant="tonal">
      Noch keine behobenen Defizite.
    </v-alert>

    <v-list v-else>
      <v-list-item v-for="deficit in deficits" :key="deficit.id">
        <template v-slot:prepend>
          <v-icon color="success">mdi-check-circle</v-icon>
        </template>

        <v-list-item-title>
          {{ deficit.subject }} - {{ deficit.topic }}
        </v-list-item-title>

        <v-list-item-subtitle>
          Behoben am: {{ formatLearningDate(deficit.resolvedAt!) }}
        </v-list-item-subtitle>
      </v-list-item>
    </v-list>
  </v-card-text>
</template>

<script setup lang="ts">
import type { LearningDeficit } from '@/types/learning'
import { formatLearningDate } from '@/types/learning'

defineProps<{
  deficits: LearningDeficit[]
  loading: boolean
}>()
</script>
