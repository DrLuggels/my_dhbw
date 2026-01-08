<template>
  <v-container>
    <v-row justify="center">
      <v-col cols="12" sm="8" md="6" lg="4">
        <v-card class="pa-4">
          <v-card-title class="text-h4 text-center mb-4">Login</v-card-title>
          
          <v-form @submit.prevent="handleLogin">
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
              class="mb-4"
            ></v-text-field>
            
            <v-alert v-if="errorMessage" type="error" class="mb-3">
              {{ errorMessage }}
            </v-alert>
            
            <v-btn type="submit" color="primary" block :loading="authStore.isLoading">
              Anmelden
            </v-btn>
          </v-form>
          
          <v-divider class="my-4"></v-divider>
          
          <p class="text-center">
            Noch kein Account? <a href="/register">Jetzt registrieren</a>
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

const email = ref('')
const password = ref('')
const errorMessage = ref('')

const handleLogin = async () => {
  errorMessage.value = ''
  
  try {
    const success = await authStore.login(email.value, password.value)
    
    if (success) {
      router.push('/dashboard')
    } else {
      errorMessage.value = authStore.error || 'Login fehlgeschlagen'
    }
  } catch (error) {
    console.error('Login error:', error)
    errorMessage.value = 'Verbindung zum Server fehlgeschlagen'
  }
}
</script>
