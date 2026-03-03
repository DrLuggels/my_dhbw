<script setup lang="ts">
import { ref } from 'vue'

const emit = defineEmits<{
  upload: [file: File]
  cancel: []
}>()

const selectedFile = ref<File | null>(null)
const dragOver = ref(false)

function onDrop(e: DragEvent) {
  dragOver.value = false
  const file = e.dataTransfer?.files[0]
  if (file) selectedFile.value = file
}

function onFileSelect(files: File | File[]) {
  const file = Array.isArray(files) ? files[0] : files
  if (file) selectedFile.value = file
}

function submit() {
  if (selectedFile.value) emit('upload', selectedFile.value)
}

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`
}
</script>

<template>
  <v-card rounded="lg" class="pa-6">
    <div class="text-h6 mb-4">Dokument hochladen</div>

    <div
      class="upload-zone pa-8 text-center rounded-lg mb-4"
      :class="{ 'drag-over': dragOver }"
      @dragover.prevent="dragOver = true"
      @dragleave="dragOver = false"
      @drop.prevent="onDrop"
    >
      <v-icon size="48" color="primary" class="mb-2">mdi-cloud-upload</v-icon>
      <div class="text-body-1">Datei hierher ziehen</div>
      <div class="text-caption text-medium-emphasis">PDF, PPTX, DOCX, HTML</div>

      <v-file-input
        class="mt-4"
        variant="outlined"
        density="compact"
        accept=".pdf,.pptx,.docx,.html,.htm,.txt"
        label="Oder Datei auswählen"
        @update:model-value="onFileSelect"
      />
    </div>

    <v-alert v-if="selectedFile" type="info" variant="tonal" class="mb-4">
      {{ selectedFile.name }} ({{ formatSize(selectedFile.size) }})
    </v-alert>

    <div class="d-flex justify-end ga-4">
      <v-btn variant="text" @click="emit('cancel')">Abbrechen</v-btn>
      <v-btn color="primary" :disabled="!selectedFile" @click="submit">
        Hochladen
      </v-btn>
    </div>
  </v-card>
</template>

<style scoped>
.upload-zone {
  border: 2px dashed #ccc;
  transition: border-color 0.2s;
}
.upload-zone.drag-over {
  border-color: #1565C0;
  background: #E3F2FD;
}
</style>
