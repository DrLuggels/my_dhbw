<template>
  <v-dialog :model-value="modelValue" @update:model-value="$emit('update:modelValue', $event)" max-width="600">
    <v-card>
      <v-card-title>
        <v-icon class="mr-2">mdi-tag-multiple</v-icon>
        Tag-Verwaltung
      </v-card-title>
      <v-card-text>
        <v-form @submit.prevent="handleCreateTag">
          <div class="d-flex gap-2 mb-4">
            <v-text-field
              v-model="localTagName"
              label="Neuer Tag"
              variant="outlined"
              density="compact"
              hide-details
            />
            <v-menu>
              <template v-slot:activator="{ props }">
                <v-btn
                  v-bind="props"
                  :color="localTagColor"
                  icon
                  variant="flat"
                  size="small"
                >
                  <v-icon>mdi-palette</v-icon>
                </v-btn>
              </template>
              <v-color-picker v-model="localTagColor" mode="hexa" />
            </v-menu>
            <v-btn type="submit" color="primary" :disabled="!localTagName">
              <v-icon>mdi-plus</v-icon>
            </v-btn>
          </div>
        </v-form>

        <v-divider class="mb-4" />

        <v-list v-if="tags.length > 0">
          <v-list-item v-for="tag in tags" :key="tag.id">
            <template v-slot:prepend>
              <v-avatar :color="tag.color" size="24">
                <v-icon size="small" color="white">mdi-tag</v-icon>
              </v-avatar>
            </template>

            <v-list-item-title>{{ tag.name }}</v-list-item-title>
            <v-list-item-subtitle>{{ tag.assignmentCount }} Zuweisungen</v-list-item-subtitle>

            <template v-slot:append>
              <v-btn icon variant="text" size="small" @click="$emit('delete', tag.id)">
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
        <v-btn variant="text" @click="$emit('update:modelValue', false)">Schliessen</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { Tag } from '@/types/knowledgeNetwork'

const props = defineProps<{
  modelValue: boolean
  tags: Tag[]
  tagName: string
  tagColor: string
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  'update:tagName': [value: string]
  'update:tagColor': [value: string]
  'create': []
  'delete': [tagId: number]
}>()

const localTagName = computed({
  get: () => props.tagName,
  set: (value) => emit('update:tagName', value)
})

const localTagColor = computed({
  get: () => props.tagColor,
  set: (value) => emit('update:tagColor', value)
})

const handleCreateTag = () => {
  emit('create')
}
</script>
