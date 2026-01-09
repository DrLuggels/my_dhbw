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
    const formData = new FormData()
    formData.append('file', file)
    if (category) {
      formData.append('category', category)
    }

    const response = await this.api.post('/files/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data'
      }
    })
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

  // Health Check
  async healthCheck() {
    const response = await this.api.get('/health')
    return response.data
  }
}

export default new ApiService()
