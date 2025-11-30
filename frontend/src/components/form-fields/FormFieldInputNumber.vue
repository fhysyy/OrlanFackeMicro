<template>
  <el-input-number
    v-model="internalValue"
    :placeholder="field.placeholder || `请输入${field.label}`"
    :disabled="disabled"
    :min="field.min"
    :max="field.max"
    :step="field.step || 1"
    :precision="field.precision"
    :step-strictly="field.stepStrictly"
    :controls-position="field.controlsPosition"
    :controls="field.controls !== false"
    :validate-event="field.validateEvent !== false"
    :size="field.size"
    :style="{ width: field.width || '100%' }"
    :readonly="field.readonly"
    @blur="handleBlur"
    @focus="handleFocus"
    @change="handleChange"
  />
</template>

<script setup lang="ts">
import { computed, defineProps, defineEmits } from 'vue'
import type { FormField } from '@/types'
import { DatabaseDataType } from '@/types'

// Props
const props = defineProps<{
  field: FormField;
  modelValue: any;
  disabled?: boolean;
}>()

// Emits
const emit = defineEmits<{
  (e: 'update:modelValue', value: any): void;
  (e: 'change', value: any): void;
  (e: 'blur', event: FocusEvent): void;
  (e: 'focus', event: FocusEvent): void;
}>()

// 内部值，用于处理v-model
const internalValue = computed({
  get: () => props.modelValue,
  set: (val) => {
    emit('update:modelValue', val)
  }
})

// 根据数据库类型设置默认精度
const getDefaultPrecision = () => {
  if (props.field.precision !== undefined) {
    return props.field.precision
  }
  
  switch (props.field.databaseType) {
  case DatabaseDataType.DECIMAL:
  case DatabaseDataType.DOUBLE:
  case DatabaseDataType.FLOAT:
    return 2
  default:
    return 0
  }
}

// 处理值变化
const handleChange = (value: any) => {
  emit('change', value)
}

// 处理失焦事件
const handleBlur = (event: FocusEvent) => {
  emit('blur', event)
}

// 处理聚焦事件
const handleFocus = (event: FocusEvent) => {
  emit('focus', event)
}
</script>
