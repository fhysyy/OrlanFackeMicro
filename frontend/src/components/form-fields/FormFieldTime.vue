<template>
  <el-time-picker
    :id="field.prop"
    v-model="internalValue"
    :placeholder="field.placeholder || '选择时间'"
    :format="field.format || 'HH:mm:ss'"
    :value-format="field.valueFormat || 'HH:mm:ss'"
    :disabled="disabled"
    :clearable="field.clearable !== false"
    :readonly="field.readonly"
    :size="field.size"
    :prefix-icon="field.prefixIcon"
    :clear-icon="field.clearIcon"
    :validate-event="field.validateEvent !== false"
    :align="field.align || 'left'"
    :popper-class="field.popperClass"
    :picker-options="field.pickerOptions"
    :name="field.prop"
    @change="handleChange"
    @blur="handleBlur"
    @focus="handleFocus"
  />
</template>

<script setup lang="ts">
import { computed, watch } from 'vue'
import type { BaseFormFieldProps } from './types'
import { FormFieldType } from '@/types/form'

// Props
const props = defineProps<{
  modelValue: string | null;
  field: BaseFormFieldProps & {
    type: FormFieldType.TIME;
    format?: string;
    valueFormat?: string;
    clearable?: boolean;
    readonly?: boolean;
    size?: 'large' | 'default' | 'small';
    prefixIcon?: string;
    clearIcon?: string;
    validateEvent?: boolean;
    align?: 'left' | 'center' | 'right';
    popperClass?: string;
    pickerOptions?: any;
  };
  disabled?: boolean;
}>()

// Emits
const emit = defineEmits<{
  (e: 'update:modelValue', value: string | null): void;
  (e: 'change', value: string | null): void;
  (e: 'blur', event: FocusEvent): void;
  (e: 'focus', event: FocusEvent): void;
}>()

// 内部值，用于处理v-model
const internalValue = computed({
  get: () => props.modelValue,
  set: (val) => {
    emit('update:modelValue', val)
    emit('change', val)
  }
})

// 处理变化
const handleChange = (val: string | null) => {
  emit('change', val)
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
:deep(.el-time-editor) {
  width: 100%;
}
</style>
