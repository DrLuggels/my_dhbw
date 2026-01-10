<template>
  <v-dialog :model-value="modelValue" max-width="800px" @update:model-value="emit('update:modelValue', $event)" persistent>
    <v-card v-if="entity">
      <!-- Header -->
      <v-card-title class="d-flex justify-space-between align-center bg-primary">
        <span class="text-white">
          <v-icon class="mr-2 text-white">{{ getEntityIcon(entity.entityType) }}</v-icon>
          {{ getEntityTypeLabel(entity.entityType) }} bestätigen
        </span>
        <div class="d-flex align-center gap-2">
          <v-btn icon size="small" variant="text" @click="handleClose" class="text-white">
            <v-icon color="white">mdi-close</v-icon>
          </v-btn>
          <v-chip :color="getConfidenceColor(entity.confidenceScore)" size="small">
            <v-icon start size="small">mdi-speedometer</v-icon>
            {{ entity.confidenceScore }}%
          </v-chip>
          <v-chip :color="getPriorityColor(entity.priority)" size="small">
            {{ entity.priority }}
          </v-chip>
        </div>
      </v-card-title>

      <v-card-text class="pt-4">
        <!-- Entity Data Display -->
        <v-alert
          v-if="entity.confidenceScore < 70"
          type="warning"
          variant="tonal"
          class="mb-4"
        >
          <v-icon start>mdi-alert</v-icon>
          Niedriger Confidence Score! Bitte überprüfen Sie die Daten sorgfältig.
        </v-alert>

        <!-- Entity Details -->
        <v-card variant="outlined" class="mb-4">
          <v-card-title class="text-h6">Erkannte Daten</v-card-title>
          <v-card-text>
            <component
              :is="getEntityComponent(entity.entityType)"
              :data="parsedData"
              :editable="false"
            />
          </v-card-text>
        </v-card>

        <!-- Questions Section -->
        <v-card v-if="entity.questions.length > 0" variant="outlined" class="mb-4">
          <v-card-title class="d-flex align-center text-h6">
            <v-icon start color="warning">mdi-help-circle</v-icon>
            Klärungsfragen ({{ unansweredCount }}/{{ entity.questions.length }})
          </v-card-title>
          <v-card-text>
            <QuestionsList
              :questions="entity.questions"
              v-model:answers="answers"
            />
          </v-card-text>
        </v-card>

        <!-- Source Document Info -->
        <v-card v-if="entity.sourceDocument" variant="outlined" class="mb-4">
          <v-card-title class="text-subtitle-1">Quelldokument</v-card-title>
          <v-card-text>
            <v-list-item density="compact">
              <template v-slot:prepend>
                <v-icon>mdi-file-document</v-icon>
              </template>
              <v-list-item-title>{{ entity.sourceDocument.fileName }}</v-list-item-title>
              <v-list-item-subtitle>{{ entity.sourceDocument.category }}</v-list-item-subtitle>
            </v-list-item>
          </v-card-text>
        </v-card>

        <!-- User Notes -->
        <v-textarea
          v-model="userNotes"
          label="Notizen (optional)"
          rows="3"
          variant="outlined"
          placeholder="Zusätzliche Notizen oder Korrekturen..."
        ></v-textarea>
      </v-card-text>

      <!-- Actions -->
      <v-card-actions class="pa-4">
        <v-btn
          color="error"
          variant="outlined"
          @click="handleReject"
          :loading="isRejecting"
        >
          <v-icon start>mdi-close</v-icon>
          Ablehnen
        </v-btn>

        <v-spacer></v-spacer>

        <v-btn
          color="grey"
          variant="text"
          @click="handleClose"
          :disabled="isConfirming || isRejecting"
        >
          Später
        </v-btn>

        <v-btn
          color="success"
          variant="elevated"
          @click="handleConfirm"
          :loading="isConfirming"
          :disabled="hasCriticalUnanswered"
        >
          <v-icon start>mdi-check</v-icon>
          Bestätigen
        </v-btn>
      </v-card-actions>

      <!-- Reject Dialog -->
      <v-dialog v-model="showRejectDialog" max-width="500px">
        <v-card>
          <v-card-title>Ablehnen bestätigen</v-card-title>
          <v-card-text>
            <v-textarea
              v-model="rejectReason"
              label="Grund für Ablehnung"
              rows="3"
              variant="outlined"
              placeholder="Warum wird diese Entität abgelehnt?"
              autofocus
            ></v-textarea>
          </v-card-text>
          <v-card-actions>
            <v-spacer></v-spacer>
            <v-btn color="grey" variant="text" @click="showRejectDialog = false">
              Abbrechen
            </v-btn>
            <v-btn
              color="error"
              variant="elevated"
              @click="confirmReject"
              :disabled="!rejectReason"
            >
              Ablehnen
            </v-btn>
          </v-card-actions>
        </v-card>
      </v-dialog>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useValidationStore } from '@/stores/validation'
import QuestionsList from './QuestionsList.vue'
import TodoDisplay from './TodoDisplay.vue'
import MeetingDisplay from './MeetingDisplay.vue'
import ProjectDisplay from './ProjectDisplay.vue'
import type { StagedEntity } from '@/types/validation'

const props = defineProps<{
  modelValue: boolean
  entity: StagedEntity | null
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  'confirmed': []
  'rejected': []
}>()

const validationStore = useValidationStore()

const answers = ref<Record<string, string>>({})
const userNotes = ref('')
const rejectReason = ref('')
const showRejectDialog = ref(false)
const isConfirming = ref(false)
const isRejecting = ref(false)

const parsedData = computed(() => {
  if (!props.entity) return null
  try {
    return JSON.parse(props.entity.entityData)
  } catch (e) {
    console.error('Failed to parse entity data:', e)
    return null
  }
})

const unansweredCount = computed(() => {
  if (!props.entity) return 0
  return props.entity.questions.filter(q => !q.isAnswered && !answers.value[q.fieldName]).length
})

const hasCriticalUnanswered = computed(() => {
  if (!props.entity) return false
  return props.entity.questions.some(
    q => q.priority === 'critical' && !q.isAnswered && !answers.value[q.fieldName]
  )
})

watch(() => props.entity, (newEntity) => {
  if (newEntity) {
    // Reset state when entity changes
    answers.value = {}
    userNotes.value = ''
    rejectReason.value = ''

    // Pre-fill already answered questions
    newEntity.questions.forEach(q => {
      if (q.isAnswered && q.userAnswer) {
        answers.value[q.fieldName] = q.userAnswer
      }
    })
  }
})

async function handleConfirm() {
  if (!props.entity) return

  isConfirming.value = true

  try {
    // First, answer any questions
    if (Object.keys(answers.value).length > 0) {
      await validationStore.answerQuestions(props.entity.id, answers.value)
    }

    // Then confirm the entity
    await validationStore.confirmEntity(props.entity.id, userNotes.value || undefined)

    emit('confirmed')
    emit('update:modelValue', false)
  } catch (error) {
    console.error('Error confirming entity:', error)
  } finally {
    isConfirming.value = false
  }
}

function handleReject() {
  showRejectDialog.value = true
}

async function confirmReject() {
  if (!props.entity || !rejectReason.value) return

  isRejecting.value = true

  try {
    await validationStore.rejectEntity(props.entity.id, rejectReason.value)

    showRejectDialog.value = false
    emit('rejected')
    emit('update:modelValue', false)
  } catch (error) {
    console.error('Error rejecting entity:', error)
  } finally {
    isRejecting.value = false
  }
}

function handleClose() {
  emit('update:modelValue', false)
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

function getEntityComponent(entityType: string) {
  const components: Record<string, any> = {
    todo: TodoDisplay,
    meeting: MeetingDisplay,
    project: ProjectDisplay
  }
  return components[entityType] || 'div'
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
</script>

<style scoped>
.gap-2 {
  gap: 8px;
}
</style>
