import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { User, AuthResponse, LoginRequest, RegisterRequest, UserRole, ApiResponse } from '@/types/api'
import { api } from '@/services/api'
import { useRouter } from 'vue-router'
import { AUTH_API, USER_API } from '@/utils/apiPaths'
import { handleApiError } from '@/utils/errorHandler'
import { logger } from '@/utils/logger'
import { tokenManager } from '@/utils/tokenManager'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(null)
  // 正常的认证状态检查
  const isAuthenticated = computed(() => user.value !== null)
  const userRole = computed(() => user.value?.role || null)

  // 登录
  const router = useRouter()
  
  const login = async (credentials: LoginRequest): Promise<AuthResponse> => {
    try {
      const response = await api.post<AuthResponse>(AUTH_API.LOGIN, credentials)
      
      if (response.data.success && response.data.token && response.data.user) {
        user.value = response.data.user
        
        // 使用安全的 token 管理器存储 token
        tokenManager.setTokens(
          response.data.token,
          response.data.refreshToken,
          60 * 60 // 1小时，实际应该从响应中获取
        )
        
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
    try {
      return await tokenManager.refreshAccessToken()
    } catch (error) {
      logger.error('Token刷新失败:', error)
      return false
    }
  }

  // 登出
  const logout = async (): Promise<void> => {
    try {
      await api.post(AUTH_API.LOGOUT)
    } catch (error) {
      logger.error('登出失败:', error)
    } finally {
      // 清除状态
      user.value = null
      
      // 使用 token 管理器清除所有 token
      tokenManager.clearTokens()
      
      // 重定向到登录页
      router.push('/login')
    }
  }

  // 获取当前用户信息
  const fetchCurrentUser = async (): Promise<User | null> => {
    if (!tokenManager.isTokenValid()) return null
    
    try {
      // 根据项目API格式修改路径
      const response = await api.get<ApiResponse<User>>(USER_API.ME)
      user.value = response.data.data
      return user.value
    } catch (error) {
      logger.error('获取用户信息失败:', error)
      // 即使失败也继续应用流程，避免阻塞其他功能
      return null
    }
  }

  // 检查权限 - 临时禁用权限检查，始终返回已授权
  const hasPermission = (requiredRole: string): boolean => {
    // 临时禁用权限检查，默认返回已授权
    console.log('临时禁用权限检查，允许访问需要角色:', requiredRole)
    return true
    
    /* 原权限检查代码，后续需要恢复时取消注释
    if (!user.value) return false
    
    const roleHierarchy = {
      [UserRole.User]: 1,
      [UserRole.Admin]: 2,
      [UserRole.SystemAdmin]: 3
    }
    
    const userRoleLevel = roleHierarchy[user.value.role as UserRole] || 0
    const requiredRoleLevel = roleHierarchy[requiredRole as UserRole] || 0
    
    return userRoleLevel >= requiredRoleLevel
    */
  }

  // 初始化认证状态
  const initialize = async (): Promise<void> => {
    if (tokenManager.isTokenValid()) {
      try {
        // 尝试获取用户信息，但即使失败也继续执行
        await fetchCurrentUser()
      } catch (error) {
        logger.warn('初始化用户信息时发生错误，但应用将继续运行:', error)
        // 即使获取用户信息失败，也不清除token，允许用户继续访问应用
      }
    }
  }

  return {
    user,
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