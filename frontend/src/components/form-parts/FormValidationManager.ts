import type { FormInstance } from 'element-plus'
import type { FormConfig, FormField } from '@/types/form'
import { FormFieldType, DatabaseDataType } from '@/types/form'
import { convertValidationRules, generateValidationRulesFromDatabase } from '@/utils/validationUtils'

/**
 * 表单验证管理器
 * 负责处理表单验证规则的生成和验证逻辑
 */
export class FormValidationManager {
  /**
   * 根据字段类型获取验证触发方式
   */
  static getFieldTrigger(type: FormFieldType): string | string[] {
    const blurTriggers = [FormFieldType.INPUT, FormFieldType.TEXTAREA, FormFieldType.INPUT_NUMBER]
    const changeTriggers = [
      FormFieldType.SELECT,
      FormFieldType.RADIO,
      FormFieldType.CHECKBOX,
      FormFieldType.SWITCH,
      FormFieldType.DATE_PICKER,
      FormFieldType.TIME_PICKER,
      FormFieldType.DATETIME_PICKER,
      FormFieldType.SLIDER,
      FormFieldType.CASCADER,
      FormFieldType.TREE_SELECT,
      FormFieldType.TRANSFER,
      FormFieldType.RATE
    ]

    if (blurTriggers.includes(type)) {
      return ['blur', 'change']
    } else if (changeTriggers.includes(type)) {
      return 'change'
    }
    return 'change'
  }

  /**
   * 生成表单验证规则
   */
  static generateFormRules(fields: FormField[] | undefined): Record<string, any[]> {
    if (!fields) return {}
    
    const rules: Record<string, any[]> = {}
    
    fields.forEach(field => {
      // 优先使用新的validationRules字段
      if (field.validationRules && field.validationRules.length > 0) {
        rules[field.prop] = convertValidationRules(field.validationRules, this.getFieldTrigger(field.type))
      }
      // 兼容旧的rules字段
      else if (field.rules && field.rules.length > 0) {
        rules[field.prop] = field.rules
      }
      // 如果有数据库配置，生成验证规则
      else if (field.databaseType) {
        const dbRules = generateValidationRulesFromDatabase({
          type: field.databaseType,
          length: field.databaseLength,
          required: field.required
        })
        if (dbRules.length > 0) {
          rules[field.prop] = dbRules.map(rule => ({
            ...rule,
            trigger: this.getFieldTrigger(field.type)
          }))
        }
      }
      // 仅处理必填字段
      else if (field.required) {
        rules[field.prop] = [
          {
            required: true,
            message: `请输入${field.label}`,
            trigger: this.getFieldTrigger(field.type)
          }
        ]
      }
    })
    
    return rules
  }

  /**
   * 验证单个字段
   */
  static async validateField(formRef: FormInstance, field: string): Promise<boolean> {
    if (!formRef) return false
    
    try {
      await formRef.validateField(field)
      return true
    } catch (error) {
      return false
    }
  }

  /**
   * 验证整个表单
   */
  static async validateForm(formRef: FormInstance): Promise<boolean> {
    if (!formRef) return false
    
    return new Promise((resolve) => {
      formRef.validate((valid) => {
        resolve(valid)
      })
    })
  }
}