// Email Types für TypeScript
export interface EmailResponse {
  id: number
  messageId: string
  subject: string
  fromAddress: string
  fromName: string
  toAddresses: string
  bodyText: string
  bodyHtml?: string
  receivedAt: string
  isRead: boolean
  isImportant: boolean
  hasAttachments: boolean
  folder: string
  
  // KI-Analyse
  isProcessed: boolean
  summary?: string
  category?: string
  isAppointment: boolean
  requiresUserAction: boolean
  suggestedAction?: string
  priority: number
  extractedData?: string
  
  // Status
  actionStatus: string
  relatedCalendarEventId?: number
  
  // Anhänge
  attachments: EmailAttachmentResponse[]
}

export interface EmailAttachmentResponse {
  id: number
  fileName: string
  contentType: string
  fileSize: number
  isInline: boolean
  relatedDocumentId?: number
}

export interface EmailSummaryResponse {
  totalUnread: number
  pendingActions: number
  appointmentsToday: number
  recentEmails: EmailResponse[]
}

export interface EmailActionRequest {
  emailId: number
  action: 'accept' | 'decline' | 'snooze' | 'archive' | 'delete' | 'mark_read'
  snoozeUntil?: string
  userNote?: string
  createCalendarEvent?: boolean
}
