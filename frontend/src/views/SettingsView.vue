<script setup lang="ts">
import { moodleApi } from '@/api/moodle'
import { useAppStore } from '@/stores/app'
import { ref } from 'vue'

const app = useAppStore()
const moodleToken = ref('')
const moodleUrl = ref('https://moodle.dhbw-ravensburg.de')
const moodleStatus = ref<string | null>(null)
const syncing = ref(false)

async function testMoodle() {
  try {
    const { data } = await moodleApi.connect(moodleToken.value, moodleUrl.value)
    if (data.success) {
      moodleStatus.value = `Verbunden als ${data.data?.username}`
      app.showSuccess('Moodle-Verbindung erfolgreich')
    } else {
      moodleStatus.value = data.message
    }
  } catch {
    moodleStatus.value = 'Verbindung fehlgeschlagen'
  }
}

async function syncMoodle() {
  syncing.value = true
  try {
    const { data } = await moodleApi.sync()
    if (data.data) {
      app.showSuccess(
        `Sync: ${data.data.courses} Kurse, ${data.data.assignments} Aufgaben, ${data.data.resources} Ressourcen`,
      )
    }
  } catch {
    app.showError('Moodle-Sync fehlgeschlagen')
  } finally {
    syncing.value = false
  }
}
</script>

<template>
  <div>
    <v-toolbar flat color="transparent">
      <v-toolbar-title>Einstellungen</v-toolbar-title>
    </v-toolbar>

    <v-container fluid class="pa-6">
      <!-- Moodle Integration -->
      <v-card elevation="1" rounded="lg" class="pa-6 mb-6">
        <div class="text-h6 mb-4">Moodle-Integration</div>

        <v-text-field
          v-model="moodleUrl"
          label="Moodle URL"
          variant="outlined"
          class="mb-4"
        />

        <v-text-field
          v-model="moodleToken"
          label="Moodle Token"
          variant="outlined"
          type="password"
          class="mb-4"
        />

        <v-alert v-if="moodleStatus" type="info" variant="tonal" class="mb-4">
          {{ moodleStatus }}
        </v-alert>

        <div class="d-flex ga-4">
          <v-btn color="primary" @click="testMoodle">Verbindung testen</v-btn>
          <v-btn
            color="primary"
            variant="outlined"
            :loading="syncing"
            @click="syncMoodle"
          >
            Jetzt synchronisieren
          </v-btn>
        </div>
      </v-card>

      <!-- API Keys Info -->
      <v-card elevation="1" rounded="lg" class="pa-6">
        <div class="text-h6 mb-4">AI-Konfiguration</div>
        <div class="text-body-2 text-medium-emphasis">
          API-Keys werden serverseitig in der .env-Datei konfiguriert.
          Die folgenden Dienste werden verwendet:
        </div>
        <v-list density="compact" class="mt-4">
          <v-list-item prepend-icon="mdi-brain" title="OpenAI" subtitle="Embeddings + Chat" />
          <v-list-item prepend-icon="mdi-robot" title="Anthropic Claude" subtitle="Entity-Extraktion + Übungen" />
          <v-list-item prepend-icon="mdi-eye" title="Google Gemini" subtitle="Vision / OCR" />
        </v-list>
      </v-card>
    </v-container>
  </div>
</template>
