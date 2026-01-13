<template>
  <v-card :variant="variant" class="difficulty-distribution">
    <v-card-title v-if="showTitle" class="d-flex align-center py-2">
      <v-icon color="info" class="mr-2">mdi-chart-donut</v-icon>
      <span class="text-subtitle-1">Schwierigkeitsverteilung</span>
    </v-card-title>

    <v-divider v-if="showTitle" />

    <v-card-text :class="{ 'pa-3': compact }">
      <!-- Loading State -->
      <div v-if="loading" class="d-flex justify-center align-center py-4">
        <v-progress-circular indeterminate color="info" size="32" />
      </div>

      <div v-else>
        <!-- Distribution Bars -->
        <div class="distribution-bars mb-3">
          <!-- Easy -->
          <div class="distribution-row mb-2">
            <div class="d-flex justify-space-between align-center mb-1">
              <span class="text-caption d-flex align-center">
                <v-icon size="14" color="success" class="mr-1">mdi-star-outline</v-icon>
                Leicht
              </span>
              <span class="text-caption">
                {{ distribution.easy.toFixed(0) }}%
                <span class="text-medium-emphasis">(Ziel: 20%)</span>
              </span>
            </div>
            <v-progress-linear
              :model-value="distribution.easy"
              color="success"
              height="8"
              rounded
              :class="{ 'needs-rebalance': Math.abs(distribution.easy - 20) > 10 }"
            />
          </div>

          <!-- Medium -->
          <div class="distribution-row mb-2">
            <div class="d-flex justify-space-between align-center mb-1">
              <span class="text-caption d-flex align-center">
                <v-icon size="14" color="warning" class="mr-1">mdi-star-half-full</v-icon>
                Mittel
              </span>
              <span class="text-caption">
                {{ distribution.medium.toFixed(0) }}%
                <span class="text-medium-emphasis">(Ziel: 40%)</span>
              </span>
            </div>
            <v-progress-linear
              :model-value="distribution.medium"
              color="warning"
              height="8"
              rounded
              :class="{ 'needs-rebalance': Math.abs(distribution.medium - 40) > 10 }"
            />
          </div>

          <!-- Hard -->
          <div class="distribution-row">
            <div class="d-flex justify-space-between align-center mb-1">
              <span class="text-caption d-flex align-center">
                <v-icon size="14" color="error" class="mr-1">mdi-star</v-icon>
                Schwer
              </span>
              <span class="text-caption">
                {{ distribution.hard.toFixed(0) }}%
                <span class="text-medium-emphasis">(Ziel: 40%)</span>
              </span>
            </div>
            <v-progress-linear
              :model-value="distribution.hard"
              color="error"
              height="8"
              rounded
              :class="{ 'needs-rebalance': Math.abs(distribution.hard - 40) > 10 }"
            />
          </div>
        </div>

        <!-- Status Chip -->
        <div class="d-flex justify-center">
          <v-chip
            :color="needsRebalancing ? 'warning' : 'success'"
            size="small"
            variant="tonal"
          >
            <v-icon start size="small">
              {{ needsRebalancing ? 'mdi-alert-circle' : 'mdi-check-circle' }}
            </v-icon>
            {{ needsRebalancing ? 'Rebalancing empfohlen' : 'Ausgewogen' }}
          </v-chip>
        </div>

        <!-- Total Exercises -->
        <div v-if="!compact" class="text-center mt-3 text-caption text-medium-emphasis">
          Basierend auf {{ totalExercises }} Übungen
        </div>

        <!-- Recommended Next Difficulty -->
        <v-alert
          v-if="recommendedDifficulty && !compact"
          type="info"
          variant="tonal"
          density="compact"
          class="mt-3"
        >
          <div class="d-flex align-center">
            <span>Empfohlen: </span>
            <v-chip
              :color="getDifficultyColor(recommendedDifficulty)"
              size="small"
              class="ml-2"
            >
              {{ getDifficultyLabel(recommendedDifficulty) }}
            </v-chip>
          </div>
        </v-alert>
      </div>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import api from '@/services/api'
import { useAuthStore } from '@/stores/auth'

interface Distribution {
  easy: number
  medium: number
  hard: number
}

interface Props {
  compact?: boolean
  showTitle?: boolean
  variant?: 'elevated' | 'flat' | 'tonal' | 'outlined' | 'text' | 'plain'
  subject?: string
}

const props = withDefaults(defineProps<Props>(), {
  compact: false,
  showTitle: true,
  variant: 'outlined'
})

const authStore = useAuthStore()
const loading = ref(true)
const distribution = ref<Distribution>({ easy: 33, medium: 33, hard: 34 })
const needsRebalancing = ref(false)
const totalExercises = ref(0)
const recommendedDifficulty = ref<string | null>(null)

const getDifficultyColor = (difficulty: string): string => {
  const colors: Record<string, string> = {
    easy: 'success',
    medium: 'warning',
    hard: 'error'
  }
  return colors[difficulty] || 'grey'
}

const getDifficultyLabel = (difficulty: string): string => {
  const labels: Record<string, string> = {
    easy: 'Leicht',
    medium: 'Mittel',
    hard: 'Schwer'
  }
  return labels[difficulty] || difficulty
}

const loadDistribution = async () => {
  if (!authStore.user?.id) return

  loading.value = true
  try {
    const response = await api.getDifficultyDistribution(authStore.user.id, props.subject)
    if (response.success) {
      const data = response.data
      distribution.value = {
        easy: data.easyPercentage || 0,
        medium: data.mediumPercentage || 0,
        hard: data.hardPercentage || 0
      }
      needsRebalancing.value = data.needsRebalancing || false
      totalExercises.value = (data.easyCount || 0) + (data.mediumCount || 0) + (data.hardCount || 0)

      // Determine recommended difficulty based on what's underrepresented
      const deviations = [
        { diff: 'easy', deviation: 20 - distribution.value.easy },
        { diff: 'medium', deviation: 40 - distribution.value.medium },
        { diff: 'hard', deviation: 40 - distribution.value.hard }
      ]
      const mostUnderrepresented = deviations.reduce((a, b) => a.deviation > b.deviation ? a : b)
      if (mostUnderrepresented.deviation > 5) {
        recommendedDifficulty.value = mostUnderrepresented.diff
      } else {
        recommendedDifficulty.value = null
      }
    }
  } catch (error) {
    console.error('Error loading distribution:', error)
  } finally {
    loading.value = false
  }
}

// Expose refresh method
const refresh = () => loadDistribution()
defineExpose({ refresh, distribution, needsRebalancing, recommendedDifficulty })

onMounted(loadDistribution)
watch(() => authStore.user?.id, loadDistribution)
watch(() => props.subject, loadDistribution)
</script>

<style scoped>
.difficulty-distribution {
  overflow: hidden;
}

.needs-rebalance {
  animation: pulse-warning 2s ease-in-out infinite;
}

@keyframes pulse-warning {
  0%, 100% {
    opacity: 1;
  }
  50% {
    opacity: 0.7;
  }
}
</style>
