<template>
  <v-card>
    <v-card-title class="d-flex align-center">
      <v-icon class="mr-2">mdi-web</v-icon>
      Wissensgraph
      <v-spacer />
      <v-btn-toggle :model-value="viewMode" @update:model-value="$emit('update:viewMode', $event)" density="compact" mandatory>
        <v-btn value="graph" size="small">
          <v-icon>mdi-graph</v-icon>
        </v-btn>
        <v-btn value="cluster" size="small">
          <v-icon>mdi-chart-scatter-plot</v-icon>
        </v-btn>
        <v-btn value="list" size="small">
          <v-icon>mdi-format-list-bulleted</v-icon>
        </v-btn>
      </v-btn-toggle>
      <v-btn
        icon
        variant="text"
        class="ml-2"
        @click="$emit('refresh')"
        :loading="loading"
      >
        <v-icon>mdi-refresh</v-icon>
      </v-btn>
    </v-card-title>

    <v-card-text>
      <!-- Graph View -->
      <div v-if="viewMode === 'graph'" class="network-container">
        <div v-if="loading" class="text-center py-8">
          <v-progress-circular indeterminate color="primary" />
          <p class="mt-4">Lade Wissensgraph...</p>
        </div>
        <div v-else-if="nodes.length === 0" class="text-center py-8">
          <v-icon size="64" color="grey">mdi-graph-outline</v-icon>
          <p class="mt-4 text-grey">Keine Daten im Wissensnetzwerk</p>
          <v-btn color="primary" class="mt-4" @click="$emit('generateLinks')">
            <v-icon start>mdi-auto-fix</v-icon>
            Automatische Links generieren
          </v-btn>
        </div>
        <NetworkGraph
          v-else
          :nodes="nodes"
          :edges="edges"
          @node-click="$emit('nodeClick', $event)"
          @node-double-click="$emit('nodeDoubleClick', $event)"
        />
      </div>

      <!-- Cluster View -->
      <div v-else-if="viewMode === 'cluster'">
        <ClusterVisualization
          :points="clusterPoints"
          :method="clusterMethod"
          @method-change="$emit('clusterMethodChange', $event)"
          @point-click="$emit('clusterPointClick', $event)"
        />
      </div>

      <!-- List View -->
      <div v-else>
        <v-text-field
          :model-value="listFilter"
          @update:model-value="$emit('update:listFilter', $event)"
          prepend-inner-icon="mdi-magnify"
          label="Filtern..."
          variant="outlined"
          density="compact"
          class="mb-4"
          clearable
        />

        <v-list v-if="filteredNodes.length > 0">
          <v-list-item
            v-for="node in filteredNodes"
            :key="node.id"
            @click="$emit('nodeClick', node)"
            :class="{ 'bg-primary-lighten-5': selectedNodeId === node.id }"
          >
            <template v-slot:prepend>
              <v-icon :color="getNodeColor(node.type, masteryMode, node.mastery)">
                {{ getNodeIcon(node.type) }}
              </v-icon>
            </template>

            <v-list-item-title>{{ node.label }}</v-list-item-title>
            <v-list-item-subtitle>
              {{ node.type }} - {{ node.linkCount }} Verknuepfungen
            </v-list-item-subtitle>

            <template v-slot:append>
              <v-chip size="small" variant="tonal" :color="getNodeColor(node.type, masteryMode, node.mastery)">
                {{ node.linkCount }}
              </v-chip>
            </template>
          </v-list-item>
        </v-list>

        <v-alert v-else type="info" variant="tonal">
          Keine Eintraege gefunden
        </v-alert>
      </div>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { GraphNode, GraphEdge, ClusterPoint } from '@/types/knowledgeNetwork'
import { getNodeIcon, getNodeColor } from '@/types/knowledgeNetwork'
import NetworkGraph from '@/components/network/NetworkGraph.vue'
import ClusterVisualization from '@/components/network/ClusterVisualization.vue'

const props = defineProps<{
  viewMode: 'graph' | 'cluster' | 'list'
  nodes: GraphNode[]
  edges: GraphEdge[]
  clusterPoints: ClusterPoint[]
  clusterMethod: string
  loading: boolean
  listFilter: string
  selectedNodeId: string | null
  masteryMode: boolean
}>()

defineEmits<{
  'update:viewMode': [value: 'graph' | 'cluster' | 'list']
  'update:listFilter': [value: string]
  'refresh': []
  'generateLinks': []
  'nodeClick': [node: GraphNode]
  'nodeDoubleClick': [node: GraphNode]
  'clusterMethodChange': [method: string]
  'clusterPointClick': [point: ClusterPoint]
}>()

const filteredNodes = computed(() => {
  if (!props.listFilter) return props.nodes
  const filter = props.listFilter.toLowerCase()
  return props.nodes.filter(n =>
    n.label.toLowerCase().includes(filter) ||
    n.type.toLowerCase().includes(filter)
  )
})
</script>

<style scoped>
.network-container {
  height: 500px;
  border: 1px solid #e0e0e0;
  border-radius: 4px;
  position: relative;
}
</style>
