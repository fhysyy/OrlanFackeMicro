<template>
  <el-form-item
    :label="field.label"
    :prop="field.prop"
    :rules="field.rules"
    :required="field.required"
    :help="field.helpText"
    :label-width="labelWidth"
    class="form-item-wrapper"
  >
    <!-- 使用动态组件渲染不同类型的表单字段 -->
    <component
      :is="getFieldComponent(field.type)"
      v-model="modelValue[field.prop]"
      :field="field"
      :disabled="field.disabled"
      :custom-props="componentProps"
      @change="$emit('change', field.prop, $event)"
    />

    <!-- 插槽：自定义字段内容 -->
    <slot
      :name="`field-${field.prop}`"
      :field="field"
      :value="modelValue[field.prop]"
    />
  </el-form-item>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { FormField } from '@/types/form'
import { FormFieldType } from '@/types/form'
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
  FormFieldTime
} from './form-fields'

// Props
const props = defineProps<{
  field: FormField
  modelValue: Record<string, any>
  labelWidth?: string
  customComponents?: Record<string, any>
}>()

// Emits
const emit = defineEmits<{
  (e: 'update:modelValue', value: Record<string, any>): void
  (e: 'change', field: string, value: any): void
}>()

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

// 计算组件属性
const componentProps = computed(() => {
  const baseProps = {
    placeholder: props.field.placeholder,
    disabled: props.field.disabled
  }
  
  // 合并字段特定的属性
  return { ...baseProps, ...props.field.componentProps }
})
</script>

<style scoped>
.form-item-wrapper {
  margin-bottom: 0;
}
</style>