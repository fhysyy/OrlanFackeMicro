import { RouteRecordRaw } from 'vue-router'
import { UserRole } from './api'

/**
 * 动态路由配置项
 */
export interface DynamicRouteConfig extends Omit<RouteRecordRaw, 'children'> {
  // 路由ID，用于后端存储和管理
  routeId?: string;
  // 父路由ID，用于构建嵌套路由
  parentId?: string | null;
  // 图标名称
  icon?: string;
  // 排序号
  order?: number;
  // 是否在菜单中显示
  hidden?: boolean;
  // 子路由
  children?: DynamicRouteConfig[];
  // 组件路径
  componentPath?: string;
  // 是否为动态加载的组件
  isDynamicComponent?: boolean;
}

/**
 * 路由权限配置
 */
export interface RoutePermission {
  // 允许访问的角色列表
  roles?: UserRole[];
  // 允许访问的权限代码列表
  permissions?: string[];
}

/**
 * 扩展路由元数据
 */
export interface EnhancedRouteMeta {
  // 是否需要认证
  requiresAuth?: boolean;
  // 权限配置
  permission?: RoutePermission;
  // 页面标题
  title?: string;
  // 图标
  icon?: string;
  // 是否在菜单中隐藏
  hidden?: boolean;
  // 排序号
  order?: number;
}

// 扩展vue-router模块
declare module 'vue-router' {
  interface RouteMeta extends EnhancedRouteMeta {}
}