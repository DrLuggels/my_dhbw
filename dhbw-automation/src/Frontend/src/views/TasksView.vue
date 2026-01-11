<template>
  <v-container fluid class="tasks-view pa-0 fill-height">
    <v-row no-gutters class="fill-height">
      <!-- Sidebar -->
      <v-col cols="auto">
        <TaskListSidebar
          :lists="taskListStore.lists"
          :selected-list-id="taskListStore.selectedListId"
          :rail="sidebarRail"
          @select="handleSelectList"
          @create="showCreateListDialog = true"
          @edit="handleEditList"
          @delete="handleDeleteList"
          @set-default="handleSetDefault"
          @reorder="handleReorderLists"
          @toggle-rail="sidebarRail = !sidebarRail"
        />
      </v-col>

      <!-- Main Content -->
      <v-col class="main-content pa-4">
        <!-- Header -->
        <div class="d-flex align-center mb-4">
          <div>
            <h1 class="text-h5">
              {{ currentListName }}
            </h1>
            <p class="text-body-2 text-grey">
              {{ taskListStore.pendingTasks.length }} offen
              <span v-if="taskListStore.completedTasks.length > 0">
                , {{ taskListStore.completedTasks.length }} erledigt
              </span>
            </p>
          </div>
          <v-spacer />
          <v-btn
            icon="mdi-refresh"
            variant="text"
            @click="refreshData"
            :loading="taskListStore.isLoading"
          />
        </div>

        <!-- Reminder Banner -->
        <ReminderBanner
          :count="taskListStore.overdueCount"
          @dismiss="dismissReminder"
          @schedule="showScheduleDialog = true"
        />

        <!-- Quick Add -->
        <QuickAddTask
          :list-id="taskListStore.selectedListId"
          @added="handleAddTask"
        />

        <!-- Tasks List -->
        <v-card flat>
          <v-list v-if="taskListStore.currentListTasks.length > 0">
            <TaskItem
              v-for="task in sortedTasks"
              :key="task.id"
              :task="task"
              @toggle="handleToggleTask"
              @click="handleEditTask"
              @edit="handleEditTask"
              @move="handleMoveTask"
              @archive="handleArchiveTask"
              @delete="handleDeleteTask"
            />
          </v-list>

          <v-card-text v-else class="text-center py-8">
            <v-icon size="64" color="grey-lighten-1">mdi-clipboard-check-outline</v-icon>
            <p class="text-h6 text-grey mt-4">Keine Aufgaben</p>
            <p class="text-body-2 text-grey">
              Fuege eine neue Aufgabe hinzu, um loszulegen.
            </p>
          </v-card-text>
        </v-card>

        <!-- Loading Overlay -->
        <v-overlay
          :model-value="taskListStore.isLoading"
          contained
          class="align-center justify-center"
        >
          <v-progress-circular indeterminate color="primary" />
        </v-overlay>
      </v-col>
    </v-row>

    <!-- Dialogs -->
    <CreateListDialog
      v-model="showCreateListDialog"
      @created="handleCreateList"
    />

    <MoveTaskDialog
      v-model="showMoveTaskDialog"
      :task="selectedTask"
      :lists="taskListStore.lists"
      @moved="handleTaskMoved"
    />

    <!-- Edit List Dialog -->
    <v-dialog v-model="showEditListDialog" max-width="400">
      <v-card v-if="editingList">
        <v-card-title>Liste bearbeiten</v-card-title>
        <v-card-text>
          <v-text-field
            v-model="editingList.name"
            label="Name"
            variant="outlined"
          />
          <v-select
            v-model="editingList.icon"
            :items="iconOptions"
            label="Icon"
            variant="outlined"
          >
            <template v-slot:item="{ props, item }">
              <v-list-item v-bind="props">
                <template v-slot:prepend>
                  <v-icon>{{ item.value }}</v-icon>
                </template>
              </v-list-item>
            </template>
          </v-select>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showEditListDialog = false">Abbrechen</v-btn>
          <v-btn color="primary" @click="saveEditList">Speichern</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Delete Confirm Dialog -->
    <v-dialog v-model="showDeleteConfirm" max-width="400">
      <v-card>
        <v-card-title>Liste loeschen?</v-card-title>
        <v-card-text>
          Die Aufgaben werden nicht geloescht, sondern nur von der Liste entfernt.
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showDeleteConfirm = false">Abbrechen</v-btn>
          <v-btn color="error" @click="confirmDeleteList">Loeschen</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Snackbar for feedback -->
    <v-snackbar v-model="snackbar.show" :color="snackbar.color" :timeout="3000">
      {{ snackbar.message }}
    </v-snackbar>
  </v-container>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useTaskListStore } from '@/stores/taskList'
import TaskListSidebar from '@/components/tasks/TaskListSidebar.vue'
import TaskItem from '@/components/tasks/TaskItem.vue'
import QuickAddTask from '@/components/tasks/QuickAddTask.vue'
import ReminderBanner from '@/components/tasks/ReminderBanner.vue'
import CreateListDialog from '@/components/tasks/CreateListDialog.vue'
import MoveTaskDialog from '@/components/tasks/MoveTaskDialog.vue'
import type { Todo, TodoList } from '@/types/tasks'

const taskListStore = useTaskListStore()

// UI State
const sidebarRail = ref(false)
const showCreateListDialog = ref(false)
const showEditListDialog = ref(false)
const showMoveTaskDialog = ref(false)
const showDeleteConfirm = ref(false)
const showScheduleDialog = ref(false)

const editingList = ref<TodoList | null>(null)
const selectedTask = ref<Todo | null>(null)
const listToDelete = ref<number | null>(null)

const snackbar = ref({
  show: false,
  message: '',
  color: 'success'
})

const iconOptions = [
  { title: 'Standard', value: 'mdi-checkbox-marked-circle-outline' },
  { title: 'Arbeit', value: 'mdi-briefcase' },
  { title: 'Studium', value: 'mdi-school' },
  { title: 'Einkauf', value: 'mdi-cart' },
  { title: 'Sport', value: 'mdi-run' },
  { title: 'Zuhause', value: 'mdi-home' },
  { title: 'Reise', value: 'mdi-airplane' },
  { title: 'Gesundheit', value: 'mdi-heart' },
  { title: 'Finanzen', value: 'mdi-currency-eur' },
  { title: 'Hobby', value: 'mdi-palette' },
  { title: 'Familie', value: 'mdi-account-group' },
  { title: 'Projekt', value: 'mdi-folder' },
  { title: 'Kirche', value: 'mdi-church' },
  { title: 'Auto', value: 'mdi-car' }
]

// Computed
const currentListName = computed(() => {
  if (taskListStore.selectedListId === null) return 'Alle Aufgaben'
  return taskListStore.selectedList?.name || 'Aufgaben'
})

const sortedTasks = computed(() => {
  const tasks = [...taskListStore.currentListTasks]

  // Sort: pending first, then by priority, then by due date
  return tasks.sort((a, b) => {
    // Status: pending/in_progress before completed
    if (a.status === 'completed' && b.status !== 'completed') return 1
    if (a.status !== 'completed' && b.status === 'completed') return -1

    // Priority: urgent > high > medium > low
    const priorityOrder = { urgent: 0, high: 1, medium: 2, low: 3 }
    const priorityDiff = priorityOrder[a.priority] - priorityOrder[b.priority]
    if (priorityDiff !== 0) return priorityDiff

    // Due date: earlier first
    if (a.dueDate && b.dueDate) {
      return new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime()
    }
    if (a.dueDate) return -1
    if (b.dueDate) return 1

    // Created at: newer first
    return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
  })
})

// Lifecycle
onMounted(async () => {
  await taskListStore.fetchLists()
  await taskListStore.fetchAllTasks()
  await taskListStore.fetchOverdueTasks()
})

// Watch for list changes to refresh tasks
watch(() => taskListStore.selectedListId, async () => {
  await taskListStore.fetchAllTasks()
})

// Methods
async function refreshData() {
  await taskListStore.fetchLists()
  await taskListStore.fetchAllTasks()
  await taskListStore.fetchOverdueTasks()
}

function handleSelectList(listId: number | null) {
  taskListStore.selectList(listId)
}

async function handleCreateList(data: { name: string; icon: string; color: string }) {
  const result = await taskListStore.createList(data.name, data.icon, data.color)
  if (result) {
    showMessage('Liste erstellt', 'success')
  } else {
    showMessage(taskListStore.error || 'Fehler beim Erstellen', 'error')
  }
}

function handleEditList(list: TodoList) {
  editingList.value = { ...list }
  showEditListDialog.value = true
}

async function saveEditList() {
  if (!editingList.value) return

  const success = await taskListStore.updateList(editingList.value.id, {
    name: editingList.value.name,
    icon: editingList.value.icon,
    color: editingList.value.color
  })

  if (success) {
    showMessage('Liste aktualisiert', 'success')
    showEditListDialog.value = false
  } else {
    showMessage('Fehler beim Speichern', 'error')
  }
}

function handleDeleteList(listId: number) {
  listToDelete.value = listId
  showDeleteConfirm.value = true
}

async function confirmDeleteList() {
  if (!listToDelete.value) return

  const success = await taskListStore.deleteList(listToDelete.value)
  if (success) {
    showMessage('Liste geloescht', 'success')
  } else {
    showMessage('Fehler beim Loeschen', 'error')
  }

  showDeleteConfirm.value = false
  listToDelete.value = null
}

async function handleSetDefault(listId: number) {
  await taskListStore.setDefaultList(listId)
  showMessage('Standard-Liste gesetzt', 'success')
}

async function handleReorderLists(listIds: number[]) {
  await taskListStore.reorderLists(listIds)
}

async function handleAddTask(data: { title: string; priority: string; listId?: number }) {
  const result = await taskListStore.createTask({
    title: data.title,
    priority: data.priority,
    listId: data.listId
  })

  if (result) {
    showMessage('Aufgabe erstellt', 'success')
  } else {
    showMessage('Fehler beim Erstellen', 'error')
  }
}

async function handleToggleTask(task: Todo) {
  const success = await taskListStore.toggleTask(task.id)
  if (success && task.status !== 'completed') {
    showMessage('Erledigt!', 'success')
  }
}

function handleEditTask(task: Todo) {
  // TODO: Open edit dialog
  console.log('Edit task:', task)
}

function handleMoveTask(task: Todo) {
  selectedTask.value = task
  showMoveTaskDialog.value = true
}

async function handleTaskMoved(taskId: number, targetListId: number) {
  const success = await taskListStore.moveTask(taskId, targetListId)
  if (success) {
    showMessage('Aufgabe verschoben', 'success')
  }
}

async function handleArchiveTask(task: Todo) {
  const success = await taskListStore.archiveTask(task.id)
  if (success) {
    showMessage('Aufgabe archiviert', 'info')
  }
}

async function handleDeleteTask(task: Todo) {
  const success = await taskListStore.deleteTask(task.id)
  if (success) {
    showMessage('Aufgabe geloescht', 'success')
  }
}

function dismissReminder() {
  // Just hides the banner
}

function showMessage(message: string, color: string) {
  snackbar.value = { show: true, message, color }
}
</script>

<style scoped>
.tasks-view {
  background: rgb(var(--v-theme-background));
}

.main-content {
  max-width: 800px;
  position: relative;
}
</style>
