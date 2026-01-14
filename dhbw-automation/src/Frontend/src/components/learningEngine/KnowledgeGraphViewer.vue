<template>
  <v-card>
    <v-card-title class="d-flex align-center">
      <v-icon start>mdi-graph</v-icon>
      Wissensgraph
      <v-spacer />
      <v-btn
        icon="mdi-refresh"
        variant="text"
        size="small"
        :loading="loading"
        @click="$emit('refresh')"
      />
    </v-card-title>

    <v-card-text>
      <!-- Filters -->
      <v-row dense class="mb-4">
        <v-col cols="12" sm="4">
          <v-select
            v-model="selectedEntityType"
            :items="entityTypeItems"
            label="Entitätstyp"
            density="compact"
            variant="outlined"
            clearable
            @update:model-value="$emit('filter', { entityType: selectedEntityType, subject: selectedSubject })"
          />
        </v-col>
        <v-col cols="12" sm="4">
          <v-text-field
            v-model="selectedSubject"
            label="Fach"
            density="compact"
            variant="outlined"
            clearable
            @update:model-value="$emit('filter', { entityType: selectedEntityType, subject: selectedSubject })"
          />
        </v-col>
        <v-col cols="12" sm="4">
          <v-text-field
            v-model="searchQuery"
            label="Suchen..."
            density="compact"
            variant="outlined"
            prepend-inner-icon="mdi-magnify"
            clearable
            @keyup.enter="$emit('search', searchQuery)"
          />
        </v-col>
      </v-row>

      <!-- Stats -->
      <v-row v-if="stats" dense class="mb-4">
        <v-col cols="6" sm="3">
          <v-card variant="tonal" color="primary" class="pa-3 text-center">
            <div class="text-h5">{{ stats.totalEntities }}</div>
            <div class="text-caption">Entitäten</div>
          </v-card>
        </v-col>
        <v-col cols="6" sm="3">
          <v-card variant="tonal" color="secondary" class="pa-3 text-center">
            <div class="text-h5">{{ stats.totalRelationships }}</div>
            <div class="text-caption">Beziehungen</div>
          </v-card>
        </v-col>
        <v-col cols="6" sm="3">
          <v-card variant="tonal" color="info" class="pa-3 text-center">
            <div class="text-h5">{{ stats.documentsCovered }}</div>
            <div class="text-caption">Dokumente</div>
          </v-card>
        </v-col>
        <v-col cols="6" sm="3">
          <v-card variant="tonal" color="success" class="pa-3 text-center">
            <div class="text-h5">{{ stats.chunksCovered }}</div>
            <div class="text-caption">Chunks</div>
          </v-card>
        </v-col>
      </v-row>

      <!-- Graph Container -->
      <div v-if="loading" class="text-center py-8">
        <v-progress-circular indeterminate color="primary" />
        <p class="mt-4">Lade Wissensgraph...</p>
      </div>

      <div v-else-if="!hasData" class="text-center py-8">
        <v-icon size="64" color="grey">mdi-graph-outline</v-icon>
        <p class="mt-4 text-body-1 text-medium-emphasis">
          Noch keine Wissensgraph-Daten vorhanden.
          <br />
          Verarbeiten Sie Dokumente, um den Wissensgraphen aufzubauen.
        </p>
      </div>

      <div v-else ref="graphContainer" class="graph-container" />

      <!-- Selected Entity Details -->
      <v-expand-transition>
        <v-card v-if="selectedEntity" variant="outlined" class="mt-4">
          <v-card-title class="d-flex align-center">
            <v-chip :color="getEntityTypeColor(selectedEntity.entityType)" size="small" class="mr-2">
              {{ getEntityTypeLabel(selectedEntity.entityType) }}
            </v-chip>
            {{ selectedEntity.name }}
            <v-spacer />
            <v-btn icon="mdi-close" variant="text" size="small" @click="selectedEntity = null" />
          </v-card-title>
          <v-card-text>
            <p v-if="selectedEntity.description" class="mb-3">{{ selectedEntity.description }}</p>

            <v-row dense>
              <v-col cols="6" sm="3">
                <div class="text-caption text-medium-emphasis">Fach</div>
                <div>{{ selectedEntity.subject || '-' }}</div>
              </v-col>
              <v-col cols="6" sm="3">
                <div class="text-caption text-medium-emphasis">Thema</div>
                <div>{{ selectedEntity.topic || '-' }}</div>
              </v-col>
              <v-col cols="6" sm="3">
                <div class="text-caption text-medium-emphasis">Konfidenz</div>
                <v-progress-linear
                  :model-value="selectedEntity.confidenceScore * 100"
                  color="primary"
                  height="8"
                  rounded
                />
              </v-col>
              <v-col cols="6" sm="3">
                <div class="text-caption text-medium-emphasis">Beherrschung</div>
                <v-progress-linear
                  :model-value="(selectedEntity.masteryScore || 0) * 100"
                  :color="getMasteryColor(selectedEntity.masteryScore || 0)"
                  height="8"
                  rounded
                />
              </v-col>
            </v-row>

            <div class="mt-4 d-flex gap-2">
              <v-btn
                size="small"
                color="primary"
                variant="tonal"
                @click="$emit('generate-questions', selectedEntity.id)"
              >
                <v-icon start>mdi-help-circle</v-icon>
                Fragen generieren
              </v-btn>
              <v-btn
                size="small"
                variant="tonal"
                @click="$emit('show-related', selectedEntity.id)"
              >
                <v-icon start>mdi-link-variant</v-icon>
                Verwandte anzeigen
              </v-btn>
            </div>
          </v-card-text>
        </v-card>
      </v-expand-transition>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
import { ref, watch, onMounted, onUnmounted, computed } from 'vue'
import { Network, type Options, type Node, type Edge } from 'vis-network'
import type { KgEntity, KgRelationship, KnowledgeGraphStats } from '@/types/learningEngine'
import { entityTypes, getMasteryColor as getMasteryColorUtil } from '@/types/learningEngine'

const props = defineProps<{
  entities: KgEntity[]
  relationships: KgRelationship[]
  stats?: KnowledgeGraphStats
  loading?: boolean
}>()

const emit = defineEmits<{
  refresh: []
  filter: [options: { entityType?: string; subject?: string }]
  search: [query: string]
  'generate-questions': [entityId: number]
  'show-related': [entityId: number]
  'entity-selected': [entity: KgEntity | null]
}>()

const graphContainer = ref<HTMLElement | null>(null)
const selectedEntityType = ref<string | undefined>()
const selectedSubject = ref<string | undefined>()
const searchQuery = ref('')
const selectedEntity = ref<KgEntity | null>(null)

let network: Network | null = null

const entityTypeItems = computed(() => [
  { title: 'Alle', value: undefined },
  ...entityTypes.map(t => ({ title: t.label, value: t.value }))
])

const hasData = computed(() => props.entities.length > 0)

const getEntityTypeColor = (type: string): string => {
  const info = entityTypes.find(t => t.value === type)
  return info?.color || 'grey'
}

const getEntityTypeLabel = (type: string): string => {
  const info = entityTypes.find(t => t.value === type)
  return info?.label || type
}

const getMasteryColor = (score: number): string => {
  return getMasteryColorUtil(score)
}

const buildGraph = () => {
  if (!graphContainer.value || !hasData.value) return

  const nodes: Node[] = props.entities.map(entity => ({
    id: entity.id,
    label: entity.name,
    title: `${entity.name}\n${entity.description || ''}\nTyp: ${getEntityTypeLabel(entity.entityType)}`,
    color: {
      background: getNodeColor(entity.entityType),
      border: getBorderColor(entity.masteryScore || 0),
      highlight: {
        background: getNodeColor(entity.entityType),
        border: '#1976D2'
      }
    },
    font: { color: '#333' },
    size: 20 + (entity.importanceScore * 20),
    shape: 'dot'
  }))

  const edges: Edge[] = props.relationships.map(rel => ({
    id: rel.id,
    from: rel.sourceEntityId,
    to: rel.targetEntityId,
    label: rel.relationshipType.replace('_', ' '),
    arrows: 'to',
    color: { color: '#999', highlight: '#1976D2' },
    width: 1 + (rel.strength * 2),
    font: { size: 10, color: '#666' }
  }))

  const options: Options = {
    nodes: {
      borderWidth: 2,
      shadow: true
    },
    edges: {
      smooth: {
        enabled: true,
        type: 'continuous',
        roundness: 0.5
      }
    },
    physics: {
      stabilization: {
        enabled: true,
        iterations: 100
      },
      barnesHut: {
        gravitationalConstant: -2000,
        centralGravity: 0.3,
        springLength: 150
      }
    },
    interaction: {
      hover: true,
      tooltipDelay: 200
    }
  }

  if (network) {
    network.destroy()
  }

  network = new Network(graphContainer.value, { nodes, edges }, options)

  network.on('click', (params) => {
    if (params.nodes.length > 0) {
      const nodeId = params.nodes[0]
      const entity = props.entities.find(e => e.id === nodeId)
      selectedEntity.value = entity || null
      emit('entity-selected', entity || null)
    } else {
      selectedEntity.value = null
      emit('entity-selected', null)
    }
  })
}

const getNodeColor = (entityType: string): string => {
  const colorMap: Record<string, string> = {
    concept: '#4CAF50',
    definition: '#2196F3',
    formula: '#FF9800',
    theorem: '#9C27B0',
    method: '#607D8B',
    example: '#00BCD4',
    person: '#795548',
    algorithm: '#673AB7'
  }
  return colorMap[entityType] || '#9E9E9E'
}

const getBorderColor = (masteryScore: number): string => {
  if (masteryScore >= 0.8) return '#4CAF50'
  if (masteryScore >= 0.5) return '#FF9800'
  if (masteryScore >= 0.3) return '#FF5722'
  return '#9E9E9E'
}

watch(() => [props.entities, props.relationships], () => {
  buildGraph()
}, { deep: true })

onMounted(() => {
  if (hasData.value) {
    buildGraph()
  }
})

onUnmounted(() => {
  if (network) {
    network.destroy()
    network = null
  }
})
</script>

<style scoped>
.graph-container {
  width: 100%;
  height: 500px;
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  background: #fafafa;
}
</style>
