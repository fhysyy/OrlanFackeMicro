<template>
  <el-slider
    :model-value="modelValue"
    :disabled="disabled"
    :min="field.min || 0"
    :max="field.max || 100"
    :step="field.step || 1"
    :range="field.range || false"
    :show-input="field.showInput || false"
    :show-input-controls="field.showInputControls !== false"
    :show-stops="field.showStops || false"
    :show-tooltip="field.showTooltip !== false"
    :tooltip-effect="field.tooltipEffect || 'dark'"
    :format-tooltip="field.formatTooltip"
    :marks="field.marks"
    :debounce="field.debounce || 300"
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
