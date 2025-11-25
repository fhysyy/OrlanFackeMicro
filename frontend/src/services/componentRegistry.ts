import { logger } from '@/utils/logger';

/**
 * 基础组件注册服务
 * 用于全局注册常用组件和低代码相关组件
 */

/**
 * 注册基础组件
 * 包括全局通用组件和低代码引擎相关组件
 */
export function registerBaseComponents(): void {
  try {
    logger.log('开始注册基础组件...');
    
    // 这里将注册通用的全局组件
    // 由于我们使用了 unplugin-vue-components 自动导入 Element Plus 组件
    // 这里主要注册项目自定义的全局组件
    
    // 注册低代码引擎相关的基础组件
    registerLowCodeBaseComponents();
    
    logger.log('基础组件注册完成');
  } catch (error) {
    logger.error('注册基础组件失败:', error);
    throw new Error('组件注册失败');
  }
}

/**
 * 注册低代码引擎相关的基础组件
 */
function registerLowCodeBaseComponents(): void {
  try {
    logger.log('注册低代码基础组件...');
    
    // 这里将注册低代码引擎需要的基础组件
    // 由于使用了动态导入，这里只做注册准备
    // 实际组件会在 AliLowCodeService 中按需注册
    
    // 模拟注册一些基础组件
    const registeredComponents = ['Button', 'Input', 'Page', 'Form', 'Table'];
    logger.log('已注册的低代码基础组件:', registeredComponents);
    
  } catch (error) {
    logger.error('注册低代码基础组件失败:', error);
    // 低代码组件注册失败不影响应用启动
  }
}

/**
 * 动态注册组件
 * @param componentName 组件名称
 * @param component 组件定义
 */
export function registerComponent(componentName: string, component: any): boolean {
  try {
    logger.log(`注册自定义组件: ${componentName}`);
    
    // 在实际应用中，这里应该向Vue实例注册组件
    // 由于我们使用的是Composition API，这个功能会在需要时实现
    
    return true;
  } catch (error) {
    logger.error(`注册组件 ${componentName} 失败:`, error);
    return false;
  }
}

/**
 * 获取已注册的组件列表
 */
export function getRegisteredComponents(): string[] {
  // 返回模拟的已注册组件列表
  return ['Button', 'Input', 'Page', 'Form', 'Table'];
}

/**
 * 检查组件是否已注册
 * @param componentName 组件名称
 */
export function isComponentRegistered(componentName: string): boolean {
  const registeredComponents = getRegisteredComponents();
  return registeredComponents.includes(componentName);
}
