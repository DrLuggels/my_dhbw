<script setup lang="ts">
import { knowledgeApi } from '@/api/knowledge'
import LoadingState from '@/components/common/LoadingState.vue'
import type { Entity } from '@/types/knowledge'
import { onMounted, ref } from 'vue'

const entities = ref<Entity[]>([])
const loading = ref(true)
const error = ref<string | null>(null)
const searchQuery = ref('')
const searchResults = ref<Entity[]>([])

onMounted(async () => {
  try {
    const { data } = await knowledgeApi.entities()
    entities.value = data.data ?? []
  } catch {
    error.value = 'Entitäten konnten nicht geladen werden'
  } finally {
    loading.value = false
  }
})

async function onSearch() {
  if (!searchQuery.value.trim()) return
  try {
    const { data } = await knowledgeApi.search(searchQuery.value)
    searchResults.value = (data.data ?? []) as unknown as Entity[]
  } catch {
    // ignore
  }
}

function masteryColor(score: number): string {
  if (score < 0.4) return 'red'
  if (score < 0.7) return 'orange'
  return 'green'
}
</script>

<template>
  <div>
    <v-toolbar flat color="transparent">
      <v-toolbar-title>Wissensgraph</v-toolbar-title>
      <v-spacer />
      <v-text-field
        v-model="searchQuery"
        variant="outlined"
        density="compact"
        placeholder="Semantische Suche..."
        prepend-inner-icon="mdi-magnify"
        hide-details
        style="max-width: 300px"
        @keyup.enter="onSearch"
      />
    </v-toolbar>

    <v-container fluid class="pa-6">
      <LoadingState :loading="loading" :error="error">
        <v-empty-state
          v-if="!entities.length"
          icon="mdi-graph-outline"
          title="Noch keine Entitäten"
          text="Lade Dokumente hoch, um den Wissensgraph aufzubauen"
        />

        <template v-else>
          <v-row>
            <v-col
              v-for="entity in entities.slice(0, 50)"
              :key="entity.id"
              cols="12"
              sm="6"
              md="4"
            >
              <v-card elevation="1" rounded="lg" class="pa-4">
                <div class="d-flex align-center mb-2">
                  <v-chip size="small" class="mr-2">{{ entity.entity_type }}</v-chip>
                  <v-chip
                    size="small"
                    :color="masteryColor(entity.mastery_score)"
                  >
                    {{ (entity.mastery_score * 100).toFixed(0) }}%
                  </v-chip>
                </div>
                <div class="text-subtitle-1 font-weight-bold">{{ entity.name }}</div>
                <div v-if="entity.description" class="text-body-2 text-medium-emphasis mt-1">
                  {{ entity.description?.slice(0, 120) }}
                </div>
                <div v-if="entity.topic" class="text-caption text-medium-emphasis mt-2">
                  {{ entity.topic }}
                </div>
              </v-card>
            </v-col>
          </v-row>
        </template>
      </LoadingState>
    </v-container>
  </div>
</template>
