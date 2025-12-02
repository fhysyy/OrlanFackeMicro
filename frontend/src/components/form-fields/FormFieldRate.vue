<template>
  <el-rate
    :model-value="modelValue"
    :disabled="disabled"
    :max="field.max || 5"
    :allow-half="field.allowHalf || false"
    :show-text="field.showText || false"
    :show-score="field.showScore || false"
    :texts="field.texts"
    :text-template="field.textTemplate"
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
