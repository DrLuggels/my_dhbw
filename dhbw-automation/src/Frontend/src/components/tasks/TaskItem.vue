<template>
  <v-slide-x-transition>
    <v-list-item
      v-if="!isRemoving"
      :class="['task-item', { 'task-completing': isAnimating }]"
      @click="emit('click', task)"
    >
      <template v-slot:prepend>
        <v-checkbox-btn
          :model-value="task.status === 'completed'"
          @update:model-value="handleToggle"
          @click.stop
          :color="priorityColor"
        />
      </template>

      <v-list-item-title :class="{ 'text-decoration-line-through text-grey': task.status === 'completed' }">
        {{ task.title }}
      </v-list-item-title>

      <v-list-item-subtitle v-if="task.description" class="text-truncate">
        {{ task.description }}
      </v-list-item-subtitle>

      <template v-slot:append>
        <!-- Due Date -->
        <v-chip
          v-if="task.dueDate"
          size="x-small"
          :color="dueDateColor"
          class="mr-2"
        >
          <v-icon size="x-small" start>mdi-calendar</v-icon>
          {{ formatDate(task.dueDate) }}
        </v-chip>

        <!-- Priority -->
        <v-chip size="x-small" :color="priorityColor" class="mr-2">
          {{ priorityIcon }}
        </v-chip>

        <!-- Actions -->
        <v-menu>
          <template v-slot:activator="{ props }">
            <v-btn
              v-bind="props"
              icon="mdi-dots-vertical"
              size="x-small"
              variant="text"
              @click.stop
            />
          </template>
          <v-list density="compact">
            <v-list-item @click.stop="emit('edit', task)">
              <template v-slot:prepend>
                <v-icon size="small">mdi-pencil</v-icon>
              </template>
              <v-list-item-title>Bearbeiten</v-list-item-title>
            </v-list-item>
            <v-list-item @click.stop="emit('move', task)">
              <template v-slot:prepend>
                <v-icon size="small">mdi-folder-move</v-icon>
              </template>
              <v-list-item-title>Verschieben</v-list-item-title>
            </v-list-item>
            <v-list-item @click.stop="handleArchive">
              <template v-slot:prepend>
                <v-icon size="small">mdi-archive</v-icon>
              </template>
              <v-list-item-title>Archivieren</v-list-item-title>
            </v-list-item>
            <v-divider />
            <v-list-item @click.stop="emit('delete', task)" class="text-error">
              <template v-slot:prepend>
                <v-icon size="small" color="error">mdi-delete</v-icon>
              </template>
              <v-list-item-title>Loeschen</v-list-item-title>
            </v-list-item>
          </v-list>
        </v-menu>
      </template>
    </v-list-item>
  </v-slide-x-transition>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import type { Todo } from '@/types/tasks'

const props = defineProps<{
  task: Todo
}>()

const emit = defineEmits<{
  (e: 'toggle', task: Todo): void
  (e: 'click', task: Todo): void
  (e: 'edit', task: Todo): void
  (e: 'move', task: Todo): void
  (e: 'archive', task: Todo): void
  (e: 'delete', task: Todo): void
}>()

const isAnimating = ref(false)
const isRemoving = ref(false)

const priorityColor = computed(() => {
  switch (props.task.priority) {
    case 'urgent': return 'error'
    case 'high': return 'warning'
    case 'medium': return 'info'
    default: return 'grey'
  }
})

const priorityIcon = computed(() => {
  switch (props.task.priority) {
    case 'urgent': return '!!!'
    case 'high': return '!!'
    case 'medium': return '!'
    default: return '-'
  }
})

const dueDateColor = computed(() => {
  if (!props.task.dueDate) return 'default'
  const dueDate = new Date(props.task.dueDate)
  const now = new Date()
  const diffDays = Math.ceil((dueDate.getTime() - now.getTime()) / (1000 * 60 * 60 * 24))

  if (diffDays < 0) return 'error'
  if (diffDays === 0) return 'warning'
  if (diffDays <= 3) return 'info'
  return 'default'
})

function formatDate(dateString: string): string {
  const date = new Date(dateString)
  const now = new Date()
  const diffDays = Math.ceil((date.getTime() - now.getTime()) / (1000 * 60 * 60 * 24))

  if (diffDays === 0) return 'Heute'
  if (diffDays === 1) return 'Morgen'
  if (diffDays === -1) return 'Gestern'
  if (diffDays < -1) return `vor ${Math.abs(diffDays)} Tagen`
  if (diffDays <= 7) return `in ${diffDays} Tagen`

  return date.toLocaleDateString('de-DE', { day: '2-digit', month: '2-digit' })
}

async function handleToggle() {
  if (props.task.status !== 'completed') {
    // Animiere beim Erledigen
    isAnimating.value = true
    await new Promise(resolve => setTimeout(resolve, 300))
    isRemoving.value = true
    await new Promise(resolve => setTimeout(resolve, 100))
  }
  emit('toggle', props.task)
  isAnimating.value = false
  isRemoving.value = false
}

async function handleArchive() {
  isAnimating.value = true
  await new Promise(resolve => setTimeout(resolve, 300))
  isRemoving.value = true
  await new Promise(resolve => setTimeout(resolve, 100))
  emit('archive', props.task)
  isAnimating.value = false
  isRemoving.value = false
}
</script>

<style scoped>
.task-item {
  transition: all 0.3s ease;
  border-radius: 8px;
  margin-bottom: 4px;
}

.task-item:hover {
  background: rgba(var(--v-theme-primary), 0.05);
}

.task-completing {
  opacity: 0;
  transform: translateX(50px);
}
</style>
