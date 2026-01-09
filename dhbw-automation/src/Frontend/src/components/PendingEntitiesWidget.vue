<template>
  <v-card class="pending-entities-widget">
    <v-card-title class="d-flex justify-space-between align-center">
      <span>
        <v-icon class="mr-2">mdi-chat-question</v-icon>
        AI Rückfragen
      </span>
      <v-btn
        icon
        size="small"
        @click="handleRefresh"
        :loading="validationStore.isLoading"
      >
        <v-icon>mdi-refresh</v-icon>
      </v-btn>
    </v-card-title>

    <v-card-text>
      <!-- Summary Stats -->
      <v-row class="mb-4">
        <v-col cols="4">
          <div class="text-center">
            <div class="text-h4 text-primary">{{ validationStore.pendingCount }}</div>
            <div class="text-caption text-grey">Ausstehend</div>
          </div>
        </v-col>
        <v-col cols="4">
          <div class="text-center">
            <div class="text-h4 text-warning">{{ validationStore.withQuestionsCount }}</div>
            <div class="text-caption text-grey">Mit Fragen</div>
          </div>
        </v-col>
        <v-col cols="4">
          <div class="text-center">
            <div class="text-h4 text-error">{{ validationStore.lowConfidenceCount }}</div>
            <div class="text-caption text-grey">Niedrig</div>
          </div>
        </v-col>
      </v-row>

      <!-- Pending Entities List -->
      <v-list v-if="validationStore.pendingCount > 0" density="compact">
        <v-list-subheader>Erfordern Ihre Bestätigung</v-list-subheader>
        <v-list-item
          v-for="entity in topEntities"
          :key="entity.id"
          @click="openEntityDialog(entity)"
          class="entity-item"
        >
          <template v-slot:prepend>
            <v-avatar :color="getPriorityColor(entity.priority)">
              <v-icon>{{ getEntityIcon(entity.entityType) }}</v-icon>
            </v-avatar>
          </template>

          <v-list-item-title>
            {{ getEntityTitle(entity) }}
            <v-chip
              v-if="entity.confidenceScore < 90"
              size="x-small"
              class="ml-2"
              :color="getConfidenceColor(entity.confidenceScore)"
            >
              {{ entity.confidenceScore }}%
            </v-chip>
          </v-list-item-title>

          <v-list-item-subtitle>
            {{ getEntityTypeLabel(entity.entityType) }}
            <span v-if="entity.questions.length > 0" class="ml-2">
              <v-icon size="x-small">mdi-help-circle</v-icon>
              {{ entity.questions.length }} Frage(n)
            </span>
          </v-list-item-subtitle>

          <v-list-item-subtitle class="mt-1">
            {{ getEntityDescription(entity) }}
          </v-list-item-subtitle>

          <template v-slot:append>
            <div class="text-caption text-grey">
              {{ formatDate(entity.createdAt) }}
            </div>
          </template>
        </v-list-item>

        <v-list-item v-if="validationStore.pendingCount > 5">
          <v-btn
            block
            variant="text"
            color="primary"
            @click="navigateToValidationView"
          >
            {{ validationStore.pendingCount - 5 }} weitere anzeigen
          </v-btn>
        </v-list-item>
      </v-list>

      <v-alert v-else type="success" variant="tonal" class="mt-2">
        <v-icon class="mr-2">mdi-check-circle</v-icon>
        Keine ausstehenden Rückfragen!
      </v-alert>

      <!-- Last Sync Info -->
      <div v-if="validationStore.pendingCount > 0" class="text-caption text-grey text-center mt-3">
        Hochgeladene Dokumente werden automatisch von der AI analysiert
      </div>
    </v-card-text>

    <v-card-actions v-if="validationStore.pendingCount > 0">
      <v-btn
        block
        color="primary"
        variant="outlined"
        @click="navigateToValidationView"
      >
        Alle ansehen ({{ validationStore.pendingCount }})
      </v-btn>
    </v-card-actions>

    <!-- Entity Confirmation Dialog -->
    <EntityConfirmationDialog
      v-model="showDialog"
      :entity="selectedEntity"
      @confirmed="handleConfirmed"
      @rejected="handleRejected"
    />
  </v-card>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useValidationStore } from '@/stores/validation'
import EntityConfirmationDialog from './validation/EntityConfirmationDialog.vue'
import type { StagedEntity, ParsedTodo, ParsedMeeting, ParsedProject } from '@/types/validation'

const router = useRouter()
const validationStore = useValidationStore()

const showDialog = ref(false)
const selectedEntity = ref<StagedEntity | null>(null)

// Top 5 entities for widget display
const topEntities = computed(() =>
  validationStore.pendingEntities.slice(0, 5)
)

function handleRefresh() {
  validationStore.fetchPendingEntities()
}

function openEntityDialog(entity: StagedEntity) {
  selectedEntity.value = entity
  showDialog.value = true
}

function navigateToValidationView() {
  router.push('/validation')
}

function handleConfirmed() {
  showDialog.value = false
  selectedEntity.value = null
  validationStore.fetchPendingEntities()
}

function handleRejected() {
  showDialog.value = false
  selectedEntity.value = null
  validationStore.fetchPendingEntities()
}

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
        return (data as ParsedTodo).description || 'Keine Beschreibung'
      case 'meeting':
        return (data as ParsedMeeting).purpose || 'Kein Zweck angegeben'
      case 'project':
        return (data as ParsedProject).description || 'Keine Beschreibung'
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
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffMins = Math.floor(diffMs / 60000)
  const diffHours = Math.floor(diffMs / 3600000)
  const diffDays = Math.floor(diffMs / 86400000)

  if (diffMins < 1) return 'Gerade eben'
  if (diffMins < 60) return `vor ${diffMins} Min.`
  if (diffHours < 24) return `vor ${diffHours} Std.`
  if (diffDays < 7) return `vor ${diffDays} Tag(en)`

  return date.toLocaleDateString('de-DE', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric'
  })
}

onMounted(() => {
  validationStore.fetchPendingEntities()
})
</script>

<style scoped>
.pending-entities-widget {
  height: 100%;
}

.entity-item {
  cursor: pointer;
  transition: background-color 0.2s;
}

.entity-item:hover {
  background-color: rgba(0, 0, 0, 0.04);
}
</style>
