<template>
  <div class="virtualized-form">
    <el-form
      ref="formRef"
      :model="formData"
      :rules="formRules"
      :label-position="formConfig.labelPosition || 'right'"
      :label-width="formConfig.labelWidth || '120px'"
      :inline="formConfig.layout === 'inline'"
      class="dynamic-form"
    >
      <!-- 虚拟滚动容器 -->
      <div
        ref="scrollContainer"
        class="scroll-container"
        @scroll="handleScroll"
      >
        <!-- 可视区域高度 -->
        <div
          class="scroll-content"
          :style="{ height: `${totalHeight}px` }"
        >
          <!-- 渲染可见区域内的表单项 -->
          <div
            v-for="item in visibleItems"
            :key="item.field.prop"
            class="form-item-container"
            :style="{ transform: `translateY(${item.top}px)` }"
          >
            <FormFormItemRenderer
              :field="item.field"
              :model-value="formData"
              :label-width="formConfig.labelWidth || '120px'"
              :custom-components="customComponents"
              @change="handleFieldChange"
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
            </FormFormItemRenderer>
          </div>
        </div>
      </div>

      <!-- 表单操作按钮 -->
      <FormActionsRenderer
        :submitting="submitting"
        :submit-button-text="formConfig.submitButtonText"
        :reset-button-text="formConfig.resetButtonText"
        :show-reset-button="formConfig.showResetButton"
        @submit="handleSubmit"
        @reset="handleReset"
      >
        <template #actions>
          <slot name="actions" />
        </template>
      </FormActionsRenderer>
    </el-form>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, onUnmounted, nextTick } from 'vue'
import { ElMessage } from 'element-plus'
import type { FormInstance } from 'element-plus'
import type { FormConfig, FormField } from '@/types/form'
import FormFormItemRenderer from './FormFormItemRenderer.vue'
import FormActionsRenderer from './FormActionsRenderer.vue'
import { FormValidationManager } from './FormValidationManager'
import { FormDataManager } from './FormDataManager'

// Props
const props = defineProps<{
  config: FormConfig
  modelValue?: Record<string, any>
  customComponents?: Record<string, any>
  itemHeight?: number // 单个表单项高度
  bufferSize?: number // 缓冲区大小，渲染额外项数
}>()

// Emits
const emit = defineEmits<{
  (e: 'update:modelValue', value: Record<string, any>): void
  (e: 'submit', value: Record<string, any>): void
  (e: 'reset'): void
  (e: 'fieldChange', field: string, value: any): void
}>()

// 配置项
const ITEM_HEIGHT = props.itemHeight || 80 // 单个表单项高度
const BUFFER_SIZE = props.bufferSize || 5 // 缓冲区大小

// 表单引用
const formRef = ref<FormInstance>()
const scrollContainer = ref<HTMLElement>()

// 提交状态
const submitting = ref(false)

// 表单数据
const formData = reactive<Record<string, any>>({})

// 可见字段
const visibleFields = computed(() => {
  return formConfig.value.fields?.filter(field => !field.hidden) || []
})

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

// 表单验证规则
const formRules = computed(() => {
  return FormValidationManager.generateFormRules(formConfig.value.fields)
})

// 虚拟滚动相关
const scrollTop = ref(0)
const containerHeight = ref(0)

// 总高度
const totalHeight = computed(() => {
  return visibleFields.value.length * ITEM_HEIGHT
})

// 可见区域
const visibleRange = computed(() => {
  const start = Math.floor(scrollTop.value / ITEM_HEIGHT)
  const end = Math.min(
    start + Math.ceil(containerHeight.value / ITEM_HEIGHT),
    visibleFields.value.length
  )
  
  // 扩展范围以包含缓冲区
  const extendedStart = Math.max(0, start - BUFFER_SIZE)
  const extendedEnd = Math.min(
    visibleFields.value.length,
    end + BUFFER_SIZE
  )
  
  return { start: extendedStart, end: extendedEnd }
})

// 可见项
const visibleItems = computed(() => {
  const { start, end } = visibleRange.value
  
  return visibleFields.value.slice(start, end).map((field, index) => ({
    field,
    index: start + index,
    top: (start + index) * ITEM_HEIGHT
  }))
})

// 处理滚动事件
const handleScroll = () => {
  if (!scrollContainer.value) return
  scrollTop.value = scrollContainer.value.scrollTop
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

// 更新容器高度
const updateContainerHeight = () => {
  if (scrollContainer.value) {
    containerHeight.value = scrollContainer.value.clientHeight
  }
}

// 组件挂载时初始化
onMounted(async () => {
  initFormData()
  
  // 等待DOM更新后设置容器高度
  await nextTick()
  updateContainerHeight()
  
  // 监听窗口大小变化
  window.addEventListener('resize', updateContainerHeight)
})

// 组件卸载时清理
onUnmounted(() => {
  window.removeEventListener('resize', updateContainerHeight)
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
.virtualized-form {
  width: 100%;
  height: 100%;
}

.dynamic-form {
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
}

.scroll-container {
  flex: 1;
  overflow-y: auto;
  position: relative;
}

.scroll-content {
  position: relative;
  width: 100%;
}

.form-item-container {
  position: absolute;
  width: 100%;
  height: var(--item-height, 80px);
  box-sizing: border-box;
  padding: 10px 0;
}
</style>