import api from '@/services/api'
import type { EmailActionRequest } from '@/types/email'

export async function fetchEmailSummary() {
  const response = await api.get('/mail/summary')
  return response.data
}

export async function fetchEmails(options: {
  folder?: string
  isRead?: boolean
  requiresAction?: boolean
  skip?: number
  take?: number
} = {}) {
  const params = new URLSearchParams()
  if (options.folder) params.append('folder', options.folder)
  if (options.isRead !== undefined) params.append('isRead', String(options.isRead))
  if (options.requiresAction !== undefined) params.append('requiresAction', String(options.requiresAction))
  if (options.skip !== undefined) params.append('skip', String(options.skip))
  if (options.take !== undefined) params.append('take', String(options.take))

  const response = await api.get(`/mail/inbox?${params.toString()}`)
  return response.data
}

export async function fetchEmailById(id: number) {
  const response = await api.get(`/mail/${id}`)
  return response.data
}

export async function syncEmails() {
  const response = await api.post('/mail/sync')
  return response.data
}

export async function markEmailAsRead(id: number, isRead: boolean = true) {
  const response = await api.put(`/mail/${id}/read`, { isRead })
  return response.data
}

export async function performEmailAction(id: number, action: EmailActionRequest) {
  const response = await api.post(`/mail/${id}/action`, action)
  return response.data
}

export async function deleteEmail(id: number) {
  const response = await api.delete(`/mail/${id}`)
  return response.data
}

export async function updateEmailCategory(id: number, category: string) {
  const response = await api.put(`/mail/${id}/category`, { category })
  return response.data
}
