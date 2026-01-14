<template>
  <v-dialog :model-value="modelValue" @update:model-value="$emit('update:modelValue', $event)" max-width="700">
    <v-card>
      <v-card-title>
        <v-icon class="mr-2">mdi-magnify</v-icon>
        Semantische Suche
      </v-card-title>
      <v-card-text>
        <v-text-field
          v-model="localQuery"
          label="Suchanfrage"
          placeholder="Was moechtest du finden? (z.B. 'Java Generics', 'SQL Joins')"
          variant="outlined"
          prepend-inner-icon="mdi-magnify"
          autofocus
          @keyup.enter="$emit('search')"
        />

        <v-select
          v-model="localEntityTypes"
          :items="entityTypeOptions"
          label="Inhaltstypen"
          variant="outlined"
          density="compact"
          multiple
          chips
          class="mt-2"
        />

        <div v-if="loading" class="text-center py-4">
          <v-progress-circular indeterminate color="primary" />
          <p class="mt-2">Suche im Wissensraum...</p>
        </div>

        <v-list v-else-if="results.length > 0" class="mt-4">
          <v-list-subheader>Suchergebnisse ({{ results.length }})</v-list-subheader>
          <v-list-item
            v-for="result in results"
            :key="`${result.entityType}-${result.entityId}`"
            @click="$emit('select', result)"
          >
            <template v-slot:prepend>
              <v-icon :color="getNodeColor(result.entityType)">
                {{ getNodeIcon(result.entityType) }}
              </v-icon>
            </template>

            <v-list-item-title>{{ result.title }}</v-list-item-title>
            <v-list-item-subtitle>
              {{ result.entityType }} - Relevanz: {{ formatScore(result.score) }}
            </v-list-item-subtitle>

            <template v-slot:append>
              <v-chip size="small" :color="getScoreColor(result.score)">
                {{ formatScore(result.score) }}
              </v-chip>
            </template>
          </v-list-item>
        </v-list>

        <v-alert v-else-if="searchPerformed" type="info" variant="tonal" class="mt-4">
          Keine Ergebnisse gefunden. Versuche andere Suchbegriffe.
        </v-alert>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="$emit('update:modelValue', false)">Schliessen</v-btn>
        <v-btn
          color="primary"
          :loading="loading"
          :disabled="!localQuery"
          @click="$emit('search')"
        >
          Suchen
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { SearchResult } from '@/types/knowledgeNetwork'
import { entityTypeOptions, getNodeIcon, getNodeColor, getScoreColor, formatScore } from '@/types/knowledgeNetwork'

const props = defineProps<{
  modelValue: boolean
  query: string
  entityTypes: string[]
  results: SearchResult[]
  loading: boolean
  searchPerformed: boolean
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  'update:query': [value: string]
  'update:entityTypes': [value: string[]]
  'search': []
  'select': [result: SearchResult]
}>()

const localQuery = computed({
  get: () => props.query,
  set: (value) => emit('update:query', value)
})

const localEntityTypes = computed({
  get: () => props.entityTypes,
  set: (value) => emit('update:entityTypes', value)
})
</script>
