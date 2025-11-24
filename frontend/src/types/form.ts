import type { FormItemRule } from 'element-plus';

/**
 * 表单字段类型枚举
 */
export enum FormFieldType {
  INPUT = 'input',
  SELECT = 'select',
  RADIO = 'radio',
  CHECKBOX = 'checkbox',
  SWITCH = 'switch',
  DATE_PICKER = 'date-picker',
  TIME_PICKER = 'time-picker',
  DATETIME_PICKER = 'datetime-picker',
  UPLOAD = 'upload',
  SLIDER = 'slider',
  INPUT_NUMBER = 'input-number',
  TEXTAREA = 'textarea',
  CASCADER = 'cascader',
  TREE_SELECT = 'tree-select',
  TRANSFER = 'transfer',
  RATE = 'rate'
}

/**
 * 表单项选项接口
 */
export interface FormOption {
  label: string;
  value: string | number | boolean;
  disabled?: boolean;
  // 用于选项的附加数据
  [key: string]: any;
}

/**
 * 表单字段基础配置接口
 */
export interface BaseFormField {
  // 字段属性名
  prop: string;
  // 字段标签
  label: string;
  // 字段类型
  type: FormFieldType;
  // 初始值
  value?: any;
  // 占位符
  placeholder?: string;
  // 是否禁用
  disabled?: boolean;
  // 是否隐藏
  hidden?: boolean;
  // 字段宽度
  width?: number | string;
  // 表单校验规则
  rules?: FormItemRule[];
  // 帮助文本
  helpText?: string;
  // 前置图标
  prefixIcon?: string;
  // 后置图标
  suffixIcon?: string;
  // 自定义类名
  className?: string;
  // 是否必填
  required?: boolean;
  // 栅格列数 (1-24)
  span?: number;
  // 附加配置
  [key: string]: any;
}

/**
 * 输入框字段配置
 */
export interface InputFormField extends BaseFormField {
  type: FormFieldType.INPUT;
  // 输入框类型
  inputType?: 'text' | 'password' | 'email' | 'url' | 'tel' | 'number';
  // 最大长度
  maxlength?: number;
  // 是否可清空
  clearable?: boolean;
  // 是否显示字数统计
  showWordLimit?: boolean;
}

/**
 * 选择框字段配置
 */
export interface SelectFormField extends BaseFormField {
  type: FormFieldType.SELECT;
  // 选项列表
  options: FormOption[];
  // 是否多选
  multiple?: boolean;
  // 是否可搜索
  filterable?: boolean;
  // 是否可清空
  clearable?: boolean;
  // 远程搜索
  remote?: boolean;
  // 远程搜索方法
  remoteMethod?: (query: string) => void;
  // 选项加载状态
  loading?: boolean;
  // 选项键名映射
  labelKey?: string;
  valueKey?: string;
}

/**
 * 单选框字段配置
 */
export interface RadioFormField extends BaseFormField {
  type: FormFieldType.RADIO;
  // 选项列表
  options: FormOption[];
  // 布局方式
  layout?: 'horizontal' | 'vertical';
  // 选项键名映射
  labelKey?: string;
  valueKey?: string;
}

/**
 * 复选框字段配置
 */
export interface CheckboxFormField extends BaseFormField {
  type: FormFieldType.CHECKBOX;
  // 选项列表
  options: FormOption[];
  // 布局方式
  layout?: 'horizontal' | 'vertical';
  // 选项键名映射
  labelKey?: string;
  valueKey?: string;
}

/**
 * 开关字段配置
 */
export interface SwitchFormField extends BaseFormField {
  type: FormFieldType.SWITCH;
  // 开启值
  activeValue?: boolean | string | number;
  // 关闭值
  inactiveValue?: boolean | string | number;
  // 开启文案
  activeText?: string;
  // 关闭文案
  inactiveText?: string;
}

/**
 * 日期选择器字段配置
 */
export interface DatePickerFormField extends BaseFormField {
  type: FormFieldType.DATE_PICKER;
  // 选择器类型
  pickerType?: 'date' | 'daterange' | 'year' | 'month' | 'monthrange';
  // 日期格式
  format?: string;
  // 占位符
  startPlaceholder?: string;
  endPlaceholder?: string;
}

/**
 * 时间选择器字段配置
 */
export interface TimePickerFormField extends BaseFormField {
  type: FormFieldType.TIME_PICKER;
  // 选择器类型
  pickerType?: 'time' | 'timerange';
  // 时间格式
  format?: string;
  // 占位符
  startPlaceholder?: string;
  endPlaceholder?: string;
}

/**
 * 日期时间选择器字段配置
 */
export interface DateTimePickerFormField extends BaseFormField {
  type: FormFieldType.DATETIME_PICKER;
  // 选择器类型
  pickerType?: 'datetime' | 'datetimerange';
  // 日期格式
  format?: string;
  // 占位符
  startPlaceholder?: string;
  endPlaceholder?: string;
}

/**
 * 文件上传字段配置
 */
export interface UploadFormField extends BaseFormField {
  type: FormFieldType.UPLOAD;
  // 上传地址
  action: string;
  // 是否支持多选
  multiple?: boolean;
  // 文件类型限制
  accept?: string;
  // 文件大小限制（字节）
  fileSize?: number;
  // 上传前钩子
  beforeUpload?: (file: File) => boolean | Promise<File>;
  // 上传成功钩子
  onSuccess?: (response: any, file: any) => void;
  // 上传失败钩子
  onError?: (error: any, file: any) => void;
}

/**
 * 滑块字段配置
 */
export interface SliderFormField extends BaseFormField {
  type: FormFieldType.SLIDER;
  // 最小值
  min?: number;
  // 最大值
  max?: number;
  // 步长
  step?: number;
  // 是否显示输入框
  showInput?: boolean;
  // 是否范围选择
  range?: boolean;
}

/**
 * 数字输入框字段配置
 */
export interface InputNumberFormField extends BaseFormField {
  type: FormFieldType.INPUT_NUMBER;
  // 最小值
  min?: number;
  // 最大值
  max?: number;
  // 步长
  step?: number;
  // 精度
  precision?: number;
  // 是否可清空
  clearable?: boolean;
}

/**
 * 文本域字段配置
 */
export interface TextareaFormField extends BaseFormField {
  type: FormFieldType.TEXTAREA;
  // 最大长度
  maxlength?: number;
  // 是否可清空
  clearable?: boolean;
  // 是否显示字数统计
  showWordLimit?: boolean;
  // 行数
  rows?: number;
  // 是否自动增高
  autosize?: boolean | { minRows: number; maxRows: number };
}

/**
 * 级联选择器字段配置
 */
export interface CascaderFormField extends BaseFormField {
  type: FormFieldType.CASCADER;
  // 选项列表
  options: any[];
  // 是否多选
  multiple?: boolean;
  // 选项键名映射
  props?: {
    value?: string;
    label?: string;
    children?: string;
    disabled?: string;
  };
  // 显示路径
  showPath?: boolean;
}

/**
 * 树形选择器字段配置
 */
export interface TreeSelectFormField extends BaseFormField {
  type: FormFieldType.TREE_SELECT;
  // 选项列表
  data: any[];
  // 是否多选
  multiple?: boolean;
  // 选项键名映射
  props?: {
    value?: string;
    label?: string;
    children?: string;
    disabled?: string;
  };
  // 占位符
  placeholder?: string;
}

/**
 * 穿梭框字段配置
 */
export interface TransferFormField extends BaseFormField {
  type: FormFieldType.TRANSFER;
  // 数据列表
  data: any[];
  // 标题
  titles?: [string, string];
  // 选项键名映射
  props?: {
    key?: string;
    label?: string;
    disabled?: string;
  };
  // 自定义渲染函数
  renderContent?: (h: any, option: any) => any;
}

/**
 * 评分字段配置
 */
export interface RateFormField extends BaseFormField {
  type: FormFieldType.RATE;
  // 最大分值
  max?: number;
  // 是否允许半星
  allowHalf?: boolean;
  // 是否可清空
  allowClear?: boolean;
}

/**
 * 表单字段配置联合类型
 */
export type FormField = 
  | InputFormField
  | SelectFormField
  | RadioFormField
  | CheckboxFormField
  | SwitchFormField
  | DatePickerFormField
  | TimePickerFormField
  | DateTimePickerFormField
  | UploadFormField
  | SliderFormField
  | InputNumberFormField
  | TextareaFormField
  | CascaderFormField
  | TreeSelectFormField
  | TransferFormField
  | RateFormField;

/**
 * 表单配置接口
 */
export interface FormConfig {
  // 表单字段列表
  fields: FormField[];
  // 表单初始数据
  initialData?: Record<string, any>;
  // 表单标签对齐方式
  labelPosition?: 'left' | 'right' | 'top';
  // 表单标签宽度
  labelWidth?: string | number;
  // 表单项间距
  itemSpacing?: number;
  // 是否显示收起展开按钮
  collapsible?: boolean;
  // 是否显示重置按钮
  showResetButton?: boolean;
  // 是否显示提交按钮
  showSubmitButton?: boolean;
  // 提交按钮文本
  submitButtonText?: string;
  // 重置按钮文本
  resetButtonText?: string;
  // 表单布局类型
  layout?: 'horizontal' | 'vertical';
  // 自定义提交函数
  onSubmit?: (data: Record<string, any>) => Promise<void> | void;
  // 自定义重置函数
  onReset?: () => void;
}

/**
 * 表单分组配置
 */
export interface FormGroup {
  // 分组标题
  title: string;
  // 分组描述
  description?: string;
  // 分组字段
  fields: FormField[];
  // 是否可折叠
  collapsible?: boolean;
  // 是否默认展开
  expanded?: boolean;
}