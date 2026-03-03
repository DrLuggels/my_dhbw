<script setup lang="ts">
import { ref } from 'vue'

defineProps<{
  question: string
  options: string[]
}>()

const emit = defineEmits<{
  answer: [value: string]
}>()

const selected = ref<string | null>(null)
</script>

<template>
  <div>
    <div class="text-h6 mb-4">{{ question }}</div>

    <v-radio-group v-model="selected">
      <v-radio
        v-for="(option, i) in options"
        :key="i"
        :label="option"
        :value="option"
        class="mb-2"
      />
    </v-radio-group>

    <v-btn
      color="primary"
      :disabled="!selected"
      class="mt-4"
      @click="emit('answer', selected!)"
    >
      Antworten
    </v-btn>
  </div>
</template>
