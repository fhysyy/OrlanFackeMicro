import type { FormConfig, FormField } from '@/types/form'
import { FormFieldType } from '@/types/form'

/**
 * 表单数据管理器
 * 负责表单数据的初始化、更新和重置
 */
export class FormDataManager {
  /**
   * 根据字段类型获取默认值
   */
  static getDefaultValueByType(type: FormFieldType): any {
    switch (type) {
      case FormFieldType.CHECKBOX:
      case FormFieldType.TRANSFER:
      case FormFieldType.SELECT:
        return []
      case FormFieldType.SWITCH:
        return false
      case FormFieldType.INPUT_NUMBER:
      case FormFieldType.RATE:
      case FormFieldType.SLIDER:
        return 0
      case FormFieldType.DATE_PICKER:
      case FormFieldType.DATETIME_PICKER:
        return null
      case FormFieldType.TIME_PICKER:
      case FormFieldType.TIME:
        return null
      default:
        return ''
    }
  }

  /**
   * 初始化表单数据
   */
  static initFormData(
    fields: FormField[] | undefined,
    modelValue: Record<string, any> | undefined,
    initialData: Record<string, any> | undefined
  ): Record<string, any> {
    const formData: Record<string, any> = {}
    
    if (!fields) return formData
    
    fields.forEach(field => {
      // 优先使用传入的modelValue
      if (modelValue && modelValue[field.prop] !== undefined) {
        formData[field.prop] = modelValue[field.prop]
      } else if (field.value !== undefined) {
        // 其次使用字段配置的value
        formData[field.prop] = field.value
      } else if (initialData && initialData[field.prop] !== undefined) {
        // 最后使用表单配置的initialData
        formData[field.prop] = initialData[field.prop]
      } else {
        // 设置默认值
        formData[field.prop] = this.getDefaultValueByType(field.type)
      }
    })
    
    return formData
  }

  /**
   * 更新表单数据
   */
  static updateFormData(
    formData: Record<string, any>,
    field: string,
    value: any
  ): Record<string, any> {
    return {
      ...formData,
      [field]: value
    }
  }

  /**
   * 检测循环引用
   */
  static hasCircularReference(obj: any, seen = new Set()): boolean {
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
    
    seen.delete(obj)
    return false
  }

  /**
   * 安全地克隆对象，避免循环引用
   */
  static safeClone(obj: any): any {
    if (obj === null || typeof obj !== 'object') {
      return obj
    }
    
    // 如果是数组
    if (Array.isArray(obj)) {
      const clonedArray: any[] = []
      for (let i = 0; i < obj.length; i++) {
        clonedArray[i] = this.safeClone(obj[i])
      }
      return clonedArray
    }
    
    // 如果是对象
    const clonedObj: Record<string, any> = {}
    for (const key in obj) {
      if (Object.prototype.hasOwnProperty.call(obj, key)) {
        // 跳过可能导致循环引用的属性
        if (key === 'fieldValue' && typeof obj[key] === 'object') {
          continue
        }
        clonedObj[key] = this.safeClone(obj[key])
      }
    }
    return clonedObj
  }

  /**
   * 安全克隆字段对象，只保留业务属性
   */
  static safeCloneField(field: FormField): FormField {
    // 定义需要保留的业务属性列表
    const businessProps = [
      'prop', 'label', 'type', 'databaseType', 'databaseLength', 'databasePrecision', 
      'databaseScale', 'value', 'placeholder', 'disabled', 'hidden', 'width', 
      'rules', 'validationRules', 'helpText', 'prefixIcon', 'suffixIcon', 
      'className', 'required', 'span', 'description', 'fieldDescription', 
      'searchable', 'sortable', 'inputType', 'maxlength', 'clearable', 
      'showWordLimit', 'options', 'multiple', 'filterable', 'remote', 
      'remoteMethod', 'loading', 'labelKey', 'valueKey', 'layout', 
      'pickerType', 'format', 'startPlaceholder', 'endPlaceholder', 'action', 
      'accept', 'fileSize', 'min', 'max', 'step', 'showInput', 'range', 
      'precision', 'rows', 'autosize', 'props', 'showPath', 'data', 'titles', 
      'renderContent', 'max', 'allowHalf', 'allowClear'
    ]
    
    // 创建只包含业务属性的新对象
    const clonedField = {} as FormField
    
    // 只复制业务属性
    for (const prop of businessProps) {
      if (prop in field) {
        try {
          // 尝试安全复制属性值
          clonedField[prop as keyof FormField] = JSON.parse(JSON.stringify((field as any)[prop]))
        } catch (e) {
          // 如果复制失败，使用默认值
          console.warn(`无法安全复制字段 ${field.prop} 的属性 ${prop}，使用默认值`)
          clonedField[prop as keyof FormField] = null
        }
      }
    }
    
    return clonedField
  }
}