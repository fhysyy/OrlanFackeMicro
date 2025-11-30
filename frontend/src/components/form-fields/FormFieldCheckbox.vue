<template>
  <div class="checkbox-group-wrapper">
    <el-checkbox-group
      :model-value="modelValue"
      @update:model-value="$emit('update:modelValue', $event)"
      :disabled="disabled"
      :class="`checkbox-group-${field.layout || 'horizontal'}`"
    >
      <el-checkbox
        v-for="option in field.options"
        :key="option[field.valueKey || 'value']"
        :label="option[field.valueKey || 'value']"
        :disabled="option.disabled"
      >
        {{ option[field.labelKey || 'label'] }}
      </el-checkbox>
    </el-checkbox-group>
  </div>
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
</script>

<style scoped>
.checkbox-group-wrapper {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
}

.checkbox-group-horizontal {
  display: flex;
  flex-wrap: wrap;
}

.checkbox-group-vertical {
  display: flex;
  flex-direction: column;
}

.checkbox-group-horizontal .el-checkbox {
  margin-right: 15px;
  margin-bottom: 4px;
}

.checkbox-group-vertical .el-checkbox {
  display: block;
  margin-bottom: 8px;
}
</style>
