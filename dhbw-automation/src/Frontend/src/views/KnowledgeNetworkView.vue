<template>
  <v-container fluid class="pa-4">
    <!-- Header -->
    <div class="d-flex justify-space-between align-center mb-4">
      <div class="d-flex align-center">
        <v-btn icon variant="text" @click="$router.back()" class="mr-2">
          <v-icon>mdi-arrow-left</v-icon>
        </v-btn>
        <h1 :class="isMobile ? 'text-h5' : 'text-h3'">
          <v-icon left color="primary">mdi-graph</v-icon>
          Wissensnetzwerk
        </h1>
      </div>

      <div class="d-flex gap-2 align-center">
        <v-btn-toggle v-model="masteryMode" density="compact" class="mr-2">
          <v-btn :value="false" size="small">
            <v-icon start>mdi-web</v-icon>
            Standard
          </v-btn>
          <v-btn :value="true" size="small" color="success">
            <v-icon start>mdi-brain</v-icon>
            Mastery
          </v-btn>
        </v-btn-toggle>
        <v-btn color="primary" variant="tonal" @click="showSearchDialog = true">
          <v-icon start>mdi-magnify</v-icon>
          Semantische Suche
        </v-btn>
        <v-btn color="secondary" variant="tonal" @click="showTagManager = true">
          <v-icon start>mdi-tag-multiple</v-icon>
          Tags
        </v-btn>
      </div>
    </div>

    <!-- Statistics -->
    <NetworkStatsCards :stats="stats" @showPending="showPendingLinks = true" />

    <!-- Main Content -->
    <v-row>
      <v-col cols="12" :md="selectedNode ? 8 : 12">
        <NetworkGraphCard
          v-model:viewMode="viewMode"
          v-model:listFilter="listFilter"
          :nodes="graphNodes"
          :edges="graphEdges"
          :cluster-points="clusterPoints"
          :cluster-method="clusterMethod"
          :loading="loadingGraph"
          :selected-node-id="selectedNode?.id ?? null"
          :mastery-mode="masteryMode"
          @refresh="handleRefresh"
          @generateLinks="handleGenerateLinks"
          @nodeClick="selectNode"
          @nodeDoubleClick="navigateToNodeEntity"
          @clusterMethodChange="handleClusterMethodChange"
          @clusterPointClick="handleClusterPointClick"
        />
      </v-col>

      <v-col v-if="selectedNode" cols="12" md="4">
        <NodeDetailsPanel
          :node="selectedNode"
          :tags="selectedNodeTags"
          :related-content="relatedContent"
          :loading-related="loadingRelated"
          :finding-similar="findingSimilar"
          :mastery-mode="masteryMode"
          @close="selectedNode = null"
          @removeTag="handleRemoveTag"
          @showAddTag="showAddTagDialog = true"
          @showCreateLink="showCreateLinkDialog = true"
          @findSimilar="handleFindSimilar"
          @navigateToEntity="navigateToEntity"
        />
      </v-col>
    </v-row>

    <!-- Dialogs -->
    <SemanticSearchDialog
      v-model="showSearchDialog"
      v-model:query="searchQuery"
      v-model:entityTypes="searchEntityTypes"
      :results="searchResults"
      :loading="searchLoading"
      :search-performed="searchPerformed"
      @search="handleSearch"
      @select="selectSearchResult"
    />

    <TagManagerDialog
      v-model="showTagManager"
      v-model:tagName="newTagName"
      v-model:tagColor="newTagColor"
      :tags="userTags"
      @create="handleCreateTag"
      @delete="handleDeleteTag"
    />

    <CreateLinkDialog
      v-model="showCreateLinkDialog"
      v-model:targetType="newLink.targetType"
      v-model:targetId="newLink.targetId"
      v-model:linkType="newLink.linkType"
      :source-label="selectedNode?.label || ''"
      :target-options="linkTargetOptions"
      :loading-targets="loadingTargets"
      :creating="creatingLink"
      @create="handleCreateLink"
    />

    <PendingLinksDialog
      v-model="showPendingLinks"
      :links="pendingLinks"
      @confirm="handleConfirmLink"
      @reject="handleRejectLink"
    />

    <AddTagDialog
      v-model="showAddTagDialog"
      v-model:selectedTag="selectedTagToAdd"
      :tags="userTags"
      @add="handleAddTag"
    />

    <!-- Snackbar -->
    <v-snackbar v-model="snackbar.show" :color="snackbar.color" :timeout="4000">
      {{ snackbar.message }}
    </v-snackbar>
  </v-container>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useDisplay } from 'vuetify'
import { useRouter } from 'vue-router'
import type { GraphNode, ClusterPoint, RelatedItem, SearchResult } from '@/types/knowledgeNetwork'
import { entityRoutes } from '@/types/knowledgeNetwork'
import { useNetworkGraph } from '@/composables/useNetworkGraph'
import { useNetworkSearch } from '@/composables/useNetworkSearch'
import { useNetworkTags } from '@/composables/useNetworkTags'
import { useNetworkLinks } from '@/composables/useNetworkLinks'
import NetworkStatsCards from '@/components/network/NetworkStatsCards.vue'
import NetworkGraphCard from '@/components/network/NetworkGraphCard.vue'
import NodeDetailsPanel from '@/components/network/NodeDetailsPanel.vue'
import SemanticSearchDialog from '@/components/network/SemanticSearchDialog.vue'
import TagManagerDialog from '@/components/network/TagManagerDialog.vue'
import CreateLinkDialog from '@/components/network/CreateLinkDialog.vue'
import PendingLinksDialog from '@/components/network/PendingLinksDialog.vue'
import AddTagDialog from '@/components/network/AddTagDialog.vue'

const { mobile } = useDisplay()
const isMobile = computed(() => mobile.value)
const router = useRouter()

// Composables
const graph = useNetworkGraph()
const search = useNetworkSearch()
const tags = useNetworkTags()
const links = useNetworkLinks()

// Destructure composable values
const { graphNodes, graphEdges, loadingGraph, clusterPoints, clusterMethod, stats } = graph
const { searchQuery, searchEntityTypes, searchResults, searchLoading, searchPerformed, findingSimilar } = search
const { userTags, selectedNodeTags, newTagName, newTagColor, selectedTagToAdd } = tags
const { pendingLinks, newLink, linkTargetOptions, loadingTargets, creatingLink } = links

// Local state
const viewMode = ref<'graph' | 'cluster' | 'list'>('list')
const listFilter = ref('')
const masteryMode = ref(false)
const selectedNode = ref<GraphNode | null>(null)
const relatedContent = ref<RelatedItem[]>([])
const loadingRelated = ref(false)

// Dialog visibility
const showSearchDialog = ref(false)
const showTagManager = ref(false)
const showCreateLinkDialog = ref(false)
const showPendingLinks = ref(false)
const showAddTagDialog = ref(false)

// Snackbar
const snackbar = ref({ show: false, message: '', color: 'success' })
const showMessage = (message: string, color: string = 'success') => {
  snackbar.value = { show: true, message, color }
}

// Handlers
const handleRefresh = () => graph.loadNetworkGraph(masteryMode.value)

const handleGenerateLinks = async () => {
  try {
    const result = await graph.generateSemanticLinks()
    if (result.success) {
      showMessage(`${result.generatedCount} Links generiert`, 'success')
      await Promise.all([graph.loadNetworkGraph(masteryMode.value), links.loadPendingLinks()])
    }
  } catch { showMessage('Fehler beim Generieren der Links', 'error') }
}

const selectNode = async (node: GraphNode) => {
  selectedNode.value = node
  loadingRelated.value = true
  try {
    const [related] = await Promise.all([
      search.loadRelatedContent(node),
      tags.loadNodeTags(node)
    ])
    relatedContent.value = related
  } finally { loadingRelated.value = false }
}

const handleSearch = async () => {
  try { await search.performSemanticSearch() }
  catch { showMessage('Fehler bei der Suche', 'error') }
}

const selectSearchResult = (result: SearchResult) => {
  const node: GraphNode = {
    id: `${result.entityType}-${result.entityId}`,
    entityType: result.entityType, entityId: result.entityId,
    label: result.title, type: result.entityType, linkCount: 0
  }
  showSearchDialog.value = false
  selectNode(node)
}

const handleFindSimilar = async () => {
  if (!selectedNode.value) return
  try {
    const result = await search.findSimilar(selectedNode.value)
    if (result.found) showSearchDialog.value = true
    else showMessage('Keine aehnlichen Inhalte gefunden', 'info')
  } catch { showMessage('Fehler beim Suchen', 'error') }
}

const handleCreateTag = async () => {
  try { await tags.createTag(); showMessage('Tag erstellt', 'success') }
  catch (e: any) { showMessage(e.message, 'error') }
}

const handleDeleteTag = async (tagId: number) => {
  if (!confirm('Tag wirklich loeschen?')) return
  try { await tags.deleteTag(tagId); showMessage('Tag geloescht', 'success') }
  catch { showMessage('Fehler beim Loeschen', 'error') }
}

const handleAddTag = async () => {
  if (!selectedNode.value || !selectedTagToAdd.value) return
  try {
    await tags.addTagToNode(selectedNode.value, selectedTagToAdd.value)
    showAddTagDialog.value = false
    selectedTagToAdd.value = null
    showMessage('Tag hinzugefuegt', 'success')
  } catch (e: any) { showMessage(e.message, 'error') }
}

const handleRemoveTag = async (tagId: number) => {
  if (!selectedNode.value) return
  try { await tags.removeTagFromNode(selectedNode.value, tagId); showMessage('Tag entfernt', 'success') }
  catch { showMessage('Fehler beim Entfernen', 'error') }
}

const handleCreateLink = async () => {
  if (!selectedNode.value) return
  try {
    await links.createLink(selectedNode.value)
    showCreateLinkDialog.value = false
    showMessage('Verknuepfung erstellt', 'success')
    await Promise.all([graph.loadNetworkGraph(masteryMode.value), search.loadRelatedContent(selectedNode.value)])
  } catch (e: any) { showMessage(e.message, 'error') }
}

const handleConfirmLink = async (linkId: number) => {
  try { await links.confirmLink(linkId); stats.value.pendingLinks--; showMessage('Link bestaetigt', 'success') }
  catch { showMessage('Fehler', 'error') }
}

const handleRejectLink = async (linkId: number) => {
  try { await links.rejectLink(linkId); stats.value.pendingLinks--; showMessage('Link abgelehnt', 'success') }
  catch { showMessage('Fehler', 'error') }
}

const handleClusterMethodChange = async (method: string) => {
  clusterMethod.value = method
  await graph.loadClusterData()
}

const handleClusterPointClick = (point: ClusterPoint) => {
  selectNode({
    id: `${point.entityType}-${point.entityId}`,
    entityType: point.entityType, entityId: point.entityId,
    label: point.label, type: point.entityType, linkCount: 0
  })
}

const navigateToEntity = (item: RelatedItem) => {
  router.push(entityRoutes[item.entityType] || '/dashboard')
  showMessage(`Navigiere zu ${item.title}`, 'info')
}

const navigateToNodeEntity = (node: GraphNode) => {
  router.push(entityRoutes[node.type] || '/dashboard')
}

// Watchers
watch(() => newLink.value.targetType, (type) => {
  if (type) links.updateTargetOptions(graphNodes.value, type)
})

watch(viewMode, async (mode) => {
  if (mode === 'cluster' && clusterPoints.value.length === 0) await graph.loadClusterData()
})

watch(masteryMode, () => graph.loadNetworkGraph(masteryMode.value))

// Initialize
onMounted(async () => {
  await Promise.all([
    graph.loadNetworkGraph(masteryMode.value),
    links.loadPendingLinks().then(count => { stats.value.pendingLinks = count }),
    tags.loadTags()
  ])
})
</script>

<style scoped>
.gap-2 {
  gap: 8px;
}
</style>
