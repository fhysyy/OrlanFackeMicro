<template>
  <div class="form-generator">
    <el-form
      ref="formRef"
      :model="formData"
      :rules="formRules"
      :label-position="formConfig.labelPosition || 'right'"
      :label-width="formConfig.labelWidth || '120px'"
      :inline="formConfig.layout === 'inline'"
      class="dynamic-form"
    >
      <template v-if="formConfig.fields">
        <!-- 基本字段 -->
        <div
          v-for="field in formConfig.fields"
          :key="field.prop"
          v-show="!field.hidden"
          :class="[
            'form-item-wrapper',
            { 'form-item-hidden': field.hidden },
            field.className
          ]"
        >
          <el-form-item
            :label="field.label"
            :prop="field.prop"
            :rules="field.rules"
            :required="field.required"
            :help="field.helpText"
          >
            <!-- 输入框 -->
            <el-input
              v-if="field.type === FormFieldType.INPUT"
              v-model="formData[field.prop]"
              :type="field.inputType || 'text'"
              :placeholder="field.placeholder || `请输入${field.label}`"
              :disabled="field.disabled"
              :maxlength="field.maxlength"
              :clearable="field.clearable"
              :show-word-limit="field.showWordLimit"
              :prefix-icon="field.prefixIcon"
              :suffix-icon="field.suffixIcon"
              :style="{ width: field.width }"
            />

            <!-- 文本域 -->
            <el-input
              v-else-if="field.type === FormFieldType.TEXTAREA"
              v-model="formData[field.prop]"
              type="textarea"
              :placeholder="field.placeholder || `请输入${field.label}`"
              :disabled="field.disabled"
              :maxlength="field.maxlength"
              :clearable="field.clearable"
              :show-word-limit="field.showWordLimit"
              :rows="field.rows || 4"
              :autosize="field.autosize"
              :style="{ width: field.width }"
            />

            <!-- 选择框 -->
            <el-select
              v-else-if="field.type === FormFieldType.SELECT"
              v-model="formData[field.prop]"
              :placeholder="field.placeholder || `请选择${field.label}`"
              :disabled="field.disabled"
              :multiple="field.multiple"
              :filterable="field.filterable"
              :clearable="field.clearable"
              :remote="field.remote"
              :remote-method="field.remoteMethod"
              :loading="field.loading"
              :style="{ width: field.width }"
              @change="handleFieldChange(field.prop, $event)"
            >
              <el-option
                v-for="option in field.options"
                :key="option[field.valueKey || 'value']"
                :label="option[field.labelKey || 'label']"
                :value="option[field.valueKey || 'value']"
                :disabled="option.disabled"
              />
            </el-select>

            <!-- 单选框 -->
            <div v-else-if="field.type === FormFieldType.RADIO" class="radio-group-wrapper">
              <el-radio-group
                v-model="formData[field.prop]"
                :disabled="field.disabled"
                :class="`radio-group-${field.layout || 'horizontal'}`"
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

            <!-- 复选框 -->
            <div v-else-if="field.type === FormFieldType.CHECKBOX" class="checkbox-group-wrapper">
              <el-checkbox-group
                v-model="formData[field.prop]"
                :disabled="field.disabled"
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

            <!-- 开关 -->
            <el-switch
              v-else-if="field.type === FormFieldType.SWITCH"
              v-model="formData[field.prop]"
              :disabled="field.disabled"
              :active-value="field.activeValue !== undefined ? field.activeValue : true"
              :inactive-value="field.inactiveValue !== undefined ? field.inactiveValue : false"
              :active-text="field.activeText"
              :inactive-text="field.inactiveText"
              @change="handleFieldChange(field.prop, $event)"
            />

            <!-- 日期选择器 -->
            <el-date-picker
              v-else-if="field.type === FormFieldType.DATE_PICKER"
              v-model="formData[field.prop]"
              :type="field.pickerType || 'date'"
              :format="field.format || 'YYYY-MM-DD'"
              :placeholder="field.placeholder || `请选择${field.label}`"
              :disabled="field.disabled"
              :start-placeholder="field.startPlaceholder"
              :end-placeholder="field.endPlaceholder"
              :style="{ width: field.width }"
              @change="handleFieldChange(field.prop, $event)"
            />

            <!-- 时间选择器 -->
            <el-time-picker
              v-else-if="field.type === FormFieldType.TIME_PICKER"
              v-model="formData[field.prop]"
              :type="field.pickerType || 'time'"
              :format="field.format || 'HH:mm:ss'"
              :placeholder="field.placeholder || `请选择${field.label}`"
              :disabled="field.disabled"
              :start-placeholder="field.startPlaceholder"
              :end-placeholder="field.endPlaceholder"
              :style="{ width: field.width }"
              @change="handleFieldChange(field.prop, $event)"
            />

            <!-- 日期时间选择器 -->
            <el-date-picker
              v-else-if="field.type === FormFieldType.DATETIME_PICKER"
              v-model="formData[field.prop]"
              :type="field.pickerType || 'datetime'"
              :format="field.format || 'YYYY-MM-DD HH:mm:ss'"
              :placeholder="field.placeholder || `请选择${field.label}`"
              :disabled="field.disabled"
              :start-placeholder="field.startPlaceholder"
              :end-placeholder="field.endPlaceholder"
              :style="{ width: field.width }"
              @change="handleFieldChange(field.prop, $event)"
            />

            <!-- 数字输入框 -->
            <el-input-number
              v-else-if="field.type === FormFieldType.INPUT_NUMBER"
              v-model="formData[field.prop]"
              :min="field.min"
              :max="field.max"
              :step="field.step"
              :precision="field.precision"
              :disabled="field.disabled"
              :clearable="field.clearable"
              :style="{ width: field.width }"
              @change="handleFieldChange(field.prop, $event)"
            />

            <!-- 滑块 -->
            <el-slider
              v-else-if="field.type === FormFieldType.SLIDER"
              v-model="formData[field.prop]"
              :min="field.min || 0"
              :max="field.max || 100"
              :step="field.step || 1"
              :disabled="field.disabled"
              :show-input="field.showInput"
              :range="field.range"
              @change="handleFieldChange(field.prop, $event)"
            />

            <!-- 评分 -->
            <el-rate
              v-else-if="field.type === FormFieldType.RATE"
              v-model="formData[field.prop]"
              :max="field.max || 5"
              :disabled="field.disabled"
              :allow-half="field.allowHalf"
              :allow-clear="field.allowClear"
              @change="handleFieldChange(field.prop, $event)"
            />

            <!-- 文件上传 -->
            <el-upload
              v-else-if="field.type === FormFieldType.UPLOAD"
              v-model:file-list="formData[field.prop] || []"
              :action="field.action"
              :multiple="field.multiple"
              :accept="field.accept"
              :disabled="field.disabled"
              :before-upload="(file) => handleBeforeUpload(file, field)"
              :on-success="(response, file) => handleUploadSuccess(response, file, field)"
              :on-error="(error, file) => handleUploadError(error, file, field)"
              :on-remove="handleFileRemove"
            >
              <el-button type="primary">点击上传</el-button>
              <template #tip>
                <div class="el-upload__tip">
                  {{ field.helpText || '请上传文件' }}
                  <span v-if="field.fileSize">，大小不超过{{ formatFileSize(field.fileSize) }}</span>
                </div>
              </template>
            </el-upload>

            <!-- 自定义组件 -->
            <component
              v-else
              :is="customComponents[field.type] || 'div'"
              v-bind="getComponentProps(field)"
              v-model="formData[field.prop]"
              :disabled="field.disabled"
              @change="handleFieldChange(field.prop, $event)"
            />

            <!-- 插槽：自定义字段内容 -->
            <slot :name="`field-${field.prop}`" :field="field" :value="formData[field.prop]">
            </slot>
          </el-form-item>
        </div>
      </template>

      <!-- 表单操作按钮 -->
      <el-form-item v-if="showActions">
        <el-button type="primary" @click="handleSubmit" :loading="submitting">
          {{ formConfig.submitButtonText || '提交' }}
        </el-button>
        <el-button @click="handleReset" v-if="formConfig.showResetButton !== false">
          {{ formConfig.resetButtonText || '重置' }}
        </el-button>
      </el-form-item>
    </el-form>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue';
import { ElMessage } from 'element-plus';
import type { FormInstance } from 'element-plus';
import type { FormConfig, FormField, FormFieldType } from '@/types/form';

// Props
const props = defineProps<{
  config: FormConfig;
  modelValue?: Record<string, any>;
  customComponents?: Record<string, any>;
}>();

// Emits
const emit = defineEmits<{
  (e: 'update:modelValue', value: Record<string, any>): void;
  (e: 'submit', value: Record<string, any>): void;
  (e: 'reset'): void;
  (e: 'fieldChange', field: string, value: any): void;
}>();

// 表单引用
const formRef = ref<FormInstance>();

// 提交状态
const submitting = ref(false);

// 表单数据
const formData = reactive<Record<string, any>>({});

// 表单规则
const formRules = computed(() => {
  const rules: Record<string, any[]> = {};
  props.config.fields?.forEach(field => {
    if (field.rules && field.rules.length > 0) {
      rules[field.prop] = field.rules;
    } else if (field.required) {
      rules[field.prop] = [
        {
          required: true,
          message: `请输入${field.label}`,
          trigger: getFieldTrigger(field.type)
        }
      ];
    }
  });
  return rules;
});

// 是否显示操作按钮
const showActions = computed(() => {
  return props.config.showSubmitButton !== false || props.config.showResetButton !== false;
});

// 初始化表单数据
function initFormData() {
  // 清空现有数据
  Object.keys(formData).forEach(key => {
    delete formData[key];
  });

  // 设置初始值
  if (props.config.fields) {
    props.config.fields.forEach(field => {
      // 优先使用传入的modelValue
      if (props.modelValue && props.modelValue[field.prop] !== undefined) {
        formData[field.prop] = props.modelValue[field.prop];
      } else if (field.value !== undefined) {
        // 其次使用字段配置的value
        formData[field.prop] = field.value;
      } else if (props.config.initialData && props.config.initialData[field.prop] !== undefined) {
        // 最后使用表单配置的initialData
        formData[field.prop] = props.config.initialData[field.prop];
      } else {
        // 设置默认值
        formData[field.prop] = getDefaultValueByType(field.type);
      }
    });
  }
}

// 根据字段类型获取默认值
function getDefaultValueByType(type: FormFieldType) {
  switch (type) {
    case FormFieldType.CHECKBOX:
    case FormFieldType.TRANSFER:
    case FormFieldType.SELECT:
      return [];
    case FormFieldType.SWITCH:
      return false;
    case FormFieldType.INPUT_NUMBER:
    case FormFieldType.RATE:
      return 0;
    default:
      return '';
  }
}

// 获取字段触发方式
function getFieldTrigger(type: FormFieldType): string | string[] {
  const blurTriggers = [FormFieldType.INPUT, FormFieldType.TEXTAREA, FormFieldType.INPUT_NUMBER];
  const changeTriggers = [
    FormFieldType.SELECT,
    FormFieldType.RADIO,
    FormFieldType.CHECKBOX,
    FormFieldType.SWITCH,
    FormFieldType.DATE_PICKER,
    FormFieldType.TIME_PICKER,
    FormFieldType.DATETIME_PICKER,
    FormFieldType.SLIDER,
    FormFieldType.CASCADER,
    FormFieldType.TREE_SELECT,
    FormFieldType.TRANSFER,
    FormFieldType.RATE
  ];

  if (blurTriggers.includes(type)) {
    return ['blur', 'change'];
  } else if (changeTriggers.includes(type)) {
    return 'change';
  }
  return 'change';
}

// 获取组件属性
function getComponentProps(field: FormField) {
  const baseProps = {
    placeholder: field.placeholder,
    disabled: field.disabled
  };
  
  // 合并字段特定的属性
  return { ...baseProps, ...field.componentProps };
}

// 格式化文件大小
function formatFileSize(bytes: number): string {
  if (bytes < 1024) return bytes + ' B';
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(2) + ' KB';
  return (bytes / (1024 * 1024)).toFixed(2) + ' MB';
}

// 处理文件上传前
function handleBeforeUpload(file: File, field: FormField): boolean | Promise<File> {
  // 检查文件大小
  if (field.fileSize && file.size > field.fileSize) {
    ElMessage.error(`文件大小不能超过${formatFileSize(field.fileSize)}`);
    return false;
  }

  // 调用自定义上传前钩子
  if (field.beforeUpload) {
    return field.beforeUpload(file);
  }
  
  return true;
}

// 处理文件上传成功
function handleUploadSuccess(response: any, file: any, field: FormField) {
  if (field.onSuccess) {
    field.onSuccess(response, file);
  }
  handleFieldChange(field.prop, formData[field.prop]);
}

// 处理文件上传失败
function handleUploadError(error: any, file: any, field: FormField) {
  if (field.onError) {
    field.onError(error, file);
  } else {
    ElMessage.error('文件上传失败');
  }
}

// 处理文件移除
function handleFileRemove(file: any, fileList: any[]) {
  // 可以在这里添加自定义逻辑
}

// 处理字段值变化
function handleFieldChange(field: string, value: any) {
  emit('fieldChange', field, value);
  emit('update:modelValue', { ...formData });
}

// 提交表单
async function handleSubmit() {
  if (!formRef.value) return;

  try {
    await formRef.value.validate();
    submitting.value = true;

    // 调用自定义提交函数
    if (props.config.onSubmit) {
      await props.config.onSubmit(formData);
    }

    // 触发提交事件
    emit('submit', { ...formData });
  } catch (error) {
    console.error('表单验证失败:', error);
  } finally {
    submitting.value = false;
  }
}

// 重置表单
function handleReset() {
  if (formRef.value) {
    formRef.value.resetFields();
  }
  initFormData();
  
  // 调用自定义重置函数
  if (props.config.onReset) {
    props.config.onReset();
  }
  
  // 触发重置事件
  emit('reset');
}

// 设置表单数据
function setFormData(data: Record<string, any>) {
  Object.assign(formData, data);
  emit('update:modelValue', { ...formData });
}

// 验证表单
function validate(): Promise<boolean> {
  return new Promise((resolve) => {
    if (!formRef.value) {
      resolve(false);
      return;
    }

    formRef.value.validate((valid) => {
      resolve(valid);
    });
  });
}

// 监听配置变化，重新初始化表单
watch(
  () => props.config,
  () => {
    initFormData();
  },
  { deep: true }
);

// 监听modelValue变化
watch(
  () => props.modelValue,
  (newValue) => {
    if (newValue) {
      Object.assign(formData, newValue);
    }
  },
  { deep: true }
);

// 组件挂载时初始化
onMounted(() => {
  initFormData();
});

// 暴露方法给父组件
defineExpose({
  formRef,
  formData,
  handleSubmit,
  handleReset,
  setFormData,
  validate
});
</script>

<style scoped>
.form-generator {
  width: 100%;
}

.dynamic-form {
  background: #fff;
  padding: 20px;
  border-radius: 4px;
}

.form-item-wrapper {
  margin-bottom: 16px;
}

.form-item-hidden {
  display: none;
}

.radio-group-wrapper,
.checkbox-group-wrapper {
  display: flex;
  flex-wrap: wrap;
}

.radio-group-horizontal .el-radio,
.checkbox-group-horizontal .el-checkbox {
  margin-right: 16px;
}

.radio-group-vertical .el-radio,
.checkbox-group-vertical .el-checkbox {
  display: block;
  margin-bottom: 8px;
}

:deep(.el-upload) {
  display: block;
}
</style>