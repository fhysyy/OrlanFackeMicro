// 事件配置接口
export interface EventConfig {
  id: string
  name: string
  description?: string
  dataSchema?: any
  sources?: string[]
  targets?: string[]
  filters?: EventFilter[]
  transformers?: EventTransformer[]
  enabled?: boolean
  priority?: number
  debounce?: number
  throttle?: number
  handlers?: ActionConfig[]
}

// 事件处理器类型
export type EventHandler = (eventData: any, context: EventContext) => Promise<any> | any

// 事件上下文接口
export interface EventContext {
  eventName: string
  source: string
  timestamp: number
  data: any
  [key: string]: any
}

// 事件过滤器接口
export type EventFilter = (eventData: any, params?: any) => boolean

// 事件转换器接口
export type EventTransformer = (eventData: any, params?: any) => any

// 事件拦截器接口
export interface EventInterceptor {
  before?: (context: EventContext) => boolean | void
  after?: (context: EventContext, results: any[], errors: Error[]) => void
}

// 动作配置接口
export interface ActionConfig {
  id?: string
  type: ActionType
  description?: string
  config?: ActionConfigMap[ActionType]
  enabled?: boolean
  condition?: string | ((eventData: any) => boolean | Promise<boolean>)
  onSuccess?: ActionConfig
  onError?: ActionConfig
  priority?: number
  timeout?: number
  retry?: {
    attempts: number
    delay: number
  }
}

// 动作类型枚举
export type ActionType = 
  | 'apiCall'
  | 'stateUpdate'
  | 'navigate'
  | 'showMessage'
  | 'showConfirm'
  | 'showNotification'
  | 'customScript'
  | 'componentMethod'
  | 'eventEmit'
  | 'wait'
  | 'loop'
  | 'conditional'
  | 'variableAssign'
  | 'log'
  | 'debug'
  | 'dataTransform'
  | 'dataFilter'
  | 'storage'
  | 'clipboard'

// 动作配置映射
export interface ActionConfigMap {
  apiCall: {
    url: string
    method?: 'get' | 'post' | 'put' | 'delete' | 'patch'
    params?: any
    headers?: Record<string, string>
    timeout?: number
    withCredentials?: boolean
    responseType?: 'json' | 'text' | 'blob' | 'arraybuffer'
  }
  
  stateUpdate: {
    target?: 'global' | 'local' | 'context' | string
    updates: Record<string, any>
  }
  
  navigate: {
    path: string
    replace?: boolean
    params?: Record<string, any>
    query?: Record<string, any>
  }
  
  showMessage: {
    message: string
    type?: 'success' | 'warning' | 'info' | 'error'
    duration?: number
    showClose?: boolean
    grouping?: boolean
    customClass?: string
  }
  
  showConfirm: {
    title?: string
    message: string
    confirmButtonText?: string
    cancelButtonText?: string
    type?: 'success' | 'warning' | 'info' | 'error'
    customClass?: string
  }
  
  showNotification: {
    title?: string
    message: string
    type?: 'success' | 'warning' | 'info' | 'error'
    duration?: number
    showClose?: boolean
    offset?: number
    position?: 'top-right' | 'top-left' | 'bottom-right' | 'bottom-left'
    customClass?: string
  }
  
  customScript: {
    script: string
    context?: Record<string, any>
    timeout?: number
  }
  
  componentMethod: {
    componentId: string
    method: string
    params?: any[]
  }
  
  eventEmit: {
    eventName: string
    data?: any
    contextId?: string
  }
  
  wait: {
    delay?: number
    until?: () => boolean | Promise<boolean>
  }
  
  loop: {
    iterations?: number
    condition?: (eventData: any, loopContext: { iteration: number; results: any[] }) => boolean
    actions?: ActionConfig[]
  }
  
  conditional: {
    condition: string | ((eventData: any) => boolean | Promise<boolean>)
    ifTrue?: ActionConfig
    ifFalse?: ActionConfig
  }
  
  variableAssign: {
    variableName: string
    value: any
    target?: 'global' | 'local' | 'context'
  }
  
  log: {
    message: string
    level?: 'debug' | 'info' | 'warn' | 'error'
    data?: any[]
  }
  
  debug: {
    break?: boolean
    inspect?: any
    stack?: boolean
  }
  
  dataTransform: {
    data: any
    transformers: Array<{
      name: string
      params?: any
    }>
  }
  
  dataFilter: {
    data: any[]
    filter: string | ((item: any, index: number, array: any[]) => boolean)
    take?: number
    skip?: number
    sortBy?: string | ((a: any, b: any) => number)
    order?: 'asc' | 'desc'
  }
  
  storage: {
    operation: 'set' | 'get' | 'remove' | 'clear' | 'has'
    key: string
    value?: any
    type?: 'localStorage' | 'sessionStorage'
    expiration?: number
  }
  
  clipboard: {
    operation: 'copy' | 'paste'
    text?: string
    target?: HTMLElement
  }
}

// 组件事件定义接口
export interface ComponentEventDefinition {
  name: string
  description?: string
  payloadSchema?: any
  example?: any
  documentation?: string
}

// 组件动作定义接口
export interface ComponentActionDefinition {
  name: string
  description?: string
  parameters?: Array<{
    name: string
    type: string
    required: boolean
    description?: string
    default?: any
  }>
  returnType?: string
  example?: any
  documentation?: string
}

// 事件流定义接口
export interface EventFlow {
  id: string
  name: string
  description?: string
  trigger: {
    eventName: string
    filter?: string | EventFilter
  }
  actions: ActionConfig[]
  enabled?: boolean
  tags?: string[]
  priority?: number
  createdAt?: string
  updatedAt?: string
  author?: string
  version?: string
  category?: string
  documentation?: string
}

// 事件流执行上下文
export interface EventFlowContext {
  flow: EventFlow
  eventData: any
  variables: Record<string, any>
  results: any[]
  errors: Error[]
  stepIndex: number
  startTime: number
  executionId: string
  cancelled: boolean
  
  // 上下文方法
  cancel: () => void
  getVariable: (name: string) => any
  setVariable: (name: string, value: any) => void
  getStepResult: (index: number) => any
  addError: (error: Error) => void
  isCancelled: () => boolean
}

// 事件总线配置选项
export interface EventBusOptions {
  debug?: boolean
  maxHistorySize?: number
  enableValidation?: boolean
  enableMetrics?: boolean
  defaultDebounce?: number
  defaultThrottle?: number
}

// 事件执行结果
export interface EventExecutionResult {
  success: boolean
  eventName: string
  payload: any
  results: any[]
  errors: Error[]
  executionTime: number
  timestamp: number
  source: string
  handlersExecuted: number
  handlersFailed: number
}

// 动作执行结果
export interface ActionExecutionResult {
  success: boolean
  actionId?: string
  actionType: string
  result?: any
  error?: Error
  executionTime: number
  retries: number
  cancelled: boolean
}

// 事件统计信息
export interface EventMetrics {
  eventName: string
  totalEmitted: number
  totalSuccess: number
  totalFailed: number
  totalCancelled: number
  averageExecutionTime: number
  minExecutionTime: number
  maxExecutionTime: number
  lastEmitted: number
  handlers: Array<{
    id: string
    executed: number
    failed: number
    averageTime: number
  }>
}

// 表达式解析选项
export interface ExpressionOptions {
  context?: Record<string, any>
  timeout?: number
  allowedGlobals?: string[]
  maxDepth?: number
  enableCache?: boolean
}

// 事件钩子接口
export interface EventHooks {
  beforeEmit?: (eventName: string, data: any) => void | Promise<void>
  afterEmit?: (result: EventExecutionResult) => void | Promise<void>
  beforeAction?: (action: ActionConfig, data: any) => void | Promise<void>
  afterAction?: (result: ActionExecutionResult) => void | Promise<void>
  onError?: (error: Error, context: { eventName?: string; action?: ActionConfig }) => void | Promise<void>
}

// 条件表达式类型
export type ConditionExpression = string | ((context: any) => boolean | Promise<boolean>)

// 表达式求值器接口
export interface ExpressionEvaluator {
  evaluate(expression: string, context: any, options?: ExpressionOptions): any
  compile(expression: string, options?: ExpressionOptions): (context: any) => any
  validate(expression: string): boolean
  parse(expression: string): any
}

// 序列化/反序列化接口
export interface EventSerializer {
  serialize(event: any): string
  deserialize(data: string): any
}

// 错误处理器接口
export interface ErrorHandler {
  handleError(error: Error, context: { event?: string; action?: string; source?: string }): void | Promise<void>
  formatError(error: Error): string
  logError(error: Error, context?: any): void
}

// 事件监听选项
export interface EventListenerOptions {
  once?: boolean
  priority?: number
  debounce?: number
  throttle?: number
  filter?: EventFilter
  transformer?: EventTransformer
  timeout?: number
}

// 内置事件定义
export const BUILT_IN_EVENTS = {
  // 生命周期事件
  APP_READY: 'app:ready',
  APP_ERROR: 'app:error',
  APP_WARNING: 'app:warning',
  APP_INFO: 'app:info',
  
  // 组件事件
  COMPONENT_MOUNTED: 'component:mounted',
  COMPONENT_UPDATED: 'component:updated',
  COMPONENT_UNMOUNTED: 'component:unmounted',
  COMPONENT_ERROR: 'component:error',
  
  // 用户交互事件
  USER_CLICK: 'user:click',
  USER_DOUBLE_CLICK: 'user:dblclick',
  USER_MOUSEOVER: 'user:mouseover',
  USER_MOUSEOUT: 'user:mouseout',
  USER_INPUT: 'user:input',
  USER_CHANGE: 'user:change',
  USER_FOCUS: 'user:focus',
  USER_BLUR: 'user:blur',
  USER_SUBMIT: 'user:submit',
  USER_CANCEL: 'user:cancel',
  USER_SELECT: 'user:select',
  USER_SCROLL: 'user:scroll',
  USER_KEYDOWN: 'user:keydown',
  USER_KEYUP: 'user:keyup',
  
  // 数据事件
  DATA_LOADED: 'data:loaded',
  DATA_CHANGED: 'data:changed',
  DATA_ERROR: 'data:error',
  DATA_VALIDATION_ERROR: 'data:validation:error',
  
  // 网络事件
  NETWORK_ONLINE: 'network:online',
  NETWORK_OFFLINE: 'network:offline',
  NETWORK_ERROR: 'network:error',
  API_REQUEST_START: 'api:request:start',
  API_REQUEST_COMPLETE: 'api:request:complete',
  API_REQUEST_ERROR: 'api:request:error',
  
  // 路由事件
  ROUTE_CHANGE_START: 'route:change:start',
  ROUTE_CHANGE_COMPLETE: 'route:change:complete',
  ROUTE_CHANGE_ERROR: 'route:change:error',
  
  // 状态管理事件
  STATE_CHANGED: 'state:changed',
  STATE_RESET: 'state:reset',
  
  // 低代码平台特定事件
  PAGE_LOADED: 'page:loaded',
  PAGE_UPDATED: 'page:updated',
  COMPONENT_ADDED: 'component:added',
  COMPONENT_REMOVED: 'component:removed',
  COMPONENT_MOVED: 'component:moved',
  PROPERTY_CHANGED: 'property:changed',
  LAYOUT_CHANGED: 'layout:changed',
  THEME_CHANGED: 'theme:changed',
  SAVE_START: 'save:start',
  SAVE_COMPLETE: 'save:complete',
  SAVE_ERROR: 'save:error',
  EXPORT_START: 'export:start',
  EXPORT_COMPLETE: 'export:complete',
  EXPORT_ERROR: 'export:error',
  IMPORT_START: 'import:start',
  IMPORT_COMPLETE: 'import:complete',
  IMPORT_ERROR: 'import:error',
  
  // 调试事件
  DEBUG_LOG: 'debug:log',
  DEBUG_WARN: 'debug:warn',
  DEBUG_ERROR: 'debug:error',
  DEBUG_INFO: 'debug:info',
  DEBUG_BREAKPOINT: 'debug:breakpoint',
} as const

// 内置动作类型
export const BUILT_IN_ACTION_TYPES = {
  // 数据操作
  API_CALL: 'apiCall',
  STATE_UPDATE: 'stateUpdate',
  
  // UI交互
  SHOW_MESSAGE: 'showMessage',
  SHOW_CONFIRM: 'showConfirm',
  SHOW_NOTIFICATION: 'showNotification',
  
  // 导航
  NAVIGATE: 'navigate',
  
  // 逻辑控制
  CONDITIONAL: 'conditional',
  LOOP: 'loop',
  WAIT: 'wait',
  
  // 通用操作
  CUSTOM_SCRIPT: 'customScript',
  EVENT_EMIT: 'eventEmit',
  COMPONENT_METHOD: 'componentMethod',
  
  // 调试
  LOG: 'log',
  DEBUG: 'debug',
} as const

// 导出工具函数
export function createEventFilter(expression: string): EventFilter {
  return (eventData: any) => {
    try {
      // 简单的表达式评估实现
      // 在生产环境中应该使用更安全的表达式解析器
      const keys = Object.keys(eventData)
      const values = Object.values(eventData)
      const fn = new Function(...keys, `return ${expression}`)
      return Boolean(fn(...values))
    } catch (error) {
      console.error('Error in event filter:', error)
      return false
    }
  }
}

export function createEventTransformer(expression: string): EventTransformer {
  return (eventData: any) => {
    try {
      const keys = Object.keys(eventData)
      const values = Object.values(eventData)
      const fn = new Function(...keys, `return ${expression}`)
      return fn(...values)
    } catch (error) {
      console.error('Error in event transformer:', error)
      return eventData
    }
  }
}

export function isEventConfig(value: any): value is EventConfig {
  return value && 
    typeof value === 'object' && 
    typeof value.id === 'string' && 
    typeof value.name === 'string'
}

export function isActionConfig(value: any): value is ActionConfig {
  return value && 
    typeof value === 'object' && 
    typeof value.type === 'string' && 
    Object.values(BUILT_IN_ACTION_TYPES).includes(value.type as any)
}

export function validateEventConfig(config: EventConfig): { valid: boolean; errors: string[] } {
  const errors: string[] = []
  
  if (!config.id || typeof config.id !== 'string') {
    errors.push('Event config must have a valid id')
  }
  
  if (!config.name || typeof config.name !== 'string') {
    errors.push('Event config must have a valid name')
  }
  
  if (config.handlers && !Array.isArray(config.handlers)) {
    errors.push('Event handlers must be an array')
  }
  
  return {
    valid: errors.length === 0,
    errors
  }
}

export function validateActionConfig(config: ActionConfig): { valid: boolean; errors: string[] } {
  const errors: string[] = []
  
  if (!config.type || typeof config.type !== 'string') {
    errors.push('Action config must have a valid type')
  }
  
  if (!Object.values(BUILT_IN_ACTION_TYPES).includes(config.type as any)) {
    errors.push(`Invalid action type: ${config.type}`)
  }
  
  // 根据动作类型验证配置
  if (config.type === 'apiCall' && (!config.config || typeof config.config.url !== 'string')) {
    errors.push('API call action requires a URL')
  }
  
  return {
    valid: errors.length === 0,
    errors
  }
}