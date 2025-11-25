// 事件处理器类型
export type EventHandler<T = unknown> = (data: T) => void | Promise<void>

// 事件配置
export interface EventConfig<T = unknown> {
  name: string
  handler: EventHandler<T>
  once?: boolean
  priority?: number
}

// 动作配置
export interface ActionConfig<T = unknown> {
  type: string
  payload?: T
  target?: string
}

// 事件上下文
export interface EventContext {
  id: string
  eventBus: any // 使用 any 暂时避免循环依赖
  data?: Record<string, unknown>
  isActive: boolean
}

// 事件过滤器
export type EventFilter<T = unknown> = (data: T) => boolean

// 事件转换器
export type EventTransformer<T = unknown, R = unknown> = (data: T) => R

// 事件拦截器
export interface EventInterceptor<T = unknown> {
  before?: (data: T) => T | void
  after?: (data: T) => void
  error?: (error: Error, data: T) => void
}

// 事件记录
export interface EventRecord {
  id: string
  name: string
  data: unknown
  timestamp: number
  source: string
  status: 'success' | 'error' | 'canceled'
}

// 标准事件类型
export const StandardEvents = {
  // 页面事件
  PAGE_LOAD: 'page:load',
  PAGE_UNLOAD: 'page:unload',
  PAGE_ERROR: 'page:error',
  
  // 组件事件
  COMPONENT_MOUNT: 'component:mount',
  COMPONENT_UNMOUNT: 'component:unmount',
  COMPONENT_UPDATE: 'component:update',
  
  // 用户事件
  USER_LOGIN: 'user:login',
  USER_LOGOUT: 'user:logout',
  USER_REGISTER: 'user:register',
  
  // 错误事件
  ERROR_OCCURRED: 'error:occurred',
  ERROR_HANDLED: 'error:handled',
  
  // 网络事件
  API_REQUEST: 'api:request',
  API_SUCCESS: 'api:success',
  API_ERROR: 'api:error',
  
  // 应用事件
  APP_INIT: 'app:init',
  APP_READY: 'app:ready',
  APP_ERROR: 'app:error'
} as const

// 事件动作类型
export interface EventActions {
  // 导航动作
  navigate: {
    path: string
    params?: Record<string, string>
    query?: Record<string, string>
  }
  
  // 提示动作
  notify: {
    type: 'success' | 'warning' | 'error' | 'info'
    message: string
    duration?: number
  }
  
  // 模态框动作
  modal: {
    type: 'open' | 'close'
    component?: string
    data?: Record<string, unknown>
  }
  
  // 数据动作
  data: {
    type: 'create' | 'update' | 'delete' | 'query'
    entity: string
    payload?: Record<string, unknown>
  }
  
  // 日志动作
  log: {
    message: string
    level?: 'debug' | 'info' | 'warn' | 'error'
    data?: unknown[]
  }
  
  debug: {
    break?: boolean
    inspect?: unknown
  }
}

// 事件总线配置选项
export interface EventBusOptions {
  debug?: boolean
  maxHistorySize?: number
  enableValidation?: boolean
}

// 事件上下文选项
export interface EventContextOptions {
  autoCleanup?: boolean
  isolated?: boolean
}