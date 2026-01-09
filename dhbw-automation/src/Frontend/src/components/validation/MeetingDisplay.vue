<template>
  <v-list v-if="data" density="compact">
    <v-list-item>
      <template v-slot:prepend>
        <v-icon color="primary">mdi-account</v-icon>
      </template>
      <v-list-item-title>Person</v-list-item-title>
      <v-list-item-subtitle>{{ data.personName || 'Nicht angegeben' }}</v-list-item-subtitle>
    </v-list-item>

    <v-list-item v-if="data.purpose">
      <template v-slot:prepend>
        <v-icon color="primary">mdi-target</v-icon>
      </template>
      <v-list-item-title>Zweck</v-list-item-title>
      <v-list-item-subtitle>{{ data.purpose }}</v-list-item-subtitle>
    </v-list-item>

    <v-list-item v-if="data.suggestedDate">
      <template v-slot:prepend>
        <v-icon color="warning">mdi-calendar</v-icon>
      </template>
      <v-list-item-title>Vorgeschlagenes Datum</v-list-item-title>
      <v-list-item-subtitle>{{ formatDate(data.suggestedDate) }}</v-list-item-subtitle>
    </v-list-item>

    <v-list-item>
      <template v-slot:prepend>
        <v-icon color="primary">mdi-clock-outline</v-icon>
      </template>
      <v-list-item-title>Geschätzte Dauer</v-list-item-title>
      <v-list-item-subtitle>{{ data.estimatedDurationMinutes }} Minuten</v-list-item-subtitle>
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
import type { ParsedMeeting } from '@/types/validation'

defineProps<{
  data: ParsedMeeting | null
  editable?: boolean
}>()

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
