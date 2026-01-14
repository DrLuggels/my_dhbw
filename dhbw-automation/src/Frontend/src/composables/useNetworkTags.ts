import { ref } from 'vue'
import api from '@/services/api'
import type { Tag, GraphNode } from '@/types/knowledgeNetwork'

export function useNetworkTags() {
  const userTags = ref<Tag[]>([])
  const selectedNodeTags = ref<Tag[]>([])
  const newTagName = ref('')
  const newTagColor = ref('#1976D2')
  const selectedTagToAdd = ref<number | null>(null)

  const loadTags = async () => {
    try {
      const response = await api.get('/tags')
      userTags.value = response.data || []
    } catch (error) {
      console.error('Error loading tags:', error)
    }
  }

  const loadNodeTags = async (node: GraphNode) => {
    try {
      const response = await api.get(`/tags/entity/${node.type}/${node.entityId}`)
      selectedNodeTags.value = response.data || []
    } catch (error) {
      console.error('Error loading tags:', error)
    }
  }

  const createTag = async () => {
    if (!newTagName.value) return null

    try {
      const response = await api.post('/tags', {
        name: newTagName.value,
        color: newTagColor.value
      })
      if (response.data) {
        userTags.value.push(response.data)
        newTagName.value = ''
        return response.data
      }
    } catch (error: any) {
      throw new Error(error.response?.data?.message || 'Fehler beim Erstellen')
    }
    return null
  }

  const deleteTag = async (tagId: number) => {
    try {
      await api.delete(`/tags/${tagId}`)
      userTags.value = userTags.value.filter(t => t.id !== tagId)
      return true
    } catch (error) {
      throw error
    }
  }

  const addTagToNode = async (node: GraphNode, tagId: number) => {
    try {
      await api.post(`/tags/${tagId}/assign`, {
        entityType: node.type,
        entityId: node.entityId
      })
      await loadNodeTags(node)
      return true
    } catch (error: any) {
      throw new Error(error.response?.data?.message || 'Fehler')
    }
  }

  const removeTagFromNode = async (node: GraphNode, tagId: number) => {
    try {
      await api.delete(
        `/tags/${tagId}/assign?entityType=${node.type}&entityId=${node.entityId}`
      )
      selectedNodeTags.value = selectedNodeTags.value.filter(t => t.id !== tagId)
      return true
    } catch (error) {
      throw error
    }
  }

  return {
    userTags,
    selectedNodeTags,
    newTagName,
    newTagColor,
    selectedTagToAdd,
    loadTags,
    loadNodeTags,
    createTag,
    deleteTag,
    addTagToNode,
    removeTagFromNode
  }
}
