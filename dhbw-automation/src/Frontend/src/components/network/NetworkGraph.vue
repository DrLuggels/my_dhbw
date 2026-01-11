<template>
  <div class="network-graph-container">
    <div ref="networkContainer" class="network-canvas"></div>

    <!-- Legend -->
    <div class="network-legend">
      <div class="legend-title">Legende</div>
      <div
        v-for="type in entityTypes"
        :key="type.value"
        class="legend-item"
      >
        <span class="legend-dot" :style="{ backgroundColor: type.color }"></span>
        <span class="legend-label">{{ type.label }}</span>
      </div>
    </div>

    <!-- Controls -->
    <div class="network-controls">
      <v-btn icon size="small" @click="zoomIn" title="Zoom In">
        <v-icon>mdi-plus</v-icon>
      </v-btn>
      <v-btn icon size="small" @click="zoomOut" title="Zoom Out">
        <v-icon>mdi-minus</v-icon>
      </v-btn>
      <v-btn icon size="small" @click="fitNetwork" title="Fit to View">
        <v-icon>mdi-fit-to-screen</v-icon>
      </v-btn>
      <v-btn icon size="small" @click="togglePhysics" :color="physicsEnabled ? 'primary' : 'grey'" title="Toggle Physics">
        <v-icon>mdi-atom</v-icon>
      </v-btn>
    </div>

    <!-- Node Details Tooltip -->
    <v-card
      v-if="hoveredNode"
      class="node-tooltip"
      :style="{ top: tooltipPosition.y + 'px', left: tooltipPosition.x + 'px' }"
      elevation="8"
    >
      <v-card-text class="pa-2">
        <div class="font-weight-medium">{{ hoveredNode.label }}</div>
        <div class="text-caption text-grey">{{ hoveredNode.type }}</div>
        <div class="text-caption">{{ hoveredNode.linkCount }} Verknuepfungen</div>
      </v-card-text>
    </v-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch, nextTick } from 'vue'

// Props
interface GraphNode {
  id: string
  entityType: string
  entityId: number
  label: string
  type: string
  linkCount: number
}

interface GraphEdge {
  from: string
  to: string
  linkType: string
}

const props = defineProps<{
  nodes: GraphNode[]
  edges: GraphEdge[]
}>()

const emit = defineEmits<{
  (e: 'node-click', node: GraphNode): void
  (e: 'node-double-click', node: GraphNode): void
}>()

// Refs
const networkContainer = ref<HTMLElement | null>(null)
const hoveredNode = ref<GraphNode | null>(null)
const tooltipPosition = ref({ x: 0, y: 0 })
const physicsEnabled = ref(true)

// vis-network instance
let network: any = null
let DataSet: any = null
let Network: any = null

// Entity type colors
const entityTypes = [
  { value: 'Document', label: 'Dokumente', color: '#2196F3' },
  { value: 'KnowledgeItem', label: 'Wissensbasis', color: '#FF9800' },
  { value: 'JavaDocsExercise', label: 'Java-Docs', color: '#4CAF50' },
  { value: 'Image', label: 'Bilder', color: '#9C27B0' },
  { value: 'MoodleResource', label: 'Moodle', color: '#F44336' }
]

const getNodeColor = (type: string): string => {
  const found = entityTypes.find(t => t.value === type)
  return found?.color || '#757575'
}

const getLinkColor = (linkType: string): string => {
  const colors: Record<string, string> = {
    'related': '#90CAF9',
    'prerequisite': '#FFCC80',
    'extension': '#A5D6A7',
    'example': '#CE93D8',
    'derived_from': '#EF9A9A'
  }
  return colors[linkType] || '#BDBDBD'
}

// Initialize vis-network
const initNetwork = async () => {
  if (!networkContainer.value) return

  try {
    // Dynamic import of vis-network
    const vis = await import('vis-network/standalone')
    DataSet = vis.DataSet
    Network = vis.Network

    // Prepare nodes data
    const visNodes = new DataSet(
      props.nodes.map(node => ({
        id: node.id,
        label: truncateLabel(node.label),
        title: node.label,
        color: {
          background: getNodeColor(node.type),
          border: getNodeColor(node.type),
          highlight: {
            background: getNodeColor(node.type),
            border: '#000'
          }
        },
        font: {
          color: '#ffffff',
          size: 12
        },
        shape: 'dot',
        size: Math.min(15 + node.linkCount * 2, 40),
        // Store original data
        originalData: node
      }))
    )

    // Prepare edges data
    const visEdges = new DataSet(
      props.edges.map((edge, index) => ({
        id: `edge-${index}`,
        from: edge.from,
        to: edge.to,
        color: {
          color: getLinkColor(edge.linkType),
          highlight: getLinkColor(edge.linkType)
        },
        arrows: edge.linkType === 'prerequisite' ? 'to' : undefined,
        dashes: edge.linkType === 'related' ? [5, 5] : false,
        title: edge.linkType,
        width: 1.5
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
          gravitationalConstant: -50,
          centralGravity: 0.01,
          springLength: 100,
          springConstant: 0.08
        },
        stabilization: {
          enabled: true,
          iterations: 200,
          updateInterval: 25
        }
      },
      interaction: {
        hover: true,
        tooltipDelay: 200,
        hideEdgesOnDrag: true,
        navigationButtons: false,
        keyboard: true
      }
    }

    // Create network
    network = new Network(
      networkContainer.value,
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
          x: params.pointer.DOM.x + 10,
          y: params.pointer.DOM.y + 10
        }
      }
    })

    network.on('blurNode', () => {
      hoveredNode.value = null
    })

  } catch (error) {
    console.error('Error initializing vis-network:', error)
  }
}

// Helper functions
const truncateLabel = (label: string, maxLength: number = 20): string => {
  if (label.length <= maxLength) return label
  return label.substring(0, maxLength - 3) + '...'
}

// Control functions
const zoomIn = () => {
  if (network) {
    const scale = network.getScale()
    network.moveTo({ scale: scale * 1.2 })
  }
}

const zoomOut = () => {
  if (network) {
    const scale = network.getScale()
    network.moveTo({ scale: scale / 1.2 })
  }
}

const fitNetwork = () => {
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
  () => [props.nodes, props.edges],
  async () => {
    await nextTick()
    if (network) {
      network.destroy()
      network = null
    }
    await initNetwork()
  },
  { deep: true }
)

// Lifecycle
onMounted(async () => {
  await nextTick()
  await initNetwork()
})

onUnmounted(() => {
  if (network) {
    network.destroy()
    network = null
  }
})
</script>

<style scoped>
.network-graph-container {
  position: relative;
  width: 100%;
  height: 100%;
  min-height: 500px;
}

.network-canvas {
  width: 100%;
  height: 100%;
  min-height: 500px;
  border: 1px solid #e0e0e0;
  border-radius: 4px;
  background: #fafafa;
}

.network-legend {
  position: absolute;
  top: 10px;
  left: 10px;
  background: rgba(255, 255, 255, 0.95);
  border-radius: 4px;
  padding: 8px 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
  font-size: 12px;
}

.legend-title {
  font-weight: 600;
  margin-bottom: 6px;
  color: #333;
}

.legend-item {
  display: flex;
  align-items: center;
  margin: 4px 0;
}

.legend-dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  margin-right: 8px;
}

.legend-label {
  color: #666;
}

.network-controls {
  position: absolute;
  top: 10px;
  right: 10px;
  display: flex;
  flex-direction: column;
  gap: 4px;
  background: rgba(255, 255, 255, 0.95);
  border-radius: 4px;
  padding: 4px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
}

.node-tooltip {
  position: absolute;
  z-index: 1000;
  pointer-events: none;
  max-width: 200px;
}
</style>
