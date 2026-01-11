<template>
  <v-container>
    <div class="d-flex justify-space-between align-center mb-6">
      <div class="d-flex align-center">
        <v-btn icon variant="text" @click="$router.back()" class="mr-3">
          <v-icon>mdi-arrow-left</v-icon>
        </v-btn>
        <h1 class="text-h3">
          <v-icon left size="large">mdi-chat-question-outline</v-icon>
          AI Rückfragen
        </h1>
      </div>
      <div class="d-flex gap-3">
        <v-btn
          color="success"
          variant="outlined"
          @click="handleBulkConfirm"
          :disabled="validationStore.pendingCount === 0"
        >
          <v-icon start>mdi-check-all</v-icon>
          Alle Hochbewerteten bestätigen
        </v-btn>
        <v-btn
          color="primary"
          variant="elevated"
          @click="handleRefresh"
          :loading="validationStore.isLoading"
        >
          <v-icon start>mdi-refresh</v-icon>
          Aktualisieren
        </v-btn>
      </div>
    </div>

    <!-- Statistics Summary -->
    <v-row class="mb-4">
      <v-col cols="12" md="3">
        <v-card>
          <v-card-text class="text-center">
            <div class="text-h3 text-primary">{{ validationStore.pendingCount }}</div>
            <div class="text-subtitle-1 text-grey">Ausstehend</div>
          </v-card-text>
        </v-card>
      </v-col>
      <v-col cols="12" md="3">
        <v-card>
          <v-card-text class="text-center">
            <div class="text-h3 text-warning">{{ validationStore.highPriorityCount }}</div>
            <div class="text-subtitle-1 text-grey">Hohe Priorität</div>
          </v-card-text>
        </v-card>
      </v-col>
      <v-col cols="12" md="3">
        <v-card>
          <v-card-text class="text-center">
            <div class="text-h3 text-info">{{ validationStore.withQuestionsCount }}</div>
            <div class="text-subtitle-1 text-grey">Mit Fragen</div>
          </v-card-text>
        </v-card>
      </v-col>
      <v-col cols="12" md="3">
        <v-card>
          <v-card-text class="text-center">
            <div class="text-h3 text-error">{{ validationStore.lowConfidenceCount }}</div>
            <div class="text-subtitle-1 text-grey">Niedriger Score</div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <!-- Filters -->
    <v-card class="mb-4">
      <v-card-text>
        <v-row align="center">
          <v-col cols="12" md="4">
            <v-select
              v-model="statusFilter"
              :items="statusOptions"
              label="Status Filter"
              variant="outlined"
              density="comfortable"
              @update:model-value="applyFilters"
            />
          </v-col>
          <v-col cols="12" md="4">
            <v-select
              v-model="entityTypeFilter"
              :items="entityTypeOptions"
              label="Typ Filter"
              variant="outlined"
              density="comfortable"
            />
          </v-col>
          <v-col cols="12" md="4">
            <v-select
              v-model="priorityFilter"
              :items="priorityOptions"
              label="Priorität Filter"
              variant="outlined"
              density="comfortable"
            />
          </v-col>
        </v-row>
      </v-card-text>
    </v-card>

    <!-- Entities List -->
    <v-card v-if="filteredEntities.length > 0">
      <v-card-title>
        {{ filteredEntities.length }} Entitäten
        <v-spacer></v-spacer>
        <v-chip v-if="selectedEntities.length > 0" color="primary">
          {{ selectedEntities.length }} ausgewählt
        </v-chip>
      </v-card-title>
      <v-card-text>
        <v-data-table
          v-model="selectedEntities"
          :headers="headers"
          :items="filteredEntities"
          :items-per-page="10"
          show-select
          class="elevation-1"
        >
          <!-- Entity Type -->
          <template v-slot:item.entityType="{ item }">
            <v-chip :prepend-icon="getEntityIcon(item.entityType)" size="small">
              {{ getEntityTypeLabel(item.entityType) }}
            </v-chip>
          </template>

          <!-- Title -->
          <template v-slot:item.title="{ item }">
            <div class="font-weight-medium">{{ getEntityTitle(item) }}</div>
            <div class="text-caption text-grey">{{ getEntityDescription(item) }}</div>
          </template>

          <!-- Confidence Score -->
          <template v-slot:item.confidenceScore="{ item }">
            <v-chip
              :color="getConfidenceColor(item.confidenceScore)"
              size="small"
            >
              {{ item.confidenceScore }}%
            </v-chip>
          </template>

          <!-- Priority -->
          <template v-slot:item.priority="{ item }">
            <v-chip
              :color="getPriorityColor(item.priority)"
              size="small"
            >
              {{ item.priority }}
            </v-chip>
          </template>

          <!-- Questions -->
          <template v-slot:item.questions="{ item }">
            <v-chip
              v-if="item.questions.length > 0"
              color="warning"
              size="small"
              prepend-icon="mdi-help-circle"
            >
              {{ item.questions.length }}
            </v-chip>
            <v-chip v-else color="success" size="small">
              <v-icon>mdi-check</v-icon>
            </v-chip>
          </template>

          <!-- Created At -->
          <template v-slot:item.createdAt="{ item }">
            <div class="text-caption">{{ formatDate(item.createdAt) }}</div>
          </template>

          <!-- Actions -->
          <template v-slot:item.actions="{ item }">
            <v-btn
              icon
              size="small"
              variant="text"
              @click="openEntityDialog(item)"
            >
              <v-icon>mdi-eye</v-icon>
            </v-btn>
            <v-btn
              icon
              size="small"
              variant="text"
              color="success"
              @click="quickConfirm(item)"
              :disabled="item.questions.length > 0"
            >
              <v-icon>mdi-check</v-icon>
            </v-btn>
            <v-btn
              icon
              size="small"
              variant="text"
              color="error"
              @click="quickReject(item)"
            >
              <v-icon>mdi-close</v-icon>
            </v-btn>
          </template>
        </v-data-table>
      </v-card-text>
    </v-card>

    <!-- No Entities -->
    <v-card v-else>
      <v-card-text class="text-center py-12">
        <v-icon size="64" color="success">mdi-check-circle-outline</v-icon>
        <div class="text-h5 mt-4">Keine ausstehenden Rückfragen!</div>
        <div class="text-body-1 text-grey mt-2">
          Alle AI-extrahierten Entitäten wurden bereits bestätigt oder abgelehnt.
        </div>
      </v-card-text>
    </v-card>

    <!-- Entity Confirmation Dialog -->
    <EntityConfirmationDialog
      v-model="showDialog"
      :entity="selectedEntity"
      @confirmed="handleEntityConfirmed"
      @rejected="handleEntityRejected"
    />

    <!-- Bulk Confirm Dialog -->
    <v-dialog v-model="showBulkConfirmDialog" max-width="600px">
      <v-card>
        <v-card-title class="d-flex justify-space-between align-center">
          <span>Bulk-Bestätigung</span>
          <v-btn icon size="small" variant="text" @click="showBulkConfirmDialog = false">
            <v-icon>mdi-close</v-icon>
          </v-btn>
          <v-btn icon size="small" variant="text" @click="showBulkConfirmDialog = false">
            <v-icon>mdi-close</v-icon>
          </v-btn>
        </v-card-title>
        <v-card-text>
          <p>Alle Entitäten mit einem Confidence Score von mindestens {{ bulkConfidenceThreshold }}% und ohne offene Fragen werden automatisch bestätigt.</p>
          <v-slider
            v-model="bulkConfidenceThreshold"
            :min="80"
            :max="100"
            :step="5"
            thumb-label
            label="Minimum Confidence Score"
          ></v-slider>
          <v-alert type="info" variant="tonal" class="mt-3">
            {{ eligibleForBulkConfirm }} Entitäten erfüllen diese Kriterien
          </v-alert>
        </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn color="grey" variant="text" @click="showBulkConfirmDialog = false">
            Abbrechen
          </v-btn>
          <v-btn
            color="success"
            variant="elevated"
            @click="confirmBulk"
            :disabled="eligibleForBulkConfirm === 0"
          >
            {{ eligibleForBulkConfirm }} Entitäten bestätigen
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Snackbar for notifications -->
    <v-snackbar v-model="snackbar" :color="snackbarColor" :timeout="3000">
      {{ snackbarText }}
    </v-snackbar>
  </v-container>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useValidationStore } from '@/stores/validation'
import EntityConfirmationDialog from '@/components/validation/EntityConfirmationDialog.vue'
import type { StagedEntity, ParsedTodo, ParsedMeeting, ParsedProject } from '@/types/validation'

const validationStore = useValidationStore()

const showDialog = ref(false)
const selectedEntity = ref<StagedEntity | null>(null)
const selectedEntities = ref<StagedEntity[]>([])
const showBulkConfirmDialog = ref(false)
const bulkConfidenceThreshold = ref(95)

// Filters
const statusFilter = ref('pending_review')
const entityTypeFilter = ref('all')
const priorityFilter = ref('all')

const statusOptions = [
  { title: 'Ausstehend', value: 'pending_review' },
  { title: 'Bestätigt', value: 'confirmed' },
  { title: 'Geändert', value: 'modified' },
  { title: 'Abgelehnt', value: 'rejected' },
  { title: 'Alle', value: 'all' }
]

const entityTypeOptions = [
  { title: 'Alle Typen', value: 'all' },
  { title: 'Aufgaben', value: 'todo' },
  { title: 'Meetings', value: 'meeting' },
  { title: 'Projekte', value: 'project' }
]

const priorityOptions = [
  { title: 'Alle Prioritäten', value: 'all' },
  { title: 'Dringend', value: 'urgent' },
  { title: 'Hoch', value: 'high' },
  { title: 'Mittel', value: 'medium' },
  { title: 'Niedrig', value: 'low' }
]

// Snackbar
const snackbar = ref(false)
const snackbarText = ref('')
const snackbarColor = ref('success')

// Data table headers
const headers = [
  { title: 'Typ', key: 'entityType', sortable: true },
  { title: 'Titel', key: 'title', sortable: false },
  { title: 'Confidence', key: 'confidenceScore', sortable: true },
  { title: 'Priorität', key: 'priority', sortable: true },
  { title: 'Fragen', key: 'questions', sortable: false },
  { title: 'Erstellt', key: 'createdAt', sortable: true },
  { title: 'Aktionen', key: 'actions', sortable: false }
]

// Filtered entities based on filters
const filteredEntities = computed(() => {
  let entities = validationStore.pendingEntities

  if (entityTypeFilter.value !== 'all') {
    entities = entities.filter(e => e.entityType === entityTypeFilter.value)
  }

  if (priorityFilter.value !== 'all') {
    entities = entities.filter(e => e.priority === priorityFilter.value)
  }

  return entities
})

const eligibleForBulkConfirm = computed(() => {
  return filteredEntities.value.filter(
    e => e.confidenceScore >= bulkConfidenceThreshold.value && e.questions.length === 0
  ).length
})

function handleRefresh() {
  validationStore.fetchPendingEntities()
}

function applyFilters() {
  const status = statusFilter.value === 'all' ? undefined : statusFilter.value
  validationStore.fetchPendingEntities(status)
}

function openEntityDialog(entity: StagedEntity) {
  selectedEntity.value = entity
  showDialog.value = true
}

async function quickConfirm(entity: StagedEntity) {
  const result = await validationStore.confirmEntity(entity.id)
  if (result) {
    showSnackbar(`${getEntityTypeLabel(entity.entityType)} erfolgreich bestätigt`, 'success')
  } else {
    showSnackbar('Fehler beim Bestätigen', 'error')
  }
}

async function quickReject(entity: StagedEntity) {
  const success = await validationStore.rejectEntity(entity.id, 'Schnellablehnung')
  if (success) {
    showSnackbar(`${getEntityTypeLabel(entity.entityType)} abgelehnt`, 'info')
  } else {
    showSnackbar('Fehler beim Ablehnen', 'error')
  }
}

function handleBulkConfirm() {
  showBulkConfirmDialog.value = true
}

async function confirmBulk() {
  const result = await validationStore.bulkConfirm(bulkConfidenceThreshold.value)
  if (result) {
    showSnackbar(`${result.promotedCount} Entitäten automatisch bestätigt`, 'success')
    showBulkConfirmDialog.value = false
  } else {
    showSnackbar('Fehler bei der Bulk-Bestätigung', 'error')
  }
}

function handleEntityConfirmed() {
  showDialog.value = false
  selectedEntity.value = null
  showSnackbar('Entität erfolgreich bestätigt', 'success')
}

function handleEntityRejected() {
  showDialog.value = false
  selectedEntity.value = null
  showSnackbar('Entität abgelehnt', 'info')
}

function showSnackbar(text: string, color: string = 'success') {
  snackbarText.value = text
  snackbarColor.value = color
  snackbar.value = true
}

// Helper functions
function getEntityIcon(entityType: string): string {
  const icons: Record<string, string> = {
    todo: 'mdi-checkbox-marked-circle',
    meeting: 'mdi-calendar-account',
    project: 'mdi-briefcase',
    learning_deficit: 'mdi-school',
    reminder: 'mdi-bell'
  }
  return icons[entityType] || 'mdi-file-document'
}

function getEntityTypeLabel(entityType: string): string {
  const labels: Record<string, string> = {
    todo: 'Aufgabe',
    meeting: 'Meeting',
    project: 'Projekt',
    learning_deficit: 'Lerndefizit',
    reminder: 'Erinnerung'
  }
  return labels[entityType] || entityType
}

function getEntityTitle(entity: StagedEntity): string {
  try {
    const data = JSON.parse(entity.entityData)

    switch (entity.entityType) {
      case 'todo':
        return (data as ParsedTodo).title || 'Neue Aufgabe'
      case 'meeting':
        return `Meeting mit ${(data as ParsedMeeting).personName || 'unbekannt'}`
      case 'project':
        return (data as ParsedProject).name || 'Neues Projekt'
      default:
        return 'Neue Entität'
    }
  } catch (e) {
    return 'Fehler beim Parsen'
  }
}

function getEntityDescription(entity: StagedEntity): string {
  try {
    const data = JSON.parse(entity.entityData)

    switch (entity.entityType) {
      case 'todo':
        return (data as ParsedTodo).description || ''
      case 'meeting':
        return (data as ParsedMeeting).purpose || ''
      case 'project':
        return (data as ParsedProject).description || ''
      default:
        return ''
    }
  } catch (e) {
    return ''
  }
}

function getPriorityColor(priority: string): string {
  const colors: Record<string, string> = {
    low: 'success',
    medium: 'info',
    high: 'warning',
    urgent: 'error'
  }
  return colors[priority] || 'grey'
}

function getConfidenceColor(score: number): string {
  if (score >= 90) return 'success'
  if (score >= 70) return 'warning'
  return 'error'
}

function formatDate(dateString: string): string {
  const date = new Date(dateString)
  return date.toLocaleDateString('de-DE', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

onMounted(() => {
  validationStore.fetchPendingEntities()
})
</script>

<style scoped>
.gap-3 {
  gap: 12px;
}
</style>
