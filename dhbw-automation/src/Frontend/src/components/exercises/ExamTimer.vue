<template>
  <div class="exam-timer" :class="{ 'warning': isWarning, 'critical': isCritical }">
    <v-chip
      :color="timerColor"
      variant="flat"
      size="large"
      class="timer-chip"
    >
      <v-icon start :class="{ 'pulse': isCritical }">mdi-timer-outline</v-icon>
      <span class="timer-display">{{ formattedTime }}</span>
    </v-chip>

    <!-- Progress bar -->
    <v-progress-linear
      v-if="showProgress"
      :model-value="progressPercent"
      :color="timerColor"
      height="4"
      class="mt-2"
      rounded
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'

interface Props {
  totalSeconds: number
  showProgress?: boolean
  warningThreshold?: number // Seconds remaining to show warning
  criticalThreshold?: number // Seconds remaining to show critical
  autoStart?: boolean
  paused?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  showProgress: true,
  warningThreshold: 60,
  criticalThreshold: 30,
  autoStart: true,
  paused: false
})

const emit = defineEmits(['tick', 'warning', 'critical', 'expired'])

const remainingSeconds = ref(props.totalSeconds)
const intervalId = ref<number | null>(null)

const formattedTime = computed(() => {
  const minutes = Math.floor(remainingSeconds.value / 60)
  const seconds = remainingSeconds.value % 60
  return `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`
})

const progressPercent = computed(() => {
  return (remainingSeconds.value / props.totalSeconds) * 100
})

const isWarning = computed(() => {
  return remainingSeconds.value <= props.warningThreshold && remainingSeconds.value > props.criticalThreshold
})

const isCritical = computed(() => {
  return remainingSeconds.value <= props.criticalThreshold
})

const timerColor = computed(() => {
  if (isCritical.value) return 'error'
  if (isWarning.value) return 'warning'
  return 'primary'
})

function startTimer() {
  if (intervalId.value) return

  intervalId.value = window.setInterval(() => {
    if (props.paused) return

    remainingSeconds.value--
    emit('tick', remainingSeconds.value)

    if (remainingSeconds.value === props.warningThreshold) {
      emit('warning', remainingSeconds.value)
    }

    if (remainingSeconds.value === props.criticalThreshold) {
      emit('critical', remainingSeconds.value)
    }

    if (remainingSeconds.value <= 0) {
      stopTimer()
      emit('expired')
    }
  }, 1000)
}

function stopTimer() {
  if (intervalId.value) {
    clearInterval(intervalId.value)
    intervalId.value = null
  }
}

function resetTimer() {
  stopTimer()
  remainingSeconds.value = props.totalSeconds
}

// Expose methods for parent component
defineExpose({
  start: startTimer,
  stop: stopTimer,
  reset: resetTimer,
  remaining: remainingSeconds
})

watch(() => props.paused, (paused) => {
  if (paused) {
    stopTimer()
  } else {
    startTimer()
  }
})

watch(() => props.totalSeconds, (newVal) => {
  remainingSeconds.value = newVal
})

onMounted(() => {
  if (props.autoStart) {
    startTimer()
  }
})

onUnmounted(() => {
  stopTimer()
})
</script>

<style scoped>
.exam-timer {
  display: inline-block;
}

.timer-chip {
  font-weight: 600;
  font-size: 1.1rem;
}

.timer-display {
  font-family: 'Roboto Mono', monospace;
  min-width: 60px;
  text-align: center;
}

.exam-timer.warning .timer-chip {
  animation: pulse-warning 1s ease-in-out infinite;
}

.exam-timer.critical .timer-chip {
  animation: pulse-critical 0.5s ease-in-out infinite;
}

.pulse {
  animation: icon-pulse 0.5s ease-in-out infinite;
}

@keyframes pulse-warning {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.8; }
}

@keyframes pulse-critical {
  0%, 100% { opacity: 1; transform: scale(1); }
  50% { opacity: 0.9; transform: scale(1.02); }
}

@keyframes icon-pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.5; }
}
</style>
