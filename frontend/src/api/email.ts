import { client } from './client'

export const emailApi = {
  test: () => client.post('/api/email/test'),
  inbox: (limit = 30, offset = 0) =>
    client.get('/api/email/inbox', { params: { limit, offset } }),
  get: (itemId: string) => client.get(`/api/email/${encodeURIComponent(itemId)}`),
}
