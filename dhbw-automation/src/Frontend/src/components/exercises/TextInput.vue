<template>
  <div class="text-input-component">
    <v-textarea
      v-if="multiline"
      v-model="answer"
      :disabled="disabled"
      :placeholder="placeholder"
      :rows="rows"
      variant="outlined"
      auto-grow
      @update:model-value="emitChange"
    />
    <v-text-field
      v-else
      v-model="answer"
      :disabled="disabled"
      :placeholder="placeholder"
      variant="outlined"
      @update:model-value="emitChange"
    />

    <!-- Character count for longer inputs -->
    <div v-if="multiline && answer" class="text-caption text-right text-medium-emphasis mt-1">
      {{ answer.length }} Zeichen
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'

interface Props {
  config: {
    config?: {
      multiline?: boolean
      rows?: number
      placeholder?: string
      maxLength?: number
    }
    correctAnswer?: string
  }
  disabled?: boolean
  modelValue?: string
}

const props = defineProps<Props>()
const emit = defineEmits(['update:modelValue', 'change'])

const answer = ref('')

const multiline = computed(() => props.config?.config?.multiline ?? false)
const rows = computed(() => props.config?.config?.rows ?? 3)
const placeholder = computed(() => props.config?.config?.placeholder ?? 'Deine Antwort...')

function emitChange() {
  emit('update:modelValue', answer.value)
  emit('change', answer.value)
}

watch(() => props.modelValue, (val) => {
  answer.value = val || ''
}, { immediate: true })
</script>

<style scoped>
.text-input-component {
  width: 100%;
  max-width: 600px;
}

/* Mobile */
@media (max-width: 600px) {
  .text-input-component {
    max-width: 100%;
  }
}
</style>
