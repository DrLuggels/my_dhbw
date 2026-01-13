<template>
  <v-card :class="['streak-widget', { 'compact': compact }]" :variant="variant">
    <v-card-text :class="{ 'pa-3': compact, 'text-center': !compact }">
      <!-- Loading State -->
      <div v-if="loading" class="d-flex justify-center align-center" :style="{ minHeight: compact ? '40px' : '80px' }">
        <v-progress-circular indeterminate size="24" color="warning" />
      </div>

      <!-- Streak Display -->
      <div v-else>
        <!-- Compact Mode (for header) -->
        <div v-if="compact" class="d-flex align-center gap-2">
          <v-icon :color="streakData.currentStreak > 0 ? 'warning' : 'grey'" size="20">
            mdi-fire
          </v-icon>
          <span class="text-body-2 font-weight-bold">{{ streakData.currentStreak }}</span>
          <v-chip v-if="streakData.multiplier > 1" size="x-small" color="success" variant="tonal">
            {{ streakData.multiplier.toFixed(2) }}x
          </v-chip>
          <v-icon
            v-if="streakData.freezesAvailable > 0"
            size="16"
            color="info"
            :title="`${streakData.freezesAvailable} Freeze(s) verfügbar`"
          >
            mdi-snowflake
          </v-icon>
        </div>

        <!-- Full Mode (for dashboard/cards) -->
        <div v-else>
          <!-- Streak Count with Fire Icon -->
          <div class="d-flex justify-center align-center mb-2">
            <v-icon
              :size="48"
              :color="getStreakColor"
              :class="{ 'streak-fire-animation': streakData.currentStreak >= 7 }"
            >
              mdi-fire
            </v-icon>
          </div>

          <div class="text-h3 font-weight-bold" :class="`text-${getStreakColor}`">
            {{ streakData.currentStreak }}
          </div>
          <div class="text-subtitle-2 text-medium-emphasis mb-2">
            {{ streakData.currentStreak === 1 ? 'Tag' : 'Tage' }} Streak
          </div>

          <!-- Multiplier Badge -->
          <v-chip
            v-if="streakData.multiplier > 1"
            color="success"
            size="small"
            class="mb-3"
          >
            <v-icon start size="small">mdi-trending-up</v-icon>
            {{ streakData.multiplier.toFixed(2) }}x Bonus
          </v-chip>

          <!-- Progress to next milestone -->
          <div v-if="nextMilestone" class="mb-3">
            <div class="text-caption text-medium-emphasis mb-1">
              Noch {{ nextMilestone.daysLeft }} Tag(e) bis {{ nextMilestone.name }}
            </div>
            <v-progress-linear
              :model-value="nextMilestone.progress"
              color="warning"
              height="6"
              rounded
            />
          </div>

          <!-- Stats Row -->
          <div class="d-flex justify-space-around text-caption mt-2">
            <div class="text-center">
              <div class="font-weight-bold">{{ streakData.longestStreak }}</div>
              <div class="text-medium-emphasis">Rekord</div>
            </div>
            <v-divider vertical />
            <div class="text-center">
              <div class="font-weight-bold">{{ streakData.totalDaysActive }}</div>
              <div class="text-medium-emphasis">Gesamt</div>
            </div>
            <v-divider vertical />
            <div class="text-center">
              <div class="font-weight-bold d-flex align-center justify-center">
                {{ streakData.freezesAvailable }}
                <v-icon size="14" color="info" class="ml-1">mdi-snowflake</v-icon>
              </div>
              <div class="text-medium-emphasis">Freezes</div>
            </div>
          </div>

          <!-- Today Status -->
          <v-alert
            v-if="!streakData.learnedToday"
            type="warning"
            variant="tonal"
            density="compact"
            class="mt-3"
          >
            <template v-slot:prepend>
              <v-icon>mdi-alert-circle</v-icon>
            </template>
            Heute noch nicht gelernt!
          </v-alert>
          <v-alert
            v-else
            type="success"
            variant="tonal"
            density="compact"
            class="mt-3"
          >
            <template v-slot:prepend>
              <v-icon>mdi-check-circle</v-icon>
            </template>
            Heute bereits gelernt!
          </v-alert>
        </div>
      </div>
    </v-card-text>

    <!-- Action Button (only in full mode) -->
    <v-card-actions v-if="!compact && showAction">
      <v-btn
        color="primary"
        variant="tonal"
        block
        :to="actionRoute"
      >
        <v-icon start>mdi-school</v-icon>
        {{ streakData.learnedToday ? 'Weitermachen' : 'Jetzt lernen' }}
      </v-btn>
    </v-card-actions>
  </v-card>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import api from '@/services/api'
import { useAuthStore } from '@/stores/auth'

interface StreakData {
  currentStreak: number
  longestStreak: number
  multiplier: number
  freezesAvailable: number
  freezesUsedThisWeek: number
  totalDaysActive: number
  totalExercisesCompleted: number
  learnedToday: boolean
  lastActivityDate: string
}

interface Props {
  compact?: boolean
  variant?: 'elevated' | 'flat' | 'tonal' | 'outlined' | 'text' | 'plain'
  showAction?: boolean
  actionRoute?: string
}

withDefaults(defineProps<Props>(), {
  compact: false,
  variant: 'tonal',
  showAction: true,
  actionRoute: '/learning'
})

const authStore = useAuthStore()
const loading = ref(true)
const streakData = ref<StreakData>({
  currentStreak: 0,
  longestStreak: 0,
  multiplier: 1.0,
  freezesAvailable: 1,
  freezesUsedThisWeek: 0,
  totalDaysActive: 0,
  totalExercisesCompleted: 0,
  learnedToday: false,
  lastActivityDate: ''
})

const milestones = [
  { days: 7, name: '1 Woche' },
  { days: 14, name: '2 Wochen' },
  { days: 30, name: '1 Monat' },
  { days: 60, name: '2 Monate' },
  { days: 100, name: '100 Tage' },
  { days: 365, name: '1 Jahr' }
]

const getStreakColor = computed(() => {
  if (streakData.value.currentStreak >= 30) return 'error'
  if (streakData.value.currentStreak >= 14) return 'warning'
  if (streakData.value.currentStreak >= 7) return 'orange'
  if (streakData.value.currentStreak >= 1) return 'amber'
  return 'grey'
})

const nextMilestone = computed(() => {
  const current = streakData.value.currentStreak
  const next = milestones.find(m => m.days > current)
  if (!next) return null

  const prev = milestones.filter(m => m.days <= current).pop()
  const prevDays = prev?.days || 0
  const progress = ((current - prevDays) / (next.days - prevDays)) * 100

  return {
    name: next.name,
    daysLeft: next.days - current,
    progress: Math.min(progress, 100)
  }
})

const loadStreak = async () => {
  if (!authStore.user?.id) return

  loading.value = true
  try {
    const response = await api.getStreak(authStore.user.id)
    if (response.success) {
      streakData.value = response.data
    }
  } catch (error) {
    console.error('Error loading streak:', error)
    // Keep default values on error
  } finally {
    loading.value = false
  }
}

// Expose refresh method for parent components
const refresh = () => loadStreak()
defineExpose({ refresh })

onMounted(loadStreak)

// Reload when user changes
watch(() => authStore.user?.id, loadStreak)
</script>

<style scoped>
.streak-widget.compact {
  background: transparent !important;
  box-shadow: none !important;
}

.streak-fire-animation {
  animation: fire-pulse 1.5s ease-in-out infinite;
}

@keyframes fire-pulse {
  0%, 100% {
    transform: scale(1);
    opacity: 1;
  }
  50% {
    transform: scale(1.1);
    opacity: 0.9;
  }
}

.gap-2 {
  gap: 0.5rem;
}
</style>
