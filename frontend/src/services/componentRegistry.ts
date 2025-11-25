// 组件注册器，用于管理和注册低代码平台可用的组件

import { ref, provide, inject } from 'vue';
import type { ComponentConfig, ComponentType } from '../types/page';

// 组件定义接口
export interface ComponentDefinition {
  type: string;
  name: string;
  description?: string;
  component?: any;
  props?: Array<{
    name: string;
    type: string;
    default?: any;
    description?: string;
    required?: boolean;
    options?: Array<{ label: string; value: any }>;
  }>;
  events?: Array<{
    name: string;
    description?: string;
    params?: Array<{
      name: string;
      type: string;
      description?: string;
    }>;
  }>;
  slots?: Array<{
    name: string;
    description?: string;
    default?: boolean;
  }>;
  category?: string;
  icon?: string;
  version?: string;
  author?: string;
  disabled?: boolean;
  previewable?: boolean;
  schema?: Record<string, any>;
  usageCount?: number; // 使用次数统计
  deprecated?: boolean; // 是否废弃
  deprecatedMessage?: string; // 废弃提示信息
  defaultProps?: Record<string, any>; // 默认属性值
}

// 组件注册中心接口
export interface ComponentRegistry {
  // 核心功能
  registerComponent(component: ComponentDefinition): void;
  registerComponents(components: ComponentDefinition[]): void;
  unregisterComponent(type: string): void;
  getComponent(type: string): ComponentDefinition | undefined;
  getAllComponents(): ComponentDefinition[];
  getAvailableComponents(): ComponentDefinition[];
  
  // 分类和查询
  getComponentsByCategory(category: string): ComponentDefinition[];
  searchComponents(query: string): ComponentDefinition[];
  getComponentCategories(): Array<{ key: string; name: string }>;
  
  // 组件状态管理
  enableComponent(type: string): void;
  disableComponent(type: string): void;
  markComponentDeprecated(type: string, message?: string): void;
  
  // 工具方法
  hasComponent(type: string): boolean;
  getComponentSchema(type: string): Record<string, any> | null;
  getComponentDocumentationUrl(type: string): string | null;
  validateComponentProps(type: string, props: Record<string, any>): { valid: boolean; errors: string[] };
  
  // 统计和管理
  incrementComponentUsage(type: string): void;
  getComponentUsageStats(): Record<string, { count: number; lastUsed?: Date }>;
  exportComponentConfig(type: string): Record<string, any> | null;
  importComponentConfig(config: Record<string, any>): boolean;
  
  // 批量操作
  exportAllComponents(): Record<string, any>;
  importComponentsFromConfig(config: Record<string, any>): boolean;
}

// 组件类别定义
const DEFAULT_CATEGORIES: Record<string, string> = {
  'basic': '基础组件',
  'layout': '布局组件',
  'form': '表单组件',
  'data': '数据展示',
  'feedback': '反馈组件',
  'navigation': '导航组件',
  'business': '业务组件',
  'other': '其他组件'
};

// 组件注册中心实现
class ComponentRegistryImpl implements ComponentRegistry {
  private components: Record<string, ComponentDefinition> = {};
  private categories: Record<string, string> = { ...DEFAULT_CATEGORIES };
  private usageStats: Record<string, { count: number; lastUsed?: Date }> = {};
  private registeredEvents: Record<string, Array<(component: ComponentDefinition) => void>> = {};

  // 注册单个组件
  registerComponent(component: ComponentDefinition): void {
    if (!component.type) {
      throw new Error('Component type is required');
    }
    
    // 设置默认值
    const defaultProps = this.extractDefaultProps(component.props || []);
    
    this.components[component.type] = {
      ...component,
      category: component.category || 'basic',
      icon: component.icon || 'component',
      version: component.version || '1.0.0',
      disabled: component.disabled || false,
      previewable: component.previewable !== false,
      deprecated: component.deprecated || false,
      usageCount: component.usageCount || 0,
      defaultProps: { ...defaultProps, ...component.defaultProps }
    };
    
    // 触发注册事件
    this.triggerEvent('componentRegistered', this.components[component.type]);
  }
  
  // 批量注册组件
  registerComponents(components: ComponentDefinition[]): void {
    components.forEach(component => this.registerComponent(component));
  }
  
  // 注销组件
  unregisterComponent(type: string): void {
    if (this.hasComponent(type)) {
      delete this.components[type];
      delete this.usageStats[type];
      
      // 触发注销事件
      this.triggerEvent('componentUnregistered', { type });
    }
  }
  
  // 获取单个组件
  getComponent(type: string): ComponentDefinition | undefined {
    return this.components[type];
  }
  
  // 获取所有组件
  getAllComponents(): ComponentDefinition[] {
    return Object.values(this.components);
  }
  
  // 获取可用组件（未禁用、未废弃）
  getAvailableComponents(): ComponentDefinition[] {
    return Object.values(this.components).filter(component => 
      !component.disabled && !component.deprecated
    );
  }
  
  // 按类别获取组件
  getComponentsByCategory(category: string): ComponentDefinition[] {
    if (category === 'all') {
      return this.getAllComponents();
    }
    return Object.values(this.components).filter(component => 
      component.category === category
    );
  }
  
  // 搜索组件
  searchComponents(query: string): ComponentDefinition[] {
    if (!query) {
      return this.getAllComponents();
    }
    
    const lowercaseQuery = query.toLowerCase();
    return Object.values(this.components).filter(component => 
      component.name.toLowerCase().includes(lowercaseQuery) ||
      component.type.toLowerCase().includes(lowercaseQuery) ||
      (component.description && component.description.toLowerCase().includes(lowercaseQuery)) ||
      (component.category && component.category.toLowerCase().includes(lowercaseQuery))
    );
  }
  
  // 获取组件类别列表
  getComponentCategories(): Array<{ key: string; name: string }> {
    // 确保 'all' 类别始终存在于首位
    const categories = [{ key: 'all', name: '全部' }];
    
    // 添加其他类别
    Object.entries(this.categories).forEach(([key, name]) => {
      categories.push({ key, name });
    });
    
    return categories;
  }
  
  // 添加自定义类别
  addCategory(key: string, name: string): void {
    this.categories[key] = name;
  }
  
  // 启用组件
  enableComponent(type: string): void {
    if (this.hasComponent(type)) {
      this.components[type].disabled = false;
      this.triggerEvent('componentEnabled', this.components[type]);
    }
  }
  
  // 禁用组件
  disableComponent(type: string): void {
    if (this.hasComponent(type)) {
      this.components[type].disabled = true;
      this.triggerEvent('componentDisabled', this.components[type]);
    }
  }
  
  // 标记组件为废弃
  markComponentDeprecated(type: string, message?: string): void {
    if (this.hasComponent(type)) {
      this.components[type].deprecated = true;
      if (message) {
        this.components[type].deprecatedMessage = message;
      }
      this.triggerEvent('componentDeprecated', this.components[type]);
    }
  }
  
  // 检查组件是否存在
  hasComponent(type: string): boolean {
    return !!this.components[type];
  }
  
  // 获取组件JSON Schema
  getComponentSchema(type: string): Record<string, any> | null {
    const component = this.components[type];
    if (!component) return null;
    
    // 如果已有schema，则返回
    if (component.schema) {
      return component.schema;
    }
    
    // 否则根据props生成schema
    return this.generateComponentSchema(component);
  }
  
  // 获取组件文档URL
  getComponentDocumentationUrl(type: string): string | null {
    const component = this.components[type];
    if (component && component.version) {
      return `/docs/components/${type}?v=${component.version}`;
    }
    return null;
  }
  
  // 验证组件属性
  validateComponentProps(type: string, props: Record<string, any>): { valid: boolean; errors: string[] } {
    const component = this.components[type];
    if (!component || !component.props) {
      return { valid: true, errors: [] };
    }
    
    const errors: string[] = [];
    
    // 检查必填属性
    component.props.forEach(prop => {
      if (prop.required && !(prop.name in props)) {
        errors.push(`属性 '${prop.name}' 是必填的`);
      }
      
      // 检查属性类型
      if (prop.name in props && prop.type && props[prop.name] !== undefined) {
        const propType = typeof props[prop.name];
        const expectedType = prop.type.toLowerCase();
        
        // 简单类型检查
        if (expectedType !== 'any' && expectedType !== propType) {
          // 处理一些特殊情况
          if (!(expectedType === 'number' && propType === 'string' && !isNaN(Number(props[prop.name])))) {
            errors.push(`属性 '${prop.name}' 类型错误，期望 ${expectedType}，得到 ${propType}`);
          }
        }
      }
      
      // 检查枚举值
      if (prop.options && prop.name in props && props[prop.name] !== undefined) {
        const validValues = prop.options.map(opt => opt.value);
        if (!validValues.includes(props[prop.name])) {
          errors.push(`属性 '${prop.name}' 值无效，有效值: ${validValues.join(', ')}`);
        }
      }
    });
    
    return { valid: errors.length === 0, errors };
  }
  
  // 增加组件使用统计
  incrementComponentUsage(type: string): void {
    if (!this.usageStats[type]) {
      this.usageStats[type] = { count: 0 };
    }
    
    this.usageStats[type].count++;
    this.usageStats[type].lastUsed = new Date();
    
    // 更新组件的使用次数
    if (this.hasComponent(type)) {
      this.components[type].usageCount = this.usageStats[type].count;
    }
  }
  
  // 获取组件使用统计
  getComponentUsageStats(): Record<string, { count: number; lastUsed?: Date }> {
    return { ...this.usageStats };
  }
  
  // 导出单个组件配置
  exportComponentConfig(type: string): Record<string, any> | null {
    const component = this.components[type];
    if (!component) {
      return null;
    }
    
    // 深拷贝避免引用问题
    return JSON.parse(JSON.stringify(component));
  }
  
  // 导入组件配置
  importComponentConfig(config: Record<string, any>): boolean {
    try {
      if (!config.type) {
        throw new Error('Component type is required');
      }
      
      this.registerComponent(config as ComponentDefinition);
      return true;
    } catch (error) {
      console.error('Failed to import component config:', error);
      return false;
    }
  }
  
  // 导出所有组件配置
  exportAllComponents(): Record<string, any> {
    return {
      version: '1.0.0',
      exportTime: new Date().toISOString(),
      components: this.getAllComponents(),
      categories: this.categories,
      usageStats: this.usageStats
    };
  }
  
  // 从配置导入多个组件
  importComponentsFromConfig(config: Record<string, any>): boolean {
    try {
      // 导入类别
      if (config.categories) {
        this.categories = { ...DEFAULT_CATEGORIES, ...config.categories };
      }
      
      // 导入组件
      if (Array.isArray(config.components)) {
        this.registerComponents(config.components as ComponentDefinition[]);
      }
      
      // 导入使用统计
      if (config.usageStats) {
        this.usageStats = { ...this.usageStats, ...config.usageStats };
      }
      
      return true;
    } catch (error) {
      console.error('Failed to import components from config:', error);
      return false;
    }
  }
  
  // 注册事件监听器
  on(event: string, callback: (component: ComponentDefinition) => void): void {
    if (!this.registeredEvents[event]) {
      this.registeredEvents[event] = [];
    }
    this.registeredEvents[event].push(callback);
  }
  
  // 移除事件监听器
  off(event: string, callback?: (component: ComponentDefinition) => void): void {
    if (!this.registeredEvents[event]) return;
    
    if (callback) {
      const index = this.registeredEvents[event].indexOf(callback);
      if (index > -1) {
        this.registeredEvents[event].splice(index, 1);
      }
    } else {
      delete this.registeredEvents[event];
    }
  }
  
  // 触发事件
  private triggerEvent(event: string, data: any): void {
    if (this.registeredEvents[event]) {
      this.registeredEvents[event].forEach(callback => {
        try {
          callback(data);
        } catch (error) {
          console.error(`Error in event handler for ${event}:`, error);
        }
      });
    }
  }
  
  // 从props定义中提取默认值
  private extractDefaultProps(props: Array<{ name: string; default?: any }>): Record<string, any> {
    const defaultProps: Record<string, any> = {};
    props.forEach(prop => {
      if (prop.default !== undefined) {
        defaultProps[prop.name] = prop.default;
      }
    });
    return defaultProps;
  }
  
  // 生成组件的JSON Schema
  private generateComponentSchema(component: ComponentDefinition): Record<string, any> {
    const schema: Record<string, any> = {
      type: 'object',
      properties: {},
      required: []
    };
    
    if (component.props) {
      component.props.forEach(prop => {
        const propSchema: Record<string, any> = {};
        
        // 设置类型
        switch (prop.type.toLowerCase()) {
          case 'string':
            propSchema.type = 'string';
            break;
          case 'number':
            propSchema.type = 'number';
            break;
          case 'boolean':
            propSchema.type = 'boolean';
            break;
          case 'object':
            propSchema.type = 'object';
            propSchema.properties = {};
            break;
          case 'array':
            propSchema.type = 'array';
            propSchema.items = {};
            break;
          default:
            propSchema.type = 'string';
        }
        
        // 设置默认值
        if (prop.default !== undefined) {
          propSchema.default = prop.default;
        }
        
        // 设置描述
        if (prop.description) {
          propSchema.description = prop.description;
        }
        
        // 设置枚举值
        if (prop.options && prop.options.length > 0) {
          propSchema.enum = prop.options.map(opt => opt.value);
        }
        
        // 添加到schema
        schema.properties[prop.name] = propSchema;
        
        // 添加到必填列表
        if (prop.required) {
          schema.required.push(prop.name);
        }
      });
    }
    
    return schema;
  }
}

// 创建组件注册中心实例
const registryInstance = new ComponentRegistryImpl();

// 响应式引用，用于在Vue组件中反应组件变化
export const componentsList = ref<ComponentDefinition[]>([]);

// 更新响应式组件列表
const updateComponentsList = () => {
  componentsList.value = registryInstance.getAllComponents();
};

// 监听组件注册事件，更新响应式列表
registryInstance.on('componentRegistered', updateComponentsList);
registryInstance.on('componentUnregistered', updateComponentsList);
registryInstance.on('componentEnabled', updateComponentsList);
registryInstance.on('componentDisabled', updateComponentsList);
registryInstance.on('componentDeprecated', updateComponentsList);

// 基础组件实现 - 使用Vue 3的组合式API
import { h } from 'vue';

// 容器组件实现
const ContainerComponent = {
  name: 'Container',
  props: {
    style: {
      type: Object,
      default: () => ({})
    },
    class: {
      type: [String, Array, Object],
      default: ''
    },
    tag: {
      type: String,
      default: 'div'
    }
  },
  setup(props, { slots }) {
    return () => h(props.tag, { style: props.style, class: props.class }, slots.default?.());
  }
};

// 行布局组件实现
const RowComponent = {
  name: 'Row',
  props: {
    gutter: {
      type: Number,
      default: 0
    },
    justify: {
      type: String,
      default: 'start'
    },
    align: {
      type: String,
      default: 'top'
    },
    style: {
      type: Object,
      default: () => ({})
    }
  },
  setup(props, { slots }) {
    const style: any = {
      display: 'flex',
      flexWrap: 'wrap',
      justifyContent: props.justify,
      alignItems: props.align,
      marginLeft: props.gutter ? `-${props.gutter / 2}px` : '0',
      marginRight: props.gutter ? `-${props.gutter / 2}px` : '0',
      ...props.style
    };
    
    return () => h('div', { style }, slots.default?.());
  }
};

// 列布局组件实现
const ColComponent = {
  name: 'Col',
  props: {
    span: {
      type: Number,
      default: 24
    },
    offset: {
      type: Number,
      default: 0
    },
    push: {
      type: Number,
      default: 0
    },
    pull: {
      type: Number,
      default: 0
    },
    gutter: {
      type: Number,
      default: 0
    },
    xs: {
      type: Number
    },
    sm: {
      type: Number
    },
    md: {
      type: Number
    },
    lg: {
      type: Number
    },
    xl: {
      type: Number
    },
    style: {
      type: Object,
      default: () => ({})
    }
  },
  setup(props, { slots }) {
    const style: any = {
      flex: `0 0 ${(props.span / 24) * 100}%`,
      maxWidth: `${(props.span / 24) * 100}%`,
      marginLeft: props.offset ? `${(props.offset / 24) * 100}%` : '0',
      order: props.push || 0,
      ...props.style
    };
    
    if (props.gutter) {
      style.paddingLeft = `${props.gutter / 2}px`;
      style.paddingRight = `${props.gutter / 2}px`;
    }
    
    return () => h('div', { style }, slots.default?.());
  }
};

// 文本组件实现
const TextComponent = {
  name: 'Text',
  props: {
    text: {
      type: String,
      default: ''
    },
    content: {
      type: String,
      default: ''
    },
    tag: {
      type: String,
      default: 'span'
    },
    style: {
      type: Object,
      default: () => ({})
    },
    class: {
      type: [String, Array, Object],
      default: ''
    },
    bold: {
      type: Boolean,
      default: false
    },
    italic: {
      type: Boolean,
      default: false
    },
    underline: {
      type: Boolean,
      default: false
    }
  },
  setup(props) {
    const textStyle: any = {
      fontWeight: props.bold ? 'bold' : 'normal',
      fontStyle: props.italic ? 'italic' : 'normal',
      textDecoration: props.underline ? 'underline' : 'none',
      ...props.style
    };
    
    // 优先使用content属性，兼容旧版的text属性
    const displayText = props.content || props.text;
    
    return () => h(props.tag, { style: textStyle, class: props.class }, displayText);
  }
};

// 静态文本组件实现 - 支持HTML
const StaticTextComponent = {
  name: 'StaticText',
  props: {
    content: {
      type: String,
      default: ''
    },
    style: {
      type: Object,
      default: () => ({})
    }
  },
  setup(props) {
    return () => h('div', { 
      style: props.style,
      innerHTML: props.content 
    });
  }
};

// 分隔线组件实现
const DividerComponent = {
  name: 'Divider',
  props: {
    orientation: {
      type: String,
      default: 'center'
    },
    contentPosition: {
      type: String,
      default: 'center'
    },
    style: {
      type: Object,
      default: () => ({})
    }
  },
  setup(props, { slots }) {
    const style: any = {
      borderTop: '1px solid #dcdfe6',
      marginTop: '20px',
      marginBottom: '20px',
      ...props.style
    };
    
    return () => h('div', { style }, slots.default?.());
  }
};

// 卡片组件实现
const CardComponent = {
  name: 'Card',
  props: {
    title: {
      type: String,
      default: ''
    },
    subtitle: {
      type: String,
      default: ''
    },
    loading: {
      type: Boolean,
      default: false
    },
    bordered: {
      type: Boolean,
      default: true
    },
    shadow: {
      type: String,
      default: 'hover'
    },
    style: {
      type: Object,
      default: () => ({})
    }
  },
  setup(props, { slots }) {
    const style: any = {
      border: props.bordered ? '1px solid #ebeef5' : 'none',
      borderRadius: '4px',
      backgroundColor: '#fff',
      boxShadow: props.shadow === 'always' ? '0 2px 12px 0 rgba(0, 0, 0, 0.1)' : 
                  props.shadow === 'hover' ? '0 2px 4px rgba(0, 0, 0, 0.12)' : 'none',
      padding: '20px',
      ...props.style
    };
    
    return () => h('div', { style }, [
      props.title && h('div', { style: { fontSize: '18px', fontWeight: 'bold', marginBottom: props.subtitle ? '5px' : '20px' } }, props.title),
      props.subtitle && h('div', { style: { fontSize: '14px', color: '#606266', marginBottom: '20px' } }, props.subtitle),
      slots.default?.()
    ]);
  }
};

// 统计卡片组件实现
const StatCardComponent = {
  name: 'StatCard',
  props: {
    title: {
      type: String,
      default: ''
    },
    value: {
      type: String,
      default: '0'
    },
    icon: {
      type: String,
      default: ''
    },
    color: {
      type: String,
      default: 'primary'
    },
    trend: {
      type: String,
      default: ''
    },
    trendColor: {
      type: String,
      default: 'success'
    },
    style: {
      type: Object,
      default: () => ({})
    }
  },
  setup(props) {
    const colorMap: Record<string, string> = {
      primary: '#409eff',
      success: '#67c23a',
      warning: '#e6a23c',
      danger: '#f56c6c',
      info: '#909399'
    };
    
    const style: any = {
      backgroundColor: '#fff',
      padding: '20px',
      borderRadius: '4px',
      border: `1px solid #ebeef5`,
      ...props.style
    };
    
    const iconStyle: any = {
      fontSize: '32px',
      color: colorMap[props.color] || '#409eff',
      marginBottom: '10px'
    };
    
    const valueStyle: any = {
      fontSize: '24px',
      fontWeight: 'bold',
      marginBottom: '5px'
    };
    
    const titleStyle: any = {
      fontSize: '14px',
      color: '#606266',
      marginBottom: '10px'
    };
    
    const trendStyle: any = {
      fontSize: '14px',
      color: colorMap[props.trendColor] || '#67c23a'
    };
    
    return () => h('div', { style }, [
      props.title && h('div', { style: titleStyle }, props.title),
      h('div', { style: { display: 'flex', alignItems: 'center', justifyContent: 'space-between' } }, [
        h('div', { style: valueStyle }, props.value),
        props.icon && h('i', { class: props.icon, style: iconStyle })
      ]),
      props.trend && h('div', { style: trendStyle }, props.trend)
    ]);
  }
};

// 链接组件实现
const LinkComponent = {
  name: 'Link',
  props: {
    href: {
      type: String,
      default: ''
    },
    target: {
      type: String,
      default: '_self'
    },
    text: {
      type: String,
      default: '链接'
    },
    type: {
      type: String,
      default: 'primary'
    },
    underline: {
      type: Boolean,
      default: true
    },
    disabled: {
      type: Boolean,
      default: false
    },
    style: {
      type: Object,
      default: () => ({})
    }
  },
  setup(props) {
    const colorMap: Record<string, string> = {
      primary: '#409eff',
      success: '#67c23a',
      warning: '#e6a23c',
      danger: '#f56c6c',
      info: '#909399'
    };
    
    const linkStyle: any = {
      color: props.disabled ? '#c0c4cc' : colorMap[props.type] || '#409eff',
      textDecoration: props.underline && !props.disabled ? 'underline' : 'none',
      cursor: props.disabled ? 'not-allowed' : 'pointer',
      ...props.style
    };
    
    const handleClick = (e: Event) => {
      if (props.disabled) {
        e.preventDefault();
      }
    };
    
    return () => h(
      'a',
      {
        href: props.href,
        target: props.target,
        style: linkStyle,
        onClick: handleClick,
        disabled: props.disabled
      },
      props.text
    );
  }
};

// 图片组件实现
const ImageComponent = {
  name: 'Image',
  props: {
    src: {
      type: String,
      default: ''
    },
    alt: {
      type: String,
      default: ''
    },
    width: {
      type: [Number, String],
      default: ''
    },
    height: {
      type: [Number, String],
      default: ''
    },
    fit: {
      type: String,
      default: 'cover'
    },
    style: {
      type: Object,
      default: () => ({})
    }
  },
  setup(props) {
    const imgStyle: any = {
      width: props.width,
      height: props.height,
      objectFit: props.fit,
      ...props.style
    };
    
    return () => h('img', { src: props.src, alt: props.alt, style: imgStyle });
  }
};

// 按钮组件实现
const ButtonComponent = {
  name: 'Button',
  props: {
    type: {
      type: String,
      default: 'default'
    },
    size: {
      type: String,
      default: 'medium'
    },
    plain: {
      type: Boolean,
      default: false
    },
    round: {
      type: Boolean,
      default: false
    },
    circle: {
      type: Boolean,
      default: false
    },
    disabled: {
      type: Boolean,
      default: false
    },
    loading: {
      type: Boolean,
      default: false
    },
    icon: {
      type: String,
      default: ''
    },
    text: {
      type: String,
      default: '按钮'
    },
    style: {
      type: Object,
      default: () => ({})
    }
  },
  emits: ['click'],
  setup(props, { slots, emit }) {
    const typeMap: Record<string, string> = {
      primary: '#409eff',
      success: '#67c23a',
      warning: '#e6a23c',
      danger: '#f56c6c',
      info: '#909399'
    };
    
    const sizeMap: Record<string, any> = {
      large: { padding: '12px 20px', fontSize: '16px' },
      medium: { padding: '10px 18px', fontSize: '14px' },
      small: { padding: '8px 15px', fontSize: '13px' },
      mini: { padding: '6px 12px', fontSize: '12px' }
    };
    
    const btnStyle: any = {
      padding: '10px 18px',
      fontSize: '14px',
      border: '1px solid #dcdfe6',
      borderRadius: props.round ? '20px' : props.circle ? '50%' : '4px',
      cursor: props.disabled ? 'not-allowed' : 'pointer',
      backgroundColor: props.plain || !typeMap[props.type] ? '#fff' : typeMap[props.type],
      color: props.plain && typeMap[props.type] ? typeMap[props.type] : 
             !props.plain && typeMap[props.type] ? '#fff' : '#606266',
      borderColor: typeMap[props.type] || '#dcdfe6',
      opacity: props.disabled ? 0.6 : 1,
      ...sizeMap[props.size],
      ...props.style
    };
    
    const handleClick = (e: Event) => {
      if (!props.disabled && !props.loading) {
        emit('click', e);
      }
    };
    
    return () => h(
      'button',
      {
        style: btnStyle,
        disabled: props.disabled || props.loading,
        onClick: handleClick
      },
      [
        props.loading && h('span', { style: { marginRight: '5px' } }, '加载中...'),
        props.icon && h('i', { class: props.icon, style: { marginRight: props.text ? '5px' : '0' } }),
        props.text
      ]
    );
  }
};

// 注册基础组件
export const registerBaseComponents = (): void => {
  // 布局组件
  registryInstance.registerComponents([
    {
      type: 'Container',
      name: '容器',
      description: '基础容器组件，用于布局和组织其他组件',
      component: ContainerComponent,
      category: 'layout',
      icon: 's-grid',
      props: [
        {
          name: 'class',
          type: 'string',
          description: 'CSS类名'
        },
        {
          name: 'style',
          type: 'object',
          description: '内联样式对象'
        },
        {
          name: 'tag',
          type: 'string',
          default: 'div',
          description: 'HTML标签名',
          options: [
            { label: 'div', value: 'div' },
            { label: 'section', value: 'section' },
            { label: 'article', value: 'article' },
            { label: 'aside', value: 'aside' }
          ]
        }
      ],
      slots: [
        {
          name: 'default',
          description: '容器内容',
          default: true
        }
      ]
    },
    // 注册小写版本以保持兼容性
    {
      type: 'container',
      name: '容器(兼容)',
      description: '基础容器组件的小写版本，用于兼容旧代码',
      component: ContainerComponent,
      category: 'layout',
      icon: 's-grid',
      props: [
        {
          name: 'class',
          type: 'string',
          description: 'CSS类名'
        },
        {
          name: 'style',
          type: 'object',
          description: '内联样式对象'
        },
        {
          name: 'tag',
          type: 'string',
          default: 'div',
          description: 'HTML标签名',
          options: [
            { label: 'div', value: 'div' },
            { label: 'section', value: 'section' },
            { label: 'article', value: 'article' },
            { label: 'aside', value: 'aside' }
          ]
        }
      ],
      slots: [
        {
          name: 'default',
          description: '容器内容',
          default: true
        }
      ]
    },
    {
      type: 'Row',
      name: '行',
      description: '栅格系统的行组件',
      component: RowComponent,
      category: 'layout',
      icon: 's-unfold',
      props: [
        {
          name: 'gutter',
          type: 'number',
          default: 0,
          description: '栅格间隔'
        },
        {
          name: 'align',
          type: 'string',
          description: '垂直对齐方式',
          options: [
            { label: 'top', value: 'top' },
            { label: 'middle', value: 'middle' },
            { label: 'bottom', value: 'bottom' }
          ]
        },
        {
          name: 'justify',
          type: 'string',
          description: '水平排列方式',
          options: [
            { label: 'start', value: 'start' },
            { label: 'end', value: 'end' },
            { label: 'center', value: 'center' },
            { label: 'space-around', value: 'space-around' },
            { label: 'space-between', value: 'space-between' }
          ]
        },
        {
          name: 'style',
          type: 'object',
          description: '内联样式对象'
        }
      ],
      slots: [
        {
          name: 'default',
          description: '行内容',
          default: true
        }
      ]
    },
    {
      type: 'Col',
      name: '列',
      description: '栅格系统的列组件',
      component: ColComponent,
      category: 'layout',
      icon: 's-fold',
      props: [
        {
          name: 'span',
          type: 'number',
          default: 24,
          description: '列占据的宽度'
        },
        {
          name: 'offset',
          type: 'number',
          default: 0,
          description: '列偏移的宽度'
        },
        {
          name: 'push',
          type: 'number',
          default: 0,
          description: '列向右移动的宽度'
        },
        {
          name: 'pull',
          type: 'number',
          default: 0,
          description: '列向左移动的宽度'
        },
        {
          name: 'gutter',
          type: 'number',
          default: 0,
          description: '栅格间隔'
        },
        {
          name: 'xs',
          type: 'number',
          description: '超小屏幕( <768px )响应式栅格数'
        },
        {
          name: 'sm',
          type: 'number',
          description: '小屏幕( ≥768px )响应式栅格数'
        },
        {
          name: 'md',
          type: 'number',
          description: '中等屏幕( ≥992px )响应式栅格数'
        },
        {
          name: 'lg',
          type: 'number',
          description: '大屏幕( ≥1200px )响应式栅格数'
        },
        {
          name: 'xl',
          type: 'number',
          description: '超大屏幕( ≥1920px )响应式栅格数'
        },
        {
          name: 'style',
          type: 'object',
          description: '内联样式对象'
        }
      ],
      slots: [
        {
          name: 'default',
          description: '列内容',
          default: true
        }
      ]
    }
  ]);

  // 基础组件
  registryInstance.registerComponents([
    {
      type: 'Text',
      name: '文本',
      description: '文本显示组件',
      component: TextComponent,
      category: 'basic',
      icon: 'edit-outline',
      props: [
        {
          name: 'text',
          type: 'string',
          description: '文本内容（兼容旧版）'
        },
        {
          name: 'content',
          type: 'string',
          description: '文本内容',
          required: true
        },
        {
          name: 'tag',
          type: 'string',
          default: 'span',
          description: 'HTML标签',
          options: [
            { label: 'span', value: 'span' },
            { label: 'div', value: 'div' },
            { label: 'p', value: 'p' },
            { label: 'h1', value: 'h1' },
            { label: 'h2', value: 'h2' },
            { label: 'h3', value: 'h3' },
            { label: 'h4', value: 'h4' },
            { label: 'h5', value: 'h5' },
            { label: 'h6', value: 'h6' }
          ]
        },
        {
          name: 'class',
          type: 'string',
          description: 'CSS类名'
        },
        {
          name: 'style',
          type: 'object',
          description: '内联样式对象'
        },
        {
          name: 'bold',
          type: 'boolean',
          default: false,
          description: '是否粗体'
        },
        {
          name: 'italic',
          type: 'boolean',
          default: false,
          description: '是否斜体'
        },
        {
          name: 'underline',
          type: 'boolean',
          default: false,
          description: '是否下划线'
        }
      ]
    },
    {
      type: 'Button',
      name: '按钮',
      description: '按钮组件',
      component: ButtonComponent,
      category: 'basic',
      icon: 'button',
      props: [
        {
          name: 'type',
          type: 'string',
          default: 'default',
          description: '按钮类型',
          options: [
            { label: 'default', value: 'default' },
            { label: 'primary', value: 'primary' },
            { label: 'success', value: 'success' },
            { label: 'warning', value: 'warning' },
            { label: 'danger', value: 'danger' },
            { label: 'info', value: 'info' }
          ]
        },
        {
          name: 'size',
          type: 'string',
          default: 'medium',
          description: '按钮大小',
          options: [
            { label: 'large', value: 'large' },
            { label: 'medium', value: 'medium' },
            { label: 'small', value: 'small' },
            { label: 'mini', value: 'mini' }
          ]
        },
        {
          name: 'text',
          type: 'string',
          default: '按钮',
          description: '按钮文字'
        },
        {
          name: 'icon',
          type: 'string',
          description: '图标类名'
        },
        {
          name: 'disabled',
          type: 'boolean',
          default: false,
          description: '是否禁用'
        },
        {
          name: 'loading',
          type: 'boolean',
          default: false,
          description: '是否加载中'
        },
        {
          name: 'plain',
          type: 'boolean',
          default: false,
          description: '是否朴素按钮'
        },
        {
          name: 'round',
          type: 'boolean',
          default: false,
          description: '是否圆角按钮'
        },
        {
          name: 'circle',
          type: 'boolean',
          default: false,
          description: '是否圆形按钮'
        },
        {
          name: 'style',
          type: 'object',
          description: '内联样式对象'
        }
      ],
      events: [
        {
          name: 'click',
          description: '点击事件',
          params: [
            {
              name: 'event',
              type: 'MouseEvent',
              description: '鼠标事件对象'
            }
          ]
        }
      ]
    },
    {
      type: 'Image',
      name: '图片',
      description: '图片显示组件',
      component: ImageComponent,
      category: 'basic',
      icon: 'picture-outline',
      props: [
        {
          name: 'src',
          type: 'string',
          description: '图片地址',
          required: true
        },
        {
          name: 'alt',
          type: 'string',
          description: '图片替代文本'
        },
        {
          name: 'width',
          type: 'string',
          description: '图片宽度'
        },
        {
          name: 'height',
          type: 'string',
          description: '图片高度'
        },
        {
          name: 'fit',
          type: 'string',
          default: 'cover',
          description: '图片适应方式',
          options: [
            { label: 'fill', value: 'fill' },
            { label: 'contain', value: 'contain' },
            { label: 'cover', value: 'cover' },
            { label: 'none', value: 'none' },
            { label: 'scale-down', value: 'scale-down' }
          ]
        },
        {
          name: 'lazy',
          type: 'boolean',
          default: false,
          description: '是否懒加载'
        },
        {
          name: 'style',
          type: 'object',
          description: '内联样式对象'
        }
      ],
      events: [
        {
          name: 'load',
          description: '图片加载成功事件'
        },
        {
          name: 'error',
          description: '图片加载失败事件'
        },
        {
          name: 'click',
          description: '点击事件',
          params: [
            {
              name: 'event',
              type: 'MouseEvent',
              description: '鼠标事件对象'
            }
          ]
        }
      ]
    },
    {
      type: 'Link',
      name: '链接',
      description: '链接组件',
      component: LinkComponent,
      category: 'basic',
      icon: 'link',
      props: [
        {
          name: 'href',
          type: 'string',
          description: '链接地址',
          required: true
        },
        {
          name: 'target',
          type: 'string',
          default: '_self',
          description: '目标打开方式',
          options: [
            { label: '_self', value: '_self' },
            { label: '_blank', value: '_blank' },
            { label: '_parent', value: '_parent' },
            { label: '_top', value: '_top' }
          ]
        },
        {
          name: 'text',
          type: 'string',
          default: '链接',
          description: '链接文字'
        },
        {
          name: 'type',
          type: 'string',
          default: 'primary',
          description: '链接类型',
          options: [
            { label: 'default', value: 'default' },
            { label: 'primary', value: 'primary' },
            { label: 'success', value: 'success' },
            { label: 'warning', value: 'warning' },
            { label: 'danger', value: 'danger' },
            { label: 'info', value: 'info' }
          ]
        },
        {
          name: 'underline',
          type: 'boolean',
          default: true,
          description: '是否下划线'
        },
        {
          name: 'disabled',
          type: 'boolean',
          default: false,
          description: '是否禁用'
        },
        {
          name: 'style',
          type: 'object',
          description: '内联样式对象'
        }
      ],
      events: [
        {
          name: 'click',
          description: '点击事件',
          params: [
            {
              name: 'event',
              type: 'MouseEvent',
              description: '鼠标事件对象'
            }
          ]
        }
      ]
    },
    {
      type: 'Divider',
      name: '分隔线',
      description: '分隔线组件',
      component: DividerComponent,
      category: 'basic',
      icon: 'minus',
      props: [
        {
          name: 'orientation',
          type: 'string',
          default: 'center',
          description: '文字方向',
          options: [
            { label: 'left', value: 'left' },
            { label: 'center', value: 'center' },
            { label: 'right', value: 'right' }
          ]
        },
        {
          name: 'contentPosition',
          type: 'string',
          default: 'center',
          description: '内容位置',
          options: [
            { label: 'left', value: 'left' },
            { label: 'center', value: 'center' },
            { label: 'right', value: 'right' }
          ]
        },
        {
          name: 'style',
          type: 'object',
          description: '内联样式对象'
        }
      ],
      slots: [
        {
          name: 'default',
          description: '分隔线文字',
          default: true
        }
      ]
    },
    {
      type: 'StaticText',
      name: '静态文本',
      description: '支持HTML的静态文本组件',
      component: StaticTextComponent,
      category: 'basic',
      icon: 'text',
      props: [
        {
          name: 'content',
          type: 'string',
          description: 'HTML内容',
          required: true
        },
        {
          name: 'style',
          type: 'object',
          description: '内联样式对象'
        }
      ]
    }
  ]);
  
  // 容器组件
  registryInstance.registerComponents([
    {
      type: 'Card',
      name: '卡片',
      description: '卡片容器组件',
      component: CardComponent,
      category: 'container',
      icon: 'document',
      props: [
        {
          name: 'title',
          type: 'string',
          description: '卡片标题'
        },
        {
          name: 'subtitle',
          type: 'string',
          description: '卡片副标题'
        },
        {
          name: 'loading',
          type: 'boolean',
          default: false,
          description: '是否加载中'
        },
        {
          name: 'bordered',
          type: 'boolean',
          default: true,
          description: '是否显示边框'
        },
        {
          name: 'shadow',
          type: 'string',
          default: 'hover',
          description: '阴影效果',
          options: [
            { label: 'always', value: 'always' },
            { label: 'hover', value: 'hover' },
            { label: 'never', value: 'never' }
          ]
        },
        {
          name: 'style',
          type: 'object',
          description: '内联样式对象'
        }
      ],
      slots: [
        {
          name: 'default',
          description: '卡片内容',
          default: true
        }
      ]
    }
  ]);
  
  // 数据展示组件
  registryInstance.registerComponents([
    {
      type: 'StatCard',
      name: '统计卡片',
      description: '数据统计展示卡片',
      component: StatCardComponent,
      category: 'data',
      icon: 'data-line',
      props: [
        {
          name: 'title',
          type: 'string',
          default: '统计项',
          description: '统计标题'
        },
        {
          name: 'value',
          type: 'string',
          default: '0',
          description: '统计数值',
          required: true
        },
        {
          name: 'icon',
          type: 'string',
          description: '图标类名'
        },
        {
          name: 'color',
          type: 'string',
          default: 'primary',
          description: '颜色类型',
          options: [
            { label: 'primary', value: 'primary' },
            { label: 'success', value: 'success' },
            { label: 'warning', value: 'warning' },
            { label: 'danger', value: 'danger' },
            { label: 'info', value: 'info' }
          ]
        },
        {
          name: 'trend',
          type: 'string',
          description: '趋势文本'
        },
        {
          name: 'trendColor',
          type: 'string',
          default: 'success',
          description: '趋势颜色',
          options: [
            { label: 'success', value: 'success' },
            { label: 'danger', value: 'danger' }
          ]
        },
        {
          name: 'style',
          type: 'object',
          description: '内联样式对象'
        }
      ]
    }
  ]);
};

// 导出组件注册中心实例
export const componentRegistry = registryInstance;

// 为了兼容性，保留原来的导出函数和配置
// 组件配置映射 - 用于兼容旧版代码
export const componentConfigMap: Record<string, any> = {};

// 初始化兼容配置
const initCompatibilityConfig = () => {
  // 当组件注册后，自动更新兼容配置
  registryInstance.on('componentRegistered', (component) => {
    componentConfigMap[component.type.toLowerCase()] = {
      name: component.name,
      icon: component.icon,
      category: component.category || '基础',
      props: (component.props || []).map((prop) => ({
        name: prop.name,
        type: prop.type === 'object' ? 'object' : 
              prop.type === 'boolean' ? 'boolean' :
              prop.type === 'number' ? 'number' :
              prop.options ? 'select' : 'string',
        label: prop.description || prop.name,
        default: prop.default,
        options: prop.options?.map(opt => opt.value),
        min: prop.type === 'number' ? 0 : undefined,
        max: prop.name === 'span' || prop.name === 'offset' ? 24 : undefined
      })),
      events: (component.events || []).map((event) => ({
        name: event.name,
        label: event.description || event.name
      }))
    };
  });
};

// 初始化兼容配置
initCompatibilityConfig();

// 兼容旧版API
export const getComponentConfig = (componentType: string): any => {
  return componentConfigMap[componentType.toLowerCase()] || registryInstance.getComponent(componentType);
};

export const getComponentCategories = (): Record<string, any[]> => {
  const categories: Record<string, any[]> = {};
  const components = registryInstance.getAllComponents();
  
  components.forEach(component => {
    const category = component.category || '基础';
    if (!categories[category]) {
      categories[category] = [];
    }
    
    categories[category].push({
      type: component.type,
      name: component.name,
      icon: component.icon,
      ...componentConfigMap[component.type.toLowerCase()]
    });
  });
  
  return categories;
};

// 其他兼容导出
export const registerComponent = (component: ComponentDefinition): void => {
  registryInstance.registerComponent(component);
};

export const registerComponents = (components: ComponentDefinition[]): void => {
  registryInstance.registerComponents(components);
};

export const getAllComponents = (): ComponentDefinition[] => {
  return registryInstance.getAllComponents();
};

export const getComponentsByCategory = (category: string): ComponentDefinition[] => {
  return registryInstance.getComponentsByCategory(category);
};

export const getAvailableComponents = (): ComponentDefinition[] => {
  return registryInstance.getAvailableComponents();
};

export const searchComponents = (query: string): ComponentDefinition[] => {
  return registryInstance.searchComponents(query);
};

export const disableComponent = (type: string): void => {
  registryInstance.disableComponent(type);
};

export const enableComponent = (type: string): void => {
  registryInstance.enableComponent(type);
};

export const hasComponent = (type: string): boolean => {
  return registryInstance.hasComponent(type);
};

// 创建Vue注入键
const componentRegistryKey = Symbol('componentRegistry');

// 提供组件注册中心
export const provideComponentRegistry = () => {
  provide(componentRegistryKey, registryInstance);
};

// 注入组件注册中心
export const injectComponentRegistry = (): ComponentRegistry => {
  const registry = inject<ComponentRegistry>(componentRegistryKey);
  if (!registry) {
    throw new Error('Component registry not provided');
  }
  return registry;
};

// 默认导出
export default {
  registerBaseComponents,
  getComponentConfig,
  getComponentCategories,
  componentConfigMap,
  componentRegistry,
  registerComponent,
  registerComponents,
  getAllComponents,
  getComponentsByCategory,
  getAvailableComponents,
  searchComponents,
  disableComponent,
  enableComponent,
  hasComponent,
  provideComponentRegistry,
  injectComponentRegistry
};