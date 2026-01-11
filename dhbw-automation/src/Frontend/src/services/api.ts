import axios, { type AxiosInstance } from 'axios'

const API_URL = import.meta.env.VITE_API_URL || ''

class ApiService {
  private api: AxiosInstance

  constructor() {
    this.api = axios.create({
      baseURL: API_URL ? `${API_URL}/api` : '/api',
      headers: {
        'Content-Type': 'application/json'
      },
      withCredentials: true
    })

    // Request Interceptor für JWT Token
    this.api.interceptors.request.use(
      (config) => {
        const token = localStorage.getItem('authToken')
        if (token) {
          config.headers.Authorization = `Bearer ${token}`
        }

        // Remove Content-Type for FormData to let browser set it with boundary
        if (config.data instanceof FormData) {
          delete config.headers['Content-Type']
        }

        return config
      },
      (error) => {
        return Promise.reject(error)
      }
    )

    // Response Interceptor für Error Handling
    this.api.interceptors.response.use(
      (response) => response,
      (error) => {
        if (error.response?.status === 401) {
          localStorage.removeItem('authToken')
          localStorage.removeItem('user')
          window.location.href = '/login'
        }
        return Promise.reject(error)
      }
    )
  }

  // Auth Endpoints
  async login(email: string, password: string) {
    const response = await this.api.post('/auth/login', { email, password })
    return response.data
  }

  async register(email: string, password: string, firstName: string, lastName: string) {
    const response = await this.api.post('/auth/register', {
      email,
      password,
      firstName,
      lastName
    })
    return response.data
  }

  async changePassword(oldPassword: string, newPassword: string) {
    const response = await this.api.post('/auth/change-password', {
      oldPassword,
      newPassword
    })
    return response.data
  }

  // Files Endpoints
  async uploadFile(file: File, category?: string) {
    console.log('=== API.uploadFile DEBUG ===')
    console.log('Received file:', file)
    console.log('File instanceof File:', file instanceof File)
    console.log('File name:', file?.name)
    console.log('File size:', file?.size)
    console.log('File type:', file?.type)

    const formData = new FormData()
    formData.append('file', file)
    if (category) {
      formData.append('category', category)
    }

    console.log('FormData created')
    console.log('FormData has file:', formData.has('file'))
    console.log('FormData.get(file):', formData.get('file'))

    // Don't set Content-Type - let browser add it with boundary
    const response = await this.api.post('/files/upload', formData)
    return response.data
  }

  async downloadFile(fileId: number) {
    const response = await this.api.get(`/files/download/${fileId}`, {
      responseType: 'blob'
    })
    return response
  }

  async getFilesByCategory(category: string) {
    const response = await this.api.get(`/files/category/${category}`)
    return response.data
  }

  async deleteFile(fileId: number) {
    const response = await this.api.delete(`/files/${fileId}`)
    return response.data
  }

  async bulkDeleteFiles(fileIds: number[]) {
    const response = await this.api.post('/files/bulk-delete', fileIds)
    return response.data
  }

  // Direct API access methods
  get(url: string, config?: any) {
    return this.api.get(url, config)
  }

  post(url: string, data?: any, config?: any) {
    return this.api.post(url, data, config)
  }

  put(url: string, data?: any, config?: any) {
    return this.api.put(url, data, config)
  }

  patch(url: string, data?: any, config?: any) {
    return this.api.patch(url, data, config)
  }

  delete(url: string, config?: any) {
    return this.api.delete(url, config)
  }

  // Calendar Endpoints
  async syncRaplaCalendar(userId: number) {
    const response = await this.api.post(`/calendar/sync-rapla/${userId}`)
    return response.data
  }

  async getWeekSchedule(weekStart?: Date) {
    const params = weekStart ? { weekStart: weekStart.toISOString() } : {}
    const response = await this.api.get('/calendar/week-schedule', { params })
    return response.data
  }

  async getUserEvents(userId: number, startDate?: Date, endDate?: Date, source?: string) {
    const params: any = {}
    if (startDate) params.startDate = startDate.toISOString()
    if (endDate) params.endDate = endDate.toISOString()
    if (source) params.source = source

    const response = await this.api.get(`/calendar/events/${userId}`, { params })
    return response.data
  }

  async testRaplaConnection() {
    const response = await this.api.get('/calendar/test-rapla')
    return response.data
  }

  async updateEventNotes(eventId: number, notes: string) {
    const response = await this.api.patch(`/calendar/${eventId}/notes`, { notes })
    return response.data
  }

  // ==================== TodoList Endpoints ====================

  async getTodoLists(userId: number) {
    const response = await this.api.get(`/todolist/user/${userId}`)
    return response.data
  }

  async getTodoList(listId: number, userId: number) {
    const response = await this.api.get(`/todolist/${listId}`, { params: { userId } })
    return response.data
  }

  async createTodoList(data: { userId: number; name: string; icon?: string; color?: string }) {
    const response = await this.api.post('/todolist', data)
    return response.data
  }

  async updateTodoList(listId: number, data: { userId: number; name?: string; icon?: string; color?: string }) {
    const response = await this.api.put(`/todolist/${listId}`, data)
    return response.data
  }

  async deleteTodoList(listId: number, userId: number) {
    const response = await this.api.delete(`/todolist/${listId}`, { params: { userId } })
    return response.data
  }

  async setDefaultList(listId: number, userId: number) {
    const response = await this.api.patch(`/todolist/${listId}/default`, { userId })
    return response.data
  }

  async reorderLists(userId: number, listIds: number[]) {
    const response = await this.api.post('/todolist/reorder', { userId, listIds })
    return response.data
  }

  async initializeTodoLists(userId: number) {
    const response = await this.api.post(`/todolist/initialize/${userId}`)
    return response.data
  }

  // ==================== Extended Todo Endpoints ====================

  async getTodos(userId: number, options?: { listId?: number; status?: string; includeArchived?: boolean }) {
    const params: any = {}
    if (options?.listId) params.listId = options.listId
    if (options?.status) params.status = options.status
    if (options?.includeArchived) params.includeArchived = options.includeArchived

    const response = await this.api.get(`/todo/user/${userId}`, { params })
    return response.data
  }

  async getTodosByList(listId: number, userId: number) {
    const response = await this.api.get(`/todo/list/${listId}`, { params: { userId } })
    return response.data
  }

  async createTodo(data: {
    userId: number;
    listId?: number;
    title: string;
    description?: string;
    priority?: string;
    dueDate?: string
  }) {
    const response = await this.api.post('/todo', data)
    return response.data
  }

  async updateTodoStatus(todoId: number, userId: number, status: string) {
    const response = await this.api.patch(`/todo/${todoId}/status`, { userId, status })
    return response.data
  }

  async moveTodo(todoId: number, userId: number, listId?: number) {
    const response = await this.api.patch(`/todo/${todoId}/move`, { userId, listId })
    return response.data
  }

  async archiveTodo(todoId: number, userId: number) {
    const response = await this.api.post(`/todo/${todoId}/archive`, null, { params: { userId } })
    return response.data
  }

  async unarchiveTodo(todoId: number, userId: number, targetListId?: number) {
    const params: any = { userId }
    if (targetListId) params.targetListId = targetListId
    const response = await this.api.post(`/todo/${todoId}/unarchive`, null, { params })
    return response.data
  }

  async getArchivedTodos(userId: number) {
    const response = await this.api.get(`/todo/user/${userId}/archived`)
    return response.data
  }

  async getOverdueTodos(userId: number, daysOld: number = 7) {
    const response = await this.api.get(`/todo/user/${userId}/overdue`, { params: { daysOld } })
    return response.data
  }

  async getRelatedTodos(todoId: number, userId: number) {
    const response = await this.api.get(`/todo/${todoId}/related`, { params: { userId } })
    return response.data
  }

  async getTodoStats(userId: number) {
    const response = await this.api.get(`/todo/user/${userId}/stats`)
    return response.data
  }

  async deleteTodo(todoId: number, userId: number) {
    const response = await this.api.delete(`/todo/${todoId}`, { params: { userId } })
    return response.data
  }

  // Health Check
  async healthCheck() {
    const response = await this.api.get('/health')
    return response.data
  }
}

export default new ApiService()
