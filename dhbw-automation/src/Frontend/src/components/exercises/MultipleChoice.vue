<template>
  <div class="multiple-choice">
    <v-radio-group
      v-if="!allowMultiple"
      v-model="selectedSingle"
      :disabled="disabled"
      @update:model-value="emitChange"
    >
      <v-radio
        v-for="option in options"
        :key="option.id"
        :value="option.id"
        :class="{ 'option-card': true, 'selected': selectedSingle === option.id }"
      >
        <template #label>
          <div class="option-label" v-html="option.label" />
        </template>
      </v-radio>
    </v-radio-group>

    <div v-else class="checkbox-group">
      <v-checkbox
        v-for="option in options"
        :key="option.id"
        v-model="selectedMultiple"
        :value="option.id"
        :disabled="disabled"
        :class="{ 'option-card': true, 'selected': selectedMultiple.includes(option.id) }"
        @update:model-value="emitChange"
      >
        <template #label>
          <div class="option-label" v-html="option.label" />
        </template>
      </v-checkbox>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'

interface Option {
  id: string
  label: string
  isCorrect?: boolean
  explanation?: string
}

interface Props {
  config: {
    config?: {
      allowMultiple?: boolean
      shuffleOptions?: boolean
      layout?: 'vertical' | 'horizontal' | 'grid'
    }
    options?: Option[]
  }
  disabled?: boolean
  modelValue?: string | string[]
}

const props = defineProps<Props>()
const emit = defineEmits(['update:modelValue', 'change'])

const selectedSingle = ref<string>('')
const selectedMultiple = ref<string[]>([])

const allowMultiple = computed(() => props.config?.config?.allowMultiple || false)

const options = computed(() => {
  let opts = props.config?.options || []
  if (props.config?.config?.shuffleOptions) {
    opts = [...opts].sort(() => Math.random() - 0.5)
  }
  return opts
})

function emitChange() {
  const value = allowMultiple.value ? selectedMultiple.value : selectedSingle.value
  emit('update:modelValue', value)
  emit('change', value)
}

// Initialize from modelValue
watch(() => props.modelValue, (val) => {
  if (allowMultiple.value) {
    selectedMultiple.value = Array.isArray(val) ? val : []
  } else {
    selectedSingle.value = typeof val === 'string' ? val : ''
  }
}, { immediate: true })
</script>

<style scoped>
.multiple-choice {
  width: 100%;
}

.option-card {
  margin-bottom: 8px;
  padding: 12px 16px;
  border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  border-radius: 8px;
  transition: all 0.2s ease;
  cursor: pointer;
}

.option-card:hover:not(:has(.v-input--disabled)) {
  border-color: rgb(var(--v-theme-primary));
  background: rgba(var(--v-theme-primary), 0.04);
}

.option-card.selected {
  border-color: rgb(var(--v-theme-primary));
  background: rgba(var(--v-theme-primary), 0.08);
}

.option-label {
  font-size: 1rem;
  line-height: 1.5;
}

.option-label :deep(code) {
  background: rgba(0, 0, 0, 0.06);
  padding: 2px 6px;
  border-radius: 4px;
}

.checkbox-group {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

/* Mobile optimizations */
@media (max-width: 600px) {
  .option-card {
    padding: 16px;
  }

  .option-label {
    font-size: 0.95rem;
  }
}
</style>
