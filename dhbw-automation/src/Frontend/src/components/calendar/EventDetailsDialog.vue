<template>
  <v-dialog :model-value="modelValue" max-width="600px" @update:model-value="emit('update:modelValue', $event)">
    <v-card v-if="event">
      <v-card-title class="d-flex justify-space-between align-center">
        <span>Event Details</span>
        <div class="d-flex align-center gap-2">
          <v-chip :color="getSourceColor(event.source)" size="small">
            {{ event.source }}
          </v-chip>
          <v-btn icon size="small" variant="text" @click="emit('update:modelValue', false)">
            <v-icon>mdi-close</v-icon>
          </v-btn>
        </div>
      </v-card-title>

      <v-card-text>
        <v-list>
          <v-list-item>
            <template v-slot:prepend>
              <v-icon color="primary">mdi-text</v-icon>
            </template>
            <v-list-item-title>Titel</v-list-item-title>
            <v-list-item-subtitle>{{ event.title }}</v-list-item-subtitle>
          </v-list-item>

          <v-list-item v-if="event.subject">
            <template v-slot:prepend>
              <v-icon color="primary">mdi-book-open-variant</v-icon>
            </template>
            <v-list-item-title>Fach</v-list-item-title>
            <v-list-item-subtitle>{{ event.subject }}</v-list-item-subtitle>
          </v-list-item>

          <v-list-item v-if="event.eventType">
            <template v-slot:prepend>
              <v-icon color="primary">mdi-tag</v-icon>
            </template>
            <v-list-item-title>Kurstyp</v-list-item-title>
            <v-list-item-subtitle>{{ event.eventType }}</v-list-item-subtitle>
          </v-list-item>

          <v-divider class="my-3"></v-divider>

          <v-list-item>
            <template v-slot:prepend>
              <v-icon color="success">mdi-clock-start</v-icon>
            </template>
            <v-list-item-title>Start</v-list-item-title>
            <v-list-item-subtitle>{{ formatDateTime(event.startTime) }}</v-list-item-subtitle>
          </v-list-item>

          <v-list-item>
            <template v-slot:prepend>
              <v-icon color="error">mdi-clock-end</v-icon>
            </template>
            <v-list-item-title>Ende</v-list-item-title>
            <v-list-item-subtitle>{{ formatDateTime(event.endTime) }}</v-list-item-subtitle>
          </v-list-item>

          <v-list-item>
            <template v-slot:prepend>
              <v-icon color="primary">mdi-timer</v-icon>
            </template>
            <v-list-item-title>Dauer</v-list-item-title>
            <v-list-item-subtitle>{{ getEventDuration(event) }}</v-list-item-subtitle>
          </v-list-item>

          <v-divider class="my-3"></v-divider>

          <v-list-item v-if="event.location">
            <template v-slot:prepend>
              <v-icon color="primary">mdi-map-marker</v-icon>
            </template>
            <v-list-item-title>Ort</v-list-item-title>
            <v-list-item-subtitle>{{ event.location }}</v-list-item-subtitle>
          </v-list-item>

          <v-list-item v-if="event.description">
            <template v-slot:prepend>
              <v-icon color="primary">mdi-text-box</v-icon>
            </template>
            <v-list-item-title>Beschreibung</v-list-item-title>
            <v-list-item-subtitle>{{ event.description }}</v-list-item-subtitle>
          </v-list-item>

          <v-list-item v-if="event.professor">
            <template v-slot:prepend>
              <v-icon color="primary">mdi-account-tie</v-icon>
            </template>
            <v-list-item-title>Dozent</v-list-item-title>
            <v-list-item-subtitle>{{ event.professor }}</v-list-item-subtitle>
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
            <v-list-item-subtitle v-if="!editingNotes && event.notes" class="mt-2">
              {{ event.notes }}
            </v-list-item-subtitle>
            <v-list-item-subtitle v-if="!editingNotes && !event.notes" class="mt-2 text-grey">
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
        <v-btn color="primary" variant="text" @click="emit('update:modelValue', false)">
          Schließen
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import type { CalendarEvent } from '@/types/calendar'

interface Props {
  modelValue: boolean
  event: CalendarEvent | null
}

const props = defineProps<Props>()
const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  'save-notes': [eventId: number, notes: string]
}>()

const editingNotes = ref(false)
const editedNotes = ref('')
const savingNotes = ref(false)

watch(() => props.event, (newEvent) => {
  if (newEvent) {
    editingNotes.value = false
    editedNotes.value = newEvent.notes || ''
  }
})

const formatDateTime = (dateString: string) => {
  // Parse ISO string directly to avoid timezone conversion issues
  // The backend stores times in local timezone (Europe/Berlin) but without timezone info
  const match = dateString.match(/^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})/)
  if (match) {
    const [, year, month, day, hour, minute] = match
    return `${day}.${month}.${year}, ${hour}:${minute}`
  }
  // Fallback to Date parsing
  const date = new Date(dateString)
  return date.toLocaleString('de-DE', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
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

const startEditingNotes = () => {
  editingNotes.value = true
  editedNotes.value = props.event?.notes || ''
}

const cancelEditingNotes = () => {
  editingNotes.value = false
  editedNotes.value = props.event?.notes || ''
}

const saveNotes = async () => {
  if (!props.event) return
  
  savingNotes.value = true
  emit('save-notes', props.event.id, editedNotes.value)
  editingNotes.value = false
  savingNotes.value = false
}
</script>
