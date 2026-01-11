export interface TodoList {
  id: number
  userId: number
  name: string
  icon: string
  color: string
  sortOrder: number
  isDefault: boolean
  isArchiveList: boolean
  createdAt: string
  updatedAt?: string
  todoCount?: number
}

export interface Todo {
  id: number
  userId: number
  listId?: number
  title: string
  description?: string
  category: string
  priority: 'low' | 'medium' | 'high' | 'urgent'
  status: 'pending' | 'in_progress' | 'completed' | 'cancelled'
  dueDate?: string
  estimatedMinutes?: number
  archivedAt?: string
  autoDeleteAfterDays: number
  lastReminderSent?: string
  reminderCount: number
  parentTodoId?: number
  relatedKeywords?: string
  relatedDocumentId?: number
  relatedEventId?: number
  relatedProjectId?: number
  extractedFrom?: string
  aiSuggestion?: string
  createdAt: string
  completedAt?: string
}

export interface CreateTodoListRequest {
  userId: number
  name: string
  icon?: string
  color?: string
}

export interface UpdateTodoListRequest {
  userId: number
  name?: string
  icon?: string
  color?: string
}

export interface CreateTodoRequest {
  userId: number
  listId?: number
  title: string
  description?: string
  category?: string
  priority?: 'low' | 'medium' | 'high' | 'urgent'
  dueDate?: string
  estimatedMinutes?: number
}

export interface UpdateTodoRequest {
  userId: number
  title?: string
  description?: string
  category?: string
  priority?: string
  dueDate?: string
  estimatedMinutes?: number
}

export interface MoveTodoRequest {
  userId: number
  listId?: number
}

export interface TodoStats {
  total: number
  pending: number
  inProgress: number
  completed: number
  cancelled: number
  urgent: number
  overdue: number
}

export interface OverdueResponse {
  success: boolean
  data: Todo[]
  count: number
}

export type TodoListWithTodos = TodoList & {
  todos: Todo[]
}
