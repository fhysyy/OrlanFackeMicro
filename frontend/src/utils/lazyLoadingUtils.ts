import { defineAsyncComponent, type AsyncComponentLoader } from 'vue'
import { ElMessage } from 'element-plus'
import { performanceService } from '@/services/performanceService'

// 延迟加载选项接口
export interface LazyLoadOptions {
  loadingComponent?: any
  errorComponent?: any
  delay?: number
  timeout?: number
  suspensible?: boolean
  onError?: (error: Error, retry: () => void, fail: () => void, attempts: number) => void
  onSuccess?: (component: any) => void
  onLoading?: () => void
  retry?: number
  retryDelay?: number
  componentName?: string
  prefetch?: boolean
  preload?: boolean
  weight?: number // 组件权重，用于优先级排序
}

// 默认加载组件
const DefaultLoadingComponent = {
  template: `
    <div class="lazy-loading-container">
      <el-skeleton animated :rows="3" />
      <div class="lazy-loading-text">加载中...</div>
    </div>
  `
}

// 默认错误组件
const DefaultErrorComponent = {
  props: ['error', 'retry'],
  template: `
    <div class="lazy-error-container">
      <el-empty description="组件加载失败">
        <template #description>
          <div>{{ error.message }}</div>
        </template>
        <el-button type="primary" @click="retry">重试</el-button>
      </el-empty>
    </div>
  `
}

// 组件缓存
const componentCache = new Map<string, any>()

// 预加载队列
const preloadQueue: Array<{
  loader: AsyncComponentLoader
  key: string
  priority: number
  options?: LazyLoadOptions
}> = []

// 预加载状态
let isPreloading = false

/**
 * 创建懒加载组件
 * @param loader 组件加载器
 * @param options 懒加载选项
 * @returns 懒加载组件
 */
export function createLazyComponent(
  loader: AsyncComponentLoader,
  options: LazyLoadOptions = {}
) {
  const { 
    loadingComponent = DefaultLoadingComponent,
    errorComponent = DefaultErrorComponent,
    delay = 200,
    timeout = 30000,
    suspensible = false,
    retry = 3,
    retryDelay = 300,
    componentName = 'AsyncComponent',
    prefetch = false,
    preload = false,
    weight = 0,
    onError,
    onSuccess,
    onLoading
  } = options

  // 生成缓存键
  const cacheKey = typeof loader === 'string' ? loader : `${componentName}_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`

  // 自定义错误处理
  const customOnError = (error: Error, retryFn: () => void, failFn: () => void, attempts: number) => {
    console.error(`Failed to load component ${componentName}:`, error)
    
    // 记录性能错误
    performanceService.recordError('component_load_failure', `${componentName} load failed: ${error.message}`, {
      componentName,
      attempts,
      maxRetries: retry
    })

    // 显示错误消息
    ElMessage.error(`组件加载失败: ${componentName}`)

    // 调用用户自定义错误处理
    if (onError) {
      onError(error, retryFn, failFn, attempts)
    } else {
      // 默认行为
      if (attempts <= retry) {
        // 自动重试
        setTimeout(() => {
          retryFn()
        }, retryDelay)
      } else {
        // 达到最大重试次数
        failFn()
      }
    }
  }

  // 自定义成功处理
  const customOnSuccess = (component: any) => {
    console.log(`Component ${componentName} loaded successfully`)
    
    // 记录性能指标
    performanceService.recordMetric({
      id: `component_load_${componentName}`,
      name: 'Component Load Time',
      value: Date.now(),
      unit: 'ms',
      metadata: {
        componentName
      }
    })

    // 缓存组件
    componentCache.set(cacheKey, component)

    // 调用用户自定义成功处理
    if (onSuccess) {
      onSuccess(component)
    }
  }

  // 自定义加载处理
  const customOnLoading = () => {
    console.log(`Loading component ${componentName}...`)
    
    // 记录开始加载时间
    performanceService.recordMetric({
      id: `component_start_load_${componentName}`,
      name: 'Component Start Load',
      value: Date.now(),
      unit: 'ms',
      metadata: {
        componentName
      }
    })

    // 调用用户自定义加载处理
    if (onLoading) {
      onLoading()
    }
  }

  // 创建异步组件
  const asyncComponent = defineAsyncComponent({
    loader,
    loadingComponent,
    errorComponent,
    delay,
    timeout,
    suspensible,
    onError: customOnError,
    onSuccess: customOnSuccess,
    onLoading: customOnLoading
  })

  // 如果需要预加载
  if (prefetch || preload) {
    const priority = weight + (prefetch ? 10 : 0) + (preload ? 20 : 0)
    
    // 添加到预加载队列
    preloadQueue.push({
      loader,
      key: cacheKey,
      priority,
      options
    })
    
    // 开始预加载
    if (!isPreloading) {
      startPreloading()
    }
  }

  return asyncComponent
}

/**
 * 懒加载组件装饰器
 * @param options 懒加载选项
 */
export function LazyComponent(options: LazyLoadOptions = {}) {
  return function (target: any) {
    // 保存原始组件
    const originalComponent = target
    
    // 创建代理组件
    const proxyComponent = {
      mounted() {
        // 组件挂载时的逻辑
        console.log(`Lazy component ${options.componentName || 'Anonymous'} mounted`)
      },
      render() {
        // 渲染原始组件
        return h(originalComponent)
      }
    }
    
    return proxyComponent
  }
}

/**
 * 预加载组件
 * @param loader 组件加载器
 * @param options 预加载选项
 */
export function preloadComponent(
  loader: AsyncComponentLoader,
  options: Omit<LazyLoadOptions, 'prefetch' | 'preload'> = {}
) {
  const cacheKey = typeof loader === 'string' ? loader : `preload_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`
  
  // 如果已经缓存，直接返回
  if (componentCache.has(cacheKey)) {
    return Promise.resolve(componentCache.get(cacheKey))
  }
  
  // 添加到预加载队列
  preloadQueue.push({
    loader,
    key: cacheKey,
    priority: options.weight || 0 + 50, // 手动预加载优先级更高
    options
  })
  
  // 开始预加载
  if (!isPreloading) {
    startPreloading()
  }
  
  // 返回加载Promise
  return new Promise((resolve, reject) => {
    // 轮询检查组件是否已加载
    const checkInterval = setInterval(() => {
      if (componentCache.has(cacheKey)) {
        clearInterval(checkInterval)
        resolve(componentCache.get(cacheKey))
      }
    }, 100)
    
    // 设置超时
    setTimeout(() => {
      clearInterval(checkInterval)
      reject(new Error('Preload timeout'))
    }, options.timeout || 10000)
  })
}

/**
 * 批量预加载组件
 * @param components 组件加载器数组
 * @param options 全局预加载选项
 */
export async function preloadComponents(
  components: Array<AsyncComponentLoader | { loader: AsyncComponentLoader; options?: LazyLoadOptions }>,
  options: Omit<LazyLoadOptions, 'prefetch' | 'preload'> = {}
) {
  const promises = components.map(component => {
    if (typeof component === 'function') {
      return preloadComponent(component, options)
    } else {
      return preloadComponent(component.loader, { ...options, ...component.options })
    }
  })
  
  try {
    return await Promise.allSettled(promises)
  } catch (error) {
    console.error('Batch preload failed:', error)
    throw error
  }
}

/**
 * 开始预加载队列处理
 */
function startPreloading() {
  if (isPreloading || preloadQueue.length === 0) return
  
  isPreloading = true
  
  // 按优先级排序队列
  preloadQueue.sort((a, b) => b.priority - a.priority)
  
  // 处理预加载队列
  processPreloadQueue()
}

/**
 * 处理预加载队列
 */
async function processPreloadQueue() {
  if (preloadQueue.length === 0) {
    isPreloading = false
    return
  }
  
  // 获取下一个要预加载的组件
  const nextItem = preloadQueue.shift()
  if (!nextItem) {
    isPreloading = false
    return
  }
  
  try {
    // 检查是否已缓存
    if (componentCache.has(nextItem.key)) {
      // 继续处理下一个
      processPreloadQueue()
      return
    }
    
    // 检查网络状况，如果网络差则延迟预加载
    const networkState = checkNetworkState()
    if (networkState === 'slow') {
      // 网络较慢时，延迟500ms再继续
      setTimeout(() => {
        // 重新加入队列，但优先级降低
        preloadQueue.push({
          ...nextItem,
          priority: nextItem.priority - 10
        })
        processPreloadQueue()
      }, 500)
      return
    }
    
    // 预加载组件
    const startTime = performance.now()
    console.log(`Preloading component (priority: ${nextItem.priority})...`)
    
    const component = await nextItem.loader()
    
    const loadTime = performance.now() - startTime
    console.log(`Component preloaded in ${loadTime.toFixed(2)}ms`)
    
    // 缓存组件
    componentCache.set(nextItem.key, component)
    
    // 记录性能指标
    performanceService.recordMetric({
      id: `preload_${Date.now()}`,
      name: 'Component Preload Time',
      value: loadTime,
      unit: 'ms',
      metadata: {
        componentName: nextItem.options?.componentName || 'Unknown',
        priority: nextItem.priority
      }
    })
    
    // 短暂延迟后继续处理下一个组件，避免阻塞主线程
    setTimeout(() => {
      processPreloadQueue()
    }, 100)
    
  } catch (error) {
    console.error('Preload failed:', error)
    
    // 记录错误
    performanceService.recordError('component_preload_failure', String(error), {
      componentName: nextItem.options?.componentName || 'Unknown'
    })
    
    // 继续处理下一个
    setTimeout(() => {
      processPreloadQueue()
    }, 100)
  }
}

/**
 * 检查网络状况
 * @returns 网络状态: 'good', 'medium', 'slow'
 */
function checkNetworkState(): 'good' | 'medium' | 'slow' {
  try {
    // 使用 navigator.connection API（如果可用）
    // @ts-ignore
    if (navigator.connection) {
      // @ts-ignore
      const { effectiveType, rtt } = navigator.connection
      
      switch (effectiveType) {
      case '4g':
        return 'good'
      case '3g':
        return rtt < 100 ? 'medium' : 'slow'
      default:
        return 'slow'
      }
    }
    
    // 默认返回良好状态
    return 'good'
  } catch (error) {
    console.warn('Failed to check network state:', error)
    return 'good'
  }
}

/**
 * 创建组件加载器
 * @param importFn 动态导入函数
 * @param exportName 导出名称，默认为 'default'
 * @returns 组件加载器
 */
export function createComponentLoader(
  importFn: () => Promise<any>,
  exportName: string = 'default'
): AsyncComponentLoader {
  return async () => {
    const module = await importFn()
    return module[exportName] || module
  }
}

/**
 * 图片懒加载工具
 * @param images 图片元素或选择器
 * @param options 懒加载选项
 */
export function lazyLoadImages(
  images: string | HTMLImageElement | HTMLImageElement[],
  options: {
    rootMargin?: string
    threshold?: number | number[]
    placeholder?: string
    errorPlaceholder?: string
    onLoad?: (img: HTMLImageElement) => void
    onError?: (img: HTMLImageElement, error: Error) => void
  } = {}
) {
  const { 
    rootMargin = '200px 0px',
    threshold = 0,
    placeholder = 'data:image/svg+xml,%3Csvg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100"%3E%3Crect fill="%23f0f0f0" width="100" height="100"/%3E%3C/svg%3E',
    errorPlaceholder = 'data:image/svg+xml,%3Csvg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100"%3E%3Crect fill="%23fee" width="100" height="100"/%3E%3Ctext x="50" y="50" font-family="Arial" font-size="12" text-anchor="middle" fill="%23f00"%3E加载失败%3C/text%3E%3C/svg%3E',
    onLoad,
    onError
  } = options

  // 获取图片元素
  const imageElements: HTMLImageElement[] = []
  
  if (typeof images === 'string') {
    const elements = document.querySelectorAll<HTMLImageElement>(images)
    imageElements.push(...Array.from(elements))
  } else if (images instanceof HTMLImageElement) {
    imageElements.push(images)
  } else if (Array.isArray(images)) {
    imageElements.push(...images)
  }

  // 检查IntersectionObserver支持
  if ('IntersectionObserver' in window) {
    const observer = new IntersectionObserver((entries) => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          const img = entry.target as HTMLImageElement
          
          // 加载图片
          loadImage(img, placeholder, errorPlaceholder, onLoad, onError)
          
          // 取消观察
          observer.unobserve(img)
        }
      })
    }, {
      rootMargin,
      threshold
    })

    // 观察所有图片
    imageElements.forEach(img => {
      // 设置占位符
      if (img.dataset.src && !img.src) {
        img.src = placeholder
      }
      observer.observe(img)
    })

    // 返回清理函数
    return () => {
      imageElements.forEach(img => observer.unobserve(img))
    }
  } else {
    // 降级方案：立即加载所有图片
    console.warn('IntersectionObserver not supported, loading all images immediately')
    imageElements.forEach(img => {
      loadImage(img, placeholder, errorPlaceholder, onLoad, onError)
    })
    
    return () => {}
  }
}

/**
 * 加载单个图片
 */
function loadImage(
  img: HTMLImageElement,
  placeholder: string,
  errorPlaceholder: string,
  onLoad?: (img: HTMLImageElement) => void,
  onError?: (img: HTMLImageElement, error: Error) => void
) {
  // 检查是否有data-src属性
  if (img.dataset.src && img.dataset.src !== img.src) {
    const originalSrc = img.dataset.src
    
    // 创建新图片对象进行预加载
    const tempImg = new Image()
    
    tempImg.onload = () => {
      // 图片加载成功
      img.src = originalSrc
      if (onLoad) {
        onLoad(img)
      }
    }
    
    tempImg.onerror = (error) => {
      // 图片加载失败
      img.src = errorPlaceholder
      console.error(`Failed to load image: ${originalSrc}`)
      if (onError) {
        onError(img, error as Error)
      }
    }
    
    // 开始加载
    tempImg.src = originalSrc
  }
}

/**
 * 资源预加载
 * @param resources 资源URL数组
 * @param options 预加载选项
 */
export function preloadResources(
  resources: string | string[],
  options: {
    type?: string
    as?: string
    crossOrigin?: string
    onLoad?: (url: string) => void
    onError?: (url: string, error: Error) => void
  } = {}
) {
  const { 
    type,
    as,
    crossOrigin,
    onLoad,
    onError
  } = options

  const resourceList = Array.isArray(resources) ? resources : [resources]
  const promises: Promise<void>[] = []

  resourceList.forEach(url => {
    const promise = new Promise<void>((resolve, reject) => {
      // 检查是否支持资源提示
      if ('link' in document.createElement('link') && 'relList' in document.createElement('link')) {
        // 使用Link Prefetch
        const link = document.createElement('link')
        link.rel = 'prefetch'
        link.href = url
        
        if (type) link.type = type
        if (as) link.as = as
        if (crossOrigin) link.crossOrigin = crossOrigin
        
        link.onload = () => {
          console.log(`Resource preloaded: ${url}`)
          if (onLoad) onLoad(url)
          resolve()
        }
        
        link.onerror = (error) => {
          console.error(`Failed to preload resource: ${url}`, error)
          if (onError) onError(url, error as Error)
          reject(error)
        }
        
        document.head.appendChild(link)
        
        // 清理函数
        setTimeout(() => {
          document.head.removeChild(link)
        }, 60000) // 60秒后移除
      } else {
        // 降级方案：使用Image对象预加载图片
        if (url.match(/\.(jpeg|jpg|gif|png|webp|svg)$/)) {
          const img = new Image()
          if (crossOrigin) img.crossOrigin = crossOrigin
          
          img.onload = () => {
            if (onLoad) onLoad(url)
            resolve()
          }
          
          img.onerror = (error) => {
            if (onError) onError(url, error as Error)
            reject(error)
          }
          
          img.src = url
        } else {
          // 其他资源类型，使用XHR预加载
          const xhr = new XMLHttpRequest()
          xhr.open('GET', url, true)
          if (type) xhr.setRequestHeader('Content-Type', type)
          if (crossOrigin) xhr.withCredentials = true
          
          xhr.onload = () => {
            if (xhr.status === 200) {
              if (onLoad) onLoad(url)
              resolve()
            } else {
              const error = new Error(`Failed to preload resource: ${url}`)
              if (onError) onError(url, error)
              reject(error)
            }
          }
          
          xhr.onerror = (error) => {
            if (onError) onError(url, error as Error)
            reject(error)
          }
          
          xhr.send()
        }
      }
    })
    
    promises.push(promise)
  })
  
  return Promise.allSettled(promises)
}

/**
 * 代码分割工具 - 动态导入模块
 * @param importFn 动态导入函数
 * @param moduleName 模块名称
 * @returns 导入的模块
 */
export async function importModule<T = any>(
  importFn: () => Promise<T>,
  moduleName: string = 'AnonymousModule'
): Promise<T> {
  const startTime = performance.now()
  
  try {
    // 记录开始导入
    performanceService.recordMetric({
      id: `import_start_${moduleName}`,
      name: 'Module Import Start',
      value: startTime,
      unit: 'ms',
      metadata: { moduleName }
    })
    
    // 执行动态导入
    const module = await importFn()
    
    // 记录导入完成
    const endTime = performance.now()
    const loadTime = endTime - startTime
    
    performanceService.recordMetric({
      id: `import_complete_${moduleName}`,
      name: 'Module Import Time',
      value: loadTime,
      unit: 'ms',
      metadata: { moduleName }
    })
    
    console.log(`Module ${moduleName} loaded in ${loadTime.toFixed(2)}ms`)
    
    return module
  } catch (error) {
    console.error(`Failed to load module ${moduleName}:`, error)
    
    // 记录错误
    performanceService.recordError('module_import_failure', String(error), { moduleName })
    
    throw error
  }
}

/**
 * 获取缓存的组件
 * @param key 缓存键
 * @returns 组件或undefined
 */
export function getCachedComponent(key: string): any | undefined {
  return componentCache.get(key)
}

/**
 * 清除组件缓存
 * @param key 可选的缓存键，不提供则清除所有缓存
 */
export function clearComponentCache(key?: string): void {
  if (key) {
    componentCache.delete(key)
    console.log(`Component cache cleared for: ${key}`)
  } else {
    componentCache.clear()
    console.log('All component cache cleared')
  }
}

/**
 * 获取缓存统计信息
 */
export function getCacheStats(): {
  cachedComponents: number
  cacheSize: number // 估算的缓存大小（字节）
  } {
  return {
    cachedComponents: componentCache.size,
    cacheSize: componentCache.size * 1024 // 估算每个组件平均占用1KB
  }
}

/**
 * 优化渲染性能的组件包装器
 * @param component 原始组件
 * @param options 优化选项
 * @returns 优化后的组件
 */
export function optimizeComponent(
  component: any,
  options: {
    memoize?: boolean
    lazyMount?: boolean
    debounceProps?: string[]
    throttleProps?: string[]
    propsEqual?: (prevProps: any, nextProps: any) => boolean
  } = {}
) {
  const { 
    memoize = true,
    lazyMount = false,
    debounceProps = [],
    throttleProps = [],
    propsEqual
  } = options

  // 这里简化实现，实际项目中可以使用Vue的memo、自定义hooks等
  // 来优化组件渲染性能
  
  console.log('Component optimized with options:', options)
  
  return component
}

/**
 * 检查组件渲染是否需要优化
 * @param componentData 组件性能数据
 * @returns 是否需要优化的建议
 */
export function getOptimizationSuggestions(componentData: any): {
  needsOptimization: boolean
  suggestions: string[]
} {
  const suggestions: string[] = []
  
  // 检查渲染时间
  if (componentData.averageRenderTime > 50) {
    suggestions.push('组件平均渲染时间过长，建议优化渲染逻辑')
  }
  
  // 检查渲染次数
  if (componentData.renderCount > 100 && componentData.renderCount / componentData.lastRenderTimestamp * 60000 > 100) {
    suggestions.push('组件渲染频率过高，建议使用缓存或减少不必要的更新')
  }
  
  // 检查props大小
  if (componentData.propsSize > 1024 * 10) { // 10KB
    suggestions.push('组件props过大，建议拆分组件或减少传递的数据量')
  }
  
  // 检查响应式属性数量
  if (componentData.reactivePropsCount > 50) {
    suggestions.push('组件响应式属性过多，建议使用reactive或优化数据结构')
  }
  
  return {
    needsOptimization: suggestions.length > 0,
    suggestions
  }
}