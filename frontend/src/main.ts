import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { registerElementPlus } from './plugins/elementPlus'
import '@/styles/index.scss'

import App from './App.vue'
import router from './router'
import { useAuthStore } from './stores/auth'
import { registerBaseComponents } from './services/componentRegistry'

// 导入性能服务
import { performanceService } from './services/performanceService'

// 导入用户偏好服务
import { userPreferenceService } from './services/userPreferenceService'

// 导入版本控制服务
import { versionControlService } from './services/versionControlService'

// 导入事件总线服务
import { eventBusService } from './services/eventBusService'

// 导入错误处理工具
import { setupGlobalErrorHandler } from './utils/errorHandler'
import { logger } from './utils/logger'

const app = createApp(App)

app.use(createPinia())
app.use(router)

// 按需注册Element Plus组件和图标
registerElementPlus(app)

// 全局注册服务
app.config.globalProperties.$eventBus = eventBusService
app.config.globalProperties.$performance = performanceService
app.config.globalProperties.$preferences = userPreferenceService
app.config.globalProperties.$versionControl = versionControlService

// 提供服务（Composition API方式）
app.provide('eventBus', eventBusService)
app.provide('performanceService', performanceService)
app.provide('userPreferenceService', userPreferenceService)
app.provide('versionControlService', versionControlService)

// 设置全局错误处理
setupGlobalErrorHandler(app, {
  performanceService,
  eventBusService
})

// 初始化应用
const initApp = async () => {
  try {
    // 性能服务已在创建时初始化
    performanceService.startOperation('app_initialization', { step: 'start' })
    
    // 初始化认证状态（必须在挂载前进行）
    const authStore = useAuthStore()
    const authOpId = performanceService.startOperation('auth_initialization')
    await authStore.initialize()
    performanceService.endOperation(authOpId)
    
    // 初始化用户偏好设置
    const prefOpId = performanceService.startOperation('preferences_initialization')
    await userPreferenceService.initialize()
    performanceService.endOperation(prefOpId)
    
    // 初始化组件库
    const componentOpId = performanceService.startOperation('components_initialization')
    registerBaseComponents()
    performanceService.endOperation(componentOpId)
    
    // 初始化版本控制服务
    const versionOpId = performanceService.startOperation('version_control_initialization')
    await versionControlService.initialize()
    performanceService.endOperation(versionOpId)
    
    // 应用主题设置
    userPreferenceService.applyTheme()
    
    // 挂载应用
    app.mount('#app')
    
    logger.log('项目启动成功')
    
    // 发出初始化完成事件
    eventBusService.emit('app:init', { status: 'completed' })
    
    // 记录应用启动性能
    performanceService.endOperation('app_initialization', { status: 'completed' })
    performanceService.recordMetric({
      id: 'app_initialization',
      name: '应用初始化',
      value: performance.now(),
      unit: 'ms'
    })
  } catch (error) {
    logger.error('项目启动失败:', error)
    
    // 记录初始化失败
    performanceService.failOperation('app_initialization', error instanceof Error ? error.message : 'Unknown error', { error })
    
    // 显示错误提示
    if (window && window.document) {
      const errorElement = document.createElement('div')
      errorElement.style.cssText = `
        position: fixed;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%);
        background: #f56c6c;
        color: white;
        padding: 20px;
        border-radius: 8px;
        z-index: 9999;
        box-shadow: 0 4px 12px rgba(245, 108, 108, 0.3);
      `
      errorElement.textContent = '应用启动失败，请刷新页面重试'
      document.body.appendChild(errorElement)
    }
    
    // 发出错误事件
    eventBusService.emit('app:error', error)
  }
}

// 启动应用
initApp()