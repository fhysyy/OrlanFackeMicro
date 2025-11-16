import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { User, AuthResponse, LoginRequest, RegisterRequest, UserRole, ApiResponse } from '@/types/api'
import { api } from '@/services/api'
import { useRouter } from 'vue-router'
import { AUTH_API, USER_API } from '@/utils/apiPaths'
import { handleApiError } from '@/utils/errorHandler'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(null)
  const token = ref<string | null>(localStorage.getItem('token'))
  const refreshToken = ref<string | null>(localStorage.getItem('refreshToken'))

  const isAuthenticated = computed(() => !!token.value && !!user.value)
  const userRole = computed(() => user.value?.role || null)

  // 登录
  const router = useRouter()
  
  const login = async (credentials: LoginRequest): Promise<AuthResponse> => {
    try {
      const response = await api.post<AuthResponse>(AUTH_API.LOGIN, credentials)
      
      if (response.data.success && response.data.token && response.data.user) {
        token.value = response.data.token
        refreshToken.value = response.data.refreshToken || null
        user.value = response.data.user
        
        // 存储到localStorage
        localStorage.setItem('token', token.value)
        if (refreshToken.value) {
          localStorage.setItem('refreshToken', refreshToken.value)
        }
        
        return response.data
      } else {
        throw new Error(response.data.errorMessage || '登录失败')
      }
    } catch (error: any) {
      await handleApiError(error, {
        defaultMessage: '登录失败',
        showMessage: true
      })
      throw error
    }
  }

  // 注册
  const register = async (userData: RegisterRequest): Promise<AuthResponse> => {
    try {
      const response = await api.post<AuthResponse>(AUTH_API.REGISTER, userData)
      return response.data
    } catch (error: any) {
      await handleApiError(error, {
        defaultMessage: '注册失败',
        showMessage: true
      })
      throw error
    }
  }

  // 刷新token
  const refresh = async (): Promise<boolean> => {
    if (!refreshToken.value) return false
    
    try {
      const response = await api.post<AuthResponse>(AUTH_API.REFRESH, {
        token: token.value,
        refreshToken: refreshToken.value
      })
      
      if (response.data.success && response.data.token) {
        token.value = response.data.token
        localStorage.setItem('token', token.value)
        return true
      }
    } catch (error) {
      console.error('Token刷新失败:', error)
    }
    
    return false
  }

  // 登出
  const logout = async (): Promise<void> => {
    try {
      await api.post(AUTH_API.LOGOUT)
    } catch (error) {
      console.error('登出失败:', error)
    } finally {
      // 清除状态
      user.value = null
      token.value = null
      refreshToken.value = null
      
      // 清除localStorage
      localStorage.removeItem('token')
      localStorage.removeItem('refreshToken')
      
      // 重定向到登录页
      router.push('/login')
    }
  }

  // 获取当前用户信息
  const fetchCurrentUser = async (): Promise<User | null> => {
    if (!token.value) return null
    
    try {
      // 根据项目API格式修改路径
      const response = await api.get<ApiResponse<User>>(USER_API.ME)
      user.value = response.data.data
      return user.value
    } catch (error) {
      console.error('获取用户信息失败:', error)
      // 即使失败也继续应用流程，避免阻塞其他功能
      return null
    }
  }

  // 检查权限
  const hasPermission = (requiredRole: string): boolean => {
    if (!user.value) return false
    
    const roleHierarchy = {
      [UserRole.User]: 1,
      [UserRole.Admin]: 2,
      [UserRole.SystemAdmin]: 3
    }
    
    const userRoleLevel = roleHierarchy[user.value.role as UserRole] || 0
    const requiredRoleLevel = roleHierarchy[requiredRole as UserRole] || 0
    
    return userRoleLevel >= requiredRoleLevel
  }

  // 初始化认证状态
  const initialize = async (): Promise<void> => {
    if (token.value) {
      try {
        // 尝试获取用户信息，但即使失败也继续执行
        await fetchCurrentUser()
      } catch (error) {
        console.warn('初始化用户信息时发生错误，但应用将继续运行:', error)
      }
    }
  }

  return {
    user,
    token,
    refreshToken,
    isAuthenticated,
    userRole,
    login,
    register,
    refresh,
    logout,
    fetchCurrentUser,
    hasPermission,
    initialize
  }
})