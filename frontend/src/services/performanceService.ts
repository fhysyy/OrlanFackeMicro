import { ref, reactive } from 'vue'

// 简化的性能指标类型
export interface PerformanceMetric {
  id: string
  name: string
  value: number
  unit: string
  timestamp: number
  metadata?: Record<string, any>
}

// 简化的性能监控配置
export interface PerformanceConfig {
  enabled: boolean
  maxHistorySize: number
  slowOperationThreshold: number
}

// 简化的性能服务
class PerformanceService {
  private config: PerformanceConfig
  private metrics: Map<string, PerformanceMetric> = new Map()
  private operations: Map<string, { startTime: number; metadata?: any }> = new Map()

  constructor(config: Partial<PerformanceConfig> = {}) {
    this.config = {
      enabled: true,
      maxHistorySize: 1000,
      slowOperationThreshold: 1000,
      ...config
    }
  }

  /**
   * 记录性能指标
   */
  recordMetric(metric: Omit<PerformanceMetric, 'timestamp'>): void {
    if (!this.config.enabled) return

    const fullMetric: PerformanceMetric = {
      ...metric,
      timestamp: Date.now()
    }

    this.metrics.set(metric.id, fullMetric)

    // 如果超过最大历史记录数，删除最旧的记录
    if (this.metrics.size > this.config.maxHistorySize) {
      const oldestKey = this.metrics.keys().next().value
      if (oldestKey) {
        this.metrics.delete(oldestKey)
      }
    }
  }

  /**
   * 开始操作计时
   */
  startOperation(operationName: string, metadata?: any): string {
    const operationId = `${operationName}_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`
    this.operations.set(operationId, {
      startTime: performance.now(),
      metadata
    })
    return operationId
  }

  /**
   * 结束操作计时
   */
  endOperation(operationId: string, metadata?: any): number | null {
    const operation = this.operations.get(operationId)
    if (!operation) return null

    const endTime = performance.now()
    const duration = endTime - operation.startTime

    this.operations.delete(operationId)

    this.recordMetric({
      id: operationId,
      name: `Operation: ${operationId}`,
      value: Math.round(duration),
      unit: 'ms',
      metadata: {
        ...operation.metadata,
        ...metadata,
        duration
      }
    })

    return duration
  }

  /**
   * 记录操作失败
   */
  failOperation(operationId: string, errorMessage: string, metadata?: any): void {
    const operation = this.operations.get(operationId)
    if (!operation) return

    this.operations.delete(operationId)

    this.recordMetric({
      id: `${operationId}_error`,
      name: `Operation Error: ${operationId}`,
      value: 0,
      unit: 'error',
      metadata: {
        ...operation.metadata,
        ...metadata,
        errorMessage
      }
    })
  }

  /**
   * 记录错误
   */
  recordError(errorType: string, errorMessage: string, metadata?: any): void {
    this.recordMetric({
      id: `error_${errorType}_${Date.now()}`,
      name: `Error: ${errorType}`,
      value: 0,
      unit: 'error',
      metadata: {
        errorMessage,
        ...metadata
      }
    })
  }

  /**
   * 获取所有指标
   */
  getMetrics(): PerformanceMetric[] {
    return Array.from(this.metrics.values())
  }

  /**
   * 获取特定指标
   */
  getMetric(id: string): PerformanceMetric | undefined {
    return this.metrics.get(id)
  }

  /**
   * 清除所有指标
   */
  clearMetrics(): void {
    this.metrics.clear()
  }

  /**
   * 获取性能统计
   */
  getStats() {
    const metrics = this.getMetrics()
    const errors = metrics.filter(m => m.unit === 'error')
    const slowOperations = metrics.filter(m => 
      m.unit === 'ms' && m.value > this.config.slowOperationThreshold
    )

    return {
      totalMetrics: metrics.length,
      errorCount: errors.length,
      slowOperationCount: slowOperations.length,
      averageOperationTime: metrics
        .filter(m => m.unit === 'ms')
        .reduce((sum, m) => sum + m.value, 0) / metrics.filter(m => m.unit === 'ms').length || 0
    }
  }

  /**
   * 启用/禁用性能监控
   */
  setEnabled(enabled: boolean): void {
    this.config.enabled = enabled
  }

  /**
   * 更新配置
   */
  updateConfig(config: Partial<PerformanceConfig>): void {
    this.config = { ...this.config, ...config }
  }
}

// 创建单例实例
export const performanceService = new PerformanceService()

export { PerformanceService }