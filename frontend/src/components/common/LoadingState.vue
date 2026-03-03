<script setup lang="ts">
defineProps<{
  loading: boolean
  error: string | null
}>()
</script>

<template>
  <div v-if="loading">
    <v-skeleton-loader type="card" class="mb-4" />
    <v-skeleton-loader type="card" />
  </div>

  <v-alert v-else-if="error" type="error" variant="tonal" class="mb-4">
    {{ error }}
  </v-alert>

  <slot v-else />
</template>
