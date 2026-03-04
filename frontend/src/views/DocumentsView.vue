<script setup lang="ts">
import DocumentList from '@/components/documents/DocumentList.vue'
import DocumentUpload from '@/components/documents/DocumentUpload.vue'
import MoodleSyncCard from '@/components/documents/MoodleSyncCard.vue'
import LoadingState from '@/components/common/LoadingState.vue'
import { useAppStore } from '@/stores/app'
import { useDocumentStore } from '@/stores/documents'
import { onMounted, ref } from 'vue'

const docs = useDocumentStore()
const app = useAppStore()
const showUpload = ref(false)

onMounted(() => docs.fetchAll())

async function onUpload(file: File) {
  const doc = await docs.upload(file)
  if (doc) {
    showUpload.value = false
    app.showSuccess('Dokument hochgeladen und verarbeitet')
  }
}
</script>

<template>
  <div>
    <v-toolbar flat color="transparent">
      <v-toolbar-title>Dokumente</v-toolbar-title>
      <v-spacer />
      <v-btn color="primary" prepend-icon="mdi-upload" @click="showUpload = true">
        Hochladen
      </v-btn>
    </v-toolbar>

    <v-container fluid class="pa-6">
      <MoodleSyncCard @synced="docs.fetchAll()" />

      <LoadingState :loading="docs.loading" :error="docs.error">
        <v-empty-state
          v-if="!docs.documents.length"
          icon="mdi-file-document-outline"
          title="Keine Dokumente"
          text="Lade dein erstes Dokument hoch, um zu beginnen"
        >
          <template #actions>
            <v-btn color="primary" @click="showUpload = true">Hochladen</v-btn>
          </template>
        </v-empty-state>

        <DocumentList v-else :documents="docs.documents" @delete="docs.remove" />
      </LoadingState>
    </v-container>

    <v-dialog v-model="showUpload" max-width="500">
      <DocumentUpload @upload="onUpload" @cancel="showUpload = false" />
    </v-dialog>
  </div>
</template>
