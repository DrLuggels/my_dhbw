import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import api from '@/services/api'

interface User {
  id: number
  email: string
  firstName: string
  lastName: string
}

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(null)
  const token = ref<string | null>(null)
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  const isAuthenticated = computed(() => !!token.value)

  function loadFromStorage() {
    const storedToken = localStorage.getItem('authToken')
    const storedUser = localStorage.getItem('user')

    if (storedToken && storedUser) {
      token.value = storedToken
      user.value = JSON.parse(storedUser)
    }
  }

  async function login(email: string, password: string) {
    isLoading.value = true
    error.value = null

    try {
      const response = await api.login(email, password)

      if (response.success) {
        token.value = response.data.token
        user.value = response.data.user

        localStorage.setItem('authToken', response.data.token)
        localStorage.setItem('user', JSON.stringify(response.data.user))

        return true
      } else {
        error.value = response.message || 'Login fehlgeschlagen'
        return false
      }
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Verbindungsfehler'
      return false
    } finally {
      isLoading.value = false
    }
  }

  async function register(
    email: string,
    password: string,
    firstName: string,
    lastName: string
  ) {
    isLoading.value = true
    error.value = null

    try {
      const response = await api.register(email, password, firstName, lastName)

      if (response.success) {
        // Nach erfolgreicher Registrierung automatisch einloggen
        return await login(email, password)
      } else {
        error.value = response.message || 'Registrierung fehlgeschlagen'
        return false
      }
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Verbindungsfehler'
      return false
    } finally {
      isLoading.value = false
    }
  }

  function logout() {
    user.value = null
    token.value = null
    localStorage.removeItem('authToken')
    localStorage.removeItem('user')
  }

  // Initialisierung beim Store-Start
  loadFromStorage()

  return {
    user,
    token,
    isLoading,
    error,
    isAuthenticated,
    login,
    register,
    logout
  }
})
