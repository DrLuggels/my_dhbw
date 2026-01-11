<template>
  <v-container fluid>
    <div class="d-flex align-center mb-6">
      <v-btn icon variant="text" to="/dashboard" class="mr-3">
        <v-icon>mdi-arrow-left</v-icon>
      </v-btn>
      <h1 class="text-h3">Kalender</h1>
    </div>

    <v-row>
      <v-col cols="12">
        <v-card>
          <v-card-title class="d-flex justify-space-between align-center">
            <span>Meine Termine</span>
            <div class="d-flex align-center gap-2">
              <v-btn color="primary" @click="syncRapla" :loading="syncing">
                <v-icon left>mdi-sync</v-icon>
                Rapla synchronisieren
              </v-btn>
              <v-select
                v-model="filterSource"
                :items="sourceOptions"
                label="Quelle filtern"
                density="compact"
                style="width: 200px;"
                @update:modelValue="loadEvents"
              ></v-select>
            </div>
          </v-card-title>

          <v-tabs v-model="activeTab" bg-color="primary">
            <v-tab value="week">
              <v-icon left>mdi-calendar-week</v-icon>
              Wochenansicht
            </v-tab>
            <v-tab value="list">
              <v-icon left>mdi-format-list-bulleted</v-icon>
              Listenansicht
            </v-tab>
          </v-tabs>

          <v-card-text>
            <v-window v-model="activeTab">
              <v-window-item value="week">
                <WeekView
                  :events="events"
                  :current-week-start="currentWeekStart"
                  :week-title="weekTitle"
                  @previous-week="previousWeek"
                  @next-week="nextWeek"
                  @event-click="openEventDetails"
                />
              </v-window-item>

              <v-window-item value="list">
                <ListView
                  :events="events"
                  :loading="loading"
                  @event-click="openEventDetails"
                />
              </v-window-item>
            </v-window>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <v-snackbar v-model="snackbar.show" :color="snackbar.color" :timeout="3000">
      {{ snackbar.message }}
    </v-snackbar>

    <EventDetailsDialog
      v-model="eventDialog"
      :event="selectedEvent"
      @save-notes="handleSaveNotes"
    />
  </v-container>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useAuthStore } from '@/stores/auth'
import api from '@/services/api'
import WeekView from '@/components/calendar/WeekView.vue'
import ListView from '@/components/calendar/ListView.vue'
import EventDetailsDialog from '@/components/calendar/EventDetailsDialog.vue'
import type { CalendarEvent } from '@/types/calendar'

const authStore = useAuthStore()

const events = ref<CalendarEvent[]>([])
const loading = ref(false)
const syncing = ref(false)
const filterSource = ref('all')
const activeTab = ref('week')
const currentWeekStart = ref(getMonday(new Date()))
const eventDialog = ref(false)
const selectedEvent = ref<CalendarEvent | null>(null)

const sourceOptions = [
  { title: 'Alle Quellen', value: 'all' },
  { title: 'Rapla', value: 'rapla' },
  { title: 'Moodle', value: 'moodle' },
  { title: 'Manuell', value: 'manual' },
  { title: 'Lernstunden', value: 'learning' },
  { title: 'KI-generiert', value: 'ai_generated' }
]

const snackbar = ref({
  show: false,
  message: '',
  color: 'success'
})

const weekTitle = computed(() => {
  const start = new Date(currentWeekStart.value)
  const end = new Date(currentWeekStart.value)
  end.setDate(end.getDate() + 6)
  return `${formatDateLong(start)} - ${formatDateLong(end)}`
})

function getMonday(date: Date): Date {
  const d = new Date(date)
  const day = d.getDay()
  const diff = d.getDate() - day + (day === 0 ? -6 : 1)
  return new Date(d.setDate(diff))
}

function previousWeek() {
  const newDate = new Date(currentWeekStart.value)
  newDate.setDate(newDate.getDate() - 7)
  currentWeekStart.value = newDate
}

function nextWeek() {
  const newDate = new Date(currentWeekStart.value)
  newDate.setDate(newDate.getDate() + 7)
  currentWeekStart.value = newDate
}

const formatDateLong = (date: Date) => {
  return date.toLocaleDateString('de-DE', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric'
  })
}

const showMessage = (message: string, color: string = 'success') => {
  snackbar.value = { show: true, message, color }
}

const openEventDetails = (event: CalendarEvent) => {
  selectedEvent.value = event
  eventDialog.value = true
}

const handleSaveNotes = async (eventId: number, notes: string) => {
  try {
    const response = await api.updateEventNotes(eventId, notes)

    if (response.success) {
      const eventIndex = events.value.findIndex(e => e.id === eventId)
      if (eventIndex !== -1) {
        events.value[eventIndex].notes = notes
        if (selectedEvent.value) {
          selectedEvent.value.notes = notes
        }
      }
      showMessage('Notizen erfolgreich gespeichert')
    } else {
      showMessage(response.message || 'Fehler beim Speichern', 'error')
    }
  } catch (error: any) {
    console.error('Error saving notes:', error)
    showMessage(error.response?.data?.message || 'Fehler beim Speichern der Notizen', 'error')
  }
}

const loadEvents = async () => {
  if (!authStore.user?.id) return

  loading.value = true
  try {
    const source = filterSource.value === 'all' ? undefined : filterSource.value
    const response = await api.getUserEvents(authStore.user.id, undefined, undefined, source)

    if (response.success && Array.isArray(response.data)) {
      events.value = response.data
    } else {
      showMessage('Fehler beim Laden der Events', 'error')
    }
  } catch (error: any) {
    console.error('Error loading events:', error)
    showMessage(error.response?.data?.message || 'Verbindung fehlgeschlagen', 'error')
  } finally {
    loading.value = false
  }
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
      showMessage(`Rapla-Kalender erfolgreich synchronisiert! ${response.data.syncedEvents} Events`)
      await loadEvents()
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

onMounted(() => {
  loadEvents()
})
</script>

