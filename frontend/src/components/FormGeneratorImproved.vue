<template>
  <FormErrorHandler
    :form-data="formData"
    :show-error-notification="showErrorNotification"
    :enable-auto-recovery="enableAutoRecovery"
    @recover="handleRecover"
    @reset="handleReset"
    @error="handleError"
  >
    <!-- 根据字段数量选择使用虚拟化表单或普通表单 -->
    <VirtualizedForm
      v-if="useVirtualization"
      ref="formRef"
      :config="formConfig"
      :model-value="formData"
      :custom-components="customComponents"
      :item-height="itemHeight"
      :buffer-size="bufferSize"
      @update:model-value="handleModelValueUpdate"
      @submit="handleSubmit"
      @reset="handleReset"
      @field-change="handleFieldChange"
    >
      <template
        v-for="(_, name) in $slots"
        #[name]="slotData"
      >
        <slot
          :name="name"
          v-bind="slotData"
        />
      </template>
    </VirtualizedForm>
    
    <el-form
      v-else
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
            <FormFormItemRenderer
              :field="item"
              :model-value="formData"
              :label-width="formConfig.labelWidth || '80px'"
              :custom-components="customComponents"
              @change="handleFieldChange"
            >
              <template
                v-for="(_, name) in $slots"
                #[name]="slotData"
              >
                <slot
                  :name="`field-${item.prop}`"
                  v-bind="slotData"
                />
              </template>
            </FormFormItemRenderer>
          </template>
        </FormLayout>
      </template>

      <!-- 表单操作按钮 -->
      <FormActionsRenderer
        :submitting="submitting"
        :submit-button-text="t('form.submit')"
        :reset-button-text="t('form.reset')"
        :show-reset-button="formConfig.showResetButton"
        @submit="handleSubmit"
        @reset="handleReset"
      >
        <template #actions>
          <slot name="actions" />
        </template>
      </FormActionsRenderer>
    </el-form>
  </FormErrorHandler>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted, onUnmounted } from 'vue'
import type { FormInstance } from 'element-plus'
import type { FormConfig, FormField } from '@/types/form'
import { t } from './form-parts/FormI18n'
import FormErrorHandler from './form-parts/FormErrorHandler.vue'
import FormFormItemRenderer from './form-parts/FormFormItemRenderer.vue'
import FormActionsRenderer from './form-parts/FormActionsRenderer.vue'
import VirtualizedForm from './form-parts/VirtualizedForm.vue'
import { FormValidationManager } from './form-parts/FormValidationManager'
import { FormDataManager } from './form-parts/FormDataManager'
import { FormLayout } from './form-fields'

// Props
const props = defineProps<{
  config: FormConfig
  modelValue?: Record<string, any>
  customComponents?: Record<string, any>
  showErrorNotification?: boolean
  enableAutoRecovery?: boolean
  useVirtualization?: boolean
  virtualizationThreshold?: number // 超过此数量的字段启用虚拟化
  itemHeight?: number // 虚拟化时单个表单项高度
  bufferSize?: number // 虚拟化时缓冲区大小
}>()

// Emits
const emit = defineEmits<{
  (e: 'update:modelValue', value: Record<string, any>): void
  (e: 'submit', value: Record<string, any>): void
  (e: 'reset'): void
  (e: 'fieldChange', field: string, value: any): void
  (e: 'error', error: Error, formData: Record<string, any>): void
}>()

// 表单引用
const formRef = ref<FormInstance>()

// 提交状态
const submitting = ref(false)

// 表单数据
const formData = reactive<Record<string, any>>({})

// 是否显示错误通知
const showErrorNotification = computed(() => props.showErrorNotification !== false)

// 是否启用自动恢复
const enableAutoRecovery = computed(() => props.enableAutoRecovery !== false)

// 表单配置
const formConfig = computed(() => ({
  fields: [],
  labelPosition: 'right',
  labelWidth: '120px',
  layout: '',
  showSubmitButton: false,
  showResetButton: false,
  ...props.config
}))

// 可见字段
const visibleFields = computed(() => {
  return formConfig.value.fields?.filter(field => !field.hidden) || []
})

// 表单验证规则
const formRules = computed(() => {
  return FormValidationManager.generateFormRules(formConfig.value.fields)
})

// 是否使用虚拟化
const useVirtualization = computed(() => {
  return (
    props.useVirtualization !== false &&
    visibleFields.value.length >= (props.virtualizationThreshold || 50)
  )
})

// 处理模型值更新
const handleModelValueUpdate = (value: Record<string, any>) => {
  Object.assign(formData, value)
  emit('update:modelValue', { ...formData })
}

// 处理字段值变化
const handleFieldChange = (field: string, value: any) => {
  // 获取当前字段对象
  const currentField = formConfig.value.fields?.find(f => f.prop === field)
  if (currentField) {
    // 使用安全克隆的字段对象
    const safeField = FormDataManager.safeCloneField(currentField)
    emit('fieldChange', safeField, value)
  } else {
    // 如果找不到字段，仍然发出事件（保持兼容性）
    emit('fieldChange', field, value)
  }
  
  // 更新表单数据
  Object.assign(formData, FormDataManager.updateFormData(formData, field, value))
  
  // 检查formData是否存在循环引用
  if (FormDataManager.hasCircularReference(formData)) {
    console.warn('检测到循环引用，尝试修复...')
    const safeFormData = FormDataManager.safeClone(formData)
    emit('update:modelValue', safeFormData)
  } else {
    emit('update:modelValue', { ...formData })
  }
  
  // 如果字段有验证规则，触发验证
  if (currentField && currentField.validationRules?.length) {
    FormValidationManager.validateField(formRef.value!, field)
  }
}

// 处理错误
const handleError = (error: Error, data: Record<string, any>) => {
  emit('error', error, data)
}

// 处理恢复
const handleRecover = (data: Record<string, any>) => {
  Object.assign(formData, data)
  emit('update:modelValue', { ...formData })
}

// 提交表单
const handleSubmit = async () => {
  if (!formRef.value) return

  try {
    const isValid = await FormValidationManager.validateForm(formRef.value)
    if (!isValid) return

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
const handleReset = () => {
  if (formRef.value) {
    formRef.value.resetFields()
  }
  
  // 重新初始化表单数据
  Object.assign(
    formData,
    FormDataManager.initFormData(
      formConfig.value.fields,
      undefined,
      formConfig.value.initialData
    )
  )
  
  // 调用自定义重置函数
  if (formConfig.value.onReset) {
    formConfig.value.onReset()
  }
  
  // 触发重置事件
  emit('reset')
}

// 设置表单数据
const setFormData = (data: Record<string, any>) => {
  Object.assign(formData, data)
  emit('update:modelValue', { ...formData })
}

// 初始化表单数据
const initFormData = () => {
  Object.assign(
    formData,
    FormDataManager.initFormData(
      formConfig.value.fields,
      props.modelValue,
      formConfig.value.initialData
    )
  )
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
        if (FormDataManager.hasCircularReference(newValue)) {
          console.warn('接收到的modelValue存在循环引用，尝试修复...')
          const safeNewValue = FormDataManager.safeClone(newValue)
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
  validate: () => FormValidationManager.validateForm(formRef.value!)
})
</script>

<style scoped>
.dynamic-form {
  width: 100%;
}
</style>