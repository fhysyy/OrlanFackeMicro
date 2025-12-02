<template>
  <div class="radio-group-wrapper">
    <el-radio-group
      :model-value="modelValue"
      :disabled="disabled"
      :class="`radio-group-${field.layout || 'horizontal'}`"
      @update:model-value="$emit('update:modelValue', $event)"
    >
      <el-radio
        v-for="option in field.options"
        :key="option[field.valueKey || 'value']"
        :label="option[field.valueKey || 'value']"
        :disabled="option.disabled"
      >
        {{ option[field.labelKey || 'label'] }}
      </el-radio>
    </el-radio-group>
  </div>
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
</script>

<style scoped>
.radio-group-wrapper {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
}

.radio-group-horizontal {
  display: flex;
  flex-wrap: wrap;
}

.radio-group-vertical {
  display: flex;
  flex-direction: column;
}

.radio-group-horizontal .el-radio {
  margin-right: 15px;
  margin-bottom: 4px;
}

.radio-group-vertical .el-radio {
  display: block;
  margin-bottom: 8px;
}
</style>
