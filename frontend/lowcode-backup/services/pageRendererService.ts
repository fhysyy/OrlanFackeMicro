import { ref, reactive, computed, nextTick } from 'vue'
import type { App } from 'vue'
import type { 
  PageConfig, 
  ComponentConfig, 
  ComponentProps, 
  DataSourceConfig,
  EventConfig,
  ActionConfig,
  LifecycleHook
} from '@/types/page'
import { deepClone } from '@/utils/deepCloneUtils'
import { getComponentDefinition, loadComponent } from './componentRegistry'
import { apiService } from '@/utils/apiService'
import { ElMessage } from 'element-plus'

// 页面渲染器服务
class PageRendererService {
  private appContext: App | null = null
  private activePages: Map<string, PageContext> = new Map()
  private performanceStats: Map<string, PerformanceData> = new Map()
  
  // 初始化服务
  initialize(app: App) {
    this.appContext = app
  }
  
  // 创建页面上下文
  createPageContext(pageId: string, config: PageConfig): PageContext {
    const context = new PageContext(pageId, config, this)
    this.activePages.set(pageId, context)
    return context
  }
  
  // 销毁页面上下文
  destroyPageContext(pageId: string) {
    const context = this.activePages.get(pageId)
    if (context) {
      context.destroy()
      this.activePages.delete(pageId)
      this.performanceStats.delete(pageId)
    }
  }
  
  // 获取页面上下文
  getPageContext(pageId: string): PageContext | undefined {
    return this.activePages.get(pageId)
  }
  
  // 预渲染页面（用于SSR或性能优化）
  async prerenderPage(config: PageConfig): Promise<string> {
    // 在客户端环境中，这里可以返回占位内容
    // 在SSR环境中，这里可以进行服务端渲染
    return `<div class="prerendered-page"></div>`
  }
  
  // 收集性能数据
  recordPerformance(pageId: string, metrics: Partial<PerformanceData>) {
    const stats = this.performanceStats.get(pageId) || {
      renderTime: 0,
      componentCount: 0,
      dataLoadTime: 0,
      hydrationTime: 0,
      firstPaint: 0
    }
    Object.assign(stats, metrics)
    this.performanceStats.set(pageId, stats)
  }
  
  // 获取性能数据
  getPerformanceStats(pageId: string): PerformanceData | undefined {
    return this.performanceStats.get(pageId)
  }
  
  // 验证页面配置
  validatePageConfig(config: PageConfig): ValidationResult {
    const result: ValidationResult = {
      isValid: true,
      errors: [],
      warnings: []
    }
    
    // 验证根组件类型
    if (!config.type) {
      result.isValid = false
      result.errors.push('页面配置缺少type属性')
    }
    
    // 验证ID唯一性
    const ids = new Set<string>()
    const validateComponentIds = (component: ComponentConfig | PageConfig, path: string = '') => {
      if (!component.id) {
        result.errors.push(`组件缺少ID: ${path || 'root'}`)
        result.isValid = false
      } else if (ids.has(component.id)) {
        result.errors.push(`重复的组件ID: ${component.id} (${path})`)
        result.isValid = false
      } else {
        ids.add(component.id)
      }
      
      // 递归验证子组件
      if (component.children) {
        component.children.forEach((child, index) => {
          validateComponentIds(child, `${path}.children[${index}]`)
        })
      }
      
      // 验证布局组件
      if ('header' in component && component.header) {
        validateComponentIds(component.header, `${path}.header`)
      }
      if ('sidebar' in component && component.sidebar) {
        validateComponentIds(component.sidebar, `${path}.sidebar`)
      }
      if ('rightSidebar' in component && component.rightSidebar) {
        validateComponentIds(component.rightSidebar, `${path}.rightSidebar`)
      }
      if ('footer' in component && component.footer) {
        validateComponentIds(component.footer, `${path}.footer`)
      }
    }
    
    validateComponentIds(config)
    
    // 验证组件是否存在
    const validateComponents = (component: ComponentConfig | PageConfig) => {
      const definition = getComponentDefinition(component.type)
      if (!definition) {
        result.warnings.push(`未找到组件定义: ${component.type}`)
      }
      
      // 递归验证子组件
      if (component.children) {
        component.children.forEach(validateComponents)
      }
      
      // 验证布局组件
      if ('header' in component && component.header) {
        validateComponents(component.header)
      }
      if ('sidebar' in component && component.sidebar) {
        validateComponents(component.sidebar)
      }
      if ('rightSidebar' in component && component.rightSidebar) {
        validateComponents(component.rightSidebar)
      }
      if ('footer' in component && component.footer) {
        validateComponents(component.footer)
      }
    }
    
    validateComponents(config)
    
    return result
  }
  
  // 优化页面配置
  optimizePageConfig(config: PageConfig): PageConfig {
    const optimized = deepClone(config) as PageConfig
    
    // 移除空属性
    const removeEmptyProperties = (obj: any) => {
      if (!obj || typeof obj !== 'object') return obj
      
      Object.keys(obj).forEach(key => {
        if (obj[key] === null || obj[key] === undefined || obj[key] === '') {
          delete obj[key]
        } else if (typeof obj[key] === 'object') {
          removeEmptyProperties(obj[key])
          if (Object.keys(obj[key]).length === 0) {
            delete obj[key]
          }
        }
      })
      
      return obj
    }
    
    return removeEmptyProperties(optimized)
  }
}

// 页面上下文类
class PageContext {
  private pageId: string
  private config: PageConfig
  private service: PageRendererService
  private dataSources: Record<string, any> = {}
  private componentInstances: Map<string, any> = new Map()
  private lifecycleHooks: Record<string, LifecycleHook> = {}
  private eventHandlers: Map<string, EventHandler[]> = new Map()
  private isDestroyed: boolean = false
  
  constructor(pageId: string, config: PageConfig, service: PageRendererService) {
    this.pageId = pageId
    this.config = config
    this.service = service
    this.initializeHooks()
  }
  
  // 初始化生命周期钩子
  private initializeHooks() {
    if (this.config.lifecycleHooks) {
      this.lifecycleHooks = { ...this.config.lifecycleHooks }
    }
  }
  
  // 加载页面数据
  async loadDataSources(): Promise<void> {
    if (!this.config.dataSources) return
    
    const dataLoadStartTime = performance.now()
    const promises = Object.entries(this.config.dataSources).map(async ([key, dataSourceConfig]) => {
      try {
        const data = await this.loadDataSource(key, dataSourceConfig)
        this.dataSources[key] = data
        return { key, success: true }
      } catch (error) {
        console.error(`Failed to load data source '${key}':`, error)
        ElMessage.error(`数据源 '${key}' 加载失败`)
        return { key, success: false, error }
      }
    })
    
    const results = await Promise.all(promises)
    
    // 记录性能数据
    this.service.recordPerformance(this.pageId, {
      dataLoadTime: performance.now() - dataLoadStartTime
    })
    
    // 执行数据处理
    if (this.config.dataProcessors) {
      await this.processData()
    }
  }
  
  // 加载单个数据源
  private async loadDataSource(key: string, config: DataSourceConfig): Promise<any> {
    // 检查是否有自定义处理器
    if (this.config.dataSourceHandlers && this.config.dataSourceHandlers[key]) {
      return await this.config.dataSourceHandlers[key](config.params)
    }
    
    // 根据数据源类型执行不同的加载逻辑
    switch (config.type) {
      case 'api':
        return await this.loadApiDataSource(config)
      case 'static':
        return config.data
      case 'function':
        return typeof config.fn === 'function' ? await config.fn(config.params) : null
      case 'websocket':
        return await this.loadWebSocketDataSource(config)
      default:
        throw new Error(`Unknown data source type: ${config.type}`)
    }
  }
  
  // 加载API数据源
  private async loadApiDataSource(config: DataSourceConfig): Promise<any> {
    if (!config.url) {
      throw new Error('API data source requires a URL')
    }
    
    const method = config.method?.toLowerCase() || 'get'
    const params = config.params || {}
    const headers = config.headers || {}
    
    return await apiService[method as keyof typeof apiService](config.url, params, headers)
  }
  
  // 加载WebSocket数据源
  private async loadWebSocketDataSource(config: DataSourceConfig): Promise<any> {
    // 这里实现WebSocket连接逻辑
    // 返回一个Promise，当收到数据时resolve
    return new Promise((resolve, reject) => {
      if (!config.url) {
        reject(new Error('WebSocket data source requires a URL'))
        return
      }
      
      const ws = new WebSocket(config.url)
      const timeout = setTimeout(() => {
        ws.close()
        reject(new Error('WebSocket connection timeout'))
      }, config.timeout || 10000)
      
      ws.onmessage = (event) => {
        clearTimeout(timeout)
        try {
          const data = JSON.parse(event.data)
          resolve(data)
          if (!config.keepAlive) {
            ws.close()
          }
        } catch (error) {
          reject(error)
          ws.close()
        }
      }
      
      ws.onerror = (error) => {
        clearTimeout(timeout)
        reject(error)
      }
      
      ws.onopen = () => {
        // 发送初始数据
        if (config.initialData) {
          ws.send(JSON.stringify(config.initialData))
        }
      }
    })
  }
  
  // 处理数据
  private async processData(): Promise<void> {
    if (!this.config.dataProcessors) return
    
    for (const [key, processor] of Object.entries(this.config.dataProcessors)) {
      try {
        if (typeof processor === 'function') {
          this.dataSources[key] = await processor(this.dataSources)
        }
      } catch (error) {
        console.error(`Failed to process data for '${key}':`, error)
      }
    }
  }
  
  // 注册组件实例
  registerComponentInstance(componentId: string, instance: any) {
    this.componentInstances.set(componentId, instance)
  }
  
  // 注销组件实例
  unregisterComponentInstance(componentId: string) {
    this.componentInstances.delete(componentId)
  }
  
  // 获取组件实例
  getComponentInstance(componentId: string): any {
    return this.componentInstances.get(componentId)
  }
  
  // 添加事件监听器
  addEventHandler(eventName: string, handler: EventHandler) {
    if (!this.eventHandlers.has(eventName)) {
      this.eventHandlers.set(eventName, [])
    }
    this.eventHandlers.get(eventName)!.push(handler)
  }
  
  // 移除事件监听器
  removeEventHandler(eventName: string, handler: EventHandler) {
    const handlers = this.eventHandlers.get(eventName)
    if (handlers) {
      const index = handlers.indexOf(handler)
      if (index > -1) {
        handlers.splice(index, 1)
      }
    }
  }
  
  // 触发事件
  async emitEvent(eventName: string, eventData: any): Promise<void> {
    // 执行注册的事件处理器
    const handlers = this.eventHandlers.get(eventName)
    if (handlers) {
      await Promise.all(handlers.map(handler => handler(eventData)))
    }
    
    // 执行页面配置中的事件处理
    if (this.config.events) {
      const eventConfig = this.config.events.find(e => e.name === eventName)
      if (eventConfig && eventConfig.actions) {
        await this.executeActions(eventConfig.actions, eventData)
      }
    }
  }
  
  // 执行动作序列
  private async executeActions(actions: ActionConfig[], eventData: any): Promise<void> {
    for (const action of actions) {
      try {
        await this.executeAction(action, eventData)
      } catch (error) {
        console.error(`Failed to execute action:`, error)
        
        // 处理错误动作
        if (action.onError) {
          await this.executeAction(action.onError, { ...eventData, error })
        }
      }
    }
  }
  
  // 执行单个动作
  private async executeAction(action: ActionConfig, eventData: any): Promise<void> {
    switch (action.type) {
      case 'apiCall':
        await this.executeApiCall(action, eventData)
        break
      case 'stateUpdate':
        this.executeStateUpdate(action)
        break
      case 'navigate':
        this.executeNavigate(action)
        break
      case 'showMessage':
        this.executeShowMessage(action)
        break
      case 'customScript':
        this.executeCustomScript(action, eventData)
        break
      case 'componentMethod':
        this.executeComponentMethod(action)
        break
      default:
        console.warn(`Unknown action type: ${action.type}`)
    }
  }
  
  // 执行API调用动作
  private async executeApiCall(action: ActionConfig, eventData: any): Promise<void> {
    const { url, method = 'get', params, headers } = action.config || {}
    
    if (!url) {
      throw new Error('API call action requires a URL')
    }
    
    const result = await apiService[method.toLowerCase() as keyof typeof apiService](
      url,
      params,
      headers
    )
    
    // 处理成功动作
    if (action.onSuccess) {
      await this.executeAction(action.onSuccess, { ...eventData, result })
    }
  }
  
  // 执行状态更新动作
  private executeStateUpdate(action: ActionConfig): void {
    // 这里可以更新页面状态或其他相关状态
    console.log('State update action:', action.updates)
  }
  
  // 执行导航动作
  private executeNavigate(action: ActionConfig): void {
    const { path, replace = false } = action.config || {}
    
    if (!path) {
      throw new Error('Navigate action requires a path')
    }
    
    const { useRouter } = require('vue-router')
    const router = useRouter()
    
    if (replace) {
      router.replace(path)
    } else {
      router.push(path)
    }
  }
  
  // 执行显示消息动作
  private executeShowMessage(action: ActionConfig): void {
    const { message, level = 'info', duration = 3000 } = action.config || {}
    
    if (!message) {
      throw new Error('Show message action requires a message')
    }
    
    ElMessage[level as keyof typeof ElMessage]({
      message,
      duration
    })
  }
  
  // 执行自定义脚本动作
  private executeCustomScript(action: ActionConfig, eventData: any): void {
    const { script } = action.config || {}
    
    if (!script) {
      throw new Error('Custom script action requires a script')
    }
    
    try {
      // 使用Function构造函数执行脚本，提供安全的上下文
      const scriptFunction = new Function(
        'context',
        'eventData',
        'dataSources',
        'emit',
        script
      )
      
      scriptFunction(
        this,
        eventData,
        this.dataSources,
        (eventName: string, data: any) => this.emitEvent(eventName, data)
      )
    } catch (error) {
      console.error('Failed to execute custom script:', error)
      throw error
    }
  }
  
  // 执行组件方法调用
  private executeComponentMethod(action: ActionConfig): void {
    const { componentId, method, params = [] } = action.config || {}
    
    if (!componentId || !method) {
      throw new Error('Component method action requires componentId and method')
    }
    
    const instance = this.getComponentInstance(componentId)
    if (instance && typeof instance[method] === 'function') {
      instance[method](...params)
    } else {
      console.warn(`Component ${componentId} or method ${method} not found`)
    }
  }
  
  // 调用生命周期钩子
  async callHook(hookName: string, ...args: any[]): Promise<void> {
    const hook = this.lifecycleHooks[hookName]
    if (typeof hook === 'function') {
      try {
        await hook(this, ...args)
      } catch (error) {
        console.error(`Failed to execute ${hookName} hook:`, error)
      }
    }
  }
  
  // 获取数据源
  getDataSource(key: string): any {
    return this.dataSources[key]
  }
  
  // 更新数据源
  updateDataSource(key: string, data: any): void {
    this.dataSources[key] = data
  }
  
  // 获取页面配置
  getConfig(): PageConfig {
    return this.config
  }
  
  // 更新页面配置
  updateConfig(config: Partial<PageConfig>): void {
    Object.assign(this.config, config)
  }
  
  // 销毁页面上下文
  async destroy(): Promise<void> {
    if (this.isDestroyed) return
    
    // 调用卸载钩子
    await this.callHook('onUnmount')
    
    // 清理资源
    this.componentInstances.clear()
    this.eventHandlers.clear()
    this.dataSources = {}
    
    this.isDestroyed = true
  }
}

// 类型定义
export interface PerformanceData {
  renderTime: number
  componentCount: number
  dataLoadTime: number
  hydrationTime?: number
  firstPaint?: number
}

export interface ValidationResult {
  isValid: boolean
  errors: string[]
  warnings: string[]
}

export type EventHandler = (eventData: any) => void | Promise<void>

// 导出服务实例
export const pageRendererService = new PageRendererService()

// 导出Vue插件
export default {
  install(app: App) {
    pageRendererService.initialize(app)
    app.provide('pageRenderer', pageRendererService)
  }
}