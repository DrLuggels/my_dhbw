<template>
  <v-card>
    <v-card-title class="d-flex align-center">
      <v-icon start>mdi-cog-transfer</v-icon>
      Dokument-Verarbeitung
    </v-card-title>

    <v-card-text>
      <p class="text-body-2 text-medium-emphasis mb-4">
        Verarbeite Dokumente für die Lern-Engine: Chunking, Embedding, Entitäts- und Beziehungsextraktion.
      </p>

      <!-- Processing Options -->
      <v-expansion-panels variant="accordion" class="mb-4">
        <v-expansion-panel title="Verarbeitungsoptionen">
          <v-expansion-panel-text>
            <v-row dense>
              <v-col cols="12" sm="6">
                <v-switch
                  v-model="options.extractEntities"
                  label="Entitäten extrahieren"
                  color="primary"
                  density="compact"
                  hide-details
                />
              </v-col>
              <v-col cols="12" sm="6">
                <v-switch
                  v-model="options.extractRelationships"
                  label="Beziehungen extrahieren"
                  color="primary"
                  density="compact"
                  hide-details
                />
              </v-col>
              <v-col cols="12" sm="6">
                <v-switch
                  v-model="options.generateEmbeddings"
                  label="Embeddings generieren"
                  color="primary"
                  density="compact"
                  hide-details
                />
              </v-col>
              <v-col cols="12" sm="6">
                <v-switch
                  v-model="options.useSemanticChunking"
                  label="Semantisches Chunking"
                  color="primary"
                  density="compact"
                  hide-details
                />
              </v-col>
            </v-row>

            <v-row dense class="mt-2">
              <v-col cols="12" sm="6">
                <v-text-field
                  v-model.number="options.targetChunkSize"
                  label="Chunk-Größe (Tokens)"
                  type="number"
                  density="compact"
                  variant="outlined"
                  :min="200"
                  :max="2000"
                />
              </v-col>
              <v-col cols="12" sm="6">
                <v-text-field
                  v-model.number="options.chunkOverlap"
                  label="Chunk-Überlappung (Tokens)"
                  type="number"
                  density="compact"
                  variant="outlined"
                  :min="0"
                  :max="500"
                />
              </v-col>
            </v-row>
          </v-expansion-panel-text>
        </v-expansion-panel>
      </v-expansion-panels>

      <!-- Document Selection -->
      <div v-if="documents.length > 0" class="mb-4">
        <div class="d-flex align-center mb-2">
          <span class="text-subtitle-2">Dokumente auswählen</span>
          <v-spacer />
          <v-btn
            size="x-small"
            variant="text"
            @click="selectAll"
          >
            Alle auswählen
          </v-btn>
          <v-btn
            size="x-small"
            variant="text"
            @click="selectNone"
          >
            Keine
          </v-btn>
        </div>

        <v-list density="compact" class="border rounded">
          <v-list-item
            v-for="doc in documents"
            :key="doc.id"
            :value="doc.id"
            @click="toggleDocument(doc.id)"
          >
            <template #prepend>
              <v-checkbox-btn
                :model-value="selectedDocuments.includes(doc.id)"
                @update:model-value="toggleDocument(doc.id)"
              />
            </template>
            <v-list-item-title>{{ doc.fileName }}</v-list-item-title>
            <v-list-item-subtitle>
              {{ doc.subject || 'Kein Fach' }} - {{ formatDate(doc.createdAt) }}
            </v-list-item-subtitle>
            <template #append>
              <v-chip
                v-if="doc.isChunked"
                size="x-small"
                color="success"
                variant="tonal"
              >
                Gechunkt
              </v-chip>
              <v-chip
                v-if="getDocumentResult(doc.id)"
                size="x-small"
                :color="getDocumentResult(doc.id)?.success ? 'success' : 'error'"
                variant="tonal"
                class="ml-1"
              >
                {{ getDocumentResult(doc.id)?.success ? 'OK' : 'Fehler' }}
              </v-chip>
            </template>
          </v-list-item>
        </v-list>
      </div>

      <div v-else class="text-center py-6 text-medium-emphasis">
        <v-icon size="48" color="grey">mdi-file-document-outline</v-icon>
        <p class="mt-2">Keine Dokumente verfügbar.</p>
      </div>

      <!-- Action Buttons -->
      <div class="d-flex gap-2 flex-wrap">
        <v-btn
          color="primary"
          :loading="processing"
          :disabled="selectedDocuments.length === 0"
          @click="processSelected"
        >
          <v-icon start>mdi-play</v-icon>
          {{ selectedDocuments.length }} Dokument(e) verarbeiten
        </v-btn>

        <v-btn
          variant="outlined"
          :disabled="processing || results.length === 0"
          @click="clearResults"
        >
          <v-icon start>mdi-broom</v-icon>
          Ergebnisse löschen
        </v-btn>
      </div>

      <!-- Processing Progress -->
      <v-expand-transition>
        <div v-if="processing" class="mt-4">
          <v-progress-linear
            :model-value="progressPercent"
            color="primary"
            height="8"
            rounded
          />
          <p class="text-caption text-center mt-2">
            Verarbeite {{ currentDocIndex + 1 }} von {{ selectedDocuments.length }}...
          </p>
        </div>
      </v-expand-transition>

      <!-- Results -->
      <v-expand-transition>
        <div v-if="results.length > 0" class="mt-4">
          <div class="text-subtitle-2 mb-2">Verarbeitungsergebnisse</div>

          <v-list density="compact" class="border rounded">
            <v-list-item
              v-for="result in results"
              :key="result.documentId"
              :class="{ 'bg-error-lighten-5': !result.success }"
            >
              <template #prepend>
                <v-icon :color="result.success ? 'success' : 'error'">
                  {{ result.success ? 'mdi-check-circle' : 'mdi-alert-circle' }}
                </v-icon>
              </template>
              <v-list-item-title>{{ result.documentName }}</v-list-item-title>
              <v-list-item-subtitle v-if="result.success">
                {{ result.chunksCreated }} Chunks, {{ result.entitiesExtracted }} Entitäten,
                {{ result.relationshipsExtracted }} Beziehungen
                <span class="text-caption">
                  ({{ formatDuration(result.processingTime) }})
                </span>
              </v-list-item-subtitle>
              <v-list-item-subtitle v-else class="text-error">
                {{ result.errorMessage }}
              </v-list-item-subtitle>

              <template v-if="result.warnings && result.warnings.length > 0" #append>
                <v-tooltip location="start">
                  <template #activator="{ props }">
                    <v-icon v-bind="props" color="warning" size="small">mdi-alert</v-icon>
                  </template>
                  <div>
                    <strong>Warnungen:</strong>
                    <ul class="pl-4 ma-0">
                      <li v-for="(warning, i) in result.warnings" :key="i">{{ warning }}</li>
                    </ul>
                  </div>
                </v-tooltip>
              </template>
            </v-list-item>
          </v-list>

          <!-- Summary -->
          <v-card variant="tonal" color="primary" class="mt-3 pa-3">
            <div class="d-flex justify-space-around text-center">
              <div>
                <div class="text-h6">{{ summaryStats.totalChunks }}</div>
                <div class="text-caption">Chunks</div>
              </div>
              <div>
                <div class="text-h6">{{ summaryStats.totalEntities }}</div>
                <div class="text-caption">Entitäten</div>
              </div>
              <div>
                <div class="text-h6">{{ summaryStats.totalRelationships }}</div>
                <div class="text-caption">Beziehungen</div>
              </div>
              <div>
                <div class="text-h6">{{ summaryStats.successRate }}%</div>
                <div class="text-caption">Erfolgsrate</div>
              </div>
            </div>
          </v-card>
        </div>
      </v-expand-transition>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import type { LearningDocumentResult, ProcessingOptions } from '@/types/learningEngine'

interface Document {
  id: number
  fileName: string
  subject?: string
  createdAt: string
  isChunked: boolean
}

const props = defineProps<{
  documents: Document[]
  processing?: boolean
  results: LearningDocumentResult[]
}>()

const emit = defineEmits<{
  process: [documentIds: number[], options: ProcessingOptions]
  'clear-results': []
}>()

const selectedDocuments = ref<number[]>([])
const currentDocIndex = ref(0)

const options = ref<ProcessingOptions>({
  extractEntities: true,
  extractRelationships: true,
  generateEmbeddings: true,
  useSemanticChunking: true,
  targetChunkSize: 500,
  chunkOverlap: 50,
  entityConfidenceThreshold: 0.7,
  relationshipStrengthThreshold: 0.5
})

const progressPercent = computed(() => {
  if (selectedDocuments.value.length === 0) return 0
  return ((currentDocIndex.value + 1) / selectedDocuments.value.length) * 100
})

const summaryStats = computed(() => {
  const successful = props.results.filter(r => r.success)
  return {
    totalChunks: successful.reduce((sum, r) => sum + r.chunksCreated, 0),
    totalEntities: successful.reduce((sum, r) => sum + r.entitiesExtracted, 0),
    totalRelationships: successful.reduce((sum, r) => sum + r.relationshipsExtracted, 0),
    successRate: props.results.length > 0
      ? Math.round((successful.length / props.results.length) * 100)
      : 0
  }
})

const getDocumentResult = (docId: number): LearningDocumentResult | undefined => {
  return props.results.find(r => r.documentId === docId)
}

const toggleDocument = (id: number) => {
  const index = selectedDocuments.value.indexOf(id)
  if (index === -1) {
    selectedDocuments.value.push(id)
  } else {
    selectedDocuments.value.splice(index, 1)
  }
}

const selectAll = () => {
  selectedDocuments.value = props.documents.map(d => d.id)
}

const selectNone = () => {
  selectedDocuments.value = []
}

const processSelected = () => {
  currentDocIndex.value = 0
  emit('process', selectedDocuments.value, options.value)
}

const clearResults = () => {
  emit('clear-results')
}

const formatDate = (dateStr: string): string => {
  return new Date(dateStr).toLocaleDateString('de-DE', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric'
  })
}

const formatDuration = (duration: string): string => {
  // Format TimeSpan string like "00:01:23.456" to readable format
  const match = duration.match(/(\d+):(\d+):(\d+)/)
  if (match) {
    const [, hours, minutes, seconds] = match
    if (parseInt(hours) > 0) return `${hours}h ${minutes}m`
    if (parseInt(minutes) > 0) return `${minutes}m ${seconds}s`
    return `${seconds}s`
  }
  return duration
}
</script>
