<script setup lang="ts">
import { learningApi } from '@/api/learning'
import LoadingState from '@/components/common/LoadingState.vue'
import { useDocumentStore } from '@/stores/documents'
import { useLearningStore } from '@/stores/learning'
import { onMounted, ref } from 'vue'

const docs = useDocumentStore()
const learning = useLearningStore()
const loading = ref(true)
const error = ref<string | null>(null)
const dueCount = ref(0)

onMounted(async () => {
  try {
    await Promise.all([
      docs.fetchAll(),
      learning.fetchStats(),
      learning.fetchStreak(),
      learningApi.due().then(r => { dueCount.value = r.data.data?.length ?? 0 }),
    ])
  } catch {
    error.value = 'Dashboard konnte nicht geladen werden'
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div>
    <v-toolbar flat color="transparent">
      <v-toolbar-title>Dashboard</v-toolbar-title>
    </v-toolbar>

    <v-container fluid class="pa-6">
      <LoadingState :loading="loading" :error="error">
        <!-- Stats Cards -->
        <v-row class="mb-6">
          <v-col cols="12" sm="6" md="3">
            <v-card elevation="1" rounded="lg" class="pa-4">
              <div class="text-subtitle-2 text-medium-emphasis">Dokumente</div>
              <div class="text-h5 font-weight-bold">{{ docs.documents.length }}</div>
            </v-card>
          </v-col>
          <v-col cols="12" sm="6" md="3">
            <v-card elevation="1" rounded="lg" class="pa-4">
              <div class="text-subtitle-2 text-medium-emphasis">Mastery</div>
              <div class="text-h5 font-weight-bold">
                {{ ((learning.stats?.average_mastery ?? 0) * 100).toFixed(0) }}%
              </div>
            </v-card>
          </v-col>
          <v-col cols="12" sm="6" md="3">
            <v-card elevation="1" rounded="lg" class="pa-4">
              <div class="text-subtitle-2 text-medium-emphasis">Streak</div>
              <div class="text-h5 font-weight-bold">
                {{ learning.streak?.current_streak ?? 0 }} Tage
              </div>
            </v-card>
          </v-col>
          <v-col cols="12" sm="6" md="3">
            <v-card elevation="1" rounded="lg" class="pa-4">
              <div class="text-subtitle-2 text-medium-emphasis">Fällige Übungen</div>
              <div class="text-h5 font-weight-bold">{{ dueCount }}</div>
            </v-card>
          </v-col>
        </v-row>

        <!-- Quick Actions -->
        <v-card elevation="1" rounded="lg" class="pa-4">
          <div class="text-subtitle-1 font-weight-bold mb-4">Schnellaktionen</div>
          <div class="d-flex ga-4 flex-wrap">
            <v-btn color="primary" prepend-icon="mdi-upload" to="/documents">
              Dokument hochladen
            </v-btn>
            <v-btn color="primary" variant="outlined" prepend-icon="mdi-school" to="/learning">
              Lernen starten
            </v-btn>
            <v-btn color="primary" variant="outlined" prepend-icon="mdi-graph" to="/knowledge">
              Wissensgraph
            </v-btn>
          </div>
        </v-card>
      </LoadingState>
    </v-container>
  </div>
</template>
