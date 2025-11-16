import { api } from './api'
import type { Role, ApiResponse, PaginatedResponse } from '@/types/api'

export const roleService = {
  // 获取角色列表
  getRoles: (params: {
    page?: number
    size?: number
    name?: string
  } = {}) => {
    return api.get<ApiResponse<PaginatedResponse<Role>>>('/api/admin/roles', { params })
  },

  // 获取角色详情
  getRole: (id: string) => {
    return api.get<ApiResponse<Role>>(`/api/admin/role/${id}`)
  },

  // 创建角色
  createRole: (data: {
    name: string
    description: string
    permissions: string[]
  }) => {
    return api.post<ApiResponse<Role>>('/api/admin/role', data)
  },

  // 更新角色
  updateRole: (id: string, data: {
    name: string
    description: string
    permissions: string[]
  }) => {
    return api.put<ApiResponse<Role>>(`/api/admin/role/${id}`, data)
  },

  // 删除角色
  deleteRole: (id: string) => {
    return api.delete<ApiResponse<null>>(`/api/admin/role/${id}`)
  }
}