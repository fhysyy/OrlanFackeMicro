import axios, { AxiosInstance, AxiosRequestConfig, AxiosResponse, AxiosError } from 'axios'
import { useAuthStore } from '@/stores/auth'
import type { ApiResponse, SystemStats, Message, FileInfo, Activity, PaginationParams, PaginationResponse } from '@/types/api'
import { ElMessage, ElMessageBox } from 'element-plus'
import { handleApiError } from '@/utils/errorHandler'
import { logger } from '@/utils/logger'
import { tokenManager } from '@/utils/tokenManager'

// 创建axios实例
const api: AxiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json'
  },
  withCredentials: true
})

// 请求拦截器
api.interceptors.request.use(
  (config: AxiosRequestConfig) => {
    // 使用 token 管理器获取认证头
    const authHeader = tokenManager.getAuthHeader()
    if (config.headers) {
      Object.assign(config.headers, authHeader)
    }
    
    return config
  },
  (error: AxiosError) => {
    logger.error('请求配置错误:', error)
    return Promise.reject(error)
  }
)

// 响应拦截器
api.interceptors.response.use(
  (response: AxiosResponse) => {
    const data = response.data as ApiResponse
    
    // 如果API返回失败状态
    if (data && typeof data === 'object' && 'success' in data && !data.success) {
      const error = new Error(data.message || '请求失败') as AxiosError & { response: AxiosResponse }
      error.response = response
      return Promise.reject(error)
    }
    
    return response
  },
  async (error: AxiosError) => {
    // 处理取消请求错误
    if (axios.isCancel(error)) {
      logger.debug('请求已取消:', error.message)
      return Promise.reject(error)
    }
    
    // 处理网络错误
    if (!error.response) {
      logger.error('网络错误:', error)
      await handleApiError(error, {
        showMessage: true,
        defaultMessage: '网络连接失败，请检查您的网络设置',
        critical: false
      })
      return Promise.reject(error)
    }
    
    if (error.response.status === 401) {
      // 清除认证状态
      tokenManager.clearTokens()
      
      if (!window.location.pathname.includes('/login')) {
        await ElMessage.warning('登录已过期，请重新登录')
        const currentPath = encodeURIComponent(window.location.pathname + window.location.search)
        window.location.href = `/login?redirect=${currentPath}`
      }
    } else {
      await handleApiError(error, {
        showMessage: true,
        defaultMessage: '请求处理失败',
        critical: false
      })
    }
    
    return Promise.reject(error)
  }
)

// 系统管理API
export const systemApi = {
  // 获取系统统计信息
  getStats: (): Promise<ApiResponse<SystemStats>> => {
    return api.get('/api/system/stats')
  },

  // 获取系统健康状态
  getHealth: (): Promise<ApiResponse<Record<string, any>>> => {
    return api.get('/api/system/health')
  },

  // 获取系统信息
  getInfo: (): Promise<ApiResponse<Record<string, any>>> => {
    return api.get('/api/system/info')
  }
}

// 消息管理API
export const messageApi = {
  // 获取消息列表
  getMessages: (params?: PaginationParams): Promise<ApiResponse<PaginationResponse<Message>>> => {
    return api.get('/api/messages', { params })
  },

  // 获取消息详情
  getMessage: (id: string): Promise<ApiResponse<Message>> => {
    return api.get(`/api/messages/${id}`)
  },

  // 创建消息
  createMessage: (data: Omit<Message, 'id' | 'createdAt' | 'updatedAt'>): Promise<ApiResponse<Message>> => {
    return api.post('/api/messages', data)
  },

  // 更新消息状态
  updateMessageStatus: (id: string, status: Message['status']): Promise<ApiResponse<void>> => {
    return api.patch(`/api/messages/${id}/status`, { status })
  },

  // 删除消息
  deleteMessage: (id: string): Promise<ApiResponse<void>> => {
    return api.delete(`/api/messages/${id}`)
  }
}

// 文件管理API
export const fileApi = {
  // 获取文件列表
  getFiles: (params?: PaginationParams): Promise<ApiResponse<PaginationResponse<FileInfo>>> => {
    return api.get('/api/files', { params })
  },

  // 获取文件详情
  getFile: (id: string): Promise<ApiResponse<FileInfo>> => {
    return api.get(`/api/files/${id}`)
  },

  // 上传文件
  uploadFile: (file: File, onProgress?: (progress: number) => void): Promise<ApiResponse<FileInfo>> => {
    const formData = new FormData()
    formData.append('file', file)
    
    return api.post('/api/files', formData, {
      headers: {
        'Content-Type': 'multipart/form-data'
      },
      onUploadProgress: (progressEvent) => {
        if (onProgress && progressEvent.total) {
          const progress = Math.round((progressEvent.loaded * 100) / progressEvent.total)
          onProgress(progress)
        }
      }
    })
  },

  // 下载文件
  downloadFile: (id: string): Promise<Blob> => {
    return api.get(`/api/files/${id}/download`, {
      responseType: 'blob'
    }).then(response => response.data)
  },

  // 删除文件
  deleteFile: (id: string): Promise<ApiResponse<void>> => {
    return api.delete(`/api/files/${id}`)
  }
}

// 活动日志API
export const activityApi = {
  // 获取活动日志
  getActivities: (params?: PaginationParams): Promise<ApiResponse<PaginationResponse<Activity>>> => {
    return api.get('/api/activities', { params })
  },

  // 创建活动日志
  createActivity: (data: Omit<Activity, 'id' | 'timestamp'>): Promise<ApiResponse<Activity>> => {
    return api.post('/api/activities', data)
  }
}

export { api }