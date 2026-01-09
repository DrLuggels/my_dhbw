<template>
  <v-card>
    <v-card-title class="d-flex align-center">
      <v-icon left color="warning">mdi-school</v-icon>
      <span class="text-h6">Lerndefizite</span>
      <v-spacer />
      <v-chip
        v-if="deficits.length > 0"
        :color="getSeverityColor()"
        size="small"
      >
        {{ deficits.length }} erkannt
      </v-chip>
    </v-card-title>

    <v-card-text>
      <div v-if="loading" class="text-center py-4">
        <v-progress-circular indeterminate color="primary" />
      </div>

      <v-alert
        v-else-if="deficits.length === 0"
        type="success"
        variant="tonal"
        class="mb-0"
      >
        <v-icon left>mdi-check-circle</v-icon>
        Keine Defizite erkannt! Weiter so!
      </v-alert>

      <v-list v-else>
        <v-list-item
          v-for="deficit in sortedDeficits"
          :key="deficit.id"
          class="deficit-item"
        >
          <template v-slot:prepend>
            <v-avatar :color="getDeficitSeverityColor(deficit.severity)" size="40">
              <v-icon color="white">{{ getDeficitIcon(deficit.errorType) }}</v-icon>
            </v-avatar>
          </template>

          <v-list-item-title class="font-weight-medium">
            {{ deficit.subject }} - {{ deficit.topic }}
          </v-list-item-title>

          <v-list-item-subtitle>
            <v-chip size="x-small" class="mr-2" variant="text">
              {{ deficit.occurrenceCount }}x aufgetreten
            </v-chip>
            <span class="text-caption">{{ deficit.errorDescription }}</span>
          </v-list-item-subtitle>

          <template v-slot:append>
            <div class="d-flex flex-column align-end gap-2">
              <v-chip
                :color="getDeficitSeverityColor(deficit.severity)"
                size="small"
                variant="tonal"
              >
                {{ getSeverityLabel(deficit.severity) }}
              </v-chip>

              <v-btn
                v-if="deficit.needsTutoring"
                color="primary"
                size="small"
                variant="elevated"
                @click="scheduleTutoring(deficit)"
                :loading="schedulingId === deficit.id"
              >
                <v-icon left size="small">mdi-calendar-plus</v-icon>
                Nachhilfe planen
              </v-btn>
            </div>
          </template>
        </v-list-item>
      </v-list>
    </v-card-text>

    <v-card-actions v-if="deficits.length > 0">
      <v-btn
        variant="text"
        color="primary"
        to="/learning"
        block
      >
        <v-icon left>mdi-arrow-right</v-icon>
        Alle Defizite anzeigen
      </v-btn>
    </v-card-actions>

    <v-snackbar v-model="snackbar.show" :color="snackbar.color" :timeout="4000">
      {{ snackbar.message }}
    </v-snackbar>
  </v-card>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import api from '@/services/api'
import { useAuthStore } from '@/stores/auth'

interface LearningDeficit {
  id: number
  userId: number
  subject: string
  topic: string
  subtopic?: string
  errorType: string
  errorDescription: string
  occurrenceCount: number
  firstOccurrence: string
  lastOccurrence: string
  severity: string
  needsTutoring: boolean
  relatedDocumentIds: string
  createdAt: string
  resolvedAt?: string
}

const authStore = useAuthStore()
const deficits = ref<LearningDeficit[]>([])
const loading = ref(false)
const schedulingId = ref<number | null>(null)

const snackbar = ref({
  show: false,
  message: '',
  color: 'success'
})

const sortedDeficits = computed(() => {
  return [...deficits.value].sort((a, b) => {
    // Sort by severity first
    const severityOrder: Record<string, number> = {
      critical: 0,
      high: 1,
      medium: 2,
      low: 3
    }
    const aSev = severityOrder[a.severity] ?? 999
    const bSev = severityOrder[b.severity] ?? 999

    if (aSev !== bSev) return aSev - bSev

    // Then by occurrence count (descending)
    return b.occurrenceCount - a.occurrenceCount
  })
})

const getSeverityColor = () => {
  const highSeverity = deficits.value.some(d => d.severity === 'high' || d.severity === 'critical')
  if (highSeverity) return 'error'

  const mediumSeverity = deficits.value.some(d => d.severity === 'medium')
  if (mediumSeverity) return 'warning'

  return 'info'
}

const getDeficitSeverityColor = (severity: string) => {
  const colors: Record<string, string> = {
    critical: 'error',
    high: 'warning',
    medium: 'info',
    low: 'success'
  }
  return colors[severity] || 'default'
}

const getSeverityLabel = (severity: string) => {
  const labels: Record<string, string> = {
    critical: 'Kritisch',
    high: 'Hoch',
    medium: 'Mittel',
    low: 'Niedrig'
  }
  return labels[severity] || severity
}

const getDeficitIcon = (errorType: string) => {
  const icons: Record<string, string> = {
    concept: 'mdi-brain',
    calculation: 'mdi-calculator',
    application: 'mdi-application',
    general: 'mdi-alert-circle'
  }
  return icons[errorType] || 'mdi-alert'
}

const loadDeficits = async () => {
  if (!authStore.user?.id) return

  loading.value = true
  try {
    const response = await api.get(`/api/learning/deficits/${authStore.user.id}`)

    if (response.data.success) {
      deficits.value = response.data.data
    }
  } catch (error) {
    console.error('Error loading deficits:', error)
    showMessage('Fehler beim Laden der Lerndefizite', 'error')
  } finally {
    loading.value = false
  }
}

const scheduleTutoring = async (deficit: LearningDeficit) => {
  schedulingId.value = deficit.id

  try {
    const response = await api.post(
      `/api/learning/schedule-tutoring/${deficit.id}?userId=${authStore.user?.id}`
    )

    if (response.data.success) {
      showMessage(
        `${response.data.exercises} Übungen generiert und ${response.data.sessions} Lernzeiten eingeplant!`,
        'success'
      )

      // Reload deficits after scheduling
      await loadDeficits()
    }
  } catch (error: any) {
    console.error('Error scheduling tutoring:', error)
    showMessage(
      error.response?.data?.message || 'Fehler beim Planen der Nachhilfe',
      'error'
    )
  } finally {
    schedulingId.value = null
  }
}

const showMessage = (message: string, color: string = 'success') => {
  snackbar.value = { show: true, message, color }
}

onMounted(() => {
  loadDeficits()
})
</script>

<style scoped>
.deficit-item {
  transition: all 0.2s;
}

.deficit-item:hover {
  background-color: rgba(var(--v-theme-primary), 0.05);
}

.gap-2 {
  gap: 0.5rem;
}
</style>
