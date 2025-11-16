// 状态管理主文件
export { useAuthStore } from './auth'

// 用户状态管理
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { User, Notification } from '@/types'

export const useUserStore = defineStore('user', () => {
  const currentUser = ref<User | null>(null)
  const notifications = ref<Notification[]>([])
  const unreadCount = computed(() => notifications.value.filter(n => !n.read).length)

  // 设置当前用户
  const setCurrentUser = (user: User) => {
    currentUser.value = user
  }

  // 清除用户信息
  const clearCurrentUser = () => {
    currentUser.value = null
  }

  // 添加通知
  const addNotification = (notification: Omit<Notification, 'id' | 'timestamp'>) => {
    const newNotification: Notification = {
      id: Date.now().toString(),
      timestamp: Date.now(),
      ...notification
    }
    notifications.value.unshift(newNotification)
    
    // 限制通知数量
    if (notifications.value.length > 50) {
      notifications.value = notifications.value.slice(0, 50)
    }
  }

  // 标记通知为已读
  const markAsRead = (id: string) => {
    const notification = notifications.value.find(n => n.id === id)
    if (notification) {
      notification.read = true
    }
  }

  // 标记所有通知为已读
  const markAllAsRead = () => {
    notifications.value.forEach(notification => {
      notification.read = true
    })
  }

  // 删除通知
  const removeNotification = (id: string) => {
    const index = notifications.value.findIndex(n => n.id === id)
    if (index !== -1) {
      notifications.value.splice(index, 1)
    }
  }

  // 清空通知
  const clearNotifications = () => {
    notifications.value = []
  }

  return {
    currentUser,
    notifications,
    unreadCount,
    
    setCurrentUser,
    clearCurrentUser,
    addNotification,
    markAsRead,
    markAllAsRead,
    removeNotification,
    clearNotifications
  }
})

// 应用设置状态管理
export const useAppStore = defineStore('app', () => {
  const sidebarCollapsed = ref(false)
  const theme = ref('light')
  const language = ref('zh-CN')
  const loading = ref(false)
  const pageTitle = ref('FakeMicro Admin')

  // 切换侧边栏状态
  const toggleSidebar = () => {
    sidebarCollapsed.value = !sidebarCollapsed.value
  }

  // 设置侧边栏状态
  const setSidebarCollapsed = (collapsed: boolean) => {
    sidebarCollapsed.value = collapsed
  }

  // 切换主题
  const toggleTheme = () => {
    theme.value = theme.value === 'light' ? 'dark' : 'light'
    document.documentElement.setAttribute('data-theme', theme.value)
  }

  // 设置主题
  const setTheme = (newTheme: string) => {
    theme.value = newTheme
    document.documentElement.setAttribute('data-theme', newTheme)
  }

  // 设置语言
  const setLanguage = (lang: string) => {
    language.value = lang
  }

  // 设置加载状态
  const setLoading = (isLoading: boolean) => {
    loading.value = isLoading
  }

  // 设置页面标题
  const setPageTitle = (title: string) => {
    pageTitle.value = title
    document.title = `${title} - FakeMicro Admin`
  }

  return {
    sidebarCollapsed,
    theme,
    language,
    loading,
    pageTitle,
    
    toggleSidebar,
    setSidebarCollapsed,
    toggleTheme,
    setTheme,
    setLanguage,
    setLoading,
    setPageTitle
  }
})

// 表格状态管理
export const useTableStore = defineStore('table', () => {
  const tableConfigs = ref<Record<string, any>>({})

  // 获取表格配置
  const getTableConfig = (tableId: string) => {
    return tableConfigs.value[tableId] || {}
  }

  // 设置表格配置
  const setTableConfig = (tableId: string, config: any) => {
    tableConfigs.value[tableId] = config
  }

  // 保存表格状态（列宽、排序等）
  const saveTableState = (tableId: string, state: any) => {
    const config = getTableConfig(tableId)
    config.state = state
    setTableConfig(tableId, config)
  }

  return {
    tableConfigs,
    getTableConfig,
    setTableConfig,
    saveTableState
  }
})