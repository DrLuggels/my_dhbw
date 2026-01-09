<template>
  <v-list v-if="data" density="compact">
    <v-list-item>
      <template v-slot:prepend>
        <v-icon color="primary">mdi-format-title</v-icon>
      </template>
      <v-list-item-title>Titel</v-list-item-title>
      <v-list-item-subtitle>{{ data.title || 'Nicht angegeben' }}</v-list-item-subtitle>
    </v-list-item>

    <v-list-item v-if="data.description">
      <template v-slot:prepend>
        <v-icon color="primary">mdi-text</v-icon>
      </template>
      <v-list-item-title>Beschreibung</v-list-item-title>
      <v-list-item-subtitle>{{ data.description }}</v-list-item-subtitle>
    </v-list-item>

    <v-list-item v-if="data.priority">
      <template v-slot:prepend>
        <v-icon :color="getPriorityColor(data.priority)">mdi-flag</v-icon>
      </template>
      <v-list-item-title>Priorität</v-list-item-title>
      <v-list-item-subtitle>
        <v-chip :color="getPriorityColor(data.priority)" size="small">
          {{ data.priority }}
        </v-chip>
      </v-list-item-subtitle>
    </v-list-item>

    <v-list-item v-if="data.category">
      <template v-slot:prepend>
        <v-icon color="primary">mdi-tag</v-icon>
      </template>
      <v-list-item-title>Kategorie</v-list-item-title>
      <v-list-item-subtitle>{{ data.category }}</v-list-item-subtitle>
    </v-list-item>

    <v-list-item v-if="data.suggestedDeadline">
      <template v-slot:prepend>
        <v-icon color="warning">mdi-calendar-clock</v-icon>
      </template>
      <v-list-item-title>Vorgeschlagene Deadline</v-list-item-title>
      <v-list-item-subtitle>{{ formatDate(data.suggestedDeadline) }}</v-list-item-subtitle>
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
import type { ParsedTodo } from '@/types/validation'

defineProps<{
  data: ParsedTodo | null
  editable?: boolean
}>()

function getPriorityColor(priority: string): string {
  const colors: Record<string, string> = {
    low: 'success',
    medium: 'info',
    high: 'warning',
    urgent: 'error'
  }
  return colors[priority] || 'grey'
}

function getConfidenceColor(score: number): string {
  if (score >= 90) return 'success'
  if (score >= 70) return 'warning'
  return 'error'
}

function formatDate(dateString: string): string {
  try {
    const date = new Date(dateString)
    return date.toLocaleDateString('de-DE', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    })
  } catch (e) {
    return dateString
  }
}
</script>
