<template>
  <v-card>
    <v-card-title class="d-flex align-center">
      <v-icon class="mr-2">mdi-connection</v-icon>
      Externe Integrationen
    </v-card-title>

    <v-card-text>
      <v-expansion-panels variant="accordion">
        <!-- Nextcloud Integration -->
        <v-expansion-panel>
          <v-expansion-panel-title>
            <div class="d-flex align-center">
              <v-icon class="mr-3" :color="nextcloudStatus.isConfigured ? 'success' : 'grey'">
                mdi-cloud
              </v-icon>
              <div>
                <div class="font-weight-medium">Nextcloud (DHBW)</div>
                <div class="text-caption text-grey">
                  {{ nextcloudStatus.isConfigured ? 'Verbunden' : 'Nicht konfiguriert' }}
                </div>
              </div>
              <v-spacer />
              <v-chip v-if="nextcloudStatus.isConfigured" size="small" color="success" variant="tonal">
                {{ nextcloudStatus.totalFiles }} Dateien
              </v-chip>
            </div>
          </v-expansion-panel-title>
          <v-expansion-panel-text>
            <v-form @submit.prevent="saveNextcloudCredentials">
              <v-text-field
                v-model="nextcloudForm.url"
                label="Nextcloud URL"
                placeholder="https://nextcloud.dhbw-ravensburg.de"
                prepend-icon="mdi-web"
                variant="outlined"
                density="compact"
                class="mb-2"
              />
              <v-text-field
                v-model="nextcloudForm.username"
                label="Benutzername"
                placeholder="Dein DHBW-Benutzername"
                prepend-icon="mdi-account"
                variant="outlined"
                density="compact"
                class="mb-2"
              />
              <v-text-field
                v-model="nextcloudForm.password"
                :type="showNextcloudPassword ? 'text' : 'password'"
                label="Passwort"
                placeholder="Dein DHBW-Passwort"
                prepend-icon="mdi-lock"
                :append-icon="showNextcloudPassword ? 'mdi-eye-off' : 'mdi-eye'"
                @click:append="showNextcloudPassword = !showNextcloudPassword"
                variant="outlined"
                density="compact"
                class="mb-3"
              />

              <div class="d-flex gap-2">
                <v-btn
                  color="primary"
                  variant="tonal"
                  :loading="testingNextcloud"
                  @click="testNextcloudConnection"
                >
                  <v-icon left>mdi-connection</v-icon>
                  Verbindung testen
                </v-btn>
                <v-btn
                  color="primary"
                  type="submit"
                  :loading="savingNextcloud"
                  :disabled="!nextcloudForm.username || !nextcloudForm.password"
                >
                  <v-icon left>mdi-content-save</v-icon>
                  Speichern
                </v-btn>
                <v-btn
                  v-if="nextcloudStatus.isConfigured"
                  color="secondary"
                  variant="tonal"
                  :loading="syncingNextcloud"
                  @click="syncNextcloud"
                >
                  <v-icon left>mdi-sync</v-icon>
                  Jetzt synchronisieren
                </v-btn>
              </div>

              <v-alert
                v-if="nextcloudMessage"
                :type="nextcloudMessageType"
                class="mt-3"
                density="compact"
                closable
                @click:close="nextcloudMessage = ''"
              >
                {{ nextcloudMessage }}
              </v-alert>

              <div v-if="nextcloudStatus.isConfigured" class="mt-3 text-caption text-grey">
                <div>Letzte Synchronisation: {{ formatDate(nextcloudStatus.lastSyncAt) }}</div>
                <div>Heruntergeladen: {{ nextcloudStatus.downloadedFiles }} / {{ nextcloudStatus.totalFiles }} Dateien</div>
              </div>
            </v-form>
          </v-expansion-panel-text>
        </v-expansion-panel>

        <!-- Moodle Integration -->
        <v-expansion-panel>
          <v-expansion-panel-title>
            <div class="d-flex align-center">
              <v-icon class="mr-3" :color="moodleStatus.isConfigured ? 'success' : 'grey'">
                mdi-school
              </v-icon>
              <div>
                <div class="font-weight-medium">Moodle (E-Learning)</div>
                <div class="text-caption text-grey">
                  {{ moodleStatus.isConfigured ? 'Verbunden' : 'Nicht konfiguriert' }}
                </div>
              </div>
            </div>
          </v-expansion-panel-title>
          <v-expansion-panel-text>
            <v-alert type="info" density="compact" class="mb-3">
              Um Moodle zu verbinden, benötigst du einen API-Token.
              Diesen findest du in Moodle unter: Einstellungen > Sicherheitsschlüssel.
            </v-alert>
            <v-text-field
              v-model="moodleForm.token"
              label="Moodle API Token"
              placeholder="Dein Moodle Web Service Token"
              prepend-icon="mdi-key"
              variant="outlined"
              density="compact"
              class="mb-3"
            />
            <v-btn
              color="primary"
              :loading="savingMoodle"
              :disabled="!moodleForm.token"
              @click="saveMoodleToken"
            >
              <v-icon left>mdi-content-save</v-icon>
              Token speichern
            </v-btn>
          </v-expansion-panel-text>
        </v-expansion-panel>

        <!-- Java-Docs Exercises -->
        <v-expansion-panel>
          <v-expansion-panel-title>
            <div class="d-flex align-center">
              <v-icon class="mr-3" color="orange">mdi-language-java</v-icon>
              <div>
                <div class="font-weight-medium">Java-Docs Übungen</div>
                <div class="text-caption text-grey">
                  {{ javaDocsStatus.exerciseCount }} Übungen verfügbar
                </div>
              </div>
              <v-spacer />
              <v-chip v-if="javaDocsStatus.exerciseCount > 0" size="small" color="orange" variant="tonal">
                {{ javaDocsStatus.topicsCount }} Themen
              </v-chip>
            </div>
          </v-expansion-panel-title>
          <v-expansion-panel-text>
            <p class="text-body-2 mb-3">
              Übungen werden automatisch vom
              <a href="https://jappuccini.github.io/java-docs/" target="_blank">Java-Docs Repository</a>
              synchronisiert.
            </p>
            <v-btn
              color="orange"
              variant="tonal"
              :loading="syncingJavaDocs"
              @click="syncJavaDocs"
            >
              <v-icon left>mdi-sync</v-icon>
              Übungen aktualisieren
            </v-btn>
            <v-alert
              v-if="javaDocsMessage"
              :type="javaDocsMessageType"
              class="mt-3"
              density="compact"
              closable
              @click:close="javaDocsMessage = ''"
            >
              {{ javaDocsMessage }}
            </v-alert>
          </v-expansion-panel-text>
        </v-expansion-panel>

        <!-- E-Mail Integration (bereits vorhanden) -->
        <v-expansion-panel>
          <v-expansion-panel-title>
            <div class="d-flex align-center">
              <v-icon class="mr-3" :color="emailStatus.isEnabled ? 'success' : 'grey'">
                mdi-email
              </v-icon>
              <div>
                <div class="font-weight-medium">E-Mail Synchronisation</div>
                <div class="text-caption text-grey">
                  {{ emailStatus.isEnabled ? emailStatus.emailAddress : 'Nicht konfiguriert' }}
                </div>
              </div>
            </div>
          </v-expansion-panel-title>
          <v-expansion-panel-text>
            <p class="text-body-2 mb-3">
              Die E-Mail-Synchronisation kann in den erweiterten Einstellungen konfiguriert werden.
            </p>
            <v-btn
              color="primary"
              variant="tonal"
              to="/profile/email-settings"
            >
              E-Mail Einstellungen
            </v-btn>
          </v-expansion-panel-text>
        </v-expansion-panel>
      </v-expansion-panels>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import api from '@/services/api'

// Nextcloud State
const nextcloudForm = ref({
  url: 'https://nextcloud.dhbw-ravensburg.de',
  username: '',
  password: ''
})
const nextcloudStatus = ref({
  isConfigured: false,
  isActive: false,
  lastSyncAt: null as Date | null,
  totalFiles: 0,
  downloadedFiles: 0
})
const showNextcloudPassword = ref(false)
const testingNextcloud = ref(false)
const savingNextcloud = ref(false)
const syncingNextcloud = ref(false)
const nextcloudMessage = ref('')
const nextcloudMessageType = ref<'success' | 'error' | 'info'>('info')

// Moodle State
const moodleForm = ref({
  token: ''
})
const moodleStatus = ref({
  isConfigured: false
})
const savingMoodle = ref(false)

// Java-Docs State
const javaDocsStatus = ref({
  exerciseCount: 0,
  topicsCount: 0
})
const syncingJavaDocs = ref(false)
const javaDocsMessage = ref('')
const javaDocsMessageType = ref<'success' | 'error' | 'info'>('info')

// E-Mail State
const emailStatus = ref({
  isEnabled: false,
  emailAddress: ''
})

// Methods
const formatDate = (date: Date | null | string) => {
  if (!date) return 'Nie'
  const d = typeof date === 'string' ? new Date(date) : date
  return d.toLocaleString('de-DE', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

const loadNextcloudStatus = async () => {
  try {
    const response = await api.get('/nextcloud/status')
    nextcloudStatus.value = response.data
  } catch (error) {
    console.log('Nextcloud not configured')
  }
}

const testNextcloudConnection = async () => {
  testingNextcloud.value = true
  nextcloudMessage.value = ''

  try {
    // Erst speichern, dann testen
    await api.post('/nextcloud/credentials', nextcloudForm.value)
    const response = await api.post('/nextcloud/test')

    if (response.data.success) {
      nextcloudMessage.value = 'Verbindung erfolgreich!'
      nextcloudMessageType.value = 'success'
      await loadNextcloudStatus()
    } else {
      nextcloudMessage.value = response.data.message || 'Verbindung fehlgeschlagen'
      nextcloudMessageType.value = 'error'
    }
  } catch (error: any) {
    nextcloudMessage.value = error.response?.data?.message || 'Verbindungsfehler'
    nextcloudMessageType.value = 'error'
  } finally {
    testingNextcloud.value = false
  }
}

const saveNextcloudCredentials = async () => {
  savingNextcloud.value = true
  nextcloudMessage.value = ''

  try {
    const response = await api.post('/nextcloud/credentials', nextcloudForm.value)

    if (response.data.success) {
      nextcloudMessage.value = 'Zugangsdaten gespeichert'
      nextcloudMessageType.value = 'success'
      await loadNextcloudStatus()
      // Passwort-Feld leeren nach Speichern
      nextcloudForm.value.password = ''
    } else {
      nextcloudMessage.value = response.data.message || 'Fehler beim Speichern'
      nextcloudMessageType.value = 'error'
    }
  } catch (error: any) {
    nextcloudMessage.value = error.response?.data?.message || 'Fehler beim Speichern'
    nextcloudMessageType.value = 'error'
  } finally {
    savingNextcloud.value = false
  }
}

const syncNextcloud = async () => {
  syncingNextcloud.value = true
  nextcloudMessage.value = ''

  try {
    const response = await api.post('/nextcloud/sync')

    if (response.data.success) {
      nextcloudMessage.value = `Synchronisation abgeschlossen: ${response.data.added} neue, ${response.data.updated} aktualisiert`
      nextcloudMessageType.value = 'success'
      await loadNextcloudStatus()
    } else {
      nextcloudMessage.value = response.data.error || 'Synchronisation fehlgeschlagen'
      nextcloudMessageType.value = 'error'
    }
  } catch (error: any) {
    nextcloudMessage.value = error.response?.data?.message || 'Synchronisationsfehler'
    nextcloudMessageType.value = 'error'
  } finally {
    syncingNextcloud.value = false
  }
}

const saveMoodleToken = async () => {
  savingMoodle.value = true

  try {
    await api.put('/user/moodle-token', { token: moodleForm.value.token })
    moodleStatus.value.isConfigured = true
    moodleForm.value.token = ''
  } catch (error) {
    console.error('Error saving Moodle token:', error)
  } finally {
    savingMoodle.value = false
  }
}

const loadJavaDocsStatus = async () => {
  try {
    const response = await api.get('/javadocs/topics')
    javaDocsStatus.value.topicsCount = response.data.length

    const exercisesResponse = await api.get('/javadocs/exercises')
    javaDocsStatus.value.exerciseCount = exercisesResponse.data.length
  } catch (error) {
    console.log('Java-Docs not loaded yet')
  }
}

const syncJavaDocs = async () => {
  syncingJavaDocs.value = true
  javaDocsMessage.value = ''

  try {
    const response = await api.post('/javadocs/sync')

    if (response.data.success) {
      javaDocsMessage.value = `Synchronisation abgeschlossen: ${response.data.added} neue, ${response.data.updated} aktualisiert`
      javaDocsMessageType.value = 'success'
      await loadJavaDocsStatus()
    } else {
      javaDocsMessage.value = response.data.error || 'Synchronisation fehlgeschlagen'
      javaDocsMessageType.value = 'error'
    }
  } catch (error: any) {
    javaDocsMessage.value = error.response?.data?.message || 'Synchronisationsfehler'
    javaDocsMessageType.value = 'error'
  } finally {
    syncingJavaDocs.value = false
  }
}

onMounted(async () => {
  await Promise.all([
    loadNextcloudStatus(),
    loadJavaDocsStatus()
  ])
})
</script>

<style scoped>
.gap-2 {
  gap: 8px;
}
</style>
