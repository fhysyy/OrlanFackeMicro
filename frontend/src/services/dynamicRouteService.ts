import { api } from './api'
import type { DynamicRouteConfig } from '../types/router'
import type { ApiResponse } from '../types/api'

/**
 * 动态路由服务
 */
export const dynamicRouteService = {
  /**
   * 获取当前用户可访问的路由列表
   */
  getRoutes(): Promise<ApiResponse<DynamicRouteConfig[]>> {
    return api.get<ApiResponse<DynamicRouteConfig[]>>('/api/routes')
  },

  /**
   * 根据角色获取路由列表（管理员使用）
   * @param role 角色名称
   */
  getRoutesByRole(role: string): Promise<ApiResponse<DynamicRouteConfig[]>> {
    return api.get<ApiResponse<DynamicRouteConfig[]>>(`/api/routes/role/${role}`)
  },

  /**
   * 创建新路由
   * @param route 路由配置
   */
  createRoute(route: Omit<DynamicRouteConfig, 'routeId' | 'children'>): Promise<ApiResponse<DynamicRouteConfig>> {
    return api.post<ApiResponse<DynamicRouteConfig>>('/api/routes', route)
  },

  /**
   * 更新路由
   * @param routeId 路由ID
   * @param route 路由配置
   */
  updateRoute(routeId: string, route: Partial<DynamicRouteConfig>): Promise<ApiResponse<DynamicRouteConfig>> {
    return api.put<ApiResponse<DynamicRouteConfig>>(`/api/routes/${routeId}`, route)
  },

  /**
   * 删除路由
   * @param routeId 路由ID
   */
  deleteRoute(routeId: string): Promise<ApiResponse<void>> {
    return api.delete<ApiResponse<void>>(`/api/routes/${routeId}`)
  },

  /**
   * 获取所有路由（系统管理员使用）
   */
  getAllRoutes(): Promise<ApiResponse<DynamicRouteConfig[]>> {
    return api.get<ApiResponse<DynamicRouteConfig[]>>('/api/routes/all')
  }
}

export default dynamicRouteService