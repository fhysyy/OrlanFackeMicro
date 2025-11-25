import axios, { AxiosInstance, AxiosRequestConfig, AxiosResponse, AxiosError } from 'axios'
import { useAuthStore } from '@/stores/auth'
import type { ApiResponse, Exam, ExamParticipant, PaginationParams, PaginationResponse, ExamQuestion } from '@/types/api'
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

// 考试管理API
export const examApi = {
  // 获取考试列表
  getExams: (params?: PaginationParams): Promise<ApiResponse<PaginationResponse<Exam>>> => {
    return api.get('/api/exam', { params })
  },

  // 获取考试详情
  getExam: (id: string): Promise<ApiResponse<Exam>> => {
    return api.get(`/api/exam/${id}`)
  },

  // 创建考试
  createExam: (data: Omit<Exam, 'id' | 'createdAt' | 'updatedAt'>): Promise<ApiResponse<Exam>> => {
    return api.post('/api/exam', data)
  },

  // 更新考试
  updateExam: (id: string, data: Partial<Exam>): Promise<ApiResponse<Exam>> => {
    return api.put(`/api/exam/${id}`, data)
  },

  // 删除考试
  deleteExam: (id: string): Promise<ApiResponse<void>> => {
    return api.delete(`/api/exam/${id}`)
  },

  // 开始考试
  startExam: (id: string): Promise<ApiResponse<void>> => {
    return api.post(`/api/exam/${id}/start`)
  },

  // 结束考试
  endExam: (id: string): Promise<ApiResponse<void>> => {
    return api.post(`/api/exam/${id}/end`)
  },

  // 获取考试状态
  getExamStatus: (id: string): Promise<ApiResponse<Exam['status']>> => {
    return api.get(`/api/exam/${id}/status`)
  },

  // 获取考试分析
  getExamAnalysis: (id: string): Promise<ApiResponse<Record<string, unknown>>> => {
    return api.get(`/api/exam/${id}/analysis`)
  },

  // 添加参与者
  addParticipant: (examId: string, studentId: string): Promise<ApiResponse<ExamParticipant>> => {
    return api.post(`/api/exam/${examId}/participants/${studentId}`)
  },

  // 移除参与者
  removeParticipant: (examId: string, studentId: string): Promise<ApiResponse<void>> => {
    return api.delete(`/api/exam/${examId}/participants/${studentId}`)
  },

  // 获取参与者列表
  getParticipants: (examId: string): Promise<ApiResponse<ExamParticipant[]>> => {
    return api.get(`/api/exam/${examId}/participants`)
  }
}

export { api }