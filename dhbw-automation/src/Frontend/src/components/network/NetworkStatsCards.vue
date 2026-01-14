<template>
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
            @click="$emit('showPending')"
          >
            Bestaetigen
          </v-btn>
        </v-card-text>
      </v-card>
    </v-col>
  </v-row>
</template>

<script setup lang="ts">
import type { NetworkStats } from '@/types/knowledgeNetwork'

defineProps<{
  stats: NetworkStats
}>()

defineEmits<{
  'showPending': []
}>()
</script>
