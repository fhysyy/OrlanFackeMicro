import { ref, reactive, watch, onMounted } from 'vue'
import { performanceService } from '@/services/performanceService'

// 用户偏好设置类型定义
export interface UserPreferences {
  // 界面设置
  interface: {
    // 主题模式
    theme: 'light' | 'dark' | 'system'
    // 语言
    language: string
    // 编辑器字体大小
    fontSize: number
    // 编辑器行高
    lineHeight: number
    // 编辑器是否显示行号
    showLineNumbers: boolean
    // 编辑器是否自动完成
    enableAutoComplete: boolean
    // 是否显示网格线
    showGrid: boolean
    // 网格大小
    gridSize: number
    // 紧凑模式
    compactMode: boolean
    // 侧边栏宽度
    sidebarWidth: number
    // 底部状态栏高度
    statusBarHeight: number
  }
  
  // 行为设置
  behavior: {
    // 是否自动保存
    autoSave: boolean
    // 自动保存间隔（秒）
    autoSaveInterval: number
    // 是否启用快捷键
    enableShortcuts: boolean
    // 是否显示提示
    showTips: boolean
    // 自动格式化代码
    autoFormatCode: boolean
    // 显示欢迎页
    showWelcomePage: boolean
    // 默认布局
    defaultLayout: 'standard' | 'minimal' | 'fullscreen'
    // 拖拽时是否显示辅助线
    showAlignmentGuides: boolean
    // 组件选择时是否高亮
    highlightSelectedComponents: boolean
  }
  
  // 通知设置
  notifications: {
    // 是否启用通知
    enabled: boolean
    // 显示通知持续时间（毫秒）
    duration: number
    // 是否显示声音
    soundEnabled: boolean
    // 通知位置
    position: 'top-right' | 'top-left' | 'bottom-right' | 'bottom-left' | 'top-center' | 'bottom-center'
    // 是否显示进度条
    showProgress: boolean
    // 是否允许批量通知
    allowBatching: boolean
    // 通知级别
    level: 'all' | 'warnings-errors' | 'errors-only'
  }
  
  // 开发设置
  development: {
    // 是否启用开发者模式
    developerMode: boolean
    // 是否显示性能指标
    showPerformanceMetrics: boolean
    // 是否启用热重载
    enableHotReload: boolean
    // 是否启用调试信息
    showDebugInfo: boolean
    // 是否启用实验性功能
    enableExperimentalFeatures: boolean
    // API超时时间（毫秒）
    apiTimeout: number
    // 请求重试次数
    requestRetryCount: number
  }
  
  // 数据设置
  data: {
    // 数据源默认配置
    defaultDataSource: {
      type: 'api' | 'local' | 'websocket'
      url?: string
      method?: 'GET' | 'POST' | 'PUT' | 'DELETE'
      headers?: Record<string, string>
    }
    // 数据刷新间隔（秒）
    dataRefreshInterval: number
    // 是否启用缓存
    enableCaching: boolean
    // 缓存时间（秒）
    cacheDuration: number
  }
  
  // 自定义配置
  custom: Record<string, any>
}

// 默认偏好设置
const defaultPreferences: UserPreferences = {
  interface: {
    theme: 'system',
    language: 'zh-CN',
    fontSize: 14,
    lineHeight: 1.5,
    showLineNumbers: true,
    enableAutoComplete: true,
    showGrid: true,
    gridSize: 10,
    compactMode: false,
    sidebarWidth: 280,
    statusBarHeight: 32
  },
  behavior: {
    autoSave: true,
    autoSaveInterval: 30,
    enableShortcuts: true,
    showTips: true,
    autoFormatCode: true,
    showWelcomePage: true,
    defaultLayout: 'standard',
    showAlignmentGuides: true,
    highlightSelectedComponents: true
  },
  notifications: {
    enabled: true,
    duration: 3000,
    soundEnabled: false,
    position: 'top-right',
    showProgress: true,
    allowBatching: true,
    level: 'all'
  },
  development: {
    developerMode: false,
    showPerformanceMetrics: false,
    enableHotReload: true,
    showDebugInfo: false,
    enableExperimentalFeatures: false,
    apiTimeout: 30000,
    requestRetryCount: 3
  },
  data: {
    defaultDataSource: {
      type: 'api'
    },
    dataRefreshInterval: 60,
    enableCaching: true,
    cacheDuration: 300
  },
  custom: {}
}

// 存储键名
const STORAGE_KEY = 'orleans_lc_platform_preferences'

// 响应式偏好设置
const preferences = reactive<UserPreferences>({ ...defaultPreferences })

// 加载状态
const isLoading = ref(true)
const hasLoaded = ref(false)
const error = ref<string | null>(null)

// 初始化函数
const initialize = async (): Promise<void> => {
  try {
    const startTime = performance.now()
    
    // 从localStorage加载偏好设置
    await loadPreferences()
    
    // 监听系统主题变化
    if (preferences.interface.theme === 'system') {
      setupSystemThemeListener()
    }
    
    // 监听偏好设置变化，自动保存
    setupAutoSave()
    
    // 应用当前主题
    applyTheme()
    
    hasLoaded.value = true
    isLoading.value = false
    
    // 记录性能
    performanceService.recordMetric({
      id: 'preferences_loaded',
      name: 'Preferences Load Time',
      value: performance.now() - startTime,
      unit: 'ms'
    })
  } catch (err) {
    error.value = err instanceof Error ? err.message : '加载用户偏好设置失败'
    isLoading.value = false
    console.error('Failed to initialize user preferences:', err)
    
    // 使用默认设置
    Object.assign(preferences, defaultPreferences)
    hasLoaded.value = true
  }
}

// 从localStorage加载偏好设置
const loadPreferences = async (): Promise<void> => {
  try {
    const stored = localStorage.getItem(STORAGE_KEY)
    if (stored) {
      const parsed = JSON.parse(stored) as Partial<UserPreferences>
      // 深度合并，避免覆盖未设置的字段
      deepMerge(preferences, parsed)
    }
  } catch (err) {
    console.error('Failed to load preferences from localStorage:', err)
    throw new Error('无法加载保存的用户偏好设置')
  }
}

// 保存偏好设置到localStorage
const savePreferences = async (): Promise<void> => {
  try {
    const startTime = performance.now()
    
    localStorage.setItem(STORAGE_KEY, JSON.stringify(preferences))
    
    // 记录性能
    performanceService.recordMetric({
      id: 'preferences_saved',
      name: 'Preferences Save Time',
      value: performance.now() - startTime,
      unit: 'ms'
    })
  } catch (err) {
    console.error('Failed to save preferences to localStorage:', err)
    throw new Error('无法保存用户偏好设置')
  }
}

// 深度合并对象
const deepMerge = (target: any, source: any): any => {
  for (const key in source) {
    if (source.hasOwnProperty(key)) {
      if (source[key] !== null && typeof source[key] === 'object' && !Array.isArray(source[key])) {
        if (!target[key]) target[key] = {}
        deepMerge(target[key], source[key])
      } else {
        target[key] = source[key]
      }
    }
  }
  return target
}

// 设置自动保存
const setupAutoSave = (): void => {
  // 监听变化并节流保存
  let saveTimeout: number | null = null
  
  const debouncedSave = () => {
    if (saveTimeout) {
      clearTimeout(saveTimeout)
    }
    
    saveTimeout = window.setTimeout(() => {
      savePreferences()
    }, 500) as unknown as number
  }
  
  // 为每个层级设置深度监听
  Object.keys(preferences).forEach(topLevelKey => {
    const topLevelValue = (preferences as any)[topLevelKey]
    if (typeof topLevelValue === 'object' && topLevelValue !== null) {
      watch(
        () => topLevelValue,
        () => debouncedSave(),
        { deep: true }
      )
    }
  })
}

// 设置系统主题监听器
const setupSystemThemeListener = (): void => {
  const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)')
  
  const handleThemeChange = (e: MediaQueryListEvent) => {
    applyTheme(e.matches ? 'dark' : 'light')
  }
  
  mediaQuery.addEventListener('change', handleThemeChange)
  
  // 清理函数
  const cleanup = () => {
    mediaQuery.removeEventListener('change', handleThemeChange)
  }
  
  // 存储清理函数，以便在需要时调用
  (window as any).__cleanupThemeListener = cleanup
}

// 应用主题
const applyTheme = (explicitTheme?: 'light' | 'dark'): void => {
  let themeToApply: 'light' | 'dark'
  
  if (explicitTheme) {
    themeToApply = explicitTheme
  } else if (preferences.interface.theme === 'system') {
    const isDarkMode = window.matchMedia('(prefers-color-scheme: dark)').matches
    themeToApply = isDarkMode ? 'dark' : 'light'
  } else {
    themeToApply = preferences.interface.theme
  }
  
  // 移除之前的主题类
  document.documentElement.classList.remove('theme-light', 'theme-dark')
  
  // 添加新的主题类
  document.documentElement.classList.add(`theme-${themeToApply}`)
  
  // 设置data-theme属性
  document.documentElement.setAttribute('data-theme', themeToApply)
  
  // 通知全局主题变化
  window.dispatchEvent(new CustomEvent('themeChanged', { detail: { theme: themeToApply } }))
}

// 更新偏好设置
const updatePreferences = async (updates: Partial<UserPreferences>): Promise<void> => {
  try {
    // 深度合并更新
    deepMerge(preferences, updates)
    
    // 如果主题设置改变，重新应用主题
    if (updates.interface?.theme) {
      applyTheme()
    }
    
    // 立即保存
    await savePreferences()
  } catch (err) {
    console.error('Failed to update preferences:', err)
    throw new Error('无法更新用户偏好设置')
  }
}

// 更新特定路径的偏好设置
const updatePreferencePath = async (path: string, value: any): Promise<void> => {
  try {
    const keys = path.split('.')
    let current: any = preferences
    
    // 遍历路径，直到最后一个键
    for (let i = 0; i < keys.length - 1; i++) {
      const key = keys[i]
      if (!current[key]) {
        current[key] = {}
      }
      current = current[key]
    }
    
    // 设置值
    current[keys[keys.length - 1]] = value
    
    // 如果更新主题相关设置，重新应用
    if (path.startsWith('interface.theme')) {
      applyTheme()
    }
    
    // 立即保存
    await savePreferences()
  } catch (err) {
    console.error(`Failed to update preference path ${path}:`, err)
    throw new Error(`无法更新路径 ${path} 的偏好设置`)
  }
}

// 重置所有偏好设置
const resetPreferences = async (): Promise<void> => {
  try {
    // 清除localStorage
    localStorage.removeItem(STORAGE_KEY)
    
    // 重置为默认值
    Object.assign(preferences, defaultPreferences)
    
    // 重新应用主题
    applyTheme()
    
    // 通知全局重置
    window.dispatchEvent(new CustomEvent('preferencesReset'))
  } catch (err) {
    console.error('Failed to reset preferences:', err)
    throw new Error('无法重置用户偏好设置')
  }
}

// 导出偏好设置
const exportPreferences = (): string => {
  return JSON.stringify(preferences, null, 2)
}

// 导入偏好设置
const importPreferences = async (jsonString: string): Promise<void> => {
  try {
    const imported = JSON.parse(jsonString) as Partial<UserPreferences>
    
    // 验证导入的偏好设置格式
    if (!validatePreferences(imported)) {
      throw new Error('导入的偏好设置格式无效')
    }
    
    // 更新偏好设置
    await updatePreferences(imported)
  } catch (err) {
    console.error('Failed to import preferences:', err)
    throw err
  }
}

// 验证偏好设置格式
const validatePreferences = (prefs: any): boolean => {
  // 基本验证
  if (typeof prefs !== 'object' || prefs === null) return false
  
  // 验证必要的顶层结构
  const requiredKeys = ['interface', 'behavior', 'notifications', 'development', 'data']
  return requiredKeys.every(key => prefs.hasOwnProperty(key))
}

// 获取偏好设置
const getPreferences = (): UserPreferences => {
  return { ...preferences }
}

// 获取特定路径的偏好设置
const getPreferencePath = (path: string): any => {
  const keys = path.split('.')
  let current: any = preferences
  
  for (const key of keys) {
    if (current === undefined || current === null) {
      return undefined
    }
    current = current[key]
  }
  
  return current
}

// 检查是否支持特定功能
const isFeatureEnabled = (featureName: string): boolean => {
  // 首先检查开发设置
  if (preferences.development[featureName as keyof typeof preferences.development] !== undefined) {
    return !!preferences.development[featureName as keyof typeof preferences.development]
  }
  
  // 检查行为设置
  if (preferences.behavior[featureName as keyof typeof preferences.behavior] !== undefined) {
    return !!preferences.behavior[featureName as keyof typeof preferences.behavior]
  }
  
  // 检查自定义设置
  if (preferences.custom[featureName] !== undefined) {
    return !!preferences.custom[featureName]
  }
  
  return false
}

// 切换功能开关
const toggleFeature = async (featureName: string): Promise<void> => {
  const currentValue = isFeatureEnabled(featureName)
  
  // 尝试在不同位置设置
  if (preferences.development[featureName as keyof typeof preferences.development] !== undefined) {
    await updatePreferencePath(`development.${featureName}`, !currentValue)
  } else if (preferences.behavior[featureName as keyof typeof preferences.behavior] !== undefined) {
    await updatePreferencePath(`behavior.${featureName}`, !currentValue)
  } else {
    // 如果不存在，在custom中设置
    preferences.custom[featureName] = !currentValue
    await savePreferences()
  }
}

// 应用界面设置
const applyInterfaceSettings = (): void => {
  // 应用字体大小
  document.documentElement.style.fontSize = `${preferences.interface.fontSize}px`
  
  // 应用行高
  document.documentElement.style.lineHeight = String(preferences.interface.lineHeight)
  
  // 应用紧凑模式
  if (preferences.interface.compactMode) {
    document.documentElement.classList.add('compact-mode')
  } else {
    document.documentElement.classList.remove('compact-mode')
  }
  
  // 通知全局界面设置变化
  window.dispatchEvent(new CustomEvent('interfaceSettingsChanged', { detail: preferences.interface }))
}

// 用户偏好设置服务
const userPreferenceService = {
  // 初始化
  initialize,
  
  // 获取状态
  isLoading,
  hasLoaded,
  error,
  
  // 获取偏好设置
  getPreferences,
  getPreferencePath,
  
  // 更新偏好设置
  updatePreferences,
  updatePreferencePath,
  
  // 重置
  resetPreferences,
  
  // 导入导出
  exportPreferences,
  importPreferences,
  
  // 主题相关
  applyTheme,
  
  // 功能开关
  isFeatureEnabled,
  toggleFeature,
  
  // 应用设置
  applyInterfaceSettings,
  
  // 响应式偏好设置
  preferences
}

// 导出组合式API
export const useUserPreferences = () => {
  // 在组件挂载时初始化
  onMounted(() => {
    if (!hasLoaded.value && !isLoading.value) {
      initialize()
    }
  })
  
  return {
    // 状态
    preferences,
    isLoading,
    hasLoaded,
    error,
    
    // 方法
    getPreferences,
    getPreferencePath,
    updatePreferences,
    updatePreferencePath,
    resetPreferences,
    exportPreferences,
    importPreferences,
    applyTheme,
    isFeatureEnabled,
    toggleFeature,
    applyInterfaceSettings
  }
}

// 暴露服务
export { userPreferenceService }

// 自动初始化（如果在浏览器环境中）
if (typeof window !== 'undefined' && !isLoading.value) {
  initialize()
}