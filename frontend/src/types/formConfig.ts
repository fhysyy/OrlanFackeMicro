import type { FormField, FormConfig, FormGroup } from './form'

/**
 * 表单配置元数据接口
 */
export interface FormConfigMetadata {
  // 表单配置ID
  id: string;
  // 表单名称
  name: string;
  // 表单描述
  description?: string;
  // 表单类型
  type?: 'default' | 'wizard' | 'step' | 'inline';
  // 表单版本
  version: string;
  // 创建时间
  createdAt: string;
  // 更新时间
  updatedAt: string;
  // 创建者
  createdBy?: string;
  // 更新者
  updatedBy?: string;
  // 状态
  status?: 'draft' | 'published' | 'archived';
  // 所属模块
  module?: string;
  // 标签
  tags?: string[];
  // 是否启用
  enabled?: boolean;
}

/**
 * 完整的表单配置接口（包含元数据）
 */
export interface CompleteFormConfig extends FormConfig, FormConfigMetadata {
  // 表单分组
  groups?: FormGroup[];
  // 表单布局配置
  layoutConfig?: {
    // 响应式布局配置
    responsive?: {
      xs?: number;
      sm?: number;
      md?: number;
      lg?: number;
      xl?: number;
      xxl?: number;
    };
    // 是否使用栅格布局
    useGrid?: boolean;
    // 栅格间隔
    gutter?: number;
  };
  // 表单提交配置
  submitConfig?: {
    // 提交方式
    method?: 'post' | 'put' | 'patch';
    // 提交URL
    url?: string;
    // 是否使用防抖
    debounce?: boolean;
    // 防抖延迟（毫秒）
    debounceDelay?: number;
    // 提交前钩子
    beforeSubmit?: string; // 函数名
    // 提交后钩子
    afterSubmit?: string; // 函数名
  };
  // 表单验证配置
  validationConfig?: {
    // 是否在提交时验证
    validateOnSubmit?: boolean;
    // 是否在输入时验证
    validateOnInput?: boolean;
    // 是否在失去焦点时验证
    validateOnBlur?: boolean;
    // 验证错误消息配置
    errorMessageConfig?: {
      // 错误消息位置
      position?: 'top' | 'bottom' | 'left' | 'right';
      // 是否显示图标
      showIcon?: boolean;
    };
  };
  // 表单样式配置
  styleConfig?: {
    // 表单容器类名
    containerClass?: string;
    // 表单项类名
    itemClass?: string;
    // 表单字段类名
    fieldClass?: string;
    // 自定义样式
    customStyles?: Record<string, string>;
  };
  // 表单高级配置
  advancedConfig?: {
    // 自定义组件映射
    componentMap?: Record<string, string>;
    // 自定义字段处理函数
    fieldHandlers?: Record<string, string>; // 函数名映射
    // 条件逻辑配置
    conditionalLogic?: Array<{
      id: string;
      triggerField: string;
      triggerValue: any;
      condition: 'equals' | 'notEquals' | 'contains' | 'notContains' | 'greaterThan' | 'lessThan';
      action: 'show' | 'hide' | 'disable' | 'enable' | 'required' | 'optional';
      targetFields: string[];
    }>;
    // 动态字段配置
    dynamicFields?: Array<{
      id: string;
      field: string;
      dependsOn: string[];
      fetchUrl?: string;
      fetchMethod?: 'get' | 'post';
      transformResponse?: string; // 函数名
    }>;
  };
}

/**
 * 表单配置列表项（用于列表展示）
 */
export interface FormConfigListItem extends FormConfigMetadata {
  // 字段数量
  fieldCount: number;
  // 分组数量
  groupCount?: number;
  // 状态（布尔值，与enabled字段保持一致）
  status: boolean;
}

/**
 * 表单配置API响应接口
 */
export interface FormConfigApiResponse {
  data: CompleteFormConfig;
  message?: string;
  success: boolean;
}

/**
 * 表单配置列表API响应接口
 */
export interface FormConfigListApiResponse {
  data: FormConfigListItem[];
  total: number;
  page: number;
  pageSize: number;
  message?: string;
  success: boolean;
}

/**
 * 表单配置创建请求接口
 */
export interface FormConfigCreateRequest extends Omit<CompleteFormConfig, 'id' | 'createdAt' | 'updatedAt'> {
  // 表单配置创建请求
  // 确保与CompleteFormConfig兼容
  formId?: string; // 兼容旧版本中的formId字段
}

/**
 * 表单配置更新请求接口
 */
export interface FormConfigUpdateRequest extends Partial<CompleteFormConfig> {
  // 表单配置更新请求
}

/**
 * 表单配置查询参数接口
 */
export interface FormConfigQueryParams {
  // 页码
  page?: number;
  // 每页数量
  pageSize?: number;
  // 搜索关键词
  keyword?: string;
  // 表单类型
  type?: string;
  // 状态
  status?: string;
  // 模块
  module?: string;
  // 创建者
  createdBy?: string;
  // 标签
  tag?: string;
  // 排序字段
  sortBy?: string;
  // 排序方向
  sortOrder?: 'asc' | 'desc';
}
