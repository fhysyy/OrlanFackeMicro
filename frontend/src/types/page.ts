import type { FormConfig } from './form'
import type { TableConfig } from './table'
import type { CrudConfig } from './crud'

/**
 * 页面布局类型
 */
export enum PageLayoutType {
  // 单列布局
  SINGLE_COLUMN = 'single-column',
  // 双列布局
  TWO_COLUMN = 'two-column',
  // 三列布局
  THREE_COLUMN = 'three-column',
  // 自定义布局
  CUSTOM = 'custom'
}

/**
 * 组件类型
 */
export enum ComponentType {
  // 基础组件
  TEXT = 'text',
  INPUT = 'input',
  SELECT = 'select',
  CHECKBOX = 'checkbox',
  RADIO = 'radio',
  SWITCH = 'switch',
  DATE_PICKER = 'date-picker',
  TIME_PICKER = 'time-picker',
  DATETIME_PICKER = 'datetime-picker',
  UPLOAD = 'upload',
  
  // 布局组件
  CONTAINER = 'container',
  ROW = 'row',
  COL = 'col',
  CARD = 'card',
  DIVIDER = 'divider',
  SPACE = 'space',
  
  // 业务组件
  FORM = 'form',
  TABLE = 'table',
  CRUD = 'crud',
  CHART = 'chart',
  LIST = 'list',
  TABS = 'tabs',
  STEPS = 'steps',
  TIMELINE = 'timeline',
  
  // 高级组件
  DRAWER = 'drawer',
  DIALOG = 'dialog',
  MODAL = 'modal',
  POPOVER = 'popover',
  TOOLTIP = 'tooltip'
}

/**
 * 数据源类型
 */
export enum DataSourceType {
  // API接口
  API = 'api',
  // 静态数据
  STATIC = 'static',
  // 状态管理
  STORE = 'store',
  // 计算属性
  COMPUTED = 'computed'
}

/**
 * 数据源配置
 */
export interface DataSourceConfig {
  // 数据源类型
  type: DataSourceType;
  // 数据源地址/路径
  source: string;
  // 请求参数
  params?: any;
  // 请求方法 (GET, POST, PUT, DELETE等)
  method?: string;
  // 数据映射
  mapping?: Record<string, string>;
  // 数据转换函数
  transform?: string; // JavaScript函数字符串
  // 初始值
  initialValue?: any;
}

/**
 * 组件属性配置
 */
export interface ComponentProps {
  // 组件ID
  id?: string;
  // 组件名称
  name?: string;
  // 组件标签
  label?: string;
  // 组件样式
  style?: Record<string, any>;
  // CSS类名
  class?: string;
  // 显示条件
  visible?: boolean | string; // JavaScript表达式字符串
  // 禁用条件
  disabled?: boolean | string; // JavaScript表达式字符串
  // 其他属性
  [key: string]: any;
}

/**
 * 组件事件配置
 */
export interface ComponentEvent {
  // 事件名称
  name: string;
  // 事件处理函数 (JavaScript函数字符串)
  handler: string;
  // 事件参数
  params?: string[];
}

/**
 * 组件数据绑定
 */
export interface DataBinding {
  // 绑定类型
  type: 'model' | 'prop' | 'event';
  // 绑定路径
  path: string;
  // 绑定目标
  target?: string;
}

/**
 * 组件配置接口
 */
export interface ComponentConfig {
  // 组件类型
  type: ComponentType;
  // 组件属性
  props?: ComponentProps;
  // 组件事件
  events?: ComponentEvent[];
  // 子组件
  children?: ComponentConfig[];
  // 数据源配置
  dataSource?: DataSourceConfig;
  // 数据绑定配置
  bindings?: DataBinding[];
  // 表单配置 (当组件类型为FORM时使用)
  formConfig?: FormConfig;
  // 表格配置 (当组件类型为TABLE时使用)
  tableConfig?: TableConfig;
  // CRUD配置 (当组件类型为CRUD时使用)
  crudConfig?: CrudConfig;
  // 条件渲染
  vIf?: string; // JavaScript表达式字符串
  // 列表渲染
  vFor?: {
    item: string;
    index?: string;
    of: string; // 数据源表达式
  };
}

/**
 * 页面布局配置
 */
export interface LayoutConfig {
  // 布局类型
  type: PageLayoutType;
  // 布局配置
  config?: {
    // 左侧边栏宽度
    leftSideWidth?: string;
    // 右侧边栏宽度
    rightSideWidth?: string;
    // 主内容区最小宽度
    mainContentMinWidth?: string;
  };
}

/**
 * 页面状态配置
 */
export interface PageState {
  // 状态名称
  [key: string]: {
    // 初始值
    initialValue: any;
    // 是否持久化
    persist?: boolean;
    // 持久化键名
    persistKey?: string;
  };
}

/**
 * 页面方法配置
 */
export interface PageMethod {
  // 方法名称
  name: string;
  // 方法体 (JavaScript函数字符串)
  body: string;
  // 方法参数
  params?: string[];
}

/**
 * 页面生命周期配置
 */
export interface PageLifecycle {
  // 创建前
  beforeCreate?: string; // JavaScript函数字符串
  // 创建后
  created?: string; // JavaScript函数字符串
  // 挂载前
  beforeMount?: string; // JavaScript函数字符串
  // 挂载后
  mounted?: string; // JavaScript函数字符串
  // 更新前
  beforeUpdate?: string; // JavaScript函数字符串
  // 更新后
  updated?: string; // JavaScript函数字符串
  // 卸载前
  beforeUnmount?: string; // JavaScript函数字符串
  // 卸载后
  unmounted?: string; // JavaScript函数字符串
}

/**
 * 页面路由配置
 */
export interface PageRoute {
  // 路由路径
  path: string;
  // 路由名称
  name?: string;
  // 路由元数据
  meta?: {
    // 页面标题
    title?: string;
    // 页面图标
    icon?: string;
    // 是否在菜单中显示
    showInMenu?: boolean;
    // 权限码
    permission?: string;
    // 是否缓存
    keepAlive?: boolean;
    // 其他元数据
    [key: string]: any;
  };
}

/**
 * 页面主题配置
 */
export interface PageTheme {
  // 主题颜色
  primaryColor?: string;
  // 页面背景色
  backgroundColor?: string;
  // 文字颜色
  textColor?: string;
  // 卡片样式
  cardStyle?: Record<string, any>;
  // 按钮样式
  buttonStyle?: Record<string, any>;
}

/**
 * 页面配置接口
 */
export interface PageConfig {
  // 页面ID
  id?: string;
  // 页面名称
  name?: string;
  // 页面描述
  description?: string;
  // 页面布局
  layout?: LayoutConfig;
  // 页面路由
  route?: PageRoute;
  // 页面状态
  state?: PageState;
  // 页面方法
  methods?: PageMethod[];
  // 页面生命周期
  lifecycle?: PageLifecycle;
  // 页面生命周期钩子
  lifecycleHooks?: Record<string, string>;
  // 页面主题
  theme?: PageTheme;
  // 组件配置
  components: ComponentConfig[];
  // 页面级数据源
  dataSources?: Record<string, DataSourceConfig>;
  // 数据源处理器
  dataSourceHandlers?: Record<string, Function>;
  // 数据处理器
  dataProcessors?: Function[];
  // 页面级样式
  styles?: string; // CSS字符串
  // 页面级脚本
  script?: string; // JavaScript字符串
  // 响应式设置
  responsive?: boolean;
  // 响应式配置
  responsiveSettings?: Record<string, any>;
  // 页面属性
  pageProps?: Record<string, any>;
  // 头部组件
  header?: ComponentConfig;
  // 侧边栏组件
  sidebar?: ComponentConfig;
  // 右侧边栏组件
  rightSidebar?: ComponentConfig;
  // 底部组件
  footer?: ComponentConfig;
}

/**
 * 页面元数据配置
 */
export interface PageMetadata {
  // 页面ID
  id: string;
  // 页面名称
  name: string;
  // 页面描述
  description?: string;
  // 页面版本
  version?: string;
  // 创建时间
  createdAt?: string;
  // 更新时间
  updatedAt?: string;
  // 创建者
  createdBy?: string;
  // 更新者
  updatedBy?: string;
  // 页面布局
  layout: LayoutConfig;
  // 页面路由
  route: PageRoute;
  // 页面状态
  state?: PageState;
  // 页面方法
  methods?: PageMethod[];
  // 页面生命周期
  lifecycle?: PageLifecycle;
  // 页面主题
  theme?: PageTheme;
  // 组件配置
  components: ComponentConfig[];
  // 页面级数据源
  dataSources?: Record<string, DataSourceConfig>;
  // 页面级样式
  styles?: string; // CSS字符串
  // 页面级脚本
  script?: string; // JavaScript字符串
}

/**
 * 页面构建选项
 */
export interface PageBuildOptions {
  // 是否启用缓存
  enableCache?: boolean;
  // 是否启用性能优化
  enableOptimization?: boolean;
  // 是否启用响应式数据
  enableReactivity?: boolean;
  // 是否启用热更新
  enableHotReload?: boolean;
  // 自定义全局属性
  globalProps?: Record<string, any>;
}