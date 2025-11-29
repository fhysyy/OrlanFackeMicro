import { logger } from '@/utils/logger'

/**
 * 基础组件注册服务
 * 用于全局注册常用组件
 */

/**
 * 注册基础组件
 * 包括全局通用组件
 */
export function registerBaseComponents(): void {
  try {
    logger.log('开始注册基础组件...')
    
    // 这里将注册通用的全局组件
    // 由于我们使用了 unplugin-vue-components 自动导入 Element Plus 组件
    // 这里主要注册项目自定义的全局组件
    
    logger.log('基础组件注册完成')
  } catch (error) {
    logger.error('注册基础组件失败:', error)
    throw new Error('组件注册失败')
  }
}

/**
 * 动态注册组件
 * @param componentName 组件名称
 * @param component 组件定义
 */
export function registerComponent(componentName: string, component: any): boolean {
  try {
    logger.log(`注册自定义组件: ${componentName}`)
    
    // 在实际应用中，这里应该向Vue实例注册组件
    // 由于我们使用的是Composition API，这个功能会在需要时实现
    
    return true
  } catch (error) {
    logger.error(`注册组件 ${componentName} 失败:`, error)
    return false
  }
}

/**
 * 获取已注册的组件列表
 */
export function getRegisteredComponents(): string[] {
  // 返回空列表，因为现在使用自动导入
  return []
}

/**
 * 检查组件是否已注册
 * @param componentName 组件名称
 */
export function isComponentRegistered(componentName: string): boolean {
  const registeredComponents = getRegisteredComponents()
  return registeredComponents.includes(componentName)
}
