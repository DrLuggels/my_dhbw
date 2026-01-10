<template>
  <v-dialog
    :model-value="modelValue"
    @update:model-value="$emit('update:modelValue', $event)"
    max-width="800"
    scrollable
  >
    <v-card v-if="email">
      <v-card-title class="d-flex justify-space-between align-center bg-primary">
        <span class="text-white">
          <v-icon class="mr-2" color="white">mdi-email-open</v-icon>
          E-Mail-Aktion
        </span>
        <v-btn
          icon
          variant="text"
          @click="close"
        >
          <v-icon color="white">mdi-close</v-icon>
        </v-btn>
      </v-card-title>

      <v-card-text class="pt-4">
        <!-- Email Header -->
        <div class="mb-4">
          <div class="text-h6 mb-2">{{ email.subject }}</div>
          <div class="text-body-2 text-grey">
            <strong>Von:</strong> {{ email.fromName || email.fromAddress }}
          </div>
          <div class="text-body-2 text-grey">
            <strong>Empfangen:</strong> {{ formatDate(email.receivedAt) }}
          </div>
          <div v-if="email.category" class="mt-2">
            <v-chip :color="getCategoryColor(email.category)" size="small">
              {{ getCategoryLabel(email.category) }}
            </v-chip>
            <v-chip v-if="email.isAppointment" color="purple" size="small" class="ml-2">
              <v-icon start>mdi-calendar</v-icon>
              Termin
            </v-chip>
          </div>
        </div>

        <v-divider class="my-4" />

        <!-- AI Summary -->
        <v-alert v-if="email.summary" type="info" variant="tonal" class="mb-4">
          <div class="text-subtitle-2 mb-1">
            <v-icon class="mr-2">mdi-robot</v-icon>
            KI-Zusammenfassung
          </div>
          {{ email.summary }}
        </v-alert>

        <!-- Extracted Event Data (für Termine) -->
        <v-card v-if="email.isAppointment && extractedEventData" variant="outlined" class="mb-4">
          <v-card-title class="text-subtitle-1">
            <v-icon class="mr-2">mdi-calendar-check</v-icon>
            Termin-Details
          </v-card-title>
          <v-card-text>
            <v-list density="compact">
              <v-list-item v-if="extractedEventData.title">
                <v-list-item-title>Titel:</v-list-item-title>
                <v-list-item-subtitle>{{ extractedEventData.title }}</v-list-item-subtitle>
              </v-list-item>
              <v-list-item v-if="extractedEventData.startTime">
                <v-list-item-title>Start:</v-list-item-title>
                <v-list-item-subtitle>{{ formatDateTime(extractedEventData.startTime) }}</v-list-item-subtitle>
              </v-list-item>
              <v-list-item v-if="extractedEventData.endTime">
                <v-list-item-title>Ende:</v-list-item-title>
                <v-list-item-subtitle>{{ formatDateTime(extractedEventData.endTime) }}</v-list-item-subtitle>
              </v-list-item>
              <v-list-item v-if="extractedEventData.location">
                <v-list-item-title>Ort:</v-list-item-title>
                <v-list-item-subtitle>{{ extractedEventData.location }}</v-list-item-subtitle>
              </v-list-item>
            </v-list>
          </v-card-text>
        </v-card>

        <!-- Email Body -->
        <v-expansion-panels v-model="expandedPanels" class="mb-4">
          <v-expansion-panel>
            <v-expansion-panel-title>
              <v-icon class="mr-2">mdi-text</v-icon>
              E-Mail-Inhalt anzeigen
            </v-expansion-panel-title>
            <v-expansion-panel-text>
              <div v-if="email.bodyHtml" v-html="sanitizeHtml(email.bodyHtml)" class="email-body" />
              <div v-else class="email-body-text">{{ email.bodyText }}</div>
            </v-expansion-panel-text>
          </v-expansion-panel>
        </v-expansion-panels>

        <!-- Attachments -->
        <div v-if="email.hasAttachments && email.attachments.length > 0" class="mb-4">
          <div class="text-subtitle-2 mb-2">
            <v-icon class="mr-2">mdi-paperclip</v-icon>
            Anhänge ({{ email.attachments.length }})
          </div>
          <v-chip
            v-for="attachment in email.attachments"
            :key="attachment.id"
            class="mr-2 mb-2"
            :prepend-icon="getFileIcon(attachment.contentType)"
          >
            {{ attachment.fileName }} ({{ formatFileSize(attachment.fileSize) }})
          </v-chip>
        </div>

        <!-- Suggested Actions -->
        <div v-if="email.requiresUserAction">
          <div class="text-subtitle-2 mb-3">
            <v-icon class="mr-2">mdi-help-circle</v-icon>
            Was möchten Sie tun?
          </div>

          <v-row>
            <!-- Accept (für Termine) -->
            <v-col v-if="email.isAppointment" cols="12" sm="6">
              <v-btn
                block
                color="success"
                variant="elevated"
                size="large"
                @click="handleAction('accept')"
                :loading="loading"
              >
                <v-icon start>mdi-check-circle</v-icon>
                Termin annehmen
              </v-btn>
            </v-col>

            <!-- Decline -->
            <v-col v-if="email.isAppointment" cols="12" sm="6">
              <v-btn
                block
                color="error"
                variant="outlined"
                size="large"
                @click="handleAction('decline')"
                :loading="loading"
              >
                <v-icon start>mdi-close-circle</v-icon>
                Ablehnen
              </v-btn>
            </v-col>

            <!-- Snooze / Remind Later -->
            <v-col cols="12" sm="6">
              <v-btn
                block
                color="warning"
                variant="outlined"
                size="large"
                @click="showSnoozeDialog = true"
                :loading="loading"
              >
                <v-icon start>mdi-clock-alert</v-icon>
                Später erinnern
              </v-btn>
            </v-col>

            <!-- Archive -->
            <v-col cols="12" sm="6">
              <v-btn
                block
                color="grey"
                variant="outlined"
                size="large"
                @click="handleAction('archive')"
                :loading="loading"
              >
                <v-icon start>mdi-archive</v-icon>
                Archivieren
              </v-btn>
            </v-col>
          </v-row>
        </div>

        <!-- Already Processed -->
        <v-alert v-else type="success" variant="tonal">
          <v-icon class="mr-2">mdi-check</v-icon>
          Keine Aktion erforderlich. Diese E-Mail wurde bereits verarbeitet.
        </v-alert>
      </v-card-text>

      <v-card-actions>
        <v-btn variant="text" @click="close">Schließen</v-btn>
        <v-spacer />
        <v-btn
          variant="text"
          color="primary"
          @click="markAsRead"
          :disabled="email.isRead"
        >
          <v-icon start>mdi-email-check</v-icon>
          Als gelesen markieren
        </v-btn>
      </v-card-actions>
    </v-card>

    <!-- Snooze Dialog -->
    <v-dialog v-model="showSnoozeDialog" max-width="400">
      <v-card>
        <v-card-title class="d-flex justify-space-between align-center">
          <span>Erinnerung planen</span>
          <v-btn icon size="small" variant="text" @click="showSnoozeDialog = false">
            <v-icon>mdi-close</v-icon>
          </v-btn>
        </v-card-title>
        <v-card-text>
          <v-list>
            <v-list-item @click="snooze(1)">
              <v-list-item-title>In 1 Stunde</v-list-item-title>
            </v-list-item>
            <v-list-item @click="snooze(3)">
              <v-list-item-title>In 3 Stunden</v-list-item-title>
            </v-list-item>
            <v-list-item @click="snooze(24)">
              <v-list-item-title>Morgen</v-list-item-title>
            </v-list-item>
            <v-list-item @click="snooze(72)">
              <v-list-item-title>In 3 Tagen</v-list-item-title>
            </v-list-item>
            <v-list-item @click="snooze(168)">
              <v-list-item-title>In 1 Woche</v-list-item-title>
            </v-list-item>
          </v-list>
        </v-card-text>
        <v-card-actions>
          <v-btn variant="text" @click="showSnoozeDialog = false">Abbrechen</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-dialog>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useMailStore } from '@/stores/mail'
import type { EmailResponse } from '@/types/email'
import dayjs from 'dayjs'
import DOMPurify from 'dompurify'

interface Props {
  modelValue: boolean
  email: EmailResponse | null
}

const props = defineProps<Props>()
const emit = defineEmits(['update:modelValue', 'action-executed'])

const mailStore = useMailStore()
const loading = ref(false)
const expandedPanels = ref<number[]>([])
const showSnoozeDialog = ref(false)

const extractedEventData = computed(() => {
  if (!props.email?.extractedData) return null
  try {
    return JSON.parse(props.email.extractedData)
  } catch {
    return null
  }
})

async function handleAction(action: 'accept' | 'decline' | 'archive') {
  if (!props.email) return

  loading.value = true
  try {
    await mailStore.performAction(props.email.id, {
      emailId: props.email.id,
      action,
      createCalendarEvent: action === 'accept' && props.email.isAppointment
    })

    emit('action-executed')
  } catch (error) {
    console.error('Fehler beim Ausführen der Aktion:', error)
  } finally {
    loading.value = false
  }
}

async function snooze(hours: number) {
  if (!props.email) return

  const snoozeUntil = dayjs().add(hours, 'hour').toISOString()

  loading.value = true
  showSnoozeDialog.value = false

  try {
    await mailStore.performAction(props.email.id, {
      emailId: props.email.id,
      action: 'snooze',
      snoozeUntil
    })

    emit('action-executed')
  } catch (error) {
    console.error('Fehler beim Snoozen:', error)
  } finally {
    loading.value = false
  }
}

async function markAsRead() {
  if (!props.email) return

  try {
    await mailStore.markAsRead(props.email.id, true)
  } catch (error) {
    console.error('Fehler beim Markieren:', error)
  }
}

function close() {
  emit('update:modelValue', false)
}

function formatDate(date: string): string {
  return dayjs(date).format('DD.MM.YYYY HH:mm')
}

function formatDateTime(date: string): string {
  return dayjs(date).format('DD.MM.YYYY HH:mm')
}

function formatFileSize(bytes: number): string {
  if (bytes < 1024) return bytes + ' B'
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB'
  return (bytes / (1024 * 1024)).toFixed(1) + ' MB'
}

function getFileIcon(contentType: string): string {
  if (contentType.includes('pdf')) return 'mdi-file-pdf-box'
  if (contentType.includes('word') || contentType.includes('document')) return 'mdi-file-word'
  if (contentType.includes('excel') || contentType.includes('spreadsheet')) return 'mdi-file-excel'
  if (contentType.includes('image')) return 'mdi-file-image'
  if (contentType.includes('zip') || contentType.includes('compressed')) return 'mdi-folder-zip'
  return 'mdi-file'
}

function sanitizeHtml(html: string): string {
  return DOMPurify.sanitize(html, {
    ALLOWED_TAGS: ['p', 'br', 'strong', 'em', 'u', 'a', 'ul', 'ol', 'li', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6'],
    ALLOWED_ATTR: ['href', 'target']
  })
}

function getCategoryColor(category: string): string {
  const colors: Record<string, string> = {
    appointment: 'purple',
    question: 'orange',
    information: 'blue',
    task: 'green',
    newsletter: 'grey',
    spam: 'red'
  }
  return colors[category] || 'grey'
}

function getCategoryLabel(category: string): string {
  const labels: Record<string, string> = {
    appointment: 'Termin',
    question: 'Frage',
    information: 'Info',
    task: 'Aufgabe',
    newsletter: 'Newsletter',
    spam: 'Spam'
  }
  return labels[category] || category
}
</script>

<style scoped>
.email-body {
  padding: 16px;
  background-color: #f5f5f5;
  border-radius: 4px;
  max-height: 400px;
  overflow-y: auto;
}

.email-body-text {
  white-space: pre-wrap;
  padding: 16px;
  background-color: #f5f5f5;
  border-radius: 4px;
  max-height: 400px;
  overflow-y: auto;
  font-family: monospace;
}
</style>
