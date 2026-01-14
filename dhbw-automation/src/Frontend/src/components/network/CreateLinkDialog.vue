<template>
  <v-dialog :model-value="modelValue" @update:model-value="$emit('update:modelValue', $event)" max-width="500">
    <v-card>
      <v-card-title>Verknuepfung erstellen</v-card-title>
      <v-card-text>
        <p class="text-body-2 mb-4">
          Erstelle eine Verknuepfung von <strong>{{ sourceLabel }}</strong> zu einem anderen Inhalt.
        </p>

        <v-select
          :model-value="targetType"
          @update:model-value="$emit('update:targetType', $event)"
          :items="entityTypeOptions"
          label="Ziel-Typ"
          variant="outlined"
          density="compact"
          class="mb-2"
        />

        <v-autocomplete
          :model-value="targetId"
          @update:model-value="$emit('update:targetId', $event)"
          :items="targetOptions"
          item-title="label"
          item-value="entityId"
          label="Ziel auswaehlen"
          variant="outlined"
          density="compact"
          :loading="loadingTargets"
          class="mb-2"
        />

        <v-select
          :model-value="linkType"
          @update:model-value="$emit('update:linkType', $event)"
          :items="linkTypeOptions"
          label="Verknuepfungstyp"
          variant="outlined"
          density="compact"
        />
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="$emit('update:modelValue', false)">Abbrechen</v-btn>
        <v-btn
          color="primary"
          :disabled="!targetId"
          :loading="creating"
          @click="$emit('create')"
        >
          Erstellen
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
import type { GraphNode } from '@/types/knowledgeNetwork'
import { entityTypeOptions, linkTypeOptions } from '@/types/knowledgeNetwork'

defineProps<{
  modelValue: boolean
  sourceLabel: string
  targetType: string
  targetId: number | null
  linkType: string
  targetOptions: GraphNode[]
  loadingTargets: boolean
  creating: boolean
}>()

defineEmits<{
  'update:modelValue': [value: boolean]
  'update:targetType': [value: string]
  'update:targetId': [value: number | null]
  'update:linkType': [value: string]
  'create': []
}>()
</script>
