<template>
  <el-date-picker
    :model-value="modelValue"
    :type="field.pickerType || 'datetime'"
    :format="field.format || 'YYYY-MM-DD HH:mm:ss'"
    :placeholder="field.placeholder || `请选择${field.label}`"
    :disabled="disabled"
    :start-placeholder="field.startPlaceholder"
    :end-placeholder="field.endPlaceholder"
    :style="{ width: field.width || '100%' }"
    @update:model-value="$emit('update:modelValue', $event)"
    @change="handleChange"
  />
</template>

<script setup lang="ts">
import { defineEmits } from 'vue'
import { BaseFormFieldProps } from './types'

// Props
const props = defineProps<BaseFormFieldProps>()

// Emits
const emit = defineEmits<{
  (e: 'update:modelValue', value: any): void;
  (e: 'change', value: any): void;
}>()

// 处理值变化
const handleChange = (value: any) => {
  emit('change', value)
}
</script>

<style scoped>
:deep(.el-date-editor) {
  width: 100%;
}
</style>
