import type { ApiResponse } from '@/types/api'
import { client } from './client'

interface CalendarEvent {
  id: number
  title: string
  description: string | null
  start_time: string
  end_time: string | null
  all_day: boolean
  event_type: string
  source: string
  subject: string | null
  location: string | null
}

export const calendarApi = {
  events: (start?: string, end?: string, source?: string) =>
    client.get<ApiResponse<CalendarEvent[]>>('/api/calendar/events', {
      params: { start, end, source },
    }),

  create: (event: Omit<CalendarEvent, 'id' | 'source'>) =>
    client.post<ApiResponse<CalendarEvent>>('/api/calendar/events', event),

  delete: (id: number) =>
    client.delete<ApiResponse<null>>(`/api/calendar/events/${id}`),

  syncRapla: () =>
    client.post<ApiResponse<{ events_synced: number }>>('/api/calendar/sync-rapla'),
}
