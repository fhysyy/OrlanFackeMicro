import { performanceService } from '@/services/performanceService'

/**
 * 防抖函数选项
 */
export interface DebounceOptions {
  /**
   * 是否立即执行第一次调用
   */
  leading?: boolean
  /**
   * 是否在延迟结束后执行
   */
  trailing?: boolean
  /**
   * 最大等待时间，如果触发频率过高，将在最大等待时间后强制执行
   */
  maxWait?: number
  /**
   * 上下文对象，默认为undefined
   */
  context?: any
  /**
   * 函数调用标识符，用于性能监控
   */
  id?: string
  /**
   * 是否启用性能监控
   */
  monitor?: boolean
}

/**
 * 防抖函数
 * @param func 需要防抖的函数
 * @param wait 等待时间（毫秒）
 * @param options 防抖选项
 * @returns 防抖后的函数
 */
export function debounce<T extends (...args: any[]) => any>(
  func: T,
  wait: number,
  options: DebounceOptions = {}
): (...args: Parameters<T>) => void {
  const { 
    leading = false,
    trailing = true,
    maxWait,
    context = undefined,
    id = `debounce_${Date.now()}`,
    monitor = false
  } = options

  let lastArgs: Parameters<T> | null = null
  let lastThis: any = null
  let result: any
  let lastCallTime: number | null = null
  let lastInvokeTime = 0
  let timerId: ReturnType<typeof setTimeout> | null = null

  // 清除定时器
  const clearTimer = () => {
    if (timerId !== null) {
      clearTimeout(timerId)
      timerId = null
    }
  }

  // 重置状态
  const reset = () => {
    lastArgs = null
    lastThis = null
    lastCallTime = null
    clearTimer()
  }

  // 执行函数
  const invokeFunc = (time: number) => {
    const args = lastArgs
    const thisArg = lastThis

    lastArgs = null
    lastThis = null
    lastInvokeTime = time
    
    // 记录性能
    if (monitor) {
      const startTime = performance.now()
      result = func.apply(thisArg, args!)
      const endTime = performance.now()
      
      performanceService.recordMetric({
        id,
        name: 'Debounced Function Execution Time',
        value: endTime - startTime,
        unit: 'ms',
        metadata: {
          functionName: func.name || 'anonymous',
          wait,
          leading,
          trailing,
          maxWait
        }
      })
    } else {
      result = func.apply(thisArg, args!)
    }
    
    return result
  }

  // 计算剩余等待时间
  const remainingWait = (time: number) => {
    if (lastCallTime === null) return 0
    const timeSinceLastCall = time - lastCallTime
    const timeSinceLastInvoke = time - lastInvokeTime
    const timeWaiting = wait - timeSinceLastCall

    return maxWait !== undefined
      ? Math.min(timeWaiting, maxWait - timeSinceLastInvoke)
      : timeWaiting
  }

  // 检查是否应该执行
  const shouldInvoke = (time: number) => {
    if (lastCallTime === null) return true
    
    const timeSinceLastCall = time - lastCallTime
    const timeSinceLastInvoke = time - lastInvokeTime

    // 超出等待时间或最大等待时间
    return (
      timeSinceLastCall >= wait ||
      timeSinceLastCall < 0 ||
      (maxWait !== undefined && timeSinceLastInvoke >= maxWait)
    )
  }

  // 定时器回调
  const timerExpired = () => {
    const time = Date.now()
    if (shouldInvoke(time)) {
      return trailingEdge(time)
    }
    // 重置定时器
    timerId = setTimeout(timerExpired, remainingWait(time))
  }

  // 前导执行
  const leadingEdge = (time: number) => {
    lastInvokeTime = time
    timerId = setTimeout(timerExpired, wait)
    return leading ? invokeFunc(time) : result
  }

  // 尾随执行
  const trailingEdge = (time: number) => {
    timerId = null

    // 只有在有参数且启用了尾随执行时才调用
    if (trailing && lastArgs) {
      return invokeFunc(time)
    }
    reset()
    return result
  }

  // 取消函数
  const cancel = () => {
    clearTimer()
    reset()
    
    if (monitor) {
      performanceService.recordMetric({
        id: `${id}_cancel`,
        name: 'Debounced Function Cancelled',
        value: Date.now(),
        unit: 'timestamp'
      })
    }
  }

  // 立即执行函数
  const flush = () => {
    if (timerId !== null) {
      const time = Date.now()
      invokeFunc(time)
      reset()
      
      if (monitor) {
        performanceService.recordMetric({
          id: `${id}_flush`,
          name: 'Debounced Function Flushed',
          value: Date.now(),
          unit: 'timestamp'
        })
      }
    }
    return result
  }

  // 判断是否处于等待状态
  const pending = () => timerId !== null

  // 主函数
  const debounced = function(this: any, ...args: Parameters<T>) {
    const time = Date.now()
    const isInvoking = shouldInvoke(time)

    lastArgs = args
    lastThis = this
    lastCallTime = time

    if (isInvoking) {
      if (timerId === null) {
        return leadingEdge(lastCallTime)
      }
      if (maxWait !== undefined) {
        // 最大等待时间逻辑
        clearTimer()
        timerId = setTimeout(timerExpired, wait)
        return invokeFunc(lastCallTime)
      }
    }
    if (timerId === null) {
      timerId = setTimeout(timerExpired, wait)
    }
    
    if (monitor) {
      performanceService.recordMetric({
        id: `${id}_call`,
        name: 'Debounced Function Called',
        value: Date.now(),
        unit: 'timestamp',
        metadata: {
          invoking: isInvoking,
          pending: pending()
        }
      })
    }
    
    return result
  }

  // 添加控制方法
  debounced.cancel = cancel
  debounced.flush = flush
  debounced.pending = pending

  return debounced
}

/**
 * 节流函数选项
 */
export interface ThrottleOptions {
  /**
   * 是否立即执行第一次调用
   */
  leading?: boolean
  /**
   * 是否在延迟结束后执行
   */
  trailing?: boolean
  /**
   * 上下文对象，默认为undefined
   */
  context?: any
  /**
   * 函数调用标识符，用于性能监控
   */
  id?: string
  /**
   * 是否启用性能监控
   */
  monitor?: boolean
}

/**
 * 节流函数
 * @param func 需要节流的函数
 * @param limit 时间限制（毫秒）
 * @param options 节流选项
 * @returns 节流后的函数
 */
export function throttle<T extends (...args: any[]) => any>(
  func: T,
  limit: number,
  options: ThrottleOptions = {}
): (...args: Parameters<T>) => void {
  const { 
    leading = true,
    trailing = true,
    context = undefined,
    id = `throttle_${Date.now()}`,
    monitor = false
  } = options

  let inThrottle = false
  let lastArgs: Parameters<T> | null = null
  let lastThis: any = null
  let result: any
  let lastInvokeTime = 0
  let timerId: ReturnType<typeof setTimeout> | null = null

  // 清除定时器
  const clearTimer = () => {
    if (timerId !== null) {
      clearTimeout(timerId)
      timerId = null
    }
  }

  // 重置状态
  const reset = () => {
    inThrottle = false
    lastArgs = null
    lastThis = null
  }

  // 执行函数
  const invokeFunc = (time: number) => {
    const args = lastArgs
    const thisArg = lastThis

    lastArgs = null
    lastThis = null
    lastInvokeTime = time
    inThrottle = true
    
    // 记录性能
    if (monitor) {
      const startTime = performance.now()
      result = func.apply(thisArg, args!)
      const endTime = performance.now()
      
      performanceService.recordMetric({
        id,
        name: 'Throttled Function Execution Time',
        value: endTime - startTime,
        unit: 'ms',
        metadata: {
          functionName: func.name || 'anonymous',
          limit,
          leading,
          trailing
        }
      })
    } else {
      result = func.apply(thisArg, args!)
    }
    
    return result
  }

  // 定时器回调
  const timerExpired = () => {
    const time = Date.now()
    if (lastArgs && trailing) {
      invokeFunc(time)
    } else {
      reset()
    }
  }

  // 启动定时器
  const startTimer = (pendingFunc: () => void, wait: number) => {
    clearTimer()
    timerId = setTimeout(pendingFunc, wait)
  }

  // 前导执行
  const leadingEdge = (time: number) => {
    lastInvokeTime = time
    startTimer(timerExpired, limit)
    return leading ? invokeFunc(time) : result
  }

  // 计算剩余时间
  const remainingWait = (time: number) => {
    const timeSinceLastInvoke = time - lastInvokeTime
    const timeWaiting = limit - timeSinceLastInvoke
    return timeWaiting
  }

  // 取消函数
  const cancel = () => {
    clearTimer()
    inThrottle = false
    lastArgs = null
    lastThis = null
    
    if (monitor) {
      performanceService.recordMetric({
        id: `${id}_cancel`,
        name: 'Throttled Function Cancelled',
        value: Date.now(),
        unit: 'timestamp'
      })
    }
  }

  // 立即执行函数
  const flush = () => {
    const time = Date.now()
    if (inThrottle || (lastArgs && trailing)) {
      invokeFunc(time)
      reset()
      startTimer(timerExpired, limit)
      
      if (monitor) {
        performanceService.recordMetric({
          id: `${id}_flush`,
          name: 'Throttled Function Flushed',
          value: Date.now(),
          unit: 'timestamp'
        })
      }
    }
    return result
  }

  // 判断是否处于节流状态
  const pending = () => inThrottle || !!timerId

  // 主函数
  const throttled = function(this: any, ...args: Parameters<T>) {
    const time = Date.now()
    const isFirstCall = lastInvokeTime === 0
    const remaining = remainingWait(time)

    lastArgs = args
    lastThis = this

    // 首次调用或超出时间限制
    if (isFirstCall || remaining <= 0 || remaining > limit) {
      // 清除定时器
      clearTimer()
      
      // 执行函数
      return leadingEdge(time)
    }
    
    // 如果没有定时器且需要尾随执行
    if (!timerId && trailing) {
      startTimer(timerExpired, remaining)
    }
    
    if (monitor) {
      performanceService.recordMetric({
        id: `${id}_call`,
        name: 'Throttled Function Called',
        value: Date.now(),
        unit: 'timestamp',
        metadata: {
          inThrottle,
          remaining
        }
      })
    }
    
    return result
  }

  // 添加控制方法
  throttled.cancel = cancel
  throttled.flush = flush
  throttled.pending = pending

  return throttled
}

/**
 * 立即执行的防抖函数
 * @param func 需要防抖的函数
 * @param wait 等待时间（毫秒）
 * @param options 防抖选项
 * @returns 防抖后的函数
 */
export function immediateDebounce<T extends (...args: any[]) => any>(
  func: T,
  wait: number,
  options: Omit<DebounceOptions, 'leading'> = {}
): (...args: Parameters<T>) => void {
  return debounce(func, wait, { ...options, leading: true, trailing: false })
}

/**
 * 尾随执行的防抖函数
 * @param func 需要防抖的函数
 * @param wait 等待时间（毫秒）
 * @param options 防抖选项
 * @returns 防抖后的函数
 */
export function trailingDebounce<T extends (...args: any[]) => any>(
  func: T,
  wait: number,
  options: Omit<DebounceOptions, 'trailing'> = {}
): (...args: Parameters<T>) => void {
  return debounce(func, wait, { ...options, leading: false, trailing: true })
}

/**
 * 生成带记忆功能的防抖函数
 * @param func 需要防抖的函数
 * @param wait 等待时间（毫秒）
 * @param options 防抖选项
 * @returns 带记忆功能的防抖函数
 */
export function memoizedDebounce<T extends (...args: any[]) => any>(
  func: T,
  wait: number,
  options: DebounceOptions = {}
): ((...args: Parameters<T>) => void) & {
  cache: Map<string, ReturnType<typeof debounce>>
  clearCache: () => void
} {
  const cache = new Map<string, ReturnType<typeof debounce>>()
  
  // 生成缓存键
  const getCacheKey = (...args: Parameters<T>): string => {
    try {
      // 使用JSON序列化参数作为缓存键
      return JSON.stringify(args)
    } catch (error) {
      // 如果参数无法序列化，使用简单的计数
      return `args_${cache.size}`
    }
  }
  
  // 清除缓存
  const clearCache = () => {
    // 取消所有待执行的函数
    cache.forEach(debouncedFn => {
      if (typeof debouncedFn.cancel === 'function') {
        debouncedFn.cancel()
      }
    })
    cache.clear()
  }
  
  // 主函数
  const memoized = function(this: any, ...args: Parameters<T>) {
    const key = getCacheKey(...args)
    
    // 从缓存获取或创建新的防抖函数
    if (!cache.has(key)) {
      cache.set(key, debounce(func, wait, options))
    }
    
    // 调用防抖函数
    const debouncedFn = cache.get(key)!
    return debouncedFn.apply(this, args)
  }
  
  // 添加缓存和清除方法
  memoized.cache = cache
  memoized.clearCache = clearCache
  
  return memoized
}

/**
 * 生成带记忆功能的节流函数
 * @param func 需要节流的函数
 * @param limit 时间限制（毫秒）
 * @param options 节流选项
 * @returns 带记忆功能的节流函数
 */
export function memoizedThrottle<T extends (...args: any[]) => any>(
  func: T,
  limit: number,
  options: ThrottleOptions = {}
): ((...args: Parameters<T>) => void) & {
  cache: Map<string, ReturnType<typeof throttle>>
  clearCache: () => void
} {
  const cache = new Map<string, ReturnType<typeof throttle>>()
  
  // 生成缓存键
  const getCacheKey = (...args: Parameters<T>): string => {
    try {
      // 使用JSON序列化参数作为缓存键
      return JSON.stringify(args)
    } catch (error) {
      // 如果参数无法序列化，使用简单的计数
      return `args_${cache.size}`
    }
  }
  
  // 清除缓存
  const clearCache = () => {
    // 取消所有待执行的函数
    cache.forEach(throttledFn => {
      if (typeof throttledFn.cancel === 'function') {
        throttledFn.cancel()
      }
    })
    cache.clear()
  }
  
  // 主函数
  const memoized = function(this: any, ...args: Parameters<T>) {
    const key = getCacheKey(...args)
    
    // 从缓存获取或创建新的节流函数
    if (!cache.has(key)) {
      cache.set(key, throttle(func, limit, options))
    }
    
    // 调用节流函数
    const throttledFn = cache.get(key)!
    return throttledFn.apply(this, args)
  }
  
  // 添加缓存和清除方法
  memoized.cache = cache
  memoized.clearCache = clearCache
  
  return memoized
}

/**
 * 创建防抖钩子（适用于Vue组合式API）
 * @param func 需要防抖的函数
 * @param wait 等待时间（毫秒）
 * @param options 防抖选项
 * @returns 防抖后的函数和控制方法
 */
export function useDebounce<T extends (...args: any[]) => any>(
  func: T,
  wait: number,
  options: DebounceOptions = {}
) {
  const debouncedFunc = debounce(func, wait, options)
  
  return {
    debouncedFn: debouncedFunc,
    cancel: debouncedFunc.cancel,
    flush: debouncedFunc.flush,
    pending: debouncedFunc.pending
  }
}

/**
 * 创建节流钩子（适用于Vue组合式API）
 * @param func 需要节流的函数
 * @param limit 时间限制（毫秒）
 * @param options 节流选项
 * @returns 节流后的函数和控制方法
 */
export function useThrottle<T extends (...args: any[]) => any>(
  func: T,
  limit: number,
  options: ThrottleOptions = {}
) {
  const throttledFunc = throttle(func, limit, options)
  
  return {
    throttledFn: throttledFunc,
    cancel: throttledFunc.cancel,
    flush: throttledFunc.flush,
    pending: throttledFunc.pending
  }
}

/**
 * 批量执行函数，将多次调用合并为一次
 * @param func 需要批量执行的函数
 * @param wait 等待时间（毫秒）
 * @param options 选项
 * @returns 批量执行函数
 */
export function batchExecute<T extends (...args: any[]) => any>(
  func: T,
  wait: number,
  options: {
    context?: any
    id?: string
    monitor?: boolean
  } = {}
): ((...args: Parameters<T>) => void) & {
  flush: () => void
  cancel: () => void
} {
  const { 
    context = undefined,
    id = `batch_${Date.now()}`,
    monitor = false
  } = options

  let calls: Parameters<T>[] = []
  let timerId: ReturnType<typeof setTimeout> | null = null

  // 执行批量调用
  const executeBatch = () => {
    if (calls.length === 0) return
    
    const callsToExecute = [...calls]
    calls = []
    
    // 记录性能
    if (monitor) {
      const startTime = performance.now()
      func.apply(context, [callsToExecute])
      const endTime = performance.now()
      
      performanceService.recordMetric({
        id,
        name: 'Batch Function Execution Time',
        value: endTime - startTime,
        unit: 'ms',
        metadata: {
          batchSize: callsToExecute.length,
          functionName: func.name || 'anonymous'
        }
      })
    } else {
      func.apply(context, [callsToExecute])
    }
  }

  // 取消
  const cancel = () => {
    clearTimeout(timerId!)
    timerId = null
    calls = []
  }

  // 立即执行
  const flush = () => {
    if (timerId !== null) {
      clearTimeout(timerId)
      timerId = null
      executeBatch()
    }
  }

  // 主函数
  const batched = function(this: any, ...args: Parameters<T>) {
    calls.push(args)
    
    if (timerId === null) {
      timerId = setTimeout(executeBatch, wait)
    }
    
    if (monitor) {
      performanceService.recordMetric({
        id: `${id}_call`,
        name: 'Batch Function Called',
        value: Date.now(),
        unit: 'timestamp',
        metadata: {
          callCount: calls.length
        }
      })
    }
  }

  // 添加控制方法
  batched.flush = flush
  batched.cancel = cancel

  return batched
}

/**
 * 延迟执行函数，支持取消和重试
 * @param func 需要延迟执行的函数
 * @param delay 延迟时间（毫秒）
 * @param options 选项
 * @returns 控制对象
 */
export function delayExecute<T extends (...args: any[]) => any>(
  func: T,
  delay: number,
  options: {
    context?: any
    args?: Parameters<T>
    id?: string
    monitor?: boolean
  } = {}
): {
  start: () => Promise<ReturnType<T>>
  cancel: () => void
  isPending: () => boolean
  restart: () => Promise<ReturnType<T>>
} {
  const { 
    context = undefined,
    args = [],
    id = `delay_${Date.now()}`,
    monitor = false
  } = options

  let timerId: ReturnType<typeof setTimeout> | null = null
  let isExecuting = false

  // 清除定时器
  const clearTimer = () => {
    if (timerId !== null) {
      clearTimeout(timerId)
      timerId = null
    }
  }

  // 执行函数
  const execute = async (): Promise<ReturnType<T>> => {
    clearTimer()
    isExecuting = true
    
    try {
      // 记录性能
      if (monitor) {
        const startTime = performance.now()
        const result = await func.apply(context, args)
        const endTime = performance.now()
        
        performanceService.recordMetric({
          id,
          name: 'Delayed Function Execution Time',
          value: endTime - startTime,
          unit: 'ms',
          metadata: {
            delay,
            functionName: func.name || 'anonymous'
          }
        })
        
        return result
      } else {
        return await func.apply(context, args)
      }
    } finally {
      isExecuting = false
    }
  }

  // 开始延迟执行
  const start = (): Promise<ReturnType<T>> => {
    return new Promise((resolve, reject) => {
      clearTimer()
      
      timerId = setTimeout(async () => {
        try {
          const result = await execute()
          resolve(result)
        } catch (error) {
          reject(error)
        }
      }, delay)
    })
  }

  // 取消执行
  const cancel = () => {
    clearTimer()
    
    if (monitor) {
      performanceService.recordMetric({
        id: `${id}_cancel`,
        name: 'Delayed Function Cancelled',
        value: Date.now(),
        unit: 'timestamp'
      })
    }
  }

  // 检查是否处于等待状态
  const isPending = () => timerId !== null || isExecuting

  // 重启执行
  const restart = (): Promise<ReturnType<T>> => {
    cancel()
    return start()
  }

  return {
    start,
    cancel,
    isPending,
    restart
  }
}

/**
 * 创建一个请求动画帧的节流函数
 * @param func 需要执行的函数
 * @returns 节流后的函数
 */
export function rafThrottle<T extends (...args: any[]) => any>(
  func: T
): (...args: Parameters<T>) => void {
  let rafId: number | null = null
  let lastArgs: Parameters<T> | null = null
  let lastThis: any = null

  // 执行函数
  const execute = () => {
    if (lastArgs === null) return
    
    func.apply(lastThis, lastArgs)
    lastArgs = null
    lastThis = null
    rafId = null
  }

  // 主函数
  const throttled = function(this: any, ...args: Parameters<T>) {
    lastArgs = args
    lastThis = this
    
    if (rafId === null) {
      rafId = requestAnimationFrame(execute)
    }
  }

  // 取消函数
  throttled.cancel = () => {
    if (rafId !== null) {
      cancelAnimationFrame(rafId)
      rafId = null
      lastArgs = null
      lastThis = null
    }
  }

  return throttled
}

/**
 * 防抖与节流组合函数 - 先防抖后节流
 * @param func 需要处理的函数
 * @param debounceWait 防抖等待时间（毫秒）
 * @param throttleLimit 节流限制时间（毫秒）
 * @returns 处理后的函数
 */
export function debounceThenThrottle<T extends (...args: any[]) => any>(
  func: T,
  debounceWait: number,
  throttleLimit: number
): (...args: Parameters<T>) => void {
  // 先创建节流函数
  const throttledFunc = throttle(func, throttleLimit)
  // 再对节流函数进行防抖
  return debounce(throttledFunc, debounceWait)
}

/**
 * 动态调整防抖时间的函数
 * @param func 需要防抖的函数
 * @param baseWait 基础等待时间（毫秒）
 * @param maxWait 最大等待时间（毫秒）
 * @param decayRate 衰减率（0-1之间，越小衰减越快）
 * @returns 动态防抖函数
 */
export function dynamicDebounce<T extends (...args: any[]) => any>(
  func: T,
  baseWait: number,
  maxWait: number = baseWait * 3,
  decayRate: number = 0.8
): (...args: Parameters<T>) => void {
  let callCount = 0
  let lastCallTime = 0
  let debouncedFunc: ReturnType<typeof debounce> | null = null

  // 计算当前等待时间
  const calculateWait = () => {
    const now = Date.now()
    const timeSinceLastCall = now - lastCallTime
    lastCallTime = now
    
    // 如果距离上次调用时间较长，重置调用计数
    if (timeSinceLastCall > maxWait * 2) {
      callCount = 0
    }
    
    // 增加调用计数
    callCount++
    
    // 计算指数增长的等待时间，但不超过最大等待时间
    const wait = Math.min(baseWait * Math.pow(1 / decayRate, callCount - 1), maxWait)
    return wait
  }

  // 主函数
  return function(this: any, ...args: Parameters<T>) {
    const currentWait = calculateWait()
    
    // 重新创建防抖函数
    if (debouncedFunc) {
      debouncedFunc.cancel()
    }
    
    debouncedFunc = debounce(func, currentWait)
    debouncedFunc.apply(this, args)
  }
}