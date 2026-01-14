import { ref } from 'vue'
import api from '@/services/api'
import type { SearchResult, GraphNode, RelatedItem } from '@/types/knowledgeNetwork'

export function useNetworkSearch() {
  const searchQuery = ref('')
  const searchEntityTypes = ref<string[]>([])
  const searchResults = ref<SearchResult[]>([])
  const searchLoading = ref(false)
  const searchPerformed = ref(false)
  const findingSimilar = ref(false)

  const performSemanticSearch = async () => {
    if (!searchQuery.value) return

    searchLoading.value = true
    searchPerformed.value = false
    try {
      const params = new URLSearchParams({ q: searchQuery.value })
      if (searchEntityTypes.value.length > 0) {
        params.append('entityTypes', searchEntityTypes.value.join(','))
      }

      const response = await api.get(`/knowledgenetwork/search?${params}`)
      searchResults.value = response.data || []
      searchPerformed.value = true
    } catch (error) {
      console.error('Error searching:', error)
      throw error
    } finally {
      searchLoading.value = false
    }
  }

  const findSimilar = async (node: GraphNode) => {
    findingSimilar.value = true
    try {
      const response = await api.get(
        `/knowledgenetwork/similar/${node.type}/${node.entityId}`
      )
      if (response.data && response.data.length > 0) {
        searchResults.value = response.data
        searchQuery.value = `Aehnlich zu: ${node.label}`
        searchPerformed.value = true
        return { found: true }
      }
      return { found: false }
    } catch (error) {
      console.error('Error finding similar:', error)
      throw error
    } finally {
      findingSimilar.value = false
    }
  }

  const loadRelatedContent = async (node: GraphNode): Promise<RelatedItem[]> => {
    try {
      const response = await api.get(`/knowledgenetwork/related/${node.type}/${node.entityId}`)
      return response.data || []
    } catch (error) {
      console.error('Error loading related content:', error)
      return []
    }
  }

  const resetSearch = () => {
    searchQuery.value = ''
    searchEntityTypes.value = []
    searchResults.value = []
    searchPerformed.value = false
  }

  return {
    searchQuery,
    searchEntityTypes,
    searchResults,
    searchLoading,
    searchPerformed,
    findingSimilar,
    performSemanticSearch,
    findSimilar,
    loadRelatedContent,
    resetSearch
  }
}
