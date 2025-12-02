<template>
  <el-switch
    :model-value="modelValue"
    :disabled="disabled"
    :active-value="field.activeValue !== undefined ? field.activeValue : true"
    :inactive-value="field.inactiveValue !== undefined ? field.inactiveValue : false"
    :active-text="field.activeText"
    :inactive-text="field.inactiveText"
    @update:model-value="$emit('update:modelValue', $event)"
    @change="handleChange"
  />
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
