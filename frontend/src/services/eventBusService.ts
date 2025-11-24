import { ref, reactive, inject, onUnmounted } from 'vue'
import type { App, Ref } from 'vue'
import type { 
  EventConfig, 
  ActionConfig, 
  EventHandler,
  EventContext,
  EventFilter,
  EventTransformer,
  EventInterceptor
} from '@/types/event'
import { ElMessage, ElMessageBox, ElNotification } from 'element-plus'
import { apiService } from '@/utils/apiService'

// 事件总线服务
class EventBusService {
  private appContext: App | null = null
  private eventHandlers: Map<string, EventHandler[]> = new Map()
  private globalInterceptors: EventInterceptor[] = []
  private globalFilters: Map<string, EventFilter> = new Map()
  private globalTransformers: Map<string, EventTransformer> = new Map()
  private activeContexts: Map<string, EventContext> = new Map()
  private eventHistory: EventRecord[] = []
  private maxHistorySize: number = 1000
  private isDebug: boolean = false
  
  // 初始化服务
  initialize(app: App, options: EventBusOptions = {}) {
    this.appContext = app
    this.maxHistorySize = options.maxHistorySize || 1000
    this.isDebug = options.debug || false
  }
  
  // 创建事件上下文
  createContext(contextId: string, options?: EventContextOptions): EventContext {
    const context = new EventContext(contextId, this, options)
    this.activeContexts.set(contextId, context)
    return context
  }
  
  // 销毁事件上下文
  destroyContext(contextId: string) {
    const context = this.activeContexts.get(contextId)
    if (context) {
      context.destroy()
      this.activeContexts.delete(contextId)
    }
  }
  
  // 获取事件上下文
  getContext(contextId: string): EventContext | undefined {
    return this.activeContexts.get(contextId)
  }
  
  // 注册全局事件处理器
  on(eventName: string, handler: EventHandler): () => void {
    if (!this.eventHandlers.has(eventName)) {
      this.eventHandlers.set(eventName, [])
    }
    this.eventHandlers.get(eventName)!.push(handler)
    
    // 返回取消订阅函数
    return () => this.off(eventName, handler)
  }
  
  // 移除全局事件处理器
  off(eventName: string, handler: EventHandler) {
    const handlers = this.eventHandlers.get(eventName)
    if (handlers) {
      const index = handlers.indexOf(handler)
      if (index > -1) {
        handlers.splice(index, 1)
      }
      
      // 如果没有处理器了，清理映射
      if (handlers.length === 0) {
        this.eventHandlers.delete(eventName)
      }
    }
  }
  
  // 触发全局事件
  async emit(eventName: string, eventData?: any): Promise<EventResult> {
    return this.processEvent('global', eventName, eventData)
  }
  
  // 一次性事件监听
  once(eventName: string, handler: EventHandler): () => void {
    const onceHandler: EventHandler = async (eventData, context) => {
      this.off(eventName, onceHandler)
      return await handler(eventData, context)
    }
    return this.on(eventName, onceHandler)
  }
  
  // 注册全局拦截器
  registerInterceptor(interceptor: EventInterceptor): void {
    this.globalInterceptors.push(interceptor)
  }
  
  // 移除全局拦截器
  removeInterceptor(interceptor: EventInterceptor): void {
    const index = this.globalInterceptors.indexOf(interceptor)
    if (index > -1) {
      this.globalInterceptors.splice(index, 1)
    }
  }
  
  // 注册全局过滤器
  registerGlobalFilter(name: string, filter: EventFilter): void {
    this.globalFilters.set(name, filter)
  }
  
  // 注册全局转换器
  registerGlobalTransformer(name: string, transformer: EventTransformer): void {
    this.globalTransformers.set(name, transformer)
  }
  
  // 获取全局过滤器
  getGlobalFilter(name: string): EventFilter | undefined {
    return this.globalFilters.get(name)
  }
  
  // 获取全局转换器
  getGlobalTransformer(name: string): EventTransformer | undefined {
    return this.globalTransformers.get(name)
  }
  
  // 处理事件
  private async processEvent(source: string, eventName: string, eventData: any = {}): Promise<EventResult> {
    const startTime = performance.now()
    
    // 创建事件上下文
    const eventContext: EventContextData = {
      eventName,
      source,
      timestamp: Date.now(),
      data: eventData
    }
    
    // 执行拦截器（前置）
    for (const interceptor of this.globalInterceptors) {
      try {
        if (interceptor.before && typeof interceptor.before === 'function') {
          const result = interceptor.before(eventContext)
          if (result === false) {
            this.recordEvent(eventName, eventData, 'canceled', source)
            return {
              success: false,
              canceled: true,
              eventName,
              data: eventData
            }
          }
        }
      } catch (error) {
        console.error('Error in event interceptor:', error)
      }
    }
    
    // 获取所有处理器
    const handlers = this.eventHandlers.get(eventName) || []
    const results: any[] = []
    const errors: Error[] = []
    
    // 执行所有处理器
    for (const handler of handlers) {
      try {
        const result = await handler(eventData, eventContext)
        results.push(result)
      } catch (error) {
        console.error(`Error handling event '${eventName}':`, error)
        errors.push(error as Error)
      }
    }
    
    // 执行拦截器（后置）
    for (const interceptor of this.globalInterceptors) {
      try {
        if (interceptor.after && typeof interceptor.after === 'function') {
          interceptor.after(eventContext, results, errors)
        }
      } catch (error) {
        console.error('Error in event interceptor:', error)
      }
    }
    
    // 记录事件
    this.recordEvent(eventName, eventData, errors.length > 0 ? 'error' : 'success', source)
    
    const endTime = performance.now()
    
    return {
      success: errors.length === 0,
      canceled: false,
      eventName,
      data: eventData,
      results,
      errors,
      executionTime: endTime - startTime
    }
  }
  
  // 记录事件到历史
  private recordEvent(eventName: string, eventData: any, status: 'success' | 'error' | 'canceled', source: string) {
    if (!this.isDebug) return
    
    const eventRecord: EventRecord = {
      eventName,
      data: eventData,
      status,
      timestamp: Date.now(),
      source
    }
    
    this.eventHistory.push(eventRecord)
    
    // 限制历史记录大小
    if (this.eventHistory.length > this.maxHistorySize) {
      this.eventHistory.shift()
    }
  }
  
  // 获取事件历史
  getEventHistory(): EventRecord[] {
    return this.eventHistory
  }
  
  // 清空事件历史
  clearEventHistory(): void {
    this.eventHistory = []
  }
  
  // 执行动作
  async executeAction(action: ActionConfig, eventData: any = {}): Promise<ActionResult> {
    const startTime = performance.now()
    
    try {
      const result = await this.processAction(action, eventData)
      
      const endTime = performance.now()
      return {
        success: true,
        result,
        executionTime: endTime - startTime
      }
    } catch (error) {
      console.error('Error executing action:', error)
      
      // 处理错误动作
      if (action.onError) {
        try {
          await this.processAction(action.onError, { ...eventData, error })
        } catch (errorActionError) {
          console.error('Error executing error action:', errorActionError)
        }
      }
      
      const endTime = performance.now()
      return {
        success: false,
        error: error as Error,
        executionTime: endTime - startTime
      }
    }
  }
  
  // 处理单个动作
  private async processAction(action: ActionConfig, eventData: any = {}): Promise<any> {
    switch (action.type) {
      case 'apiCall':
        return await this.executeApiCall(action, eventData)
      case 'stateUpdate':
        return this.executeStateUpdate(action, eventData)
      case 'navigate':
        return this.executeNavigate(action)
      case 'showMessage':
        return this.executeShowMessage(action)
      case 'showConfirm':
        return await this.executeShowConfirm(action)
      case 'showNotification':
        return this.executeShowNotification(action)
      case 'customScript':
        return this.executeCustomScript(action, eventData)
      case 'componentMethod':
        return this.executeComponentMethod(action, eventData)
      case 'eventEmit':
        return await this.executeEventEmit(action, eventData)
      case 'wait':
        return await this.executeWait(action)
      case 'loop':
        return await this.executeLoop(action, eventData)
      case 'conditional':
        return await this.executeConditional(action, eventData)
      default:
        throw new Error(`Unknown action type: ${action.type}`)
    }
  }
  
  // 执行API调用动作
  private async executeApiCall(action: ActionConfig, eventData: any): Promise<any> {
    const { url, method = 'get', params, headers, timeout } = action.config || {}
    
    if (!url) {
      throw new Error('API call action requires a URL')
    }
    
    // 准备请求参数，支持表达式替换
    const processedParams = this.processActionParams(params || {}, eventData)
    const processedHeaders = this.processActionParams(headers || {}, eventData)
    
    const requestConfig: any = {
      headers: processedHeaders
    }
    
    if (timeout) {
      requestConfig.timeout = timeout
    }
    
    const result = await apiService[method.toLowerCase() as keyof typeof apiService](
      url,
      processedParams,
      requestConfig
    )
    
    // 处理成功动作
    if (action.onSuccess) {
      await this.processAction(action.onSuccess, { ...eventData, result })
    }
    
    return result
  }
  
  // 执行状态更新动作
  private executeStateUpdate(action: ActionConfig, eventData: any): any {
    const { updates, target } = action.config || {}
    
    if (!updates) {
      throw new Error('State update action requires updates')
    }
    
    // 处理更新参数
    const processedUpdates = this.processActionParams(updates, eventData)
    
    // 根据目标更新状态
    if (target === 'global' && this.appContext) {
      // 更新全局状态
      const globalState = this.appContext.config.globalProperties.$globalState
      if (globalState) {
        Object.assign(globalState, processedUpdates)
      }
    } else if (target === 'context' && eventData.context) {
      // 更新上下文状态
      Object.assign(eventData.context, processedUpdates)
    }
    
    return processedUpdates
  }
  
  // 执行导航动作
  private executeNavigate(action: ActionConfig): void {
    const { path, replace = false, params, query } = action.config || {}
    
    if (!path) {
      throw new Error('Navigate action requires a path')
    }
    
    // 动态导入路由，避免循环依赖
    const { useRouter } = require('vue-router')
    const router = useRouter()
    
    const navigationParams: any = {
      path
    }
    
    if (params) {
      navigationParams.params = params
    }
    
    if (query) {
      navigationParams.query = query
    }
    
    if (replace) {
      router.replace(navigationParams)
    } else {
      router.push(navigationParams)
    }
  }
  
  // 执行显示消息动作
  private executeShowMessage(action: ActionConfig): any {
    const { message, type = 'info', duration = 3000, showClose = true, grouping } = action.config || {}
    
    if (!message) {
      throw new Error('Show message action requires a message')
    }
    
    return ElMessage({
      message: message,
      type: type as any,
      duration,
      showClose,
      grouping
    })
  }
  
  // 执行显示确认对话框动作
  private async executeShowConfirm(action: ActionConfig): Promise<boolean> {
    const { title = '确认', message, confirmButtonText = '确定', cancelButtonText = '取消', type } = action.config || {}
    
    if (!message) {
      throw new Error('Show confirm action requires a message')
    }
    
    try {
      await ElMessageBox.confirm(message, title, {
        confirmButtonText,
        cancelButtonText,
        type: type as any,
        distinguishCancelAndClose: true
      })
      
      // 处理确认动作
      if (action.onSuccess) {
        await this.processAction(action.onSuccess, { confirmed: true })
      }
      
      return true
    } catch {
      // 处理取消动作
      if (action.onError) {
        await this.processAction(action.onError, { confirmed: false })
      }
      return false
    }
  }
  
  // 执行显示通知动作
  private executeShowNotification(action: ActionConfig): any {
    const { 
      title, 
      message, 
      type = 'info', 
      duration = 4500, 
      showClose = true,
      offset = 0,
      position = 'top-right'
    } = action.config || {}
    
    if (!message) {
      throw new Error('Show notification action requires a message')
    }
    
    return ElNotification({
      title,
      message,
      type: type as any,
      duration,
      showClose,
      offset,
      position: position as any
    })
  }
  
  // 执行自定义脚本动作
  private executeCustomScript(action: ActionConfig, eventData: any): any {
    const { script, context } = action.config || {}
    
    if (!script) {
      throw new Error('Custom script action requires a script')
    }
    
    try {
      // 创建安全的执行上下文
      const executionContext = {
        eventData,
        context: context || {},
        emit: (eventName: string, data: any) => this.emit(eventName, data),
        executeAction: (action: ActionConfig) => this.executeAction(action, eventData),
        ElMessage,
        ElMessageBox,
        ElNotification
      }
      
      // 使用Function构造函数执行脚本
      const scriptFunction = new Function(
        'context',
        script
      )
      
      return scriptFunction(executionContext)
    } catch (error) {
      console.error('Failed to execute custom script:', error)
      throw error
    }
  }
  
  // 执行组件方法调用动作
  private executeComponentMethod(action: ActionConfig, eventData: any): any {
    const { componentId, method, params = [] } = action.config || {}
    
    if (!componentId || !method) {
      throw new Error('Component method action requires componentId and method')
    }
    
    // 尝试从事件数据中获取组件实例
    let componentInstance: any = null
    
    // 从上下文或事件数据中查找组件实例
    if (eventData.context && eventData.context.componentInstances) {
      componentInstance = eventData.context.componentInstances[componentId]
    } else if (eventData.$refs && eventData.$refs[componentId]) {
      componentInstance = eventData.$refs[componentId]
    }
    
    if (!componentInstance) {
      throw new Error(`Component '${componentId}' not found`)
    }
    
    // 处理参数，支持表达式
    const processedParams = params.map((param: any) => 
      this.processActionParams(param, eventData)
    )
    
    // 调用组件方法
    if (typeof componentInstance[method] !== 'function') {
      throw new Error(`Method '${method}' not found on component '${componentId}'`)
    }
    
    return componentInstance[method](...processedParams)
  }
  
  // 执行事件触发动作
  private async executeEventEmit(action: ActionConfig, eventData: any): Promise<EventResult> {
    const { eventName, data } = action.config || {}
    
    if (!eventName) {
      throw new Error('Event emit action requires an eventName')
    }
    
    // 合并数据
    const mergedData = { ...eventData, ...(data || {}) }
    
    return await this.emit(eventName, mergedData)
  }
  
  // 执行等待动作
  private async executeWait(action: ActionConfig): Promise<void> {
    const { delay = 1000 } = action.config || {}
    
    return new Promise(resolve => setTimeout(resolve, delay))
  }
  
  // 执行循环动作
  private async executeLoop(action: ActionConfig, eventData: any): Promise<any[]> {
    const { iterations = 1, actions, condition } = action.config || {}
    
    if (!actions || !Array.isArray(actions)) {
      throw new Error('Loop action requires actions array')
    }
    
    const results: any[] = []
    let iteration = 0
    
    // 条件循环
    if (condition && typeof condition === 'function') {
      while (condition(eventData, { iteration, results }) && iteration < 1000) { // 防止无限循环
        const iterationResults = await Promise.all(
          actions.map(action => this.processAction(action, { ...eventData, iteration }))
        )
        results.push(iterationResults)
        iteration++
      }
    } 
    // 固定次数循环
    else {
      for (let i = 0; i < iterations; i++) {
        const iterationResults = await Promise.all(
          actions.map(action => this.processAction(action, { ...eventData, iteration: i }))
        )
        results.push(iterationResults)
      }
    }
    
    return results
  }
  
  // 执行条件动作
  private async executeConditional(action: ActionConfig, eventData: any): Promise<any> {
    const { condition, ifTrue, ifFalse } = action.config || {}
    
    if (!condition) {
      throw new Error('Conditional action requires a condition')
    }
    
    let conditionResult = false
    
    // 评估条件
    if (typeof condition === 'function') {
      conditionResult = await condition(eventData)
    } else {
      // 简单表达式评估
      try {
        conditionResult = Boolean(eval(`(${condition})`))
      } catch (error) {
        console.error('Failed to evaluate condition:', error)
        conditionResult = false
      }
    }
    
    // 执行相应的动作
    if (conditionResult && ifTrue) {
      return await this.processAction(ifTrue, { ...eventData, conditionResult })
    } else if (!conditionResult && ifFalse) {
      return await this.processAction(ifFalse, { ...eventData, conditionResult })
    }
    
    return conditionResult
  }
  
  // 处理动作参数（支持表达式）
  private processActionParams(params: any, eventData: any): any {
    if (typeof params === 'string' && params.startsWith('{{') && params.endsWith('}}')) {
      // 简单表达式替换
      const expression = params.slice(2, -2).trim()
      try {
        return this.evaluateExpression(expression, eventData)
      } catch (error) {
        console.error('Failed to evaluate expression:', error)
        return params
      }
    } else if (typeof params === 'object' && params !== null) {
      // 递归处理对象
      if (Array.isArray(params)) {
        return params.map(item => this.processActionParams(item, eventData))
      } else {
        const result: any = {}
        for (const [key, value] of Object.entries(params)) {
          result[key] = this.processActionParams(value, eventData)
        }
        return result
      }
    }
    
    return params
  }
  
  // 简单表达式评估
  private evaluateExpression(expression: string, context: any): any {
    try {
      // 安全的表达式评估
      const keys = Object.keys(context)
      const values = Object.values(context)
      const fn = new Function(...keys, `return ${expression}`)
      return fn(...values)
    } catch (error) {
      console.error(`Failed to evaluate expression: ${expression}`, error)
      throw error
    }
  }
  
  // 应用过滤器
  applyFilter(eventData: any, filter: EventFilter | string, params?: any): any {
    if (typeof filter === 'string') {
      const filterFn = this.getGlobalFilter(filter)
      if (!filterFn) {
        console.warn(`Filter not found: ${filter}`)
        return eventData
      }
      return filterFn(eventData, params)
    }
    return filter(eventData, params)
  }
  
  // 应用转换器
  applyTransformer(eventData: any, transformer: EventTransformer | string, params?: any): any {
    if (typeof transformer === 'string') {
      const transformerFn = this.getGlobalTransformer(transformer)
      if (!transformerFn) {
        console.warn(`Transformer not found: ${transformer}`)
        return eventData
      }
      return transformerFn(eventData, params)
    }
    return transformer(eventData, params)
  }
  
  // 获取统计信息
  getStats(): EventStats {
    const handlerCount = Array.from(this.eventHandlers.values()).reduce(
      (sum, handlers) => sum + handlers.length, 0
    )
    
    return {
      activeEvents: this.eventHandlers.size,
      handlerCount,
      activeContexts: this.activeContexts.size,
      eventHistorySize: this.eventHistory.length,
      globalInterceptors: this.globalInterceptors.length,
      globalFilters: this.globalFilters.size,
      globalTransformers: this.globalTransformers.size
    }
  }
}

// 事件上下文类
class EventContext {
  private contextId: string
  private service: EventBusService
  private eventHandlers: Map<string, EventHandler[]> = new Map()
  private interceptors: EventInterceptor[] = []
  private filters: Map<string, EventFilter> = new Map()
  private transformers: Map<string, EventTransformer> = new Map()
  private isDestroyed: boolean = false
  private options: EventContextOptions
  
  constructor(contextId: string, service: EventBusService, options: EventContextOptions = {}) {
    this.contextId = contextId
    this.service = service
    this.options = options
  }
  
  // 注册事件处理器
  on(eventName: string, handler: EventHandler): () => void {
    if (!this.eventHandlers.has(eventName)) {
      this.eventHandlers.set(eventName, [])
    }
    this.eventHandlers.get(eventName)!.push(handler)
    
    // 返回取消订阅函数
    return () => this.off(eventName, handler)
  }
  
  // 移除事件处理器
  off(eventName: string, handler: EventHandler) {
    const handlers = this.eventHandlers.get(eventName)
    if (handlers) {
      const index = handlers.indexOf(handler)
      if (index > -1) {
        handlers.splice(index, 1)
      }
      
      if (handlers.length === 0) {
        this.eventHandlers.delete(eventName)
      }
    }
  }
  
  // 触发事件
  async emit(eventName: string, eventData?: any): Promise<EventResult> {
    const startTime = performance.now()
    
    // 创建事件上下文数据
    const eventContextData: EventContextData = {
      eventName,
      source: this.contextId,
      timestamp: Date.now(),
      data: eventData
    }
    
    // 执行上下文拦截器（前置）
    for (const interceptor of this.interceptors) {
      try {
        if (interceptor.before && typeof interceptor.before === 'function') {
          const result = interceptor.before(eventContextData)
          if (result === false) {
            return {
              success: false,
              canceled: true,
              eventName,
              data: eventData
            }
          }
        }
      } catch (error) {
        console.error('Error in context event interceptor:', error)
      }
    }
    
    // 获取上下文处理器
    const handlers = this.eventHandlers.get(eventName) || []
    const results: any[] = []
    const errors: Error[] = []
    
    // 执行上下文处理器
    for (const handler of handlers) {
      try {
        const result = await handler(eventData, eventContextData)
        results.push(result)
      } catch (error) {
        console.error(`Error handling event '${eventName}' in context '${this.contextId}':`, error)
        errors.push(error as Error)
      }
    }
    
    // 执行上下文拦截器（后置）
    for (const interceptor of this.interceptors) {
      try {
        if (interceptor.after && typeof interceptor.after === 'function') {
          interceptor.after(eventContextData, results, errors)
        }
      } catch (error) {
        console.error('Error in context event interceptor:', error)
      }
    }
    
    const endTime = performance.now()
    
    // 如果配置了冒泡，也触发全局事件
    if (this.options.propagateEvents !== false) {
      await this.service.emit(eventName, eventData)
    }
    
    return {
      success: errors.length === 0,
      canceled: false,
      eventName,
      data: eventData,
      results,
      errors,
      executionTime: endTime - startTime
    }
  }
  
  // 一次性事件监听
  once(eventName: string, handler: EventHandler): () => void {
    const onceHandler: EventHandler = async (eventData, context) => {
      this.off(eventName, onceHandler)
      return await handler(eventData, context)
    }
    return this.on(eventName, onceHandler)
  }
  
  // 注册拦截器
  registerInterceptor(interceptor: EventInterceptor): void {
    this.interceptors.push(interceptor)
  }
  
  // 移除拦截器
  removeInterceptor(interceptor: EventInterceptor): void {
    const index = this.interceptors.indexOf(interceptor)
    if (index > -1) {
      this.interceptors.splice(index, 1)
    }
  }
  
  // 注册过滤器
  registerFilter(name: string, filter: EventFilter): void {
    this.filters.set(name, filter)
  }
  
  // 注册转换器
  registerTransformer(name: string, transformer: EventTransformer): void {
    this.transformers.set(name, transformer)
  }
  
  // 执行动作
  async executeAction(action: ActionConfig, eventData: any = {}): Promise<ActionResult> {
    return this.service.executeAction(action, { ...eventData, context: this })
  }
  
  // 执行动作序列
  async executeActions(actions: ActionConfig[], eventData: any = {}): Promise<ActionResult[]> {
    const results: ActionResult[] = []
    
    for (const action of actions) {
      const result = await this.executeAction(action, eventData)
      results.push(result)
      
      // 如果配置了遇到错误就停止
      if (!result.success && this.options.stopOnError !== false) {
        break
      }
    }
    
    return results
  }
  
  // 应用过滤器
  applyFilter(eventData: any, filter: EventFilter | string, params?: any): any {
    if (typeof filter === 'string') {
      // 先尝试从上下文获取
      const filterFn = this.filters.get(filter) || this.service.getGlobalFilter(filter)
      if (!filterFn) {
        console.warn(`Filter not found: ${filter}`)
        return eventData
      }
      return filterFn(eventData, params)
    }
    return filter(eventData, params)
  }
  
  // 应用转换器
  applyTransformer(eventData: any, transformer: EventTransformer | string, params?: any): any {
    if (typeof transformer === 'string') {
      // 先尝试从上下文获取
      const transformerFn = this.transformers.get(transformer) || this.service.getGlobalTransformer(transformer)
      if (!transformerFn) {
        console.warn(`Transformer not found: ${transformer}`)
        return eventData
      }
      return transformerFn(eventData, params)
    }
    return transformer(eventData, params)
  }
  
  // 获取上下文ID
  getContextId(): string {
    return this.contextId
  }
  
  // 销毁上下文
  destroy(): void {
    if (this.isDestroyed) return
    
    // 清理所有处理器
    this.eventHandlers.clear()
    this.interceptors = []
    this.filters.clear()
    this.transformers.clear()
    
    this.isDestroyed = true
  }
}

// 类型定义
export interface EventBusOptions {
  debug?: boolean
  maxHistorySize?: number
}

export interface EventContextOptions {
  propagateEvents?: boolean
  stopOnError?: boolean
}

export interface EventContextData {
  eventName: string
  source: string
  timestamp: number
  data: any
}

export interface EventResult {
  success: boolean
  canceled: boolean
  eventName: string
  data: any
  results?: any[]
  errors?: Error[]
  executionTime?: number
}

export interface ActionResult {
  success: boolean
  result?: any
  error?: Error
  executionTime: number
}

export interface EventRecord {
  eventName: string
  data: any
  status: 'success' | 'error' | 'canceled'
  timestamp: number
  source: string
}

export interface EventStats {
  activeEvents: number
  handlerCount: number
  activeContexts: number
  eventHistorySize: number
  globalInterceptors: number
  globalFilters: number
  globalTransformers: number
}

// 导出服务实例
export const eventBusService = new EventBusService()

// 导出Vue插件
export default {
  install(app: App, options?: EventBusOptions) {
    eventBusService.initialize(app, options)
    app.provide('eventBus', eventBusService)
    app.config.globalProperties.$eventBus = eventBusService
  }
}

// 导出Vue组合式API
export function useEventBus() {
  const eventBus = inject<EventBusService>('eventBus')
  if (!eventBus) {
    throw new Error('EventBusService not found. Please make sure the plugin is installed.')
  }
  
  // 创建上下文（如果需要）
  const context = ref<EventContext | null>(null)
  
  // 自动创建和销毁上下文的便捷方法
  const createAutoContext = (contextId?: string, options?: EventContextOptions) => {
    if (context.value) return context.value
    
    const id = contextId || `component-context-${Date.now()}`
    context.value = eventBus.createContext(id, options)
    
    // 自动清理
    onUnmounted(() => {
      if (context.value) {
        eventBus.destroyContext(context.value.getContextId())
        context.value = null
      }
    })
    
    return context.value
  }
  
  return {
    eventBus,
    createAutoContext,
    context,
    // 直接代理常用方法
    on: eventBus.on.bind(eventBus),
    emit: eventBus.emit.bind(eventBus),
    once: eventBus.once.bind(eventBus),
    executeAction: eventBus.executeAction.bind(eventBus)
  }
}