import { createRouter, createWebHistory, RouteRecordRaw, NavigationGuardNext, RouteLocationNormalized } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { ElMessage } from 'element-plus'
import { UserRole } from '@/types/api'

// 定义路由元数据类型
declare module 'vue-router' {
  interface RouteMeta {
    requiresAuth?: boolean
    permission?: {
      roles?: UserRole[]
      permissions?: string[]
    }
    title?: string
  }
}

// 检查用户是否有权限访问路由
function hasPermission(route: RouteRecordRaw, userRole: UserRole | null): boolean {
  return true;
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
}

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/Login.vue'),
    meta: { requiresAuth: false }
  },

  {
    path: '/',
    component: () => import('@/layouts/MainLayout.vue'),
    redirect: '/dashboard',
    meta: { requiresAuth: true },
    children: [
      {
        path: 'dashboard',
        name: 'Dashboard',
        component: () => import('@/views/Dashboard.vue'),
        meta: { 
          requiresAuth: true,
          permission: { roles: [UserRole.User, UserRole.Admin, UserRole.SystemAdmin] },
          title: '仪表板'
        }
      },
      {
        path: 'users',
        name: 'Users',
        component: () => import('@/views/Users.vue'),
        meta: { 
          requiresAuth: true,
          permission: { roles: [UserRole.Admin, UserRole.SystemAdmin] },
          title: '用户管理'
        }
      },
      {
        path: 'roles',
        name: 'Roles',
        component: () => import('@/views/Roles.vue'),
        meta: { 
          requiresAuth: true,
          permission: { roles: [UserRole.SystemAdmin] },
          title: '角色管理'
        }
      },
      {
        path: 'permissions',
        name: 'Permissions',
        component: () => import('@/views/Permissions.vue'),
        meta: { 
          requiresAuth: true,
          permission: { roles: [UserRole.SystemAdmin] },
          title: '权限管理'
        }
      },
      {
        path: 'messages',
        name: 'Messages',
        component: () => import('@/views/Messages.vue'),
        meta: { 
          requiresAuth: true,
          permission: { roles: [UserRole.User, UserRole.Admin, UserRole.SystemAdmin] },
          title: '消息管理'
        }
      },
      {
        path: 'files',
        name: 'Files',
        component: () => import('@/views/Files.vue'),
        meta: { 
          requiresAuth: true,
          permission: { roles: [UserRole.User, UserRole.Admin, UserRole.SystemAdmin] },
          title: '文件管理'
        }
      },
      {
        path: 'system',
        name: 'System',
        component: () => import('@/views/System.vue'),
        meta: { 
          requiresAuth: true,
          permission: { roles: [UserRole.SystemAdmin] },
          title: '系统监控'
        }
      },
      {
        path: 'dictionary-types',
        name: 'DictionaryTypes',
        component: () => import('@/pages/dictionary/DictionaryTypeList.vue'),
        meta: { 
          requiresAuth: true,
          permission: { roles: [UserRole.Admin, UserRole.SystemAdmin] },
          title: '字典类型管理'
        }
      },
      {
        path: 'dictionary-items',
        name: 'DictionaryItems',
        component: () => import('@/pages/dictionary/DictionaryItemList.vue'),
        meta: { 
          requiresAuth: true,
          permission: { roles: [UserRole.Admin, UserRole.SystemAdmin] },
          title: '字典项管理'
        }
      },
      // 班级学生成绩管理相关路由
      {
        path: 'class-management',
        name: 'ClassManagement',
        component: () => import('@/views/ClassManagement.vue'),
        meta: { 
          requiresAuth: true,
          permission: { roles: [UserRole.Admin, UserRole.SystemAdmin] },
          title: '班级管理'
        }
      },
      {
        path: 'student-management',
        name: 'StudentManagement',
        component: () => import('@/views/StudentManagement.vue'),
        meta: { 
          requiresAuth: true,
          permission: { roles: [UserRole.User, UserRole.Admin, UserRole.SystemAdmin] },
          title: '学生管理'
        }
      },
      {
        path: 'score-management',
        name: 'ScoreManagement',
        component: () => import('@/views/ScoreManagement.vue'),
        meta: { 
          requiresAuth: true,
          permission: { roles: [UserRole.User, UserRole.Admin, UserRole.SystemAdmin] },
          title: '成绩管理'
        }
      },
      {
        path: 'exam-management',
        name: 'ExamManagement',
        component: () => import('@/views/ExamManagement.vue'),
        meta: { 
          requiresAuth: true,
          permission: { roles: [UserRole.User, UserRole.Admin, UserRole.SystemAdmin] },
          title: '考试管理'
        }
      },
      {
        path: 'profile',
        name: 'Profile',
        component: () => import('@/views/Profile.vue'),
        meta: { 
          requiresAuth: true,
          permission: { roles: [UserRole.User, UserRole.Admin, UserRole.SystemAdmin] },
          title: '个人资料'
        }
      }
    ]
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'NotFound',
    component: () => import('@/views/NotFound.vue')
  }
]

// 创建路由实例
const router = createRouter({
  history: createWebHistory(),
  routes
})

// 路由守卫
router.beforeEach((to: RouteLocationNormalized, from, next: NavigationGuardNext) => {
  const authStore = useAuthStore()
  
  // 设置页面标题
  if (to.meta.title) {
    document.title = `${to.meta.title} - Orleans管理系统`
  } else {
    document.title = 'Orleans管理系统'
  }
  
  // 检查是否需要认证
  if (to.meta.requiresAuth && !(authStore.token && authStore.user)) {
    ElMessage.warning('请先登录')
    next({ name: 'Login', query: { redirect: to.fullPath } })
    return
  }
  
  // 如果已登录，不需要访问登录页面
  if (to.name === 'Login' && authStore.token && authStore.user) {
    next({ name: 'Dashboard' })
    return
  }
  
  // 细粒度权限检查
  if (to.meta.requiresAuth && authStore.token && authStore.user) {
    const userRole = authStore.user?.role || null
   
    // 检查路由权限
    if (!hasPermission(to, userRole)) {
      ElMessage.error('没有权限访问此页面')
      // 如果是从其他页面跳转过来的，返回上一页
      if (from.fullPath !== '/' && from.fullPath !== to.fullPath) {
        next(from)
      } else {
        // 否则重定向到仪表盘
        next({ name: 'Dashboard' })
      }
      return
    }
  }
  
  next()
})

// 全局错误处理
router.onError((error) => {
  console.error('路由错误:', error)
  ElMessage.error('加载页面失败，请刷新重试')
})

export default router