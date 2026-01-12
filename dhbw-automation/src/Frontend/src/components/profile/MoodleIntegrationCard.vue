<template>
  <v-card>
    <v-card-title>
      <v-icon left>mdi-school</v-icon>
      Moodle Integration
      <v-spacer></v-spacer>
      <v-chip
        v-if="status.isConnected"
        size="small"
        color="success"
        variant="outlined"
      >
        <v-icon start size="x-small">mdi-check-circle</v-icon>
        Verbunden
      </v-chip>
      <v-chip
        v-else
        size="small"
        color="warning"
        variant="outlined"
      >
        <v-icon start size="x-small">mdi-alert-circle</v-icon>
        Nicht verbunden
      </v-chip>
    </v-card-title>

    <v-card-text>
      <!-- Status Info wenn verbunden -->
      <v-alert v-if="status.isConnected" type="success" variant="tonal" class="mb-4">
        <div class="d-flex align-center">
          <v-icon class="mr-2">mdi-account-check</v-icon>
          <div>
            <div class="font-weight-medium">{{ status.fullname || status.username }}</div>
            <div class="text-caption">
              Letzter Sync: {{ status.lastSync ? formatDate(status.lastSync) : 'Noch nie' }}
            </div>
          </div>
        </div>
      </v-alert>

      <!-- Login Form wenn nicht verbunden -->
      <v-form v-if="!status.isConnected" ref="loginFormRef" v-model="loginValid">
        <v-alert type="info" variant="tonal" class="mb-4">
          <v-icon left>mdi-information</v-icon>
          Verbinde dein DHBW Moodle-Konto, um Kurse, Aufgaben und Materialien automatisch zu synchronisieren.
        </v-alert>

        <v-text-field
          v-model="credentials.username"
          label="Moodle Benutzername"
          prepend-icon="mdi-account"
          :rules="[v => !!v || 'Benutzername erforderlich']"
          variant="outlined"
          class="mb-3"
          hint="Dein DHBW Moodle Benutzername"
          persistent-hint
        ></v-text-field>

        <v-text-field
          v-model="credentials.password"
          label="Moodle Passwort"
          prepend-icon="mdi-lock"
          :type="showPassword ? 'text' : 'password'"
          :append-inner-icon="showPassword ? 'mdi-eye-off' : 'mdi-eye'"
          @click:append-inner="showPassword = !showPassword"
          :rules="[v => !!v || 'Passwort erforderlich']"
          variant="outlined"
          class="mb-3"
        ></v-text-field>

        <v-btn
          color="primary"
          @click="handleLogin"
          :loading="loading"
          :disabled="!loginValid"
          block
        >
          <v-icon left>mdi-login</v-icon>
          Mit Moodle verbinden
        </v-btn>
      </v-form>

      <!-- Sync Controls wenn verbunden -->
      <div v-else>
        <!-- Sync Toggle -->
        <v-switch
          v-model="syncEnabled"
          label="Automatische Synchronisation"
          color="primary"
          @change="toggleSync"
          :loading="toggling"
          hide-details
          class="mb-4"
        ></v-switch>

        <v-divider class="mb-4"></v-divider>

        <!-- Statistiken -->
        <v-row dense class="mb-4">
          <v-col cols="6" sm="3">
            <div class="text-center">
              <div class="text-h5 font-weight-bold">{{ stats.courses }}</div>
              <div class="text-caption">Kurse</div>
            </div>
          </v-col>
          <v-col cols="6" sm="3">
            <div class="text-center">
              <div class="text-h5 font-weight-bold">{{ stats.assignments }}</div>
              <div class="text-caption">Aufgaben</div>
            </div>
          </v-col>
          <v-col cols="6" sm="3">
            <div class="text-center">
              <div class="text-h5 font-weight-bold">{{ stats.resources }}</div>
              <div class="text-caption">Materialien</div>
            </div>
          </v-col>
          <v-col cols="6" sm="3">
            <div class="text-center">
              <div class="text-h5 font-weight-bold">{{ stats.events }}</div>
              <div class="text-caption">Events</div>
            </div>
          </v-col>
        </v-row>

        <v-divider class="mb-4"></v-divider>

        <!-- Action Buttons -->
        <div class="d-flex gap-2 flex-wrap">
          <v-btn
            color="primary"
            @click="handleSync"
            :loading="syncing"
            variant="elevated"
          >
            <v-icon left>mdi-sync</v-icon>
            Jetzt synchronisieren
          </v-btn>

          <v-btn
            color="info"
            @click="handleTestConnection"
            :loading="testing"
            variant="outlined"
          >
            <v-icon left>mdi-connection</v-icon>
            Verbindung testen
          </v-btn>

          <v-btn
            color="error"
            @click="showDisconnectDialog = true"
            variant="text"
          >
            <v-icon left>mdi-link-off</v-icon>
            Trennen
          </v-btn>
        </div>
      </div>

      <!-- Error Message -->
      <v-alert v-if="error" type="error" variant="tonal" class="mt-4" closable @click:close="error = ''">
        {{ error }}
      </v-alert>

      <!-- Success Message -->
      <v-alert v-if="success" type="success" variant="tonal" class="mt-4" closable @click:close="success = ''">
        {{ success }}
      </v-alert>
    </v-card-text>

    <!-- Disconnect Dialog -->
    <v-dialog v-model="showDisconnectDialog" max-width="400">
      <v-card>
        <v-card-title>Moodle-Verbindung trennen?</v-card-title>
        <v-card-text>
          Bist du sicher, dass du die Moodle-Verbindung trennen möchtest?
          Deine synchronisierten Daten bleiben erhalten.
        </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn @click="showDisconnectDialog = false">Abbrechen</v-btn>
          <v-btn color="error" @click="handleDisconnect" :loading="disconnecting">
            Trennen
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-card>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import api from '@/services/api'

interface MoodleStatus {
  isConnected: boolean
  syncEnabled: boolean
  username?: string
  fullname?: string
  lastSync?: string
  lastError?: string
}

interface SyncStats {
  courses: number
  assignments: number
  resources: number
  events: number
}

const loginFormRef = ref()
const loginValid = ref(false)
const showPassword = ref(false)
const loading = ref(false)
const syncing = ref(false)
const testing = ref(false)
const toggling = ref(false)
const disconnecting = ref(false)
const showDisconnectDialog = ref(false)

const error = ref('')
const success = ref('')

const credentials = ref({
  username: '',
  password: ''
})

const status = ref<MoodleStatus>({
  isConnected: false,
  syncEnabled: false
})

const syncEnabled = computed({
  get: () => status.value.syncEnabled,
  set: (val) => { status.value.syncEnabled = val }
})

const stats = ref<SyncStats>({
  courses: 0,
  assignments: 0,
  resources: 0,
  events: 0
})

const formatDate = (dateStr: string) => {
  const date = new Date(dateStr)
  return date.toLocaleString('de-DE', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

const loadStatus = async () => {
  try {
    const response = await api.get('/moodle/status')
    if (response.data.success && response.data.data) {
      const data = response.data.data
      status.value = {
        isConnected: data.isConfigured,
        syncEnabled: data.isSyncEnabled,
        username: data.moodleUsername,
        fullname: data.moodleUsername,
        lastSync: data.lastSync,
        lastError: data.lastSyncError
      }

      // Stats direkt aus Status-Response laden
      stats.value = {
        courses: data.coursesCount || 0,
        assignments: data.assignmentsCount || 0,
        resources: data.resourcesCount || 0,
        events: data.calendarEventsCount || 0
      }
    }
  } catch (err) {
    console.error('Error loading Moodle status:', err)
  }
}

const loadStats = async () => {
  try {
    const [coursesRes, assignmentsRes, resourcesRes, eventsRes] = await Promise.all([
      api.get('/moodle/courses'),
      api.get('/moodle/assignments'),
      api.get('/moodle/resources'),
      api.get('/moodle/calendar')
    ])

    stats.value = {
      courses: coursesRes.data.data?.length || 0,
      assignments: assignmentsRes.data.data?.length || 0,
      resources: resourcesRes.data.data?.length || 0,
      events: eventsRes.data.data?.length || 0
    }
  } catch (err) {
    console.error('Error loading Moodle stats:', err)
  }
}

const handleLogin = async () => {
  if (!loginValid.value) return

  loading.value = true
  error.value = ''
  success.value = ''

  try {
    const response = await api.post('/moodle/login', credentials.value)

    if (response.data.success) {
      success.value = 'Erfolgreich mit Moodle verbunden!'
      credentials.value = { username: '', password: '' }
      await loadStatus()
    } else {
      error.value = response.data.message || 'Login fehlgeschlagen'
    }
  } catch (err: any) {
    error.value = err.response?.data?.message || 'Verbindungsfehler'
  } finally {
    loading.value = false
  }
}

const handleSync = async () => {
  syncing.value = true
  error.value = ''
  success.value = ''

  try {
    const response = await api.post('/moodle/sync')

    if (response.data.success) {
      const data = response.data.data
      success.value = `Sync erfolgreich! ${data.courses?.added || 0} Kurse, ${data.assignments?.added || 0} Aufgaben synchronisiert.`
      await loadStats()
      await loadStatus()
    } else {
      error.value = response.data.message || 'Sync fehlgeschlagen'
    }
  } catch (err: any) {
    error.value = err.response?.data?.message || 'Sync-Fehler'
  } finally {
    syncing.value = false
  }
}

const handleTestConnection = async () => {
  testing.value = true
  error.value = ''
  success.value = ''

  try {
    const response = await api.post('/moodle/test')

    if (response.data.success) {
      success.value = `Verbindung OK! Site: ${response.data.data?.siteName || 'DHBW Moodle'}`
    } else {
      error.value = response.data.message || 'Verbindungstest fehlgeschlagen'
    }
  } catch (err: any) {
    error.value = err.response?.data?.message || 'Verbindungsfehler'
  } finally {
    testing.value = false
  }
}

const toggleSync = async () => {
  toggling.value = true
  error.value = ''

  try {
    const endpoint = syncEnabled.value ? '/moodle/enable' : '/moodle/disable'
    const response = await api.post(endpoint)

    if (!response.data.success) {
      error.value = response.data.message || 'Fehler beim Ändern der Sync-Einstellung'
      syncEnabled.value = !syncEnabled.value
    }
  } catch (err: any) {
    error.value = err.response?.data?.message || 'Fehler'
    syncEnabled.value = !syncEnabled.value
  } finally {
    toggling.value = false
  }
}

const handleDisconnect = async () => {
  disconnecting.value = true

  try {
    await api.post('/moodle/disable')
    status.value = { isConnected: false, syncEnabled: false }
    stats.value = { courses: 0, assignments: 0, resources: 0, events: 0 }
    success.value = 'Moodle-Verbindung getrennt'
    showDisconnectDialog.value = false
  } catch (err: any) {
    error.value = err.response?.data?.message || 'Fehler beim Trennen'
  } finally {
    disconnecting.value = false
  }
}

onMounted(() => {
  loadStatus()
})
</script>

<style scoped>
.gap-2 {
  gap: 8px;
}
</style>
