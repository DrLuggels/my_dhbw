<template>
  <v-dialog :model-value="modelValue" @update:model-value="$emit('update:modelValue', $event)" max-width="400">
    <v-card>
      <v-card-title>Tag hinzufuegen</v-card-title>
      <v-card-text>
        <v-select
          :model-value="selectedTag"
          @update:model-value="$emit('update:selectedTag', $event)"
          :items="tags"
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
        <v-btn variant="text" @click="$emit('update:modelValue', false)">Abbrechen</v-btn>
        <v-btn color="primary" :disabled="!selectedTag" @click="$emit('add')">
          Hinzufuegen
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
import type { Tag } from '@/types/knowledgeNetwork'

defineProps<{
  modelValue: boolean
  tags: Tag[]
  selectedTag: number | null
}>()

defineEmits<{
  'update:modelValue': [value: boolean]
  'update:selectedTag': [value: number | null]
  'add': []
}>()
</script>
