import type { ApiResponse } from '@/types/api'
import { client } from './client'

interface MoodleCourse {
  id: number
  moodle_id: number
  shortname: string
  fullname: string
  last_synced: string | null
}

interface MoodleAssignment {
  id: number
  name: string
  due_date: string | null
  status: string
}

export const moodleApi = {
  connect: (token: string, baseUrl = 'https://moodle.dhbw-ravensburg.de') =>
    client.post<ApiResponse<{ username: string }>>('/api/moodle/connect', {
      token, base_url: baseUrl,
    }),

  sync: () =>
    client.post<ApiResponse<{ courses: number; assignments: number; resources: number }>>('/api/moodle/sync'),

  courses: () =>
    client.get<ApiResponse<MoodleCourse[]>>('/api/moodle/courses'),

  assignments: () =>
    client.get<ApiResponse<MoodleAssignment[]>>('/api/moodle/assignments'),

  download: (resourceId: number) =>
    client.post<ApiResponse<{ filepath: string }>>(`/api/moodle/resources/${resourceId}/download`),
}
