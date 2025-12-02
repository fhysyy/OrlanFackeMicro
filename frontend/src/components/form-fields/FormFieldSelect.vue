<template>
  <el-select
    :model-value="modelValue"
    :placeholder="field.placeholder || `请选择${field.label}`"
    :disabled="disabled"
    :multiple="field.multiple"
    :filterable="field.filterable"
    :clearable="field.clearable"
    :remote="field.remote"
    :remote-method="field.remoteMethod"
    :loading="field.loading"
    :style="{ width: field.width || '100%' }"
    @update:model-value="$emit('update:modelValue', $event)"
    @change="handleChange"
  >
    <el-option
      v-for="option in field.options"
      :key="option[field.valueKey || 'value']"
      :label="option[field.labelKey || 'label']"
      :value="option[field.valueKey || 'value']"
      :disabled="option.disabled"
    />
  </el-select>
</template>

<script setup lang="ts">
import { defineEmits } from 'vue'
import { BaseFormFieldProps } from './types'

// Props
const props = defineProps<BaseFormFieldProps & {
  modelValue: any;
}>()

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
:deep(.el-select) {
  width: 100%;
}
</style>
