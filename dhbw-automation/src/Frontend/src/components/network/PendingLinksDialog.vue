<template>
  <v-dialog :model-value="modelValue" @update:model-value="$emit('update:modelValue', $event)" max-width="600">
    <v-card>
      <v-card-title>
        <v-icon class="mr-2" color="warning">mdi-link-variant</v-icon>
        Vorgeschlagene Verknuepfungen
      </v-card-title>
      <v-card-text>
        <p class="text-body-2 mb-4">
          Diese Verknuepfungen wurden automatisch basierend auf semantischer Aehnlichkeit vorgeschlagen.
        </p>

        <v-list v-if="links.length > 0">
          <v-list-item v-for="link in links" :key="link.id">
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
                @click="$emit('confirm', link.id)"
              >
                <v-icon>mdi-check</v-icon>
              </v-btn>
              <v-btn
                icon
                variant="text"
                color="error"
                size="small"
                @click="$emit('reject', link.id)"
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
        <v-btn variant="text" @click="$emit('update:modelValue', false)">Schliessen</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
import type { PendingLink } from '@/types/knowledgeNetwork'
import { formatScore } from '@/types/knowledgeNetwork'

defineProps<{
  modelValue: boolean
  links: PendingLink[]
}>()

defineEmits<{
  'update:modelValue': [value: boolean]
  'confirm': [linkId: number]
  'reject': [linkId: number]
}>()
</script>
