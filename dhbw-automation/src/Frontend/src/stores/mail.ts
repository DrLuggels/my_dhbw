import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { 
  EmailResponse, 
  EmailSummaryResponse, 
  EmailActionRequest 
} from '@/types/email'
import * as mailApi from './mailActions'
import { sortEmailsByDate } from './mailHelpers'

export const useMailStore = defineStore('mail', () => {
  const emails = ref<EmailResponse[]>([])
  const summary = ref<EmailSummaryResponse | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)
  const lastSync = ref<Date | null>(null)

  const unreadCount = computed(() => summary.value?.totalUnread ?? 0)
  const pendingActionsCount = computed(() => summary.value?.pendingActions ?? 0)
  const hasUnread = computed(() => unreadCount.value > 0)
  const hasPendingActions = computed(() => pendingActionsCount.value > 0)
  const unreadEmails = computed(() => emails.value.filter(e => !e.isRead))
  const pendingActionEmails = computed(() => 
    emails.value.filter(e => e.requiresUserAction && e.actionStatus === 'pending')
  )
  const appointmentEmails = computed(() => emails.value.filter(e => e.isAppointment))
  const sortedEmails = computed(() => sortEmailsByDate(emails.value))

  async function fetchSummary() {
    try {
      loading.value = true
      error.value = null
      summary.value = await mailApi.fetchEmailSummary()
      return summary.value
    } catch (err: any) {
      error.value = err.message || 'Fehler beim Laden der E-Mail-Zusammenfassung'
      throw err
    } finally {
      loading.value = false
    }
  }

  async function fetchEmails(options = {}) {
    try {
      loading.value = true
      error.value = null
      emails.value = await mailApi.fetchEmails(options)
      return emails.value
    } catch (err: any) {
      error.value = err.message || 'Fehler beim Laden der E-Mails'
      throw err
    } finally {
      loading.value = false
    }
  }

  async function fetchEmailById(id: number) {
    try {
      loading.value = true
      error.value = null
      const email = await mailApi.fetchEmailById(id)
      const index = emails.value.findIndex(e => e.id === id)
      if (index !== -1) emails.value[index] = email
      return email
    } catch (err: any) {
      error.value = err.message || 'Fehler beim Laden der E-Mail'
      throw err
    } finally {
      loading.value = false
    }
  }

  async function syncEmails() {
    try {
      loading.value = true
      error.value = null
      const result = await mailApi.syncEmails()
      lastSync.value = new Date()
      await Promise.all([fetchSummary(), fetchEmails()])
      return result
    } catch (err: any) {
      error.value = err.message || 'Fehler beim Synchronisieren'
      throw err
    } finally {
      loading.value = false
    }
  }

  async function markAsRead(id: number, isRead = true) {
    try {
      const email = await mailApi.markEmailAsRead(id, isRead)
      const index = emails.value.findIndex(e => e.id === id)
      if (index !== -1) emails.value[index] = email
      await fetchSummary()
      return email
    } catch (err: any) {
      error.value = err.message || 'Fehler beim Aktualisieren'
      throw err
    }
  }

  async function performAction(id: number, action: EmailActionRequest) {
    try {
      loading.value = true
      const result = await mailApi.performEmailAction(id, action)
      await Promise.all([fetchSummary(), fetchEmailById(id)])
      return result
    } catch (err: any) {
      error.value = err.message || 'Fehler beim Ausführen der Aktion'
      throw err
    } finally {
      loading.value = false
    }
  }

  async function deleteEmail(id: number) {
    try {
      await mailApi.deleteEmail(id)
      emails.value = emails.value.filter(e => e.id !== id)
      await fetchSummary()
    } catch (err: any) {
      error.value = err.message || 'Fehler beim Löschen'
      throw err
    }
  }

  function clearError() {
    error.value = null
  }

  function $reset() {
    emails.value = []
    summary.value = null
    loading.value = false
    error.value = null
    lastSync.value = null
  }

  return {
    emails,
    summary,
    loading,
    error,
    lastSync,
    unreadCount,
    pendingActionsCount,
    hasUnread,
    hasPendingActions,
    unreadEmails,
    pendingActionEmails,
    appointmentEmails,
    sortedEmails,
    fetchSummary,
    fetchEmails,
    fetchEmailById,
    syncEmails,
    markAsRead,
    performAction,
    deleteEmail,
    clearError,
    $reset
  }
})
