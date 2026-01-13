<template>
  <v-card variant="outlined" class="priority-card">
    <v-card-title class="d-flex align-center py-2">
      <v-icon color="primary" class="mr-2">mdi-target</v-icon>
      <span class="text-subtitle-1">Lernprioritäten</span>
      <v-spacer />
      <v-btn
        icon
        size="small"
        variant="text"
        :loading="refreshing"
        @click="refreshPriorities"
        title="Prioritäten aktualisieren"
      >
        <v-icon>mdi-refresh</v-icon>
      </v-btn>
    </v-card-title>

    <v-divider />

    <v-card-text class="pa-0">
      <!-- Loading State -->
      <div v-if="loading" class="d-flex justify-center align-center py-6">
        <v-progress-circular indeterminate color="primary" />
      </div>

      <!-- Empty State -->
      <v-alert
        v-else-if="priorities.length === 0"
        type="info"
        variant="tonal"
        class="ma-3"
      >
        Keine Prioritäten berechnet. Füge Lernthemen hinzu!
      </v-alert>

      <!-- Priority List -->
      <v-list v-else density="compact" class="py-0">
        <v-list-item
          v-for="(priority, index) in displayedPriorities"
          :key="priority.nodeId"
          :class="{ 'bg-warning-lighten-5': priority.isUrgent }"
          @click="$emit('select', priority)"
        >
          <template v-slot:prepend>
            <v-avatar :color="getPriorityColor(priority.compositeScore)" size="32" class="mr-3">
              <span class="text-caption font-weight-bold text-white">{{ index + 1 }}</span>
            </v-avatar>
          </template>

          <v-list-item-title class="d-flex align-center">
            <span class="font-weight-medium">{{ priority.topic }}</span>
            <v-chip
              v-if="priority.daysUntilDeadline != null && priority.daysUntilDeadline <= 7"
              size="x-small"
              :color="(priority.daysUntilDeadline ?? 0) <= 3 ? 'error' : 'warning'"
              class="ml-2"
            >
              {{ priority.daysUntilDeadline }}d
            </v-chip>
          </v-list-item-title>

          <v-list-item-subtitle class="d-flex align-center gap-2 mt-1">
            <v-chip size="x-small" variant="outlined">{{ priority.subject }}</v-chip>
            <span class="text-caption">
              Mastery: {{ Math.round((1 - priority.masteryGap / 100) * 100) }}%
            </span>
          </v-list-item-subtitle>

          <!-- Score Breakdown (expandable) -->
          <template v-if="showDetails">
            <div class="mt-2 d-flex gap-1">
              <v-tooltip location="top">
                <template v-slot:activator="{ props }">
                  <v-chip v-bind="props" size="x-small" color="error" variant="tonal">
                    U: {{ Math.round(priority.deadlineUrgency) }}
                  </v-chip>
                </template>
                Deadline-Urgency
              </v-tooltip>
              <v-tooltip location="top">
                <template v-slot:activator="{ props }">
                  <v-chip v-bind="props" size="x-small" color="info" variant="tonal">
                    R: {{ Math.round(priority.topicRelevance) }}
                  </v-chip>
                </template>
                Themen-Relevanz
              </v-tooltip>
              <v-tooltip location="top">
                <template v-slot:activator="{ props }">
                  <v-chip v-bind="props" size="x-small" color="warning" variant="tonal">
                    G: {{ Math.round(priority.masteryGap) }}
                  </v-chip>
                </template>
                Mastery-Lücke
              </v-tooltip>
              <v-tooltip location="top">
                <template v-slot:activator="{ props }">
                  <v-chip v-bind="props" size="x-small" color="purple" variant="tonal">
                    D: {{ Math.round(priority.decayAmount) }}
                  </v-chip>
                </template>
                Wissens-Decay
              </v-tooltip>
            </div>
          </template>

          <template v-slot:append>
            <v-btn
              icon
              size="small"
              color="primary"
              variant="text"
              @click.stop="$emit('learn', priority)"
              title="Jetzt lernen"
            >
              <v-icon>mdi-play-circle</v-icon>
            </v-btn>
          </template>
        </v-list-item>

        <!-- Show More Button -->
        <v-list-item v-if="priorities.length > maxItems && !showAll" @click="showAll = true">
          <v-list-item-title class="text-center text-primary">
            + {{ priorities.length - maxItems }} weitere anzeigen
          </v-list-item-title>
        </v-list-item>
      </v-list>
    </v-card-text>

    <!-- Deadline Alert -->
    <v-alert
      v-if="urgentDeadline"
      type="warning"
      variant="tonal"
      density="compact"
      class="ma-2"
    >
      <template v-slot:prepend>
        <v-icon>mdi-calendar-alert</v-icon>
      </template>
      <strong>{{ urgentDeadline.topic }}</strong> fällig in {{ urgentDeadline.daysUntilDeadline }} Tag(en)!
    </v-alert>
  </v-card>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import api from '@/services/api'
import { useAuthStore } from '@/stores/auth'

interface Priority {
  nodeId: number
  subject: string
  topic: string
  subtopic?: string
  compositeScore: number
  deadlineUrgency: number
  topicRelevance: number
  masteryGap: number
  decayAmount: number
  deadline?: string
  daysUntilDeadline?: number | null
  isUrgent?: boolean
}

interface Props {
  maxItems?: number
  showDetails?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  maxItems: 5,
  showDetails: false
})

const emit = defineEmits<{
  select: [priority: Priority]
  learn: [priority: Priority]
}>()

const authStore = useAuthStore()
const loading = ref(true)
const refreshing = ref(false)
const showAll = ref(false)
const priorities = ref<Priority[]>([])

const displayedPriorities = computed(() => {
  if (showAll.value) return priorities.value
  return priorities.value.slice(0, props.maxItems)
})

const urgentDeadline = computed(() => {
  return priorities.value.find(p =>
    p.daysUntilDeadline !== null &&
    p.daysUntilDeadline !== undefined &&
    p.daysUntilDeadline <= 3
  )
})

const getPriorityColor = (score: number): string => {
  if (score >= 70) return 'error'
  if (score >= 50) return 'warning'
  if (score >= 30) return 'info'
  return 'success'
}

const loadPriorities = async () => {
  if (!authStore.user?.id) return

  loading.value = true
  try {
    const response = await api.getPriorities(authStore.user.id, 10)
    if (response.success) {
      priorities.value = response.data.map((p: Priority) => ({
        ...p,
        isUrgent: p.daysUntilDeadline != null && p.daysUntilDeadline <= 3
      }))
    }
  } catch (error) {
    console.error('Error loading priorities:', error)
  } finally {
    loading.value = false
  }
}

const refreshPriorities = async () => {
  if (!authStore.user?.id) return

  refreshing.value = true
  try {
    await api.refreshPriorities(authStore.user.id)
    await loadPriorities()
  } catch (error) {
    console.error('Error refreshing priorities:', error)
  } finally {
    refreshing.value = false
  }
}

// Expose refresh method
const refresh = () => loadPriorities()
defineExpose({ refresh })

onMounted(loadPriorities)
watch(() => authStore.user?.id, loadPriorities)
</script>

<style scoped>
.priority-card {
  overflow: hidden;
}

.gap-1 {
  gap: 0.25rem;
}

.gap-2 {
  gap: 0.5rem;
}

.bg-warning-lighten-5 {
  background-color: rgba(255, 193, 7, 0.05);
}
</style>
