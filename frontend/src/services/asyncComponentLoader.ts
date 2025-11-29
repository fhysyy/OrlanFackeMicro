// 异步组件加载器，用于实现组件的延迟加载和按需加载
import { defineAsyncComponent } from 'vue'
import type { ComponentDefinition } from './componentRegistry'

// 异步组件加载配置接口
interface AsyncComponentOptions {
  loadingComponent?: any;         // 加载中占位组件
  errorComponent?: any;           // 加载错误占位组件
  delay?: number;                 // 延迟显示加载状态的时间（毫秒）
  timeout?: number;               // 加载超时时间（毫秒）
  retry?: boolean;                // 加载失败是否重试
  maxRetries?: number;            // 最大重试次数
  retryDelay?: number;            // 重试间隔（毫秒）
}

// 默认配置
const DEFAULT_OPTIONS: AsyncComponentOptions = {
  delay: 200,
  timeout: 30000,
  retry: true,
  maxRetries: 3,
  retryDelay: 1000
}

// 组件加载状态
interface ComponentLoadStatus {
  loading: boolean;
  loaded: boolean;
  error: Error | null;
  retries: number;
  component: any | null;
}

// 异步组件加载器类
class AsyncComponentLoader {
  private loadStatus: Map<string, ComponentLoadStatus> = new Map()
  private componentCache: Map<string, any> = new Map()
  private options: AsyncComponentOptions
  private loadCallbacks: Map<string, Array<(component: any, error: Error | null) => void>> = new Map()

  constructor(options: AsyncComponentOptions = {}) {
    this.options = { ...DEFAULT_OPTIONS, ...options }
  }

  // 注册异步组件（将组件定义转换为异步加载模式）
  registerAsyncComponent(componentDefinition: ComponentDefinition): ComponentDefinition {
    if (!componentDefinition.type) {
      throw new Error('Component type is required for async registration')
    }

    // 为组件添加异步加载能力
    return {
      ...componentDefinition,
      // 如果已经有异步加载函数，则使用它；否则包装原始组件
      component: componentDefinition.asyncLoader 
        ? this.createAsyncComponentWrapper(componentDefinition.type, componentDefinition.asyncLoader)
        : componentDefinition.component
    }
  }

  // 创建异步组件包装器
  private createAsyncComponentWrapper(
    componentType: string,
    loader: () => Promise<any>
  ): any {
    // 检查是否已缓存
    if (this.componentCache.has(componentType)) {
      return this.componentCache.get(componentType)
    }

    // 创建异步组件
    const asyncComponent = defineAsyncComponent({
      // 使用我们的加载器函数并添加缓存逻辑
      loader: async () => {
        return this.loadComponent(componentType, loader)
      },
      loadingComponent: this.options.loadingComponent,
      errorComponent: this.options.errorComponent,
      delay: this.options.delay,
      timeout: this.options.timeout
    })

    // 缓存结果
    this.componentCache.set(componentType, asyncComponent)
    return asyncComponent
  }

  // 加载组件并处理缓存和重试逻辑
  private async loadComponent(
    componentType: string,
    loader: () => Promise<any>
  ): Promise<any> {
    // 初始化加载状态
    if (!this.loadStatus.has(componentType)) {
      this.loadStatus.set(componentType, {
        loading: false,
        loaded: false,
        error: null,
        retries: 0,
        component: null
      })
    }

    const status = this.loadStatus.get(componentType)!

    // 如果正在加载，等待完成
    if (status.loading) {
      return this.waitForComponentLoad(componentType)
    }

    // 如果已加载成功，直接返回
    if (status.loaded && status.component) {
      return status.component
    }

    // 开始加载
    status.loading = true
    status.error = null

    try {
      const component = await loader()
      status.component = component.default || component
      status.loaded = true
      status.loading = false
      status.retries = 0

      // 通知所有等待的回调
      this.notifyLoadCallbacks(componentType, status.component, null)
      
      return status.component
    } catch (error) {
      status.error = error as Error
      status.loading = false

      // 处理重试逻辑
      if (this.options.retry && status.retries < (this.options.maxRetries || 3)) {
        status.retries++
        console.warn(`Failed to load component ${componentType}, retrying (${status.retries}/${this.options.maxRetries})...`)
        
        // 延迟重试
        await new Promise(resolve => setTimeout(resolve, this.options.retryDelay || 1000))
        return this.loadComponent(componentType, loader)
      }

      console.error(`Failed to load component ${componentType} after ${status.retries} retries:`, error)
      this.notifyLoadCallbacks(componentType, null, error as Error)
      throw error
    }
  }

  // 等待组件加载完成
  private waitForComponentLoad(componentType: string): Promise<any> {
    return new Promise((resolve, reject) => {
      if (!this.loadCallbacks.has(componentType)) {
        this.loadCallbacks.set(componentType, [])
      }

      this.loadCallbacks.get(componentType)!.push((component, error) => {
        if (error) {
          reject(error)
        } else {
          resolve(component)
        }
      })
    })
  }

  // 通知所有加载回调
  private notifyLoadCallbacks(componentType: string, component: any, error: Error | null): void {
    const callbacks = this.loadCallbacks.get(componentType) || []
    callbacks.forEach(callback => callback(component, error))
    this.loadCallbacks.delete(componentType)
  }

  // 预加载单个组件
  async preloadComponent(componentType: string, loader: () => Promise<any>): Promise<void> {
    try {
      // 确保isComponentLoaded方法存在
      if (typeof this.isComponentLoaded === 'function' && !this.isComponentLoaded(componentType)) {
        // 确保loadComponent方法存在
        if (typeof this.loadComponent === 'function') {
          await this.loadComponent(componentType, loader)
        } else {
          console.warn(`Cannot preload component ${componentType}: loadComponent method is missing`)
        }
      }
    } catch (error) {
      console.warn(`Preloading component ${componentType} failed, will try again when needed:`, error)
    }
  }

  // 预加载多个组件
  async preloadComponents(components: Array<{ type: string; loader: () => Promise<any> }>): Promise<void> {
    // 确保components是有效数组
    if (!Array.isArray(components)) {
      console.warn('preloadComponents: components parameter must be an array')
      return
    }
    
    const preloadPromises = components
      .filter(item => item && typeof item.type === 'string' && typeof item.loader === 'function')
      .map(({ type, loader }) => 
        this.preloadComponent(type, loader)
      )

    // 确保Promise.all存在
    if (typeof Promise.all === 'function') {
      await Promise.all(preloadPromises)
    } else {
      // 降级处理
      for (const promise of preloadPromises) {
        await promise
      }
    }
  }

  // 清理组件缓存
  clearCache(componentType?: string): void {
    if (componentType) {
      this.componentCache.delete(componentType)
      this.loadStatus.delete(componentType)
    } else {
      this.componentCache.clear()
      this.loadStatus.clear()
    }
  }

  // 获取组件加载状态
  getLoadStatus(componentType: string): ComponentLoadStatus | undefined {
    return this.loadStatus.get(componentType)
  }

  // 检查组件是否已加载
  isComponentLoaded(componentType: string): boolean {
    const status = this.loadStatus.get(componentType)
    return status ? status.loaded : false
  }

  // 创建动态导入的加载器函数
  createDynamicImportLoader(modulePath: string): () => Promise<any> {
    return () => import(/* @vite-ignore */ modulePath)
  }
}

// 创建单例实例
const asyncComponentLoader = new AsyncComponentLoader()

// 性能监控装饰器
function monitorComponentLoadTime(target: any, propertyKey: string, descriptor: PropertyDescriptor) {
  // 安全检查：确保descriptor.value存在
  if (!descriptor.value) {
    console.warn(`Cannot apply monitorComponentLoadTime to ${propertyKey} as it doesn't have a value descriptor`)
    return descriptor
  }
  
  const originalMethod = descriptor.value
  
  descriptor.value = async function(...args: any[]) {
    const componentType = args[0]
    const startTime = performance.now()
    
    try {
      const result = await originalMethod.apply(this, args)
      const endTime = performance.now()
      
      // 记录加载时间，超过阈值时警告
      const loadTime = endTime - startTime
      if (loadTime > 1000) {
        console.warn(`Slow component load: ${componentType} took ${loadTime.toFixed(2)}ms`)
      }
      
      return result
    } catch (error) {
      const endTime = performance.now()
      console.error(`Component load failed after ${(endTime - startTime).toFixed(2)}ms:`, error)
      throw error
    }
  }
  
  return descriptor
}

// 应用性能监控
// 使用更安全的方式来应用装饰器
const originalLoadComponent = AsyncComponentLoader.prototype.loadComponent
if (typeof originalLoadComponent === 'function') {
  AsyncComponentLoader.prototype.loadComponent = async function(...args: any[]) {
    const componentType = args[0]
    const startTime = performance.now()
    
    try {
      const result = await originalLoadComponent.apply(this, args)
      const endTime = performance.now()
      
      // 记录加载时间，超过阈值时警告
      const loadTime = endTime - startTime
      if (loadTime > 1000) {
        console.warn(`Slow component load: ${componentType} took ${loadTime.toFixed(2)}ms`)
      }
      
      return result
    } catch (error) {
      const endTime = performance.now()
      console.error(`Component load failed after ${(endTime - startTime).toFixed(2)}ms:`, error)
      throw error
    }
  }
} else {
  console.warn('Failed to apply performance monitoring to loadComponent method')
}

// 导出 - 确保每个方法都存在
const registerAsyncComponent = typeof asyncComponentLoader.registerAsyncComponent === 'function' ? asyncComponentLoader.registerAsyncComponent : () => { throw new Error('registerAsyncComponent is not available') }
const preloadComponent = typeof asyncComponentLoader.preloadComponent === 'function' ? asyncComponentLoader.preloadComponent : () => Promise.resolve()
const preloadComponents = typeof asyncComponentLoader.preloadComponents === 'function' ? asyncComponentLoader.preloadComponents : () => Promise.resolve()
const clearCache = typeof asyncComponentLoader.clearCache === 'function' ? asyncComponentLoader.clearCache : () => {}
const getLoadStatus = typeof asyncComponentLoader.getLoadStatus === 'function' ? asyncComponentLoader.getLoadStatus : () => null
const isComponentLoaded = typeof asyncComponentLoader.isComponentLoaded === 'function' ? asyncComponentLoader.isComponentLoaded : () => false
// 确保createDynamicImportLoader存在且已初始化
let createDynamicImportLoaderRef
try {
  createDynamicImportLoaderRef = typeof asyncComponentLoader.createDynamicImportLoader === 'function' ? asyncComponentLoader.createDynamicImportLoader : (() => () => Promise.resolve({}))
} catch (e) {
  createDynamicImportLoaderRef = () => () => Promise.resolve({})
}

export {
  asyncComponentLoader,
  registerAsyncComponent,
  preloadComponent,
  preloadComponents,
  clearCache,
  getLoadStatus,
  isComponentLoaded,
  createDynamicImportLoaderRef as createDynamicImportLoader
}

export default asyncComponentLoader