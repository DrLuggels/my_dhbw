<template>
  <v-container>
    <v-row justify="center">
      <v-col cols="12" sm="8" md="6" lg="4">
        <v-card class="pa-4">
          <v-card-title class="text-h4 text-center mb-4">Registrierung</v-card-title>

          <v-form @submit.prevent="handleRegister">
            <v-text-field
              v-model="firstName"
              label="Vorname"
              type="text"
              variant="outlined"
              required
              class="mb-3"
            ></v-text-field>

            <v-text-field
              v-model="lastName"
              label="Nachname"
              type="text"
              variant="outlined"
              required
              class="mb-3"
            ></v-text-field>

            <v-text-field
              v-model="email"
              label="E-Mail"
              type="email"
              variant="outlined"
              required
              class="mb-3"
            ></v-text-field>

            <v-text-field
              v-model="password"
              label="Passwort"
              type="password"
              variant="outlined"
              required
              class="mb-3"
            ></v-text-field>

            <v-text-field
              v-model="passwordConfirm"
              label="Passwort bestätigen"
              type="password"
              variant="outlined"
              required
              class="mb-4"
            ></v-text-field>

            <v-alert v-if="errorMessage" type="error" class="mb-3">
              {{ errorMessage }}
            </v-alert>

            <v-alert v-if="successMessage" type="success" class="mb-3">
              {{ successMessage }}
            </v-alert>

            <v-btn type="submit" color="primary" block :loading="authStore.isLoading">
              Registrieren
            </v-btn>
          </v-form>

          <v-divider class="my-4"></v-divider>

          <p class="text-center">
            Schon registriert? <a href="/login">Jetzt anmelden</a>
          </p>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const authStore = useAuthStore()

const firstName = ref('')
const lastName = ref('')
const email = ref('')
const password = ref('')
const passwordConfirm = ref('')
const errorMessage = ref('')
const successMessage = ref('')

const handleRegister = async () => {
  errorMessage.value = ''
  successMessage.value = ''

  // Validierung
  if (!firstName.value || !lastName.value || !email.value || !password.value) {
    errorMessage.value = 'Bitte füllen Sie alle Felder aus'
    return
  }

  if (password.value !== passwordConfirm.value) {
    errorMessage.value = 'Passwörter stimmen nicht überein'
    return
  }

  if (password.value.length < 8) {
    errorMessage.value = 'Passwort muss mindestens 8 Zeichen lang sein'
    return
  }

  try {
    const success = await authStore.register(
      email.value,
      password.value,
      firstName.value,
      lastName.value
    )

    if (success) {
      successMessage.value = 'Registrierung erfolgreich! Sie werden weitergeleitet...'
      setTimeout(() => {
        router.push('/dashboard')
      }, 1500)
    } else {
      errorMessage.value = authStore.error || 'Registrierung fehlgeschlagen'
    }
  } catch (error) {
    console.error('Register error:', error)
    errorMessage.value = 'Verbindung zum Server fehlgeschlagen'
  }
}
</script>
