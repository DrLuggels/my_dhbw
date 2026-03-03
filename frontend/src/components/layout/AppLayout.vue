<script setup lang="ts">
import { useAppStore } from '@/stores/app'
import AppNav from './AppNav.vue'

const app = useAppStore()
</script>

<template>
  <AppNav />

  <v-main>
    <router-view />
  </v-main>

  <v-snackbar
    v-model="app.snackbar.show"
    :color="app.snackbar.color"
    location="bottom right"
    :timeout="3000"
  >
    {{ app.snackbar.text }}
  </v-snackbar>
</template>
