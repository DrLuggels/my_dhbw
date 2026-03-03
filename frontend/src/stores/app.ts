import { defineStore } from 'pinia'
import { ref } from 'vue'

export const useAppStore = defineStore('app', () => {
  const snackbar = ref({ show: false, text: '', color: 'success' })
  const sidebarOpen = ref(true)

  function showSuccess(text: string) {
    snackbar.value = { show: true, text, color: 'success' }
  }

  function showError(text: string) {
    snackbar.value = { show: true, text, color: 'error' }
  }

  function toggleSidebar() {
    sidebarOpen.value = !sidebarOpen.value
  }

  return { snackbar, sidebarOpen, showSuccess, showError, toggleSidebar }
})
