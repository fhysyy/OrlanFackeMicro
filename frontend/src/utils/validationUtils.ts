import type { ValidationRule } from '../types/form';
import type { FormItemRule } from 'element-plus';

/**
 * 常用正则表达式
 */
export const regexPatterns = {
  // 邮箱正则
  email: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
  // 手机号正则（中国大陆）
  phone: /^1[3-9]\d{9}$/,
  // 身份证号正则（中国大陆18位）
  idcard: /^[1-9]\d{5}(19|20)\d{2}(0[1-9]|1[0-2])(0[1-9]|[12]\d|3[01])\d{3}[\dXx]$/,
  // 不含中文正则
  noChinese: /^[^\u4e00-\u9fa5]*$/,
  // 包含数字正则
  hasNumber: /\d/,
  // 包含特殊字符正则
  hasSpecialChar: /[!@#$%^&*(),.?":{}|<>]/
};

/**
 * 验证函数集合
 */
export const validators = {
  /**
   * 验证必填
   */
  required: (value: any): boolean => {
    if (value === null || value === undefined) return false;
    if (typeof value === 'string') return value.trim() !== '';
    if (Array.isArray(value)) return value.length > 0;
    return true;
  },

  /**
   * 验证最小长度
   */
  min: (value: string, min: number): boolean => {
    return value.length >= min;
  },

  /**
   * 验证最大长度
   */
  max: (value: string, max: number): boolean => {
    return value.length <= max;
  },

  /**
   * 验证固定长度
   */
  len: (value: string, len: number): boolean => {
    return value.length === len;
  },

  /**
   * 验证邮箱格式
   */
  email: (value: string): boolean => {
    return regexPatterns.email.test(value);
  },

  /**
   * 验证手机号格式（中国大陆）
   */
  phone: (value: string): boolean => {
    return regexPatterns.phone.test(value);
  },

  /**
   * 验证身份证号格式（中国大陆18位）
   */
  idcard: (value: string): boolean => {
    return regexPatterns.idcard.test(value);
  },

  /**
   * 验证不含中文
   */
  noChinese: (value: string): boolean => {
    return regexPatterns.noChinese.test(value);
  },

  /**
   * 验证包含数字
   */
  hasNumber: (value: string): boolean => {
    return regexPatterns.hasNumber.test(value);
  },

  /**
   * 验证包含特殊字符
   */
  hasSpecialChar: (value: string): boolean => {
    return regexPatterns.hasSpecialChar.test(value);
  },

  /**
   * 验证正则表达式
   */
  pattern: (value: string, pattern: RegExp): boolean => {
    return pattern.test(value);
  }
};

/**
 * 将可视化验证规则转换为Element Plus的表单验证规则
 */
export function convertValidationRules(validationRules?: ValidationRule[]): FormItemRule[] {
  if (!validationRules || validationRules.length === 0) {
    return [];
  }

  return validationRules.map(rule => {
    const formRule: FormItemRule = {
      message: rule.message,
      trigger: rule.trigger || 'blur'
    };

    switch (rule.type) {
      case 'required':
        formRule.required = true;
        break;
      case 'min':
        formRule.min = rule.value;
        formRule.type = 'string';
        break;
      case 'max':
        formRule.max = rule.value;
        formRule.type = 'string';
        break;
      case 'len':
        formRule.len = rule.value;
        formRule.type = 'string';
        break;
      case 'email':
        formRule.type = 'email';
        break;
      case 'pattern':
        formRule.pattern = rule.value;
        break;
      default:
        // 对于自定义验证规则，使用validator函数
        formRule.validator = (rule: any, value: any, callback: any) => {
          if (!value && rule.type !== 'required') {
            callback();
            return;
          }

          const isValid = validators[rule.type as keyof typeof validators](value, rule.value);
          if (isValid) {
            callback();
          } else {
            callback(new Error(rule.message));
          }
        };
    }

    return formRule;
  });
}

/**
 * 根据数据库类型和长度生成默认验证规则
 */
export function generateValidationRulesFromDatabase(
  databaseType: string,
  length?: number,
  precision?: number,
  scale?: number
): ValidationRule[] {
  const rules: ValidationRule[] = [];

  // 为所有字段添加必填验证（可选）
  // rules.push({
  //   type: 'required',
  //   message: '该字段为必填项',
  //   trigger: 'blur'
  // });

  switch (databaseType) {
    case 'varchar':
    case 'text':
      if (length) {
        rules.push({
          type: 'max',
          value: length,
          message: `最多允许输入${length}个字符`,
          trigger: 'blur'
        });
      }
      break;

    case 'int':
    case 'bigint':
      rules.push({
        type: 'pattern',
        value: /^\d*$/,
        message: '请输入有效的整数',
        trigger: 'blur'
      });
      break;

    case 'decimal':
    case 'float':
    case 'double':
      const numberRegex = scale !== undefined ? 
        new RegExp(`^\d+(\.\d{0,${scale}})?$`) : 
        /^\d+(\.\d*)?$/;
      
      rules.push({
        type: 'pattern',
        value: numberRegex,
        message: `请输入有效的数字${scale ? `（最多${scale}位小数）` : ''}`,
        trigger: 'blur'
      });
      break;

    case 'boolean':
      // 布尔类型不需要特殊验证规则
      break;

    case 'date':
      rules.push({
        type: 'pattern',
        value: /^\d{4}-\d{2}-\d{2}$/,
        message: '请输入有效的日期格式（YYYY-MM-DD）',
        trigger: 'blur'
      });
      break;

    case 'datetime':
    case 'timestamp':
      rules.push({
        type: 'pattern',
        value: /^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}$/,
        message: '请输入有效的日期时间格式（YYYY-MM-DD HH:MM:SS）',
        trigger: 'blur'
      });
      break;
  }

  return rules;
}

/**
 * 获取常用验证规则模板
 */
export function getValidationRuleTemplates(): { [key: string]: ValidationRule } {
  return {
    required: {
      type: 'required',
      message: '该字段为必填项',
      trigger: 'blur'
    },
    email: {
      type: 'email',
      message: '请输入有效的邮箱地址',
      trigger: 'blur'
    },
    phone: {
      type: 'phone',
      message: '请输入有效的手机号',
      trigger: 'blur'
    },
    idcard: {
      type: 'idcard',
      message: '请输入有效的身份证号',
      trigger: 'blur'
    },
    noChinese: {
      type: 'noChinese',
      message: '该字段不允许包含中文',
      trigger: 'blur'
    },
    hasNumber: {
      type: 'hasNumber',
      message: '该字段必须包含数字',
      trigger: 'blur'
    },
    hasSpecialChar: {
      type: 'hasSpecialChar',
      message: '该字段必须包含特殊字符',
      trigger: 'blur'
    }
  };
}
