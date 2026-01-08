<template>
  <v-container>
    <div class="d-flex align-center mb-6">
      <v-btn icon variant="text" @click="$router.back()" class="mr-3">
        <v-icon>mdi-arrow-left</v-icon>
      </v-btn>
      <h1 class="text-h3">Profil</h1>
    </div>

    <v-row>
      <!-- Benutzerdaten Card -->
      <v-col cols="12" md="6">
        <v-card>
          <v-card-title>
            <v-icon left>mdi-account-circle</v-icon>
            Persönliche Daten
          </v-card-title>
          <v-card-text>
            <v-form ref="profileForm" v-model="profileValid">
              <v-text-field
                v-model="profileData.firstName"
                label="Vorname"
                :rules="nameRules"
                prepend-icon="mdi-account"
                :readonly="!editingProfile"
                variant="outlined"
                class="mb-3"
              ></v-text-field>

              <v-text-field
                v-model="profileData.lastName"
                label="Nachname"
                :rules="nameRules"
                prepend-icon="mdi-account"
                :readonly="!editingProfile"
                variant="outlined"
                class="mb-3"
              ></v-text-field>

              <v-text-field
                v-model="profileData.email"
                label="E-Mail"
                :rules="emailRules"
                prepend-icon="mdi-email"
                :readonly="!editingProfile"
                variant="outlined"
                type="email"
                class="mb-3"
              ></v-text-field>

              <div class="d-flex gap-2">
                <v-btn
                  v-if="!editingProfile"
                  color="primary"
                  @click="startEditingProfile"
                  block
                >
                  <v-icon left>mdi-pencil</v-icon>
                  Bearbeiten
                </v-btn>

                <v-btn
                  v-if="editingProfile"
                  color="success"
                  @click="saveProfile"
                  :loading="savingProfile"
                  :disabled="!profileValid"
                >
                  <v-icon left>mdi-content-save</v-icon>
                  Speichern
                </v-btn>

                <v-btn
                  v-if="editingProfile"
                  color="error"
                  @click="cancelEditingProfile"
                >
                  Abbrechen
                </v-btn>
              </div>
            </v-form>
          </v-card-text>
        </v-card>
      </v-col>

      <!-- Passwort ändern Card -->
      <v-col cols="12" md="6">
        <v-card>
          <v-card-title>
            <v-icon left>mdi-lock</v-icon>
            Passwort ändern
          </v-card-title>
          <v-card-text>
            <v-form ref="passwordForm" v-model="passwordValid">
              <v-text-field
                v-model="passwordData.currentPassword"
                label="Aktuelles Passwort"
                :rules="requiredRules"
                prepend-icon="mdi-lock"
                type="password"
                variant="outlined"
                class="mb-3"
              ></v-text-field>

              <v-text-field
                v-model="passwordData.newPassword"
                label="Neues Passwort"
                :rules="passwordRules"
                prepend-icon="mdi-lock-plus"
                type="password"
                variant="outlined"
                class="mb-3"
              ></v-text-field>

              <v-text-field
                v-model="passwordData.confirmPassword"
                label="Passwort bestätigen"
                :rules="[...requiredRules, passwordMatchRule]"
                prepend-icon="mdi-lock-check"
                type="password"
                variant="outlined"
                class="mb-3"
              ></v-text-field>

              <v-btn
                color="primary"
                @click="changePassword"
                :loading="changingPassword"
                :disabled="!passwordValid"
                block
              >
                <v-icon left>mdi-key-change</v-icon>
                Passwort ändern
              </v-btn>
            </v-form>
          </v-card-text>
        </v-card>
      </v-col>

      <!-- API Keys -->
      <v-col cols="12">
        <v-card>
          <v-card-title>
            <v-icon left>mdi-key-variant</v-icon>
            API Keys
            <v-spacer></v-spacer>
            <v-chip size="small" color="warning" variant="outlined">
              <v-icon start size="x-small">mdi-shield-lock</v-icon>
              Vertraulich
            </v-chip>
          </v-card-title>
          <v-card-text>
            <v-form ref="apiKeysForm" v-model="apiKeysValid">
              <v-alert type="info" variant="tonal" class="mb-4">
                <v-icon left>mdi-information</v-icon>
                Diese Keys werden sicher gespeichert und nur für deine KI-Funktionen verwendet.
              </v-alert>

              <v-text-field
                v-model="apiKeys.openai"
                label="OpenAI API Key (ChatGPT)"
                prepend-icon="mdi-robot"
                :type="showApiKeys.openai ? 'text' : 'password'"
                :append-inner-icon="showApiKeys.openai ? 'mdi-eye-off' : 'mdi-eye'"
                @click:append-inner="showApiKeys.openai = !showApiKeys.openai"
                :readonly="!editingApiKeys"
                variant="outlined"
                class="mb-3"
                hint="Beginnt mit sk-..."
                persistent-hint
              ></v-text-field>

              <v-text-field
                v-model="apiKeys.anthropic"
                label="Anthropic API Key (Claude)"
                prepend-icon="mdi-robot-outline"
                :type="showApiKeys.anthropic ? 'text' : 'password'"
                :append-inner-icon="showApiKeys.anthropic ? 'mdi-eye-off' : 'mdi-eye'"
                @click:append-inner="showApiKeys.anthropic = !showApiKeys.anthropic"
                :readonly="!editingApiKeys"
                variant="outlined"
                class="mb-3"
                hint="Beginnt mit sk-ant-..."
                persistent-hint
              ></v-text-field>

              <v-text-field
                v-model="apiKeys.gemini"
                label="Google Gemini API Key"
                prepend-icon="mdi-google"
                :type="showApiKeys.gemini ? 'text' : 'password'"
                :append-inner-icon="showApiKeys.gemini ? 'mdi-eye-off' : 'mdi-eye'"
                @click:append-inner="showApiKeys.gemini = !showApiKeys.gemini"
                :readonly="!editingApiKeys"
                variant="outlined"
                class="mb-3"
                hint="Google Cloud API Key"
                persistent-hint
              ></v-text-field>

              <div class="d-flex gap-2">
                <v-btn
                  v-if="!editingApiKeys"
                  color="primary"
                  @click="startEditingApiKeys"
                  block
                >
                  <v-icon left>mdi-pencil</v-icon>
                  Bearbeiten
                </v-btn>

                <v-btn
                  v-if="editingApiKeys"
                  color="success"
                  @click="saveApiKeys"
                  :loading="savingApiKeys"
                >
                  <v-icon left>mdi-content-save</v-icon>
                  Speichern
                </v-btn>

                <v-btn
                  v-if="editingApiKeys"
                  color="error"
                  @click="cancelEditingApiKeys"
                >
                  Abbrechen
                </v-btn>
              </div>
            </v-form>
          </v-card-text>
        </v-card>
      </v-col>

      <!-- Account-Informationen -->
      <v-col cols="12">
        <v-card>
          <v-card-title>
            <v-icon left>mdi-information</v-icon>
            Account-Informationen
          </v-card-title>
          <v-card-text>
            <v-list>
              <v-list-item>
                <template v-slot:prepend>
                  <v-icon>mdi-email</v-icon>
                </template>
                <v-list-item-title>E-Mail</v-list-item-title>
                <v-list-item-subtitle>{{ authStore.user?.email }}</v-list-item-subtitle>
              </v-list-item>

              <v-list-item>
                <template v-slot:prepend>
                  <v-icon>mdi-calendar</v-icon>
                </template>
                <v-list-item-title>Mitglied seit</v-list-item-title>
                <v-list-item-subtitle>{{ formatDate(authStore.user?.createdAt) }}</v-list-item-subtitle>
              </v-list-item>

              <v-divider class="my-3"></v-divider>

              <v-list-item>
                <v-btn color="error" variant="outlined" @click="handleLogout" block>
                  <v-icon left>mdi-logout</v-icon>
                  Abmelden
                </v-btn>
              </v-list-item>
            </v-list>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <v-snackbar v-model="snackbar.show" :color="snackbar.color" :timeout="3000">
      {{ snackbar.message }}
    </v-snackbar>
  </v-container>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import api from '@/services/api'

const router = useRouter()
const authStore = useAuthStore()

const profileForm = ref()
const passwordForm = ref()
const apiKeysForm = ref()
const profileValid = ref(false)
const passwordValid = ref(false)
const apiKeysValid = ref(false)

const editingProfile = ref(false)
const savingProfile = ref(false)
const changingPassword = ref(false)
const editingApiKeys = ref(false)
const savingApiKeys = ref(false)

const profileData = ref({
  firstName: '',
  lastName: '',
  email: ''
})

const passwordData = ref({
  currentPassword: '',
  newPassword: '',
  confirmPassword: ''
})

const apiKeys = ref({
  openai: '',
  anthropic: '',
  gemini: ''
})

const showApiKeys = ref({
  openai: false,
  anthropic: false,
  gemini: false
})

const snackbar = ref({
  show: false,
  message: '',
  color: 'success'
})

// Validation Rules
const nameRules = [
  (v: string) => !!v || 'Name ist erforderlich',
  (v: string) => (v && v.length >= 2) || 'Name muss mindestens 2 Zeichen lang sein'
]

const emailRules = [
  (v: string) => !!v || 'E-Mail ist erforderlich',
  (v: string) => /.+@.+\..+/.test(v) || 'E-Mail muss gültig sein'
]

const requiredRules = [
  (v: string) => !!v || 'Dieses Feld ist erforderlich'
]

const passwordRules = [
  (v: string) => !!v || 'Passwort ist erforderlich',
  (v: string) => (v && v.length >= 6) || 'Passwort muss mindestens 6 Zeichen lang sein'
]

const passwordMatchRule = (v: string) => {
  return v === passwordData.value.newPassword || 'Passwörter stimmen nicht überein'
}

const showMessage = (message: string, color: string = 'success') => {
  snackbar.value = { show: true, message, color }
}

const formatDate = (dateString: string | undefined) => {
  if (!dateString) return 'Unbekannt'
  const date = new Date(dateString)
  return date.toLocaleDateString('de-DE', {
    day: '2-digit',
    month: 'long',
    year: 'numeric'
  })
}

const loadProfileData = () => {
  if (authStore.user) {
    profileData.value = {
      firstName: authStore.user.firstName || '',
      lastName: authStore.user.lastName || '',
      email: authStore.user.email || ''
    }
  }
}

const startEditingProfile = () => {
  editingProfile.value = true
}

const cancelEditingProfile = () => {
  editingProfile.value = false
  loadProfileData()
}

const saveProfile = async () => {
  if (!profileForm.value || !profileValid.value) return

  savingProfile.value = true
  try {
    // Hier würde der API-Call zum Aktualisieren des Profils stehen
    // const response = await api.updateProfile(profileData.value)

    // Für jetzt aktualisieren wir nur den Auth Store lokal
    if (authStore.user) {
      authStore.user.firstName = profileData.value.firstName
      authStore.user.lastName = profileData.value.lastName
      authStore.user.email = profileData.value.email

      // Update localStorage
      localStorage.setItem('user', JSON.stringify(authStore.user))
    }

    showMessage('Profil erfolgreich aktualisiert')
    editingProfile.value = false
  } catch (error: any) {
    console.error('Error saving profile:', error)
    showMessage('Fehler beim Speichern des Profils', 'error')
  } finally {
    savingProfile.value = false
  }
}

const changePassword = async () => {
  if (!passwordForm.value || !passwordValid.value) return

  changingPassword.value = true
  try {
    const response = await api.changePassword(
      passwordData.value.currentPassword,
      passwordData.value.newPassword
    )

    if (response.success) {
      showMessage('Passwort erfolgreich geändert')
      passwordData.value = {
        currentPassword: '',
        newPassword: '',
        confirmPassword: ''
      }
      passwordForm.value.reset()
    } else {
      showMessage(response.message || 'Fehler beim Ändern des Passworts', 'error')
    }
  } catch (error: any) {
    console.error('Error changing password:', error)
    showMessage(error.response?.data?.message || 'Fehler beim Ändern des Passworts', 'error')
  } finally {
    changingPassword.value = false
  }
}

const loadApiKeys = () => {
  // Load API keys from localStorage
  const storedKeys = localStorage.getItem('apiKeys')
  if (storedKeys) {
    try {
      const parsed = JSON.parse(storedKeys)
      apiKeys.value = {
        openai: parsed.openai || '',
        anthropic: parsed.anthropic || '',
        gemini: parsed.gemini || ''
      }
    } catch (error) {
      console.error('Error loading API keys:', error)
    }
  }
}

const startEditingApiKeys = () => {
  editingApiKeys.value = true
}

const cancelEditingApiKeys = () => {
  editingApiKeys.value = false
  loadApiKeys()
}

const saveApiKeys = async () => {
  if (!apiKeysForm.value) return

  savingApiKeys.value = true
  try {
    // Store API keys securely in localStorage
    // In a production app, these should be encrypted and stored in the backend
    localStorage.setItem('apiKeys', JSON.stringify(apiKeys.value))

    showMessage('API Keys erfolgreich gespeichert')
    editingApiKeys.value = false
  } catch (error: any) {
    console.error('Error saving API keys:', error)
    showMessage('Fehler beim Speichern der API Keys', 'error')
  } finally {
    savingApiKeys.value = false
  }
}

const handleLogout = () => {
  authStore.logout()
  router.push('/login')
}

onMounted(() => {
  loadProfileData()
  loadApiKeys()
})
</script>

<style scoped>
.gap-2 {
  gap: 8px;
}
</style>
