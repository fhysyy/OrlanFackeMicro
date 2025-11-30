<template>
  <el-upload
    :model-value="modelValue"
    @update:model-value="$emit('update:modelValue', $event)"
    :disabled="disabled"
    :action="field.action"
    :headers="field.headers"
    :multiple="field.multiple || false"
    :limit="field.limit"
    :before-upload="field.beforeUpload"
    :before-remove="field.beforeRemove"
    :on-success="field.onSuccess"
    :on-error="field.onError"
    :on-progress="field.onProgress"
    :on-change="field.onChange"
    :on-remove="field.onRemove"
    :on-preview="field.onPreview"
    :file-list="fileList"
    :show-file-list="field.showFileList !== false"
    :auto-upload="field.autoUpload !== false"
    :list-type="field.listType || 'text'"
    :drag="field.drag || false"
    :accept="field.accept"
  >
    <el-icon v-if="field.drag"><upload-filled /></el-icon>
    <el-button v-else :type="field.buttonType || 'primary'">
      <el-icon><upload /></el-icon>
      {{ field.buttonText || '上传文件' }}
    </el-button>
    <template #tip>
      <div v-if="field.tip" class="el-upload__tip">
        {{ field.tip }}
      </div>
    </template>
  </el-upload>
</template>

<script setup lang="ts">
import { defineProps, defineEmits, computed, watch } from 'vue';
import { BaseFormFieldProps } from './types';
import { Upload, UploadFilled } from '@element-plus/icons-vue';

// Props
const props = defineProps<BaseFormFieldProps>();

// Emits
const emit = defineEmits<{
  (e: 'update:modelValue', value: any): void;
  (e: 'change', value: any): void;
}>();

// 计算文件列表
const fileList = computed(() => {
  if (!props.modelValue || Array.isArray(props.modelValue)) {
    return props.modelValue || [];
  }
  return [props.modelValue];
});

// 监听文件列表变化
watch(fileList, (newVal) => {
  emit('update:modelValue', props.field.multiple ? newVal : newVal[0]);
});
</script>
