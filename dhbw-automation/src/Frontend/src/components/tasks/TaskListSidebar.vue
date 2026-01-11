<template>
  <v-navigation-drawer
    :model-value="true"
    :rail="rail"
    permanent
    class="task-sidebar"
  >
    <v-list density="compact" nav>
      <!-- Header -->
      <v-list-item class="mb-2">
        <template v-slot:prepend>
          <v-icon>mdi-format-list-checks</v-icon>
        </template>
        <v-list-item-title class="text-h6">Aufgaben</v-list-item-title>
        <template v-slot:append>
          <v-btn
            variant="text"
            icon="mdi-plus"
            size="small"
            @click="emit('create')"
            title="Neue Liste"
          />
        </template>
      </v-list-item>

      <v-divider class="mb-2" />

      <!-- Alle Aufgaben -->
      <v-list-item
        :active="selectedListId === null"
        @click="emit('select', null)"
        prepend-icon="mdi-view-list"
        title="Alle Aufgaben"
        :class="{ 'active-list': selectedListId === null }"
      >
        <template v-slot:append>
          <v-chip size="x-small" :color="totalCount > 0 ? 'primary' : 'default'">
            {{ totalCount }}
          </v-chip>
        </template>
      </v-list-item>

      <v-divider class="my-2" />

      <!-- Listen -->
      <draggable
        v-model="sortedLists"
        item-key="id"
        handle=".drag-handle"
        @end="handleReorder"
        :disabled="rail"
      >
        <template #item="{ element: list }">
          <v-list-item
            :key="list.id"
            :active="selectedListId === list.id"
            @click="emit('select', list.id)"
            :class="{ 'active-list': selectedListId === list.id }"
          >
            <template v-slot:prepend>
              <v-icon :color="list.color" class="drag-handle" style="cursor: grab">
                {{ list.icon }}
              </v-icon>
            </template>

            <v-list-item-title>{{ list.name }}</v-list-item-title>

            <template v-slot:append>
              <v-chip size="x-small" :color="list.todoCount > 0 ? list.color : 'default'">
                {{ list.todoCount || 0 }}
              </v-chip>
              <v-menu v-if="!list.isArchiveList">
                <template v-slot:activator="{ props }">
                  <v-btn
                    v-bind="props"
                    icon="mdi-dots-vertical"
                    size="x-small"
                    variant="text"
                    class="ml-1"
                    @click.stop
                  />
                </template>
                <v-list density="compact">
                  <v-list-item @click="emit('edit', list)">
                    <template v-slot:prepend>
                      <v-icon size="small">mdi-pencil</v-icon>
                    </template>
                    <v-list-item-title>Bearbeiten</v-list-item-title>
                  </v-list-item>
                  <v-list-item
                    v-if="!list.isDefault"
                    @click="emit('setDefault', list.id)"
                  >
                    <template v-slot:prepend>
                      <v-icon size="small">mdi-star</v-icon>
                    </template>
                    <v-list-item-title>Als Standard</v-list-item-title>
                  </v-list-item>
                  <v-divider v-if="!list.isDefault" />
                  <v-list-item
                    v-if="!list.isDefault"
                    @click="emit('delete', list.id)"
                    class="text-error"
                  >
                    <template v-slot:prepend>
                      <v-icon size="small" color="error">mdi-delete</v-icon>
                    </template>
                    <v-list-item-title>Loeschen</v-list-item-title>
                  </v-list-item>
                </v-list>
              </v-menu>
            </template>
          </v-list-item>
        </template>
      </draggable>

      <!-- Archiv -->
      <v-divider class="my-2" v-if="archiveList" />
      <v-list-item
        v-if="archiveList"
        :active="selectedListId === archiveList.id"
        @click="emit('select', archiveList.id)"
        :class="{ 'active-list': selectedListId === archiveList.id }"
      >
        <template v-slot:prepend>
          <v-icon :color="archiveList.color">{{ archiveList.icon }}</v-icon>
        </template>
        <v-list-item-title>{{ archiveList.name }}</v-list-item-title>
        <template v-slot:append>
          <v-chip size="x-small" color="grey">
            {{ archiveList.todoCount || 0 }}
          </v-chip>
        </template>
      </v-list-item>
    </v-list>

    <!-- Rail Toggle -->
    <template v-slot:append>
      <v-divider />
      <v-list-item @click="emit('toggleRail')">
        <template v-slot:prepend>
          <v-icon>{{ rail ? 'mdi-chevron-right' : 'mdi-chevron-left' }}</v-icon>
        </template>
        <v-list-item-title v-if="!rail">Minimieren</v-list-item-title>
      </v-list-item>
    </template>
  </v-navigation-drawer>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import draggable from 'vuedraggable'
import type { TodoList } from '@/types/tasks'

const props = defineProps<{
  lists: TodoList[]
  selectedListId: number | null
  rail?: boolean
}>()

const emit = defineEmits<{
  (e: 'select', listId: number | null): void
  (e: 'create'): void
  (e: 'edit', list: TodoList): void
  (e: 'delete', listId: number): void
  (e: 'setDefault', listId: number): void
  (e: 'reorder', listIds: number[]): void
  (e: 'toggleRail'): void
}>()

// Computed
const regularLists = computed(() =>
  props.lists.filter(l => !l.isArchiveList)
)

const archiveList = computed(() =>
  props.lists.find(l => l.isArchiveList)
)

const totalCount = computed(() =>
  props.lists.reduce((sum, l) => sum + (l.todoCount || 0), 0)
)

// Sortable lists for drag & drop
const sortedLists = ref<TodoList[]>([])

watch(() => regularLists.value, (newLists) => {
  sortedLists.value = [...newLists].sort((a, b) => a.sortOrder - b.sortOrder)
}, { immediate: true })

function handleReorder() {
  const listIds = sortedLists.value.map(l => l.id)
  emit('reorder', listIds)
}
</script>

<style scoped>
.task-sidebar {
  border-right: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
}

.active-list {
  background: rgba(var(--v-theme-primary), 0.1);
}

.drag-handle:hover {
  cursor: grab;
}
</style>
