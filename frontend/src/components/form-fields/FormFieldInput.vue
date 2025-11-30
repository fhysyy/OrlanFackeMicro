<template>
  <el-input
    v-model="internalValue"
    :type="field.inputType || 'text'"
    :placeholder="field.placeholder || `请输入${field.label}`"
    :disabled="disabled"
    :maxlength="field.maxlength || field.databaseLength"
    :clearable="field.clearable !== false"
    :show-word-limit="field.showWordLimit || !!field.databaseLength"
    :prefix-icon="field.prefixIcon"
    :suffix-icon="field.suffixIcon"
    :style="{ width: field.width || '100%' }"
    :readonly="field.readonly"
    :validate-event="field.validateEvent !== false"
    :size="field.size"
    :autocomplete="field.autocomplete || 'off'"
    :spellcheck="field.spellcheck || false"
    :rows="field.rows"
    :minlength="field.minlength"
    @blur="handleBlur"
    @focus="handleFocus"
    @change="handleChange"
  >
    <!-- 自定义前缀 -->
    <template
      v-if="field.prefix"
      #prefix
    >
      {{ field.prefix }}
    </template>
    
    <!-- 自定义后缀 -->
    <template
      v-if="field.suffix"
      #suffix
    >
      {{ field.suffix }}
    </template>
    
    <!-- 自定义输入框内容 -->
    <template
      v-if="field.prepend"
      #prepend
    >
      {{ field.prepend }}
    </template>
    
    <!-- 自定义输入框内容 -->
    <template
      v-if="field.append"
      #append
    >
      {{ field.append }}
    </template>
  </el-input>
</template>

<script setup lang="ts">
import { computed, defineProps, defineEmits } from 'vue'
import { BaseFormFieldProps } from './types'
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
