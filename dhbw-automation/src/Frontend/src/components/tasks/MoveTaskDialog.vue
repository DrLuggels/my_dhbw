<template>
  <v-dialog v-model="dialog" max-width="400">
    <v-card>
      <v-card-title>Aufgabe verschieben</v-card-title>

      <v-card-text v-if="task">
        <p class="text-body-2 mb-4">
          <strong>{{ task.title }}</strong> in welche Liste verschieben?
        </p>

        <v-list density="compact">
          <v-list-item
            v-for="list in availableLists"
            :key="list.id"
            :active="selectedListId === list.id"
            @click="selectedListId = list.id"
          >
            <template v-slot:prepend>
              <v-icon :color="list.color">{{ list.icon }}</v-icon>
            </template>
            <v-list-item-title>{{ list.name }}</v-list-item-title>
            <template v-slot:append>
              <v-icon v-if="selectedListId === list.id" color="primary">
                mdi-check
              </v-icon>
              <v-chip v-if="list.id === task.listId" size="x-small" color="info">
                Aktuell
              </v-chip>
            </template>
          </v-list-item>
        </v-list>
      </v-card-text>

      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="close">Abbrechen</v-btn>
        <v-btn
          color="primary"
          variant="flat"
          @click="submit"
          :loading="isLoading"
          :disabled="selectedListId === task?.listId"
        >
          Verschieben
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import type { Todo, TodoList } from '@/types/tasks'

const dialog = defineModel<boolean>({ default: false })

const props = defineProps<{
  task: Todo | null
  lists: TodoList[]
}>()

const emit = defineEmits<{
  (e: 'moved', taskId: number, targetListId: number): void
}>()

const selectedListId = ref<number | null>(null)
const isLoading = ref(false)

const availableLists = computed(() =>
  props.lists.filter(l => !l.isArchiveList)
)

watch(dialog, (newVal) => {
  if (newVal && props.task) {
    selectedListId.value = props.task.listId ?? null
  }
})

function close() {
  dialog.value = false
}

async function submit() {
  if (!props.task || selectedListId.value === null) return

  isLoading.value = true
  try {
    emit('moved', props.task.id, selectedListId.value)
    close()
  } finally {
    isLoading.value = false
  }
}
</script>
