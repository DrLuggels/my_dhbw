<template>
  <div class="omni-graph-container">
    <div ref="graphContainer" class="graph-canvas"></div>

    <!-- Legend -->
    <div class="graph-legend">
      <div class="legend-title">Meisterschaft</div>
      <div class="legend-item">
        <span class="legend-dot" style="background-color: #4CAF50;"></span>
        <span class="legend-label">Gemeistert (>80%)</span>
      </div>
      <div class="legend-item">
        <span class="legend-dot" style="background-color: #FFC107;"></span>
        <span class="legend-label">Fortgeschritten (50-80%)</span>
      </div>
      <div class="legend-item">
        <span class="legend-dot" style="background-color: #FF9800;"></span>
        <span class="legend-label">Lernend (30-50%)</span>
      </div>
      <div class="legend-item">
        <span class="legend-dot" style="background-color: #F44336;"></span>
        <span class="legend-label">Neu (<30%)</span>
      </div>
      <div class="legend-divider"></div>
      <div class="legend-title">Beziehungen</div>
      <div class="legend-item">
        <span class="legend-line" style="background-color: #FF5722;"></span>
        <span class="legend-label">Voraussetzung</span>
      </div>
      <div class="legend-item">
        <span class="legend-line dashed" style="background-color: #2196F3;"></span>
        <span class="legend-label">Verwandt</span>
      </div>
    </div>

    <!-- Controls -->
    <div class="graph-controls">
      <v-btn icon size="small" @click="zoomIn" title="Zoom In">
        <v-icon>mdi-plus</v-icon>
      </v-btn>
      <v-btn icon size="small" @click="zoomOut" title="Zoom Out">
        <v-icon>mdi-minus</v-icon>
      </v-btn>
      <v-btn icon size="small" @click="fitGraph" title="Fit to View">
        <v-icon>mdi-fit-to-screen</v-icon>
      </v-btn>
      <v-btn icon size="small" @click="togglePhysics" :color="physicsEnabled ? 'primary' : 'grey'" title="Toggle Physics">
        <v-icon>mdi-atom</v-icon>
      </v-btn>
    </div>

    <!-- Node Tooltip -->
    <v-card v-if="hoveredNode" class="node-tooltip" :style="{ top: tooltipPosition.y + 'px', left: tooltipPosition.x + 'px' }" elevation="8">
      <v-card-text class="pa-2">
        <div class="font-weight-medium">{{ hoveredNode.label }}</div>
        <div class="text-caption text-grey">{{ hoveredNode.subject }} - {{ hoveredNode.topic }}</div>
        <div class="d-flex align-center mt-1">
          <v-progress-linear :model-value="hoveredNode.masteryScore * 100" :color="getMasteryColor(hoveredNode.masteryScore)" height="8" rounded style="width: 100px;" class="mr-2" />
          <span class="text-caption">{{ Math.round(hoveredNode.masteryScore * 100) }}%</span>
        </div>
      </v-card-text>
    </v-card>

    <!-- Stats Overlay -->
    <div v-if="graph.metadata" class="graph-stats">
      <div class="text-caption">
        <strong>{{ graph.metadata.totalNodes }}</strong> Knoten |
        <strong>{{ graph.metadata.totalEdges }}</strong> Kanten |
        <strong>{{ Math.round(graph.metadata.averageMastery * 100) }}%</strong> Ø Meisterschaft
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch, nextTick } from 'vue'
import type { KnowledgeGraph, GraphNode } from '@/types/omniLearning'
import { getMasteryColor } from '@/types/omniLearning'

const props = defineProps<{
  graph: KnowledgeGraph
}>()

const emit = defineEmits<{
  (e: 'node-click', node: GraphNode): void
  (e: 'node-double-click', node: GraphNode): void
}>()

// Refs
const graphContainer = ref<HTMLElement | null>(null)
const hoveredNode = ref<GraphNode | null>(null)
const tooltipPosition = ref({ x: 0, y: 0 })
const physicsEnabled = ref(true)

// vis-network instance
let network: any = null
let DataSet: any = null
let Network: any = null

// Get edge color based on relationship type
const getEdgeColor = (relationshipType: string, isPrerequisite: boolean): string => {
  if (isPrerequisite) return '#FF5722'

  const colors: Record<string, string> = {
    'related_to': '#2196F3',
    'part_of': '#4CAF50',
    'depends_on': '#FF9800',
    'extends': '#9C27B0',
    'contradicts': '#F44336',
    'is_example_of': '#00BCD4',
    'defines': '#795548',
    'explains': '#607D8B',
    'applies_to': '#E91E63',
    'similar_to': '#3F51B5',
    'leads_to': '#009688',
    'alternative_to': '#CDDC39'
  }

  return colors[relationshipType] || '#BDBDBD'
}

// Initialize vis-network
const initGraph = async () => {
  if (!graphContainer.value || !props.graph) return

  try {
    const vis = await import('vis-network/standalone')
    DataSet = vis.DataSet
    Network = vis.Network

    // Prepare nodes
    const visNodes = new DataSet(
      props.graph.nodes.map(node => ({
        id: node.id,
        label: truncateLabel(node.label),
        title: `${node.label}\n${node.subject} - ${node.topic}\nMeisterschaft: ${Math.round(node.masteryScore * 100)}%`,
        x: node.x,
        y: node.y,
        color: {
          background: node.color || getMasteryColor(node.masteryScore),
          border: node.color || getMasteryColor(node.masteryScore),
          highlight: {
            background: node.color || getMasteryColor(node.masteryScore),
            border: '#000'
          }
        },
        font: {
          color: '#ffffff',
          size: 12
        },
        shape: 'dot',
        size: node.size || 20,
        originalData: node
      }))
    )

    // Prepare edges
    const visEdges = new DataSet(
      props.graph.edges.map((edge, index) => ({
        id: `edge-${index}`,
        from: edge.source,
        to: edge.target,
        color: {
          color: getEdgeColor(edge.relationshipType, edge.isPrerequisite),
          highlight: getEdgeColor(edge.relationshipType, edge.isPrerequisite)
        },
        arrows: edge.isPrerequisite ? 'to' : undefined,
        dashes: edge.relationshipType === 'related_to' ? [5, 5] : false,
        title: edge.relationshipType,
        width: Math.max(1, edge.strength * 3),
        smooth: {
          type: 'continuous',
          roundness: 0.5
        }
      }))
    )

    // Network options
    const options = {
      nodes: {
        borderWidth: 2,
        shadow: true
      },
      edges: {
        smooth: {
          type: 'continuous',
          roundness: 0.5
        },
        shadow: false
      },
      physics: {
        enabled: physicsEnabled.value,
        solver: 'forceAtlas2Based',
        forceAtlas2Based: {
          gravitationalConstant: -80,
          centralGravity: 0.005,
          springLength: 150,
          springConstant: 0.05,
          damping: 0.4
        },
        stabilization: {
          enabled: true,
          iterations: 300,
          updateInterval: 25
        }
      },
      interaction: {
        hover: true,
        tooltipDelay: 200,
        hideEdgesOnDrag: true,
        navigationButtons: false,
        keyboard: true,
        zoomView: true
      },
      layout: {
        improvedLayout: true,
        randomSeed: 42
      }
    }

    // Create network
    network = new Network(
      graphContainer.value,
      { nodes: visNodes, edges: visEdges },
      options
    )

    // Event handlers
    network.on('click', (params: any) => {
      if (params.nodes.length > 0) {
        const nodeId = params.nodes[0]
        const nodeData = visNodes.get(nodeId)
        if (nodeData?.originalData) {
          emit('node-click', nodeData.originalData)
        }
      }
    })

    network.on('doubleClick', (params: any) => {
      if (params.nodes.length > 0) {
        const nodeId = params.nodes[0]
        const nodeData = visNodes.get(nodeId)
        if (nodeData?.originalData) {
          emit('node-double-click', nodeData.originalData)
        }
      }
    })

    network.on('hoverNode', (params: any) => {
      const nodeData = visNodes.get(params.node)
      if (nodeData?.originalData) {
        hoveredNode.value = nodeData.originalData
        tooltipPosition.value = {
          x: params.pointer.DOM.x + 15,
          y: params.pointer.DOM.y + 15
        }
      }
    })

    network.on('blurNode', () => {
      hoveredNode.value = null
    })

  } catch (error) {
    console.error('Error initializing graph:', error)
  }
}

// Helper
const truncateLabel = (label: string, maxLength: number = 20): string => {
  if (label.length <= maxLength) return label
  return label.substring(0, maxLength - 3) + '...'
}

// Control functions
const zoomIn = () => {
  if (network) {
    const scale = network.getScale()
    network.moveTo({ scale: scale * 1.3, animation: true })
  }
}

const zoomOut = () => {
  if (network) {
    const scale = network.getScale()
    network.moveTo({ scale: scale / 1.3, animation: true })
  }
}

const fitGraph = () => {
  if (network) {
    network.fit({ animation: true })
  }
}

const togglePhysics = () => {
  physicsEnabled.value = !physicsEnabled.value
  if (network) {
    network.setOptions({
      physics: { enabled: physicsEnabled.value }
    })
  }
}

// Watch for data changes
watch(
  () => props.graph,
  async () => {
    await nextTick()
    if (network) {
      network.destroy()
      network = null
    }
    await initGraph()
  },
  { deep: true }
)

// Lifecycle
onMounted(async () => {
  await nextTick()
  await initGraph()
})

onUnmounted(() => {
  if (network) {
    network.destroy()
    network = null
  }
})
</script>

<style scoped>
.omni-graph-container {
  position: relative;
  width: 100%;
  height: 100%;
  min-height: 500px;
}

.graph-canvas {
  width: 100%;
  height: 100%;
  min-height: 500px;
  border: 1px solid #e0e0e0;
  border-radius: 4px;
  background: linear-gradient(135deg, #fafafa 0%, #f0f0f0 100%);
}

.graph-legend {
  position: absolute;
  top: 10px;
  left: 10px;
  background: rgba(255, 255, 255, 0.95);
  border-radius: 8px;
  padding: 12px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.15);
  font-size: 11px;
  max-width: 180px;
}

.legend-title {
  font-weight: 600;
  margin-bottom: 8px;
  color: #333;
  font-size: 12px;
}

.legend-divider {
  height: 1px;
  background: #e0e0e0;
  margin: 10px 0;
}

.legend-item {
  display: flex;
  align-items: center;
  margin: 6px 0;
}

.legend-dot {
  width: 12px;
  height: 12px;
  border-radius: 50%;
  margin-right: 8px;
  flex-shrink: 0;
}

.legend-line {
  width: 20px;
  height: 3px;
  margin-right: 8px;
  flex-shrink: 0;
  border-radius: 2px;
}

.legend-line.dashed {
  background: repeating-linear-gradient(
    to right,
    currentColor,
    currentColor 4px,
    transparent 4px,
    transparent 8px
  );
}

.legend-label {
  color: #666;
}

.graph-controls {
  position: absolute;
  top: 10px;
  right: 10px;
  display: flex;
  flex-direction: column;
  gap: 4px;
  background: rgba(255, 255, 255, 0.95);
  border-radius: 8px;
  padding: 6px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.15);
}

.node-tooltip {
  position: absolute;
  z-index: 1000;
  pointer-events: none;
  max-width: 250px;
  border-radius: 8px;
}

.graph-stats {
  position: absolute;
  bottom: 10px;
  left: 10px;
  background: rgba(255, 255, 255, 0.95);
  border-radius: 6px;
  padding: 8px 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}
</style>
