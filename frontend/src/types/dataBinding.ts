import type { Ref } from 'vue'

// 数据绑定配置
export interface DataBindingConfig {
  // 数据源
  source?: any
  // 数据路径或表达式
  path?: string
  expression?: string
  // 绑定模式
  mode?: 'one-way' | 'one-way-target' | 'two-way'
  // 数据转换函数
  transform?: DataTransformer | string
  // 反向转换函数（用于双向绑定）
  reverseTransform?: DataTransformer | string
  // 数据过滤函数
  filter?: DataFilter | string
  // 转换参数
  transformParams?: any
  // 过滤参数
  filterParams?: any
  // 防抖时间（毫秒）
  debounce?: number
  // 节流时间（毫秒）
  throttle?: number
  // 轮询间隔（毫秒）
  pollingInterval?: number
  // 更新事件名称
  updateEvent?: string
  // 是否强制更新
  forceUpdate?: boolean
}

// 数据源配置
export interface DataSourceConfig {
  // 数据源类型
  type: 'api' | 'static' | 'function' | 'websocket' | 'localStorage' | 'sessionStorage'
  // API相关配置
  url?: string
  method?: string
  params?: any
  headers?: Record<string, string>
  // 静态数据
  data?: any
  // 函数数据源
  fn?: Function
  // WebSocket配置
  initialMessage?: any
  autoReconnect?: boolean
  reconnectInterval?: number
  keepAlive?: boolean
  timeout?: number
  // 存储相关配置
  key?: string
  defaultValue?: any
  // 自动刷新配置
  autoRefresh?: boolean
  refreshInterval?: number
  // 数据处理器
  dataProcessor?: (newData: any, oldData?: any) => any
  // 数据源处理器（自定义处理逻辑）
  handler?: (params?: any) => Promise<any>
  // 初始数据
  initialData?: any
}

// 数据转换函数
export type DataTransformer = (data: any, params?: any) => any

// 数据过滤函数
export type DataFilter = (data: any, params?: any) => any

// 绑定表达式
export interface BindingExpression {
  // 表达式字符串
  expression: string
  // 编译后的表达式函数
  compiled?: (context: any) => any
}

// 数据上下文
export interface DataContext {
  // 数据源数据
  $data: Record<string, any>
  // 组件属性
  $props: Record<string, any>
  // 组件方法
  $methods: Record<string, Function>
  // 过滤器
  $filters: Record<string, DataFilter>
  // 转换器
  $transformers: Record<string, DataTransformer>
}

// 数据绑定
export interface DataBinding {
  // 刷新绑定
  refresh: () => void
  // 销毁绑定
  destroy: () => void
}

// 数据源
export interface DataSource {
  // 获取数据
  getData: () => any
  // 设置数据
  setData: (data: any) => Promise<void>
  // 刷新数据
  refresh: () => Promise<void>
  // 监听数据更新
  onUpdate: (handler: (data: any) => void) => () => void
  // 销毁数据源
  destroy: () => void
}

// 绑定上下文
export interface BindingContext {
  // 设置数据
  setData: (data: any) => void
  // 获取数据
  getData: <T = any>(path?: string) => T
  // 设置属性
  setProps: (props: any) => void
  // 设置方法
  setMethods: (methods: Record<string, Function>) => void
  // 注册数据源
  registerDataSource: (name: string, config: DataSourceConfig) => Promise<void>
  // 移除数据源
  removeDataSource: (name: string) => void
  // 创建数据绑定
  createBinding: (target: any, targetKey: string, sourceExpression: string, options?: DataBindingConfig) => DataBinding
  // 移除数据绑定
  removeBinding: (binding: DataBinding) => void
  // 计算表达式
  evaluateExpression: <T = any>(expression: string) => T
  // 创建计算属性
  createComputed: <T = any>(expression: string) => Ref<T>
  // 监听表达式变化
  watchExpression: (expression: string, callback: (newValue: any, oldValue: any) => void) => () => void
  // 批量创建数据绑定
  createBindings: (bindings: Array<{target: any; targetKey: string; sourceExpression: string; options?: DataBindingConfig}>) => DataBinding[]
  // 应用过滤器
  applyFilter: (data: any, filter: DataFilter | string, params?: any) => any
  // 应用转换器
  applyTransformer: (data: any, transformer: DataTransformer | string, params?: any) => any
  // 获取上下文数据
  getContext: () => DataContext
  // 销毁上下文
  destroy: () => void
}

// 绑定表达式编译结果
export interface CompiledExpression {
  // 原始表达式
  expression: string
  // 评估函数
  evaluate: (context: any) => any
}

// 解析后的绑定
export interface ParsedBinding {
  // 数据源
  source: any
  // 表达式
  expression: string
  // 绑定模式
  mode: string
  // 转换器
  transform?: DataTransformer | string
  // 过滤器
  filter?: DataFilter | string
  // 防抖时间
  debounce: number
  // 节流时间
  throttle: number
}

// 绑定统计信息
export interface BindingStats {
  // 活跃上下文数量
  activeContexts: number
  // 表达式缓存大小
  expressionCacheSize: number
  // 全局过滤器数量
  globalFilters: number
  // 全局转换器数量
  globalTransformers: number
}

// 数据绑定服务
export interface DataBindingService {
  // 创建绑定上下文
  createBindingContext: (contextId: string, initialData?: any) => BindingContext
  // 销毁绑定上下文
  destroyBindingContext: (contextId: string) => void
  // 获取绑定上下文
  getBindingContext: (contextId: string) => BindingContext | undefined
  // 注册全局过滤器
  registerGlobalFilter: (name: string, filter: DataFilter) => void
  // 注册全局转换器
  registerGlobalTransformer: (name: string, transformer: DataTransformer) => void
  // 获取全局过滤器
  getGlobalFilter: (name: string) => DataFilter | undefined
  // 获取全局转换器
  getGlobalTransformer: (name: string) => DataTransformer | undefined
  // 编译绑定表达式
  compileExpression: (expression: string) => CompiledExpression
  // 创建数据绑定
  createBinding: (target: any, targetKey: string, source: any, sourceExpression: string, options?: DataBindingConfig) => DataBinding
  // 创建数据源
  createDataSource: (config: DataSourceConfig) => Promise<DataSource>
  // 解析数据绑定配置
  parseBindingConfig: (config: DataBindingConfig) => ParsedBinding
  // 应用过滤器
  applyFilter: (data: any, filter: DataFilter | string, params?: any) => any
  // 应用转换器
  applyTransformer: (data: any, transformer: DataTransformer | string, params?: any) => any
  // 清除表达式缓存
  clearExpressionCache: () => void
  // 获取统计信息
  getStats: () => BindingStats
}

// 数据处理器
export interface DataProcessor {
  // 处理数据
  process: (data: any) => any
  // 名称
  name: string
  // 描述
  description?: string
}

// 数据验证规则
export interface ValidationRule {
  // 验证函数
  validator: (value: any) => boolean | Promise<boolean>
  // 错误消息
  message: string
  // 是否必填
  required?: boolean
  // 触发条件
  trigger?: 'blur' | 'change' | 'input'
}

// 数据验证结果
export interface ValidationResult {
  // 是否有效
  isValid: boolean
  // 错误信息
  errors: string[]
  // 验证字段
  field?: string
}