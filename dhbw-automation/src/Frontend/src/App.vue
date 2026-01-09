<template>
  <v-app>
    <v-app-bar app color="primary" dark>
      <v-app-bar-title>DHBW Study Automation</v-app-bar-title>
      <v-spacer></v-spacer>
      <v-btn icon to="/validation" title="AI Rückfragen">
        <v-badge
          :content="validationStore.pendingCount"
          :model-value="validationStore.pendingCount > 0"
          color="error"
          overlap
        >
          <v-icon>mdi-chat-question</v-icon>
        </v-badge>
      </v-btn>
      <v-btn icon to="/travel" title="Zugverbindungen">
        <v-icon>mdi-train</v-icon>
      </v-btn>
      <v-btn icon to="/profile">
        <v-icon>mdi-account</v-icon>
      </v-btn>
    </v-app-bar>

    <v-main>
      <router-view />
    </v-main>
  </v-app>
</template>

<script setup lang="ts">
import { useValidationStore } from '@/stores/validation'

const validationStore = useValidationStore()
</script>

<style scoped>
</style>
