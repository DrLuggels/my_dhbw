import type { EmailResponse } from '@/types/email'

export function formatDate(dateString: string): string {
  const date = new Date(dateString)
  return date.toLocaleString('de-DE', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

export function formatDateTime(dateString: string): string {
  const date = new Date(dateString)
  return date.toLocaleString('de-DE', {
    weekday: 'short',
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

export function getCategoryColor(category: string): string {
  const colorMap: Record<string, string> = {
    appointment: 'purple',
    deadline: 'error',
    course: 'primary',
    event: 'success',
    general: 'grey',
    todo: 'warning'
  }
  return colorMap[category] || 'grey'
}

export function getCategoryLabel(category: string): string {
  const labelMap: Record<string, string> = {
    appointment: 'Termin',
    deadline: 'Frist',
    course: 'Kurs',
    event: 'Veranstaltung',
    general: 'Allgemein',
    todo: 'Aufgabe'
  }
  return labelMap[category] || category
}

export function sanitizeHtml(html: string): string {
  // Basic HTML sanitization - in production use a library like DOMPurify
  const temp = document.createElement('div')
  temp.textContent = html
  return temp.innerHTML
}

export function groupEmailsByDate(emails: EmailResponse[]): Record<string, EmailResponse[]> {
  return emails.reduce((groups, email) => {
    const date = new Date(email.receivedAt).toLocaleDateString('de-DE')
    if (!groups[date]) {
      groups[date] = []
    }
    groups[date].push(email)
    return groups
  }, {} as Record<string, EmailResponse[]>)
}

export function filterEmailsByCategory(emails: EmailResponse[], category: string): EmailResponse[] {
  return emails.filter(email => email.category === category)
}

export function sortEmailsByDate(emails: EmailResponse[], desc: boolean = true): EmailResponse[] {
  return [...emails].sort((a, b) => {
    const dateA = new Date(a.receivedAt).getTime()
    const dateB = new Date(b.receivedAt).getTime()
    return desc ? dateB - dateA : dateA - dateB
  })
}
