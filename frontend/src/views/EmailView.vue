<script setup lang="ts">
import { emailApi } from '@/api/email'
import { useAppStore } from '@/stores/app'
import { onMounted, ref, computed } from 'vue'

const app = useAppStore()

interface EmailSummary {
  item_id: string
  subject: string
  sender_name: string
  sender_email: string
  received: string
  is_read: boolean
  has_attachments: boolean
}

interface EmailDetail {
  item_id: string
  subject: string
  sender_name: string
  sender_email: string
  received: string
  is_read: boolean
  has_attachments: boolean
  body: string
  attachments: string[]
}

const loading = ref(true)
const loadingDetail = ref(false)
const error = ref('')
const emails = ref<EmailSummary[]>([])
const total = ref(0)
const unread = ref(0)
const page = ref(1)
const perPage = 20
const selectedId = ref<string | null>(null)
const detail = ref<EmailDetail | null>(null)

const totalPages = computed(() => Math.ceil(total.value / perPage))

function formatDate(iso: string): string {
  const d = new Date(iso)
  const now = new Date()
  const isToday = d.toDateString() === now.toDateString()
  if (isToday) {
    return d.toLocaleTimeString('de-DE', { hour: '2-digit', minute: '2-digit' })
  }
  return d.toLocaleDateString('de-DE', { day: '2-digit', month: '2-digit', year: '2-digit' })
}

function formatDateFull(iso: string): string {
  return new Date(iso).toLocaleString('de-DE', {
    weekday: 'long',
    day: '2-digit',
    month: 'long',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

function senderInitials(name: string): string {
  const parts = name.split(/[\s,]+/).filter(Boolean)
  if (parts.length >= 2) return (parts[0][0] + parts[1][0]).toUpperCase()
  return name.substring(0, 2).toUpperCase()
}

function senderColor(name: string): string {
  const colors = ['#1565C0', '#00897B', '#E65100', '#6A1B9A', '#C62828', '#2E7D32', '#AD1457']
  let hash = 0
  for (const c of name) hash = ((hash << 5) - hash + c.charCodeAt(0)) | 0
  return colors[Math.abs(hash) % colors.length]
}

async function loadInbox() {
  loading.value = true
  error.value = ''
  try {
    const { data } = await emailApi.inbox(perPage, (page.value - 1) * perPage)
    if (data.success) {
      emails.value = data.data.emails
      total.value = data.data.total
      unread.value = data.data.unread
    } else {
      error.value = data.message || 'Fehler beim Laden'
    }
  } catch {
    error.value = 'E-Mail-Server nicht erreichbar. Prüfe die Einstellungen.'
  } finally {
    loading.value = false
  }
}

async function openEmail(email: EmailSummary) {
  selectedId.value = email.item_id
  loadingDetail.value = true
  detail.value = null
  try {
    const { data } = await emailApi.get(email.item_id)
    if (data.success) {
      detail.value = data.data
      email.is_read = true
    }
  } catch {
    app.showError('E-Mail konnte nicht geladen werden')
  } finally {
    loadingDetail.value = false
  }
}

function closeDetail() {
  selectedId.value = null
  detail.value = null
}

async function changePage(p: number) {
  page.value = p
  selectedId.value = null
  detail.value = null
  await loadInbox()
}

onMounted(loadInbox)
</script>

<template>
  <div>
    <v-toolbar flat color="transparent">
      <v-toolbar-title>
        E-Mail
        <v-chip v-if="unread > 0" size="small" color="primary" class="ml-2">
          {{ unread }} ungelesen
        </v-chip>
      </v-toolbar-title>
      <v-spacer />
      <v-btn icon @click="loadInbox" :loading="loading">
        <v-icon>mdi-refresh</v-icon>
      </v-btn>
    </v-toolbar>

    <!-- Loading -->
    <div v-if="loading && emails.length === 0" class="d-flex justify-center pa-12">
      <v-progress-circular indeterminate color="primary" />
    </div>

    <!-- Error -->
    <v-container v-else-if="error" fluid class="pa-6">
      <v-alert type="error" variant="tonal">
        {{ error }}
        <template #append>
          <v-btn variant="text" @click="loadInbox">Erneut versuchen</v-btn>
        </template>
      </v-alert>
    </v-container>

    <!-- Empty -->
    <v-container v-else-if="emails.length === 0" fluid class="pa-6">
      <v-card elevation="1" rounded="lg" class="pa-12 text-center">
        <v-icon size="64" color="grey-lighten-1" class="mb-4">mdi-email-outline</v-icon>
        <div class="text-h6 text-medium-emphasis">Posteingang leer</div>
      </v-card>
    </v-container>

    <!-- Email Content -->
    <v-container v-else fluid class="pa-4 pt-0">
      <v-row no-gutters>
        <!-- Email List -->
        <v-col :cols="selectedId ? 5 : 12" :lg="selectedId ? 4 : 12">
          <v-card elevation="1" rounded="lg" class="email-list-card">
            <v-list class="pa-0" lines="three">
              <template v-for="(email, i) in emails" :key="email.item_id">
                <v-list-item
                  :active="selectedId === email.item_id"
                  @click="openEmail(email)"
                  class="email-item px-4"
                  :class="{ 'email-unread': !email.is_read }"
                >
                  <template #prepend>
                    <v-avatar :color="senderColor(email.sender_name)" size="40" class="mr-3">
                      <span class="text-white text-caption font-weight-bold">
                        {{ senderInitials(email.sender_name) }}
                      </span>
                    </v-avatar>
                  </template>

                  <v-list-item-title class="text-body-2" :class="{ 'font-weight-bold': !email.is_read }">
                    {{ email.sender_name }}
                  </v-list-item-title>

                  <v-list-item-subtitle class="text-body-2 mt-1" :class="{ 'font-weight-medium text-on-surface': !email.is_read }">
                    {{ email.subject }}
                  </v-list-item-subtitle>

                  <template #append>
                    <div class="d-flex flex-column align-end ga-1">
                      <span class="text-caption text-medium-emphasis">{{ formatDate(email.received) }}</span>
                      <div class="d-flex ga-1">
                        <v-icon v-if="email.has_attachments" size="14" color="grey">mdi-paperclip</v-icon>
                        <v-icon v-if="!email.is_read" size="8" color="primary">mdi-circle</v-icon>
                      </div>
                    </div>
                  </template>
                </v-list-item>
                <v-divider v-if="i < emails.length - 1" />
              </template>
            </v-list>

            <!-- Pagination -->
            <v-divider v-if="totalPages > 1" />
            <div v-if="totalPages > 1" class="d-flex justify-center pa-3">
              <v-pagination
                :model-value="page"
                :length="totalPages"
                :total-visible="5"
                density="compact"
                size="small"
                @update:model-value="changePage"
              />
            </div>
          </v-card>
        </v-col>

        <!-- Email Detail -->
        <v-col v-if="selectedId" cols="7" lg="8" class="pl-4">
          <v-card elevation="1" rounded="lg" class="email-detail-card">
            <!-- Loading Detail -->
            <div v-if="loadingDetail" class="d-flex justify-center pa-12">
              <v-progress-circular indeterminate color="primary" />
            </div>

            <!-- Detail Content -->
            <template v-else-if="detail">
              <div class="pa-6 pb-4">
                <div class="d-flex align-start">
                  <div class="flex-grow-1">
                    <div class="text-h6 mb-2">{{ detail.subject }}</div>
                    <div class="d-flex align-center ga-3">
                      <v-avatar :color="senderColor(detail.sender_name)" size="36">
                        <span class="text-white text-caption font-weight-bold">
                          {{ senderInitials(detail.sender_name) }}
                        </span>
                      </v-avatar>
                      <div>
                        <div class="text-body-2 font-weight-medium">{{ detail.sender_name }}</div>
                        <div class="text-caption text-medium-emphasis">{{ detail.sender_email }}</div>
                      </div>
                    </div>
                  </div>
                  <div class="d-flex flex-column align-end ga-1">
                    <v-btn icon size="small" variant="text" @click="closeDetail">
                      <v-icon>mdi-close</v-icon>
                    </v-btn>
                    <span class="text-caption text-medium-emphasis">{{ formatDateFull(detail.received) }}</span>
                  </div>
                </div>

                <!-- Attachments -->
                <div v-if="detail.attachments.length > 0" class="mt-4">
                  <v-chip
                    v-for="att in detail.attachments"
                    :key="att"
                    size="small"
                    variant="tonal"
                    prepend-icon="mdi-paperclip"
                    class="mr-2 mb-1"
                  >
                    {{ att }}
                  </v-chip>
                </div>
              </div>

              <v-divider />

              <!-- Email Body -->
              <div class="pa-6 email-body" v-html="detail.body" />
            </template>
          </v-card>
        </v-col>
      </v-row>
    </v-container>
  </div>
</template>

<style scoped>
.email-list-card {
  max-height: calc(100vh - 120px);
  overflow-y: auto;
}

.email-detail-card {
  max-height: calc(100vh - 120px);
  overflow-y: auto;
}

.email-item {
  cursor: pointer;
  min-height: 72px;
}

.email-unread {
  background: #E3F2FD;
}

.email-body {
  font-size: 14px;
  line-height: 1.7;
  word-break: break-word;
}

.email-body :deep(a) {
  color: #1565C0;
}

.email-body :deep(img) {
  max-width: 100%;
  height: auto;
}

.email-body :deep(table) {
  max-width: 100%;
  overflow-x: auto;
}
</style>
