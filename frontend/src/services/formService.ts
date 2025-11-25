import type { FormConfig, FormField, FormFieldType, FormOption } from '../types/form';
import type { ApiResponse } from '../types/api';
import { api } from './api';

/**
 * 创建基本输入框字段
 */
export function createInputField(prop: string, label: string, options?: Partial<FormField>): FormField {
  return {
    prop,
    label,
    type: FormFieldType.INPUT,
    placeholder: `请输入${label}`,
    clearable: true,
    ...options
  };
}

/**
 * 创建选择框字段
 */
export function createSelectField(prop: string, label: string, options: FormOption[], selectOptions?: Partial<FormField>): FormField {
  return {
    prop,
    label,
    type: FormFieldType.SELECT,
    options,
    placeholder: `请选择${label}`,
    clearable: true,
    ...selectOptions
  };
}

/**
 * 创建日期选择器字段
 */
export function createDateField(prop: string, label: string, options?: Partial<FormField>): FormField {
  return {
    prop,
    label,
    type: FormFieldType.DATE_PICKER,
    pickerType: 'date',
    format: 'YYYY-MM-DD',
    placeholder: `请选择${label}`,
    ...options
  };
}

/**
 * 表单模板接口
 */
export interface FormTemplate {
  id: string;
  name: string;
  description: string;
  config: FormConfig;
  createdAt: string;
  updatedAt: string;
}

/**
 * 表单服务
 */
export const formService = {
  // 重定向到直接导出的函数
  createInputField,
  
  /**
   * 创建文本域字段
   */

  /**
   * 创建文本域字段
   */
  createTextareaField(prop: string, label: string, options?: Partial<FormField>): FormField {
    return {
      prop,
      label,
      type: FormFieldType.TEXTAREA,
      placeholder: `请输入${label}`,
      rows: 4,
      clearable: true,
      ...options
    };
  },

  // 重定向到直接导出的函数
  createSelectField,
  
  /**
   * 创建单选框字段
   */

  /**
   * 创建单选框字段
   */
  createRadioField(prop: string, label: string, options: FormOption[], radioOptions?: Partial<FormField>): FormField {
    return {
      prop,
      label,
      type: FormFieldType.RADIO,
      options,
      layout: 'horizontal',
      ...radioOptions
    };
  },

  /**
   * 创建复选框字段
   */
  createCheckboxField(prop: string, label: string, options: FormOption[], checkboxOptions?: Partial<FormField>): FormField {
    return {
      prop,
      label,
      type: FormFieldType.CHECKBOX,
      options,
      value: [],
      layout: 'horizontal',
      ...checkboxOptions
    };
  },

  // 重定向到直接导出的函数
  createDateField,
  
  /**
   * 创建日期选择器字段（兼容旧版）
   */
  createDatePickerField(prop: string, label: string, options?: Partial<FormField>): FormField {
    return createDateField(prop, label, options);
  },

  /**
   * 创建日期时间选择器字段
   */
  createDateTimePickerField(prop: string, label: string, options?: Partial<FormField>): FormField {
    return {
      prop,
      label,
      type: FormFieldType.DATETIME_PICKER,
      pickerType: 'datetime',
      format: 'YYYY-MM-DD HH:mm:ss',
      placeholder: `请选择${label}`,
      ...options
    };
  },

  /**
   * 创建数字输入框字段
   */
  createInputNumberField(prop: string, label: string, options?: Partial<FormField>): FormField {
    return {
      prop,
      label,
      type: FormFieldType.INPUT_NUMBER,
      step: 1,
      clearable: true,
      ...options
    };
  },

  /**
   * 创建开关字段
   */
  createSwitchField(prop: string, label: string, options?: Partial<FormField>): FormField {
    return {
      prop,
      label,
      type: FormFieldType.SWITCH,
      activeValue: true,
      inactiveValue: false,
      ...options
    };
  },

  /**
   * 从数据模型生成表单配置
   */
  generateFormConfigFromModel(model: Record<string, any>, options?: Partial<FormConfig>): FormConfig {
    const fields: FormField[] = [];

    Object.entries(model).forEach(([key, value]) => {
      let field: FormField;
      const label = this.camelCaseToLabel(key);

      switch (typeof value) {
        case 'string':
          // 判断是否为日期格式
          if (this.isDateFormat(value)) {
            field = this.createDateTimePickerField(key, label, {
              value
            });
          } else {
            // 默认创建输入框
            field = this.createInputField(key, label, {
              value
            });
          }
          break;
        case 'number':
          field = this.createInputNumberField(key, label, {
            value
          });
          break;
        case 'boolean':
          field = this.createSwitchField(key, label, {
            value
          });
          break;
        case 'object':
          if (Array.isArray(value)) {
            // 简单数组，创建复选框
            field = this.createCheckboxField(key, label, 
              value.map(v => ({ label: String(v), value: v })),
              { value }
            );
          } else if (value === null) {
            field = this.createInputField(key, label);
          } else {
            // 对象类型，创建JSON编辑器或嵌套表单
            field = this.createTextareaField(key, label, {
              value: JSON.stringify(value, null, 2),
              rows: 4,
              helpText: 'JSON格式'
            });
          }
          break;
        default:
          field = this.createInputField(key, label);
      }

      fields.push(field);
    });

    return {
      fields,
      initialData: { ...model },
      labelWidth: '120px',
      showResetButton: true,
      showSubmitButton: true,
      ...options
    };
  },

  /**
   * 获取表单模板列表
   */
  async getFormTemplates(): Promise<ApiResponse<FormTemplate[]>> {
    return api.get<ApiResponse<FormTemplate[]>>('/api/form-templates');
  },

  /**
   * 获取表单模板详情
   */
  async getFormTemplate(id: string): Promise<ApiResponse<FormTemplate>> {
    return api.get<ApiResponse<FormTemplate>>(`/api/form-templates/${id}`);
  },

  /**
   * 创建表单模板
   */
  async createFormTemplate(template: Omit<FormTemplate, 'id' | 'createdAt' | 'updatedAt'>): Promise<ApiResponse<FormTemplate>> {
    return api.post<ApiResponse<FormTemplate>>('/api/form-templates', template);
  },

  /**
   * 更新表单模板
   */
  async updateFormTemplate(id: string, template: Partial<FormTemplate>): Promise<ApiResponse<FormTemplate>> {
    return api.put<ApiResponse<FormTemplate>>(`/api/form-templates/${id}`, template);
  },

  /**
   * 删除表单模板
   */
  async deleteFormTemplate(id: string): Promise<ApiResponse<void>> {
    return api.delete<ApiResponse<void>>(`/api/form-templates/${id}`);
  },

  /**
   * 驼峰命名转中文标签
   */
  camelCaseToLabel(camelCase: string): string {
    return camelCase
      .replace(/([A-Z])/g, ' $1')
      .replace(/^./, str => str.toUpperCase())
      .replace(/Id$/, 'ID')
      .trim();
  },

  /**
   * 检查字符串是否为日期格式
   */
  isDateFormat(str: string): boolean {
    // 简单的日期格式检查
    const dateRegex = /^\d{4}[-/]\d{1,2}[-/]\d{1,2}(\s\d{1,2}:\d{2}(:\d{2})?)?$/;
    return dateRegex.test(str) && !isNaN(Date.parse(str));
  },

  /**
   * 合并表单配置
   */
  mergeFormConfig(baseConfig: FormConfig, overrideConfig: Partial<FormConfig>): FormConfig {
    const merged = { ...baseConfig, ...overrideConfig };
    
    // 合并字段配置
    if (overrideConfig.fields) {
      const baseFieldMap = new Map<string, FormField>();
      baseConfig.fields.forEach(field => {
        baseFieldMap.set(field.prop, field);
      });

      merged.fields = overrideConfig.fields.map(overrideField => {
        const baseField = baseFieldMap.get(overrideField.prop);
        return baseField ? { ...baseField, ...overrideField } : overrideField;
      });
    }

    return merged;
  }
};

export default formService;