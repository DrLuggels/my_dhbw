<template>
  <v-container>
    <div class="d-flex align-center mb-6">
      <v-btn icon variant="text" @click="$router.back()" class="mr-3">
        <v-icon>mdi-arrow-left</v-icon>
      </v-btn>
      <h1 class="text-h3">Profil</h1>
    </div>

    <v-row>
      <v-col cols="12" md="6">
        <ProfileDataCard 
          :profile-data="profileData" 
          :saving="savingProfile"
          @save="saveProfile"
        />
      </v-col>

      <v-col cols="12" md="6">
        <PasswordChangeCard
          ref="passwordCardRef"
          :loading="changingPassword"
          @change="changePassword"
        />
      </v-col>

      <v-col cols="12">
        <MoodleIntegrationCard />
      </v-col>

      <v-col cols="12">
        <v-card>
          <v-card-title>
            <v-icon left>mdi-google</v-icon>
            Google Calendar Integration
          </v-card-title>
          <v-card-text>
            <GoogleCalendarConnect :user-id="authStore.user?.id || 1" />
          </v-card-text>
        </v-card>
      </v-col>

      <v-col cols="12">
        <ApiKeysCard
          :api-keys="apiKeys"
          :saving="savingApiKeys"
          @save="saveApiKeys"
        />
      </v-col>

      <v-col cols="12">
        <AccountInfoCard
          :email="authStore.user?.email"
          @logout="handleLogout"
        />
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
import GoogleCalendarConnect from '@/components/GoogleCalendarConnect.vue'
import ProfileDataCard from '@/components/profile/ProfileDataCard.vue'
import PasswordChangeCard from '@/components/profile/PasswordChangeCard.vue'
import ApiKeysCard from '@/components/profile/ApiKeysCard.vue'
import AccountInfoCard from '@/components/profile/AccountInfoCard.vue'
import MoodleIntegrationCard from '@/components/profile/MoodleIntegrationCard.vue'

const router = useRouter()
const authStore = useAuthStore()

const passwordCardRef = ref()
const savingProfile = ref(false)
const changingPassword = ref(false)
const savingApiKeys = ref(false)

const profileData = ref({
  firstName: '',
  lastName: '',
  email: ''
})

const apiKeys = ref({
  openai: '',
  anthropic: '',
  gemini: ''
})

const snackbar = ref({
  show: false,
  message: '',
  color: 'success'
})

const showMessage = (message: string, color: string = 'success') => {
  snackbar.value = { show: true, message, color }
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

const saveProfile = async (data: typeof profileData.value) => {
  savingProfile.value = true
  try {
    if (authStore.user) {
      authStore.user.firstName = data.firstName
      authStore.user.lastName = data.lastName
      authStore.user.email = data.email
      localStorage.setItem('user', JSON.stringify(authStore.user))
    }
    profileData.value = data
    showMessage('Profil erfolgreich aktualisiert')
  } catch (error: any) {
    console.error('Error saving profile:', error)
    showMessage('Fehler beim Speichern des Profils', 'error')
  } finally {
    savingProfile.value = false
  }
}

const changePassword = async (currentPassword: string, newPassword: string) => {
  changingPassword.value = true
  try {
    const response = await api.changePassword(currentPassword, newPassword)

    if (response.success) {
      showMessage('Passwort erfolgreich geändert')
      passwordCardRef.value?.reset()
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

const loadApiKeys = async () => {
  try {
    // Lade API-Keys vom Backend
    const response = await api.get('/user/api-keys')
    if (response.data.success && response.data.data) {
      const data = response.data.data
      // Zeige nur ob Keys gesetzt sind (aus Sicherheitsgründen)
      apiKeys.value = {
        openai: data.openAiKeyPreview || '',
        anthropic: data.anthropicKeyPreview || '',
        gemini: data.geminiKeyPreview || ''
      }
    }
  } catch (error) {
    console.error('Error loading API keys from backend:', error)
    // Fallback auf localStorage
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
}

const saveApiKeys = async (keys: typeof apiKeys.value) => {
  savingApiKeys.value = true
  try {
    // Sende API-Keys an Backend statt localStorage
    const response = await api.put('/user/api-keys', {
      openAiApiKey: keys.openai || null,
      anthropicApiKey: keys.anthropic || null,
      geminiApiKey: keys.gemini || null
    })
    
    if (response.data.success) {
      // Speichere nur lokal als Backup (optional)
      localStorage.setItem('apiKeys', JSON.stringify(keys))
      apiKeys.value = keys
      showMessage('API Keys erfolgreich gespeichert')
    } else {
      showMessage(response.data.message || 'Fehler beim Speichern', 'error')
    }
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
