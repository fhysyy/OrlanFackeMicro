<template>
  <div class="form-generator">
    <el-form
      ref="formRef"
      :model="formData"
      :rules="formRules"
      :label-position="formConfig.labelPosition || 'right'"
      :label-width="formConfig.labelWidth || '120px'"
      :inline="formConfig.layout === 'inline'"
      class="dynamic-form"
    >
      <template v-if="formConfig.fields">
        <!-- 使用表单布局组件渲染字段 -->
        <FormLayout
          :layout="formConfig.layout || 'grid'"
          :gutter="formConfig.gutter || 20"
          :xs="formConfig.xs || 12"
          :sm="formConfig.sm || 8"
          :md="formConfig.md || 6"
          :lg="formConfig.lg || 6"
          :xl="formConfig.xl || 6"
          :min-col-width="'100px'"
          :children="visibleFields"
        >
          <template #item-="{ item, index }">
            <el-form-item
              :label="item.label"
              :prop="item.prop"
              :rules="item.rules"
              :required="item.required"
              :help="item.helpText"
              :label-width="formConfig.labelWidth || '80px'"
              class="form-item-wrapper"
            >
              <!-- 使用动态组件渲染不同类型的表单字段 -->
              <component
                :is="getFieldComponent(item.type)"
                v-model="formData[item.prop]"
                :field="item"
                :disabled="item.disabled"
                :custom-props="getComponentProps(item)"
                @change="(val) => handleFieldChange(item.prop, val)"
              />

              <!-- 插槽：自定义字段内容 -->
              <slot
                :name="`field-${item.prop}`"
                :field="item"
                :value="formData[item.prop]"
              />
            </el-form-item>
          </template>
        </FormLayout>
      </template>

      <!-- 表单操作按钮 -->
      <el-form-item v-if="showActions">
        <el-button
          type="primary"
          :loading="submitting"
          @click="handleSubmit"
        >
          {{ formConfig.submitButtonText || '提交' }}
        </el-button>
        <el-button
          v-if="formConfig.showResetButton !== false"
          @click="handleReset"
        >
          {{ formConfig.resetButtonText || '重置' }}
        </el-button>
      </el-form-item>
    </el-form>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import type { FormInstance } from 'element-plus'
import type { FormConfig, FormField, ValidationRule } from '@/types/form'
import { FormFieldType, DatabaseDataType } from '@/types/form'
import { convertValidationRules, generateValidationRulesFromDatabase } from '@/utils/validationUtils'
import {
  FormFieldInput,
  FormFieldTextarea,
  FormFieldSelect,
  FormFieldRadio,
  FormFieldCheckbox,
  FormFieldSwitch,
  FormFieldDatePicker,
  FormFieldTimePicker,
  FormFieldDatetimePicker,
  FormFieldInputNumber,
  FormFieldSlider,
  FormFieldRate,
  FormFieldUpload,
  FormFieldTime,
  FormLayout
} from './form-fields'

// Props
const props = defineProps<{
  config: FormConfig;
  modelValue?: Record<string, any>;
  customComponents?: Record<string, any>;
}>()

// Emits
const emit = defineEmits<{
  (e: 'update:modelValue', value: Record<string, any>): void;
  (e: 'submit', value: Record<string, any>): void;
  (e: 'reset'): void;
  (e: 'fieldChange', field: string, value: any): void;
}>()

// 表单引用
const formRef = ref<FormInstance>()

// 提交状态
const submitting = ref(false)

// 表单数据
const formData = reactive<Record<string, any>>({})

// 可见字段
const visibleFields = computed(() => {
  return formConfig.value.fields?.filter(field => !field.hidden) || []
})

// 组件映射
const componentMap = {
  [FormFieldType.INPUT]: FormFieldInput,
  [FormFieldType.TEXTAREA]: FormFieldTextarea,
  [FormFieldType.SELECT]: FormFieldSelect,
  [FormFieldType.RADIO]: FormFieldRadio,
  [FormFieldType.CHECKBOX]: FormFieldCheckbox,
  [FormFieldType.SWITCH]: FormFieldSwitch,
  [FormFieldType.DATE_PICKER]: FormFieldDatePicker,
  [FormFieldType.TIME_PICKER]: FormFieldTimePicker,
  [FormFieldType.DATETIME_PICKER]: FormFieldDatetimePicker,
  [FormFieldType.INPUT_NUMBER]: FormFieldInputNumber,
  [FormFieldType.SLIDER]: FormFieldSlider,
  [FormFieldType.RATE]: FormFieldRate,
  [FormFieldType.UPLOAD]: FormFieldUpload,
  [FormFieldType.TIME]: FormFieldTime
}

// 获取字段组件
function getFieldComponent(type: FormFieldType) {
  return componentMap[type] || props.customComponents?.[type] || 'div'
}

// 表单配置计算属性 - 确保始终有默认值
const formConfig = computed(() => ({
  fields: [],
  labelPosition: 'right',
  labelWidth: '120px',
  layout: '',
  showSubmitButton: false,
  showResetButton: false,
  ...props.config
}))

// 表单规则
const formRules = computed(() => {
  const rules: Record<string, any[]> = {}
  formConfig.value.fields?.forEach(field => {
    // 优先使用新的validationRules字段
    if (field.validationRules && field.validationRules.length > 0) {
      rules[field.prop] = convertValidationRules(field.validationRules, getFieldTrigger(field.type))
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
          trigger: getFieldTrigger(field.type)
        }))
      }
    }
    // 仅处理必填字段
    else if (field.required) {
      rules[field.prop] = [
        {
          required: true,
          message: `请输入${field.label}`,
          trigger: getFieldTrigger(field.type)
        }
      ]
    }
  })
  return rules
})

// 是否显示操作按钮
const showActions = computed(() => {
  return formConfig.value.showSubmitButton !== false || formConfig.value.showResetButton !== false
})

// 初始化表单数据
function initFormData() {
  // 清空现有数据
  Object.keys(formData).forEach(key => {
    delete formData[key]
  })

  // 设置初始值
  if (formConfig.value.fields) {
    formConfig.value.fields.forEach(field => {
      // 优先使用传入的modelValue
      if (props.modelValue && props.modelValue[field.prop] !== undefined) {
        formData[field.prop] = props.modelValue[field.prop]
      } else if (field.value !== undefined) {
        // 其次使用字段配置的value
        formData[field.prop] = field.value
      } else if (formConfig.value.initialData && formConfig.value.initialData[field.prop] !== undefined) {
        // 最后使用表单配置的initialData
        formData[field.prop] = formConfig.value.initialData[field.prop]
      } else {
        // 设置默认值
        formData[field.prop] = getDefaultValueByType(field.type)
      }
    })
  }
}

// 根据字段类型获取默认值
function getDefaultValueByType(type: FormFieldType) {
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

// 获取字段触发方式
function getFieldTrigger(type: FormFieldType): string | string[] {
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

// 获取组件属性
function getComponentProps(field: FormField) {
  const baseProps = {
    placeholder: field.placeholder,
    disabled: field.disabled
  }
  
  // 根据数据库类型添加特定属性
  if (field.databaseType) {
    switch (field.databaseType) {
    case DatabaseDataType.VARCHAR:
    case DatabaseDataType.CHAR:
      if (field.databaseLength) {
        baseProps.maxlength = field.databaseLength
        baseProps.showWordLimit = true
      }
      break
    case DatabaseDataType.TEXT:
    case DatabaseDataType.MEDIUMTEXT:
    case DatabaseDataType.LONGTEXT:
      if (field.type === FormFieldType.TEXTAREA && field.databaseLength) {
        baseProps.maxlength = field.databaseLength
        baseProps.showWordLimit = true
      }
      break
    case DatabaseDataType.INT:
    case DatabaseDataType.SMALLINT:
    case DatabaseDataType.TINYINT:
    case DatabaseDataType.BIGINT:
      if (field.type === FormFieldType.INPUT_NUMBER) {
        baseProps.integer = true
      }
      break
    }
  }
  
  // 合并字段特定的属性
  return { ...baseProps, ...field.componentProps }
}

// 格式化文件大小
function formatFileSize(bytes: number): string {
  if (bytes < 1024) return bytes + ' B'
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(2) + ' KB'
  return (bytes / (1024 * 1024)).toFixed(2) + ' MB'
}

// 处理文件上传前
function handleBeforeUpload(file: File, field: FormField): boolean | Promise<File> {
  // 检查文件大小
  if (field.fileSize && file.size > field.fileSize) {
    ElMessage.error(`文件大小不能超过${formatFileSize(field.fileSize)}`)
    return false
  }

  // 调用自定义上传前钩子
  if (field.beforeUpload) {
    return field.beforeUpload(file)
  }
  
  return true
}

// 处理文件上传成功
function handleUploadSuccess(response: any, file: any, field: FormField) {
  if (field.onSuccess) {
    field.onSuccess(response, file)
  }
  handleFieldChange(field.prop, formData[field.prop])
}

// 处理文件上传失败
function handleUploadError(error: any, file: any, field: FormField) {
  if (field.onError) {
    field.onError(error, file)
  } else {
    ElMessage.error('文件上传失败')
  }
}

// 处理文件移除
function handleFileRemove(file: any, fileList: any[]) {
  // 可以在这里添加自定义逻辑
}

// 检测循环引用的辅助函数
function hasCircularReference(obj: any, seen = new Set()): boolean {
  if (obj === null || typeof obj !== 'object') {
    return false
  }
  
  if (seen.has(obj)) {
    return true
  }
  
  seen.add(obj)
  
  for (const key in obj) {
    if (Object.prototype.hasOwnProperty.call(obj, key)) {
      if (hasCircularReference(obj[key], seen)) {
        return true
      }
    }
  }
  
  seen.delete(obj)
  return false
}

// 安全克隆字段对象，只保留业务属性
const safeCloneField = (field: FormField): FormField => {
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

// 处理字段值变化
function handleFieldChange(field: string, value: any) {
  // 获取当前字段对象
  const currentField = formConfig.value.fields?.find(f => f.prop === field)
  if (currentField) {
    // 使用安全克隆的字段对象
    const safeField = safeCloneField(currentField)
    emit('fieldChange', safeField, value)
  } else {
    // 如果找不到字段，仍然发出事件（保持兼容性）
    emit('fieldChange', field, value)
  }
  
  // 检查formData是否存在循环引用
  if (hasCircularReference(formData)) {
    console.warn('检测到循环引用，尝试修复...')
    // 创建一个不包含循环引用的新对象
    const safeFormData = JSON.parse(JSON.stringify(formData, (key, value) => {
      // 处理可能导致循环引用的特殊属性
      if (key === 'fieldValue' && typeof value === 'object' && value !== null) {
        return { ...value }
      }
      return value
    }))
    emit('update:modelValue', safeFormData)
  } else {
    emit('update:modelValue', { ...formData })
  }
  
  // 如果字段有验证规则，触发验证
  if (currentField && currentField.validationRules?.length) {
    validateField(field)
  }
}

// 验证单个字段
async function validateField(field: string) {
  if (!formRef.value) return
  
  try {
    await formRef.value.validateField(field)
    return true
  } catch (error) {
    return false
  }
}

// 提交表单
async function handleSubmit() {
  if (!formRef.value) return

  try {
    await formRef.value.validate()
    submitting.value = true

    // 调用自定义提交函数
    if (formConfig.value.onSubmit) {
      await formConfig.value.onSubmit(formData)
    }

    // 触发提交事件
    emit('submit', { ...formData })
  } catch (error) {
    console.error('表单验证失败:', error)
  } finally {
    submitting.value = false
  }
}

// 重置表单
function handleReset() {
  if (formRef.value) {
    formRef.value.resetFields()
  }
  initFormData()
  
  // 调用自定义重置函数
  if (formConfig.value.onReset) {
    formConfig.value.onReset()
  }
  
  // 触发重置事件
  emit('reset')
}

// 设置表单数据
function setFormData(data: Record<string, any>) {
  Object.assign(formData, data)
  emit('update:modelValue', { ...formData })
}

// 验证表单
function validate(): Promise<boolean> {
  return new Promise((resolve) => {
    if (!formRef.value) {
      resolve(false)
      return
    }

    formRef.value.validate((valid) => {
      resolve(valid)
    })
  })
}

// 监听配置变化，重新初始化表单
watch(
  () => props.config,
  () => {
    initFormData()
  },
  { deep: true }
)

// 监听modelValue变化
watch(
  () => props.modelValue,
  (newValue) => {
    if (newValue) {
      try {
        // 检查newValue是否存在循环引用
        if (hasCircularReference(newValue)) {
          console.warn('接收到的modelValue存在循环引用，尝试修复...')
          // 创建一个不包含循环引用的新对象
          const safeNewValue = JSON.parse(JSON.stringify(newValue, (key, value) => {
            // 处理可能导致循环引用的特殊属性
            if (key === 'fieldValue' && typeof value === 'object' && value !== null) {
              return { ...value }
            }
            return value
          }))
          Object.assign(formData, safeNewValue)
        } else {
          Object.assign(formData, newValue)
        }
      } catch (error) {
        console.error('处理modelValue变化时出错:', error)
        // 使用深拷贝工具来避免循环引用
        Object.keys(formData).forEach(key => {
          delete formData[key]
        })
        // 逐个属性赋值，避免直接引用
        for (const [key, value] of Object.entries(newValue)) {
          if (typeof value === 'object' && value !== null) {
            formData[key] = Array.isArray(value) ? [...value] : { ...value }
          } else {
            formData[key] = value
          }
        }
      }
    }
  },
  { deep: true }
)

// 组件挂载时初始化
onMounted(() => {
  initFormData()
})

// 暴露方法给父组件
defineExpose({
  formRef,
  formData,
  handleSubmit,
  handleReset,
  setFormData,
  validate
})
</script>

<style scoped>
.form-generator {
  width: 100%;
}

.dynamic-form {
  width: 100%;
}

.form-row {
  margin-bottom: 12px;
}

.form-item-wrapper {
  margin-bottom: 0;
}

.form-item-hidden {
  display: none;
}

.radio-group-wrapper,
.checkbox-group-wrapper {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
}

.radio-group-horizontal .el-radio,
.checkbox-group-horizontal .el-checkbox {
  margin-right: 15px;
  margin-bottom: 4px;
}

/* 优化标签和输入框的对齐 */
:deep(.el-form-item__label) {
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

:deep(.el-form-item__content) {
  display: flex;
  flex-direction: column;
}

/* 确保输入控件在容器内正确对齐 */
:deep(.el-input),
:deep(.el-select),
:deep(.el-date-editor),
:deep(.el-time-editor) {
  width: 100%;
}
</style>