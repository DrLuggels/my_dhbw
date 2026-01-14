<template>
  <v-card-text>
    <!-- Sub-tabs within Learning Engine -->
    <v-tabs v-model="subTab" density="compact" class="mb-4">
      <v-tab value="dashboard">
        <v-icon start size="small">mdi-view-dashboard</v-icon>
        Dashboard
      </v-tab>
      <v-tab value="questions">
        <v-icon start size="small">mdi-head-question</v-icon>
        Fragen
      </v-tab>
      <v-tab value="graph">
        <v-icon start size="small">mdi-graph</v-icon>
        Wissensgraph
      </v-tab>
      <v-tab value="process">
        <v-icon start size="small">mdi-cog-transfer</v-icon>
        Verarbeitung
      </v-tab>
    </v-tabs>

    <!-- Dashboard -->
    <div v-if="subTab === 'dashboard'">
      <MasteryDashboard
        :stats="masteryStats"
        :weak-areas="weakAreas"
        :due-for-review="dueForReview"
        :loading="loadingStats"
        :loading-weak-areas="loadingWeakAreas"
        :loading-due-review="loadingDueReview"
        @refresh-weak-areas="loadWeakAreas()"
        @refresh-due-review="loadDueForReview()"
        @practice-entity="practiceEntity"
      />
    </div>

    <!-- Adaptive Questions -->
    <div v-else-if="subTab === 'questions'">
      <AdaptiveQuestions
        ref="questionsRef"
        :questions="questions"
        :loading="loadingQuestions"
        :submitting="submittingAnswer"
        @generate="handleGenerateQuestions"
        @submit="handleSubmitAnswer"
        @reset="resetQuestions"
      />
    </div>

    <!-- Knowledge Graph -->
    <div v-else-if="subTab === 'graph'">
      <KnowledgeGraphViewer
        :entities="knowledgeGraph?.entities || []"
        :relationships="knowledgeGraph?.relationships || []"
        :stats="knowledgeGraph?.stats"
        :loading="loadingGraph"
        @refresh="loadKnowledgeGraph()"
        @filter="handleGraphFilter"
        @search="handleGraphSearch"
        @generate-questions="generateEntityQuestionsAndSwitch"
        @show-related="showRelatedEntities"
      />
    </div>

    <!-- Document Processing -->
    <div v-else-if="subTab === 'process'">
      <DocumentProcessor
        :documents="documents"
        :processing="processingDocument"
        :results="processingResults"
        @process="handleProcessDocuments"
        @clear-results="clearProcessingResults"
      />
    </div>

    <!-- Error Display -->
    <v-snackbar v-model="showError" color="error" :timeout="5000">
      {{ error }}
      <template #actions>
        <v-btn variant="text" @click="clearError">Schließen</v-btn>
      </template>
    </v-snackbar>
  </v-card-text>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useLearningEngine } from '@/composables/useLearningEngine'
import type { QuestionGenerationRequest, ProcessingOptions } from '@/types/learningEngine'
import api from '@/services/api'
import {
  MasteryDashboard,
  AdaptiveQuestions,
  KnowledgeGraphViewer,
  DocumentProcessor
} from '@/components/learningEngine'

interface Document {
  id: number
  fileName: string
  subject?: string
  createdAt: string
  isChunked: boolean
}

const {
  // State
  knowledgeGraph,
  questions,
  weakAreas,
  dueForReview,
  masteryStats,
  processingResults,
  error,

  // Loading states
  loadingGraph,
  loadingQuestions,
  loadingWeakAreas,
  loadingStats,
  processingDocument,
  submittingAnswer,

  // Methods
  loadKnowledgeGraph,
  loadMasteryStats,
  loadWeakAreas,
  loadDueForReview,
  generateQuestions,
  generateEntityQuestions,
  submitAnswer,
  resetQuestions,
  processDocumentsBatch,
  clearProcessingResults,
  clearError
} = useLearningEngine()

const subTab = ref('dashboard')
const documents = ref<Document[]>([])
const questionsRef = ref<InstanceType<typeof AdaptiveQuestions> | null>(null)
const loadingDueReview = ref(false)

const showError = computed({
  get: () => !!error.value,
  set: (val) => { if (!val) clearError() }
})

const loadDocuments = async () => {
  try {
    const response = await api.get('/files')
    if (response.data.success) {
      documents.value = response.data.data.map((doc: any) => ({
        id: doc.id,
        fileName: doc.fileName,
        subject: doc.subject,
        createdAt: doc.createdAt,
        isChunked: doc.isChunked
      }))
    }
  } catch (err) {
    console.error('Error loading documents:', err)
  }
}

const handleGenerateQuestions = async (request: QuestionGenerationRequest) => {
  await generateQuestions(request)
}

const handleSubmitAnswer = async (questionId: string, answer: string, entityId?: number) => {
  const feedback = await submitAnswer({
    questionId,
    userAnswer: answer,
    entityId
  })

  if (feedback && questionsRef.value) {
    questionsRef.value.setFeedback(feedback)
  }
}

const handleProcessDocuments = async (documentIds: number[], options: ProcessingOptions) => {
  await processDocumentsBatch(documentIds, options)
  // Reload knowledge graph after processing
  await loadKnowledgeGraph()
}

const handleGraphFilter = async (options: { entityType?: string; subject?: string }) => {
  await loadKnowledgeGraph(options)
}

const handleGraphSearch = async (query: string) => {
  // Search is handled by the KnowledgeGraphViewer internally
  console.log('Search query:', query)
}

const practiceEntity = async (entityId: number) => {
  await generateEntityQuestions(entityId, 5)
  subTab.value = 'questions'
}

const generateEntityQuestionsAndSwitch = async (entityId: number) => {
  await generateEntityQuestions(entityId, 5)
  subTab.value = 'questions'
}

const showRelatedEntities = (entityId: number) => {
  // Could implement a modal or expand the graph to show related entities
  console.log('Show related for entity:', entityId)
}

// Load initial data based on active tab
watch(subTab, async (newTab) => {
  switch (newTab) {
    case 'dashboard':
      await Promise.all([
        loadMasteryStats(),
        loadWeakAreas(),
        loadDueForReview()
      ])
      break
    case 'graph':
      if (!knowledgeGraph.value) {
        await loadKnowledgeGraph()
      }
      break
    case 'process':
      if (documents.value.length === 0) {
        await loadDocuments()
      }
      break
  }
}, { immediate: true })

onMounted(async () => {
  // Initial data load
  await Promise.all([
    loadMasteryStats(),
    loadWeakAreas(),
    loadDueForReview()
  ])
})
</script>
