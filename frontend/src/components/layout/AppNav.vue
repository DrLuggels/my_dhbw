<script setup lang="ts">
import { useAppStore } from '@/stores/app'

const app = useAppStore()

const navItems = [
  { title: 'Dashboard', icon: 'mdi-view-dashboard', to: '/dashboard' },
  { title: 'Dokumente', icon: 'mdi-file-document-multiple', to: '/documents' },
  { title: 'Lernen', icon: 'mdi-school', to: '/learning' },
  { title: 'Wissensgraph', icon: 'mdi-graph', to: '/knowledge' },
  { title: 'Kalender', icon: 'mdi-calendar', to: '/calendar' },
  { title: 'Einstellungen', icon: 'mdi-cog', to: '/settings' },
]
</script>

<template>
  <v-navigation-drawer v-model="app.sidebarOpen" :width="260">
    <div class="pa-4">
      <div class="text-h6 font-weight-bold text-primary">DHBW Study</div>
      <div class="text-caption text-medium-emphasis">Automation v2</div>
    </div>

    <v-divider />

    <v-list nav density="comfortable">
      <v-list-item
        v-for="item in navItems"
        :key="item.to"
        :to="item.to"
        :prepend-icon="item.icon"
        :title="item.title"
        rounded="lg"
        class="mx-2"
      />
    </v-list>
  </v-navigation-drawer>

  <v-app-bar flat color="surface" border="b">
    <v-app-bar-nav-icon @click="app.toggleSidebar()" />
    <v-app-bar-title class="text-body-1 font-weight-medium">
      DHBW Study Automation
    </v-app-bar-title>
  </v-app-bar>
</template>
