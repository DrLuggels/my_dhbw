export interface CalendarEvent {
  id: number
  title: string
  startTime: string
  endTime: string
  location: string
  subject: string
  eventType: string | null
  source: string
  description?: string
  professor?: string
  notes?: string
}
