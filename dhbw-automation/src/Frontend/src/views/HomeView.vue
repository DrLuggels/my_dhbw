<template>
  <v-container>
    <v-row justify="center">
      <v-col cols="12" md="8">
        <h1 class="text-h3 mb-4">Willkommen zum DHBW Study Automation System</h1>
        <p class="text-body-1 mb-6">
          Dein intelligentes System zur Automatisierung deines Studiums.
        </p>
        
        <v-row>
          <v-col cols="12" md="6">
            <v-card>
              <v-card-title>Schnellstart</v-card-title>
              <v-card-text>
                <v-btn color="primary" to="/login" block class="mb-2">Login</v-btn>
                <v-btn color="secondary" to="/dashboard" block>Zum Dashboard</v-btn>
              </v-card-text>
            </v-card>
          </v-col>
          
          <v-col cols="12" md="6">
            <v-card>
              <v-card-title>Features</v-card-title>
              <v-card-text>
                <ul>
                  <li>📁 Datei-Management</li>
                  <li>🤖 KI-Analyse</li>
                  <li>📅 Kalender-Integration</li>
                  <li>📧 Mail-Automatisierung</li>
                </ul>
              </v-card-text>
            </v-card>
          </v-col>
        </v-row>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
// Home View
</script>
