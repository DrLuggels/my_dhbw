<template>
  <div class="fill-blank">
    <div class="fill-blank-content" v-html="renderedTemplate" />

    <!-- Input fields are rendered dynamically via the template -->
    <div v-if="blankInputs.length" class="blank-inputs mt-4">
      <div v-for="blank in blankInputs" :key="blank.id" class="blank-input-row mb-3">
        <label class="text-body-2 text-medium-emphasis mb-1 d-block">
          Lucke {{ blank.index + 1 }}:
        </label>
        <v-text-field
          v-model="answers[blank.id]"
          :disabled="disabled"
          :hint="blank.hint"
          density="compact"
          variant="outlined"
          hide-details="auto"
          @update:model-value="emitChange"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'

interface Blank {
  id: string
  correctAnswers?: string[]
  hint?: string
}

interface Props {
  config: {
    config?: {
      caseSensitive?: boolean
      allowPartialMatch?: boolean
    }
    template?: string
    blanks?: Blank[]
    correctAnswer?: string
  }
  disabled?: boolean
  modelValue?: string | Record<string, string>
}

const props = defineProps<Props>()
const emit = defineEmits(['update:modelValue', 'change'])

const answers = ref<Record<string, string>>({})

// Parse template and extract blanks
const blankInputs = computed(() => {
  const template = props.config?.template || ''
  const blanks: Array<{ id: string; index: number; hint?: string }> = []

  // Match {{blank:id}} pattern
  const regex = /\{\{blank:(\w+)\}\}/g
  let match
  let index = 0

  while ((match = regex.exec(template)) !== null) {
    const id = match[1]
    const blankConfig = props.config?.blanks?.find(b => b.id === id)
    blanks.push({
      id,
      index,
      hint: blankConfig?.hint
    })
    index++
  }

  return blanks
})

// Render template with input indicators
const renderedTemplate = computed(() => {
  let template = props.config?.template || ''

  // Replace {{blank:id}} with visual placeholder
  template = template.replace(/\{\{blank:(\w+)\}\}/g, (_, id) => {
    const value = answers.value[id]
    if (value) {
      return `<span class="filled-blank">${escapeHtml(value)}</span>`
    }
    return `<span class="blank-placeholder">_____</span>`
  })

  return template
})

function escapeHtml(text: string): string {
  const div = document.createElement('div')
  div.textContent = text
  return div.innerHTML
}

function emitChange() {
  // If only one blank, emit as string; otherwise as object
  if (blankInputs.value.length === 1) {
    const value = answers.value[blankInputs.value[0].id] || ''
    emit('update:modelValue', value)
    emit('change', value)
  } else {
    emit('update:modelValue', { ...answers.value })
    emit('change', { ...answers.value })
  }
}

// Initialize from modelValue
watch(() => props.modelValue, (val) => {
  if (typeof val === 'string' && blankInputs.value.length === 1) {
    answers.value[blankInputs.value[0].id] = val
  } else if (typeof val === 'object' && val !== null) {
    answers.value = { ...val }
  }
}, { immediate: true })

// Initialize empty answers for all blanks
watch(blankInputs, (blanks) => {
  for (const blank of blanks) {
    if (!(blank.id in answers.value)) {
      answers.value[blank.id] = ''
    }
  }
}, { immediate: true })
</script>

<style scoped>
.fill-blank-content {
  font-size: 1.1rem;
  line-height: 2;
  padding: 16px;
  background: rgba(var(--v-theme-surface-variant), 0.3);
  border-radius: 12px;
}

.fill-blank-content :deep(.blank-placeholder) {
  display: inline-block;
  min-width: 80px;
  border-bottom: 2px solid rgb(var(--v-theme-primary));
  margin: 0 4px;
  text-align: center;
}

.fill-blank-content :deep(.filled-blank) {
  display: inline-block;
  padding: 2px 8px;
  background: rgba(var(--v-theme-primary), 0.15);
  border: 1px solid rgba(var(--v-theme-primary), 0.3);
  border-radius: 4px;
  font-weight: 500;
  margin: 0 4px;
}

.fill-blank-content :deep(code) {
  background: rgba(0, 0, 0, 0.06);
  padding: 2px 6px;
  border-radius: 4px;
  font-family: monospace;
}

.blank-input-row {
  max-width: 400px;
}

/* Mobile */
@media (max-width: 600px) {
  .fill-blank-content {
    font-size: 1rem;
    padding: 12px;
  }

  .blank-input-row {
    max-width: 100%;
  }
}
</style>
