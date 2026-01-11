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

      <div class="d-flex gap-2">
        <v-btn
          color="primary"
          variant="tonal"
          @click="showSearchDialog = true"
        >
          <v-icon start>mdi-magnify</v-icon>
          Semantische Suche
        </v-btn>
        <v-btn
          color="secondary"
          variant="tonal"
          @click="showTagManager = true"
        >
          <v-icon start>mdi-tag-multiple</v-icon>
          Tags
        </v-btn>
      </div>
    </div>

    <!-- Statistics -->
    <v-row class="mb-4">
      <v-col cols="12" sm="6" md="3">
        <v-card color="primary" variant="tonal">
          <v-card-text class="text-center">
            <div class="text-h3 mb-2">{{ stats.totalNodes }}</div>
            <div class="text-subtitle-1">Wissensknoten</div>
          </v-card-text>
        </v-card>
      </v-col>
      <v-col cols="12" sm="6" md="3">
        <v-card color="success" variant="tonal">
          <v-card-text class="text-center">
            <div class="text-h3 mb-2">{{ stats.totalLinks }}</div>
            <div class="text-subtitle-1">Verknuepfungen</div>
          </v-card-text>
        </v-card>
      </v-col>
      <v-col cols="12" sm="6" md="3">
        <v-card color="info" variant="tonal">
          <v-card-text class="text-center">
            <div class="text-h3 mb-2">{{ stats.embeddingsCount }}</div>
            <div class="text-subtitle-1">Embeddings</div>
          </v-card-text>
        </v-card>
      </v-col>
      <v-col cols="12" sm="6" md="3">
        <v-card color="warning" variant="tonal">
          <v-card-text class="text-center">
            <div class="text-h3 mb-2">{{ stats.pendingLinks }}</div>
            <div class="text-subtitle-1">Vorgeschlagene Links</div>
            <v-btn
              v-if="stats.pendingLinks > 0"
              size="small"
              color="warning"
              class="mt-2"
              @click="showPendingLinks = true"
            >
              Bestaetigen
            </v-btn>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <!-- Main Content: Graph + Details -->
    <v-row>
      <!-- Network Graph -->
      <v-col cols="12" :md="selectedNode ? 8 : 12">
        <v-card>
          <v-card-title class="d-flex align-center">
            <v-icon class="mr-2">mdi-web</v-icon>
            Wissensgraph
            <v-spacer />
            <v-btn-toggle v-model="viewMode" density="compact" mandatory>
              <v-btn value="graph" size="small">
                <v-icon>mdi-graph</v-icon>
              </v-btn>
              <v-btn value="list" size="small">
                <v-icon>mdi-format-list-bulleted</v-icon>
              </v-btn>
            </v-btn-toggle>
            <v-btn
              icon
              variant="text"
              class="ml-2"
              @click="loadNetworkGraph"
              :loading="loadingGraph"
            >
              <v-icon>mdi-refresh</v-icon>
            </v-btn>
          </v-card-title>

          <v-card-text>
            <!-- Graph View -->
            <div v-if="viewMode === 'graph'" class="network-container">
              <div v-if="loadingGraph" class="text-center py-8">
                <v-progress-circular indeterminate color="primary" />
                <p class="mt-4">Lade Wissensgraph...</p>
              </div>
              <div v-else-if="graphNodes.length === 0" class="text-center py-8">
                <v-icon size="64" color="grey">mdi-graph-outline</v-icon>
                <p class="mt-4 text-grey">Keine Daten im Wissensnetzwerk</p>
                <v-btn color="primary" class="mt-4" @click="generateSemanticLinks">
                  <v-icon start>mdi-auto-fix</v-icon>
                  Automatische Links generieren
                </v-btn>
              </div>
              <NetworkGraph
                v-else
                :nodes="graphNodes"
                :edges="graphEdges"
                @node-click="selectNode"
                @node-double-click="navigateToNodeEntity"
              />
            </div>

            <!-- List View -->
            <div v-else>
              <v-text-field
                v-model="listFilter"
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
                  @click="selectNode(node)"
                  :class="{ 'bg-primary-lighten-5': selectedNode?.id === node.id }"
                >
                  <template v-slot:prepend>
                    <v-icon :color="getNodeColor(node.type)">
                      {{ getNodeIcon(node.type) }}
                    </v-icon>
                  </template>

                  <v-list-item-title>{{ node.label }}</v-list-item-title>
                  <v-list-item-subtitle>
                    {{ node.type }} - {{ node.linkCount }} Verknuepfungen
                  </v-list-item-subtitle>

                  <template v-slot:append>
                    <v-chip size="small" variant="tonal" :color="getNodeColor(node.type)">
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
      </v-col>

      <!-- Details Panel -->
      <v-col v-if="selectedNode" cols="12" md="4">
        <v-card>
          <v-card-title class="d-flex align-center">
            <v-icon :color="getNodeColor(selectedNode.type)" class="mr-2">
              {{ getNodeIcon(selectedNode.type) }}
            </v-icon>
            Details
            <v-spacer />
            <v-btn icon variant="text" @click="selectedNode = null">
              <v-icon>mdi-close</v-icon>
            </v-btn>
          </v-card-title>

          <v-card-text>
            <h3 class="text-h6 mb-2">{{ selectedNode.label }}</h3>

            <v-chip size="small" class="mb-4" :color="getNodeColor(selectedNode.type)">
              {{ selectedNode.type }}
            </v-chip>

            <!-- Tags -->
            <div class="mb-4">
              <div class="text-subtitle-2 mb-2">Tags</div>
              <div class="d-flex flex-wrap gap-1">
                <v-chip
                  v-for="tag in selectedNodeTags"
                  :key="tag.id"
                  size="small"
                  :color="tag.color"
                  closable
                  @click:close="removeTag(tag.id)"
                >
                  {{ tag.name }}
                </v-chip>
                <v-btn
                  size="x-small"
                  variant="tonal"
                  @click="showAddTagDialog = true"
                >
                  <v-icon size="small">mdi-plus</v-icon>
                </v-btn>
              </div>
            </div>

            <v-divider class="mb-4" />

            <!-- Related Content -->
            <div class="text-subtitle-2 mb-2">Verknuepfte Inhalte</div>

            <div v-if="loadingRelated" class="text-center py-4">
              <v-progress-circular indeterminate size="24" />
            </div>

            <v-list v-else-if="relatedContent.length > 0" density="compact">
              <v-list-item
                v-for="item in relatedContent"
                :key="`${item.entityType}-${item.entityId}`"
                @click="navigateToEntity(item)"
              >
                <template v-slot:prepend>
                  <v-icon size="small" :color="getNodeColor(item.entityType)">
                    {{ getNodeIcon(item.entityType) }}
                  </v-icon>
                </template>

                <v-list-item-title class="text-body-2">
                  {{ item.title }}
                </v-list-item-title>
                <v-list-item-subtitle class="text-caption">
                  {{ item.linkType }} - {{ formatScore(item.score) }}
                </v-list-item-subtitle>
              </v-list-item>
            </v-list>

            <v-alert v-else type="info" variant="tonal" density="compact">
              Keine verknuepften Inhalte
            </v-alert>

            <v-divider class="my-4" />

            <!-- Actions -->
            <div class="d-flex flex-column gap-2">
              <v-btn
                color="primary"
                variant="outlined"
                block
                @click="showCreateLinkDialog = true"
              >
                <v-icon start>mdi-link-plus</v-icon>
                Verknuepfung erstellen
              </v-btn>
              <v-btn
                color="info"
                variant="outlined"
                block
                @click="findSimilar"
                :loading="findingSimilar"
              >
                <v-icon start>mdi-magnify</v-icon>
                Aehnliche finden
              </v-btn>
            </div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <!-- Semantic Search Dialog -->
    <v-dialog v-model="showSearchDialog" max-width="700">
      <v-card>
        <v-card-title>
          <v-icon class="mr-2">mdi-magnify</v-icon>
          Semantische Suche
        </v-card-title>
        <v-card-text>
          <v-text-field
            v-model="searchQuery"
            label="Suchanfrage"
            placeholder="Was moechtest du finden? (z.B. 'Java Generics', 'SQL Joins')"
            variant="outlined"
            prepend-inner-icon="mdi-magnify"
            autofocus
            @keyup.enter="performSemanticSearch"
          />

          <v-select
            v-model="searchEntityTypes"
            :items="entityTypeOptions"
            label="Inhaltstypen"
            variant="outlined"
            density="compact"
            multiple
            chips
            class="mt-2"
          />

          <div v-if="searchLoading" class="text-center py-4">
            <v-progress-circular indeterminate color="primary" />
            <p class="mt-2">Suche im Wissensraum...</p>
          </div>

          <v-list v-else-if="searchResults.length > 0" class="mt-4">
            <v-list-subheader>Suchergebnisse ({{ searchResults.length }})</v-list-subheader>
            <v-list-item
              v-for="result in searchResults"
              :key="`${result.entityType}-${result.entityId}`"
              @click="selectSearchResult(result)"
            >
              <template v-slot:prepend>
                <v-icon :color="getNodeColor(result.entityType)">
                  {{ getNodeIcon(result.entityType) }}
                </v-icon>
              </template>

              <v-list-item-title>{{ result.title }}</v-list-item-title>
              <v-list-item-subtitle>
                {{ result.entityType }} - Relevanz: {{ formatScore(result.score) }}
              </v-list-item-subtitle>

              <template v-slot:append>
                <v-chip size="small" :color="getScoreColor(result.score)">
                  {{ formatScore(result.score) }}
                </v-chip>
              </template>
            </v-list-item>
          </v-list>

          <v-alert v-else-if="searchPerformed" type="info" variant="tonal" class="mt-4">
            Keine Ergebnisse gefunden. Versuche andere Suchbegriffe.
          </v-alert>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showSearchDialog = false">Schliessen</v-btn>
          <v-btn
            color="primary"
            :loading="searchLoading"
            :disabled="!searchQuery"
            @click="performSemanticSearch"
          >
            Suchen
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Tag Manager Dialog -->
    <v-dialog v-model="showTagManager" max-width="600">
      <v-card>
        <v-card-title>
          <v-icon class="mr-2">mdi-tag-multiple</v-icon>
          Tag-Verwaltung
        </v-card-title>
        <v-card-text>
          <!-- Create new tag -->
          <v-form @submit.prevent="createTag">
            <div class="d-flex gap-2 mb-4">
              <v-text-field
                v-model="newTagName"
                label="Neuer Tag"
                variant="outlined"
                density="compact"
                hide-details
              />
              <v-menu>
                <template v-slot:activator="{ props }">
                  <v-btn
                    v-bind="props"
                    :color="newTagColor"
                    icon
                    variant="flat"
                    size="small"
                  >
                    <v-icon>mdi-palette</v-icon>
                  </v-btn>
                </template>
                <v-color-picker v-model="newTagColor" mode="hexa" />
              </v-menu>
              <v-btn type="submit" color="primary" :disabled="!newTagName">
                <v-icon>mdi-plus</v-icon>
              </v-btn>
            </div>
          </v-form>

          <v-divider class="mb-4" />

          <!-- Tag list -->
          <v-list v-if="userTags.length > 0">
            <v-list-item v-for="tag in userTags" :key="tag.id">
              <template v-slot:prepend>
                <v-avatar :color="tag.color" size="24">
                  <v-icon size="small" color="white">mdi-tag</v-icon>
                </v-avatar>
              </template>

              <v-list-item-title>{{ tag.name }}</v-list-item-title>
              <v-list-item-subtitle>{{ tag.assignmentCount }} Zuweisungen</v-list-item-subtitle>

              <template v-slot:append>
                <v-btn icon variant="text" size="small" @click="deleteTag(tag.id)">
                  <v-icon size="small">mdi-delete</v-icon>
                </v-btn>
              </template>
            </v-list-item>
          </v-list>

          <v-alert v-else type="info" variant="tonal">
            Noch keine Tags erstellt
          </v-alert>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showTagManager = false">Schliessen</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Create Link Dialog -->
    <v-dialog v-model="showCreateLinkDialog" max-width="500">
      <v-card>
        <v-card-title>Verknuepfung erstellen</v-card-title>
        <v-card-text>
          <p class="text-body-2 mb-4">
            Erstelle eine Verknuepfung von <strong>{{ selectedNode?.label }}</strong> zu einem anderen Inhalt.
          </p>

          <v-select
            v-model="newLink.targetType"
            :items="entityTypeOptions"
            label="Ziel-Typ"
            variant="outlined"
            density="compact"
            class="mb-2"
          />

          <v-autocomplete
            v-model="newLink.targetId"
            :items="linkTargetOptions"
            item-title="label"
            item-value="entityId"
            label="Ziel auswaehlen"
            variant="outlined"
            density="compact"
            :loading="loadingTargets"
            class="mb-2"
          />

          <v-select
            v-model="newLink.linkType"
            :items="linkTypeOptions"
            label="Verknuepfungstyp"
            variant="outlined"
            density="compact"
          />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showCreateLinkDialog = false">Abbrechen</v-btn>
          <v-btn
            color="primary"
            :disabled="!newLink.targetId"
            :loading="creatingLink"
            @click="createLink"
          >
            Erstellen
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Pending Links Dialog -->
    <v-dialog v-model="showPendingLinks" max-width="600">
      <v-card>
        <v-card-title>
          <v-icon class="mr-2" color="warning">mdi-link-variant</v-icon>
          Vorgeschlagene Verknuepfungen
        </v-card-title>
        <v-card-text>
          <p class="text-body-2 mb-4">
            Diese Verknuepfungen wurden automatisch basierend auf semantischer Aehnlichkeit vorgeschlagen.
          </p>

          <v-list v-if="pendingLinks.length > 0">
            <v-list-item v-for="link in pendingLinks" :key="link.id">
              <v-list-item-title class="text-body-2">
                {{ link.sourceTitle }} <v-icon size="small">mdi-arrow-right</v-icon> {{ link.targetTitle }}
              </v-list-item-title>
              <v-list-item-subtitle>
                {{ link.linkType }} - Konfidenz: {{ formatScore(link.confidence) }}
              </v-list-item-subtitle>

              <template v-slot:append>
                <v-btn
                  icon
                  variant="text"
                  color="success"
                  size="small"
                  @click="confirmLink(link.id)"
                >
                  <v-icon>mdi-check</v-icon>
                </v-btn>
                <v-btn
                  icon
                  variant="text"
                  color="error"
                  size="small"
                  @click="rejectLink(link.id)"
                >
                  <v-icon>mdi-close</v-icon>
                </v-btn>
              </template>
            </v-list-item>
          </v-list>

          <v-alert v-else type="success" variant="tonal">
            Keine ausstehenden Vorschlaege
          </v-alert>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showPendingLinks = false">Schliessen</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Add Tag Dialog -->
    <v-dialog v-model="showAddTagDialog" max-width="400">
      <v-card>
        <v-card-title>Tag hinzufuegen</v-card-title>
        <v-card-text>
          <v-select
            v-model="selectedTagToAdd"
            :items="userTags"
            item-title="name"
            item-value="id"
            label="Tag auswaehlen"
            variant="outlined"
          >
            <template v-slot:item="{ item, props }">
              <v-list-item v-bind="props">
                <template v-slot:prepend>
                  <v-avatar :color="item.raw.color" size="24">
                    <v-icon size="small" color="white">mdi-tag</v-icon>
                  </v-avatar>
                </template>
              </v-list-item>
            </template>
          </v-select>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showAddTagDialog = false">Abbrechen</v-btn>
          <v-btn color="primary" :disabled="!selectedTagToAdd" @click="addTag">
            Hinzufuegen
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

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
import api from '@/services/api'
import NetworkGraph from '@/components/network/NetworkGraph.vue'

const { mobile } = useDisplay()
const isMobile = computed(() => mobile.value)
const router = useRouter()

// View state
const viewMode = ref<'graph' | 'list'>('list')
const listFilter = ref('')
const _networkContainer = ref<HTMLElement | null>(null)

// Stats
const stats = ref({
  totalNodes: 0,
  totalLinks: 0,
  embeddingsCount: 0,
  pendingLinks: 0
})

// Graph data
const graphNodes = ref<GraphNode[]>([])
const graphEdges = ref<GraphEdge[]>([])
const loadingGraph = ref(false)

// Selected node
const selectedNode = ref<GraphNode | null>(null)
const selectedNodeTags = ref<Tag[]>([])
const relatedContent = ref<RelatedItem[]>([])
const loadingRelated = ref(false)
const findingSimilar = ref(false)

// Search
const showSearchDialog = ref(false)
const searchQuery = ref('')
const searchEntityTypes = ref<string[]>([])
const searchResults = ref<SearchResult[]>([])
const searchLoading = ref(false)
const searchPerformed = ref(false)

// Tags
const showTagManager = ref(false)
const userTags = ref<Tag[]>([])
const newTagName = ref('')
const newTagColor = ref('#1976D2')
const showAddTagDialog = ref(false)
const selectedTagToAdd = ref<number | null>(null)

// Links
const showCreateLinkDialog = ref(false)
const showPendingLinks = ref(false)
const pendingLinks = ref<PendingLink[]>([])
const newLink = ref({
  targetType: '',
  targetId: null as number | null,
  linkType: 'related'
})
const linkTargetOptions = ref<GraphNode[]>([])
const loadingTargets = ref(false)
const creatingLink = ref(false)

// Snackbar
const snackbar = ref({
  show: false,
  message: '',
  color: 'success'
})

// Types
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

interface Tag {
  id: number
  name: string
  color: string
  assignmentCount: number
}

interface RelatedItem {
  entityType: string
  entityId: number
  title: string
  linkType: string
  score: number
}

interface SearchResult {
  entityType: string
  entityId: number
  title: string
  score: number
}

interface PendingLink {
  id: number
  sourceTitle: string
  targetTitle: string
  linkType: string
  confidence: number
}

// Options
const entityTypeOptions = [
  { title: 'Dokumente', value: 'Document' },
  { title: 'Wissensbasis', value: 'KnowledgeItem' },
  { title: 'Java-Docs Uebungen', value: 'JavaDocsExercise' },
  { title: 'Bilder', value: 'Image' },
  { title: 'Moodle Ressourcen', value: 'MoodleResource' }
]

const linkTypeOptions = [
  { title: 'Verwandt', value: 'related' },
  { title: 'Voraussetzung', value: 'prerequisite' },
  { title: 'Erweiterung', value: 'extension' },
  { title: 'Beispiel', value: 'example' },
  { title: 'Abgeleitet von', value: 'derived_from' }
]

// Computed
const filteredNodes = computed(() => {
  if (!listFilter.value) return graphNodes.value
  const filter = listFilter.value.toLowerCase()
  return graphNodes.value.filter(n =>
    n.label.toLowerCase().includes(filter) ||
    n.type.toLowerCase().includes(filter)
  )
})

// Methods
const loadNetworkGraph = async () => {
  loadingGraph.value = true
  try {
    const response = await api.get('/knowledgenetwork/graph')
    if (response.data) {
      graphNodes.value = response.data.nodes || []
      graphEdges.value = response.data.edges || []
      stats.value.totalNodes = graphNodes.value.length
      stats.value.totalLinks = graphEdges.value.length
    }
  } catch (error) {
    console.error('Error loading graph:', error)
    showMessage('Fehler beim Laden des Graphen', 'error')
  } finally {
    loadingGraph.value = false
  }
}

const loadStats = async () => {
  try {
    // Get pending links count
    const pendingResponse = await api.get('/knowledgenetwork/links/pending')
    if (pendingResponse.data) {
      pendingLinks.value = pendingResponse.data
      stats.value.pendingLinks = pendingLinks.value.length
    }
  } catch (error) {
    console.error('Error loading stats:', error)
  }
}

const loadTags = async () => {
  try {
    const response = await api.get('/tags')
    userTags.value = response.data || []
  } catch (error) {
    console.error('Error loading tags:', error)
  }
}

const selectNode = async (node: GraphNode) => {
  selectedNode.value = node
  await Promise.all([
    loadRelatedContent(node),
    loadNodeTags(node)
  ])
}

const loadRelatedContent = async (node: GraphNode) => {
  loadingRelated.value = true
  try {
    const response = await api.get(`/knowledgenetwork/related/${node.type}/${node.entityId}`)
    relatedContent.value = response.data || []
  } catch (error) {
    console.error('Error loading related content:', error)
  } finally {
    loadingRelated.value = false
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
    showMessage('Fehler bei der Suche', 'error')
  } finally {
    searchLoading.value = false
  }
}

const selectSearchResult = (result: SearchResult) => {
  const node: GraphNode = {
    id: `${result.entityType}-${result.entityId}`,
    entityType: result.entityType,
    entityId: result.entityId,
    label: result.title,
    type: result.entityType,
    linkCount: 0
  }
  showSearchDialog.value = false
  selectNode(node)
}

const findSimilar = async () => {
  if (!selectedNode.value) return

  findingSimilar.value = true
  try {
    const response = await api.get(
      `/knowledgenetwork/similar/${selectedNode.value.type}/${selectedNode.value.entityId}`
    )
    if (response.data && response.data.length > 0) {
      searchResults.value = response.data
      showSearchDialog.value = true
      searchQuery.value = `Aehnlich zu: ${selectedNode.value.label}`
      searchPerformed.value = true
    } else {
      showMessage('Keine aehnlichen Inhalte gefunden', 'info')
    }
  } catch (error) {
    console.error('Error finding similar:', error)
    showMessage('Fehler beim Suchen', 'error')
  } finally {
    findingSimilar.value = false
  }
}

const generateSemanticLinks = async () => {
  try {
    const response = await api.post('/knowledgenetwork/generate-links')
    if (response.data.success) {
      showMessage(`${response.data.generatedCount} Links generiert`, 'success')
      await Promise.all([loadNetworkGraph(), loadStats()])
    }
  } catch (error) {
    console.error('Error generating links:', error)
    showMessage('Fehler beim Generieren der Links', 'error')
  }
}

const createTag = async () => {
  if (!newTagName.value) return

  try {
    const response = await api.post('/tags', {
      name: newTagName.value,
      color: newTagColor.value
    })
    if (response.data) {
      userTags.value.push(response.data)
      newTagName.value = ''
      showMessage('Tag erstellt', 'success')
    }
  } catch (error: any) {
    showMessage(error.response?.data?.message || 'Fehler beim Erstellen', 'error')
  }
}

const deleteTag = async (tagId: number) => {
  if (!confirm('Tag wirklich loeschen?')) return

  try {
    await api.delete(`/tags/${tagId}`)
    userTags.value = userTags.value.filter(t => t.id !== tagId)
    showMessage('Tag geloescht', 'success')
  } catch (error) {
    showMessage('Fehler beim Loeschen', 'error')
  }
}

const addTag = async () => {
  if (!selectedNode.value || !selectedTagToAdd.value) return

  try {
    await api.post(`/tags/${selectedTagToAdd.value}/assign`, {
      entityType: selectedNode.value.type,
      entityId: selectedNode.value.entityId
    })
    await loadNodeTags(selectedNode.value)
    showAddTagDialog.value = false
    selectedTagToAdd.value = null
    showMessage('Tag hinzugefuegt', 'success')
  } catch (error: any) {
    showMessage(error.response?.data?.message || 'Fehler', 'error')
  }
}

const removeTag = async (tagId: number) => {
  if (!selectedNode.value) return

  try {
    await api.delete(
      `/tags/${tagId}/assign?entityType=${selectedNode.value.type}&entityId=${selectedNode.value.entityId}`
    )
    selectedNodeTags.value = selectedNodeTags.value.filter(t => t.id !== tagId)
    showMessage('Tag entfernt', 'success')
  } catch (error) {
    showMessage('Fehler beim Entfernen', 'error')
  }
}

const createLink = async () => {
  if (!selectedNode.value || !newLink.value.targetId) return

  creatingLink.value = true
  try {
    await api.post('/knowledgenetwork/links', {
      sourceEntityType: selectedNode.value.type,
      sourceEntityId: selectedNode.value.entityId,
      targetEntityType: newLink.value.targetType,
      targetEntityId: newLink.value.targetId,
      linkType: newLink.value.linkType
    })
    showCreateLinkDialog.value = false
    showMessage('Verknuepfung erstellt', 'success')
    await Promise.all([
      loadNetworkGraph(),
      loadRelatedContent(selectedNode.value)
    ])
  } catch (error: any) {
    showMessage(error.response?.data?.message || 'Fehler', 'error')
  } finally {
    creatingLink.value = false
  }
}

const confirmLink = async (linkId: number) => {
  try {
    await api.post(`/knowledgenetwork/links/${linkId}/confirm`)
    pendingLinks.value = pendingLinks.value.filter(l => l.id !== linkId)
    stats.value.pendingLinks--
    showMessage('Link bestaetigt', 'success')
  } catch (error) {
    showMessage('Fehler', 'error')
  }
}

const rejectLink = async (linkId: number) => {
  try {
    await api.post(`/knowledgenetwork/links/${linkId}/reject`)
    pendingLinks.value = pendingLinks.value.filter(l => l.id !== linkId)
    stats.value.pendingLinks--
    showMessage('Link abgelehnt', 'success')
  } catch (error) {
    showMessage('Fehler', 'error')
  }
}

const navigateToEntity = (item: RelatedItem) => {
  // Navigate based on entity type
  const routes: Record<string, string> = {
    'Document': '/files',
    'KnowledgeItem': '/learning',
    'JavaDocsExercise': '/learning',
    'Image': '/files'
  }
  const route = routes[item.entityType] || '/dashboard'
  router.push(route)
  showMessage(`Navigiere zu ${item.title}`, 'info')
}

const navigateToNodeEntity = (node: GraphNode) => {
  // Navigate based on node type
  const routes: Record<string, string> = {
    'Document': '/files',
    'KnowledgeItem': '/learning',
    'JavaDocsExercise': '/learning',
    'Image': '/files',
    'MoodleResource': '/learning'
  }
  const route = routes[node.type] || '/dashboard'
  router.push(route)
}

// Watch for target type changes to load options
watch(() => newLink.value.targetType, async (type) => {
  if (!type) return
  loadingTargets.value = true
  try {
    // Load entities of the selected type
    linkTargetOptions.value = graphNodes.value.filter(n => n.type === type)
  } finally {
    loadingTargets.value = false
  }
})

// Helpers
const getNodeIcon = (type: string) => {
  const icons: Record<string, string> = {
    'Document': 'mdi-file-document',
    'KnowledgeItem': 'mdi-lightbulb',
    'JavaDocsExercise': 'mdi-language-java',
    'Image': 'mdi-image',
    'MoodleResource': 'mdi-school'
  }
  return icons[type] || 'mdi-circle'
}

const getNodeColor = (type: string) => {
  const colors: Record<string, string> = {
    'Document': 'blue',
    'KnowledgeItem': 'orange',
    'JavaDocsExercise': 'green',
    'Image': 'purple',
    'MoodleResource': 'red'
  }
  return colors[type] || 'grey'
}

const getScoreColor = (score: number) => {
  if (score >= 0.9) return 'success'
  if (score >= 0.7) return 'info'
  if (score >= 0.5) return 'warning'
  return 'grey'
}

const formatScore = (score: number) => {
  return `${Math.round(score * 100)}%`
}

const showMessage = (message: string, color: string = 'success') => {
  snackbar.value = { show: true, message, color }
}

// Initialize
onMounted(async () => {
  await Promise.all([
    loadNetworkGraph(),
    loadStats(),
    loadTags()
  ])
})
</script>

<style scoped>
.network-container {
  height: 500px;
  border: 1px solid #e0e0e0;
  border-radius: 4px;
  position: relative;
}

.gap-1 {
  gap: 4px;
}

.gap-2 {
  gap: 8px;
}
</style>
