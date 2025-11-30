/**
 * 表单配置实体类定义
 * 用于前后端数据交互和数据库存储
 */

/**
 * 表单状态枚举
 */
export enum FormConfigStatus {
  DRAFT = 'draft',
  PUBLISHED = 'published',
  ARCHIVED = 'archived'
}

/**
 * 表单类型枚举
 */
export enum FormType {
  DEFAULT = 'default',
  WIZARD = 'wizard',
  STEP = 'step',
  INLINE = 'inline'
}

/**
 * 表单选项实体类
 */
export interface FormOptionEntity {
  // 选项ID（用于唯一标识）
  id?: string;
  // 选项标签
  label: string;
  // 选项值
  value: string | number | boolean;
  // 是否禁用
  disabled?: boolean;
  // 排序号
  order?: number;
  // 附加属性（JSON格式存储）
  extra?: Record<string, any>;
}

/**
 * 表单字段验证规则实体类
 */
export interface FormValidationRuleEntity {
  // 规则ID
  id?: string;
  // 规则类型
  type: 'required' | 'min' | 'max' | 'len' | 'email' | 'phone' | 'idcard' | 'pattern';
  // 规则值（如最小长度、最大长度等）
  value?: any;
  // 错误提示信息
  message: string;
  // 触发方式
  trigger?: 'blur' | 'change' | 'focus';
}

/**
 * 表单字段实体类
 * 用于数据库存储的简化版本
 */
export interface FormFieldEntity {
  // 字段ID（主键）
  id?: string;
  // 所属表单配置ID
  formConfigId: string;
  // 所属分组ID（可选）
  groupId?: string;
  // 字段属性名
  prop: string;
  // 字段标签
  label: string;
  // 字段类型
  type: string;
  // 初始值
  initialValue?: any;
  // 占位符
  placeholder?: string;
  // 是否必填
  required?: boolean;
  // 是否禁用
  disabled?: boolean;
  // 是否隐藏
  hidden?: boolean;
  // 帮助文本
  helpText?: string;
  // 栅格列数
  span?: number;
  // 排序号
  order?: number;
  // 字段配置（JSON格式存储，包含所有特定类型字段的配置）
  config: Record<string, any>;
  // 验证规则
  validationRules?: FormValidationRuleEntity[];
  // 选项列表（用于select、radio、checkbox等）
  options?: FormOptionEntity[];
  // 创建时间
  createdAt?: string;
  // 更新时间
  updatedAt?: string;
}

/**
 * 表单分组实体类
 */
export interface FormGroupEntity {
  // 分组ID（主键）
  id?: string;
  // 所属表单配置ID
  formConfigId: string;
  // 分组标题
  title: string;
  // 分组描述
  description?: string;
  // 是否可折叠
  collapsible?: boolean;
  // 是否默认展开
  expanded?: boolean;
  // 排序号
  order?: number;
  // 创建时间
  createdAt?: string;
  // 更新时间
  updatedAt?: string;
  // 关联的字段（通常在查询时加载）
  fields?: FormFieldEntity[];
}

/**
 * 表单条件逻辑实体类
 */
export interface FormConditionalLogicEntity {
  // 逻辑ID（主键）
  id?: string;
  // 所属表单配置ID
  formConfigId: string;
  // 触发字段
  triggerField: string;
  // 触发值
  triggerValue: any;
  // 条件类型
  condition: 'equals' | 'notEquals' | 'contains' | 'notContains' | 'greaterThan' | 'lessThan';
  // 执行动作
  action: 'show' | 'hide' | 'disable' | 'enable' | 'required' | 'optional';
  // 目标字段列表
  targetFields: string[];
  // 创建时间
  createdAt?: string;
  // 更新时间
  updatedAt?: string;
}

/**
 * 表单配置实体类
 * 核心实体类，用于数据库存储和前后端交互
 */
export interface FormConfigEntity {
  // 表单配置ID（主键）
  id: string;
  // 表单名称
  name: string;
  // 表单描述
  description?: string;
  // 表单类型
  type?: FormType;
  // 表单版本
  version: string;
  // 状态
  status?: FormConfigStatus;
  // 是否启用
  enabled?: boolean;
  // 所属模块
  module?: string;
  // 标签（JSON格式存储）
  tags?: string[];
  // 表单布局配置（JSON格式存储）
  layoutConfig?: Record<string, any>;
  // 表单提交配置（JSON格式存储）
  submitConfig?: Record<string, any>;
  // 表单验证配置（JSON格式存储）
  validationConfig?: Record<string, any>;
  // 表单样式配置（JSON格式存储）
  styleConfig?: Record<string, any>;
  // 表单高级配置（JSON格式存储）
  advancedConfig?: Record<string, any>;
  // 创建者
  createdBy?: string;
  // 更新者
  updatedBy?: string;
  // 创建时间
  createdAt: string;
  // 更新时间
  updatedAt: string;
  // 关联的分组（通常在查询时加载）
  groups?: FormGroupEntity[];
  // 关联的字段（通常在查询时加载）
  fields?: FormFieldEntity[];
  // 关联的条件逻辑（通常在查询时加载）
  conditionalLogics?: FormConditionalLogicEntity[];
}

/**
 * 表单配置创建请求实体类
 */
export interface FormConfigCreateEntity extends Omit<FormConfigEntity, 'id' | 'createdAt' | 'updatedAt'> {
  // 创建表单配置时不需要ID和时间戳
}

/**
 * 表单配置更新请求实体类
 */
export interface FormConfigUpdateEntity extends Partial<FormConfigEntity> {
  // 更新时只需要提供部分字段
  // 必须包含ID
  id: string;
}

/**
 * 表单配置查询响应实体类（用于列表展示）
 */
export interface FormConfigListItemEntity {
  id: string;
  name: string;
  description?: string;
  type?: FormType;
  version: string;
  status?: FormConfigStatus;
  enabled?: boolean;
  module?: string;
  tags?: string[];
  fieldCount: number;
  groupCount?: number;
  createdBy?: string;
  updatedBy?: string;
  createdAt: string;
  updatedAt: string;
}
