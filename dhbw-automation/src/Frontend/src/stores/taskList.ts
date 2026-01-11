import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import api from '@/services/api'
import type { TodoList, Todo, TodoStats } from '@/types/tasks'
import { useAuthStore } from './auth'

export const useTaskListStore = defineStore('taskList', () => {
  // State
  const lists = ref<TodoList[]>([])
  const selectedListId = ref<number | null>(null)
  const tasks = ref<Todo[]>([])
  const archivedTasks = ref<Todo[]>([])
  const overdueTasks = ref<Todo[]>([])
  const stats = ref<TodoStats | null>(null)
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  // Computed
  const currentListTasks = computed(() => {
    if (selectedListId.value === null) {
      // "Alle" Liste - zeige alle Tasks außer archivierte
      return tasks.value.filter(t => !t.archivedAt)
    }
    return tasks.value.filter(t => t.listId === selectedListId.value && !t.archivedAt)
  })

  const pendingTasks = computed(() =>
    currentListTasks.value.filter(t => t.status === 'pending')
  )

  const completedTasks = computed(() =>
    currentListTasks.value.filter(t => t.status === 'completed')
  )

  const overdueCount = computed(() => overdueTasks.value.length)

  const selectedList = computed(() =>
    lists.value.find(l => l.id === selectedListId.value) || null
  )

  const regularLists = computed(() =>
    lists.value.filter(l => !l.isArchiveList)
  )

  const archiveList = computed(() =>
    lists.value.find(l => l.isArchiveList) || null
  )

  // Helper
  function getUserId(): number {
    const authStore = useAuthStore()
    return authStore.user?.id || 0
  }

  // Actions - Lists
  async function fetchLists() {
    const userId = getUserId()
    if (!userId) return

    isLoading.value = true
    error.value = null

    try {
      const response = await api.getTodoLists(userId)
      if (response.success) {
        lists.value = response.data
        // Initialisiere Standard-Listen falls keine existieren
        if (lists.value.length === 0) {
          await initializeLists()
        }
      }
    } catch (err: any) {
      error.value = err.message || 'Fehler beim Laden der Listen'
      console.error('Error fetching lists:', err)
    } finally {
      isLoading.value = false
    }
  }

  async function initializeLists() {
    const userId = getUserId()
    if (!userId) return

    try {
      const response = await api.initializeTodoLists(userId)
      if (response.success) {
        lists.value = response.data
      }
    } catch (err: any) {
      console.error('Error initializing lists:', err)
    }
  }

  async function createList(name: string, icon?: string, color?: string) {
    const userId = getUserId()
    if (!userId) return null

    isLoading.value = true
    error.value = null

    try {
      const response = await api.createTodoList({ userId, name, icon, color })
      if (response.success) {
        lists.value.push(response.data)
        return response.data
      }
      return null
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Fehler beim Erstellen der Liste'
      console.error('Error creating list:', err)
      return null
    } finally {
      isLoading.value = false
    }
  }

  async function updateList(listId: number, data: { name?: string; icon?: string; color?: string }) {
    const userId = getUserId()
    if (!userId) return false

    try {
      const response = await api.updateTodoList(listId, { userId, ...data })
      if (response.success) {
        const index = lists.value.findIndex(l => l.id === listId)
        if (index !== -1) {
          lists.value[index] = response.data
        }
        return true
      }
      return false
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Fehler beim Aktualisieren'
      console.error('Error updating list:', err)
      return false
    }
  }

  async function deleteList(listId: number) {
    const userId = getUserId()
    if (!userId) return false

    try {
      const response = await api.deleteTodoList(listId, userId)
      if (response.success) {
        lists.value = lists.value.filter(l => l.id !== listId)
        if (selectedListId.value === listId) {
          selectedListId.value = null
        }
        return true
      }
      return false
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Fehler beim Löschen'
      console.error('Error deleting list:', err)
      return false
    }
  }

  async function setDefaultList(listId: number) {
    const userId = getUserId()
    if (!userId) return false

    try {
      const response = await api.setDefaultList(listId, userId)
      if (response.success) {
        // Update all lists
        lists.value.forEach(l => {
          l.isDefault = l.id === listId
        })
        return true
      }
      return false
    } catch (err: any) {
      console.error('Error setting default list:', err)
      return false
    }
  }

  async function reorderLists(listIds: number[]) {
    const userId = getUserId()
    if (!userId) return false

    try {
      const response = await api.reorderLists(userId, listIds)
      if (response.success) {
        // Update sort order locally
        listIds.forEach((id, index) => {
          const list = lists.value.find(l => l.id === id)
          if (list) list.sortOrder = index
        })
        lists.value.sort((a, b) => a.sortOrder - b.sortOrder)
        return true
      }
      return false
    } catch (err: any) {
      console.error('Error reordering lists:', err)
      return false
    }
  }

  // Actions - Tasks
  async function fetchTasks(listId?: number) {
    const userId = getUserId()
    if (!userId) return

    isLoading.value = true
    error.value = null

    try {
      const options: any = {}
      if (listId !== undefined) {
        options.listId = listId
      }

      const response = await api.getTodos(userId, options)
      if (response.success) {
        tasks.value = response.data
      }
    } catch (err: any) {
      error.value = err.message || 'Fehler beim Laden der Aufgaben'
      console.error('Error fetching tasks:', err)
    } finally {
      isLoading.value = false
    }
  }

  async function fetchAllTasks() {
    const userId = getUserId()
    if (!userId) return

    isLoading.value = true
    try {
      const response = await api.getTodos(userId)
      if (response.success) {
        tasks.value = response.data
      }
    } catch (err: any) {
      console.error('Error fetching all tasks:', err)
    } finally {
      isLoading.value = false
    }
  }

  async function createTask(data: {
    title: string;
    description?: string;
    priority?: string;
    dueDate?: string;
    listId?: number
  }) {
    const userId = getUserId()
    if (!userId) return null

    try {
      const response = await api.createTodo({
        userId,
        listId: data.listId ?? selectedListId.value ?? undefined,
        title: data.title,
        description: data.description,
        priority: data.priority,
        dueDate: data.dueDate
      })

      if (response.success) {
        tasks.value.unshift(response.data)
        return response.data
      }
      return null
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Fehler beim Erstellen'
      console.error('Error creating task:', err)
      return null
    }
  }

  async function toggleTask(taskId: number) {
    const userId = getUserId()
    if (!userId) return false

    const task = tasks.value.find(t => t.id === taskId)
    if (!task) return false

    const newStatus = task.status === 'completed' ? 'pending' : 'completed'

    try {
      const response = await api.updateTodoStatus(taskId, userId, newStatus)
      if (response.success) {
        const index = tasks.value.findIndex(t => t.id === taskId)
        if (index !== -1) {
          tasks.value[index] = response.data
        }
        return true
      }
      return false
    } catch (err: any) {
      console.error('Error toggling task:', err)
      return false
    }
  }

  async function moveTask(taskId: number, targetListId?: number) {
    const userId = getUserId()
    if (!userId) return false

    try {
      const response = await api.moveTodo(taskId, userId, targetListId)
      if (response.success) {
        const index = tasks.value.findIndex(t => t.id === taskId)
        if (index !== -1) {
          tasks.value[index] = response.data
        }
        return true
      }
      return false
    } catch (err: any) {
      console.error('Error moving task:', err)
      return false
    }
  }

  async function archiveTask(taskId: number) {
    const userId = getUserId()
    if (!userId) return false

    try {
      const response = await api.archiveTodo(taskId, userId)
      if (response.success) {
        // Remove from active tasks
        tasks.value = tasks.value.filter(t => t.id !== taskId)
        // Add to archived
        archivedTasks.value.unshift(response.data)
        return true
      }
      return false
    } catch (err: any) {
      console.error('Error archiving task:', err)
      return false
    }
  }

  async function unarchiveTask(taskId: number, targetListId?: number) {
    const userId = getUserId()
    if (!userId) return false

    try {
      const response = await api.unarchiveTodo(taskId, userId, targetListId)
      if (response.success) {
        // Remove from archived
        archivedTasks.value = archivedTasks.value.filter(t => t.id !== taskId)
        // Add to active tasks
        tasks.value.unshift(response.data)
        return true
      }
      return false
    } catch (err: any) {
      console.error('Error unarchiving task:', err)
      return false
    }
  }

  async function deleteTask(taskId: number) {
    const userId = getUserId()
    if (!userId) return false

    try {
      const response = await api.deleteTodo(taskId, userId)
      if (response.success) {
        tasks.value = tasks.value.filter(t => t.id !== taskId)
        archivedTasks.value = archivedTasks.value.filter(t => t.id !== taskId)
        return true
      }
      return false
    } catch (err: any) {
      console.error('Error deleting task:', err)
      return false
    }
  }

  async function fetchArchivedTasks() {
    const userId = getUserId()
    if (!userId) return

    try {
      const response = await api.getArchivedTodos(userId)
      if (response.success) {
        archivedTasks.value = response.data
      }
    } catch (err: any) {
      console.error('Error fetching archived tasks:', err)
    }
  }

  async function fetchOverdueTasks(daysOld: number = 7) {
    const userId = getUserId()
    if (!userId) return

    try {
      const response = await api.getOverdueTodos(userId, daysOld)
      if (response.success) {
        overdueTasks.value = response.data
      }
    } catch (err: any) {
      console.error('Error fetching overdue tasks:', err)
    }
  }

  async function fetchStats() {
    const userId = getUserId()
    if (!userId) return

    try {
      const response = await api.getTodoStats(userId)
      if (response.success) {
        stats.value = response.data
      }
    } catch (err: any) {
      console.error('Error fetching stats:', err)
    }
  }

  function selectList(listId: number | null) {
    selectedListId.value = listId
  }

  function clearError() {
    error.value = null
  }

  return {
    // State
    lists,
    selectedListId,
    tasks,
    archivedTasks,
    overdueTasks,
    stats,
    isLoading,
    error,

    // Computed
    currentListTasks,
    pendingTasks,
    completedTasks,
    overdueCount,
    selectedList,
    regularLists,
    archiveList,

    // Actions - Lists
    fetchLists,
    createList,
    updateList,
    deleteList,
    setDefaultList,
    reorderLists,

    // Actions - Tasks
    fetchTasks,
    fetchAllTasks,
    createTask,
    toggleTask,
    moveTask,
    archiveTask,
    unarchiveTask,
    deleteTask,
    fetchArchivedTasks,
    fetchOverdueTasks,
    fetchStats,

    // Utilities
    selectList,
    clearError
  }
})
