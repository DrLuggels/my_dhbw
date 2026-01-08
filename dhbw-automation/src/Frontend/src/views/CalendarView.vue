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
              <v-btn
                color="primary"
                @click="syncRapla"
                :loading="syncing"
              >
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
              <!-- Wochenansicht -->
              <v-window-item value="week">
                <div class="mb-4 d-flex justify-space-between align-center">
                  <v-btn icon @click="previousWeek">
                    <v-icon>mdi-chevron-left</v-icon>
                  </v-btn>
                  <h3>{{ weekTitle }}</h3>
                  <v-btn icon @click="nextWeek">
                    <v-icon>mdi-chevron-right</v-icon>
                  </v-btn>
                </div>

                <div class="week-view">
                  <div class="week-grid">
                    <div class="time-column">
                      <div class="time-header"></div>
                      <div v-for="hour in hours" :key="hour" class="time-slot">
                        {{ hour }}:00
                      </div>
                    </div>

                    <div v-for="day in weekDays" :key="day.date" class="day-column">
                      <div class="day-header" :class="{ 'today': isToday(day.date) }">
                        <div class="day-name">{{ day.name }}</div>
                        <div class="day-date">{{ formatDate(day.date) }}</div>
                      </div>
                      <div class="day-events">
                        <div v-for="hour in hours" :key="hour" class="hour-slot"></div>
                        <div
                          v-for="event in getDayEvents(day.date)"
                          :key="event.id"
                          class="event-card"
                          :style="getEventStyle(event)"
                          :class="'event-' + event.source"
                          @click="openEventDetails(event)"
                        >
                          <div class="event-time">
                            {{ formatTime(event.startTime) }} - {{ formatTime(event.endTime) }}
                          </div>
                          <div class="event-title">{{ event.title }}</div>
                          <div v-if="event.location" class="event-location">
                            <v-icon size="x-small">mdi-map-marker</v-icon>
                            {{ event.location }}
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </v-window-item>

              <!-- Listenansicht -->
              <v-window-item value="list">
                <v-data-table
                  :headers="headers"
                  :items="events"
                  :loading="loading"
                  :items-per-page="25"
                  class="elevation-1"
                  hover
                  @click:row="(_, { item }) => openEventDetails(item)"
                >
                  <template v-slot:item.startTime="{ item }">
                    {{ formatDateTime(item.startTime) }}
                  </template>

                  <template v-slot:item.endTime="{ item }">
                    {{ formatDateTime(item.endTime) }}
                  </template>

                  <template v-slot:item.source="{ item }">
                    <v-chip :color="getSourceColor(item.source)" size="small">
                      {{ item.source }}
                    </v-chip>
                  </template>

                  <template v-slot:item.eventType="{ item }">
                    <v-chip v-if="item.eventType" color="info" size="small">
                      {{ item.eventType }}
                    </v-chip>
                  </template>
                </v-data-table>
              </v-window-item>
            </v-window>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <v-snackbar v-model="snackbar.show" :color="snackbar.color" :timeout="3000">
      {{ snackbar.message }}
    </v-snackbar>

    <!-- Event Details Modal -->
    <v-dialog v-model="eventDialog" max-width="600px">
      <v-card v-if="selectedEvent">
        <v-card-title class="d-flex justify-space-between align-center">
          <span>Event Details</span>
          <v-chip :color="getSourceColor(selectedEvent.source)" size="small">
            {{ selectedEvent.source }}
          </v-chip>
        </v-card-title>

        <v-card-text>
          <v-list>
            <v-list-item>
              <template v-slot:prepend>
                <v-icon color="primary">mdi-text</v-icon>
              </template>
              <v-list-item-title>Titel</v-list-item-title>
              <v-list-item-subtitle>{{ selectedEvent.title }}</v-list-item-subtitle>
            </v-list-item>

            <v-list-item v-if="selectedEvent.subject">
              <template v-slot:prepend>
                <v-icon color="primary">mdi-book-open-variant</v-icon>
              </template>
              <v-list-item-title>Fach</v-list-item-title>
              <v-list-item-subtitle>{{ selectedEvent.subject }}</v-list-item-subtitle>
            </v-list-item>

            <v-list-item v-if="selectedEvent.eventType">
              <template v-slot:prepend>
                <v-icon color="primary">mdi-tag</v-icon>
              </template>
              <v-list-item-title>Kurstyp</v-list-item-title>
              <v-list-item-subtitle>{{ selectedEvent.eventType }}</v-list-item-subtitle>
            </v-list-item>

            <v-divider class="my-3"></v-divider>

            <v-list-item>
              <template v-slot:prepend>
                <v-icon color="success">mdi-clock-start</v-icon>
              </template>
              <v-list-item-title>Start</v-list-item-title>
              <v-list-item-subtitle>{{ formatDateTime(selectedEvent.startTime) }}</v-list-item-subtitle>
            </v-list-item>

            <v-list-item>
              <template v-slot:prepend>
                <v-icon color="error">mdi-clock-end</v-icon>
              </template>
              <v-list-item-title>Ende</v-list-item-title>
              <v-list-item-subtitle>{{ formatDateTime(selectedEvent.endTime) }}</v-list-item-subtitle>
            </v-list-item>

            <v-list-item>
              <template v-slot:prepend>
                <v-icon color="primary">mdi-timer</v-icon>
              </template>
              <v-list-item-title>Dauer</v-list-item-title>
              <v-list-item-subtitle>{{ getEventDuration(selectedEvent) }}</v-list-item-subtitle>
            </v-list-item>

            <v-divider class="my-3"></v-divider>

            <v-list-item v-if="selectedEvent.location">
              <template v-slot:prepend>
                <v-icon color="primary">mdi-map-marker</v-icon>
              </template>
              <v-list-item-title>Ort</v-list-item-title>
              <v-list-item-subtitle>{{ selectedEvent.location }}</v-list-item-subtitle>
            </v-list-item>

            <v-list-item v-if="selectedEvent.description">
              <template v-slot:prepend>
                <v-icon color="primary">mdi-text-box</v-icon>
              </template>
              <v-list-item-title>Beschreibung</v-list-item-title>
              <v-list-item-subtitle>{{ selectedEvent.description }}</v-list-item-subtitle>
            </v-list-item>

            <v-list-item v-if="selectedEvent.professor">
              <template v-slot:prepend>
                <v-icon color="primary">mdi-account-tie</v-icon>
              </template>
              <v-list-item-title>Dozent</v-list-item-title>
              <v-list-item-subtitle>{{ selectedEvent.professor }}</v-list-item-subtitle>
            </v-list-item>

            <v-divider class="my-3"></v-divider>

            <v-list-item>
              <template v-slot:prepend>
                <v-icon color="primary">mdi-note-text</v-icon>
              </template>
              <v-list-item-title class="d-flex justify-space-between align-center">
                <span>Notizen</span>
                <v-chip size="small" color="info" variant="outlined">
                  <v-icon start size="x-small">mdi-robot</v-icon>
                  KI-bereit
                </v-chip>
              </v-list-item-title>
              <v-list-item-subtitle v-if="!editingNotes && selectedEvent.notes" class="mt-2">
                {{ selectedEvent.notes }}
              </v-list-item-subtitle>
              <v-list-item-subtitle v-if="!editingNotes && !selectedEvent.notes" class="mt-2 text-grey">
                Keine Notizen vorhanden. Diese können später automatisch von der KI ausgefüllt werden.
              </v-list-item-subtitle>

              <v-textarea
                v-if="editingNotes"
                v-model="editedNotes"
                label="Notizen bearbeiten"
                rows="4"
                variant="outlined"
                class="mt-2"
                placeholder="Hier können später automatisch KI-generierte Zusammenfassungen erscheinen..."
              ></v-textarea>
            </v-list-item>
          </v-list>
        </v-card-text>

        <v-card-actions>
          <v-btn
            v-if="!editingNotes"
            color="secondary"
            variant="text"
            @click="startEditingNotes"
          >
            <v-icon left>mdi-pencil</v-icon>
            Notizen bearbeiten
          </v-btn>
          <v-btn
            v-if="editingNotes"
            color="success"
            variant="text"
            @click="saveNotes"
            :loading="savingNotes"
          >
            <v-icon left>mdi-content-save</v-icon>
            Speichern
          </v-btn>
          <v-btn
            v-if="editingNotes"
            color="error"
            variant="text"
            @click="cancelEditingNotes"
          >
            Abbrechen
          </v-btn>
          <v-spacer></v-spacer>
          <v-btn color="primary" variant="text" @click="closeDialog">
            Schließen
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-container>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useAuthStore } from '@/stores/auth'
import api from '@/services/api'

const authStore = useAuthStore()

interface CalendarEvent {
  id: number
  title: string
  startTime: string
  endTime: string
  location: string
  subject: string
  eventType: string | null
  source: string
  description?: string
  professor?: string
  notes?: string
}

const events = ref<CalendarEvent[]>([])
const loading = ref(false)
const syncing = ref(false)
const filterSource = ref('all')
const activeTab = ref('week')
const currentWeekStart = ref(getMonday(new Date()))
const eventDialog = ref(false)
const selectedEvent = ref<CalendarEvent | null>(null)
const editingNotes = ref(false)
const editedNotes = ref('')
const savingNotes = ref(false)

const sourceOptions = [
  { title: 'Alle Quellen', value: 'all' },
  { title: 'Rapla', value: 'rapla' },
  { title: 'Moodle', value: 'moodle' },
  { title: 'Manuell', value: 'manual' }
]

const headers = [
  { title: 'Titel', key: 'title', sortable: true },
  { title: 'Fach', key: 'subject', sortable: true },
  { title: 'Kurstyp', key: 'eventType', sortable: true },
  { title: 'Start', key: 'startTime', sortable: true },
  { title: 'Ende', key: 'endTime', sortable: true },
  { title: 'Ort', key: 'location', sortable: true },
  { title: 'Quelle', key: 'source', sortable: true }
]

const hours = Array.from({ length: 15 }, (_, i) => i + 7) // 7:00 - 21:00

const snackbar = ref({
  show: false,
  message: '',
  color: 'success'
})

// Wochenansicht - Berechnete Werte
const weekDays = computed(() => {
  const days = []
  const dayNames = ['Montag', 'Dienstag', 'Mittwoch', 'Donnerstag', 'Freitag', 'Samstag', 'Sonntag']

  for (let i = 0; i < 7; i++) {
    const date = new Date(currentWeekStart.value)
    date.setDate(date.getDate() + i)
    days.push({
      date: date.toISOString().split('T')[0],
      name: dayNames[i]
    })
  }

  return days
})

const weekTitle = computed(() => {
  const start = new Date(currentWeekStart.value)
  const end = new Date(currentWeekStart.value)
  end.setDate(end.getDate() + 6)

  return `${formatDateLong(start)} - ${formatDateLong(end)}`
})

// Hilfsfunktionen
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

function isToday(dateStr: string): boolean {
  const today = new Date().toISOString().split('T')[0]
  return dateStr === today
}

function getDayEvents(dateStr: string): CalendarEvent[] {
  return events.value.filter(event => {
    const eventDate = new Date(event.startTime).toISOString().split('T')[0]
    return eventDate === dateStr
  })
}

function getEventStyle(event: CalendarEvent) {
  const start = new Date(event.startTime)
  const end = new Date(event.endTime)

  const startHour = start.getHours()
  const startMinute = start.getMinutes()
  const endHour = end.getHours()
  const endMinute = end.getMinutes()

  const topOffset = ((startHour - 7) * 60 + startMinute) / 60 * 60 // 60px pro Stunde
  const duration = ((endHour - startHour) * 60 + (endMinute - startMinute)) / 60 * 60

  return {
    top: `${topOffset + 40}px`, // 40px für Header
    height: `${Math.max(duration, 30)}px`,
    left: '4px',
    right: '4px'
  }
}

const showMessage = (message: string, color: string = 'success') => {
  snackbar.value = { show: true, message, color }
}

const formatDateTime = (dateString: string) => {
  const date = new Date(dateString)
  return date.toLocaleString('de-DE', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

const formatTime = (dateString: string) => {
  const date = new Date(dateString)
  return date.toLocaleTimeString('de-DE', {
    hour: '2-digit',
    minute: '2-digit'
  })
}

const formatDate = (dateString: string) => {
  const date = new Date(dateString + 'T00:00:00')
  return date.toLocaleDateString('de-DE', {
    day: '2-digit',
    month: '2-digit'
  })
}

const formatDateLong = (date: Date) => {
  return date.toLocaleDateString('de-DE', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric'
  })
}

const getSourceColor = (source: string) => {
  switch (source) {
    case 'rapla': return 'primary'
    case 'moodle': return 'secondary'
    case 'manual': return 'info'
    default: return 'grey'
  }
}

const openEventDetails = (event: CalendarEvent) => {
  selectedEvent.value = event
  editingNotes.value = false
  editedNotes.value = event.notes || ''
  eventDialog.value = true
}

const closeDialog = () => {
  eventDialog.value = false
  editingNotes.value = false
  editedNotes.value = ''
}

const startEditingNotes = () => {
  editingNotes.value = true
  editedNotes.value = selectedEvent.value?.notes || ''
}

const cancelEditingNotes = () => {
  editingNotes.value = false
  editedNotes.value = selectedEvent.value?.notes || ''
}

const saveNotes = async () => {
  if (!selectedEvent.value) return

  savingNotes.value = true
  try {
    const response = await api.updateEventNotes(selectedEvent.value.id, editedNotes.value)

    if (response.success) {
      // Lokale Daten aktualisieren
      const eventIndex = events.value.findIndex(e => e.id === selectedEvent.value!.id)
      if (eventIndex !== -1) {
        events.value[eventIndex].notes = editedNotes.value
        selectedEvent.value.notes = editedNotes.value
      }

      showMessage('Notizen erfolgreich gespeichert')
      editingNotes.value = false
    } else {
      showMessage(response.message || 'Fehler beim Speichern', 'error')
    }
  } catch (error: any) {
    console.error('Error saving notes:', error)
    showMessage(error.response?.data?.message || 'Fehler beim Speichern der Notizen', 'error')
  } finally {
    savingNotes.value = false
  }
}

const getEventDuration = (event: CalendarEvent) => {
  const start = new Date(event.startTime)
  const end = new Date(event.endTime)
  const durationMs = end.getTime() - start.getTime()
  const hours = Math.floor(durationMs / (1000 * 60 * 60))
  const minutes = Math.floor((durationMs % (1000 * 60 * 60)) / (1000 * 60))

  if (hours > 0 && minutes > 0) {
    return `${hours}h ${minutes}min`
  } else if (hours > 0) {
    return `${hours}h`
  } else {
    return `${minutes}min`
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

<style scoped>
.week-view {
  overflow-x: auto;
  min-height: 600px;
}

.week-grid {
  display: flex;
  min-width: 1000px;
}

.time-column {
  width: 80px;
  flex-shrink: 0;
  border-right: 1px solid #e0e0e0;
}

.time-header {
  height: 40px;
  border-bottom: 2px solid #e0e0e0;
}

.time-slot {
  height: 60px;
  padding: 4px;
  font-size: 12px;
  color: #666;
  border-bottom: 1px solid #f0f0f0;
  text-align: right;
  padding-right: 8px;
}

.day-column {
  flex: 1;
  min-width: 120px;
  border-right: 1px solid #e0e0e0;
  position: relative;
}

.day-column:last-child {
  border-right: none;
}

.day-header {
  height: 40px;
  border-bottom: 2px solid #e0e0e0;
  padding: 4px 8px;
  text-align: center;
  background: #f5f5f5;
}

.day-header.today {
  background: #e3f2fd;
  font-weight: bold;
}

.day-name {
  font-size: 14px;
  font-weight: 500;
}

.day-date {
  font-size: 12px;
  color: #666;
}

.day-events {
  position: relative;
  height: 900px;
}

.hour-slot {
  height: 60px;
  border-bottom: 1px solid #f0f0f0;
}

.event-card {
  position: absolute;
  background: #1976d2;
  color: white;
  border-radius: 4px;
  padding: 4px 6px;
  font-size: 11px;
  overflow: hidden;
  cursor: pointer;
  border-left: 3px solid #1565c0;
  box-shadow: 0 1px 3px rgba(0,0,0,0.2);
}

.event-card:hover {
  box-shadow: 0 2px 6px rgba(0,0,0,0.3);
  z-index: 10;
}

.event-rapla {
  background: #1976d2;
  border-left-color: #1565c0;
}

.event-moodle {
  background: #7b1fa2;
  border-left-color: #6a1b9a;
}

.event-manual {
  background: #0288d1;
  border-left-color: #01579b;
}

.event-time {
  font-size: 10px;
  opacity: 0.9;
  margin-bottom: 2px;
}

.event-title {
  font-weight: 500;
  line-height: 1.2;
  margin-bottom: 2px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.event-location {
  font-size: 10px;
  opacity: 0.8;
  display: flex;
  align-items: center;
  gap: 2px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* Klickbare Tabellenzeilen */
:deep(.v-data-table tbody tr) {
  cursor: pointer;
}

:deep(.v-data-table tbody tr:hover) {
  background-color: rgba(0, 0, 0, 0.04);
}
</style>
