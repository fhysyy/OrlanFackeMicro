<template>
  <el-input
    v-model="internalValue"
    type="textarea"
    :placeholder="field.placeholder || `请输入${field.label}`"
    :disabled="disabled"
    :rows="field.rows || 3"
    :maxlength="field.maxlength || field.databaseLength"
    :minlength="field.minlength"
    :show-word-limit="field.showWordLimit || !!field.databaseLength"
    :resize="field.resize || 'vertical'"
    :autosize="field.autosize"
    :readonly="field.readonly"
    :validate-event="field.validateEvent !== false"
    :size="field.size"
    :style="{ width: field.width || '100%' }"
    @blur="handleBlur"
    @focus="handleFocus"
    @change="handleChange"
  />
</template>

<script setup lang="ts">
import { computed, defineProps, defineEmits } from 'vue'
import type { FormField } from '@/types'

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

// 处理值变化
const handleChange = (value: any) => {
  emit('change', value)
}

// 处理失焦
const handleBlur = (event: FocusEvent) => {
  emit('blur', event)
}

// 处理聚焦
const handleFocus = (event: FocusEvent) => {
  emit('focus', event)
}
</script>

<style scoped>
:deep(.el-input) {
  width: 100%;
}
</style>
