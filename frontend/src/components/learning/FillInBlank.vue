<script setup lang="ts">
import { ref } from 'vue'

defineProps<{
  question: string
}>()

const emit = defineEmits<{
  answer: [value: string]
}>()

const input = ref('')
</script>

<template>
  <div>
    <div class="text-h6 mb-4">{{ question }}</div>

    <v-text-field
      v-model="input"
      variant="outlined"
      label="Deine Antwort"
      @keyup.enter="emit('answer', input)"
    />

    <v-btn
      color="primary"
      :disabled="!input.trim()"
      @click="emit('answer', input)"
    >
      Antworten
    </v-btn>
  </div>
</template>
