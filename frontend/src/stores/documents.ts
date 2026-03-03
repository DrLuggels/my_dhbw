import { documentsApi } from '@/api/documents'
import type { Document, DocumentDetail } from '@/types/documents'
import { defineStore } from 'pinia'
import { computed, ref } from 'vue'

export const useDocumentStore = defineStore('documents', () => {
  const documents = ref<Document[]>([])
  const current = ref<DocumentDetail | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)

  const processedDocs = computed(() =>
    documents.value.filter(d => d.processing_status === 'done'),
  )

  async function fetchAll() {
    loading.value = true
    error.value = null
    try {
      const { data } = await documentsApi.list()
      documents.value = data.data ?? []
    } catch {
      error.value = 'Fehler beim Laden der Dokumente'
    } finally {
      loading.value = false
    }
  }

  async function fetchOne(id: number) {
    loading.value = true
    error.value = null
    try {
      const { data } = await documentsApi.get(id)
      current.value = data.data
    } catch {
      error.value = 'Dokument nicht gefunden'
    } finally {
      loading.value = false
    }
  }

  async function upload(file: File) {
    loading.value = true
    error.value = null
    try {
      const { data } = await documentsApi.upload(file)
      if (data.data) documents.value.unshift(data.data)
      return data.data
    } catch {
      error.value = 'Upload fehlgeschlagen'
      return null
    } finally {
      loading.value = false
    }
  }

  async function remove(id: number) {
    try {
      await documentsApi.delete(id)
      documents.value = documents.value.filter(d => d.id !== id)
    } catch {
      error.value = 'Löschen fehlgeschlagen'
    }
  }

  return { documents, current, loading, error, processedDocs, fetchAll, fetchOne, upload, remove }
})
