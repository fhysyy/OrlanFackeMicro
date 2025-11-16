import axios, { AxiosInstance, AxiosRequestConfig, AxiosResponse, AxiosError } from 'axios'
import { useAuthStore } from '@/stores/auth'
import type { ApiResponse } from '@/types/api'
import { ElMessage, ElMessageBox } from 'element-plus'
import { handleApiError } from '@/utils/errorHandler'

// 创建axios实例
const api: AxiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '', // 使用相对路径，让Vite代理处理
  timeout: 30000, // 增加超时时间
  headers: {
    'Content-Type': 'application/json'
  },
  withCredentials: true // 允许携带凭证
})

// 请求拦截器
api.interceptors.request.use(
  (config: AxiosRequestConfig) => {
    const authStore = useAuthStore()
    if (authStore.token && config.headers) {
      config.headers.Authorization = `Bearer ${authStore.token}`
    }
    
    // 添加请求时间戳，用于调试和避免缓存
    if (config.params) {
      config.params._t = Date.now()
    } else {
      config.params = { _t: Date.now() }
    }
    
    // 取消重复请求
    if (api.cancelDuplicateRequest) {
      api.cancelDuplicateRequest(config)
    }
    
    return config
  },
  (error: AxiosError) => {
    console.error('请求配置错误:', error)
    return Promise.reject(error)
  }
)

// 响应拦截器
api.interceptors.response.use(
  (response: AxiosResponse) => {
    // 清理完成的请求
    if (api.cleanPendingRequest) {
      api.cleanPendingRequest(response.config)
    }
    
    // 统一处理响应数据格式
    const data = response.data as ApiResponse
    
    // 如果API返回失败状态
    if (data && typeof data === 'object' && 'success' in data && !data.success) {
      // 构建错误对象
      const error = new Error(data.message || '请求失败') as AxiosError & { response: AxiosResponse }
      error.response = response
      return Promise.reject(error)
    }
    
    return response
  },
  async (error: AxiosError) => {
    // 处理取消请求错误
    if (axios.isCancel(error)) {
      console.log('请求已取消:', error.message)
      return Promise.reject(error)
    }
    
    // 处理网络错误
    if (!error.response) {
      // 清理失败的请求
      if (error.config && api.cleanPendingRequest) {
        api.cleanPendingRequest(error.config)
      }
      
      console.error('网络错误:', error)
      await handleApiError(error, {
        showMessage: true,
        defaultMessage: '网络连接失败，请检查您的网络设置',
        critical: false
      })
      return Promise.reject(error)
    }
    
    // 清理已处理的请求
    if (api.cleanPendingRequest) {
      api.cleanPendingRequest(error.config as AxiosRequestConfig)
    }
    
    const authStore = useAuthStore()
    
    if (error.response.status === 401) {
      // 清除认证状态
      await authStore.logout()
      
      // 避免重复弹出登录提示或跳转到登录页
      if (!window.location.pathname.includes('/login')) {
        // 显示友好提示
        await ElMessage.warning('登录已过期，请重新登录')
        
        // 保存当前路径，登录后可以跳转回来
        const currentPath = encodeURIComponent(window.location.pathname + window.location.search)
        window.location.href = `/login?redirect=${currentPath}`
      }
    } else {
      // 处理其他HTTP错误
      await handleApiError(error, {
        showMessage: true,
        defaultMessage: '请求处理失败',
        critical: false
      })
    }
    
    return Promise.reject(error)
  }
)

// 添加请求取消功能
const pendingRequests = new Map<string, AbortController>()

// 生成请求标识
function generateRequestKey(config: AxiosRequestConfig): string {
  const { method, url, params, data } = config
  return `${method}-${url}-${JSON.stringify(params || {})}-${JSON.stringify(data || {})}`
}

// 添加取消重复请求的方法
api.cancelDuplicateRequest = (config: AxiosRequestConfig): void => {
  const requestKey = generateRequestKey(config)
  
  // 如果有相同的请求正在进行，取消它
  if (pendingRequests.has(requestKey)) {
    pendingRequests.get(requestKey)?.abort()
    pendingRequests.delete(requestKey)
  }
  
  // 创建新的取消控制器
  const controller = new AbortController()
  config.signal = controller.signal
  pendingRequests.set(requestKey, controller)
}

// 清理完成的请求
api.cleanPendingRequest = (config: AxiosRequestConfig): void => {
  const requestKey = generateRequestKey(config)
  if (pendingRequests.has(requestKey)) {
    pendingRequests.delete(requestKey)
  }
}

// 考试管理API
export const examApi = {
  // 获取考试列表
  getExams: (params?: any): Promise<ApiResponse<any>> => {
    return api.get('/api/exam', { params })
  },

  // 获取考试详情
  getExam: (id: number): Promise<ApiResponse<any>> => {
    return api.get(`/api/exam/${id}`)
  },

  // 创建考试
  createExam: (data: any): Promise<ApiResponse<any>> => {
    return api.post('/api/exam', data)
  },

  // 更新考试
  updateExam: (id: number, data: any): Promise<ApiResponse<any>> => {
    return api.put(`/api/exam/${id}`, data)
  },

  // 删除考试
  deleteExam: (id: number): Promise<ApiResponse<any>> => {
    return api.delete(`/api/exam/${id}`)
  },

  // 开始考试
  startExam: (id: number): Promise<ApiResponse<any>> => {
    return api.post(`/api/exam/${id}/start`)
  },

  // 结束考试
  endExam: (id: number): Promise<ApiResponse<any>> => {
    return api.post(`/api/exam/${id}/end`)
  },

  // 获取考试状态
  getExamStatus: (id: number): Promise<ApiResponse<any>> => {
    return api.get(`/api/exam/${id}/status`)
  },

  // 获取考试分析
  getExamAnalysis: (id: number): Promise<ApiResponse<any>> => {
    return api.get(`/api/exam/${id}/analysis`)
  },

  // 添加参与者
  addParticipant: (examId: number, studentId: number): Promise<ApiResponse<any>> => {
    return api.post(`/api/exam/${examId}/participants/${studentId}`)
  },

  // 移除参与者
  removeParticipant: (examId: number, studentId: number): Promise<ApiResponse<any>> => {
    return api.delete(`/api/exam/${examId}/participants/${studentId}`)
  },

  // 获取参与者列表
  getParticipants: (examId: number): Promise<ApiResponse<any>> => {
    return api.get(`/api/exam/${examId}/participants`)
  }
}

export { api }