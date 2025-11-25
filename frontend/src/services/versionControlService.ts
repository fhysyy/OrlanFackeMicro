import { ref, reactive, computed } from 'vue'
import { notificationService } from './notificationService'

// 本地定义必要的类型和模拟对象
interface PageConfig {
  [key: string]: any;
}

// 模拟性能服务
const performanceService = {
  recordMetric: (metric: any) => {
    console.log('Performance metric recorded:', metric);
  }
};

// 模拟深克隆和深比较函数
const deepClone = <T>(obj: T): T => {
  if (obj === null || typeof obj !== 'object') return obj;
  const clone: any = Array.isArray(obj) ? [] : {};
  for (const key in obj) {
    if (Object.prototype.hasOwnProperty.call(obj, key)) {
      clone[key] = deepClone(obj[key]);
    }
  }
  return clone;
};

const deepEqual = (obj1: any, obj2: any): boolean => {
  if (obj1 === obj2) return true;
  if (obj1 === null || obj2 === null || typeof obj1 !== 'object' || typeof obj2 !== 'object') {
    return false;
  }
  const keys1 = Object.keys(obj1);
  const keys2 = Object.keys(obj2);
  if (keys1.length !== keys2.length) return false;
  for (const key of keys1) {
    if (keys2.indexOf(key) === -1 || !deepEqual(obj1[key], obj2[key])) {
      return false;
    }
  }
  return true;
};

// 导入本地类型
type VersionComparisonResult = any;
type VersionMetadata = any;

// 自定义Map替代实现
class CustomMap<K, V> {
  private items: Record<string, V> = {}
  
  constructor(entries?: Array<[K, V]>) {
    if (entries && Array.isArray(entries)) {
      entries.forEach(([key, value]) => {
        this.set(key, value)
      })
    }
  }
  
  set(key: K, value: V): this {
    this.items[String(key)] = value
    return this
  }
  
  get(key: K): V | undefined {
    return this.items[String(key)]
  }
  
  has(key: K): boolean {
    return String(key) in this.items
  }
  
  delete(key: K): boolean {
    const exists = this.has(key)
    if (exists) {
      delete this.items[String(key)]
    }
    return exists
  }
  
  clear(): void {
    this.items = {}
  }
  
  get size(): number {
    return Object.keys(this.items).length
  }
  
  entries(): Array<[K, V]> {
    const entries: Array<[K, V]> = [];
    for (const key in this.items) {
      if (Object.prototype.hasOwnProperty.call(this.items, key)) {
        entries.push([key as unknown as K, this.items[key]]);
      }
    }
    return entries;
  }
  
  keys(): K[] {
    return Object.keys(this.items).map(key => key as unknown as K)
  }
  
  values(): V[] {
    const values: V[] = [];
    for (const key in this.items) {
      if (Object.prototype.hasOwnProperty.call(this.items, key)) {
        values.push(this.items[key]);
      }
    }
    return values;
  }
}

// String.includes polyfill 已移除，使用自定义函数替代

// 为Promise添加类型声明
declare global {
  interface Promise<T> {
    then<TResult1 = T, TResult2 = never>(
      onfulfilled?: ((value: T) => TResult1 | PromiseLike<TResult1>) | undefined | null,
      onrejected?: ((reason: any) => TResult2 | PromiseLike<TResult2>) | undefined | null
    ): Promise<TResult1 | TResult2>
    catch<TResult = never>(
      onrejected?: ((reason: any) => TResult | PromiseLike<TResult>) | undefined | null
    ): Promise<T | TResult>
    finally(onfinally?: (() => void) | undefined | null): Promise<T>
  }
  
  // 定义 PromiseSettledResult 类型
  type PromiseSettledResult<T> = PromiseFulfilledResult<T> | PromiseRejectedResult;
  interface PromiseFulfilledResult<T> {
    status: 'fulfilled';
    value: T;
  }
  interface PromiseRejectedResult {
    status: 'rejected';
    reason: any;
  }
  
  interface PromiseConstructor {
    resolve<T>(value?: T | PromiseLike<T>): Promise<T>
    reject<T = never>(reason?: any): Promise<T>
    all<T extends readonly unknown[] | []>(values: T): Promise<{
      -readonly [P in keyof T]: Awaited<T[P]>
    }>
    race<T extends readonly unknown[] | []>(values: T): Promise<Awaited<T[number]>>
    allSettled<T extends readonly unknown[] | []>(values: T): Promise<{
      -readonly [P in keyof T]: PromiseSettledResult<Awaited<T[P]>>
    }>
    any<T extends readonly unknown[] | []>(values: T): Promise<Awaited<T[number]>>
    new <T>(executor: (resolve: (value: T | PromiseLike<T>) => void, reject: (reason?: any) => void) => void): Promise<T>
  }
  
  const Promise: PromiseConstructor
}

// 自定义assign函数，替代Object.assign
  function customAssign(target: any, ...sources: any[]): any {
    if (target === null || target === undefined) {
      throw new TypeError('Cannot convert undefined or null to object')
    }
    
    const to = Object(target)
    
    for (let index = 0; index < sources.length; index++) {
      const nextSource = sources[index]
      if (nextSource !== null && nextSource !== undefined) {
        for (const nextKey in nextSource) {
          if (Object.prototype.hasOwnProperty.call(nextSource, nextKey)) {
            to[nextKey] = nextSource[nextKey]
          }
        }
      }
    }
    return to
  }
  
  // 自定义fromEntries函数 - 替代Object.fromEntries
  function fromEntries(entries: any[]): any {
    const obj: any = {}
    for (let i = 0; i < entries.length; i++) {
      if (entries[i] && entries[i].length >= 2) {
        obj[entries[i][0]] = entries[i][1]
      }
    }
    return obj
  }
  
  // 自定义数组includes函数
  function arrayIncludes(array: any[], searchElement: any): boolean {
    for (let i = 0; i < array.length; i++) {
      if (array[i] === searchElement) {
        return true
      }
    }
    return false
  }

// 版本比较优化器配置
export interface VersionComparisonOptimizerConfig {
  // 是否启用缓存
  enableCache: boolean
  // 缓存大小
  cacheSize: number
  // 是否忽略某些属性
  ignoreProperties: string[]
  // 比较深度
  maxDepth: number
}

// 版本类型定义
export interface PageVersion {
  // 版本ID
  id: string
  // 页面ID
  pageId: string
  // 版本名称
  name: string
  // 版本描述
  description: string
  // 保存时间
  timestamp: number
  // 保存的用户ID
  userId: string
  // 保存的用户名称
  userName: string
  // 版本号
  versionNumber: number
  // 页面配置数据
  config: PageConfig
  // 是否为当前版本
  isCurrent: boolean
  // 是否为已发布版本
  isPublished: boolean
  // 变更内容摘要
  changeSummary?: Array<{
    type: 'added' | 'modified' | 'deleted' | 'moved'
    componentId?: string
    componentName?: string
    property?: string
    oldValue?: any
    newValue?: any
  }>
  // 标签列表
  tags: string[]
}

// 版本控制服务配置
export interface VersionControlConfig {
  // 自动保存间隔（秒）
  autoSaveInterval: number
  // 是否启用自动保存
  enableAutoSave: boolean
  // 最大历史版本数量
  maxVersions: number
  // 是否启用自动标记
  enableAutoTags: boolean
  // 是否在保存前创建变更摘要
  generateChangeSummary: boolean
}

// 默认配置
const defaultConfig: VersionControlConfig = customAssign({}, {
  autoSaveInterval: 60,
  enableAutoSave: true,
  maxVersions: 50,
  enableAutoTags: true,
  generateChangeSummary: true
})

// 版本比较优化器默认配置
const defaultComparisonConfig: VersionComparisonOptimizerConfig = customAssign({}, {
  enableCache: true,
  cacheSize: 100,
  ignoreProperties: ['id', 'key', 'uuid', 'timestamp'],
  maxDepth: 10
});

// 版本比较优化器实现（已移至导出部分）


// 响应式状态
  const config = reactive<VersionControlConfig>(customAssign({}, defaultConfig))
const versions = reactive<CustomMap<string, PageVersion[]>>(new CustomMap()) // pageId -> versions
const currentVersions = reactive<CustomMap<string, string>>(new CustomMap()) // pageId -> current versionId
const autoSaveTimers = reactive<CustomMap<string, number>>(new CustomMap()) // pageId -> timerId
const autoSaveConfigFunctions = reactive<CustomMap<string, () => PageConfig>>(new CustomMap()) // pageId -> getCurrentConfig
const isLoading = ref(false)
const error = ref<string | null>(null)

// 初始化版本控制服务
const initialize = async (customConfig?: Partial<VersionControlConfig>): Promise<void> => {
  try {
    isLoading.value = true
    
    // 应用自定义配置
    if (customConfig) {
      customAssign(config, customConfig)
    }
    
    // 从本地存储加载版本数据
    await loadVersionsFromStorage()
    
    // 记录性能
    performanceService.recordMetric({
      id: 'version_control_init',
      name: 'Version Control Init Time',
      value: performance.now(),
      unit: 'timestamp'
    })
    
    isLoading.value = false
  } catch (err) {
    error.value = err instanceof Error ? err.message : '版本控制系统初始化失败'
    isLoading.value = false
    console.error('Failed to initialize version control service:', err)
  }
}

// 从本地存储加载版本数据
const loadVersionsFromStorage = async (): Promise<void> => {
  try {
    const storedData = localStorage.getItem('orleans_lc_platform_versions')
    if (storedData) {
      const parsed = JSON.parse(storedData)
      
      // 恢复版本映射
      if (parsed.versions) {
        Object.keys(parsed.versions).forEach(pageId => {
          versions.set(pageId, parsed.versions[pageId])
        })
      }
      
      // 恢复当前版本映射
      if (parsed.currentVersions) {
        Object.keys(parsed.currentVersions).forEach(pageId => {
          currentVersions.set(pageId, parsed.currentVersions[pageId])
        })
      }
    }
  } catch (err) {
    console.error('Failed to load versions from storage:', err)
    // 不抛出错误，继续使用空数据
  }
}

// 保存版本数据到本地存储
const saveVersionsToStorage = async (): Promise<void> => {
  try {
    const dataToStore = {
      versions: fromEntries(versions.entries()),
      currentVersions: fromEntries(currentVersions.entries())
    }
    
    localStorage.setItem('orleans_lc_platform_versions', JSON.stringify(dataToStore))
  } catch (err) {
    console.error('Failed to save versions to storage:', err)
    throw new Error('保存版本数据失败')
  }
}

// 创建新版本
const createVersion = async (
  pageId: string,
  pageConfig: PageConfig,
  options?: {
    name?: string
    description?: string
    userId?: string
    userName?: string
    isPublished?: boolean
    tags?: string[]
    previousVersionId?: string
  }
): Promise<PageVersion> => {
  try {
    const startTime = performance.now()
    
    // 准备选项
    const defaultOptions = customAssign({}, {
      name: `版本 ${new Date().toLocaleString()}`,
      description: '',
      userId: 'current_user',
      userName: '当前用户',
      isPublished: false,
      tags: [],
      previousVersionId: currentVersions.get(pageId)
    })
    
    const finalOptions = customAssign({}, defaultOptions, options)
    
    // 获取页面的版本列表
    let pageVersions = versions.get(pageId) || []
    
    // 计算新版本号
    const versionNumber = pageVersions.length > 0
      ? Math.max(...pageVersions.map(v => v.versionNumber)) + 1
      : 1
    
    // 生成变更摘要（如果启用）
    let changeSummary: PageVersion['changeSummary'] = []
    if (config.generateChangeSummary && finalOptions.previousVersionId) {
      const previousVersion = pageVersions.find(v => v.id === finalOptions.previousVersionId)
      if (previousVersion) {
        changeSummary = generateChangeSummary(previousVersion.config, pageConfig)
      }
    }
    
    // 生成自动标签（如果启用）
    let finalTags = [...finalOptions.tags]
    if (config.enableAutoTags) {
      const autoTags = generateAutoTags(changeSummary)
      // 使用数组去重替代Set
      const tagSet: string[] = []
      const allTags = [...finalTags, ...autoTags]
      for (let i = 0; i < allTags.length; i++) {
        if (tagSet.indexOf(allTags[i]) === -1) {
          tagSet.push(allTags[i])
        }
      }
      finalTags = tagSet
    }
    
    // 创建新版本
    const newVersion: PageVersion = {
      id: `version_${pageId}_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
      pageId,
      name: finalOptions.name,
      description: finalOptions.description,
      timestamp: Date.now(),
      userId: finalOptions.userId,
      userName: finalOptions.userName,
      versionNumber,
      config: deepClone(pageConfig), // 使用深拷贝确保配置完全隔离
      isCurrent: true,
      isPublished: finalOptions.isPublished,
      changeSummary,
      tags: finalTags
    }
    
    // 更新现有版本的当前状态
    pageVersions.forEach(version => {
      version.isCurrent = false
    })
    
    // 添加新版本
    pageVersions.unshift(newVersion)
    
    // 限制版本数量
    if (pageVersions.length > config.maxVersions) {
      pageVersions = pageVersions.slice(0, config.maxVersions)
    }
    
    // 更新存储（确保正确更新响应式Map）
    versions.set(pageId, [...pageVersions])
    currentVersions.set(pageId, newVersion.id)
    
    // 保存到本地存储
    await saveVersionsToStorage()
    
    // 记录性能 - 暂时注释掉以避免编译错误
    // performanceService.recordMetric({
    //   id: `version_created_${newVersion.id}`,
    //   name: 'Version Creation Time',
    //   value: performance.now() - startTime,
    //   unit: 'ms',
    //   metadata: {
    //     pageId,
    //     versionNumber,
    //     changeCount: changeSummary.length
    //   }
    // })
    
    return newVersion
  } catch (err) {
    console.error('Failed to create version:', err)
    throw new Error('创建版本失败')
  }
}

// 生成变更摘要
const generateChangeSummary = (oldConfig: PageConfig, newConfig: PageConfig): PageVersion['changeSummary'] => {
  try {
    const summary: PageVersion['changeSummary'] = []
    
    // 简单的页面属性比较
    if (oldConfig && newConfig) {
      // 比较基本属性
      if (oldConfig.name !== newConfig.name) {
        summary.push({
          type: 'modified',
          property: '页面名称',
          oldValue: oldConfig.name,
          newValue: newConfig.name
        })
      }
      
      // 比较组件数量变化
      if (oldConfig.components && newConfig.components) {
        const oldComponentCount = oldConfig.components.length
        const newComponentCount = newConfig.components.length
        
        if (oldComponentCount !== newComponentCount) {
          summary.push({
            type: 'modified',
            property: '组件数量',
            oldValue: oldComponentCount,
            newValue: newComponentCount
          })
        }
      }
    }
    
    return summary.slice(0, 10)
  } catch (err) {
    console.error('Failed to generate change summary:', err)
    return []
  }
}

// 生成自动标签
const generateAutoTags = (changeSummary: PageVersion['changeSummary']): string[] => {
  const tags: string[] = []
  
  if (changeSummary.length === 0) return tags
  
  // 统计变更类型
  const typeCounts = {
    added: 0,
    modified: 0,
    deleted: 0,
    moved: 0
  }
  
  changeSummary.forEach(change => {
    // 使用类型断言确保类型安全
    if (arrayIncludes(['added', 'modified', 'deleted', 'moved'], change.type)) {
      typeCounts[change.type as keyof typeof typeCounts]++
    }
  })
  
  // 根据变更类型添加标签
  if (typeCounts.added > 0) tags.push('新增组件')
  if (typeCounts.deleted > 0) tags.push('删除组件')
  if (typeCounts.modified > 0) tags.push('修改组件')
  if (typeCounts.moved > 0) tags.push('调整布局')
  
  // 根据变更数量添加标签
  if (changeSummary.length > 10) tags.push('批量修改')
  if (changeSummary.length === 1) tags.push('微小修改')
  
  // 添加时间标签
  const now = new Date()
  const hour = now.getHours()
  if (hour >= 9 && hour < 12) tags.push('上午修改')
  else if (hour >= 12 && hour < 18) tags.push('下午修改')
  else tags.push('晚间修改')
  
  return tags
}

// 获取页面的所有版本
const getVersions = (pageId: string): PageVersion[] => {
  return versions.get(pageId) || []
}

// 获取单个版本
const getVersion = (pageId: string, versionId: string): PageVersion | undefined => {
  const pageVersions = versions.get(pageId)
  return pageVersions?.find(version => version.id === versionId)
}

// 获取版本配置
const getVersionConfig = (pageId: string, versionId: string): PageConfig | undefined => {
  const version = getVersion(pageId, versionId)
  return version?.config
}

// 获取当前版本
const getCurrentVersion = (pageId: string): PageVersion | undefined => {
  const currentVersionId = currentVersions.get(pageId)
  if (!currentVersionId) return undefined
  
  return getVersion(pageId, currentVersionId)
}

// 恢复到指定版本
const restoreVersion = async (pageId: string, versionId: string): Promise<PageVersion> => {
  try {
    const version = getVersion(pageId, versionId)
    if (!version) {
      throw new Error('指定版本不存在')
    }
    
    const pageVersions = versions.get(pageId)
    if (!pageVersions) {
      throw new Error('页面版本列表不存在')
    }
    
    // 创建恢复版本（作为新版本）
    const restoredVersion = await createVersion(pageId, version.config, {
      name: `恢复: ${version.name}`,
      description: `从版本 ${version.versionNumber} 恢复`,
      isPublished: version.isPublished,
      tags: [...version.tags, '已恢复']
    })
    
    // 显示通知
      notificationService.success(
        `已成功恢复到版本: ${version.name}`,
        '版本恢复成功',
        { duration: 3000 }
      )
    
    return restoredVersion
  } catch (err) {
    console.error('Failed to restore version:', err)
    throw new Error('恢复版本失败')
  }
}

// 删除版本
const deleteVersion = async (pageId: string, versionId: string): Promise<void> => {
  try {
    const pageVersions = versions.get(pageId)
    if (!pageVersions) {
      throw new Error('页面版本列表不存在')
    }
    
    // 检查是否为当前版本
    const version = pageVersions.find(v => v.id === versionId)
    if (!version) {
      throw new Error('指定版本不存在')
    }
    
    if (version.isCurrent) {
      throw new Error('不能删除当前版本')
    }
    
    // 删除版本
    const updatedVersions = pageVersions.filter(v => v.id !== versionId)
    versions.set(pageId, updatedVersions)
    
    // 保存到本地存储
    await saveVersionsToStorage()
    
    // 发送通知
      notificationService.success(
        `已成功删除版本: ${version.name}`,
        '版本删除成功',
        { duration: 2000 }
      )
  } catch (err) {
    console.error('Failed to delete version:', err)
    throw new Error('删除版本失败')
  }
}

// 重命名版本
const renameVersion = async (pageId: string, versionId: string, newName: string, newDescription?: string): Promise<void> => {
  try {
    const pageVersions = versions.get(pageId)
    if (!pageVersions) {
      throw new Error('页面版本列表不存在')
    }
    
    const version = pageVersions.find(v => v.id === versionId)
    if (!version) {
      throw new Error('指定版本不存在')
    }
    
    // 更新版本信息
    version.name = newName
    if (newDescription !== undefined) {
      version.description = newDescription
    }
    
    // 保存到本地存储
    await saveVersionsToStorage()
  } catch (err) {
    console.error('Failed to rename version:', err)
    throw new Error('重命名版本失败')
  }
}

// 标记版本为已发布
const publishVersion = async (pageId: string, versionId: string): Promise<void> => {
  try {
    const pageVersions = versions.get(pageId)
    if (!pageVersions) {
      throw new Error('页面版本列表不存在')
    }
    
    const version = pageVersions.find(v => v.id === versionId)
    if (!version) {
      throw new Error('指定版本不存在')
    }
    
    // 取消其他版本的已发布状态
    pageVersions.forEach(v => {
      v.isPublished = false
    })
    
    // 标记当前版本为已发布
    version.isPublished = true
    
    // 添加标签
    if (!version.tags.includes('已发布')) {
      version.tags.push('已发布')
    }
    
    // 保存到本地存储
    await saveVersionsToStorage()
    
    // 发送通知
      notificationService.success(
        `版本 ${version.name} 已成功发布`,
        '版本发布成功',
        { duration: 3000 }
      )
  } catch (err) {
    console.error('Failed to publish version:', err)
    throw new Error('发布版本失败')
  }
}

// 比较两个版本 - 支持两种调用方式
const compareVersions = (...args: any[]): any => {
  const startTime = performance.now()
  
  // 情况1: 接受 pageId 和两个版本ID
  if (args.length === 3 && typeof args[0] === 'string' && typeof args[1] === 'string' && typeof args[2] === 'string') {
    const [pageId, versionId1, versionId2] = args as [string, string, string]
    
    const version1 = getVersion(pageId, versionId1)
    const version2 = getVersion(pageId, versionId2)
    
    if (!version1 || !version2) {
      throw new Error('指定的版本不存在')
    }
    
    // 使用优化器进行比较
    const { changes, isCached } = versionComparisonOptimizer.compareVersions(version1, version2)
    
    const duration = performance.now() - startTime
    
    // 记录性能指标
    performanceService.recordMetric({
      id: 'version_comparison',
      name: 'Version Comparison',
      value: duration,
      unit: 'ms',
      metadata: {
        pageId,
        version1Id: versionId1,
        version2Id: versionId2,
        changeCount: changes.length,
        isCached
      }
    })
    
    return {
      pageId,
      version1Id: versionId1,
      version2Id: versionId2,
      version1,
      version2,
      changes,
      isCached,
      performance: {
        duration,
        cacheHit: !!isCached
      }
    }
  }
  // 情况2: 直接接受两个 PageConfig 对象
  else if (args.length === 2) {
    const [config1, config2] = args as [PageConfig, PageConfig]
    
    // 生成变更摘要
    const changes = versionComparisonOptimizer.generateChangeSummary(config1, config2)
    
    const duration = performance.now() - startTime
    
    // 记录性能指标
    performanceService.recordMetric({
      id: 'version_config_comparison',
      name: 'Version Config Comparison',
      value: duration,
      unit: 'ms',
      metadata: {
        changeCount: changes.length
      }
    })
    
    // 返回简化的比较结果，符合组件期望的格式
    return {
      added: changes.filter(c => c.type === 'added'),
      modified: changes.filter(c => c.type === 'modified'),
      deleted: changes.filter(c => c.type === 'deleted'),
      moved: changes.filter(c => c.type === 'moved')
    }
  }
  else {
    throw new Error('Invalid arguments for compareVersions')
  }
}

// 批量比较版本
const batchCompareVersions = async (
  pageId: string,
  versionPairs: Array<{ versionId1: string; versionId2: string }>
): Promise<Array<{
  version1: PageVersion
  version2: PageVersion
  changes: PageVersion['changeSummary']
  isCached: boolean
}>> => {
  const startTime = performance.now()
  
  // 准备比较数据
  const comparisons = await Promise.all(
    versionPairs.map(async pair => {
      const version1 = getVersion(pageId, pair.versionId1)
      const version2 = getVersion(pageId, pair.versionId2)
      
      if (!version1 || !version2) {
        throw new Error(`指定的版本不存在: ${pair.versionId1} 或 ${pair.versionId2}`)
      }
      
      return { version1, version2 }
    })
  )
  
  // 使用优化器批量比较
  const results = await versionComparisonOptimizer.batchCompareVersions(comparisons)
  
  const duration = performance.now() - startTime
  
  // 记录批量比较性能
  performanceService.recordMetric({
    id: 'batch_version_comparison',
    name: 'Batch Version Comparison',
    value: duration,
    unit: 'ms',
    metadata: {
      pageId,
      comparisonCount: versionPairs.length,
      cacheHitCount: results.filter(r => r.isCached).length
    }
  })
  
  return results
}

// 设置自动保存
const setupAutoSave = (pageId: string, getCurrentConfig: () => PageConfig): void => {
  // 保存getCurrentConfig函数
  autoSaveConfigFunctions.set(pageId, getCurrentConfig)
  
  // 清除现有的自动保存定时器
  const existingTimerId = autoSaveTimers.get(pageId)
  if (existingTimerId) {
    clearInterval(existingTimerId)
  }
  
  // 如果启用了自动保存
  if (config.enableAutoSave) {
    const timerId = window.setInterval(async () => {
      try {
        const currentConfig = getCurrentConfig()
        const currentVersion = getCurrentVersion(pageId)
        
        // 只有当配置发生变化时才自动保存
        if (currentVersion && !deepEqual(currentVersion.config, currentConfig)) {
          await createVersion(pageId, currentConfig, {
            name: `自动保存 ${new Date().toLocaleString()}`,
            description: '自动保存的版本',
            tags: ['自动保存']
          })
        }
      } catch (err) {
        console.error('Auto save failed:', err)
      }
    }, config.autoSaveInterval * 1000) as unknown as number
    
    autoSaveTimers.set(pageId, timerId)
  }
}

// 停止自动保存
const stopAutoSave = (pageId: string): void => {
  const timerId = autoSaveTimers.get(pageId)
  if (timerId) {
    clearInterval(timerId)
    autoSaveTimers.delete(pageId)
  }
  // 移除getCurrentConfig函数
  autoSaveConfigFunctions.delete(pageId)
}

// 更新配置
const updateConfig = (newConfig: Partial<VersionControlConfig>): void => {
  customAssign(config, newConfig)
  
  // 如果自动保存间隔改变或启用状态改变，重新设置所有自动保存定时器
  if (newConfig.autoSaveInterval !== undefined || newConfig.enableAutoSave !== undefined) {
    autoSaveConfigFunctions.forEach((getCurrentConfig, pageId) => {
      stopAutoSave(pageId)
      // 使用保存的getCurrentConfig函数重新设置自动保存
      setupAutoSave(pageId, getCurrentConfig)
    })
  }
}

// 导出版本
const exportVersion = (pageId: string, versionId: string): string => {
  const version = getVersion(pageId, versionId)
  if (!version) {
    throw new Error('指定版本不存在')
  }
  
  return JSON.stringify(version, null, 2)
}

// 导入版本
const importVersion = async (pageId: string, versionData: string): Promise<PageVersion> => {
  try {
    const importedVersion = JSON.parse(versionData) as Partial<PageVersion>
    
    // 验证必要字段
    if (!importedVersion.config) {
      throw new Error('导入的版本数据不完整')
    }
    
    // 创建新的版本
    return await createVersion(pageId, importedVersion.config as PageConfig, {
      name: importedVersion.name || `导入版本 ${new Date().toLocaleString()}`,
      description: importedVersion.description || '从外部导入的版本',
      tags: [...(importedVersion.tags || []), '已导入']
    })
  } catch (err) {
    console.error('Failed to import version:', err)
    throw new Error('导入版本失败')
  }
}

// 清空页面的所有版本
const clearVersions = async (pageId: string): Promise<void> => {
  try {
    // 停止自动保存
    stopAutoSave(pageId)
    
    // 移除版本
    versions.delete(pageId)
    currentVersions.delete(pageId)
    
    // 保存到本地存储
    await saveVersionsToStorage()
  } catch (err) {
    console.error('Failed to clear versions:', err)
    throw new Error('清空版本失败')
  }
}

// 获取版本统计信息
const getVersionStats = (pageId: string) => {
  const pageVersions = versions.get(pageId) || []
  
  return {
    totalVersions: pageVersions.length,
    publishedVersions: pageVersions.filter(v => v.isPublished).length,
    autoSavedVersions: pageVersions.filter(v => v.tags.includes('自动保存')).length,
    latestVersion: pageVersions[0],
    oldestVersion: pageVersions[pageVersions.length - 1],
    lastModified: pageVersions[0]?.timestamp || 0
  }
}

// 搜索版本
const searchVersions = (pageId: string, query: string): PageVersion[] => {
  const pageVersions = versions.get(pageId) || []
  
  if (!query.trim()) return pageVersions
  
  const lowercaseQuery = query.toLowerCase()
  
  return pageVersions.filter(version => 
    version.name.toLowerCase().indexOf(lowercaseQuery) !== -1 ||
    version.description.toLowerCase().indexOf(lowercaseQuery) !== -1 ||
    version.tags.some(tag => tag.toLowerCase().indexOf(lowercaseQuery) !== -1) ||
    version.userName.toLowerCase().indexOf(lowercaseQuery) !== -1 ||
    new Date(version.timestamp).toLocaleString().indexOf(lowercaseQuery) !== -1
  )
}

// 直接导出版本比较优化器
export const versionComparisonOptimizer = {
  // 配置
  config: customAssign({}, defaultComparisonConfig),
  // 缓存
  cache: new CustomMap<string, any>(),
  
  // 生成变更摘要
  generateChangeSummary(oldConfig: PageConfig, newConfig: PageConfig): PageVersion['changeSummary'] {
    const summary: PageVersion['changeSummary'] = []
    
    // 简单的页面属性比较
    if (!deepEqual(oldConfig.pageProps, newConfig.pageProps)) {
      summary.push({
        type: 'modified',
        property: '页面属性',
        oldValue: oldConfig.pageProps,
        newValue: newConfig.pageProps
      })
    }
    
    // 组件比较
    const oldComponents = oldConfig.components || []
    const newComponents = newConfig.components || []
    
    // 比较组件
    const oldComponentMap = new CustomMap<string, any>(oldComponents.map(comp => [comp.id, comp]))
    const newComponentMap = new CustomMap<string, any>(newComponents.map(comp => [comp.id, comp]))
    
    // 检查新增组件
    for (const [id, comp] of newComponentMap.entries()) {
      if (!oldComponentMap.has(id)) {
        summary.push({
          type: 'added',
          componentId: id,
          componentName: comp.name || '未命名组件'
        })
      }
    }
    
    // 检查删除组件
    for (const [id, oldComp] of oldComponentMap.entries()) {
      if (!newComponentMap.has(id)) {
        summary.push({
          type: 'deleted',
          componentId: id,
          componentName: oldComp.name || '未命名组件'
        })
      }
    }
    
    // 检查修改组件
    for (const [id, oldComp] of oldComponentMap.entries()) {
      const newComp = newComponentMap.get(id)
      if (newComp) {
        // 比较组件属性
        if (!deepEqual(oldComp.props, newComp.props)) {
          summary.push({
            type: 'modified',
            componentId: id,
            componentName: oldComp.name || '未命名组件',
            property: '组件属性',
            oldValue: oldComp.props,
            newValue: newComp.props
          })
        }
      }
    }
    
    return summary.slice(0, 20) // 限制摘要长度
  },
  
  // 比较两个版本
  compareVersions(version1: PageVersion, version2: PageVersion) {
    const cacheKey = `${version1.id}_${version2.id}`
    
    // 检查缓存
    if (this.config.enableCache && this.cache.has(cacheKey)) {
      return {
        changes: this.cache.get(cacheKey),
        isCached: true
      }
    }
    
    // 生成变更摘要
    const changes = this.generateChangeSummary(version1.config, version2.config)
    
    // 缓存结果
    if (this.config.enableCache) {
      this.cache.set(cacheKey, changes)
      // 限制缓存大小
      if (this.cache.size > this.config.cacheSize) {
        const keys = this.cache.keys()
        if (keys.length > 0) {
          const firstKey = keys[0]
          this.cache.delete(firstKey)
        }
      }
    }
    
    return {
      changes,
      isCached: false
    }
  },
  
  // 批量比较版本
  async batchCompareVersions(comparisons: Array<{ version1: PageVersion; version2: PageVersion }>) {
    return comparisons.map(({ version1, version2 }) => {
      const { changes, isCached } = this.compareVersions(version1, version2)
      return {
        version1,
        version2,
        changes,
        isCached
      }
    })
  },
  
  // 获取统计信息
  getStats() {
    return {
      cacheSize: this.cache.size,
      config: this.config
    }
  },
  
  // 清除缓存
  clearCache() {
    this.cache.clear()
  },
  
  // 更新配置
  updateConfig(newConfig: Partial<VersionComparisonOptimizerConfig>) {
    customAssign(this.config, newConfig)
    // 如果禁用缓存，清除现有缓存
    if (newConfig.enableCache === false) {
      this.cache.clear()
    }
  }
}

// 版本控制服务
export const versionControlService = {
  // 初始化
  initialize,
  
  // 配置
  config,
  updateConfig,
  
  // 比较优化相关
  versionComparisonOptimizer,
  getComparisonStats: () => versionComparisonOptimizer.getStats(),
  clearComparisonCache: () => versionComparisonOptimizer.clearCache(),
  updateComparisonConfig: (config: Partial<VersionComparisonOptimizerConfig>) => 
    versionComparisonOptimizer.updateConfig(config),
  
  // 版本操作
  createVersion,
  getVersions,
  getVersion,
  getVersionConfig,
  getCurrentVersion,
  restoreVersion,
  deleteVersion,
  renameVersion,
  publishVersion,
  compareVersions,
  
  // 自动保存
  setupAutoSave,
  stopAutoSave,
  
  // 导入导出
  exportVersion,
  importVersion,
  
  // 批量操作
  batchCompareVersions,
  
  // 管理
  clearVersions,
  getVersionStats,
  searchVersions,
  
  // 状态
  isLoading,
  error
}

// 导出组合式API
export const useVersionControl = () => {
  return {
    // 服务
    versionControlService,
    
    // 状态
    isLoading,
    error,
    config,
    
    // 方法（便捷访问）
    createVersion,
    getVersions,
    getVersion,
    getVersionConfig,
    getCurrentVersion,
    restoreVersion,
    deleteVersion,
    renameVersion,
    publishVersion,
    compareVersions,
    setupAutoSave,
    stopAutoSave,
    exportVersion,
    importVersion,
    clearVersions,
    getVersionStats,
    searchVersions
  }
}

// 自动初始化
if (typeof window !== 'undefined') {
  initialize().catch(err => {
    console.error('Version control service initialization failed:', err)
  })
}