import { createRouter, createWebHistory, RouteRecordRaw, NavigationGuardNext, RouteLocationNormalized } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { ElMessage } from 'element-plus'
import { UserRole } from '@/types/api'
import type { DynamicRouteConfig } from '@/types/router'
import dynamicRouteService from '@/services/dynamicRouteService'
import { logger } from '@/utils/logger'
import { tokenManager } from '@/utils/tokenManager'

// 定义路由元数据类型
declare module 'vue-router' {
  interface RouteMeta {
    requiresAuth?: boolean
    permission?: {
      roles?: UserRole[]
      permissions?: string[]
    }
    title?: string
    icon?: string
  }
}

// 静态路由
const staticRoutes: Array<RouteRecordRaw> = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/Login.vue'),
    meta: { requiresAuth: false }
  },
  {
    path: '/404',
    name: 'NotFound',
    component: () => import('@/views/NotFound.vue'),
    meta: { requiresAuth: false }
  },
  {
    path: '/:pathMatch(.*)*',
    redirect: '/404'
  }
]

// 主布局路由（作为动态路由的容器）
const mainLayoutRoute: RouteRecordRaw = {
  path: '/',
  name: 'Main',
  component: () => import('@/layouts/MainLayout.vue'),
  meta: { requiresAuth: true },
  children: [
    {
      path: 'dashboard',
      name: 'Dashboard',
      component: () => import('@/views/Dashboard.vue'),
      meta: {
        requiresAuth: true,
        permission: { roles: [UserRole.User, UserRole.Admin, UserRole.SystemAdmin] },
        title: '仪表板',
        icon: 'el-icon-data-line'
      }
    },
    {
      path: 'vtj-designer',
      name: 'VtjDesigner',
      component: () => import('@/views/VTJDesigner.vue'),
      meta: {
        requiresAuth: true,
        title: 'VTJ设计器',
        icon: 'el-icon-edit'
      }
    },
    {
      path: 'custom-crud',
      name: 'CustomCrud',
      component: () => import('@/views/CustomCrudPage.vue'),
      meta: {
        requiresAuth: true,
        title: '自定义CRUD页面',
        icon: 'el-icon-setting'
      }
    },
    {
      path: 'configurable-form',
      name: 'ConfigurableForm',
      component: () => import('@/views/ConfigurableForm.vue'),
      meta: {
        requiresAuth: true,
        title: '可配置表单',
        icon: 'el-icon-document-add'
      }
    },
    {
      path: 'form-config-management',
      name: 'FormConfigManagement',
      component: () => import('@/views/FormConfigManagement.vue'),
      meta: {
        requiresAuth: true,
        title: '表单配置管理',
        icon: 'el-icon-setting'
      }
    }

  ]
}

// 创建路由实例
const router = createRouter({
  history: createWebHistory(),
  routes: [...staticRoutes, mainLayoutRoute]
})

// 动态路由加载状态
let dynamicRoutesLoaded = false

/**
 * 将动态路由配置转换为Vue Router格式
 */
function convertToRouteRecord(route: DynamicRouteConfig): RouteRecordRaw {
  const routeRecord: RouteRecordRaw = {
    path: route.path,
    name: route.name,
    meta: route.meta || {},
    redirect: route.redirect
  }

  // 处理组件加载
  if (route.componentPath) {
    if (route.isDynamicComponent) {
      // 动态组件路径加载 - 添加.vue扩展名，确保符合Vite要求
      routeRecord.component = () => import(`@/views/${route.componentPath}.vue`)
    } else {
      // 静态组件引用
      routeRecord.component = route.component
    }
  }

  // 处理子路由
  if (route.children && route.children.length > 0) {
    routeRecord.children = route.children.map(child => convertToRouteRecord(child))
  }

  return routeRecord
}

/**
 * 加载动态路由
 */
async function loadDynamicRoutes() {
  try {
    // 暂时移除token验证，允许在未登录状态下加载动态路由
    /*
    if (!tokenManager.isTokenValid()) {
      return false
    }
    */

    try {
      // 从后端获取路由配置
      const response = await dynamicRouteService.getRoutes()
      const dynamicRoutes = response.data || []

      // 转换并添加动态路由
      const mainRoute = router.getRoute('Main')
      if (mainRoute) {
        dynamicRoutes.forEach(route => {
          const routeRecord = convertToRouteRecord(route)
          // 将动态路由添加为主布局的子路由
          router.addRoute('Main', routeRecord)
        })
      }
    } catch (apiError) {
      logger.warn('动态路由API调用失败，将继续使用静态路由:', apiError)
      // API调用失败时不阻止应用运行
    }

    dynamicRoutesLoaded = true
    return true
  } catch (error) {
    logger.error('加载动态路由时发生错误:', error)
    // 即使发生错误也设置为已加载，避免重复尝试
    dynamicRoutesLoaded = true
    return true
  }
}

// 检查用户是否有权限访问路由 - 暂时注释掉权限检查，允许所有访问
function hasPermission(route: RouteRecordRaw, userRole: UserRole | null): boolean {
  // 暂时允许所有访问
  return true
  /*
  // 如果没有定义权限要求，默认允许访问
  if (!route.meta?.permission) {
    return true
  }
  
  const { roles = [] } = route.meta.permission
  
  // 如果定义了角色要求，但用户没有角色，返回false
  if (roles.length > 0 && !userRole) {
    return false
  }
  
  // 检查用户角色是否在允许列表中
  return roles.length === 0 || (userRole && roles.includes(userRole))
  */
}

// 路由守卫 - 暂时注释掉鉴权逻辑
router.beforeEach(async (to: RouteLocationNormalized, from, next: NavigationGuardNext) => {
  const authStore = useAuthStore()
  
  // 设置页面标题
  if (to.meta.title) {
    document.title = `${to.meta.title} - Orleans管理系统`
  } else {
    document.title = 'Orleans管理系统'
  }
  
  // 暂时注释掉认证检查，允许所有访问
  /*
  // 检查是否需要认证
  if (to.meta.requiresAuth && !tokenManager.isTokenValid()) {
    ElMessage.warning('请先登录')
    next({ name: 'Login', query: { redirect: to.fullPath } })
    return
  }
  
  // 如果已登录，不需要访问登录页面
  if (to.name === 'Login' && tokenManager.isTokenValid()) {
    next({ name: 'Dashboard' })
    return
  }
  */
  
  // 如果已认证且未加载动态路由，则加载动态路由
  // 暂时修改为不检查token，直接加载动态路由
  if (to.meta.requiresAuth && !dynamicRoutesLoaded) {
    const loaded = await loadDynamicRoutes()
    // 如果路由加载成功，重新导航到目标路由，确保路由正确匹配
    if (loaded) {
      next({ ...to, replace: true })
      return
    }
  }
  
  // 暂时注释掉细粒度权限检查
  /*
  // 细粒度权限检查
  if (to.meta.requiresAuth && tokenManager.isTokenValid()) {
    const authStore = useAuthStore()
    const userRole = authStore.user?.role || null
   
    // 检查路由权限
    if (!hasPermission(to, userRole)) {
      ElMessage.error('没有权限访问此页面')
      // 使用404而不是返回上一页，避免暴露权限信息
      next('/404')
      return
    }
  }
  */
  
  next()
})

// 全局错误处理
router.onError((error) => {
  logger.error('路由错误:', error)
  ElMessage.error('加载页面失败，请刷新重试')
})

// 导出路由相关功能供其他模块使用
export { loadDynamicRoutes }

export default router