<template>
  <v-list v-if="data" density="compact">
    <v-list-item>
      <template v-slot:prepend>
        <v-icon color="primary">mdi-format-title</v-icon>
      </template>
      <v-list-item-title>Name</v-list-item-title>
      <v-list-item-subtitle>{{ data.name || 'Nicht angegeben' }}</v-list-item-subtitle>
    </v-list-item>

    <v-list-item v-if="data.description">
      <template v-slot:prepend>
        <v-icon color="primary">mdi-text</v-icon>
      </template>
      <v-list-item-title>Beschreibung</v-list-item-title>
      <v-list-item-subtitle>{{ data.description }}</v-list-item-subtitle>
    </v-list-item>

    <v-list-item v-if="data.estimatedPriority">
      <template v-slot:prepend>
        <v-icon :color="getPriorityColor(data.estimatedPriority)">mdi-flag</v-icon>
      </template>
      <v-list-item-title>Geschätzte Priorität</v-list-item-title>
      <v-list-item-subtitle>
        <v-chip :color="getPriorityColor(data.estimatedPriority)" size="small">
          {{ data.estimatedPriority }}
        </v-chip>
      </v-list-item-subtitle>
    </v-list-item>

    <v-list-item>
      <template v-slot:prepend>
        <v-icon color="info">mdi-speedometer</v-icon>
      </template>
      <v-list-item-title>AI Confidence</v-list-item-title>
      <v-list-item-subtitle>
        <v-progress-linear
          :model-value="data.confidenceScore"
          :color="getConfidenceColor(data.confidenceScore)"
          height="20"
          class="mt-1"
        >
          <strong>{{ data.confidenceScore }}%</strong>
        </v-progress-linear>
      </v-list-item-subtitle>
    </v-list-item>
  </v-list>
  <v-alert v-else type="error" variant="tonal">
    Fehler beim Laden der Daten
  </v-alert>
</template>

<script setup lang="ts">
import type { ParsedProject } from '@/types/validation'

defineProps<{
  data: ParsedProject | null
  editable?: boolean
}>()

function getPriorityColor(priority: string): string {
  const colors: Record<string, string> = {
    low: 'success',
    medium: 'info',
    high: 'warning'
  }
  return colors[priority] || 'grey'
}

function getConfidenceColor(score: number): string {
  if (score >= 90) return 'success'
  if (score >= 70) return 'warning'
  return 'error'
}
</script>
