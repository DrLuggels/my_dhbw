<template>
  <v-app>
    <v-app-bar app color="primary" dark>
      <v-app-bar-title>DHBW Study Automation</v-app-bar-title>
      <v-spacer></v-spacer>
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
// App Root Component
</script>

<style scoped>
</style>
