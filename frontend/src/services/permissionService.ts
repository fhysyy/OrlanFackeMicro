import { api } from './api'
import type { Permission, PermissionGroup, ApiResponse } from '@/types/api'

export const permissionService = {
  // 获取权限分组列表
  getPermissionGroups: () => {
    return api.get<ApiResponse<PermissionGroup[]>>('/api/admin/permissions/groups')
  },

  // 获取所有权限
  getAllPermissions: () => {
    return api.get<ApiResponse<Permission[]>>('/api/admin/permissions')
  },

  // 获取用户权限
  getUserPermissions: (userId: string) => {
    return api.get<ApiResponse<Permission[]>>(`/api/admin/permissions/user/${userId}`)
  },

  // 获取角色权限
  getRolePermissions: (roleId: string) => {
    return api.get<ApiResponse<Permission[]>>(`/api/admin/permissions/role/${roleId}`)
  },
  
  // 创建权限
  createPermission: (data: {
    name: string
    description: string
    code: string
    category: string
  }) => {
    return api.post<ApiResponse<Permission>>('/api/admin/permissions', data)
  },
  
  // 更新权限
  updatePermission: (id: string, data: {
    name: string
    description: string
    code: string
    category: string
  }) => {
    return api.put<ApiResponse<Permission>>(`/api/admin/permissions/${id}`, data)
  },
  
  // 删除权限
  deletePermission: (id: string) => {
    return api.delete<ApiResponse<void>>(`/api/admin/permissions/${id}`)
  }
}