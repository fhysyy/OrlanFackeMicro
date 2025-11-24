import { ref, reactive, computed } from 'vue'
import { performanceService } from '@/services/performanceService'
import { notificationService } from '@/components/NotificationSystem.vue'
import { PageConfig } from '@/types/page'

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
const defaultConfig: VersionControlConfig = {
  autoSaveInterval: 60,
  enableAutoSave: true,
  maxVersions: 50,
  enableAutoTags: true,
  generateChangeSummary: true
}

// 响应式状态
const config = reactive<VersionControlConfig>({ ...defaultConfig })
const versions = reactive<Map<string, PageVersion[]>>(new Map()) // pageId -> versions
const currentVersions = reactive<Map<string, string>>(new Map()) // pageId -> current versionId
const autoSaveTimers = reactive<Map<string, number>>(new Map()) // pageId -> timerId
const isLoading = ref(false)
const error = ref<string | null>(null)

// 初始化版本控制服务
const initialize = async (customConfig?: Partial<VersionControlConfig>): Promise<void> => {
  try {
    isLoading.value = true
    
    // 应用自定义配置
    if (customConfig) {
      Object.assign(config, customConfig)
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
      versions: Object.fromEntries(versions.entries()),
      currentVersions: Object.fromEntries(currentVersions.entries())
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
  config: PageConfig,
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
    const defaultOptions = {
      name: `版本 ${new Date().toLocaleString()}`,
      description: '',
      userId: 'current_user',
      userName: '当前用户',
      isPublished: false,
      tags: [],
      previousVersionId: currentVersions.get(pageId)
    }
    
    const finalOptions = { ...defaultOptions, ...options }
    
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
        changeSummary = generateChangeSummary(previousVersion.config, config)
      }
    }
    
    // 生成自动标签（如果启用）
    let finalTags = [...finalOptions.tags]
    if (config.enableAutoTags) {
      const autoTags = generateAutoTags(changeSummary)
      finalTags = [...new Set([...finalTags, ...autoTags])]
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
      config: { ...config }, // 深拷贝配置
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
    
    // 更新存储
    versions.set(pageId, pageVersions)
    currentVersions.set(pageId, newVersion.id)
    
    // 保存到本地存储
    await saveVersionsToStorage()
    
    // 记录性能
    performanceService.recordMetric({
      id: `version_created_${newVersion.id}`,
      name: 'Version Creation Time',
      value: performance.now() - startTime,
      unit: 'ms',
      metadata: {
        pageId,
        versionNumber,
        changeCount: changeSummary.length
      }
    })
    
    return newVersion
  } catch (err) {
    console.error('Failed to create version:', err)
    throw new Error('创建版本失败')
  }
}

// 生成变更摘要
const generateChangeSummary = (oldConfig: PageConfig, newConfig: PageConfig): PageVersion['changeSummary'] => {
  const summary: PageVersion['changeSummary'] = []
  
  try {
    // 比较页面属性
    if (JSON.stringify(oldConfig.pageProps) !== JSON.stringify(newConfig.pageProps)) {
      summary.push({
        type: 'modified',
        property: '页面属性',
        oldValue: oldConfig.pageProps,
        newValue: newConfig.pageProps
      })
    }
    
    // 创建组件ID映射
    const oldComponentsMap = new Map(oldConfig.components.map(c => [c.id, c]))
    const newComponentsMap = new Map(newConfig.components.map(c => [c.id, c]))
    
    // 检查新增的组件
    newComponentsMap.forEach((newComponent, componentId) => {
      if (!oldComponentsMap.has(componentId)) {
        summary.push({
          type: 'added',
          componentId,
          componentName: newComponent.name,
          newValue: newComponent
        })
      }
    })
    
    // 检查删除的组件
    oldComponentsMap.forEach((oldComponent, componentId) => {
      if (!newComponentsMap.has(componentId)) {
        summary.push({
          type: 'deleted',
          componentId,
          componentName: oldComponent.name,
          oldValue: oldComponent
        })
      }
    })
    
    // 检查修改的组件
    newComponentsMap.forEach((newComponent, componentId) => {
      if (oldComponentsMap.has(componentId)) {
        const oldComponent = oldComponentsMap.get(componentId)!
        
        // 比较组件属性
        if (JSON.stringify(oldComponent.props) !== JSON.stringify(newComponent.props)) {
          summary.push({
            type: 'modified',
            componentId,
            componentName: newComponent.name,
            property: '组件属性',
            oldValue: oldComponent.props,
            newValue: newComponent.props
          })
        }
        
        // 比较组件样式
        if (JSON.stringify(oldComponent.styles) !== JSON.stringify(newComponent.styles)) {
          summary.push({
            type: 'modified',
            componentId,
            componentName: newComponent.name,
            property: '组件样式',
            oldValue: oldComponent.styles,
            newValue: newComponent.styles
          })
        }
        
        // 检查位置移动
        if (oldComponent.position && newComponent.position) {
          if (oldComponent.position.x !== newComponent.position.x || 
              oldComponent.position.y !== newComponent.position.y) {
            summary.push({
              type: 'moved',
              componentId,
              componentName: newComponent.name,
              oldValue: oldComponent.position,
              newValue: newComponent.position
            })
          }
        }
      }
    })
    
    // 限制摘要长度
    return summary.slice(0, 50) // 最多50个变更记录
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
    typeCounts[change.type]++
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
    
    // 发送通知
    notificationService.success({
      title: '版本恢复成功',
      message: `已成功恢复到版本: ${version.name}`,
      duration: 3000
    })
    
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
    notificationService.success({
      title: '版本删除成功',
      message: `已成功删除版本: ${version.name}`,
      duration: 2000
    })
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
    notificationService.success({
      title: '版本发布成功',
      message: `版本 ${version.name} 已成功发布`,
      duration: 3000
    })
  } catch (err) {
    console.error('Failed to publish version:', err)
    throw new Error('发布版本失败')
  }
}

// 比较两个版本
const compareVersions = (pageId: string, versionId1: string, versionId2: string): {
  version1: PageVersion
  version2: PageVersion
  changes: PageVersion['changeSummary']
} => {
  const version1 = getVersion(pageId, versionId1)
  const version2 = getVersion(pageId, versionId2)
  
  if (!version1 || !version2) {
    throw new Error('指定的版本不存在')
  }
  
  // 计算变更
  const changes = generateChangeSummary(version1.config, version2.config)
  
  return {
    version1,
    version2,
    changes
  }
}

// 设置自动保存
const setupAutoSave = (pageId: string, getCurrentConfig: () => PageConfig): void => {
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
        if (currentVersion && JSON.stringify(currentVersion.config) !== JSON.stringify(currentConfig)) {
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
}

// 更新配置
const updateConfig = (newConfig: Partial<VersionControlConfig>): void => {
  Object.assign(config, newConfig)
  
  // 如果自动保存间隔改变，重新设置所有自动保存定时器
  if (newConfig.autoSaveInterval !== undefined || newConfig.enableAutoSave !== undefined) {
    autoSaveTimers.forEach((_, pageId) => {
      // 注意：这里需要重新获取getCurrentConfig函数，实际应用中可能需要更复杂的处理
      stopAutoSave(pageId)
      // setupAutoSave需要重新调用，但这里无法直接获取getCurrentConfig函数
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
    version.name.toLowerCase().includes(lowercaseQuery) ||
    version.description.toLowerCase().includes(lowercaseQuery) ||
    version.tags.some(tag => tag.toLowerCase().includes(lowercaseQuery)) ||
    version.userName.toLowerCase().includes(lowercaseQuery) ||
    new Date(version.timestamp).toLocaleString().includes(lowercaseQuery)
  )
}

// 版本控制服务
export const versionControlService = {
  // 初始化
  initialize,
  
  // 配置
  config,
  updateConfig,
  
  // 版本操作
  createVersion,
  getVersions,
  getVersion,
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