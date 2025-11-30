<template>
  <el-dialog
    :model-value="visible"
    @update:model-value="$emit('update:visible', $event)"
    :title="dialogTitle"
    :width="dialogWidth || '50%'"
    :fullscreen="fullscreen"
    :close-on-click-modal="false"
    @close="handleDialogClose"
  >
    <template #default>
      <el-form
        ref="formRef"
        :model="formData"
        :rules="dialogConfig?.rules"
        :label-width="dialogConfig?.labelWidth || '100px'"
        :disabled="dialogType === 'view'"
      >
        <FormGenerator
          v-if="dialogConfig"
          :config="dialogConfig"
          :model="formData"
          :form-ref="formRef"
          :disabled="dialogType === 'view'"
        />
      </el-form>
    </template>
    <template #footer>
      <template v-if="dialogType !== 'view'">
        <el-button @click="handleCancel">取消</el-button>
        <el-button type="primary" @click="handleSubmit">{{ submitButtonText }}</el-button>
      </template>
      <template v-else>
        <el-button @click="handleCancel">关闭</el-button>
      </template>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, reactive, watch } from 'vue';
import FormGenerator from '../FormGenerator.vue';
import type { FormConfig } from '../../types/form';

// Props定义
const props = defineProps<{
  visible: boolean;
  dialogType: 'create' | 'update' | 'view';
  dialogConfig?: FormConfig;
  initialData?: Record<string, any>;
  entityName?: string;
  dialogWidth?: string;
  fullscreen?: boolean;
  submitButtonText?: string;
}>();

// Emits定义
const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'submit', data: any): void;
  (e: 'cancel'): void;
  (e: 'close'): void;
}>();

// 响应式数据
const formRef = ref();
const formData = reactive(props.initialData ? { ...props.initialData } : {});

// 计算属性
const dialogTitle = computed(() => {
  const typeMap = {
    create: `创建${props.entityName || ''}`,
    update: `编辑${props.entityName || ''}`,
    view: `查看${props.entityName || ''}`
  };
  return typeMap[props.dialogType];
});

const submitButtonText = computed(() => {
  return props.submitButtonText || (props.dialogType === 'create' ? '确定' : '保存');
});

// 处理提交
const handleSubmit = async () => {
  if (formRef.value && props.dialogType !== 'view') {
    try {
      await formRef.value.validate();
      emit('submit', { ...formData });
    } catch (error) {
      return;
    }
  } else {
    emit('submit', { ...formData });
  }
};

// 处理取消
const handleCancel = () => {
  emit('update:visible', false);
  emit('cancel');
};

// 处理对话框关闭
const handleDialogClose = () => {
  emit('update:visible', false);
  emit('close');
};

// 设置表单数据
const setFormData = (data: Record<string, any>) => {
  // 清空当前数据
  Object.keys(formData).forEach(key => {
    delete formData[key];
  });
  // 设置新数据
  Object.assign(formData, { ...data });
};

// 重置表单
const resetForm = () => {
  if (formRef.value) {
    formRef.value.resetFields();
  }
  // 清空当前数据
  Object.keys(formData).forEach(key => {
    delete formData[key];
  });
  // 设置默认值
  if (props.dialogConfig) {
    props.dialogConfig.fields.forEach(field => {
      if (field.defaultValue !== undefined) {
        formData[field.name] = field.defaultValue;
      }
    });
  }
};

// 监听外部数据变化
watch(() => props.initialData, (newVal) => {
  if (newVal) {
    setFormData(newVal);
  }
}, { deep: true });

// 暴露方法给父组件
defineExpose({
  formRef,
  setFormData,
  resetForm,
  validate: async () => {
    if (formRef.value) {
      return await formRef.value.validate();
    }
    return true;
  },
  clearValidate: () => {
    if (formRef.value) {
      formRef.value.clearValidate();
    }
  }
});
</script>