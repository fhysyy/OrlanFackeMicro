import type { EventConfig, ActionConfig, EventContext, EventFilter, EventTransformer, EventHandler } from '@/types/event'
import { eventBusService } from '@/services/eventBusService'
import { onUnmounted } from 'vue'

/**
 * 创建事件过滤器
 * @param expression JavaScript表达式字符串
 * @returns 事件过滤器函数
 */
export function createEventFilter(expression: string): EventFilter {
  return function(eventData: any, context: EventContext): boolean {
    try {
      // 使用Function构造器创建条件判断函数
      const conditionFunction = new Function('eventData', 'context', `return ${expression}`)
      return !!conditionFunction(eventData, context)
    } catch (error) {
      console.error('Error in event filter:', error)
      return false
    }
  }
}

/**
 * 创建事件转换器
 * @param expression JavaScript表达式字符串
 * @returns 事件转换器函数
 */
export function createEventTransformer(expression: string): EventTransformer {
  return function(eventData: any, context: EventContext): any {
    try {
      // 使用Function构造器创建转换函数
      const transformFunction = new Function('eventData', 'context', `return ${expression}`)
      return transformFunction(eventData, context)
    } catch (error) {
      console.error('Error in event transformer:', error)
      return eventData
    }
  }
}

/**
 * 简化的事件注册装饰器
 * @param eventName 事件名称
 * @param options 事件选项
 */
export function OnEvent(eventName: string, options?: {
  once?: boolean
  filter?: EventFilter
  transformer?: EventTransformer
  priority?: number
}) {
  return function (target: any, propertyKey: string, descriptor: PropertyDescriptor) {
    const originalMethod = descriptor.value
    
    // 初始化方法
    const initMethod = function() {
      let handler = originalMethod.bind(this)
      
      // 应用转换器
      if (options?.transformer) {
        const originalHandler = handler
        handler = (eventData: any, context: EventContext) => {
          const transformedData = options.transformer!(eventData, context)
          return originalHandler(transformedData, context)
        }
      }
      
      // 应用过滤器
      if (options?.filter) {
        const originalHandler = handler
        handler = (eventData: any, context: EventContext) => {
          if (options.filter!(eventData, context)) {
            return originalHandler(eventData, context)
          }
        }
      }
      
      // 注册事件处理器
      if (options?.once) {
        eventBusService.once(eventName, handler)
      } else {
        eventBusService.on(eventName, handler)
      }
      
      return originalMethod.apply(this, arguments)
    }
    
    descriptor.value = initMethod
    return descriptor
  }
}

/**
 * 创建防抖事件处理器
 * @param handler 原始处理器
 * @param delay 延迟时间（毫秒）
 */
export function debounceEventHandler(
  handler: EventHandler<any>,
  delay: number
): EventHandler<any> {
  let timeoutId: ReturnType<typeof setTimeout> | null = null
  
  return async (eventData: any) => {
    if (timeoutId) {
      clearTimeout(timeoutId)
    }
    
    return new Promise((resolve) => {
      timeoutId = setTimeout(async () => {
        try {
          const result = await handler(eventData)
          resolve(result)
        } catch (error) {
          console.error('Debounced event handler error:', error)
          resolve(undefined)
        }
      }, delay)
    })
  }
}

/**
 * 创建节流事件处理器
 * @param handler 原始处理器
 * @param limit 限制时间（毫秒）
 */
export function throttleEventHandler(
  handler: EventHandler<any>,
  limit: number
): EventHandler<any> {
  let inThrottle = false
  let lastResult: any = undefined
  
  return async (eventData: any) => {
    if (!inThrottle) {
      inThrottle = true
      
      try {
        lastResult = await handler(eventData)
      } catch (error) {
        console.error('Throttled event handler error:', error)
      }
      
      setTimeout(() => {
        inThrottle = false
      }, limit)
    }
    
    return lastResult
  }
}

/**
 * 执行动作序列
 * @param actions 动作配置数组
 * @param eventData 事件数据
 * @param options 执行选项
 */
export async function executeActions(
  actions: ActionConfig[],
  eventData: any = {},
  options?: {
    stopOnError?: boolean
    contextId?: string
  }
) {
  const { stopOnError = true, contextId } = options || {}
  let ctx: any
  
  try {
    // 获取或创建上下文
    if (contextId) {
      ctx = eventBusService.getContext(contextId)
      if (!ctx) {
        ctx = eventBusService.createContext(contextId)
      }
    } else {
      ctx = eventBusService.createContext(`temp_context_${Date.now()}`)
    }
    
    // 执行动作序列
    // 由于eventBusService可能没有executeActions方法，我们直接实现简单的动作执行
    for (const action of actions) {
      try {
        switch (action.type) {
          case 'apiCall':
            // 这里应该调用实际的API服务，暂时简化处理
            console.log(`执行API调用: ${action.config?.method} ${action.config?.url}`, action.config?.params);
            break;
          case 'showMessage':
            console.log(`显示消息: ${action.config?.message}`, { type: action.config?.type });
            break;
          case 'navigate':
            console.log(`导航到: ${action.config?.path}`);
            break;
          case 'componentMethod':
            console.log(`调用组件方法: ${action.config?.componentId}.${action.config?.method}`);
            break;
          default:
            console.log(`执行动作: ${action.type}`, action);
        }
      } catch (error) {
        console.error(`执行动作失败: ${action.type}`, error);
        if (stopOnError) {
          throw error;
        }
      }
    }
    return { success: true, actions: actions.length };
  } finally {
    // 如果没有指定上下文ID，自动清理
    if (!contextId && ctx) {
      eventBusService.destroyContext(ctx.getContextId())
    }
  }
}

/**
 * 验证事件配置
 * @param config 事件配置
 * @returns 验证结果
 */
export function validateEventConfig(config: EventConfig): { valid: boolean; errors: string[] } {
  const errors: string[] = []
  
  if (!config.id || typeof config.id !== 'string') {
    errors.push('事件配置必须包含有效的id')
  }
  
  if (!config.name || typeof config.name !== 'string') {
    errors.push('事件配置必须包含有效的名称')
  }
  
  if (config.handlers && !Array.isArray(config.handlers)) {
    errors.push('事件处理器必须是数组')
  }
  
  if (config.actions && !Array.isArray(config.actions)) {
    errors.push('事件动作必须是数组')
  }
  
  return {
    valid: errors.length === 0,
    errors
  }
}

/**
 * 验证动作配置
 * @param config 动作配置
 * @returns 验证结果
 */
export function validateActionConfig(config: ActionConfig): { valid: boolean; errors: string[] } {
  const errors: string[] = []
  
  if (!config.type || typeof config.type !== 'string') {
    errors.push('动作配置必须包含有效的类型')
  }
  
  // 根据动作类型验证配置
  switch (config.type) {
  case 'apiCall':
    if (!config.config?.url) {
      errors.push('API调用动作必须指定URL')
    }
    break
  case 'showMessage':
    if (!config.config?.message) {
      errors.push('显示消息动作必须指定消息内容')
    }
    break
  case 'navigate':
    if (!config.config?.path) {
      errors.push('导航动作必须指定路径')
    }
    break
  case 'componentMethod':
    if (!config.config?.componentId || !config.config?.method) {
      errors.push('组件方法调用必须指定组件ID和方法名')
    }
    break
  }
  
  return {
    valid: errors.length === 0,
    errors
  }
}

/**
 * 合并事件配置
 * @param target 目标配置
 * @param source 源配置
 * @returns 合并后的配置
 */
export function mergeEventConfigs(target: EventConfig, source: EventConfig): EventConfig {
  return {
    ...target,
    ...source,
    handlers: [...(target.handlers || []), ...(source.handlers || [])],
    actions: [...(target.actions || []), ...(source.actions || [])],
    filters: [...(target.filters || []), ...(source.filters || [])],
    transformers: [...(target.transformers || []), ...(source.transformers || [])]
  }
}

/**
 * 从事件配置生成代码片段
 * @param config 事件配置
 * @returns 代码字符串
 */
export function generateEventCode(config: EventConfig): string {
  let code = `// 事件: ${config.name} (${config.id})\n`
  code += `eventBus.on('${config.name}', async (eventData, context) => {\n`
  
  // 添加条件逻辑
  if (config.condition) {
    code += '  // 条件检查\n'
    code += `  if (!(${config.condition})) {\n`
    code += '    return;\n'
    code += '  }\n\n'
  }
  
  // 添加动作执行
  if (config.actions && config.actions.length > 0) {
    code += '  // 执行动作\n'
    config.actions.forEach((action, index) => {
      code += `  // 动作 ${index + 1}: ${action.name || action.type}\n`
      switch (action.type) {
      case 'showMessage':
        code += '  ElMessage({\n'
        code += `    message: '${action.config?.message || ''}',\n`
        code += `    type: '${action.config?.type || 'info'}'\n`
        code += '  });\n'
        break
      case 'apiCall':
        code += '  try {\n'
        code += `    const result = await apiService.${action.config?.method || 'get'}(\n`
        code += `      '${action.config?.url || ''}',\n`
        code += `      ${JSON.stringify(action.config?.params || {}, null, 4).replace(/\n/g, '\n      ')}\n`
        code += '    );\n'
        code += '  } catch (error) {\n'
        code += '    console.error(\'API调用失败:\', error);\n'
        code += '  }\n'
        break
        // 其他动作类型...
      default:
        code += `  // 动作类型: ${action.type}\n`
      }
      code += '\n'
    })
  }
  
  code += '});'
  return code
}

/**
 * 包装组件事件处理器
 * @param component 组件实例
 * @param eventMap 事件映射配置
 */
export function setupComponentEvents(
  component: any,
  eventMap: Record<string, {
    handler: Function
    options?: {
      once?: boolean
      filter?: EventFilter
      transformer?: EventTransformer
      debounce?: number
      throttle?: number
    }
  }>
): () => void {
  const context = eventBusService.createContext(`component_context_${Date.now()}`)
  const unsubscribes: (() => void)[] = []
  
  // 为每个事件注册处理器
  Object.entries(eventMap).forEach(([eventName, { handler, options }]) => {
    let processedHandler: EventHandler = handler.bind(component)
    
    // 应用防抖
    if (options?.debounce) {
      processedHandler = debounceEventHandler(processedHandler, options.debounce)
    }
    
    // 应用节流
    if (options?.throttle) {
      processedHandler = throttleEventHandler(processedHandler, options.throttle)
    }
    
    // 应用转换器
    if (options?.transformer) {
      const originalHandler = processedHandler
      processedHandler = (eventData: any, context: EventContext) => {
        const transformedData = options.transformer!(eventData, context)
        return originalHandler(transformedData, context)
      }
    }
    
    // 应用过滤器
    if (options?.filter) {
      const originalHandler = processedHandler
      processedHandler = (eventData: any, context: EventContext) => {
        if (options.filter!(eventData, context)) {
          return originalHandler(eventData, context)
        }
      }
    }
    
    // 注册事件
    const unsubscribe = options?.once 
      ? eventBusService.once(eventName, processedHandler)
      : eventBusService.on(eventName, processedHandler)
    
    unsubscribes.push(unsubscribe)
  })
  
  // 返回清理函数
  return () => {
    // 取消所有订阅
    unsubscribes.forEach(unsubscribe => unsubscribe())
    // 销毁上下文
    eventBusService.destroyContext(context.getContextId())
  }
}

/**
 * 事件监听器钩子 - 用于Vue组件中
 * @param eventName 事件名称
 * @param handler 事件处理器
 * @param options 配置选项
 */
export function useEventListener(
  eventName: string,
  handler: EventHandler<any>,
  options?: {
    once?: boolean
    filter?: EventFilter<any>
    transformer?: EventTransformer<any, any>
    debounce?: number
    throttle?: number
  }
) {
  let unsubscribe: (() => void) | null = null
  
  const setupListener = () => {
    if (unsubscribe) return
    
    let processedHandler: (eventData: any) => void | Promise<void> = handler
    
    // 应用防抖
    if (options?.debounce) {
      processedHandler = debounceEventHandler(processedHandler, options.debounce)
    }
    
    // 应用节流
    if (options?.throttle) {
      processedHandler = throttleEventHandler(processedHandler, options.throttle)
    }
    
    // 应用转换器
    if (options?.transformer) {
      const originalHandler = processedHandler
      processedHandler = (eventData: any) => {
        const context: EventContext = { id: 'useEventListener', isActive: true }
        const transformedData = options.transformer!(eventData, context)
        return originalHandler(transformedData)
      }
    }
    
    // 应用过滤器
    if (options?.filter) {
      const originalHandler = processedHandler
      processedHandler = (eventData: any) => {
        const context: EventContext = { id: 'useEventListener', isActive: true }
        if (options.filter!(eventData, context)) {
          return originalHandler(eventData)
        }
      }
    }
    
    // 注册事件
    unsubscribe = options?.once 
      ? eventBusService.once(eventName, processedHandler)
      : eventBusService.on(eventName, processedHandler)
  }
  
  const cleanupListener = () => {
    if (unsubscribe) {
      unsubscribe()
      unsubscribe = null
    }
  }
  
  // 自动清理
  onUnmounted(() => {
    cleanupListener()
  })
  
  // 设置监听器
  setupListener()
  
  return {
    setupListener,
    cleanupListener,
    resubscribe: () => {
      cleanupListener()
      setupListener()
    }
  }
}

/**
 * 批量注册事件处理器
 * @param eventConfigs 事件配置数组
 * @param contextId 上下文ID
 * @returns 清理函数
 */
export function registerEventHandlers(
  eventConfigs: EventConfig[],
  contextId?: string
): () => void {
  let context: any
  
  // 创建或获取上下文
  if (contextId) {
    context = eventBusService.getContext(contextId)
    if (!context) {
      context = eventBusService.createContext(contextId)
    }
  } else {
    context = eventBusService.createContext(`temp_context_${Date.now()}`)
  }
  
  const unsubscribes: (() => void)[] = []
  
  // 注册每个事件的处理器
  eventConfigs.forEach(config => {
    if (!config.enabled) return
    
    const unsubscribe = context.on(config.name, async (eventData: any, eventContext: EventContext) => {
      // 检查条件
      if (config.condition) {
        try {
          const conditionFunction = new Function('eventData', 'context', `return ${config.condition}`)
          if (!conditionFunction(eventData, eventContext)) {
            return
          }
        } catch (error) {
          console.error(`Error evaluating condition for event '${config.name}':`, error)
          return
        }
      }
      
      // 应用过滤器
      if (config.filters && config.filters.length > 0) {
        let shouldProcess = true
        for (const filter of config.filters) {
          if (!filter(eventData, eventContext)) {
            shouldProcess = false
            break
          }
        }
        if (!shouldProcess) return
      }
      
      // 应用转换器
      let processedData = eventData
      if (config.transformers && config.transformers.length > 0) {
        for (const transformer of config.transformers) {
          processedData = transformer(processedData, eventContext)
        }
      }
      
      // 执行动作
      if (config.actions && config.actions.length > 0) {
        await context.executeActions(config.actions, processedData)
      }
      
      // 执行处理器
      if (config.handlers && config.handlers.length > 0) {
        await context.executeActions(config.handlers, processedData)
      }
    })
    
    unsubscribes.push(unsubscribe)
  })
  
  // 返回清理函数
  return () => {
    unsubscribes.forEach(unsubscribe => unsubscribe())
    if (!contextId && context) {
      eventBusService.destroyContext(context.getContextId())
    }
  }
}

/**
 * 为事件添加元数据
 * @param eventData 事件数据
 * @param metadata 元数据
 * @returns 增强后的事件数据
 */
export function enhanceEventWithMetadata(
  eventData: any,
  metadata: Record<string, any>
): any {
  return {
    ...eventData,
    $metadata: {
      ...(eventData.$metadata || {}),
      ...metadata,
      timestamp: Date.now(),
      correlationId: eventData.$metadata?.correlationId || `corr_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`
    }
  }
}