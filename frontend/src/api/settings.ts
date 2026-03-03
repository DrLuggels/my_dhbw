import { client } from './client'

export const settingsApi = {
  get: () => client.get('/api/settings'),
  update: (data: Record<string, unknown>) => client.put('/api/settings', data),
  models: () => client.get('/api/settings/models'),
  usage: () => client.get('/api/settings/usage'),
}
