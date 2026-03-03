<script setup lang="ts">
import type { Document } from '@/types/documents'

defineProps<{
  documents: Document[]
}>()

const emit = defineEmits<{
  delete: [id: number]
}>()

function statusColor(status: string): string {
  const map: Record<string, string> = {
    done: 'success', processing: 'info', pending: 'warning', error: 'error',
  }
  return map[status] ?? 'default'
}

function categoryIcon(cat: string): string {
  const map: Record<string, string> = {
    slides_export: 'mdi-presentation', textbook: 'mdi-book-open-variant',
    exercise_sheet: 'mdi-clipboard-text', paper: 'mdi-file-document',
  }
  return map[cat] ?? 'mdi-file'
}

function formatSize(bytes: number): string {
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`
}

function formatDate(d: string): string {
  return new Date(d).toLocaleDateString('de-DE')
}
</script>

<template>
  <v-list>
    <v-list-item
      v-for="doc in documents"
      :key="doc.id"
      :title="doc.title"
      :subtitle="`${doc.filetype.toUpperCase()} - ${formatSize(doc.filesize)} - ${formatDate(doc.created_at)}`"
    >
      <template #prepend>
        <v-icon :color="statusColor(doc.processing_status)">
          {{ categoryIcon(doc.doc_category) }}
        </v-icon>
      </template>

      <template #append>
        <v-chip size="small" :color="statusColor(doc.processing_status)" class="mr-2">
          {{ doc.processing_status }}
        </v-chip>
        <v-btn
          icon="mdi-delete"
          variant="text"
          size="small"
          color="error"
          @click="emit('delete', doc.id)"
        />
      </template>
    </v-list-item>
  </v-list>
</template>
