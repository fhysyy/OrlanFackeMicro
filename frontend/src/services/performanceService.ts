import { reactive, toRefs, nextTick } from 'vue'

// 性能指标类型定义
export interface PerformanceMetric {
  id: string
  name: string
  value: number
  unit: string
  timestamp: number
  metadata?: Record<string, any>
}

// 性能监控配置
export interface PerformanceMonitorConfig {
  enabled: boolean
  samplingRate: number // 0-1之间的采样率
  maxHistorySize: number
  slowOperationThreshold: number // 慢操作阈值，毫秒
  autoLog: boolean
}

// 组件性能数据
export interface ComponentPerformanceData {
  componentId: string
  componentName: string
  renderCount: number
  renderTime: number // 总计渲染时间
  lastRenderTime: number // 最后一次渲染时间
  averageRenderTime: number // 平均渲染时间
  propsSize: number // props大小，字节
  reactivePropsCount: number // 响应式属性数量
  lastRenderTimestamp: number
  renderTimeDistribution: number[] // 最近几次的渲染时间分布
}

// 绑定性能数据
export interface BindingPerformanceData {
  bindingId: string
  sourceType: string
  targetType: string
  updateCount: number
  updateTime: number // 总计更新时间
  lastUpdateTime: number // 最后一次更新时间
  averageUpdateTime: number // 平均更新时间
  dataSize: number // 数据大小，字节
  lastUpdateTimestamp: number
  updateTimeDistribution: number[] // 最近几次的更新时间分布
}

// 资源使用情况
export interface ResourceUsage {
  memoryUsage: {
    used: number // 已用内存，MB
    total?: number // 总内存，MB
    percentage?: number // 内存使用率，%
  }
  cpuUsage?: number // CPU使用率，%
  domNodes: number // DOM节点数量
  eventListeners: number // 事件监听器数量
  activeComponents: number // 活跃组件数量
  activeBindings: number // 活跃数据绑定数量
  timestamp: number
}

// 操作性能数据
export interface OperationPerformanceData {
  operationId: string
  operationName: string
  startTime: number
  endTime?: number
  duration?: number
  status: 'pending' | 'completed' | 'failed'
  error?: string
  metadata?: Record<string, any>
}

// 性能服务类
class PerformanceService {
  // 默认配置
  private defaultConfig: PerformanceMonitorConfig = {
    enabled: true,
    samplingRate: 1.0,
    maxHistorySize: 1000,
    slowOperationThreshold: 100,
    autoLog: false
  }

  // 配置
  private config = reactive<PerformanceMonitorConfig>({ ...this.defaultConfig })

  // 性能指标历史
  private metricsHistory: PerformanceMetric[] = []

  // 组件性能数据映射
  private componentPerformanceMap = new Map<string, ComponentPerformanceData>()

  // 绑定性能数据映射
  private bindingPerformanceMap = new Map<string, BindingPerformanceData>()

  // 资源使用历史
  private resourceUsageHistory: ResourceUsage[] = []

  // 操作性能跟踪
  private operationsInProgress = new Map<string, OperationPerformanceData>()
  private completedOperations: OperationPerformanceData[] = []

  // 性能警告
  private performanceWarnings: {
    id: string
    type: string
    message: string
    severity: 'low' | 'medium' | 'high'
    timestamp: number
    metadata?: Record<string, any>
  }[] = []

  // 构造函数
  constructor() {
    // 初始化性能监控
    this.initMonitoring()
    
    // 注册全局错误处理
    this.registerErrorHandlers()
  }

  // 初始化监控
  private initMonitoring() {
    // 定期收集资源使用情况
    setInterval(() => {
      if (this.config.enabled) {
        this.collectResourceUsage()
      }
    }, 5000)
    
    // 清理旧数据
    setInterval(() => {
      this.cleanupOldData()
    }, 30000)
  }

  // 注册错误处理器
  private registerErrorHandlers() {
    window.addEventListener('error', (event) => {
      this.recordError('global_error', event.error?.message || 'Unknown error', {
        error: event.error,
        filename: event.filename,
        lineno: event.lineno,
        colno: event.colno
      })
    })

    window.addEventListener('unhandledrejection', (event) => {
      this.recordError('promise_rejection', 'Unhandled promise rejection', {
        reason: event.reason
      })
    })
  }

  // 清理旧数据
  private cleanupOldData() {
    // 清理指标历史
    if (this.metricsHistory.length > this.config.maxHistorySize) {
      this.metricsHistory = this.metricsHistory.slice(-this.config.maxHistorySize)
    }
    
    // 清理已完成的操作
    if (this.completedOperations.length > this.config.maxHistorySize) {
      this.completedOperations = this.completedOperations.slice(-this.config.maxHistorySize)
    }
    
    // 清理资源使用历史
    if (this.resourceUsageHistory.length > 100) { // 保留最近100条
      this.resourceUsageHistory = this.resourceUsageHistory.slice(-100)
    }
    
    // 清理性能警告
    if (this.performanceWarnings.length > 50) { // 保留最近50条
      this.performanceWarnings = this.performanceWarnings.slice(-50)
    }
  }

  // 收集资源使用情况
  private collectResourceUsage() {
    // 估算内存使用
    const memoryUsage = this.estimateMemoryUsage()
    
    // 计算DOM节点数量
    const domNodes = this.countDomNodes()
    
    // 计算事件监听器数量
    const eventListeners = this.countEventListeners()
    
    const resourceUsage: ResourceUsage = {
      memoryUsage,
      domNodes,
      eventListeners,
      activeComponents: this.componentPerformanceMap.size,
      activeBindings: this.bindingPerformanceMap.size,
      timestamp: Date.now()
    }
    
    // 添加到历史
    this.resourceUsageHistory.push(resourceUsage)
    
    // 检查性能问题
    this.checkPerformanceIssues(resourceUsage)
    
    return resourceUsage
  }

  // 估算内存使用
  private estimateMemoryUsage(): ResourceUsage['memoryUsage'] {
    // 这里使用 navigator.memory API (仅在Chrome中可用) 或估算方法
    let usedMemory = 0
    let totalMemory = undefined
    let percentage = undefined
    
    // @ts-ignore
    if (navigator && navigator.memory) {
      // @ts-ignore
      usedMemory = navigator.memory.usedJSHeapSize / (1024 * 1024) // 转换为MB
      // @ts-ignore
      totalMemory = navigator.memory.jsHeapSizeLimit / (1024 * 1024) // 转换为MB
      percentage = (usedMemory / totalMemory) * 100
    } else {
      // 估算方法：根据DOM节点数量和对象数量估算
      const domNodesCount = this.countDomNodes()
      // 假设每个DOM节点平均占用约100字节
      usedMemory = (domNodesCount * 100) / (1024 * 1024)
    }
    
    return {
      used: Math.round(usedMemory * 100) / 100,
      total: totalMemory,
      percentage: percentage !== undefined ? Math.round(percentage * 100) / 100 : undefined
    }
  }

  // 计算DOM节点数量
  private countDomNodes(): number {
    try {
      return document.querySelectorAll('*').length
    } catch (error) {
      return 0
    }
  }

  // 计算事件监听器数量（近似值）
  private countEventListeners(): number {
    // 这是一个近似值，因为浏览器没有提供直接的API来获取所有事件监听器
    let count = 0
    
    try {
      // 统计主要DOM元素的事件监听器
      const elements = document.querySelectorAll('button, a, input, select, textarea')
      
      // 估算每个交互元素平均有2个事件监听器
      count = elements.length * 2
      
      // 加上文档和窗口的事件监听器
      count += 10 // 估计值
    } catch (error) {
      // 出错时返回保守估计
      return 100
    }
    
    return count
  }

  // 检查性能问题
  private checkPerformanceIssues(resourceUsage: ResourceUsage) {
    const warnings: typeof this.performanceWarnings = []
    
    // 检查内存使用
    if (resourceUsage.memoryUsage.percentage && resourceUsage.memoryUsage.percentage > 80) {
      warnings.push({
        id: `mem_${Date.now()}`,
        type: 'high_memory_usage',
        message: `内存使用率过高: ${resourceUsage.memoryUsage.percentage.toFixed(1)}%`,
        severity: 'high',
        timestamp: Date.now(),
        metadata: { memoryUsage: resourceUsage.memoryUsage }
      })
    }
    
    // 检查DOM节点数量
    if (resourceUsage.domNodes > 5000) {
      warnings.push({
        id: `dom_${Date.now()}`,
        type: 'high_dom_nodes',
        message: `DOM节点数量过多: ${resourceUsage.domNodes}个`,
        severity: 'medium',
        timestamp: Date.now(),
        metadata: { domNodes: resourceUsage.domNodes }
      })
    }
    
    // 检查事件监听器数量
    if (resourceUsage.eventListeners > 1000) {
      warnings.push({
        id: `event_${Date.now()}`,
        type: 'high_event_listeners',
        message: `事件监听器数量过多: ${resourceUsage.eventListeners}个`,
        severity: 'medium',
        timestamp: Date.now(),
        metadata: { eventListeners: resourceUsage.eventListeners }
      })
    }
    
    // 添加警告
    if (warnings.length > 0) {
      this.performanceWarnings.push(...warnings)
      
      // 自动记录警告
      if (this.config.autoLog) {
        warnings.forEach(warning => {
          console.warn(`[Performance Warning] ${warning.message}`, warning.metadata)
        })
      }
    }
  }

  // 记录性能指标
  recordMetric(metric: Omit<PerformanceMetric, 'timestamp'>): void {
    if (!this.config.enabled) return
    
    // 根据采样率决定是否记录
    if (Math.random() > this.config.samplingRate) return
    
    const fullMetric: PerformanceMetric = {
      ...metric,
      timestamp: Date.now()
    }
    
    // 添加到历史
    this.metricsHistory.push(fullMetric)
    
    // 自动记录
    if (this.config.autoLog) {
      console.debug(`[Performance Metric] ${metric.name}: ${metric.value}${metric.unit}`, metric.metadata)
    }
  }

  // 开始操作跟踪
  startOperation(operationName: string, metadata?: Record<string, any>): string {
    if (!this.config.enabled) return ''
    
    const operationId = `${operationName}_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`
    const operationData: OperationPerformanceData = {
      operationId,
      operationName,
      startTime: Date.now(),
      status: 'pending',
      metadata
    }
    
    // 记录正在进行的操作
    this.operationsInProgress.set(operationId, operationData)
    
    return operationId
  }

  // 结束操作跟踪
  endOperation(operationId: string, metadata?: Record<string, any>): void {
    if (!this.config.enabled || !operationId) return
    
    const operationData = this.operationsInProgress.get(operationId)
    if (!operationData) return
    
    // 更新操作数据
    operationData.endTime = Date.now()
    operationData.duration = operationData.endTime - operationData.startTime
    operationData.status = 'completed'
    operationData.metadata = {
      ...operationData.metadata,
      ...metadata
    }
    
    // 检查是否为慢操作
    if (operationData.duration! > this.config.slowOperationThreshold) {
      this.recordSlowOperation(operationData)
    }
    
    // 移动到已完成操作列表
    this.operationsInProgress.delete(operationId)
    this.completedOperations.push(operationData)
    
    // 自动记录
    if (this.config.autoLog) {
      console.debug(`[Operation Completed] ${operationData.operationName}: ${operationData.duration}ms`, operationData.metadata)
    }
  }

  // 记录操作失败
  failOperation(operationId: string, error: string, metadata?: Record<string, any>): void {
    if (!this.config.enabled || !operationId) return
    
    const operationData = this.operationsInProgress.get(operationId)
    if (!operationData) return
    
    // 更新操作数据
    operationData.endTime = Date.now()
    operationData.duration = operationData.endTime - operationData.startTime
    operationData.status = 'failed'
    operationData.error = error
    operationData.metadata = {
      ...operationData.metadata,
      ...metadata
    }
    
    // 移动到已完成操作列表
    this.operationsInProgress.delete(operationId)
    this.completedOperations.push(operationData)
    
    // 记录错误
    this.recordError('operation_failed', error, operationData.metadata)
    
    // 自动记录
    if (this.config.autoLog) {
      console.error(`[Operation Failed] ${operationData.operationName}: ${error}`, operationData.metadata)
    }
  }

  // 记录慢操作
  private recordSlowOperation(operation: OperationPerformanceData): void {
    const warning = {
      id: `slow_op_${Date.now()}`,
      type: 'slow_operation',
      message: `慢操作检测: ${operation.operationName} (${operation.duration}ms)`,
      severity: 'medium',
      timestamp: Date.now(),
      metadata: {
        operationName: operation.operationName,
        duration: operation.duration,
        threshold: this.config.slowOperationThreshold,
        ...operation.metadata
      }
    }
    
    this.performanceWarnings.push(warning)
    
    // 自动记录
    if (this.config.autoLog) {
      console.warn(`[Slow Operation] ${warning.message}`, warning.metadata)
    }
  }

  // 记录错误
  recordError(errorType: string, message: string, metadata?: Record<string, any>): void {
    const warning = {
      id: `error_${Date.now()}`,
      type: errorType,
      message: `错误: ${message}`,
      severity: 'high' as const,
      timestamp: Date.now(),
      metadata
    }
    
    this.performanceWarnings.push(warning)
    
    // 自动记录
    if (this.config.autoLog) {
      console.error(`[Error] ${warning.message}`, warning.metadata)
    }
  }

  // 记录组件渲染性能
  recordComponentRender(
    componentId: string,
    componentName: string,
    renderTime: number,
    propsSize: number = 0,
    reactivePropsCount: number = 0
  ): void {
    if (!this.config.enabled) return
    
    let componentData = this.componentPerformanceMap.get(componentId)
    
    if (!componentData) {
      // 创建新的组件性能数据
      componentData = {
        componentId,
        componentName,
        renderCount: 0,
        renderTime: 0,
        lastRenderTime: 0,
        averageRenderTime: 0,
        propsSize,
        reactivePropsCount,
        lastRenderTimestamp: 0,
        renderTimeDistribution: []
      }
      this.componentPerformanceMap.set(componentId, componentData)
    }
    
    // 更新组件性能数据
    componentData.renderCount++
    componentData.renderTime += renderTime
    componentData.lastRenderTime = renderTime
    componentData.averageRenderTime = componentData.renderTime / componentData.renderCount
    componentData.propsSize = propsSize
    componentData.reactivePropsCount = reactivePropsCount
    componentData.lastRenderTimestamp = Date.now()
    
    // 更新渲染时间分布
    componentData.renderTimeDistribution.push(renderTime)
    if (componentData.renderTimeDistribution.length > 20) {
      componentData.renderTimeDistribution = componentData.renderTimeDistribution.slice(-20)
    }
    
    // 检查是否为慢渲染
    if (renderTime > this.config.slowOperationThreshold) {
      this.recordSlowRender(componentData)
    }
    
    // 自动记录
    if (this.config.autoLog) {
      console.debug(`[Component Render] ${componentName}: ${renderTime}ms`, {
        componentId,
        renderCount: componentData.renderCount,
        averageRenderTime: componentData.averageRenderTime.toFixed(2)
      })
    }
  }

  // 记录慢渲染
  private recordSlowRender(componentData: ComponentPerformanceData): void {
    const warning = {
      id: `slow_render_${Date.now()}`,
      type: 'slow_component_render',
      message: `组件渲染过慢: ${componentData.componentName} (${componentData.lastRenderTime}ms)`,
      severity: 'medium',
      timestamp: Date.now(),
      metadata: {
        componentName: componentData.componentName,
        componentId: componentData.componentId,
        renderTime: componentData.lastRenderTime,
        threshold: this.config.slowOperationThreshold,
        renderCount: componentData.renderCount
      }
    }
    
    this.performanceWarnings.push(warning)
    
    // 自动记录
    if (this.config.autoLog) {
      console.warn(`[Slow Render] ${warning.message}`, warning.metadata)
    }
  }

  // 记录绑定性能
  recordBindingUpdate(
    bindingId: string,
    sourceType: string,
    targetType: string,
    updateTime: number,
    dataSize: number = 0
  ): void {
    if (!this.config.enabled) return
    
    let bindingData = this.bindingPerformanceMap.get(bindingId)
    
    if (!bindingData) {
      // 创建新的绑定性能数据
      bindingData = {
        bindingId,
        sourceType,
        targetType,
        updateCount: 0,
        updateTime: 0,
        lastUpdateTime: 0,
        averageUpdateTime: 0,
        dataSize,
        lastUpdateTimestamp: 0,
        updateTimeDistribution: []
      }
      this.bindingPerformanceMap.set(bindingId, bindingData)
    }
    
    // 更新绑定性能数据
    bindingData.updateCount++
    bindingData.updateTime += updateTime
    bindingData.lastUpdateTime = updateTime
    bindingData.averageUpdateTime = bindingData.updateTime / bindingData.updateCount
    bindingData.dataSize = dataSize
    bindingData.lastUpdateTimestamp = Date.now()
    
    // 更新更新时间分布
    bindingData.updateTimeDistribution.push(updateTime)
    if (bindingData.updateTimeDistribution.length > 20) {
      bindingData.updateTimeDistribution = bindingData.updateTimeDistribution.slice(-20)
    }
    
    // 检查是否为慢更新
    if (updateTime > this.config.slowOperationThreshold / 2) { // 绑定更新的阈值较低
      this.recordSlowBindingUpdate(bindingData)
    }
    
    // 自动记录
    if (this.config.autoLog) {
      console.debug(`[Binding Update] ${sourceType} -> ${targetType}: ${updateTime}ms`, {
        bindingId,
        updateCount: bindingData.updateCount,
        averageUpdateTime: bindingData.averageUpdateTime.toFixed(2)
      })
    }
  }

  // 记录慢绑定更新
  private recordSlowBindingUpdate(bindingData: BindingPerformanceData): void {
    const warning = {
      id: `slow_binding_${Date.now()}`,
      type: 'slow_binding_update',
      message: `数据绑定更新过慢: ${bindingData.sourceType} -> ${bindingData.targetType} (${bindingData.lastUpdateTime}ms)`,
      severity: 'medium',
      timestamp: Date.now(),
      metadata: {
        sourceType: bindingData.sourceType,
        targetType: bindingData.targetType,
        bindingId: bindingData.bindingId,
        updateTime: bindingData.lastUpdateTime,
        threshold: this.config.slowOperationThreshold / 2,
        updateCount: bindingData.updateCount
      }
    }
    
    this.performanceWarnings.push(warning)
    
    // 自动记录
    if (this.config.autoLog) {
      console.warn(`[Slow Binding] ${warning.message}`, warning.metadata)
    }
  }

  // 设置配置
  setConfig(newConfig: Partial<PerformanceMonitorConfig>): void {
    Object.assign(this.config, newConfig)
  }

  // 获取配置
  getConfig(): PerformanceMonitorConfig {
    return { ...this.config }
  }

  // 获取性能指标历史
  getMetricsHistory(limit?: number): PerformanceMetric[] {
    if (limit) {
      return this.metricsHistory.slice(-limit)
    }
    return [...this.metricsHistory]
  }

  // 获取组件性能数据
  getComponentPerformanceData(componentId?: string): ComponentPerformanceData[] {
    if (componentId) {
      const data = this.componentPerformanceMap.get(componentId)
      return data ? [data] : []
    }
    return Array.from(this.componentPerformanceMap.values())
  }

  // 获取绑定性能数据
  getBindingPerformanceData(bindingId?: string): BindingPerformanceData[] {
    if (bindingId) {
      const data = this.bindingPerformanceMap.get(bindingId)
      return data ? [data] : []
    }
    return Array.from(this.bindingPerformanceMap.values())
  }

  // 获取资源使用历史
  getResourceUsageHistory(limit?: number): ResourceUsage[] {
    if (limit) {
      return this.resourceUsageHistory.slice(-limit)
    }
    return [...this.resourceUsageHistory]
  }

  // 获取最近的资源使用情况
  getCurrentResourceUsage(): ResourceUsage | undefined {
    return this.resourceUsageHistory[this.resourceUsageHistory.length - 1]
  }

  // 获取性能警告
  getPerformanceWarnings(severity?: 'low' | 'medium' | 'high'): typeof this.performanceWarnings {
    if (severity) {
      return this.performanceWarnings.filter(warning => warning.severity === severity)
    }
    return [...this.performanceWarnings]
  }

  // 清除性能警告
  clearPerformanceWarnings(): void {
    this.performanceWarnings = []
  }

  // 获取操作数据
  getOperations(status?: 'pending' | 'completed' | 'failed'): OperationPerformanceData[] {
    let operations: OperationPerformanceData[] = []
    
    // 添加进行中的操作
    if (!status || status === 'pending') {
      operations = operations.concat(Array.from(this.operationsInProgress.values()))
    }
    
    // 添加已完成的操作
    if (!status || status === 'completed' || status === 'failed') {
      operations = operations.concat(
        this.completedOperations.filter(op => !status || op.status === status)
      )
    }
    
    // 按时间倒序排序
    return operations.sort((a, b) => b.startTime - a.startTime)
  }

  // 计算性能统计信息
  getPerformanceStats(): {
    averageRenderTime: number
    averageBindingUpdateTime: number
    slowRenderCount: number
    slowBindingCount: number
    resourceUsage: ResourceUsage | undefined
    errorCount: number
  } {
    const componentData = this.getComponentPerformanceData()
    const bindingData = this.getBindingPerformanceData()
    
    // 计算平均渲染时间
    const averageRenderTime = componentData.length > 0
      ? componentData.reduce((sum, data) => sum + data.averageRenderTime, 0) / componentData.length
      : 0
    
    // 计算平均绑定更新时间
    const averageBindingUpdateTime = bindingData.length > 0
      ? bindingData.reduce((sum, data) => sum + data.averageUpdateTime, 0) / bindingData.length
      : 0
    
    // 计算慢渲染次数
    const slowRenderCount = componentData.filter(
      data => data.lastRenderTime > this.config.slowOperationThreshold
    ).length
    
    // 计算慢绑定更新次数
    const slowBindingCount = bindingData.filter(
      data => data.lastUpdateTime > this.config.slowOperationThreshold / 2
    ).length
    
    // 计算错误数量
    const errorCount = this.performanceWarnings.filter(
      warning => warning.severity === 'high'
    ).length
    
    return {
      averageRenderTime: Math.round(averageRenderTime * 100) / 100,
      averageBindingUpdateTime: Math.round(averageBindingUpdateTime * 100) / 100,
      slowRenderCount,
      slowBindingCount,
      resourceUsage: this.getCurrentResourceUsage(),
      errorCount
    }
  }

  // 生成性能报告
  generatePerformanceReport(): string {
    const stats = this.getPerformanceStats()
    
    let report = `性能报告 - ${new Date().toLocaleString()}\n`
    report += `==============================\n\n`
    
    // 渲染性能
    report += `渲染性能:\n`
    report += `  平均渲染时间: ${stats.averageRenderTime.toFixed(2)}ms\n`
    report += `  慢渲染组件数量: ${stats.slowRenderCount}\n`
    report += `  活跃组件数量: ${this.componentPerformanceMap.size}\n\n`
    
    // 绑定性能
    report += `数据绑定性能:\n`
    report += `  平均更新时间: ${stats.averageBindingUpdateTime.toFixed(2)}ms\n`
    report += `  慢绑定更新数量: ${stats.slowBindingCount}\n`
    report += `  活跃绑定数量: ${this.bindingPerformanceMap.size}\n\n`
    
    // 资源使用
    if (stats.resourceUsage) {
      report += `资源使用:\n`
      report += `  内存使用: ${stats.resourceUsage.memoryUsage.used.toFixed(2)}MB`
      if (stats.resourceUsage.memoryUsage.percentage) {
        report += ` (${stats.resourceUsage.memoryUsage.percentage.toFixed(1)}%)`
      }
      report += `\n`
      report += `  DOM节点数量: ${stats.resourceUsage.domNodes}\n`
      report += `  事件监听器数量: ${stats.resourceUsage.eventListeners}\n\n`
    }
    
    // 错误和警告
    report += `错误和警告:\n`
    report += `  错误数量: ${stats.errorCount}\n`
    report += `  警告数量: ${this.performanceWarnings.length - stats.errorCount}\n\n`
    
    // 操作统计
    const pendingOperations = Array.from(this.operationsInProgress.values()).length
    const completedOperations = this.completedOperations.filter(op => op.status === 'completed').length
    const failedOperations = this.completedOperations.filter(op => op.status === 'failed').length
    
    report += `操作统计:\n`
    report += `  进行中的操作: ${pendingOperations}\n`
    report += `  已完成的操作: ${completedOperations}\n`
    report += `  失败的操作: ${failedOperations}\n`
    
    return report
  }

  // 导出性能数据
  exportPerformanceData(): {
    config: PerformanceMonitorConfig
    metrics: PerformanceMetric[]
    componentPerformance: ComponentPerformanceData[]
    bindingPerformance: BindingPerformanceData[]
    resourceUsage: ResourceUsage[]
    warnings: typeof this.performanceWarnings
    operations: OperationPerformanceData[]
    report: string
  } {
    return {
      config: this.getConfig(),
      metrics: this.getMetricsHistory(),
      componentPerformance: this.getComponentPerformanceData(),
      bindingPerformance: this.getBindingPerformanceData(),
      resourceUsage: this.getResourceUsageHistory(),
      warnings: this.getPerformanceWarnings(),
      operations: this.getOperations(),
      report: this.generatePerformanceReport()
    }
  }
}

// 创建性能服务单例
const performanceService = new PerformanceService()

// 提供Vue组合式API
export function usePerformance() {
  // 创建性能指标
  const recordMetric = (metric: Omit<PerformanceMetric, 'timestamp'>) => {
    performanceService.recordMetric(metric)
  }

  // 跟踪操作
  const trackOperation = {
    start: (operationName: string, metadata?: Record<string, any>) => {
      return performanceService.startOperation(operationName, metadata)
    },
    end: (operationId: string, metadata?: Record<string, any>) => {
      performanceService.endOperation(operationId, metadata)
    },
    fail: (operationId: string, error: string, metadata?: Record<string, any>) => {
      performanceService.failOperation(operationId, error, metadata)
    }
  }

  // 跟踪组件渲染
  const trackComponentRender = (
    componentId: string,
    componentName: string,
    renderTime: number,
    propsSize?: number,
    reactivePropsCount?: number
  ) => {
    performanceService.recordComponentRender(
      componentId,
      componentName,
      renderTime,
      propsSize,
      reactivePropsCount
    )
  }

  // 跟踪绑定更新
  const trackBindingUpdate = (
    bindingId: string,
    sourceType: string,
    targetType: string,
    updateTime: number,
    dataSize?: number
  ) => {
    performanceService.recordBindingUpdate(
      bindingId,
      sourceType,
      targetType,
      updateTime,
      dataSize
    )
  }

  // 性能钩子 - 用于组件
  const useComponentPerformance = (componentName: string, componentId?: string) => {
    const id = componentId || `${componentName}_${Math.random().toString(36).substr(2, 9)}`
    let startTime = 0
    
    // 在组件挂载前记录时间
    const beforeRender = () => {
      startTime = performance.now()
    }
    
    // 在组件更新后记录渲染时间
    const afterRender = () => {
      nextTick(() => {
        const renderTime = performance.now() - startTime
        performanceService.recordComponentRender(id, componentName, renderTime)
      })
    }
    
    return {
      id,
      beforeRender,
      afterRender
    }
  }

  // 获取性能统计
  const performanceStats = reactive({
    get() {
      return performanceService.getPerformanceStats()
    }
  })

  // 获取配置
  const config = toRefs(performanceService.getConfig())

  // 设置配置
  const updateConfig = (newConfig: Partial<PerformanceMonitorConfig>) => {
    performanceService.setConfig(newConfig)
  }

  return {
    recordMetric,
    trackOperation,
    trackComponentRender,
    trackBindingUpdate,
    useComponentPerformance,
    performanceStats,
    config,
    updateConfig,
    getPerformanceReport: performanceService.generatePerformanceReport.bind(performanceService),
    exportPerformanceData: performanceService.exportPerformanceData.bind(performanceService),
    getResourceUsage: performanceService.getCurrentResourceUsage.bind(performanceService),
    getPerformanceWarnings: performanceService.getPerformanceWarnings.bind(performanceService)
  }
}

// 导出服务单例
export { performanceService }

// 默认导出
export default performanceService