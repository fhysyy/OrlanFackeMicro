import { ref, reactive, computed } from 'vue'
import { performanceService } from './performanceService'
import { notificationService } from './notificationService';
import { PageConfig } from '@/types/page'
import type { ComponentConfig } from '@/types/component'
import { versionControlService } from './versionControlService'

// 高级版本差异类型定义
export interface AdvancedVersionDiff {
  // 总体统计
  stats: {
    addedComponents: number
    modifiedComponents: number
    deletedComponents: number
    addedProperties: number
    modifiedProperties: number
    deletedProperties: number
    addedStyles: number
    modifiedStyles: number
    deletedStyles: number
  }
  // 组件级差异
  componentDiffs: ComponentDiff[]
  // 页面级差异
  pageDiffs: PropertyDiff[]
  // 变更历史时间线
  changeTimeline: ChangeTimelineItem[]
  // 可恢复的变更路径
  changePaths: string[]
}

export interface ComponentDiff {
  id: string
  name: string
  type: 'added' | 'modified' | 'deleted' | 'moved'
  componentType: string
  position?: {
    old: number
    new: number
  }
  propertyDiffs: PropertyDiff[]
  styleDiffs: PropertyDiff[]
  childrenDiffs?: ComponentDiff[]
  eventsDiff?: EventsDiff
}

export interface PropertyDiff {
  property: string
  type: 'added' | 'modified' | 'deleted'
  oldValue: any
  newValue: any
  propertyType: string
  isStyle?: boolean
}

export interface EventsDiff {
  added: number
  modified: number
  deleted: number
  changes: EventChange[]
}

export interface EventChange {
  eventType: string
  actionIndex: number
  type: 'added' | 'modified' | 'deleted'
  actionChange?: {
    type?: string
    config?: any
  }
}

export interface ChangeTimelineItem {
  timestamp: number
  userName: string
  versionId: string
  versionName: string
  changeCount: number
  isMajorChange: boolean
}

export interface VersionVisualizationOptions {
  showComponentDetails: boolean
  showStyleDetails: boolean
  showPropertyDetails: boolean
  showEventsDetails: boolean
  showChildrenDetails: boolean
  maxDepth: number
  filterChanges: string[]
}

// 默认可视化选项
const defaultVisualizationOptions: VersionVisualizationOptions = {
  showComponentDetails: true,
  showStyleDetails: true,
  showPropertyDetails: true,
  showEventsDetails: true,
  showChildrenDetails: true,
  maxDepth: 3,
  filterChanges: []
}

// 高级版本控制服务
export const advancedVersionControlService = {
  // 配置
  visualizationOptions: reactive<VersionVisualizationOptions>({ ...defaultVisualizationOptions }),
  
  // 高级版本比较
  async compareVersionsAdvanced(
    pageId: string,
    oldVersionId: string,
    newVersionId: string
  ): Promise<AdvancedVersionDiff> {
    const startTime = performance.now()
    
    try {
      // 获取两个版本
      const oldVersion = versionControlService.getVersion(pageId, oldVersionId)
      const newVersion = versionControlService.getVersion(pageId, newVersionId)
      
      if (!oldVersion || !newVersion) {
        throw new Error('版本不存在')
      }
      
      // 获取完整配置
      const oldConfig = await versionControlService.getVersionConfig(pageId, oldVersionId)
      const newConfig = await versionControlService.getVersionConfig(pageId, newVersionId)
      
      // 执行深度比较
      const diff = this.performDeepDiff(oldConfig, newConfig)
      
      // 生成变更时间线
      const changeTimeline = this.generateChangeTimeline(pageId, oldVersion, newVersion)
      
      // 生成变更路径
      const changePaths = this.generateChangePaths(diff)
      
      // 记录性能
      performanceService.recordMetric({
        id: 'advanced_version_compare',
        name: 'Advanced Version Comparison',
        value: performance.now() - startTime,
        unit: 'ms',
        metadata: {
          oldVersionId,
          newVersionId,
          componentCount: diff.stats.addedComponents + diff.stats.modifiedComponents + diff.stats.deletedComponents
        }
      })
      
      return {
        ...diff,
        changeTimeline,
        changePaths
      }
    } catch (error) {
      console.error('高级版本比较失败:', error)
      notificationService.error('版本比较失败', error instanceof Error ? error.message : '未知错误')
      throw error
    }
  },
  
  // 执行深度差异比较
  performDeepDiff(oldConfig: PageConfig, newConfig: PageConfig): Omit<AdvancedVersionDiff, 'changeTimeline' | 'changePaths'> {
    const result: Omit<AdvancedVersionDiff, 'changeTimeline' | 'changePaths'> = {
      stats: {
        addedComponents: 0,
        modifiedComponents: 0,
        deletedComponents: 0,
        addedProperties: 0,
        modifiedProperties: 0,
        deletedProperties: 0,
        addedStyles: 0,
        modifiedStyles: 0,
        deletedStyles: 0
      },
      componentDiffs: [],
      pageDiffs: []
    }
    
    // 比较页面属性
    result.pageDiffs = this.comparePageProperties(oldConfig, newConfig)
    result.pageDiffs.forEach(diff => {
      if (diff.type === 'added') result.stats.addedProperties++
      else if (diff.type === 'modified') result.stats.modifiedProperties++
      else if (diff.type === 'deleted') result.stats.deletedProperties++
    })
    
    // 比较组件
    result.componentDiffs = this.compareComponents(
      oldConfig.components || [],
      newConfig.components || [],
      result.stats
    )
    
    return result
  },
  
  // 比较页面属性
  comparePageProperties(oldConfig: PageConfig, newConfig: PageConfig): PropertyDiff[] {
    const diffs: PropertyDiff[] = []
    
    const compareProps = ['name', 'description', 'layout', 'settings', 'metadata']
    
    compareProps.forEach(prop => {
      const oldValue = (oldConfig as any)[prop]
      const newValue = (newConfig as any)[prop]
      
      if (oldValue === undefined && newValue !== undefined) {
        diffs.push({
          property: prop,
          type: 'added',
          oldValue: undefined,
          newValue,
          propertyType: typeof newValue
        })
      } else if (oldValue !== undefined && newValue === undefined) {
        diffs.push({
          property: prop,
          type: 'deleted',
          oldValue,
          newValue: undefined,
          propertyType: typeof oldValue
        })
      } else if (JSON.stringify(oldValue) !== JSON.stringify(newValue)) {
        diffs.push({
          property: prop,
          type: 'modified',
          oldValue,
          newValue,
          propertyType: typeof newValue
        })
      }
    })
    
    // 比较页面样式
    const oldStyles = oldConfig.styles || {}
    const newStyles = newConfig.styles || {}
    
    Object.keys(newStyles).forEach(key => {
      if (!(key in oldStyles)) {
        diffs.push({
          property: `styles.${key}`,
          type: 'added',
          oldValue: undefined,
          newValue: newStyles[key],
          propertyType: typeof newStyles[key],
          isStyle: true
        })
      } else if (JSON.stringify(oldStyles[key]) !== JSON.stringify(newStyles[key])) {
        diffs.push({
          property: `styles.${key}`,
          type: 'modified',
          oldValue: oldStyles[key],
          newValue: newStyles[key],
          propertyType: typeof newStyles[key],
          isStyle: true
        })
      }
    })
    
    Object.keys(oldStyles).forEach(key => {
      if (!(key in newStyles)) {
        diffs.push({
          property: `styles.${key}`,
          type: 'deleted',
          oldValue: oldStyles[key],
          newValue: undefined,
          propertyType: typeof oldStyles[key],
          isStyle: true
        })
      }
    })
    
    return diffs
  },
  
  // 比较组件数组
  compareComponents(
    oldComponents: ComponentConfig[],
    newComponents: ComponentConfig[],
    stats: AdvancedVersionDiff['stats']
  ): ComponentDiff[] {
    const componentDiffs: ComponentDiff[] = []
    
    // 创建组件映射
    const oldComponentMap = new Map(oldComponents.map(comp => [comp.id, comp]))
    const newComponentMap = new Map(newComponents.map(comp => [comp.id, comp]))
    
    // 检查新增和修改的组件
    newComponents.forEach((newComp, newIndex) => {
      if (!oldComponentMap.has(newComp.id)) {
        // 新增组件
        const diff: ComponentDiff = {
          id: newComp.id,
          name: newComp.name || newComp.type,
          type: 'added',
          componentType: newComp.type,
          position: { new: newIndex },
          propertyDiffs: [],
          styleDiffs: []
        }
        
        // 检查是否有子组件
        if (newComp.children && newComp.children.length > 0) {
          diff.childrenDiffs = this.compareComponents([], newComp.children, stats)
        }
        
        // 检查事件配置
        if (newComp.events && newComp.events.length > 0) {
          diff.eventsDiff = {
            added: newComp.events.length,
            modified: 0,
            deleted: 0,
            changes: newComp.events.map((event, index) => ({
              eventType: event.type,
              actionIndex: index,
              type: 'added',
              actionChange: {
                type: event.actions?.[0]?.type,
                config: event.actions?.[0]?.config
              }
            }))
          }
        }
        
        componentDiffs.push(diff)
        stats.addedComponents++
      } else {
        // 检查修改的组件
        const oldComp = oldComponentMap.get(newComp.id)!
        const oldIndex = oldComponents.findIndex(c => c.id === newComp.id)
        
        const propertyDiffs = this.compareComponentProperties(oldComp, newComp)
        const styleDiffs = this.compareComponentStyles(oldComp, newComp)
        const eventsDiff = this.compareComponentEvents(oldComp, newComp)
        
        // 检查子组件
        let childrenDiffs: ComponentDiff[] = []
        if (oldComp.children || newComp.children) {
          childrenDiffs = this.compareComponents(
            oldComp.children || [],
            newComp.children || [],
            stats
          )
        }
        
        // 如果有差异，创建组件差异对象
        if (propertyDiffs.length > 0 || styleDiffs.length > 0 || 
            (eventsDiff && (eventsDiff.added > 0 || eventsDiff.modified > 0 || eventsDiff.deleted > 0)) ||
            childrenDiffs.length > 0 ||
            oldIndex !== newIndex) {
          
          const diff: ComponentDiff = {
            id: newComp.id,
            name: newComp.name || newComp.type,
            type: oldIndex !== newIndex ? 'moved' : 'modified',
            componentType: newComp.type,
            position: { old: oldIndex, new: newIndex },
            propertyDiffs,
            styleDiffs
          }
          
          if (eventsDiff) diff.eventsDiff = eventsDiff
          if (childrenDiffs.length > 0) diff.childrenDiffs = childrenDiffs
          
          componentDiffs.push(diff)
          stats.modifiedComponents++
          
          // 更新统计
          propertyDiffs.forEach(pdiff => {
            if (pdiff.type === 'added') stats.addedProperties++
            else if (pdiff.type === 'modified') stats.modifiedProperties++
            else if (pdiff.type === 'deleted') stats.deletedProperties++
          })
          
          styleDiffs.forEach(sdiff => {
            if (sdiff.type === 'added') stats.addedStyles++
            else if (sdiff.type === 'modified') stats.modifiedStyles++
            else if (sdiff.type === 'deleted') stats.deletedStyles++
          })
        }
        
        oldComponentMap.delete(newComp.id)
      }
    })
    
    // 检查删除的组件
    oldComponentMap.forEach((oldComp, oldIndex) => {
      const diff: ComponentDiff = {
        id: oldComp.id,
        name: oldComp.name || oldComp.type,
        type: 'deleted',
        componentType: oldComp.type,
        position: { old: oldIndex },
        propertyDiffs: [],
        styleDiffs: []
      }
      
      // 记录删除的事件
      if (oldComp.events && oldComp.events.length > 0) {
        diff.eventsDiff = {
          added: 0,
          modified: 0,
          deleted: oldComp.events.length,
          changes: oldComp.events.map((event, index) => ({
            eventType: event.type,
            actionIndex: index,
            type: 'deleted',
            actionChange: {
              type: event.actions?.[0]?.type,
              config: event.actions?.[0]?.config
            }
          }))
        }
      }
      
      componentDiffs.push(diff)
      stats.deletedComponents++
    })
    
    return componentDiffs
  },
  
  // 比较组件属性
  compareComponentProperties(oldComp: ComponentConfig, newComp: ComponentConfig): PropertyDiff[] {
    const diffs: PropertyDiff[] = []
    
    // 比较基本属性
    const compareProps = ['name', 'type', 'dataBindings', 'props', 'modelValue', 'vModel']
    
    compareProps.forEach(prop => {
      const oldValue = (oldComp as any)[prop]
      const newValue = (newComp as any)[prop]
      
      if (oldValue === undefined && newValue !== undefined) {
        diffs.push({
          property: prop,
          type: 'added',
          oldValue: undefined,
          newValue,
          propertyType: typeof newValue
        })
      } else if (oldValue !== undefined && newValue === undefined) {
        diffs.push({
          property: prop,
          type: 'deleted',
          oldValue,
          newValue: undefined,
          propertyType: typeof oldValue
        })
      } else if (JSON.stringify(oldValue) !== JSON.stringify(newValue)) {
        diffs.push({
          property: prop,
          type: 'modified',
          oldValue,
          newValue,
          propertyType: typeof newValue
        })
      }
    })
    
    return diffs
  },
  
  // 比较组件样式
  compareComponentStyles(oldComp: ComponentConfig, newComp: ComponentConfig): PropertyDiff[] {
    const diffs: PropertyDiff[] = []
    
    const oldStyles = oldComp.style || {}
    const newStyles = newComp.style || {}
    
    // 检查新增和修改的样式
    Object.keys(newStyles).forEach(key => {
      if (!(key in oldStyles)) {
        diffs.push({
          property: key,
          type: 'added',
          oldValue: undefined,
          newValue: newStyles[key],
          propertyType: typeof newStyles[key],
          isStyle: true
        })
      } else if (JSON.stringify(oldStyles[key]) !== JSON.stringify(newStyles[key])) {
        diffs.push({
          property: key,
          type: 'modified',
          oldValue: oldStyles[key],
          newValue: newStyles[key],
          propertyType: typeof newStyles[key],
          isStyle: true
        })
      }
    })
    
    // 检查删除的样式
    Object.keys(oldStyles).forEach(key => {
      if (!(key in newStyles)) {
        diffs.push({
          property: key,
          type: 'deleted',
          oldValue: oldStyles[key],
          newValue: undefined,
          propertyType: typeof oldStyles[key],
          isStyle: true
        })
      }
    })
    
    return diffs
  },
  
  // 比较组件事件
  compareComponentEvents(oldComp: ComponentConfig, newComp: ComponentConfig): EventsDiff | null {
    const oldEvents = oldComp.events || []
    const newEvents = newComp.events || []
    
    if (oldEvents.length === 0 && newEvents.length === 0) {
      return null
    }
    
    const eventsDiff: EventsDiff = {
      added: 0,
      modified: 0,
      deleted: 0,
      changes: []
    }
    
    // 创建事件映射
    const oldEventMap = new Map(oldEvents.map((e, i) => [`${e.type}-${i}`, e]))
    const newEventMap = new Map(newEvents.map((e, i) => [`${e.type}-${i}`, e]))
    
    // 检查新增和修改的事件
    newEvents.forEach((newEvent, newIndex) => {
      const key = `${newEvent.type}-${newIndex}`
      
      if (!oldEventMap.has(key)) {
        eventsDiff.added++
        eventsDiff.changes.push({
          eventType: newEvent.type,
          actionIndex: newIndex,
          type: 'added',
          actionChange: {
            type: newEvent.actions?.[0]?.type,
            config: newEvent.actions?.[0]?.config
          }
        })
      } else {
        const oldEvent = oldEventMap.get(key)!
        
        if (JSON.stringify(oldEvent.actions) !== JSON.stringify(newEvent.actions)) {
          eventsDiff.modified++
          eventsDiff.changes.push({
            eventType: newEvent.type,
            actionIndex: newIndex,
            type: 'modified',
            actionChange: {
              type: newEvent.actions?.[0]?.type,
              config: newEvent.actions?.[0]?.config
            }
          })
        }
        
        oldEventMap.delete(key)
      }
    })
    
    // 检查删除的事件
    oldEventMap.forEach((oldEvent, key) => {
      const parts = key.split('-')
      const index = parseInt(parts[1])
      
      eventsDiff.deleted++
      eventsDiff.changes.push({
        eventType: oldEvent.type,
        actionIndex: index,
        type: 'deleted',
        actionChange: {
          type: oldEvent.actions?.[0]?.type,
          config: oldEvent.actions?.[0]?.config
        }
      })
    })
    
    return eventsDiff
  },
  
  // 生成变更时间线
  generateChangeTimeline(
    pageId: string,
    startVersion: any,
    endVersion: any
  ): ChangeTimelineItem[] {
    const timeline: ChangeTimelineItem[] = []
    const versions = versionControlService.getVersions(pageId)
    
    // 按时间戳排序
    versions.sort((a, b) => a.timestamp - b.timestamp)
    
    // 找到起始和结束版本的索引
    const startIndex = versions.findIndex(v => v.id === startVersion.id)
    const endIndex = versions.findIndex(v => v.id === endVersion.id)
    
    // 确保索引有效
    if (startIndex === -1 || endIndex === -1) {
      return timeline
    }
    
    // 生成时间线
    const start = Math.min(startIndex, endIndex)
    const end = Math.max(startIndex, endIndex)
    
    for (let i = start; i <= end; i++) {
      const version = versions[i]
      const changeCount = version.changeSummary?.length || 0
      
      timeline.push({
        timestamp: version.timestamp,
        userName: version.userName,
        versionId: version.id,
        versionName: version.name,
        changeCount,
        isMajorChange: changeCount > 10 || version.tags?.includes('major') || false
      })
    }
    
    return timeline
  },
  
  // 生成变更路径
  generateChangePaths(diff: Omit<AdvancedVersionDiff, 'changeTimeline' | 'changePaths'>): string[] {
    const paths: string[] = []
    
    // 生成组件路径
    diff.componentDiffs.forEach(compDiff => {
      const basePath = `components.${compDiff.id}`
      paths.push(basePath)
      
      // 添加属性路径
      compDiff.propertyDiffs.forEach(propDiff => {
        paths.push(`${basePath}.${propDiff.property}`)
      })
      
      // 添加样式路径
      compDiff.styleDiffs.forEach(styleDiff => {
        paths.push(`${basePath}.style.${styleDiff.property}`)
      })
      
      // 递归添加子组件路径
      if (compDiff.childrenDiffs) {
        this.addChildPaths(compDiff.childrenDiffs, basePath, paths)
      }
    })
    
    // 添加页面属性路径
    diff.pageDiffs.forEach(pageDiff => {
      paths.push(pageDiff.property)
    })
    
    return paths
  },
  
  // 递归添加子组件路径
  addChildPaths(childDiffs: ComponentDiff[], basePath: string, paths: string[]): void {
    childDiffs.forEach(childDiff => {
      const childPath = `${basePath}.children.${childDiff.id}`
      paths.push(childPath)
      
      // 添加子组件的属性路径
      childDiff.propertyDiffs.forEach(propDiff => {
        paths.push(`${childPath}.${propDiff.property}`)
      })
      
      // 添加子组件的样式路径
      childDiff.styleDiffs.forEach(styleDiff => {
        paths.push(`${childPath}.style.${styleDiff.property}`)
      })
      
      // 递归处理嵌套子组件
      if (childDiff.childrenDiffs) {
        this.addChildPaths(childDiff.childrenDiffs, childPath, paths)
      }
    })
  },
  
  // 过滤差异结果
  filterDiffResults(
    diff: AdvancedVersionDiff,
    filters: Partial<VersionVisualizationOptions>
  ): AdvancedVersionDiff {
    const options = { ...this.visualizationOptions, ...filters }
    
    // 过滤组件差异
    const filteredComponentDiffs = diff.componentDiffs.map(compDiff => {
      const filteredComp: ComponentDiff = {
        ...compDiff,
        propertyDiffs: options.showPropertyDetails ? compDiff.propertyDiffs : [],
        styleDiffs: options.showStyleDetails ? compDiff.styleDiffs : [],
        eventsDiff: options.showEventsDetails ? compDiff.eventsDiff : undefined,
        childrenDiffs: undefined
      }
      
      // 处理子组件（如果需要且深度允许）
      if (options.showChildrenDetails && compDiff.childrenDiffs) {
        // 这里简化处理，实际应该根据maxDepth递归过滤
        filteredComp.childrenDiffs = compDiff.childrenDiffs
      }
      
      return filteredComp
    })
    
    // 过滤页面差异
    const filteredPageDiffs = diff.pageDiffs.filter(pageDiff => {
      if (!options.filterChanges.length) return true
      return options.filterChanges.includes(pageDiff.type)
    })
    
    return {
      ...diff,
      componentDiffs: filteredComponentDiffs,
      pageDiffs: filteredPageDiffs
    }
  },
  
  // 导出差异报告
  exportDiffReport(
    pageId: string,
    oldVersionId: string,
    newVersionId: string,
    diff: AdvancedVersionDiff,
    format: 'json' | 'html' | 'text' = 'json'
  ): string {
    const oldVersion = versionControlService.getVersion(pageId, oldVersionId)
    const newVersion = versionControlService.getVersion(pageId, newVersionId)
    
    const reportData = {
      title: `版本差异报告 - ${oldVersion?.name} 到 ${newVersion?.name}`,
      generatedAt: new Date().toISOString(),
      pageId,
      versions: {
        old: {
          id: oldVersionId,
          name: oldVersion?.name,
          timestamp: oldVersion?.timestamp
        },
        new: {
          id: newVersionId,
          name: newVersion?.name,
          timestamp: newVersion?.timestamp
        }
      },
      stats: diff.stats,
      summary: {
        totalChanges: diff.stats.addedComponents + diff.stats.modifiedComponents + diff.stats.deletedComponents,
        majorChanges: diff.changeTimeline.filter(item => item.isMajorChange).length
      }
    }
    
    switch (format) {
      case 'json':
        return JSON.stringify({
          ...reportData,
          detailedDiffs: diff
        }, null, 2)
        
      case 'html':
        return this.generateHtmlReport(reportData, diff)
        
      case 'text':
      default:
        return this.generateTextReport(reportData, diff)
    }
  },
  
  // 生成HTML报告
  generateHtmlReport(reportData: any, diff: AdvancedVersionDiff): string {
    return `<!DOCTYPE html>
<html>
<head>
  <title>${reportData.title}</title>
  <style>
    body { font-family: Arial, sans-serif; margin: 20px; }
    h1 { color: #333; }
    .summary { background: #f5f5f5; padding: 15px; border-radius: 5px; margin-bottom: 20px; }
    .stats { display: flex; gap: 20px; flex-wrap: wrap; }
    .stat-item { background: white; padding: 10px; border-radius: 4px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }
    .added { color: green; }
    .modified { color: orange; }
    .deleted { color: red; }
    .component-list { margin-top: 20px; }
    .component { border: 1px solid #ddd; margin: 10px 0; padding: 10px; border-radius: 4px; }
  </style>
</head>
<body>
  <h1>${reportData.title}</h1>
  <div class="summary">
    <p>生成时间: ${new Date(reportData.generatedAt).toLocaleString()}</p>
    <div class="stats">
      <div class="stat-item added">新增组件: ${diff.stats.addedComponents}</div>
      <div class="stat-item modified">修改组件: ${diff.stats.modifiedComponents}</div>
      <div class="stat-item deleted">删除组件: ${diff.stats.deletedComponents}</div>
      <div class="stat-item modified">属性修改: ${diff.stats.modifiedProperties}</div>
      <div class="stat-item modified">样式修改: ${diff.stats.modifiedStyles}</div>
    </div>
  </div>
  <div class="component-list">
    <h2>组件变更明细</h2>
    <!-- 实际应用中会生成详细的组件变更列表 -->
    <p>总共 ${diff.componentDiffs.length} 个组件发生变更</p>
  </div>
</body>
</html>`
  },
  
  // 生成文本报告
  generateTextReport(reportData: any, diff: AdvancedVersionDiff): string {
    return `=== ${reportData.title} ===
生成时间: ${new Date(reportData.generatedAt).toLocaleString()}
页面ID: ${reportData.pageId}

--- 版本信息 ---
旧版本: ${reportData.versions.old.name} (${reportData.versions.old.id})
新版本: ${reportData.versions.new.name} (${reportData.versions.new.id})

--- 变更统计 ---
新增组件: ${diff.stats.addedComponents}
修改组件: ${diff.stats.modifiedComponents}
删除组件: ${diff.stats.deletedComponents}

新增属性: ${diff.stats.addedProperties}
修改属性: ${diff.stats.modifiedProperties}
删除属性: ${diff.stats.deletedProperties}

新增样式: ${diff.stats.addedStyles}
修改样式: ${diff.stats.modifiedStyles}
删除样式: ${diff.stats.deletedStyles}

--- 变更摘要 ---
总共 ${diff.componentDiffs.length} 个组件发生变更
总共 ${diff.changeTimeline.length} 个版本变更
`
  },
  
  // 应用可视化选项
  updateVisualizationOptions(options: Partial<VersionVisualizationOptions>): void {
    Object.assign(this.visualizationOptions, options)
  },
  
  // 重置可视化选项
  resetVisualizationOptions(): void {
    Object.assign(this.visualizationOptions, defaultVisualizationOptions)
  }
}

// 组合式API
export const useAdvancedVersionControl = () => {
  const isComparing = ref(false)
  const currentDiff = ref<AdvancedVersionDiff | null>(null)
  const comparisonError = ref<string | null>(null)
  
  // 执行版本比较
  const compareVersions = async (
    pageId: string,
    oldVersionId: string,
    newVersionId: string
  ) => {
    try {
      isComparing.value = true
      comparisonError.value = null
      
      const diff = await advancedVersionControlService.compareVersionsAdvanced(
        pageId,
        oldVersionId,
        newVersionId
      )
      
      currentDiff.value = diff
      return diff
    } catch (error) {
      comparisonError.value = error instanceof Error ? error.message : '比较失败'
      throw error
    } finally {
      isComparing.value = false
    }
  }
  
  // 导出差异报告
  const exportReport = (format: 'json' | 'html' | 'text' = 'json') => {
    if (!currentDiff.value || !currentDiff.value.changeTimeline.length) {
      throw new Error('没有可导出的比较结果')
    }
    
    const pageId = currentDiff.value.changeTimeline[0].versionId.split('_')[0] || ''
    const oldVersionId = currentDiff.value.changeTimeline[0].versionId
    const newVersionId = currentDiff.value.changeTimeline[currentDiff.value.changeTimeline.length - 1].versionId
    
    return advancedVersionControlService.exportDiffReport(
      pageId,
      oldVersionId,
      newVersionId,
      currentDiff.value,
      format
    )
  }
  
  return {
    // 状态
    isComparing,
    currentDiff,
    comparisonError,
    visualizationOptions: advancedVersionControlService.visualizationOptions,
    
    // 方法
    compareVersions,
    filterDiffResults: advancedVersionControlService.filterDiffResults.bind(advancedVersionControlService),
    exportReport,
    updateVisualizationOptions: advancedVersionControlService.updateVisualizationOptions.bind(advancedVersionControlService),
    resetVisualizationOptions: advancedVersionControlService.resetVisualizationOptions.bind(advancedVersionControlService)
  }
}