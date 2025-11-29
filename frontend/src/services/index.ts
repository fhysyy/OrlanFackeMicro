// API 服务主文件
export { api } from './api'

// 服务导出
export * from './dictionaryService'
export * from './fileService'
export * from './roleService'
export * from './permissionService'

// 用户服务
import { api } from './api'
import type { User, PaginatedResponse, ApiResponse, UserRole, UserStatus, Role, Permission, PermissionGroup } from '@/types/api'
import { USER_API, ROLE_API, PERMISSION_API } from '@/utils/apiPaths'

export const userService = {
  // 获取用户列表
  getUsers: (params: {
    page?: number
    size?: number
    keyword?: string
    role?: UserRole
    status?: UserStatus
  } = {}) => {
    return api.get<ApiResponse<User[]>>(USER_API.LIST, { params })
  },

  // 获取用户详情
  getUser: (id: string) => {
    return api.get<ApiResponse<User>>(USER_API.GET(id))
  },

  // 创建用户
  createUser: (data: {
    username: string
    email: string
    password: string
    phone?: string
    role: UserRole
  }) => {
    return api.post<ApiResponse<User>>(USER_API.CREATE, data)
  },

  // 更新用户
  updateUser: (id: string, data: Partial<{
    email: string
    phone: string
    role: UserRole
    status: UserStatus
  }>) => {
    return api.put<ApiResponse<User>>(USER_API.UPDATE(id), data)
  },

  // 删除用户
  deleteUser: (id: string) => {
    return api.delete<ApiResponse<void>>(USER_API.DELETE(id))
  },

  // 启用/禁用用户
  toggleUserStatus: (id: string, enabled: boolean) => {
    return api.patch<ApiResponse<User>>(`/api/Admin/user/${id}/status`, { enabled })
  },

  // 重置用户密码
  resetPassword: (id: string, newPassword: string) => {
    return api.patch<ApiResponse<void>>(`/api/Admin/user/${id}/password`, { newPassword })
  }
}

// 认证服务
export const authService = {
  // 用户登录
  login: (data: { username: string; password: string; rememberMe?: boolean }) => {
    return api.post<ApiResponse<{ token: string; user: User }>>('/api/Auth/login', data)
  },

  // 用户注册
  register: (data: {
    username: string
    email: string
    password: string
    confirmPassword: string
    phone?: string
  }) => {
    return api.post<ApiResponse<User>>('/api/Auth/register', data)
  },

  // 刷新 Token
  refreshToken: () => {
    return api.post<ApiResponse<{ token: string }>>('/api/Auth/refresh')
  },

  // 退出登录
  logout: () => {
    return api.post<ApiResponse<void>>('/api/Auth/logout')
  },

  // 获取当前用户信息
  getCurrentUser: () => {
    return api.get<ApiResponse<User>>('/api/Auth/me')
  },

  // 修改密码
  changePassword: (data: {
    currentPassword: string
    newPassword: string
    confirmPassword: string
  }) => {
    return api.post<ApiResponse<void>>('/api/Auth/change-password', data)
  },

  // 忘记密码
  forgotPassword: (email: string) => {
    return api.post<ApiResponse<void>>('/api/Auth/forgot-password', { email })
  },

  // 重置密码
  resetPassword: (token: string, newPassword: string) => {
    return api.post<ApiResponse<void>>('/api/Auth/reset-password', {
      token,
      newPassword
    })
  }
}

// 消息服务
export const messageService = {
  // 获取消息列表
  getMessages: (params: {
    page?: number
    size?: number
    type?: string
    status?: string
    keyword?: string
  } = {}) => {
    return api.get<PaginatedResponse<any[]>>('/api/messages', { params })
  },

  // 获取消息详情
  getMessage: (id: string) => {
    return api.get<ApiResponse<any>>(`/api/messages/${id}`)
  },

  // 发送消息
  sendMessage: (data: {
    title: string
    content: string
    type: string
    receiverIds: string[]
  }) => {
    return api.post<ApiResponse<any>>('/api/messages', data)
  },

  // 批量发送消息
  sendBatchMessages: (data: {
    templateId: string
    userGroups: string[]
    variables?: Record<string, any>
  }) => {
    return api.post<ApiResponse<any>>('/api/messages/batch', data)
  },

  // 删除消息
  deleteMessage: (id: string) => {
    return api.delete<ApiResponse<void>>(`/api/messages/${id}`)
  },

  // 标记消息为已读
  markAsRead: (id: string) => {
    return api.patch<ApiResponse<any>>(`/api/messages/${id}/read`)
  },

  // 获取消息模板
  getTemplates: () => {
    return api.get<ApiResponse<any[]>>('/api/messages/templates')
  },

  // 创建消息模板
  createTemplate: (data: {
    name: string
    content: string
    type: string
    variables: string[]
  }) => {
    return api.post<ApiResponse<any>>('/api/messages/templates', data)
  }
}

// 文件服务 - 已从fileService.ts导入

// 系统服务
export const systemService = {
  // 获取系统统计
  getStats: () => {
    return api.get<ApiResponse<any>>('/api/system/stats')
  },

  // 获取系统信息
  getSystemInfo: () => {
    return api.get<ApiResponse<any>>('/api/system/info')
  },

  // 获取系统日志
  getLogs: (params: {
    page?: number
    size?: number
    level?: string
    startDate?: string
    endDate?: string
  } = {}) => {
    return api.get<PaginatedResponse<any[]>>('/api/system/logs', { params })
  },

  // 清理系统缓存
  clearCache: () => {
    return api.post<ApiResponse<void>>('/api/system/clear-cache')
  },

  // 备份数据库
  backupDatabase: () => {
    return api.post<ApiResponse<{ backupId: string }>>('/api/system/backup')
  },

  // 重启服务
  restartService: (service: string) => {
    return api.post<ApiResponse<void>>(`/api/system/restart/${service}`)
  }
}

// 工具服务
export const utilsService = {
  // 验证码
  getCaptcha: () => {
    return api.get<ApiResponse<{ image: string; key: string }>>('/api/utils/captcha')
  },

  // 发送验证码
  sendVerificationCode: (email: string, type: 'register' | 'reset' | 'login') => {
    return api.post<ApiResponse<void>>('/api/utils/verification-code', {
      email,
      type
    })
  },

  // 验证验证码
  verifyCode: (email: string, code: string, type: string) => {
    return api.post<ApiResponse<{ valid: boolean }>>('/api/utils/verify-code', {
      email,
      code,
      type
    })
  }
}

// 导出所有服务
// 角色服务
export const roleService = {
  // 获取角色列表
  getRoles: (params: {
    page?: number
    size?: number
    keyword?: string
  } = {}) => {
    return api.get<ApiResponse<Role[]>>(ROLE_API.LIST, { params })
  },

  // 获取角色详情
  getRole: (id: string) => {
    return api.get<ApiResponse<Role>>(ROLE_API.GET(id))
  },

  // 创建角色
  createRole: (data: {
    name: string
    description: string
    permissions: string[]
  }) => {
    return api.post<ApiResponse<Role>>(ROLE_API.CREATE, data)
  },

  // 更新角色
  updateRole: (id: string, data: Partial<{
    name: string
    description: string
    permissions: string[]
  }>) => {
    return api.put<ApiResponse<Role>>(ROLE_API.UPDATE(id), data)
  },

  // 删除角色
  deleteRole: (id: string) => {
    return api.delete<ApiResponse<void>>(ROLE_API.DELETE(id))
  }
}

// 权限服务
export const permissionService = {
  // 获取所有权限
  getPermissions: () => {
    return api.get<ApiResponse<Permission[]>>(PERMISSION_API.LIST)
  },

  // 获取用户权限列表
  getUserPermissions: (userId: string) => {
    return api.get<ApiResponse<Permission[]>>(`/api/Permission/user/${userId}`)
  },

  // 获取角色权限列表
  getRolePermissions: (roleId: string) => {
    return api.get<ApiResponse<Permission[]>>(`/api/Permission/role/${roleId}`)
  },

  // 为用户分配角色
  assignRoleToUser: (userId: string, roleId: string) => {
    return api.post<ApiResponse<{ Message: string }>>('/api/Permission/assign-role', { userId, roleId })
  },

  // 为角色分配权限
  assignPermissionToRole: (roleId: string, permissionId: string) => {
    return api.post<ApiResponse<{ Message: string }>>('/api/Permission/assign-permission', { roleId, permissionId })
  },

  // 创建权限
  createPermission: (data: {
    name: string
    description: string
    code: string
    category: string
    parentId?: string
  }) => {
    return api.post<ApiResponse<Permission>>('/api/Permission', data)
  },

  // 更新权限
  updatePermission: (id: string, data: Partial<{
    name: string
    description: string
    category: string
    parentId?: string
  }>) => {
    return api.put<ApiResponse<Permission>>(`/api/Permission/${id}`, data)
  },

  // 删除权限
  deletePermission: (id: string) => {
    return api.delete<ApiResponse<void>>(`/api/Permission/${id}`)
  }
}

export default {
  userService,
  roleService,
  permissionService
}