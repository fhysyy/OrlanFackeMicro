<template>
  <el-time-picker
    :model-value="modelValue"
    @update:model-value="$emit('update:modelValue', $event)"
    :type="field.pickerType || 'time'"
    :format="field.format || 'HH:mm:ss'"
    :placeholder="field.placeholder || `请选择${field.label}`"
    :disabled="disabled"
    :start-placeholder="field.startPlaceholder"
    :end-placeholder="field.endPlaceholder"
    :style="{ width: field.width || '100%' }"
    @change="handleChange"
  />
</template>

<script setup lang="ts">
import { defineProps, defineEmits } from 'vue';
import { BaseFormFieldProps } from './types';

// Props
const props = defineProps<BaseFormFieldProps>();

// Emits
const emit = defineEmits<{
  (e: 'update:modelValue', value: any): void;
  (e: 'change', value: any): void;
}>();

// 处理值变化
const handleChange = (value: any) => {
  emit('change', value);
};
</script>

<style scoped>
:deep(.el-time-editor) {
  width: 100%;
}
</style>
