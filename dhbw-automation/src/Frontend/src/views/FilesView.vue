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
            Die Kategorie wird automatisch von der KI erkannt. Du kannst optional eine manuelle Kategorie angeben.
          </div>
        </v-alert>
        
        <v-select
          v-model="category"
          :items="categories"
          label="Kategorie (Optional - KI erkennt automatisch)"
          variant="outlined"
          clearable
          class="mb-2"
          @update:modelValue="loadFiles"
        ></v-select>
        
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
      <v-card-title>Meine Dateien</v-card-title>
      <v-card-text>
        <v-alert v-if="documents.length === 0" type="info">
          Noch keine Dateien hochgeladen
        </v-alert>
        
        <v-list v-else>
          <v-list-item
            v-for="doc in documents"
            :key="doc.id"
            :title="doc.fileName"
            :subtitle="`${doc.fileType} • ${formatFileSize(doc.fileSize)}`"
          >
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
import { ref, onMounted } from 'vue'
import api from '@/services/api'

const file = ref<File[]>([])
const category = ref('')
const uploading = ref(false)
const loading = ref(false)
const documents = ref<any[]>([])
const errorMessage = ref('')
const successMessage = ref('')

const categories = [
  'Vorlesung',
  'Übung',
  'Projekt',
  'Prüfung',
  'Sonstiges'
]

const loadFiles = async () => {
  if (!category.value) return
  
  loading.value = true
  try {
    const response = await api.getFilesByCategory(category.value)
    documents.value = response.documents || []
  } catch (error: any) {
    console.error('Load files error:', error)
    errorMessage.value = 'Fehler beim Laden der Dateien'
  } finally {
    loading.value = false
  }
}

const handleUpload = async () => {
  if (file.value.length === 0) {
    errorMessage.value = 'Bitte wähle eine Datei aus'
    return
  }
  
  uploading.value = true
  errorMessage.value = ''
  successMessage.value = ''
  
  try {
    // Category is optional - AI will detect it automatically if not provided
    const response = await api.uploadFile(file.value[0], category.value || '')
    
    if (response.success) {
      successMessage.value = 'Datei erfolgreich hochgeladen!'
      file.value = []
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

const formatFileSize = (bytes: number): string => {
  if (bytes < 1024) return bytes + ' B'
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB'
  return (bytes / (1024 * 1024)).toFixed(1) + ' MB'
}

onMounted(() => {
  if (category.value) loadFiles()
})
</script>
