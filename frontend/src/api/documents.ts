import type { ApiResponse } from '@/types/api'
import type { Chunk, Document, DocumentDetail } from '@/types/documents'
import { client } from './client'

export const documentsApi = {
  list: () =>
    client.get<ApiResponse<Document[]>>('/api/documents'),

  get: (id: number) =>
    client.get<ApiResponse<DocumentDetail>>(`/api/documents/${id}`),

  upload: (file: File) => {
    const form = new FormData()
    form.append('file', file)
    return client.post<ApiResponse<Document>>('/api/documents/upload', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },

  delete: (id: number) =>
    client.delete<ApiResponse<null>>(`/api/documents/${id}`),

  chunks: (id: number) =>
    client.get<ApiResponse<Chunk[]>>(`/api/documents/${id}/chunks`),

  reprocess: (id: number) =>
    client.post<ApiResponse<Document>>(`/api/documents/${id}/reprocess`),
}
