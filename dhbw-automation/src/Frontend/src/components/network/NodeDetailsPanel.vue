<template>
  <v-card>
    <v-card-title class="d-flex align-center">
      <v-icon :color="getNodeColor(node.type, masteryMode, node.mastery)" class="mr-2">
        {{ getNodeIcon(node.type) }}
      </v-icon>
      Details
      <v-spacer />
      <v-btn icon variant="text" @click="$emit('close')">
        <v-icon>mdi-close</v-icon>
      </v-btn>
    </v-card-title>

    <v-card-text>
      <h3 class="text-h6 mb-2">{{ node.label }}</h3>

      <v-chip size="small" class="mb-4" :color="getNodeColor(node.type, masteryMode, node.mastery)">
        {{ node.type }}
      </v-chip>

      <!-- Tags -->
      <div class="mb-4">
        <div class="text-subtitle-2 mb-2">Tags</div>
        <div class="d-flex flex-wrap gap-1">
          <v-chip
            v-for="tag in tags"
            :key="tag.id"
            size="small"
            :color="tag.color"
            closable
            @click:close="$emit('removeTag', tag.id)"
          >
            {{ tag.name }}
          </v-chip>
          <v-btn
            size="x-small"
            variant="tonal"
            @click="$emit('showAddTag')"
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
          @click="$emit('navigateToEntity', item)"
        >
          <template v-slot:prepend>
            <v-icon size="small" :color="getNodeColor(item.entityType, false)">
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
          @click="$emit('showCreateLink')"
        >
          <v-icon start>mdi-link-plus</v-icon>
          Verknuepfung erstellen
        </v-btn>
        <v-btn
          color="info"
          variant="outlined"
          block
          @click="$emit('findSimilar')"
          :loading="findingSimilar"
        >
          <v-icon start>mdi-magnify</v-icon>
          Aehnliche finden
        </v-btn>
      </div>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
import type { GraphNode, Tag, RelatedItem } from '@/types/knowledgeNetwork'
import { getNodeIcon, getNodeColor, formatScore } from '@/types/knowledgeNetwork'

defineProps<{
  node: GraphNode
  tags: Tag[]
  relatedContent: RelatedItem[]
  loadingRelated: boolean
  findingSimilar: boolean
  masteryMode: boolean
}>()

defineEmits<{
  'close': []
  'removeTag': [tagId: number]
  'showAddTag': []
  'showCreateLink': []
  'findSimilar': []
  'navigateToEntity': [item: RelatedItem]
}>()
</script>

<style scoped>
.gap-1 {
  gap: 4px;
}

.gap-2 {
  gap: 8px;
}
</style>
