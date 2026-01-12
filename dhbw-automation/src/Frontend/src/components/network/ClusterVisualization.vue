<template>
  <div class="cluster-visualization">
    <div v-if="loading" class="text-center py-8">
      <v-progress-circular indeterminate color="primary" size="64"></v-progress-circular>
      <p class="mt-4">Berechne Cluster-Visualisierung...</p>
      <p class="text-caption text-grey">Dies kann einige Sekunden dauern</p>
    </div>

    <div v-else-if="error" class="text-center py-8">
      <v-icon size="64" color="error">mdi-alert-circle</v-icon>
      <p class="mt-4 text-error">{{ error }}</p>
    </div>

    <div v-else-if="points.length === 0" class="text-center py-8">
      <v-icon size="64" color="grey">mdi-chart-scatter-plot</v-icon>
      <p class="mt-4">Keine Daten für Clustering verfügbar</p>
      <p class="text-caption text-grey">Erstelle zuerst Embeddings für deine Dokumente</p>
    </div>

    <div v-else class="cluster-container">
      <!-- Controls -->
      <div class="cluster-controls mb-4">
        <v-btn-group density="compact" variant="outlined">
          <v-btn
            :color="method === 'umap' ? 'primary' : ''"
            @click="changeMethod('umap')"
            size="small"
          >
            UMAP
          </v-btn>
          <v-btn
            :color="method === 'tsne' ? 'primary' : ''"
            @click="changeMethod('tsne')"
            size="small"
          >
            t-SNE
          </v-btn>
          <v-btn
            :color="method === 'pca' ? 'primary' : ''"
            @click="changeMethod('pca')"
            size="small"
          >
            PCA
          </v-btn>
        </v-btn-group>

        <v-spacer />

        <v-btn
          icon
          size="small"
          @click="resetZoom"
          title="Zoom zurücksetzen"
        >
          <v-icon>mdi-fit-to-screen</v-icon>
        </v-btn>
      </div>

      <!-- Chart Canvas -->
      <div ref="chartContainer" class="chart-canvas"></div>

      <!-- Legend -->
      <div class="cluster-legend mt-4">
        <div class="legend-title">Kategorien</div>
        <div
          v-for="type in entityTypes"
          :key="type.value"
          class="legend-item"
        >
          <span class="legend-dot" :style="{ backgroundColor: type.color }"></span>
          <span class="legend-label">{{ type.label }}</span>
        </div>
      </div>

      <!-- Point Details -->
      <v-card
        v-if="selectedPoint"
        class="point-details mt-4"
        elevation="2"
      >
        <v-card-title class="d-flex align-center">
          <v-icon start :color="getEntityColor(selectedPoint.entityType)">
            {{ getEntityIcon(selectedPoint.entityType) }}
          </v-icon>
          {{ selectedPoint.label }}
        </v-card-title>
        <v-card-text>
          <div class="text-caption text-grey mb-2">{{ selectedPoint.entityType }}</div>
          <v-btn
            color="primary"
            size="small"
            @click="openEntity(selectedPoint)"
          >
            Details anzeigen
          </v-btn>
        </v-card-text>
      </v-card>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch, nextTick } from 'vue'
import * as Plotly from 'plotly.js-dist-min'

interface ClusterPoint {
  entityType: string
  entityId: number
  label: string
  x: number
  y: number
  category: string
}

const props = defineProps<{
  points: ClusterPoint[]
  method?: string
}>()

const emit = defineEmits<{
  (e: 'method-change', method: string): void
  (e: 'point-click', point: ClusterPoint): void
}>()

// State
const chartContainer = ref<HTMLElement | null>(null)
const loading = ref(false)
const error = ref<string | null>(null)
const method = ref(props.method || 'umap')
const selectedPoint = ref<ClusterPoint | null>(null)

// Entity types and colors
const entityTypes = [
  { value: 'Document', label: 'Dokumente', color: '#2196F3', icon: 'mdi-file-document' },
  { value: 'KnowledgeItem', label: 'Wissensbasis', color: '#FF9800', icon: 'mdi-lightbulb' },
  { value: 'JavaDocsExercise', label: 'Java-Docs', color: '#4CAF50', icon: 'mdi-code-braces' },
  { value: 'Image', label: 'Bilder', color: '#9C27B0', icon: 'mdi-image' },
  { value: 'MoodleResource', label: 'Moodle', color: '#F44336', icon: 'mdi-school' }
]

const getEntityColor = (type: string): string => {
  const found = entityTypes.find(t => t.value === type)
  return found?.color || '#757575'
}

const getEntityIcon = (type: string): string => {
  const found = entityTypes.find(t => t.value === type)
  return found?.icon || 'mdi-circle'
}

const changeMethod = (newMethod: string) => {
  method.value = newMethod
  emit('method-change', newMethod)
}

const resetZoom = () => {
  if (chartContainer.value) {
    Plotly.relayout(chartContainer.value, {
      'xaxis.autorange': true,
      'yaxis.autorange': true
    })
  }
}

const openEntity = (point: ClusterPoint) => {
  emit('point-click', point)
}

const renderChart = () => {
  if (!chartContainer.value || props.points.length === 0) return

  try {
    // Group points by entity type
    const traces = entityTypes.map(type => {
      const typePoints = props.points.filter(p => p.entityType === type.value)
      
      return {
        x: typePoints.map(p => p.x),
        y: typePoints.map(p => p.y),
        text: typePoints.map(p => p.label),
        customdata: typePoints,
        mode: 'markers',
        type: 'scatter',
        name: type.label,
        marker: {
          size: 10,
          color: type.color,
          opacity: 0.7,
          line: {
            color: 'white',
            width: 1
          }
        },
        hovertemplate: '<b>%{text}</b><br>' +
                      'X: %{x:.2f}<br>' +
                      'Y: %{y:.2f}<br>' +
                      '<extra></extra>'
      }
    }).filter(trace => trace.x.length > 0) // Only include types with data

    const layout = {
      title: {
        text: `Cluster-Visualisierung (${method.value.toUpperCase()})`,
        font: { size: 16 }
      },
      xaxis: {
        title: 'Dimension 1',
        zeroline: true,
        showgrid: true,
        gridcolor: '#e0e0e0'
      },
      yaxis: {
        title: 'Dimension 2',
        zeroline: true,
        showgrid: true,
        gridcolor: '#e0e0e0'
      },
      hovermode: 'closest',
      showlegend: true,
      legend: {
        x: 1,
        y: 1,
        xanchor: 'right'
      },
      margin: {
        l: 60,
        r: 150,
        t: 60,
        b: 60
      },
      plot_bgcolor: '#fafafa',
      paper_bgcolor: '#ffffff'
    }

    const config = {
      responsive: true,
      displayModeBar: true,
      displaylogo: false,
      modeBarButtonsToRemove: ['lasso2d', 'select2d']
    }

    Plotly.newPlot(chartContainer.value, traces, layout, config)

    // Add click event
    const plotElement = chartContainer.value as any
    plotElement.on('plotly_click', (data: any) => {
      const point = data.points[0]
      if (point && point.customdata) {
        selectedPoint.value = point.customdata
      }
    })

  } catch (err: any) {
    error.value = `Fehler beim Rendern: ${err.message}`
    console.error('Chart rendering error:', err)
  }
}

// Watch for points changes
watch(() => props.points, () => {
  nextTick(() => renderChart())
}, { deep: true })

// Initial render
onMounted(() => {
  nextTick(() => renderChart())
})
</script>

<style scoped>
.cluster-visualization {
  width: 100%;
  min-height: 500px;
}

.cluster-container {
  width: 100%;
}

.cluster-controls {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.chart-canvas {
  width: 100%;
  height: 600px;
  background: white;
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}

.cluster-legend {
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
  padding: 1rem;
  background: #f5f5f5;
  border-radius: 8px;
}

.legend-title {
  font-weight: 600;
  width: 100%;
  margin-bottom: 0.5rem;
}

.legend-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.legend-dot {
  width: 12px;
  height: 12px;
  border-radius: 50%;
  border: 1px solid white;
}

.legend-label {
  font-size: 0.875rem;
}

.point-details {
  animation: slideIn 0.3s ease-out;
}

@keyframes slideIn {
  from {
    opacity: 0;
    transform: translateY(10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
</style>
