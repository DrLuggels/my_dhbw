import { ref } from 'vue'
import api from '@/services/api'
import type { PendingLink, GraphNode, NewLink } from '@/types/knowledgeNetwork'

export function useNetworkLinks() {
  const pendingLinks = ref<PendingLink[]>([])
  const newLink = ref<NewLink>({
    targetType: '',
    targetId: null,
    linkType: 'related'
  })
  const linkTargetOptions = ref<GraphNode[]>([])
  const loadingTargets = ref(false)
  const creatingLink = ref(false)

  const loadPendingLinks = async () => {
    try {
      const response = await api.get('/knowledgenetwork/links/pending')
      pendingLinks.value = response.data || []
      return pendingLinks.value.length
    } catch (error) {
      console.error('Error loading pending links:', error)
      return 0
    }
  }

  const createLink = async (sourceNode: GraphNode) => {
    if (!newLink.value.targetId) return false

    creatingLink.value = true
    try {
      await api.post('/knowledgenetwork/links', {
        sourceEntityType: sourceNode.type,
        sourceEntityId: sourceNode.entityId,
        targetEntityType: newLink.value.targetType,
        targetEntityId: newLink.value.targetId,
        linkType: newLink.value.linkType
      })
      return true
    } catch (error: any) {
      throw new Error(error.response?.data?.message || 'Fehler')
    } finally {
      creatingLink.value = false
    }
  }

  const confirmLink = async (linkId: number) => {
    try {
      await api.post(`/knowledgenetwork/links/${linkId}/confirm`)
      pendingLinks.value = pendingLinks.value.filter(l => l.id !== linkId)
      return true
    } catch (error) {
      throw error
    }
  }

  const rejectLink = async (linkId: number) => {
    try {
      await api.post(`/knowledgenetwork/links/${linkId}/reject`)
      pendingLinks.value = pendingLinks.value.filter(l => l.id !== linkId)
      return true
    } catch (error) {
      throw error
    }
  }

  const updateTargetOptions = (nodes: GraphNode[], targetType: string) => {
    linkTargetOptions.value = nodes.filter(n => n.type === targetType)
  }

  const resetNewLink = () => {
    newLink.value = {
      targetType: '',
      targetId: null,
      linkType: 'related'
    }
  }

  return {
    pendingLinks,
    newLink,
    linkTargetOptions,
    loadingTargets,
    creatingLink,
    loadPendingLinks,
    createLink,
    confirmLink,
    rejectLink,
    updateTargetOptions,
    resetNewLink
  }
}
