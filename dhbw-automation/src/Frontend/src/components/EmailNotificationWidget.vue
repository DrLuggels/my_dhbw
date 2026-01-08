<template>
  <v-card class="email-notification-widget">
    <v-card-title class="d-flex justify-space-between align-center">
      <span>
        <v-icon class="mr-2">mdi-email</v-icon>
        Posteingang
      </span>
      <v-btn 
        icon 
        size="small" 
        @click="handleRefresh"
        :loading="mailStore.loading"
      >
        <v-icon>mdi-refresh</v-icon>
      </v-btn>
    </v-card-title>

    <v-card-text>
      <!-- Summary Stats -->
      <v-row class="mb-4">
        <v-col cols="4">
          <div class="text-center">
            <div class="text-h4 text-primary">{{ mailStore.unreadCount }}</div>
            <div class="text-caption text-grey">Ungelesen</div>
          </div>
        </v-col>
        <v-col cols="4">
          <div class="text-center">
            <div class="text-h4 text-warning">{{ mailStore.pendingActionsCount }}</div>
            <div class="text-caption text-grey">Aktion nötig</div>
          </div>
        </v-col>
        <v-col cols="4">
          <div class="text-center">
            <div class="text-h4 text-info">{{ appointmentsToday }}</div>
            <div class="text-caption text-grey">Termine heute</div>
          </div>
        </v-col>
      </v-row>

      <!-- Recent Emails requiring action -->
      <v-list v-if="pendingEmails.length > 0" density="compact">
        <v-list-subheader>Erfordern Ihre Aufmerksamkeit</v-list-subheader>
        <v-list-item
          v-for="email in pendingEmails"
          :key="email.id"
          @click="openEmailAction(email)"
          class="email-item"
        >
          <template v-slot:prepend>
            <v-avatar :color="getPriorityColor(email.priority)">
              <v-icon v-if="email.isAppointment">mdi-calendar</v-icon>
              <v-icon v-else>mdi-email</v-icon>
            </v-avatar>
          </template>

          <v-list-item-title>
            {{ email.subject }}
            <v-chip
              v-if="email.category"
              size="x-small"
              class="ml-2"
              :color="getCategoryColor(email.category)"
            >
              {{ getCategoryLabel(email.category) }}
            </v-chip>
          </v-list-item-title>

          <v-list-item-subtitle>
            {{ email.fromName || email.fromAddress }}
          </v-list-item-subtitle>

          <v-list-item-subtitle class="mt-1">
            {{ email.summary || truncateText(email.bodyText, 80) }}
          </v-list-item-subtitle>

          <template v-slot:append>
            <div class="text-caption text-grey">
              {{ formatDate(email.receivedAt) }}
            </div>
          </template>
        </v-list-item>
      </v-list>

      <v-alert v-else type="info" variant="tonal" class="mt-2">
        <v-icon class="mr-2">mdi-check-circle</v-icon>
        Keine ausstehenden E-Mails!
      </v-alert>

      <!-- Last Sync Info -->
      <div v-if="mailStore.lastSync" class="text-caption text-grey text-center mt-3">
        Letzte Synchronisation: {{ formatDate(mailStore.lastSync) }}
      </div>
    </v-card-text>

    <v-card-actions>
      <v-btn
        block
        color="primary"
        variant="outlined"
        to="/mail"
      >
        Alle E-Mails anzeigen
      </v-btn>
    </v-card-actions>

    <!-- Email Action Modal -->
    <EmailActionModal
      v-model="showActionModal"
      :email="selectedEmail"
      @action-executed="handleActionExecuted"
    />
  </v-card>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useMailStore } from '@/stores/mail'
import type { EmailResponse } from '@/types/email'
import EmailActionModal from './EmailActionModal.vue'
import dayjs from 'dayjs'
import relativeTime from 'dayjs/plugin/relativeTime'
import 'dayjs/locale/de'

dayjs.extend(relativeTime)
dayjs.locale('de')

const mailStore = useMailStore()
const showActionModal = ref(false)
const selectedEmail = ref<EmailResponse | null>(null)

const pendingEmails = computed(() => 
  mailStore.summary?.recentEmails.filter(e => e.requiresUserAction) || []
)

const appointmentsToday = computed(() => 
  mailStore.summary?.appointmentsToday || 0
)

async function handleRefresh() {
  try {
    await mailStore.syncEmails()
  } catch (error) {
    console.error('Fehler beim Aktualisieren:', error)
  }
}

function openEmailAction(email: EmailResponse) {
  selectedEmail.value = email
  showActionModal.value = true
}

async function handleActionExecuted() {
  // Refresh summary after action
  await mailStore.fetchSummary()
  showActionModal.value = false
}

function getPriorityColor(priority: number): string {
  switch (priority) {
    case 1: return 'error'
    case 2: return 'warning'
    case 3: return 'info'
    default: return 'grey'
  }
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

function formatDate(date: string | Date): string {
  return dayjs(date).fromNow()
}

function truncateText(text: string, length: number): string {
  if (text.length <= length) return text
  return text.substring(0, length) + '...'
}

onMounted(async () => {
  await mailStore.fetchSummary()
  mailStore.startAutoRefresh()
})

onUnmounted(() => {
  mailStore.stopAutoRefresh()
})
</script>

<style scoped>
.email-notification-widget {
  height: 100%;
}

.email-item {
  cursor: pointer;
  transition: background-color 0.2s;
}

.email-item:hover {
  background-color: rgba(0, 0, 0, 0.05);
}
</style>
