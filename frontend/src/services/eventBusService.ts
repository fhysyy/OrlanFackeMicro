import type { App } from 'vue'
import type { EventHandler } from '@/types/event'
import { logger } from '@/utils/logger'

// 简化的事件总线服务
class EventBusService {
  private appContext: App | null = null
  private eventHandlers: Map<string, EventHandler[]> = new Map()
  private isDebug: boolean = false
  
  // 初始化服务
  initialize(app: App, options: { debug?: boolean } = {}) {
    this.appContext = app
    this.isDebug = options.debug || false
  }
  
  // 注册事件监听器
  on(eventName: string, handler: EventHandler) {
    if (!this.eventHandlers.has(eventName)) {
      this.eventHandlers.set(eventName, [])
    }
    this.eventHandlers.get(eventName)!.push(handler)
    
    if (this.isDebug) {
      logger.debug(`EventBus registered handler for event: ${eventName}`)
    }
  }
  
  // 注册一次性事件监听器
  once(eventName: string, handler: EventHandler) {
    const onceHandler = (data: any) => {
      handler(data)
      this.off(eventName, onceHandler)
    }
    this.on(eventName, onceHandler)
  }
  
  // 移除事件监听器
  off(eventName: string, handler?: EventHandler) {
    if (!this.eventHandlers.has(eventName)) return
    
    if (!handler) {
      this.eventHandlers.delete(eventName)
    } else {
      const handlers = this.eventHandlers.get(eventName)!
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
  emit(eventName: string, data?: any) {
    if (this.isDebug) {
      logger.debug(`EventBus emitting event: ${eventName}`, data)
    }
    
    const handlers = this.eventHandlers.get(eventName)
    if (handlers) {
      handlers.forEach(handler => {
        try {
          handler(data)
        } catch (error) {
          logger.error(`EventBus error in handler for event ${eventName}:`, error)
        }
      })
    }
  }
  
  // 清除所有事件监听器
  clear() {
    this.eventHandlers.clear()
    if (this.isDebug) {
      logger.debug('EventBus all event handlers cleared')
    }
  }
  
  // 获取事件监听器数量
  getListenerCount(eventName?: string): number {
    if (!eventName) {
      return Array.from(this.eventHandlers.values()).reduce((total, handlers) => total + handlers.length, 0)
    }
    return this.eventHandlers.get(eventName)?.length || 0
  }
}

// 创建单例实例
export const eventBusService = new EventBusService()

export { EventBusService }