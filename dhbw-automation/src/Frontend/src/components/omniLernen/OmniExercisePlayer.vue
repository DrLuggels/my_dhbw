<template>
  <div class="exercise-player">
    <!-- Multiple Choice -->
    <div v-if="exercise.componentType === 'multiple_choice'" class="mb-4">
      <v-radio-group v-model="selectedAnswer" :disabled="loading">
        <v-radio v-for="(option, index) in config.options" :key="index" :value="index" :label="option" class="mb-2" />
      </v-radio-group>
    </div>

    <!-- Fill Blank -->
    <div v-else-if="exercise.componentType === 'fill_blank'" class="mb-4">
      <div class="fill-blank-text text-body-1 mb-4">
        <template v-for="(part, index) in fillBlankParts" :key="index">
          <span v-if="part.type === 'text'">{{ part.content }}</span>
          <v-text-field v-else v-model="fillBlankAnswers[part.index]" density="compact" variant="outlined" style="display: inline-block; width: 150px; margin: 0 4px;" :disabled="loading" hide-details />
        </template>
      </div>
    </div>

    <!-- Drag Drop -->
    <div v-else-if="exercise.componentType === 'drag_drop'" class="mb-4">
      <div class="d-flex flex-wrap gap-2 mb-4">
        <v-chip v-for="(item, index) in availableDragItems" :key="index" :draggable="!loading" @dragstart="onDragStart(index)" color="primary" variant="outlined" class="drag-item">
          {{ item }}
        </v-chip>
      </div>
      <div class="drop-zones">
        <div v-for="(zone, zoneIndex) in config.dropZones" :key="zoneIndex" class="drop-zone pa-3 mb-2" @drop="onDrop($event, zoneIndex)" @dragover.prevent>
          <div class="text-caption text-grey mb-2">{{ zone.label }}</div>
          <div class="d-flex flex-wrap gap-2">
            <v-chip v-for="(item, itemIndex) in dragDropAnswers[zoneIndex]" :key="itemIndex" color="success" closable @click:close="removeDragItem(zoneIndex, itemIndex)">
              {{ item }}
            </v-chip>
          </div>
        </div>
      </div>
    </div>

    <!-- Slider -->
    <div v-else-if="exercise.componentType === 'slider'" class="mb-4">
      <v-slider v-model="sliderAnswer" :min="config.min || 0" :max="config.max || 100" :step="config.step || 1" :label="config.label" thumb-label="always" :disabled="loading" />
      <div v-if="config.labels" class="d-flex justify-space-between text-caption text-grey">
        <span>{{ config.labels.min }}</span>
        <span>{{ config.labels.max }}</span>
      </div>
    </div>

    <!-- Code Editor -->
    <div v-else-if="exercise.componentType === 'code_editor'" class="mb-4">
      <v-textarea v-model="codeAnswer" :label="config.language || 'Code'" variant="outlined" rows="10" :disabled="loading" class="code-editor" style="font-family: monospace;" />
      <div v-if="config.hint" class="text-caption text-grey mt-2">
        <v-icon size="small">mdi-lightbulb-outline</v-icon>
        {{ config.hint }}
      </div>
    </div>

    <!-- Text Input (Freitext) -->
    <div v-else-if="exercise.componentType === 'text_input'" class="mb-4">
      <v-textarea v-model="textAnswer" label="Deine Antwort" variant="outlined" rows="4" :disabled="loading" />
    </div>

    <!-- Fallback for unknown types -->
    <div v-else class="mb-4">
      <v-alert type="warning" variant="tonal">
        Unbekannter Uebungstyp: {{ exercise.componentType }}
      </v-alert>
      <v-textarea v-model="textAnswer" label="Deine Antwort" variant="outlined" rows="4" :disabled="loading" />
    </div>

    <!-- Hint -->
    <v-expand-transition>
      <v-alert v-if="showHint && exercise.hint" type="info" variant="tonal" class="mb-4">
        <v-icon start>mdi-lightbulb</v-icon>
        {{ exercise.hint }}
      </v-alert>
    </v-expand-transition>

    <!-- Actions -->
    <div class="d-flex justify-space-between align-center">
      <v-btn variant="text" @click="showHint = !showHint" :disabled="!exercise.hint">
        <v-icon left>mdi-lightbulb-outline</v-icon>
        {{ showHint ? 'Hinweis ausblenden' : 'Hinweis anzeigen' }}
      </v-btn>

      <div>
        <v-btn variant="text" class="mr-2" @click="$emit('skip')">
          Ueberspringen
        </v-btn>
        <v-btn color="primary" size="large" @click="submitAnswer" :loading="loading" :disabled="!canSubmit">
          <v-icon left>mdi-check</v-icon>
          Antwort absenden
        </v-btn>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import type { OmniExercise } from '@/types/omniLearning'

const props = defineProps<{
  exercise: OmniExercise
  loading?: boolean
}>()

const emit = defineEmits<{
  (e: 'submit', answer: any): void
  (e: 'skip'): void
}>()

// State
const showHint = ref(false)
const selectedAnswer = ref<number | null>(null)
const fillBlankAnswers = ref<string[]>([])
const dragDropAnswers = ref<string[][]>([])
const sliderAnswer = ref(50)
const codeAnswer = ref('')
const textAnswer = ref('')
const draggedItem = ref<number | null>(null)
const availableDragItems = ref<string[]>([])

// Computed
const config = computed(() => {
  if (typeof props.exercise.config === 'string') {
    try {
      return JSON.parse(props.exercise.config)
    } catch {
      return {}
    }
  }
  return props.exercise.config || {}
})

const fillBlankParts = computed(() => {
  if (props.exercise.componentType !== 'fill_blank') return []

  const text = props.exercise.question
  const parts: { type: 'text' | 'blank'; content?: string; index?: number }[] = []
  const regex = /___+|\[blank\]|\{blank\}/g
  let lastIndex = 0
  let blankIndex = 0
  let match

  while ((match = regex.exec(text)) !== null) {
    if (match.index > lastIndex) {
      parts.push({ type: 'text', content: text.slice(lastIndex, match.index) })
    }
    parts.push({ type: 'blank', index: blankIndex++ })
    lastIndex = match.index + match[0].length
  }

  if (lastIndex < text.length) {
    parts.push({ type: 'text', content: text.slice(lastIndex) })
  }

  return parts
})

const canSubmit = computed(() => {
  switch (props.exercise.componentType) {
    case 'multiple_choice':
      return selectedAnswer.value !== null
    case 'fill_blank':
      return fillBlankAnswers.value.some(a => a && a.trim().length > 0)
    case 'drag_drop':
      return dragDropAnswers.value.some(zone => zone.length > 0)
    case 'slider':
      return true
    case 'code_editor':
      return codeAnswer.value.trim().length > 0
    case 'text_input':
    default:
      return textAnswer.value.trim().length > 0
  }
})

// Methods
const initializeAnswers = () => {
  showHint.value = false
  selectedAnswer.value = null
  fillBlankAnswers.value = []
  textAnswer.value = ''
  codeAnswer.value = ''
  sliderAnswer.value = config.value.default || 50

  if (props.exercise.componentType === 'fill_blank') {
    const blankCount = fillBlankParts.value.filter(p => p.type === 'blank').length
    fillBlankAnswers.value = new Array(blankCount).fill('')
  }

  if (props.exercise.componentType === 'drag_drop') {
    availableDragItems.value = [...(config.value.items || [])]
    dragDropAnswers.value = (config.value.dropZones || []).map(() => [] as string[])
  }
}

const onDragStart = (index: number) => {
  draggedItem.value = index
}

const onDrop = (_event: DragEvent, zoneIndex: number) => {
  if (draggedItem.value === null) return
  const item = availableDragItems.value[draggedItem.value]
  if (item) {
    dragDropAnswers.value[zoneIndex].push(item)
    availableDragItems.value.splice(draggedItem.value, 1)
  }
  draggedItem.value = null
}

const removeDragItem = (zoneIndex: number, itemIndex: number) => {
  const item = dragDropAnswers.value[zoneIndex][itemIndex]
  dragDropAnswers.value[zoneIndex].splice(itemIndex, 1)
  availableDragItems.value.push(item)
}

const submitAnswer = () => {
  let answer: any

  switch (props.exercise.componentType) {
    case 'multiple_choice':
      answer = selectedAnswer.value
      break
    case 'fill_blank':
      answer = fillBlankAnswers.value
      break
    case 'drag_drop':
      answer = dragDropAnswers.value
      break
    case 'slider':
      answer = sliderAnswer.value
      break
    case 'code_editor':
      answer = codeAnswer.value
      break
    case 'text_input':
    default:
      answer = textAnswer.value
      break
  }

  emit('submit', answer)
}

// Watch for exercise changes
watch(() => props.exercise, () => {
  initializeAnswers()
}, { immediate: true })
</script>

<style scoped>
.exercise-player {
  min-height: 200px;
}

.fill-blank-text {
  line-height: 2.5;
}

.drag-item {
  cursor: grab;
}

.drag-item:active {
  cursor: grabbing;
}

.drop-zone {
  border: 2px dashed #ccc;
  border-radius: 8px;
  min-height: 60px;
  background-color: #f5f5f5;
}

.drop-zone:hover {
  border-color: #1976d2;
  background-color: #e3f2fd;
}

.code-editor {
  font-family: 'Consolas', 'Monaco', monospace !important;
}

.gap-2 {
  gap: 8px;
}
</style>
