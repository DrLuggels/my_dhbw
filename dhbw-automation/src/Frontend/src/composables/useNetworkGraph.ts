import { ref } from 'vue'
import api from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import type { GraphNode, GraphEdge, NetworkStats, ClusterPoint } from '@/types/knowledgeNetwork'

export function useNetworkGraph() {
  const authStore = useAuthStore()

  const graphNodes = ref<GraphNode[]>([])
  const graphEdges = ref<GraphEdge[]>([])
  const loadingGraph = ref(false)
  const clusterPoints = ref<ClusterPoint[]>([])
  const clusterMethod = ref('umap')
  const loadingClusters = ref(false)
  const stats = ref<NetworkStats>({
    totalNodes: 0,
    totalLinks: 0,
    embeddingsCount: 0,
    pendingLinks: 0
  })

  const loadNetworkGraph = async (masteryMode: boolean = false) => {
    loadingGraph.value = true
    try {
      let response
      if (authStore.user?.id && masteryMode) {
        response = await api.getUserKnowledgeGraph(authStore.user.id)
        if (response.success && response.data) {
          graphNodes.value = response.data.nodes?.map((n: any) => ({
            id: `node-${n.id}`,
            entityType: 'KnowledgeNode',
            entityId: n.id,
            label: n.subtopic || n.topic,
            type: n.subject,
            linkCount: 0,
            mastery: n.mastery,
            effectiveStrength: n.effectiveStrength,
            lastInteraction: n.lastInteraction,
            decayRate: n.decayRate
          })) || []
          graphEdges.value = response.data.edges?.map((e: any) => ({
            from: `node-${e.sourceNodeId}`,
            to: `node-${e.targetNodeId}`,
            linkType: e.relationType
          })) || []
        }
      } else {
        response = await api.get('/knowledgenetwork/graph')
        if (response.data) {
          graphNodes.value = response.data.nodes || []
          graphEdges.value = response.data.edges || []
        }
      }
      stats.value.totalNodes = graphNodes.value.length
      stats.value.totalLinks = graphEdges.value.length
    } catch (error) {
      console.error('Error loading graph:', error)
      throw error
    } finally {
      loadingGraph.value = false
    }
  }

  const loadClusterData = async () => {
    loadingClusters.value = true
    try {
      const response = await api.get(`/knowledgenetwork/clusters?method=${clusterMethod.value}`)
      if (response.data && response.data.success) {
        clusterPoints.value = response.data.points || []
        return { success: true }
      } else {
        return { success: false, message: response.data?.message || 'Cluster-Daten nicht verfuegbar' }
      }
    } catch (error) {
      console.error('Error loading cluster data:', error)
      throw error
    } finally {
      loadingClusters.value = false
    }
  }

  const generateSemanticLinks = async () => {
    const response = await api.post('/knowledgenetwork/generate-links')
    return response.data
  }

  return {
    graphNodes,
    graphEdges,
    loadingGraph,
    clusterPoints,
    clusterMethod,
    loadingClusters,
    stats,
    loadNetworkGraph,
    loadClusterData,
    generateSemanticLinks
  }
}
