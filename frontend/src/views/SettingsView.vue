<script setup lang="ts">
import { settingsApi } from '@/api/settings'
import { moodleApi } from '@/api/moodle'
import { emailApi } from '@/api/email'
import { useAppStore } from '@/stores/app'
import { onMounted, ref, computed } from 'vue'

const app = useAppStore()

interface ModelOption { id: string; name: string; description: string }
interface ModelCategory { provider: string; task: string; models: ModelOption[] }
interface UsageSummary { provider: string; model: string; total_input: number; total_output: number; total_calls: number }
interface UsageResponse { by_provider: UsageSummary[]; total_input: number; total_output: number; total_calls: number }

// State
const loading = ref(true)
const saving = ref(false)
const moodleTesting = ref(false)
const moodleSyncing = ref(false)
const moodleStatus = ref<string | null>(null)
const emailTesting = ref(false)
const emailStatus = ref<string | null>(null)

// Settings form
const aiProvider = ref('direct')
const githubToken = ref('')
const openaiKey = ref('')
const anthropicKey = ref('')
const geminiKey = ref('')
const openaiModel = ref('gpt-4.1-mini')
const anthropicModel = ref('claude-sonnet-4-6')
const geminiModel = ref('gemini-2.5-flash')
const embeddingModel = ref('text-embedding-3-small')
const moodleUrl = ref('https://moodle.dhbw-ravensburg.de')
const moodleToken = ref('')
const emailAddress = ref('')
const emailUsername = ref('')
const emailPassword = ref('')
const emailServer = ref('')
const raplaUrl = ref('')

// Flags: whether a key is already set (from server)
const githubTokenSet = ref(false)
const openaiKeySet = ref(false)
const anthropicKeySet = ref(false)
const geminiKeySet = ref(false)
const moodleTokenSet = ref(false)
const emailPasswordSet = ref(false)

// Models catalog
const modelCategories = ref<ModelCategory[]>([])

// Usage
const usage = ref<UsageResponse | null>(null)

// Show/hide password toggles
const showGithub = ref(false)
const showOpenai = ref(false)
const showAnthropic = ref(false)
const showGemini = ref(false)
const showMoodleToken = ref(false)
const showEmailPw = ref(false)

const openaiModels = computed(() =>
  modelCategories.value.find(c => c.provider === 'openai' && c.task.includes('Chat'))?.models ?? []
)
const anthropicModels = computed(() =>
  modelCategories.value.find(c => c.provider === 'anthropic')?.models ?? []
)
const geminiModels = computed(() =>
  modelCategories.value.find(c => c.provider === 'gemini')?.models ?? []
)
const embeddingModels = computed(() =>
  modelCategories.value.find(c => c.task.includes('Embedding'))?.models ?? []
)

function formatNumber(n: number): string {
  if (n >= 1_000_000) return (n / 1_000_000).toFixed(1) + 'M'
  if (n >= 1_000) return (n / 1_000).toFixed(1) + 'K'
  return String(n)
}

function providerIcon(provider: string): string {
  if (provider === 'openai') return 'mdi-brain'
  if (provider === 'anthropic') return 'mdi-robot'
  if (provider === 'gemini') return 'mdi-eye'
  return 'mdi-api'
}

function providerColor(provider: string): string {
  if (provider === 'openai') return '#10A37F'
  if (provider === 'anthropic') return '#D4A06A'
  if (provider === 'gemini') return '#4285F4'
  return '#666'
}

onMounted(async () => {
  try {
    const [settingsRes, modelsRes, usageRes] = await Promise.all([
      settingsApi.get(),
      settingsApi.models(),
      settingsApi.usage(),
    ])

    const s = settingsRes.data.data
    if (s) {
      aiProvider.value = s.ai_provider
      githubTokenSet.value = s.github_token_set
      openaiKeySet.value = s.openai_key_set
      anthropicKeySet.value = s.anthropic_key_set
      geminiKeySet.value = s.gemini_key_set
      openaiModel.value = s.openai_model
      anthropicModel.value = s.anthropic_model
      geminiModel.value = s.gemini_model
      embeddingModel.value = s.embedding_model
      moodleUrl.value = s.moodle_base_url
      moodleTokenSet.value = s.moodle_token_set
      emailAddress.value = s.email_address
      emailUsername.value = s.email_username
      emailPasswordSet.value = s.email_password_set
      emailServer.value = s.email_server
      raplaUrl.value = s.rapla_calendar_url
    }

    modelCategories.value = modelsRes.data.data ?? []
    usage.value = usageRes.data.data ?? null
  } finally {
    loading.value = false
  }
})

async function saveSettings(fields: Record<string, unknown>, successMsg: string) {
  saving.value = true
  try {
    const { data } = await settingsApi.update(fields)
    if (data.success) {
      const s = data.data
      if (s) {
        githubTokenSet.value = s.github_token_set
        openaiKeySet.value = s.openai_key_set
        anthropicKeySet.value = s.anthropic_key_set
        geminiKeySet.value = s.gemini_key_set
        moodleTokenSet.value = s.moodle_token_set
        emailPasswordSet.value = s.email_password_set
      }
      app.showSuccess(successMsg)
    } else {
      app.showError(data.message || 'Fehler beim Speichern')
    }
  } catch {
    app.showError('Fehler beim Speichern')
  } finally {
    saving.value = false
  }
}

function saveApiKeys() {
  const fields: Record<string, unknown> = { ai_provider: aiProvider.value }
  if (githubToken.value) fields.github_token = githubToken.value
  if (openaiKey.value) fields.openai_api_key = openaiKey.value
  if (anthropicKey.value) fields.anthropic_api_key = anthropicKey.value
  if (geminiKey.value) fields.gemini_api_key = geminiKey.value
  saveSettings(fields, 'API-Keys gespeichert')
  // Clear inputs
  githubToken.value = ''
  openaiKey.value = ''
  anthropicKey.value = ''
  geminiKey.value = ''
}

function saveModels() {
  saveSettings({
    openai_model: openaiModel.value,
    anthropic_model: anthropicModel.value,
    gemini_model: geminiModel.value,
    embedding_model: embeddingModel.value,
  }, 'Modelle gespeichert')
}

function saveMoodle() {
  const fields: Record<string, unknown> = { moodle_base_url: moodleUrl.value }
  if (moodleToken.value) fields.moodle_token = moodleToken.value
  saveSettings(fields, 'Moodle-Einstellungen gespeichert')
  moodleToken.value = ''
}

function saveEmail() {
  const fields: Record<string, unknown> = {
    email_address: emailAddress.value,
    email_username: emailUsername.value,
    email_server: emailServer.value,
  }
  if (emailPassword.value) fields.email_password = emailPassword.value
  saveSettings(fields, 'E-Mail-Einstellungen gespeichert')
  emailPassword.value = ''
}

async function testEmail() {
  emailTesting.value = true
  try {
    const { data } = await emailApi.test()
    if (data.success) {
      emailStatus.value = `Verbunden: ${data.data.inbox_count} Mails, ${data.data.unread_count} ungelesen`
      app.showSuccess('E-Mail-Verbindung erfolgreich')
    } else {
      emailStatus.value = data.message
    }
  } catch {
    emailStatus.value = 'Verbindung fehlgeschlagen'
  } finally {
    emailTesting.value = false
  }
}

function saveRapla() {
  saveSettings({ rapla_calendar_url: raplaUrl.value }, 'Rapla-URL gespeichert')
}

async function testMoodle() {
  moodleTesting.value = true
  try {
    const token = moodleToken.value || '__use_saved__'
    const { data } = await moodleApi.connect(
      moodleToken.value || 'saved',
      moodleUrl.value,
    )
    if (data.success) {
      moodleStatus.value = `Verbunden als ${data.data?.username}`
      app.showSuccess('Moodle-Verbindung erfolgreich')
    } else {
      moodleStatus.value = data.message
    }
  } catch {
    moodleStatus.value = 'Verbindung fehlgeschlagen'
  } finally {
    moodleTesting.value = false
  }
}

async function syncMoodle() {
  moodleSyncing.value = true
  try {
    const { data } = await moodleApi.sync()
    if (data.data) {
      app.showSuccess(`Sync: ${data.data.courses} Kurse, ${data.data.assignments} Aufgaben, ${data.data.resources} Ressourcen`)
    }
  } catch {
    app.showError('Moodle-Sync fehlgeschlagen')
  } finally {
    moodleSyncing.value = false
  }
}
</script>

<template>
  <div>
    <v-toolbar flat color="transparent">
      <v-toolbar-title>Einstellungen</v-toolbar-title>
    </v-toolbar>

    <div v-if="loading" class="d-flex justify-center pa-12">
      <v-progress-circular indeterminate color="primary" />
    </div>

    <v-container v-else fluid class="pa-6">
      <v-row>
        <v-col cols="12" lg="8">

          <!-- API Keys -->
          <v-card elevation="1" rounded="lg" class="pa-6 mb-6">
            <div class="d-flex align-center mb-4">
              <v-icon class="mr-2" color="primary">mdi-key-variant</v-icon>
              <div class="text-h6">API-Schlüssel</div>
            </div>

            <v-btn-toggle
              v-model="aiProvider"
              mandatory
              color="primary"
              class="mb-6"
              density="comfortable"
              rounded="lg"
            >
              <v-btn value="direct" variant="outlined">
                <v-icon start>mdi-key</v-icon>
                Direkte API-Keys
              </v-btn>
              <v-btn value="github" variant="outlined">
                <v-icon start>mdi-github</v-icon>
                GitHub Models
              </v-btn>
            </v-btn-toggle>

            <v-alert
              v-if="aiProvider === 'github'"
              type="info"
              variant="tonal"
              class="mb-4"
              density="compact"
            >
              GitHub Models ermöglicht Zugriff auf OpenAI, Anthropic und Google-Modelle über einen einzigen GitHub-Token.
              Benötigt ein GitHub Pro/Copilot-Abo.
            </v-alert>

            <template v-if="aiProvider === 'github'">
              <v-text-field
                v-model="githubToken"
                label="GitHub Personal Access Token"
                variant="outlined"
                :type="showGithub ? 'text' : 'password'"
                :append-inner-icon="showGithub ? 'mdi-eye-off' : 'mdi-eye'"
                @click:append-inner="showGithub = !showGithub"
                :placeholder="githubTokenSet ? '••••••••  (gespeichert)' : 'ghp_...'"
                :hint="githubTokenSet ? 'Key ist gesetzt. Leer lassen um beizubehalten.' : ''"
                persistent-hint
                class="mb-2"
              />
            </template>

            <template v-else>
              <v-text-field
                v-model="openaiKey"
                label="OpenAI API Key"
                variant="outlined"
                :type="showOpenai ? 'text' : 'password'"
                :append-inner-icon="showOpenai ? 'mdi-eye-off' : 'mdi-eye'"
                @click:append-inner="showOpenai = !showOpenai"
                :placeholder="openaiKeySet ? '••••••••  (gespeichert)' : 'sk-...'"
                :hint="openaiKeySet ? 'Key ist gesetzt. Leer lassen um beizubehalten.' : ''"
                persistent-hint
                class="mb-2"
              >
                <template #prepend-inner>
                  <v-icon color="#10A37F" size="small">mdi-brain</v-icon>
                </template>
              </v-text-field>

              <v-text-field
                v-model="anthropicKey"
                label="Anthropic API Key"
                variant="outlined"
                :type="showAnthropic ? 'text' : 'password'"
                :append-inner-icon="showAnthropic ? 'mdi-eye-off' : 'mdi-eye'"
                @click:append-inner="showAnthropic = !showAnthropic"
                :placeholder="anthropicKeySet ? '••••••••  (gespeichert)' : 'sk-ant-...'"
                :hint="anthropicKeySet ? 'Key ist gesetzt. Leer lassen um beizubehalten.' : ''"
                persistent-hint
                class="mb-2"
              >
                <template #prepend-inner>
                  <v-icon color="#D4A06A" size="small">mdi-robot</v-icon>
                </template>
              </v-text-field>

              <v-text-field
                v-model="geminiKey"
                label="Google Gemini API Key"
                variant="outlined"
                :type="showGemini ? 'text' : 'password'"
                :append-inner-icon="showGemini ? 'mdi-eye-off' : 'mdi-eye'"
                @click:append-inner="showGemini = !showGemini"
                :placeholder="geminiKeySet ? '••••••••  (gespeichert)' : 'AI...'"
                :hint="geminiKeySet ? 'Key ist gesetzt. Leer lassen um beizubehalten.' : ''"
                persistent-hint
                class="mb-2"
              >
                <template #prepend-inner>
                  <v-icon color="#4285F4" size="small">mdi-eye</v-icon>
                </template>
              </v-text-field>
            </template>

            <div class="d-flex justify-end mt-4">
              <v-btn color="primary" :loading="saving" @click="saveApiKeys">
                <v-icon start>mdi-content-save</v-icon>
                Speichern
              </v-btn>
            </div>
          </v-card>

          <!-- Model Selection -->
          <v-card elevation="1" rounded="lg" class="pa-6 mb-6">
            <div class="d-flex align-center mb-4">
              <v-icon class="mr-2" color="primary">mdi-tune-variant</v-icon>
              <div class="text-h6">Modell-Auswahl</div>
            </div>

            <v-row>
              <v-col cols="12" sm="6">
                <v-select
                  v-model="openaiModel"
                  :items="openaiModels"
                  item-title="name"
                  item-value="id"
                  label="OpenAI Chat-Modell"
                  variant="outlined"
                >
                  <template #prepend-inner>
                    <v-icon color="#10A37F" size="small">mdi-brain</v-icon>
                  </template>
                  <template #item="{ item, props: itemProps }">
                    <v-list-item v-bind="itemProps" :subtitle="item.raw.description" />
                  </template>
                </v-select>
              </v-col>
              <v-col cols="12" sm="6">
                <v-select
                  v-model="anthropicModel"
                  :items="anthropicModels"
                  item-title="name"
                  item-value="id"
                  label="Anthropic-Modell"
                  variant="outlined"
                >
                  <template #prepend-inner>
                    <v-icon color="#D4A06A" size="small">mdi-robot</v-icon>
                  </template>
                  <template #item="{ item, props: itemProps }">
                    <v-list-item v-bind="itemProps" :subtitle="item.raw.description" />
                  </template>
                </v-select>
              </v-col>
              <v-col cols="12" sm="6">
                <v-select
                  v-model="geminiModel"
                  :items="geminiModels"
                  item-title="name"
                  item-value="id"
                  label="Gemini Vision-Modell"
                  variant="outlined"
                >
                  <template #prepend-inner>
                    <v-icon color="#4285F4" size="small">mdi-eye</v-icon>
                  </template>
                  <template #item="{ item, props: itemProps }">
                    <v-list-item v-bind="itemProps" :subtitle="item.raw.description" />
                  </template>
                </v-select>
              </v-col>
              <v-col cols="12" sm="6">
                <v-select
                  v-model="embeddingModel"
                  :items="embeddingModels"
                  item-title="name"
                  item-value="id"
                  label="Embedding-Modell"
                  variant="outlined"
                >
                  <template #prepend-inner>
                    <v-icon color="#10A37F" size="small">mdi-vector-combine</v-icon>
                  </template>
                  <template #item="{ item, props: itemProps }">
                    <v-list-item v-bind="itemProps" :subtitle="item.raw.description" />
                  </template>
                </v-select>
              </v-col>
            </v-row>

            <div class="d-flex justify-end">
              <v-btn color="primary" :loading="saving" @click="saveModels">
                <v-icon start>mdi-content-save</v-icon>
                Speichern
              </v-btn>
            </div>
          </v-card>

          <!-- Moodle Integration -->
          <v-card elevation="1" rounded="lg" class="pa-6 mb-6">
            <div class="d-flex align-center mb-4">
              <v-icon class="mr-2" color="#F98012">mdi-school</v-icon>
              <div class="text-h6">Moodle-Integration</div>
            </div>

            <v-text-field
              v-model="moodleUrl"
              label="Moodle URL"
              variant="outlined"
              class="mb-2"
            />

            <v-text-field
              v-model="moodleToken"
              label="Moodle Token"
              variant="outlined"
              :type="showMoodleToken ? 'text' : 'password'"
              :append-inner-icon="showMoodleToken ? 'mdi-eye-off' : 'mdi-eye'"
              @click:append-inner="showMoodleToken = !showMoodleToken"
              :placeholder="moodleTokenSet ? '••••••••  (gespeichert)' : ''"
              :hint="moodleTokenSet ? 'Token ist gesetzt. Leer lassen um beizubehalten.' : ''"
              persistent-hint
              class="mb-2"
            />

            <v-alert v-if="moodleStatus" type="info" variant="tonal" class="mb-4" density="compact">
              {{ moodleStatus }}
            </v-alert>

            <div class="d-flex ga-3">
              <v-btn color="primary" :loading="saving" @click="saveMoodle">
                <v-icon start>mdi-content-save</v-icon>
                Speichern
              </v-btn>
              <v-btn variant="outlined" :loading="moodleTesting" @click="testMoodle">
                Verbindung testen
              </v-btn>
              <v-btn variant="outlined" :loading="moodleSyncing" @click="syncMoodle">
                Jetzt synchronisieren
              </v-btn>
            </div>
          </v-card>

          <!-- Email Config (EWS) -->
          <v-card elevation="1" rounded="lg" class="pa-6 mb-6">
            <div class="d-flex align-center mb-4">
              <v-icon class="mr-2" color="#EA4335">mdi-email</v-icon>
              <div class="text-h6">E-Mail (Exchange)</div>
            </div>

            <v-text-field
              v-model="emailServer"
              label="Exchange Server"
              variant="outlined"
              placeholder="webmail.dhbw-ravensburg.de"
              class="mb-2"
            />

            <v-text-field
              v-model="emailUsername"
              label="Login-Username"
              variant="outlined"
              placeholder="domab\Benutzername"
              class="mb-2"
            />

            <v-text-field
              v-model="emailPassword"
              label="Passwort"
              variant="outlined"
              :type="showEmailPw ? 'text' : 'password'"
              :append-inner-icon="showEmailPw ? 'mdi-eye-off' : 'mdi-eye'"
              @click:append-inner="showEmailPw = !showEmailPw"
              :placeholder="emailPasswordSet ? '••••••••  (gespeichert)' : ''"
              :hint="emailPasswordSet ? 'Passwort ist gesetzt. Leer lassen um beizubehalten.' : ''"
              persistent-hint
              class="mb-2"
            />

            <v-text-field
              v-model="emailAddress"
              label="E-Mail-Adresse (SMTP)"
              variant="outlined"
              placeholder="name@stud.dhbw-ravensburg.de"
              class="mb-2"
            />

            <v-alert v-if="emailStatus" type="info" variant="tonal" class="mb-4" density="compact">
              {{ emailStatus }}
            </v-alert>

            <div class="d-flex ga-3">
              <v-btn color="primary" :loading="saving" @click="saveEmail">
                <v-icon start>mdi-content-save</v-icon>
                Speichern
              </v-btn>
              <v-btn variant="outlined" :loading="emailTesting" @click="testEmail">
                Verbindung testen
              </v-btn>
            </div>
          </v-card>

          <!-- Rapla Config -->
          <v-card elevation="1" rounded="lg" class="pa-6 mb-6">
            <div class="d-flex align-center mb-4">
              <v-icon class="mr-2" color="#00897B">mdi-calendar-sync</v-icon>
              <div class="text-h6">Rapla Stundenplan</div>
            </div>

            <v-text-field
              v-model="raplaUrl"
              label="Rapla Kalender-URL"
              variant="outlined"
              placeholder="https://rapla-ravensburg.dhbw.de/rapla?page=calendar&user=..."
              class="mb-2"
            />

            <div class="d-flex justify-end">
              <v-btn color="primary" :loading="saving" @click="saveRapla">
                <v-icon start>mdi-content-save</v-icon>
                Speichern
              </v-btn>
            </div>
          </v-card>

        </v-col>

        <!-- Sidebar: Token Usage -->
        <v-col cols="12" lg="4">
          <v-card elevation="1" rounded="lg" class="pa-6 sticky-card">
            <div class="d-flex align-center mb-4">
              <v-icon class="mr-2" color="primary">mdi-chart-bar</v-icon>
              <div class="text-h6">Token-Verbrauch</div>
            </div>

            <template v-if="usage && usage.total_calls > 0">
              <!-- Summary Cards -->
              <div class="usage-grid mb-4">
                <div class="usage-stat">
                  <div class="usage-value">{{ formatNumber(usage.total_input + usage.total_output) }}</div>
                  <div class="usage-label">Tokens gesamt</div>
                </div>
                <div class="usage-stat">
                  <div class="usage-value">{{ usage.total_calls }}</div>
                  <div class="usage-label">API-Aufrufe</div>
                </div>
              </div>

              <v-divider class="mb-4" />

              <!-- Per Provider Breakdown -->
              <div
                v-for="item in usage.by_provider"
                :key="item.provider + item.model"
                class="usage-row mb-3"
              >
                <div class="d-flex align-center mb-1">
                  <v-icon :color="providerColor(item.provider)" size="small" class="mr-2">
                    {{ providerIcon(item.provider) }}
                  </v-icon>
                  <span class="text-body-2 font-weight-medium">{{ item.model }}</span>
                  <v-spacer />
                  <v-chip size="x-small" variant="tonal">{{ item.total_calls }}x</v-chip>
                </div>
                <div class="d-flex ga-4 text-caption text-medium-emphasis ml-7">
                  <span>In: {{ formatNumber(item.total_input) }}</span>
                  <span>Out: {{ formatNumber(item.total_output) }}</span>
                </div>
              </div>
            </template>

            <div v-else class="text-center text-medium-emphasis pa-6">
              <v-icon size="48" color="grey-lighten-1" class="mb-2">mdi-chart-bar</v-icon>
              <div class="text-body-2">Noch keine API-Aufrufe</div>
              <div class="text-caption">Nutzungsstatistiken erscheinen hier nach dem ersten AI-Aufruf.</div>
            </div>
          </v-card>

          <!-- Status Card -->
          <v-card elevation="1" rounded="lg" class="pa-6 mt-4">
            <div class="d-flex align-center mb-3">
              <v-icon class="mr-2" color="primary">mdi-information</v-icon>
              <div class="text-h6">Status</div>
            </div>
            <v-list density="compact" class="pa-0">
              <v-list-item>
                <template #prepend>
                  <v-icon :color="openaiKeySet ? 'success' : 'grey'" size="small">
                    {{ openaiKeySet ? 'mdi-check-circle' : 'mdi-circle-outline' }}
                  </v-icon>
                </template>
                <v-list-item-title class="text-body-2">OpenAI</v-list-item-title>
              </v-list-item>
              <v-list-item>
                <template #prepend>
                  <v-icon :color="anthropicKeySet ? 'success' : 'grey'" size="small">
                    {{ anthropicKeySet ? 'mdi-check-circle' : 'mdi-circle-outline' }}
                  </v-icon>
                </template>
                <v-list-item-title class="text-body-2">Anthropic</v-list-item-title>
              </v-list-item>
              <v-list-item>
                <template #prepend>
                  <v-icon :color="geminiKeySet ? 'success' : 'grey'" size="small">
                    {{ geminiKeySet ? 'mdi-check-circle' : 'mdi-circle-outline' }}
                  </v-icon>
                </template>
                <v-list-item-title class="text-body-2">Google Gemini</v-list-item-title>
              </v-list-item>
              <v-list-item>
                <template #prepend>
                  <v-icon :color="moodleTokenSet ? 'success' : 'grey'" size="small">
                    {{ moodleTokenSet ? 'mdi-check-circle' : 'mdi-circle-outline' }}
                  </v-icon>
                </template>
                <v-list-item-title class="text-body-2">Moodle</v-list-item-title>
              </v-list-item>
              <v-list-item>
                <template #prepend>
                  <v-icon :color="emailPasswordSet ? 'success' : 'grey'" size="small">
                    {{ emailPasswordSet ? 'mdi-check-circle' : 'mdi-circle-outline' }}
                  </v-icon>
                </template>
                <v-list-item-title class="text-body-2">E-Mail</v-list-item-title>
              </v-list-item>
            </v-list>
          </v-card>
        </v-col>
      </v-row>
    </v-container>
  </div>
</template>

<style scoped>
.sticky-card {
  position: sticky;
  top: 80px;
}

.usage-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
}

.usage-stat {
  text-align: center;
  padding: 12px;
  border-radius: 8px;
  background: #f5f5f5;
}

.usage-value {
  font-size: 24px;
  font-weight: 700;
  color: #1565C0;
}

.usage-label {
  font-size: 12px;
  color: #757575;
  margin-top: 2px;
}

.usage-row {
  padding: 8px 0;
  border-bottom: 1px solid #f0f0f0;
}

.usage-row:last-child {
  border-bottom: none;
}
</style>
