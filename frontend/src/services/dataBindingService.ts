import { ref, reactive, computed, watch, isRef, isReactive, toRef, nextTick } from 'vue'
import type { Ref, Reactive } from 'vue'
import type { 
  DataBindingConfig, 
  DataSourceConfig, 
  DataTransformer,
  DataFilter,
  BindingExpression,
  DataContext
} from '@/types/dataBinding'
import { apiService } from '@/utils/apiService'
import { ElMessage } from 'element-plus'

// 数据绑定服务
class DataBindingService {
  private bindingContexts: Map<string, BindingContext> = new Map()
  private globalFilters: Map<string, DataFilter> = new Map()
  private globalTransformers: Map<string, DataTransformer> = new Map()
  private expressionCache: Map<string, CompiledExpression> = new Map()
  
  // 创建绑定上下文
  createBindingContext(contextId: string, initialData?: any): BindingContext {
    const context = new BindingContext(contextId, initialData, this)
    this.bindingContexts.set(contextId, context)
    return context
  }
  
  // 销毁绑定上下文
  destroyBindingContext(contextId: string) {
    const context = this.bindingContexts.get(contextId)
    if (context) {
      context.destroy()
      this.bindingContexts.delete(contextId)
    }
  }
  
  // 获取绑定上下文
  getBindingContext(contextId: string): BindingContext | undefined {
    return this.bindingContexts.get(contextId)
  }
  
  // 注册全局过滤器
  registerGlobalFilter(name: string, filter: DataFilter) {
    this.globalFilters.set(name, filter)
  }
  
  // 注册全局转换器
  registerGlobalTransformer(name: string, transformer: DataTransformer) {
    this.globalTransformers.set(name, transformer)
  }
  
  // 获取全局过滤器
  getGlobalFilter(name: string): DataFilter | undefined {
    return this.globalFilters.get(name)
  }
  
  // 获取全局转换器
  getGlobalTransformer(name: string): DataTransformer | undefined {
    return this.globalTransformers.get(name)
  }
  
  // 编译绑定表达式 - 优化版本
  @performanceMonitor
  compileExpression(expression: string): CompiledExpression {
    // 检查缓存
    if (this.expressionCache.has(expression)) {
      return this.expressionCache.get(expression)! as CompiledExpression
    }
    
    // 使用优化的表达式评估器
    const compiled: CompiledExpression = {
      expression,
      evaluate: (context: any) => bindingOptimizer.optimizeExpression(expression, context)
    }
    
    this.expressionCache.set(expression, compiled)
    return compiled
  }
  
  // 判断是否为简单路径表达式 - 保留用于兼容性
  private isSimplePath(expression: string): boolean {
    return /^[a-zA-Z_$][a-zA-Z0-9_$]*(\.[a-zA-Z_$][a-zA-Z0-9_$]*|\[\d+\]|\['[^']*'\]|"[^"]*")*$/.test(expression)
  }
  
  // 评估简单路径表达式 - 使用优化的路径解析器
  private evaluateSimplePath(path: string, context: any): any {
    return bindingOptimizer.pathParser.getValue(context, path)
  }
  
  // 创建表达式评估函数
  private createExpressionFunction(expression: string): (context: any) => any {
    // 构建安全的表达式函数，只允许访问指定的属性和方法
    const safeContext = 'ctx'
    
    // 替换表达式中的数据源引用
    const safeExpression = expression
      .replace(/\$data\.(\w+)/g, `${safeContext}.$data.$1`)
      .replace(/\$props\.(\w+)/g, `${safeContext}.$props.$1`)
      .replace(/\$methods\.(\w+)/g, `${safeContext}.$methods.$1`)
      .replace(/\$filters\.(\w+)\(/g, `this.getFilter('$1')(`)
      .replace(/\$transformers\.(\w+)\(/g, `this.getTransformer('$1')(`)
    
    // 创建函数
    try {
      const fn = new Function(safeContext, `try { return (${safeExpression}); } catch (e) { console.error('Expression evaluation error:', e); return undefined; }`)
      return (context: any) => fn(context)
    } catch (error) {
      console.error(`Failed to create expression function for: ${expression}`, error)
      throw new Error(`Invalid expression: ${expression}`)
    }
  }
  
  // 创建数据绑定
  createBinding(target: any, targetKey: string, source: any, sourceExpression: string, options?: BindingOptions): DataBinding {
    return new DataBinding(target, targetKey, source, sourceExpression, options || {})
  }
  
  // 创建数据源
  async createDataSource(config: DataSourceConfig): Promise<DataSource> {
    const dataSource = new DataSource(config)
    await dataSource.initialize()
    return dataSource
  }
  
  // 解析数据绑定配置
  parseBindingConfig(config: DataBindingConfig): ParsedBinding {
    return {
      source: config.source,
      expression: config.expression || config.path || '',
      mode: config.mode || 'one-way',
      transform: config.transform,
      filter: config.filter,
      debounce: config.debounce || 0,
      throttle: config.throttle || 0
    }
  }
  
  // 执行数据过滤
  applyFilter(data: any, filter: DataFilter | string, params?: any): any {
    if (typeof filter === 'string') {
      const filterFn = this.getGlobalFilter(filter)
      if (!filterFn) {
        console.warn(`Filter not found: ${filter}`)
        return data
      }
      return filterFn(data, params)
    }
    return filter(data, params)
  }
  
  // 执行数据转换
  applyTransformer(data: any, transformer: DataTransformer | string, params?: any): any {
    if (typeof transformer === 'string') {
      const transformerFn = this.getGlobalTransformer(transformer)
      if (!transformerFn) {
        console.warn(`Transformer not found: ${transformer}`)
        return data
      }
      return transformerFn(data, params)
    }
    return transformer(data, params)
  }
  
  // 清除表达式缓存
  clearExpressionCache() {
    this.expressionCache.clear()
  }
  
  // 获取统计信息
  getStats(): BindingStats {
    return {
      activeContexts: this.bindingContexts.size,
      expressionCacheSize: this.expressionCache.size,
      globalFilters: this.globalFilters.size,
      globalTransformers: this.globalTransformers.size
    }
  }
}

// 绑定上下文类
class BindingContext {
  private contextId: string
  private data: Reactive<DataContext>
  private bindings: Map<string, DataBinding> = new Map()
  private dataSources: Map<string, DataSource> = new Map()
  private service: DataBindingService
  private watchers: Map<string, () => void> = new Map()
  private isDestroyed: boolean = false
  
  constructor(contextId: string, initialData: any = {}, service: DataBindingService) {
    this.contextId = contextId
    this.service = service
    
    // 初始化响应式数据上下文
    this.data = reactive({
      $data: initialData || {},
      $props: {},
      $methods: {},
      $filters: this.service.globalFilters,
      $transformers: this.service.globalTransformers
    })
  }
  
  // 设置数据
  setData(data: any): void {
    Object.assign(this.data.$data, data)
  }
  
  // 获取数据
  getData<T = any>(path?: string): T {
    if (!path) {
      return this.data.$data as T
    }
    
    const compiled = this.service.compileExpression(path)
    return compiled.evaluate(this.data.$data)
  }
  
  // 设置属性
  setProps(props: any): void {
    Object.assign(this.data.$props, props)
  }
  
  // 设置方法
  setMethods(methods: Record<string, Function>): void {
    Object.assign(this.data.$methods, methods)
  }
  
  // 注册数据源
  async registerDataSource(name: string, config: DataSourceConfig): Promise<void> {
    const dataSource = await this.service.createDataSource(config)
    this.dataSources.set(name, dataSource)
    
    // 将数据源挂载到数据上下文中
    this.data.$data[name] = dataSource.getData()
    
    // 监听数据源变化
    dataSource.onUpdate((newData: any) => {
      this.data.$data[name] = newData
    })
  }
  
  // 移除数据源
  removeDataSource(name: string): void {
    const dataSource = this.dataSources.get(name)
    if (dataSource) {
      dataSource.destroy()
      this.dataSources.delete(name)
      delete this.data.$data[name]
    }
  }
  
  // 创建数据绑定
  createBinding(target: any, targetKey: string, sourceExpression: string, options?: BindingOptions): DataBinding {
    const binding = this.service.createBinding(target, targetKey, this.data, sourceExpression, options)
    const bindingId = `${targetKey}-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`
    this.bindings.set(bindingId, binding)
    return binding
  }
  
  // 移除数据绑定
  removeBinding(binding: DataBinding): void {
    binding.destroy()
    // 从映射中删除绑定（需要遍历查找，因为我们只有binding对象）
    for (const [id, b] of this.bindings.entries()) {
      if (b === binding) {
        this.bindings.delete(id)
        break
      }
    }
  }
  
  // 计算表达式
  evaluateExpression<T = any>(expression: string): T {
    try {
      const compiled = this.service.compileExpression(expression)
      return compiled.evaluate(this.data)
    } catch (error) {
      console.error(`Failed to evaluate expression: ${expression}`, error)
      return undefined as T
    }
  }
  
  // 创建计算属性
  createComputed<T = any>(expression: string): Ref<T> {
    return computed(() => this.evaluateExpression<T>(expression))
  }
  
  // 监听表达式变化
  watchExpression(expression: string, callback: (newValue: any, oldValue: any) => void): () => void {
    // 使用Vue的watch函数监听响应式数据的变化
    const unwatch = watch(
      () => this.evaluateExpression(expression),
      callback,
      { deep: true, immediate: false }
    )
    
    const watcherId = `watch-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`
    this.watchers.set(watcherId, unwatch)
    
    // 返回解除监听的函数
    return () => {
      unwatch()
      this.watchers.delete(watcherId)
    }
  }
  
  // 批量创建数据绑定
  createBindings(bindings: Array<{
    target: any
    targetKey: string
    sourceExpression: string
    options?: BindingOptions
  }>): DataBinding[] {
    return bindings.map(b => this.createBinding(b.target, b.targetKey, b.sourceExpression, b.options))
  }
  
  // 应用过滤器
  applyFilter(data: any, filter: DataFilter | string, params?: any): any {
    return this.service.applyFilter(data, filter, params)
  }
  
  // 应用转换器
  applyTransformer(data: any, transformer: DataTransformer | string, params?: any): any {
    return this.service.applyTransformer(data, transformer, params)
  }
  
  // 获取上下文数据
  getContext(): DataContext {
    return this.data
  }
  
  // 销毁上下文
  destroy(): void {
    if (this.isDestroyed) return
    
    // 清理所有绑定
    for (const binding of this.bindings.values()) {
      binding.destroy()
    }
    this.bindings.clear()
    
    // 清理所有数据源
    for (const dataSource of this.dataSources.values()) {
      dataSource.destroy()
    }
    this.dataSources.clear()
    
    // 清理所有监听器
    for (const unwatch of this.watchers.values()) {
      unwatch()
    }
    this.watchers.clear()
    
    this.isDestroyed = true
  }
}

// 数据绑定类
class DataBinding {
  private target: any
  private targetKey: string
  private source: any
  private expression: string
  private options: BindingOptions
  private compiledExpression?: CompiledExpression
  private watcher?: () => void
  private isDestroyed: boolean = false
  private targetUpdateScheduler: any = null
  private sourceUpdateScheduler: any = null
  
  constructor(target: any, targetKey: string, source: any, expression: string, options: BindingOptions) {
    this.target = target
    this.targetKey = targetKey
    this.source = source
    this.expression = expression
    this.options = options
    
    // 初始化智能更新调度器
    this.targetUpdateScheduler = bindingOptimizer.createUpdateScheduler({
      debounce: options.debounce,
      throttle: options.throttle,
      batchUpdate: options.batchUpdate || false,
      priority: options.priority || 'medium'
    })
    
    this.sourceUpdateScheduler = bindingOptimizer.createUpdateScheduler({
      debounce: options.debounce,
      throttle: options.throttle,
      batchUpdate: options.batchUpdate || false,
      priority: options.priority || 'medium'
    })
    
    this.init()
  }
  
  // 初始化绑定
  private init(): void {
    // 编译表达式
    if (this.expression) {
      try {
        this.compiledExpression = dataBindingService.compileExpression(this.expression)
      } catch (error) {
        console.error(`Failed to compile binding expression: ${this.expression}`, error)
        return
      }
    }
    
    // 建立绑定关系
    if (this.options.mode === 'one-way' || this.options.mode === 'two-way') {
      this.setupSourceBinding()
    }
    
    if (this.options.mode === 'two-way') {
      this.setupTargetBinding()
    }
    
    // 初始同步
    if (this.options.mode !== 'one-way-target') {
      this.syncToTarget()
    }
  }
  
  // 设置源绑定（源 -> 目标）
  private setupSourceBinding(): void {
    if (!this.compiledExpression) return
    
    // 如果目标是响应式对象，使用Vue的watch
    if (isRef(this.source) || isReactive(this.source)) {
      this.watcher = watch(
        () => this.evaluateExpression(),
        (newValue, oldValue) => {
          if (newValue !== oldValue || this.options.forceUpdate) {
            this.scheduleUpdateToTarget(newValue)
          }
        },
        { deep: this.options.deep || true }
      )
    }
  }
  
  // 设置目标绑定（目标 -> 源）
  private setupTargetBinding(): void {
    // 如果目标是Vue组件或具有事件系统，监听其变化
    if (this.target && this.target.$on) {
      // Vue组件的事件监听
      const eventName = this.options.updateEvent || 'update:' + this.targetKey
      this.target.$on(eventName, (value: any) => {
        this.scheduleUpdateToSource(value)
      })
    } else if (this.target && this.target.addEventListener) {
      // DOM元素的事件监听
      const eventName = this.options.updateEvent || 'input'
      this.target.addEventListener(eventName, (event: Event) => {
        const target = event.target as HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement
        this.scheduleUpdateToSource(target.value)
      })
    } else {
      // 其他情况，轮询检查变化
      this.startPolling()
    }
  }
  
  // 轮询检查目标变化
  private startPolling(): void {
    let lastValue = this.getTargetValue()
    
    const poll = () => {
      if (this.isDestroyed) return
      
      const currentValue = this.getTargetValue()
      if (currentValue !== lastValue) {
        lastValue = currentValue
        this.scheduleUpdateToSource(currentValue)
      }
      
      setTimeout(poll, this.options.pollingInterval || 100)
    }
    
    setTimeout(poll, this.options.pollingInterval || 100)
  }
  
  // 评估表达式
  private evaluateExpression(): any {
    if (!this.compiledExpression) return this.source
    
    try {
      return this.compiledExpression.evaluate(this.source)
    } catch (error) {
      console.error(`Failed to evaluate binding expression: ${this.expression}`, error)
      return undefined
    }
  }
  
  // 获取目标值
  private getTargetValue(): any {
    if (isRef(this.target)) {
      return this.target.value
    }
    return this.target[this.targetKey]
  }
  
  // 设置目标值
  private setTargetValue(value: any): void {
    if (isRef(this.target)) {
      this.target.value = value
    } else if (this.target && typeof this.target === 'object') {
      this.target[this.targetKey] = value
    }
  }
  
  // 设置源值 - 使用优化的路径解析器
  private setSourceValue(value: any): void {
    // 使用高效路径解析器设置值
    if (this.expression && this.source) {
      bindingOptimizer.pathParser.setValue(this.source, this.expression, value)
    }
  }
  
  // 同步到目标
  private syncToTarget(): void {
    let value = this.evaluateExpression()
    
    // 应用转换器
    if (this.options.transform) {
      value = dataBindingService.applyTransformer(value, this.options.transform, this.options.transformParams)
    }
    
    // 应用过滤器
    if (this.options.filter) {
      value = dataBindingService.applyFilter(value, this.options.filter, this.options.filterParams)
    }
    
    this.setTargetValue(value)
  }
  
  // 同步到源
  private syncToSource(value: any): void {
    // 应用反向转换器
    if (this.options.reverseTransform) {
      value = dataBindingService.applyTransformer(value, this.options.reverseTransform, this.options.transformParams)
    }
    
    this.setSourceValue(value)
  }
  
  // 调度更新到目标 - 使用智能更新调度器
  private scheduleUpdateToTarget(value: any): void {
    this.targetUpdateScheduler.scheduleUpdate(value, () => {
      this.syncToTarget()
    })
  }
  
  // 调度更新到源 - 使用智能更新调度器
  private scheduleUpdateToSource(value: any): void {
    this.sourceUpdateScheduler.scheduleUpdate(value, (actualValue: any) => {
      this.syncToSource(actualValue)
    })
  }
  
  // 刷新绑定
  refresh(): void {
    this.syncToTarget()
  }
  
  // 销毁绑定
  destroy(): void {
    if (this.isDestroyed) return
    
    // 清理监听器
    if (this.watcher) {
      this.watcher()
    }
    
    // 销毁更新调度器
    this.targetUpdateScheduler.destroy()
    this.sourceUpdateScheduler.destroy()
    
    this.isDestroyed = true
  }
}

// 数据源类
class DataSource {
  private config: DataSourceConfig
  private data: any = null
  private updateHandlers: ((data: any) => void)[] = []
  private isInitialized: boolean = false
  private isDestroyed: boolean = false
  private pollingInterval?: number
  private wsConnection?: WebSocket
  
  constructor(config: DataSourceConfig) {
    this.config = config
  }
  
  // 初始化数据源
  async initialize(): Promise<void> {
    if (this.isInitialized) return
    
    try {
      this.data = await this.loadData()
      
      // 设置自动更新
      if (this.config.autoRefresh && this.config.refreshInterval) {
        this.startAutoRefresh()
      }
      
      // 设置WebSocket连接
      if (this.config.type === 'websocket') {
        await this.setupWebSocket()
      }
      
      this.isInitialized = true
    } catch (error) {
      console.error('Failed to initialize data source:', error)
      ElMessage.error('数据源初始化失败')
      throw error
    }
  }
  
  // 加载数据
  private async loadData(): Promise<any> {
    switch (this.config.type) {
      case 'api':
        return await this.loadApiData()
      case 'static':
        return this.config.data
      case 'function':
        return typeof this.config.fn === 'function' ? await this.config.fn(this.config.params) : null
      case 'websocket':
        return this.config.initialData || null
      case 'localStorage':
        return this.loadLocalStorageData()
      case 'sessionStorage':
        return this.loadSessionStorageData()
      default:
        throw new Error(`Unknown data source type: ${this.config.type}`)
    }
  }
  
  // 加载API数据
  private async loadApiData(): Promise<any> {
    if (!this.config.url) {
      throw new Error('API data source requires a URL')
    }
    
    const method = (this.config.method || 'get').toLowerCase()
    const params = this.config.params || {}
    const headers = this.config.headers || {}
    
    return await apiService[method as keyof typeof apiService](this.config.url, params, headers)
  }
  
  // 加载LocalStorage数据
  private loadLocalStorageData(): any {
    if (!this.config.key) {
      throw new Error('LocalStorage data source requires a key')
    }
    
    try {
      const value = localStorage.getItem(this.config.key)
      return value ? JSON.parse(value) : this.config.defaultValue
    } catch (error) {
      console.error('Failed to load data from localStorage:', error)
      return this.config.defaultValue
    }
  }
  
  // 加载SessionStorage数据
  private loadSessionStorageData(): any {
    if (!this.config.key) {
      throw new Error('SessionStorage data source requires a key')
    }
    
    try {
      const value = sessionStorage.getItem(this.config.key)
      return value ? JSON.parse(value) : this.config.defaultValue
    } catch (error) {
      console.error('Failed to load data from sessionStorage:', error)
      return this.config.defaultValue
    }
  }
  
  // 设置WebSocket连接
  private async setupWebSocket(): Promise<void> {
    if (!this.config.url) {
      throw new Error('WebSocket data source requires a URL')
    }
    
    return new Promise((resolve, reject) => {
      try {
        this.wsConnection = new WebSocket(this.config.url)
        
        this.wsConnection.onopen = () => {
          console.log('WebSocket connection established')
          
          // 发送初始消息
          if (this.config.initialMessage) {
            this.wsConnection?.send(JSON.stringify(this.config.initialMessage))
          }
          
          resolve()
        }
        
        this.wsConnection.onmessage = (event) => {
          try {
            const data = JSON.parse(event.data)
            this.updateData(data)
          } catch (error) {
            console.error('Failed to parse WebSocket message:', error)
          }
        }
        
        this.wsConnection.onerror = (error) => {
          console.error('WebSocket error:', error)
          reject(error)
        }
        
        this.wsConnection.onclose = () => {
          console.log('WebSocket connection closed')
          
          // 尝试重连
          if (this.config.autoReconnect && !this.isDestroyed) {
            setTimeout(() => this.setupWebSocket(), this.config.reconnectInterval || 5000)
          }
        }
      } catch (error) {
        reject(error)
      }
    })
  }
  
  // 开始自动刷新
  private startAutoRefresh(): void {
    if (!this.config.refreshInterval) return
    
    this.pollingInterval = window.setInterval(async () => {
      if (this.isDestroyed) return
      
      try {
        const newData = await this.loadData()
        this.updateData(newData)
      } catch (error) {
        console.error('Failed to refresh data source:', error)
      }
    }, this.config.refreshInterval)
  }
  
  // 更新数据
  private updateData(newData: any): void {
    // 应用数据处理器
    if (this.config.dataProcessor) {
      newData = this.config.dataProcessor(newData, this.data)
    }
    
    this.data = newData
    
    // 通知所有监听器
    this.notifyUpdateHandlers(newData)
  }
  
  // 通知更新处理器
  private notifyUpdateHandlers(data: any): void {
    for (const handler of this.updateHandlers) {
      try {
        handler(data)
      } catch (error) {
        console.error('Error in data source update handler:', error)
      }
    }
  }
  
  // 手动刷新数据
  async refresh(): Promise<void> {
    try {
      const newData = await this.loadData()
      this.updateData(newData)
    } catch (error) {
      console.error('Failed to refresh data source:', error)
      throw error
    }
  }
  
  // 获取数据
  getData(): any {
    return this.data
  }
  
  // 设置数据（仅适用于客户端数据源）
  async setData(data: any): Promise<void> {
    this.data = data
    
    // 如果是存储类型数据源，保存到存储
    if (this.config.type === 'localStorage' && this.config.key) {
      try {
        localStorage.setItem(this.config.key, JSON.stringify(data))
      } catch (error) {
        console.error('Failed to save data to localStorage:', error)
      }
    } else if (this.config.type === 'sessionStorage' && this.config.key) {
      try {
        sessionStorage.setItem(this.config.key, JSON.stringify(data))
      } catch (error) {
        console.error('Failed to save data to sessionStorage:', error)
      }
    } else if (this.config.type === 'websocket' && this.wsConnection && this.wsConnection.readyState === WebSocket.OPEN) {
      // 发送到WebSocket
      this.wsConnection.send(JSON.stringify(data))
    }
    
    // 通知更新
    this.notifyUpdateHandlers(data)
  }
  
  // 监听数据更新
  onUpdate(handler: (data: any) => void): () => void {
    this.updateHandlers.push(handler)
    
    // 返回取消监听的函数
    return () => {
      const index = this.updateHandlers.indexOf(handler)
      if (index > -1) {
        this.updateHandlers.splice(index, 1)
      }
    }
  }
  
  // 销毁数据源
  destroy(): void {
    if (this.isDestroyed) return
    
    // 清除定时器
    if (this.pollingInterval) {
      clearInterval(this.pollingInterval)
    }
    
    // 关闭WebSocket连接
    if (this.wsConnection) {
      this.wsConnection.close()
    }
    
    // 清理监听器
    this.updateHandlers = []
    
    this.isDestroyed = true
  }
}

// 类型定义
export interface BindingOptions {
  mode?: 'one-way' | 'one-way-target' | 'two-way'
  deep?: boolean
  transform?: DataTransformer | string
  reverseTransform?: DataTransformer | string
  filter?: DataFilter | string
  transformParams?: any
  filterParams?: any
  debounce?: number
  throttle?: number
  pollingInterval?: number
  updateEvent?: string
  forceUpdate?: boolean
}

export interface CompiledExpression {
  expression: string
  evaluate: (context: any) => any
}

export interface ParsedBinding {
  source: any
  expression: string
  mode: string
  transform?: DataTransformer | string
  filter?: DataFilter | string
  debounce: number
  throttle: number
}

export interface BindingStats {
  activeContexts: number
  expressionCacheSize: number
  globalFilters: number
  globalTransformers: number
}

export interface DataBinding {
  refresh: () => void
  destroy: () => void
}

export interface DataSource {
  getData: () => any
  setData: (data: any) => Promise<void>
  refresh: () => Promise<void>
  onUpdate: (handler: (data: any) => void) => () => void
  destroy: () => void
}

export interface BindingContext {
  setData: (data: any) => void
  getData: <T = any>(path?: string) => T
  setProps: (props: any) => void
  setMethods: (methods: Record<string, Function>) => void
  registerDataSource: (name: string, config: DataSourceConfig) => Promise<void>
  removeDataSource: (name: string) => void
  createBinding: (target: any, targetKey: string, sourceExpression: string, options?: BindingOptions) => DataBinding
  removeBinding: (binding: DataBinding) => void
  evaluateExpression: <T = any>(expression: string) => T
  createComputed: <T = any>(expression: string) => Ref<T>
  watchExpression: (expression: string, callback: (newValue: any, oldValue: any) => void) => () => void
  createBindings: (bindings: Array<{target: any; targetKey: string; sourceExpression: string; options?: BindingOptions}>) => DataBinding[]
  applyFilter: (data: any, filter: DataFilter | string, params?: any) => any
  applyTransformer: (data: any, transformer: DataTransformer | string, params?: any) => any
  getContext: () => DataContext
  destroy: () => void
}

// 导出服务实例
export const dataBindingService = new DataBindingService()

// 导出Vue插件
export default {
  install(app: any) {
    app.provide('dataBinding', dataBindingService)
    app.config.globalProperties.$dataBinding = dataBindingService
  }
}