import type { FormConfig, FormField } from '@/types/formConfig'

/**
 * 表单工具类 - 提供表单处理相关的静态方法
 */
export class FormUtils {
  /**
   * 检测对象中是否存在循环引用
   * @param obj 要检测的对象
   * @param seen 已访问对象的集合（内部使用）
   * @returns 是否存在循环引用
   */
  static hasCircularReference(obj: any, seen = new WeakSet()): boolean {
    if (obj === null || typeof obj !== 'object') {
      return false
    }
    
    if (seen.has(obj)) {
      return true
    }
    
    seen.add(obj)
    
    for (const key in obj) {
      if (Object.prototype.hasOwnProperty.call(obj, key)) {
        if (this.hasCircularReference(obj[key], seen)) {
          return true
        }
      }
    }
    
    return false
  }

  /**
   * 安全的深拷贝方法，处理循环引用问题
   * @param obj 要拷贝的对象
   * @returns 拷贝后的对象
   */
  static safeDeepClone(obj: any): any {
    if (obj === null || typeof obj !== 'object') {
      return obj
    }
    
    // 如果是数组
    if (Array.isArray(obj)) {
      const clonedArray: any[] = []
      for (let i = 0; i < obj.length; i++) {
        clonedArray[i] = this.safeDeepClone(obj[i])
      }
      return clonedArray
    }
    
    // 如果是对象
    const clonedObj: Record<string, any> = {}
    for (const key in obj) {
      if (Object.prototype.hasOwnProperty.call(obj, key)) {
        clonedObj[key] = this.safeDeepClone(obj[key])
      }
    }
    return clonedObj
  }

  /**
   * 安全的JSON序列化，处理循环引用问题
   * @param obj 要序列化的对象
   * @param space 缩进空格数
   * @returns JSON字符串
   */
  static safeJsonStringify(obj: any, space?: number): string {
    const seen = new WeakSet()
    return JSON.stringify(obj, (key, value) => {
      if (typeof value === 'object' && value !== null) {
        if (seen.has(value)) {
          return '[Circular Reference]'
        }
        seen.add(value)
      }
      return value
    }, space)
  }

  /**
   * 规范化表单配置
   * @param config 表单配置对象
   * @returns 规范化后的表单配置
   */
  static normalizeFormConfig(config: Partial<FormConfig>): FormConfig {
    const normalized: FormConfig = {
      id: config.id || this.generateId(),
      title: config.title || '',
      description: config.description || '',
      fields: config.fields || [],
      layout: config.layout || {
        type: 'vertical',
        columns: 1,
        spacing: 'medium'
      },
      settings: {
        showProgressBar: config.settings?.showProgressBar ?? true,
        allowSaveDraft: config.settings?.allowSaveDraft ?? true,
        validateOnBlur: config.settings?.validateOnBlur ?? false,
        ...config.settings
      },
      createdAt: config.createdAt || new Date().toISOString(),
      updatedAt: config.updatedAt || new Date().toISOString(),
      version: config.version || '1.0.0'
    }

    // 规范化字段
    normalized.fields = normalized.fields.map(field => this.normalizeFormField(field))

    return normalized
  }

  /**
   * 规范化表单字段
   * @param field 表单字段
   * @returns 规范化后的表单字段
   */
  static normalizeFormField(field: Partial<FormField>): FormField {
    return {
      id: field.id || this.generateId(),
      type: field.type || 'input',
      name: field.name || '',
      label: field.label || field.name || '',
      placeholder: field.placeholder || '',
      required: field.required || false,
      disabled: field.disabled || false,
      options: field.options || [],
      validation: field.validation || {},
      defaultValue: field.defaultValue,
      description: field.description || '',
      width: field.width || '100%',
      className: field.className || '',
      conditionalLogic: field.conditionalLogic,
      dependsOn: field.dependsOn,
      showWhen: field.showWhen
    }
  }

  /**
   * 生成唯一ID
   * @returns 唯一ID字符串
   */
  static generateId(): string {
    return Date.now().toString(36) + Math.random().toString(36).substr(2, 9)
  }

  /**
   * 验证表单配置是否有效
   * @param config 表单配置
   * @returns 验证结果
   */
  static validateFormConfig(config: Partial<FormConfig>): { valid: boolean; errors: string[] } {
    const errors: string[] = []

    if (!config.title || typeof config.title !== 'string') {
      errors.push('表单标题是必需的')
    }

    if (!config.fields || !Array.isArray(config.fields) || config.fields.length === 0) {
      errors.push('表单必须至少包含一个字段')
    } else {
      config.fields.forEach((field, index) => {
        const fieldErrors = this.validateFormField(field)
        fieldErrors.forEach(error => {
          errors.push(`字段 ${index + 1}: ${error}`)
        })
      })
    }

    return {
      valid: errors.length === 0,
      errors
    }
  }

  /**
   * 验证表单字段是否有效
   * @param field 表单字段
   * @returns 验证错误列表
   */
  static validateFormField(field: Partial<FormField>): string[] {
    const errors: string[] = []

    if (!field.name || typeof field.name !== 'string') {
      errors.push('字段名称是必需的')
    }

    if (!field.type || typeof field.type !== 'string') {
      errors.push('字段类型是必需的')
    }

    if (field.type === 'select' || field.type === 'radio' || field.type === 'checkbox') {
      if (!field.options || !Array.isArray(field.options) || field.options.length === 0) {
        errors.push(`${field.type} 类型的字段必须包含选项`)
      }
    }

    return errors
  }

  /**
   * 合并表单配置
   * @param base 基础配置
   * @param override 覆盖配置
   * @returns 合并后的配置
   */
  static mergeFormConfig(base: Partial<FormConfig>, override: Partial<FormConfig>): FormConfig {
    const merged = { ...base }

    // 深度合并字段
    if (override.fields) {
      merged.fields = [...(base.fields || []), ...override.fields]
    }

    // 深度合并设置
    if (override.settings) {
      merged.settings = { ...(base.settings || {}), ...override.settings }
    }

    // 深度合并布局
    if (override.layout) {
      merged.layout = { ...(base.layout || {}), ...override.layout }
    }

    // 合并其他属性
    Object.keys(override).forEach(key => {
      if (key !== 'fields' && key !== 'settings' && key !== 'layout') {
        (merged as any)[key] = (override as any)[key]
      }
    })

    return this.normalizeFormConfig(merged)
  }
}