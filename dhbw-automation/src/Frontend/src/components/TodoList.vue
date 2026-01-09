<template>
  <v-card>
    <v-card-title class="d-flex align-center">
      <v-icon left>mdi-checkbox-marked-circle-outline</v-icon>
      <span class="text-h6">Aufgaben</span>
      <v-spacer />
      <v-chip :color="getStatusColor()" size="small">
        {{ pendingCount }} offen
      </v-chip>
    </v-card-title>

    <v-card-text>
      <div v-if="loading" class="text-center py-4">
        <v-progress-circular indeterminate color="primary" />
      </div>

      <div v-else-if="todos.length === 0" class="text-center py-8">
        <v-icon size="64" color="grey-lighten-1">mdi-checkbox-marked-circle</v-icon>
        <p class="text-body-1 mt-4">Keine Aufgaben vorhanden</p>
      </div>

      <v-list v-else>
        <v-list-item
          v-for="todo in sortedTodos"
          :key="todo.id"
          :class="{ 'completed-todo': todo.status === 'completed' }"
          class="todo-item"
        >
          <template v-slot:prepend>
            <v-checkbox
              :model-value="todo.status === 'completed'"
              @update:model-value="toggleTodo(todo)"
              hide-details
              color="primary"
            />
          </template>

          <v-list-item-title :class="{ 'text-decoration-line-through': todo.status === 'completed' }">
            {{ todo.title }}
          </v-list-item-title>

          <v-list-item-subtitle v-if="todo.dueDate || todo.description">
            <span v-if="todo.dueDate">
              <v-icon size="small">mdi-calendar</v-icon>
              {{ formatDate(todo.dueDate) }}
            </span>
            <span v-if="todo.description" class="ml-2">
              {{ todo.description.substring(0, 50) }}{{ todo.description.length > 50 ? '...' : '' }}
            </span>
          </v-list-item-subtitle>

          <template v-slot:append>
            <v-chip
              :color="getPriorityColor(todo.priority)"
              size="small"
              variant="tonal"
            >
              {{ getPriorityLabel(todo.priority) }}
            </v-chip>
          </template>
        </v-list-item>
      </v-list>
    </v-card-text>

    <v-card-actions>
      <v-btn
        block
        variant="text"
        color="primary"
        @click="loadTodos"
      >
        <v-icon left>mdi-refresh</v-icon>
        Aktualisieren
      </v-btn>
    </v-card-actions>
  </v-card>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import axios from 'axios'
import { useAuthStore } from '@/stores/auth'

interface Todo {
  id: number
  userId: number
  title: string
  description?: string
  category: string
  priority: string
  status: string
  dueDate?: string
  estimatedMinutes?: number
  createdAt: string
}

const authStore = useAuthStore()
const todos = ref<Todo[]>([])
const loading = ref(false)

const sortedTodos = computed(() => {
  return [...todos.value].sort((a, b) => {
    // Completed tasks at the bottom
    if (a.status !== b.status) {
      return a.status === 'completed' ? 1 : -1
    }

    // Sort by priority
    const priorityOrder: Record<string, number> = { urgent: 0, high: 1, medium: 2, low: 3 }
    const aPrio = priorityOrder[a.priority] ?? 999
    const bPrio = priorityOrder[b.priority] ?? 999

    if (aPrio !== bPrio) return aPrio - bPrio

    // Sort by due date
    if (a.dueDate && b.dueDate) {
      return new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime()
    }

    return 0
  })
})

const pendingCount = computed(() => {
  return todos.value.filter(t => t.status === 'pending' || t.status === 'in_progress').length
})

const loadTodos = async () => {
  if (!authStore.user?.id) return

  loading.value = true
  try {
    const response = await axios.get(`/api/todo/user/${authStore.user.id}`, {
      params: {
        status: null // Get all todos
      }
    })

    if (response.data.success) {
      todos.value = response.data.data
    }
  } catch (error) {
    console.error('Error loading todos:', error)
    alert('Fehler beim Laden der Aufgaben')
  } finally {
    loading.value = false
  }
}

const toggleTodo = async (todo: Todo) => {
  if (!authStore.user?.id) return

  const newStatus = todo.status === 'completed' ? 'pending' : 'completed'

  try {
    await axios.patch(`/api/todo/${todo.id}/status`, {
      userId: authStore.user.id,
      status: newStatus
    })

    todo.status = newStatus
  } catch (error) {
    console.error('Error toggling todo:', error)
    alert('Fehler beim Aktualisieren der Aufgabe')
    // Reload to restore state
    await loadTodos()
  }
}

const getPriorityColor = (priority: string) => {
  const colors: Record<string, string> = {
    urgent: 'error',
    high: 'warning',
    medium: 'info',
    low: 'success'
  }
  return colors[priority] || 'default'
}

const getPriorityLabel = (priority: string) => {
  const labels: Record<string, string> = {
    urgent: '🔥',
    high: '⬆️',
    medium: '➡️',
    low: '⬇️'
  }
  return labels[priority] || priority
}

const getStatusColor = () => {
  const count = pendingCount.value
  if (count === 0) return 'success'
  if (count < 3) return 'info'
  if (count < 5) return 'warning'
  return 'error'
}

const formatDate = (date: string) => {
  return new Date(date).toLocaleDateString('de-DE', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric'
  })
}

onMounted(() => {
  loadTodos()
})
</script>

<style scoped>
.todo-item {
  transition: all 0.2s;
}

.todo-item:hover {
  background-color: rgba(var(--v-theme-primary), 0.05);
}

.completed-todo {
  opacity: 0.6;
}
</style>
