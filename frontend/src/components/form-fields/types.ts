import { FormField, FormFieldType, DatabaseDataType, ValidationRule } from '@/types'

// 基础表单字段组件属性接口
export interface BaseFormFieldProps {
  field: FormField;
  modelValue: any;
  disabled?: boolean;
}

// 表单字段组件类型映射
export const FormFieldComponents: Record<FormFieldType, string> = {
  [FormFieldType.INPUT]: 'FormFieldInput',
  [FormFieldType.TEXTAREA]: 'FormFieldTextarea',
  [FormFieldType.SELECT]: 'FormFieldSelect',
  [FormFieldType.RADIO]: 'FormFieldRadio',
  [FormFieldType.CHECKBOX]: 'FormFieldCheckbox',
  [FormFieldType.SWITCH]: 'FormFieldSwitch',
  [FormFieldType.DATE_PICKER]: 'FormFieldDatePicker',
  [FormFieldType.TIME_PICKER]: 'FormFieldTimePicker',
  [FormFieldType.TIME]: 'FormFieldTime',
  [FormFieldType.DATETIME_PICKER]: 'FormFieldDatetimePicker',
  [FormFieldType.INPUT_NUMBER]: 'FormFieldInputNumber',
  [FormFieldType.SLIDER]: 'FormFieldSlider',
  [FormFieldType.RATE]: 'FormFieldRate',
  [FormFieldType.UPLOAD]: 'FormFieldUpload',
  [FormFieldType.CASCADER]: 'FormFieldCascader',
  [FormFieldType.TREE_SELECT]: 'FormFieldTreeSelect',
  [FormFieldType.TRANSFER]: 'FormFieldTransfer'
}

// 表单布局类型
export type FormLayout = 'vertical' | 'inline' | 'grid' | 'grid-right';

// 表单布局配置
export interface FormLayoutConfig {
  type: FormLayout;
  gutter?: number;
  justify?: 'start' | 'center' | 'end' | 'space-between' | 'space-around';
  xs?: number;
  sm?: number;
  md?: number;
  lg?: number;
  xl?: number;
}
