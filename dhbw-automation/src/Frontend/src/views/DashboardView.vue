<template>
  <v-container>
    <div class="d-flex justify-space-between align-center mb-6">
      <h1 class="text-h3">Dashboard</h1>
      <div>
        <span class="mr-3">Willkommen, {{ authStore.user?.firstName }}!</span>
        <v-btn color="error" variant="outlined" @click="handleLogout">
          <v-icon left>mdi-logout</v-icon>
          Abmelden
        </v-btn>
      </div>
    </div>
    
    <v-row>
      <v-col cols="12" md="4">
        <v-card>
          <v-card-title>Dokumente</v-card-title>
          <v-card-text>
            <div class="text-h2">{{ stats.documents }}</div>
            <p>Hochgeladene Dateien</p>
          </v-card-text>
        </v-card>
      </v-col>
      
      <v-col cols="12" md="4">
        <v-card>
          <v-card-title>Termine</v-card-title>
          <v-card-text>
            <div class="text-h2">{{ stats.events }}</div>
            <p>Anstehende Events</p>
          </v-card-text>
        </v-card>
      </v-col>
      
      <v-col cols="12" md="4">
        <v-card>
          <v-card-title>Rapla Sync</v-card-title>
          <v-card-text>
            <v-btn 
              color="primary" 
              @click="syncRapla" 
              :loading="syncing"
              block
            >
              <v-icon left>mdi-sync</v-icon>
              Kalender synchronisieren
            </v-btn>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
    
    <v-row class="mt-4">
      <v-col cols="12">
        <v-card>
          <v-card-title>Schnellaktionen</v-card-title>
          <v-card-text>
            <v-btn color="primary" class="mr-2" to="/files">
              <v-icon left>mdi-file-upload</v-icon>
              Datei hochladen
            </v-btn>
            <v-btn color="secondary" @click="testRapla" :loading="testing">
              <v-icon left>mdi-calendar</v-icon>
              Rapla-Verbindung testen
            </v-btn>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
    
    <v-snackbar v-model="snackbar.show" :color="snackbar.color" :timeout="3000">
      {{ snackbar.message }}
    </v-snackbar>
  </v-container>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import api from '@/services/api'

const router = useRouter()
const authStore = useAuthStore()

const stats = ref({
  documents: 0,
  events: 0
})

const syncing = ref(false)
const testing = ref(false)

const snackbar = ref({
  show: false,
  message: '',
  color: 'success'
})

const showMessage = (message: string, color: string = 'success') => {
  snackbar.value = { show: true, message, color }
}

const handleLogout = () => {
  authStore.logout()
  router.push('/login')
}

const syncRapla = async () => {
  if (!authStore.user?.id) {
    showMessage('Benutzer nicht angemeldet', 'error')
    return
  }
  
  syncing.value = true
  try {
    const response = await api.syncRaplaCalendar(authStore.user.id)
    if (response.success) {
      showMessage('Rapla-Kalender erfolgreich synchronisiert!')
      await loadStats()
    } else {
      showMessage(response.message || 'Synchronisierung fehlgeschlagen', 'error')
    }
  } catch (error: any) {
    console.error('Sync error:', error)
    showMessage(error.response?.data?.message || 'Verbindung fehlgeschlagen', 'error')
  } finally {
    syncing.value = false
  }
}

const testRapla = async () => {
  testing.value = true
  try {
    const response = await api.testRaplaConnection()
    if (response.success) {
      showMessage(`Rapla-Verbindung erfolgreich (${response.dataLength} Bytes)`)
    } else {
      showMessage('Rapla-Verbindung fehlgeschlagen', 'error')
    }
  } catch (error: any) {
    console.error('Test error:', error)
    showMessage(error.response?.data?.message || 'Verbindung fehlgeschlagen', 'error')
  } finally {
    testing.value = false
  }
}

const loadStats = async () => {
  if (!authStore.user?.id) return
  
  try {
    // Lade Kalenderevents
    const eventsResponse = await api.getUserEvents(authStore.user.id)
    if (eventsResponse.success) {
      stats.value.events = eventsResponse.count
    }
  } catch (error) {
    console.error('Error loading stats:', error)
  }
}

onMounted(() => {
  loadStats()
})
</script>
