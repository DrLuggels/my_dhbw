<template>
  <v-card flat class="quick-add-card mb-4">
    <v-card-text class="pa-3">
      <v-text-field
        v-model="newTaskTitle"
        placeholder="Neue Aufgabe hinzufuegen..."
        density="compact"
        variant="outlined"
        hide-details
        @keyup.enter="addTask"
        :loading="isAdding"
        :disabled="isAdding"
      >
        <template v-slot:prepend-inner>
          <v-icon size="small" color="grey">mdi-plus</v-icon>
        </template>
        <template v-slot:append-inner>
          <v-menu v-if="newTaskTitle.trim()">
            <template v-slot:activator="{ props }">
              <v-btn
                v-bind="props"
                icon="mdi-chevron-down"
                size="x-small"
                variant="text"
              />
            </template>
            <v-list density="compact">
              <v-list-subheader>Prioritaet</v-list-subheader>
              <v-list-item
                v-for="p in priorities"
                :key="p.value"
                @click="priority = p.value"
              >
                <template v-slot:prepend>
                  <v-icon :color="p.color" size="small">{{ p.icon }}</v-icon>
                </template>
                <v-list-item-title>{{ p.label }}</v-list-item-title>
                <template v-slot:append>
                  <v-icon v-if="priority === p.value" color="primary" size="small">
                    mdi-check
                  </v-icon>
                </template>
              </v-list-item>
            </v-list>
          </v-menu>
          <v-btn
            v-if="newTaskTitle.trim()"
            icon="mdi-send"
            size="small"
            color="primary"
            variant="text"
            @click="addTask"
            :loading="isAdding"
          />
        </template>
      </v-text-field>

      <!-- Priority indicator -->
      <v-chip
        v-if="priority !== 'medium'"
        size="x-small"
        :color="priorityInfo.color"
        class="mt-2"
        closable
        @click:close="priority = 'medium'"
      >
        {{ priorityInfo.label }}
      </v-chip>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'

const props = defineProps<{
  listId?: number | null
}>()

const emit = defineEmits<{
  (e: 'added', task: { title: string; priority: string; listId?: number }): void
}>()

const newTaskTitle = ref('')
const priority = ref<'low' | 'medium' | 'high' | 'urgent'>('medium')
const isAdding = ref(false)

const priorities = [
  { value: 'urgent', label: 'Dringend', color: 'error', icon: 'mdi-alert-circle' },
  { value: 'high', label: 'Hoch', color: 'warning', icon: 'mdi-arrow-up' },
  { value: 'medium', label: 'Mittel', color: 'info', icon: 'mdi-minus' },
  { value: 'low', label: 'Niedrig', color: 'grey', icon: 'mdi-arrow-down' }
]

const priorityInfo = computed(() =>
  priorities.find(p => p.value === priority.value) || priorities[2]
)

async function addTask() {
  const title = newTaskTitle.value.trim()
  if (!title) return

  isAdding.value = true

  try {
    emit('added', {
      title,
      priority: priority.value,
      listId: props.listId ?? undefined
    })

    // Reset form
    newTaskTitle.value = ''
    priority.value = 'medium'
  } finally {
    isAdding.value = false
  }
}
</script>

<style scoped>
.quick-add-card {
  background: rgba(var(--v-theme-surface), 0.8);
  border: 1px dashed rgba(var(--v-border-color), 0.3);
}

.quick-add-card:focus-within {
  border-color: rgb(var(--v-theme-primary));
  border-style: solid;
}
</style>
