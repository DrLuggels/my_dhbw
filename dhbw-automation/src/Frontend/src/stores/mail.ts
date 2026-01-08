import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import api from '@/services/api'
import type { 
  EmailResponse, 
  EmailSummaryResponse, 
  EmailActionRequest 
} from '@/types/email'

export const useMailStore = defineStore('mail', () => {
  // State
  const emails = ref<EmailResponse[]>([])
  const summary = ref<EmailSummaryResponse | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)
  const lastSync = ref<Date | null>(null)

  // Computed
  const unreadCount = computed(() => summary.value?.totalUnread ?? 0)
  const pendingActionsCount = computed(() => summary.value?.pendingActions ?? 0)
  const hasUnread = computed(() => unreadCount.value > 0)
  const hasPendingActions = computed(() => pendingActionsCount.value > 0)

  const unreadEmails = computed(() => 
    emails.value.filter(e => !e.isRead)
  )

  const pendingActionEmails = computed(() => 
    emails.value.filter(e => e.requiresUserAction && e.actionStatus === 'pending')
  )

  const appointmentEmails = computed(() => 
    emails.value.filter(e => e.isAppointment)
  )

  // Actions
  async function fetchSummary() {
    try {
      loading.value = true
      error.value = null
      
      const response = await api.get('/api/mail/summary')
      summary.value = response.data
      
      return response.data
    } catch (err: any) {
      error.value = err.message || 'Fehler beim Laden der E-Mail-Zusammenfassung'
      console.error('Error fetching mail summary:', err)
      throw err
    } finally {
      loading.value = false
    }
  }

  async function fetchEmails(options: {
    folder?: string
    isRead?: boolean
    requiresAction?: boolean
    skip?: number
    take?: number
  } = {}) {
    try {
      loading.value = true
      error.value = null

      const params = new URLSearchParams()
      if (options.folder) params.append('folder', options.folder)
      if (options.isRead !== undefined) params.append('isRead', String(options.isRead))
      if (options.requiresAction !== undefined) params.append('requiresAction', String(options.requiresAction))
      if (options.skip !== undefined) params.append('skip', String(options.skip))
      if (options.take !== undefined) params.append('take', String(options.take))

      const response = await api.get(`/api/mail/inbox?${params.toString()}`)
      emails.value = response.data
      
      return response.data
    } catch (err: any) {
      error.value = err.message || 'Fehler beim Laden der E-Mails'
      console.error('Error fetching emails:', err)
      throw err
    } finally {
      loading.value = false
    }
  }

  async function fetchEmailById(id: number) {
    try {
      loading.value = true
      error.value = null

      const response = await api.get(`/api/mail/${id}`)
      
      // Update in emails array if present
      const index = emails.value.findIndex(e => e.id === id)
      if (index !== -1) {
        emails.value[index] = response.data
      }
      
      return response.data
    } catch (err: any) {
      error.value = err.message || 'Fehler beim Laden der E-Mail'
      console.error('Error fetching email:', err)
      throw err
    } finally {
      loading.value = false
    }
  }

  async function syncEmails() {
    try {
      loading.value = true
      error.value = null

      const response = await api.post('/api/mail/sync')
      lastSync.value = new Date()
      
      // Refresh summary and emails after sync
      await Promise.all([
        fetchSummary(),
        fetchEmails({ requiresAction: true, take: 20 })
      ])
      
      return response.data
    } catch (err: any) {
      error.value = err.message || 'Fehler bei der E-Mail-Synchronisation'
      console.error('Error syncing emails:', err)
      throw err
    } finally {
      loading.value = false
    }
  }

  async function executeAction(emailId: number, action: EmailActionRequest) {
    try {
      loading.value = true
      error.value = null

      const response = await api.post(`/api/mail/${emailId}/action`, action)
      
      // Update email in local state
      const index = emails.value.findIndex(e => e.id === emailId)
      if (index !== -1) {
        emails.value[index] = response.data
      }

      // Refresh summary
      await fetchSummary()
      
      return response.data
    } catch (err: any) {
      error.value = err.message || 'Fehler beim Ausführen der Aktion'
      console.error('Error executing action:', err)
      throw err
    } finally {
      loading.value = false
    }
  }

  async function markAsRead(emailId: number, isRead: boolean = true) {
    try {
      const response = await api.put(`/api/mail/${emailId}/read`, { isRead })
      
      // Update local state
      const email = emails.value.find(e => e.id === emailId)
      if (email) {
        email.isRead = isRead
      }

      // Update summary
      if (summary.value) {
        summary.value.totalUnread += isRead ? -1 : 1
      }
      
      return response.data
    } catch (err: any) {
      error.value = err.message || 'Fehler beim Markieren der E-Mail'
      console.error('Error marking email as read:', err)
      throw err
    }
  }

  async function deleteEmail(emailId: number, deleteFromServer: boolean = false) {
    try {
      await api.delete(`/api/mail/${emailId}?deleteFromServer=${deleteFromServer}`)
      
      // Remove from local state
      emails.value = emails.value.filter(e => e.id !== emailId)
      
      // Refresh summary
      await fetchSummary()
    } catch (err: any) {
      error.value = err.message || 'Fehler beim Löschen der E-Mail'
      console.error('Error deleting email:', err)
      throw err
    }
  }

  // Auto-refresh summary every 2 minutes
  let refreshInterval: number | null = null

  function startAutoRefresh() {
    if (refreshInterval) return
    
    refreshInterval = window.setInterval(() => {
      fetchSummary().catch(console.error)
    }, 2 * 60 * 1000) // 2 minutes
  }

  function stopAutoRefresh() {
    if (refreshInterval) {
      clearInterval(refreshInterval)
      refreshInterval = null
    }
  }

  // Reset store
  function $reset() {
    emails.value = []
    summary.value = null
    loading.value = false
    error.value = null
    lastSync.value = null
    stopAutoRefresh()
  }

  return {
    // State
    emails,
    summary,
    loading,
    error,
    lastSync,
    
    // Computed
    unreadCount,
    pendingActionsCount,
    hasUnread,
    hasPendingActions,
    unreadEmails,
    pendingActionEmails,
    appointmentEmails,
    
    // Actions
    fetchSummary,
    fetchEmails,
    fetchEmailById,
    syncEmails,
    executeAction,
    markAsRead,
    deleteEmail,
    startAutoRefresh,
    stopAutoRefresh,
    $reset
  }
})
