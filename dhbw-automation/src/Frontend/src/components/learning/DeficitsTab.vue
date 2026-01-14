<template>
  <v-card-text>
    <div v-if="loading" class="text-center py-8">
      <v-progress-circular indeterminate color="primary" />
    </div>

    <v-alert v-else-if="deficits.length === 0" type="success" variant="tonal">
      Keine aktiven Lerndefizite! Weiter so!
    </v-alert>

    <v-expansion-panels v-else>
      <v-expansion-panel v-for="deficit in deficits" :key="deficit.id">
        <v-expansion-panel-title>
          <div class="d-flex align-center w-100">
            <v-chip :color="getDeficitSeverityColor(deficit.severity)" size="small" class="mr-3">
              {{ getSeverityLabel(deficit.severity) }}
            </v-chip>
            <strong>{{ deficit.subject }}</strong>
            <v-icon class="mx-2">mdi-chevron-right</v-icon>
            {{ deficit.topic }}
            <v-spacer />
            <v-chip size="small" variant="text">{{ deficit.occurrenceCount }}x</v-chip>
          </div>
        </v-expansion-panel-title>

        <v-expansion-panel-text>
          <v-divider class="mb-4" />
          <div class="mb-4">
            <v-chip size="small" class="mr-2">
              <v-icon left size="small">mdi-alert</v-icon>
              {{ deficit.errorType }}
            </v-chip>
            <v-chip size="small" class="mr-2">
              <v-icon left size="small">mdi-calendar</v-icon>
              Zuletzt: {{ formatLearningDate(deficit.lastOccurrence) }}
            </v-chip>
          </div>

          <p class="text-body-1 mb-4">
            <strong>Fehlerbeschreibung:</strong><br />
            {{ deficit.errorDescription }}
          </p>

          <v-alert v-if="deficit.needsTutoring" type="warning" variant="tonal" class="mb-4">
            <strong>Empfehlung:</strong> Dieses Defizit tritt häufig auf. Eine Nachhilfesession wird empfohlen.
          </v-alert>

          <div class="d-flex gap-2">
            <v-btn
              v-if="deficit.needsTutoring"
              color="primary"
              @click="$emit('scheduleTutoring', deficit.id)"
              :loading="schedulingId === deficit.id"
            >
              <v-icon left>mdi-calendar-plus</v-icon>
              Nachhilfe planen (5 Übungen + Lernzeit)
            </v-btn>
            <v-btn
              color="success"
              variant="outlined"
              @click="$emit('resolve', deficit.id)"
              :loading="resolvingId === deficit.id"
            >
              <v-icon left>mdi-check</v-icon>
              Als behoben markieren
            </v-btn>
          </div>
        </v-expansion-panel-text>
      </v-expansion-panel>
    </v-expansion-panels>
  </v-card-text>
</template>

<script setup lang="ts">
import type { LearningDeficit } from '@/types/learning'
import { getDeficitSeverityColor, getSeverityLabel, formatLearningDate } from '@/types/learning'

defineProps<{
  deficits: LearningDeficit[]
  loading: boolean
  schedulingId: number | null
  resolvingId: number | null
}>()

defineEmits<{
  'scheduleTutoring': [deficitId: number]
  'resolve': [deficitId: number]
}>()
</script>

<style scoped>
.gap-2 { gap: 0.5rem; }
</style>
