<template>
  <v-slide-y-transition>
    <v-alert
      v-if="show && count > 0"
      type="warning"
      variant="tonal"
      closable
      class="mb-4"
      @click:close="emit('dismiss')"
    >
      <template v-slot:prepend>
        <v-icon>mdi-clock-alert</v-icon>
      </template>

      <v-alert-title>
        {{ count }} {{ count === 1 ? 'Aufgabe wartet' : 'Aufgaben warten' }} seit ueber einer Woche
      </v-alert-title>

      <p class="text-body-2 mb-2">
        Diese Aufgaben sind schon laenger offen. Moechtest du einen Termin einplanen?
      </p>

      <template v-slot:append>
        <v-btn
          color="warning"
          variant="flat"
          size="small"
          @click="emit('schedule')"
        >
          <v-icon start>mdi-calendar-plus</v-icon>
          Termin planen
        </v-btn>
      </template>
    </v-alert>
  </v-slide-y-transition>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'

const props = defineProps<{
  count: number
}>()

const emit = defineEmits<{
  (e: 'dismiss'): void
  (e: 'schedule'): void
}>()

const show = ref(true)

// Reset show when count changes
watch(() => props.count, () => {
  if (props.count > 0) {
    show.value = true
  }
})
</script>
