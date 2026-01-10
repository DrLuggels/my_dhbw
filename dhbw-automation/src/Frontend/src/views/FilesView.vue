<template>
  <v-container>
    <h1 class="text-h3 mb-6">Dateien</h1>
    
    <v-card class="mb-4">
      <v-card-title>Datei hochladen</v-card-title>
      <v-card-text>
        <v-file-input
          v-model="file"
          label="Datei auswählen"
          variant="outlined"
          prepend-icon="mdi-paperclip"
          accept=".pdf,.docx,.doc,.txt"
          class="mb-2"
        ></v-file-input>
        
        <v-alert type="info" variant="tonal" class="mb-2">
          <div class="text-body-2">
            <v-icon size="small" class="mr-1">mdi-robot</v-icon>
            Die KI analysiert dein Dokument automatisch und erkennt Kategorie, Fach, Themen, TODOs, Termine und mehr.
          </div>
        </v-alert>
        
        <v-alert v-if="errorMessage" type="error" class="mb-2" closable @click:close="errorMessage = ''">
          {{ errorMessage }}
        </v-alert>
        
        <v-alert v-if="successMessage" type="success" class="mb-2" closable @click:close="successMessage = ''">
          {{ successMessage }}
        </v-alert>
        
        <v-btn color="primary" @click="handleUpload" :loading="uploading">
          <v-icon left>mdi-upload</v-icon>
          Hochladen
        </v-btn>
      </v-card-text>
    </v-card>
    
    <v-card>
      <v-card-title class="d-flex justify-space-between align-center">
        <span>Meine Dateien</span>
        <div class="d-flex align-center gap-2">
          <v-switch
            v-model="showProcessed"
            label="Verarbeitete anzeigen"
            color="primary"
            hide-details
            density="compact"
          ></v-switch>
          <v-btn
            v-if="selectedDocuments.length > 0"
            color="error"
            @click="handleBulkDelete"
            size="small"
          >
            <v-icon left>mdi-delete</v-icon>
            {{ selectedDocuments.length }} löschen
          </v-btn>
        </div>
      </v-card-title>
      <v-card-text>
        <v-alert v-if="filteredDocuments.length === 0 && documents.length === 0" type="info">
          Noch keine Dateien hochgeladen
        </v-alert>

        <v-alert v-else-if="filteredDocuments.length === 0 && documents.length > 0" type="info">
          Alle Dokumente sind verarbeitet. Aktiviere "Verarbeitete anzeigen" um sie zu sehen.
        </v-alert>

        <v-list v-else>
          <v-list-item
            v-for="doc in filteredDocuments"
            :key="doc.id"
          >
            <template v-slot:prepend>
              <v-checkbox
                v-model="selectedDocuments"
                :value="doc.id"
                hide-details
                density="compact"
              ></v-checkbox>
            </template>

            <v-list-item-title>{{ doc.fileName }}</v-list-item-title>
            <v-list-item-subtitle>
              {{ doc.fileType }} • {{ formatFileSize(doc.fileSize) }}
              <v-chip
                v-if="doc.isProcessed"
                size="x-small"
                color="success"
                class="ml-2"
              >
                Verarbeitet
              </v-chip>
            </v-list-item-subtitle>

            <template v-slot:append>
              <v-btn icon size="small" variant="text" @click="handleDownload(doc)">
                <v-icon>mdi-download</v-icon>
              </v-btn>
              <v-btn icon size="small" variant="text" color="error" @click="handleDelete(doc)">
                <v-icon>mdi-delete</v-icon>
              </v-btn>
            </template>
          </v-list-item>
        </v-list>
      </v-card-text>
    </v-card>
  </v-container>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import api from '@/services/api'

const file = ref<File | null>(null)
const uploading = ref(false)
const loading = ref(false)
const documents = ref<any[]>([])
const errorMessage = ref('')
const successMessage = ref('')
const showProcessed = ref(false)
const selectedDocuments = ref<number[]>([])

const filteredDocuments = computed(() => {
  if (showProcessed.value) {
    return documents.value
  }
  return documents.value.filter(doc => !doc.isProcessed)
})

const loadFiles = async () => {
  loading.value = true
  try {
    const response = await api.get('/files')
    documents.value = response.data.data || []
  } catch (error: any) {
    console.error('Load files error:', error)
    errorMessage.value = 'Fehler beim Laden der Dateien'
  } finally {
    loading.value = false
  }
}

const handleUpload = async () => {
  if (!file.value) {
    errorMessage.value = 'Bitte wähle eine Datei aus'
    return
  }

  uploading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    // Debug logging
    console.log('=== FILE UPLOAD DEBUG ===')
    console.log('file.value:', file.value)
    console.log('file.value type:', typeof file.value)
    console.log('file.value instanceof File:', file.value instanceof File)
    console.log('File name:', file.value.name)
    console.log('File size:', file.value.size)
    console.log('File type:', file.value.type)

    // AI will detect category automatically
    const response = await api.uploadFile(file.value)

    if (response.success) {
      successMessage.value = 'Datei erfolgreich hochgeladen!'
      file.value = null
      await loadFiles()
    } else {
      errorMessage.value = response.message || 'Upload fehlgeschlagen'
    }
  } catch (error: any) {
    console.error('Upload error:', error)
    errorMessage.value = error.response?.data?.message || 'Upload fehlgeschlagen'
  } finally {
    uploading.value = false
  }
}

const handleDownload = async (doc: any) => {
  try {
    const response = await api.downloadFile(doc.id)
    const url = window.URL.createObjectURL(new Blob([response.data]))
    const link = document.createElement('a')
    link.href = url
    link.setAttribute('download', doc.fileName)
    document.body.appendChild(link)
    link.click()
    link.remove()
  } catch (error) {
    console.error('Download error:', error)
    errorMessage.value = 'Download fehlgeschlagen'
  }
}

const handleDelete = async (doc: any) => {
  if (!confirm(`Möchtest du "${doc.fileName}" wirklich löschen?`)) return

  try {
    const response = await api.deleteFile(doc.id)
    if (response.success) {
      successMessage.value = 'Datei gelöscht'
      await loadFiles()
    }
  } catch (error) {
    console.error('Delete error:', error)
    errorMessage.value = 'Löschen fehlgeschlagen'
  }
}

const handleBulkDelete = async () => {
  if (selectedDocuments.value.length === 0) return

  const count = selectedDocuments.value.length
  if (!confirm(`Möchtest du wirklich ${count} Dokument(e) löschen?`)) return

  try {
    const response = await api.bulkDeleteFiles(selectedDocuments.value)
    if (response.success) {
      const data = response.data as { successCount: number; failureCount: number }
      successMessage.value = `${data.successCount} Dokument(e) gelöscht`
      if (data.failureCount > 0) {
        errorMessage.value = `${data.failureCount} Dokument(e) konnten nicht gelöscht werden`
      }
      selectedDocuments.value = []
      await loadFiles()
    }
  } catch (error) {
    console.error('Bulk delete error:', error)
    errorMessage.value = 'Bulk-Löschen fehlgeschlagen'
  }
}

const formatFileSize = (bytes: number): string => {
  if (bytes < 1024) return bytes + ' B'
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB'
  return (bytes / (1024 * 1024)).toFixed(1) + ' MB'
}

onMounted(() => {
  loadFiles()
})
</script>
