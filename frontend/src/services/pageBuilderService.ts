import type { PageMetadata, ComponentConfig, ComponentProps, ComponentEvent } from '../types/page';
import type { VNode } from 'vue';
import { h, reactive, computed, ref, watch } from 'vue';
import { ElMessage } from 'element-plus';
import { api } from './api';

/**
 * 组件注册中心
 */
const componentRegistry: Record<string, any> = {};

/**
 * 页面构建器服务
 */
export class PageBuilderService {
  /**
   * 注册组件
   */
  static registerComponent(name: string, component: any): void {
    componentRegistry[name] = component;
  }

  /**
   * 注册多个组件
   */
  static registerComponents(components: Record<string, any>): void {
    Object.assign(componentRegistry, components);
  }

  /**
   * 获取组件
   */
  static getComponent(name: string): any {
    return componentRegistry[name];
  }

  /**
   * 获取所有注册的组件
   */
  static getAllComponents(): Record<string, any> {
    return { ...componentRegistry };
  }

  /**
   * 解析JavaScript表达式
   */
  static parseExpression(expression: string, context: any): any {
    try {
      // 创建一个安全的函数执行环境
      const keys = Object.keys(context);
      const values = Object.values(context);
      const fn = new Function(...keys, `return ${expression};`);
      return fn(...values);
    } catch (error) {
      console.error('表达式解析错误:', error, expression);
      return undefined;
    }
  }

  /**
   * 执行JavaScript函数
   */
  static executeFunction(funcBody: string, context: any, params: any[] = []): any {
    try {
      // 创建一个安全的函数执行环境
      const keys = Object.keys(context);
      const values = Object.values(context);
      const fn = new Function(...keys, `return function(${params.join(',')}) { ${funcBody} };`);
      const func = fn(...values);
      return func(...params);
    } catch (error) {
      console.error('函数执行错误:', error, funcBody);
      return undefined;
    }
  }

  /**
   * 构建组件树
   */
  static buildComponent(componentConfig: ComponentConfig, context: any): VNode {
    const { type, props = {}, events = [], children = [], vIf, vFor, crudConfig, formConfig, tableConfig } = componentConfig;
    
    // 获取组件 - 支持大小写不敏感的查找
    let component = this.getComponent(type) || this.getComponent(type.replace(/-([a-z])/g, g => g[1].toUpperCase()));
    
    // 如果未找到，尝试大小写不敏感的查找
    if (!component) {
      const allComponents = this.getAllComponents();
      const lowerType = type.toLowerCase();
      
      for (const [key, comp] of Object.entries(allComponents)) {
        if (key.toLowerCase() === lowerType) {
          component = comp;
          break;
        }
      }
    }
    
    if (!component) {
      console.error(`组件 ${type} 未注册`);
      return h('div', { style: { color: 'red' } }, `[未注册组件: ${type}]`);
    }

    // 构建props
    const mergedProps = this.buildProps(props, context);
    
    // 添加特殊配置
    if (crudConfig) mergedProps.config = crudConfig;
    if (formConfig) mergedProps.config = formConfig;
    if (tableConfig) mergedProps.config = tableConfig;

    // 构建事件
    const eventHandlers = this.buildEvents(events, context);

    // 构建子组件
    const childNodes = this.buildChildren(children, context);

    // 处理条件渲染
    if (vIf) {
      const condition = this.parseExpression(vIf, context);
      if (!condition) {
        return h('template', null);
      }
    }

    // 处理列表渲染
    if (vFor) {
      const { item, index = 'index', of } = vFor;
      const items = this.parseExpression(of, context) || [];
      
      return h('template', null, items.map((value: any, i: number) => {
        const itemContext = { ...context, [item]: value, [index]: i };
        return this.buildComponent(componentConfig, itemContext);
      }));
    }

    return h(component, { ...mergedProps, ...eventHandlers }, childNodes);
  }

  /**
   * 构建组件属性
   */
  private static buildProps(props: ComponentProps, context: any): Record<string, any> {
    const result: Record<string, any> = {};
    
    Object.entries(props).forEach(([key, value]) => {
      // 处理表达式
      if (typeof value === 'string' && value.startsWith('{{') && value.endsWith('}}')) {
        const expression = value.slice(2, -2).trim();
        result[key] = this.parseExpression(expression, context);
      } 
      // 处理函数表达式
      else if (typeof value === 'string' && value.startsWith('function(')) {
        try {
          result[key] = new Function(`return ${value}`)();
        } catch (error) {
          console.error('函数属性解析错误:', error, value);
          result[key] = value;
        }
      }
      // 处理对象类型
      else if (typeof value === 'object' && value !== null) {
        result[key] = this.buildProps(value as ComponentProps, context);
      }
      // 直接值
      else {
        result[key] = value;
      }
    });
    
    // 处理visible和disabled表达式
    if (typeof result.visible === 'string') {
      result.visible = this.parseExpression(result.visible, context);
    }
    if (typeof result.disabled === 'string') {
      result.disabled = this.parseExpression(result.disabled, context);
    }
    
    return result;
  }

  /**
   * 构建组件事件
   */
  private static buildEvents(events: ComponentEvent[], context: any): Record<string, Function> {
    const result: Record<string, Function> = {};
    
    events.forEach(event => {
      result[event.name] = (...args: any[]) => {
        try {
          // 准备事件处理的上下文
          const eventContext = { ...context, $event: args[0], arguments: args };
          return this.executeFunction(event.body, eventContext, event.params || []);
        } catch (error) {
          console.error(`事件处理错误 [${event.name}]:`, error);
          ElMessage.error('事件处理出错');
        }
      };
    });
    
    return result;
  }

  /**
   * 构建子组件
   */
  private static buildChildren(children: ComponentConfig[], context: any): VNode[] {
    return children.map(child => this.buildComponent(child, context));
  }

  /**
   * 创建页面上下文
   */
  static createPageContext(metadata: PageMetadata): any {
    const context: any = {
      // Vue响应式API
      reactive,
      ref,
      computed,
      watch,
      
      // Element Plus 消息提示
      ElMessage,
      
      // API服务
      api
    };

    // 创建页面状态
    if (metadata.state) {
      Object.entries(metadata.state).forEach(([key, stateConfig]) => {
        const initialValue = stateConfig.persist ? 
          this.getPersistedState(stateConfig.persistKey || key, stateConfig.initialValue) : 
          stateConfig.initialValue;
        
        context[key] = ref(initialValue);
        
        // 持久化状态
        if (stateConfig.persist) {
          watch(context[key], (newValue: any) => {
            this.persistState(stateConfig.persistKey || key, newValue);
          }, { deep: true });
        }
      });
    }

    // 创建页面方法
    if (metadata.methods) {
      metadata.methods.forEach(method => {
        context[method.name] = (...args: any[]) => {
          return this.executeFunction(method.body, context, args);
        };
      });
    }

    // 执行页面生命周期
    if (metadata.lifecycle?.created) {
      this.executeFunction(metadata.lifecycle.created, context);
    }

    return context;
  }

  /**
   * 构建页面
   */
  static buildPage(metadata: PageMetadata): VNode {
    const context = this.createPageContext(metadata);
    
    // 构建组件树
    const componentTree = this.buildComponent({
      type: 'container',
      props: {
        style: {
          height: '100%',
          width: '100%'
        }
      },
      children: metadata.components
    }, context);

    // 执行挂载后生命周期
    setTimeout(() => {
      if (metadata.lifecycle?.mounted) {
        this.executeFunction(metadata.lifecycle.mounted, context);
      }
    }, 0);

    return componentTree;
  }

  /**
   * 持久化状态到localStorage
   */
  private static persistState(key: string, value: any): void {
    try {
      localStorage.setItem(`page_state_${key}`, JSON.stringify(value));
    } catch (error) {
      console.error('状态持久化失败:', error);
    }
  }

  /**
   * 从localStorage获取持久化状态
   */
  private static getPersistedState(key: string, defaultValue: any): any {
    try {
      const stored = localStorage.getItem(`page_state_${key}`);
      return stored ? JSON.parse(stored) : defaultValue;
    } catch (error) {
      console.error('获取持久化状态失败:', error);
      return defaultValue;
    }
  }

  /**
   * 验证页面元数据
   */
  static validateMetadata(metadata: PageMetadata): { valid: boolean; errors: string[] } {
    const errors: string[] = [];

    // 验证基本字段
    if (!metadata.id) errors.push('页面ID不能为空');
    if (!metadata.name) errors.push('页面名称不能为空');
    if (!metadata.route?.path) errors.push('页面路由路径不能为空');
    
    // 验证组件配置
    const validateComponent = (component: ComponentConfig, path: string = 'root'): void => {
      if (!component.type) {
        errors.push(`组件类型不能为空: ${path}`);
        return;
      }

      // 验证组件是否已注册
      const registeredComponent = this.getComponent(component.type) || 
                                this.getComponent(component.type.replace(/-([a-z])/g, g => g[1].toUpperCase()));
      if (!registeredComponent) {
        errors.push(`未注册的组件: ${component.type} (路径: ${path})`);
      }

      // 递归验证子组件
      if (component.children) {
        component.children.forEach((child, index) => {
          validateComponent(child, `${path}.children[${index}]`);
        });
      }
    };

    metadata.components.forEach((component, index) => {
      validateComponent(component, `components[${index}]`);
    });

    return { valid: errors.length === 0, errors };
  }

  /**
   * 生成默认页面元数据
   */
  static generateDefaultMetadata(pageId: string, pageName: string, path: string): PageMetadata {
    return {
      id: pageId,
      name: pageName,
      description: `${pageName} 页面`,
      version: '1.0.0',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      layout: {
        type: 'single-column'
      },
      route: {
        path,
        name: pageId,
        meta: {
          title: pageName,
          showInMenu: true
        }
      },
      components: [
        {
          type: 'card',
          props: {
            title: pageName,
            style: {
              margin: '20px'
            }
          },
          children: [
            {
              type: 'text',
              props: {
                text: '页面内容区域'
              }
            }
          ]
        }
      ]
    };
  }
}