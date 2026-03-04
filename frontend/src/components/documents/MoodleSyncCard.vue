<script setup lang="ts">
import { moodleApi, type MoodleSyncStatus } from '@/api/moodle'
import { ref, computed, onUnmounted } from 'vue'

const emit = defineEmits<{ synced: [] }>()

const status = ref<MoodleSyncStatus | null>(null)
const loading = ref(false)
const error = ref('')
let pollTimer: ReturnType<typeof setInterval> | null = null

const isRunning = computed(() => {
  const s = status.value?.status
  return s === 'syncing_metadata' || s === 'downloading' || s === 'processing'
})

const isDone = computed(() => status.value?.status === 'done')
const isError = computed(() => status.value?.status === 'error')

const progressPercent = computed(() => {
  if (!status.value || !status.value.total_to_process) return 0
  return Math.round(
    ((status.value.downloaded + status.value.processed) /
      (status.value.total_to_process * 2)) * 100,
  )
})

const statusText = computed(() => {
  if (!status.value) return ''
  const s = status.value
  switch (s.status) {
    case 'syncing_metadata': return 'Metadaten synchronisieren...'
    case 'downloading': return s.current_file
      ? `Herunterladen: ${s.current_file}`
      : 'Dateien herunterladen...'
    case 'processing': return s.current_file
      ? `Verarbeiten: ${s.current_file}`
      : 'Dateien verarbeiten...'
    case 'done': return `Fertig: ${s.processed} verarbeitet, ${s.new_resources} neu, ${s.changed_resources} geändert`
    case 'error': return 'Sync fehlgeschlagen'
    default: return ''
  }
})

async function startSync() {
  error.value = ''
  loading.value = true
  try {
    const res = await moodleApi.autoSync()
    status.value = res.data.data
    startPolling()
  } catch (e) {
    error.value = 'Sync konnte nicht gestartet werden'
  } finally {
    loading.value = false
  }
}

function startPolling() {
  stopPolling()
  pollTimer = setInterval(async () => {
    try {
      const res = await moodleApi.syncStatus()
      status.value = res.data.data
      if (!isRunning.value) {
        stopPolling()
        if (isDone.value) emit('synced')
      }
    } catch {
      stopPolling()
    }
  }, 2000)
}

function stopPolling() {
  if (pollTimer) {
    clearInterval(pollTimer)
    pollTimer = null
  }
}

onUnmounted(stopPolling)
</script>

<template>
  <v-card elevation="1" rounded="lg" class="mb-6">
    <v-card-text class="d-flex align-center ga-4">
      <v-icon color="primary" size="28">mdi-school</v-icon>

      <div class="flex-grow-1">
        <div class="d-flex align-center ga-2 mb-1">
          <span class="text-subtitle-2 font-weight-medium">Moodle Sync</span>
          <v-chip v-if="isRunning" size="x-small" color="info" variant="tonal">
            Läuft
          </v-chip>
          <v-chip v-else-if="isDone" size="x-small" color="success" variant="tonal">
            Fertig
          </v-chip>
          <v-chip v-else-if="isError" size="x-small" color="error" variant="tonal">
            Fehler
          </v-chip>
        </div>

        <v-progress-linear
          v-if="isRunning"
          :model-value="progressPercent"
          :indeterminate="status?.status === 'syncing_metadata'"
          color="primary"
          rounded
          height="6"
          class="mb-1"
        />

        <div v-if="statusText" class="text-body-2 text-medium-emphasis">
          {{ statusText }}
        </div>

        <div v-if="isError && status?.errors?.length" class="mt-1">
          <div
            v-for="(err, i) in status.errors.slice(0, 3)"
            :key="i"
            class="text-caption text-error"
          >
            {{ err }}
          </div>
        </div>

        <div v-if="error" class="text-caption text-error mt-1">{{ error }}</div>
      </div>

      <v-btn
        color="primary"
        variant="tonal"
        :loading="loading || isRunning"
        :disabled="isRunning"
        prepend-icon="mdi-sync"
        @click="startSync"
      >
        Synchronisieren
      </v-btn>
    </v-card-text>
  </v-card>
</template>
