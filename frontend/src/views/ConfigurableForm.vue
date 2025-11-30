<template>
  <div class="configurable-form-page">
    <el-card class="page-card">
      <template #header>
        <div class="card-header">
          <h2>可配置表单</h2>
          <p class="subtitle">基于数据库配置的动态表单生成器</p>
        </div>
      </template>

      <!-- 表单控制栏 -->
      <el-row class="form-controls" :gutter="20">
        <el-col :span="8">
          <el-button-group>
            <el-button type="primary" @click="toggleMode">
              <el-icon><Switch /></el-icon>
              {{ isEditMode ? '切换到预览' : '切换到编辑' }}
            </el-button>
            <el-button @click="resetForm">
              <el-icon><RefreshRight /></el-icon>
              重置表单
            </el-button>
            <el-button @click="loadSampleData">
              <el-icon><DocumentCopy /></el-icon>
              加载示例
            </el-button>
          </el-button-group>
        </el-col>
        <el-col :span="16" class="text-right">
          <el-button type="success" @click="saveFormConfig" :loading="saving">
            <el-icon><Download /></el-icon>
            保存配置
          </el-button>
        </el-col>
      </el-row>

      <el-divider />

      <!-- 编辑模式：表单构建器 -->
      <div v-if="isEditMode" class="edit-mode">
        <FormBuilder
          v-model="formConfig"
          @update:formData="handleFormDataUpdate"
        />
      </div>

      <!-- 预览模式：表单渲染器 -->
      <div v-else class="preview-mode">
        <el-card class="preview-card">
          <template #header>
            <div class="preview-header">
              <h3>表单预览</h3>
              <el-tag type="info">{{ formConfig.title || '未命名表单' }}</el-tag>
            </div>
          </template>

          <FormGenerator
            ref="formGeneratorRef"
            :config="formConfig"
            v-model="formData"
            @submit="handleFormSubmit"
            @reset="handleFormReset"
            @fieldChange="handleFieldChange"
          />
        </el-card>

        <!-- 表单数据展示 -->
        <el-card class="data-card" v-if="Object.keys(formData).length > 0">
          <template #header>
            <h3>表单数据</h3>
          </template>
          <el-descriptions :column="1" border>
            <el-descriptions-item 
              v-for="(value, key) in formData" 
              :key="key"
              :label="getFieldLabel(key)"
            >
              <pre>{{ formatValue(value) }}</pre>
            </el-descriptions-item>
          </el-descriptions>
        </el-card>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Switch, RefreshRight, DocumentCopy, Download } from '@element-plus/icons-vue';
import type { FormConfig, FormField } from '@/types/form';
import { FormFieldType, DatabaseDataType } from '@/types/form';
import FormBuilder from '@/components/FormBuilder.vue';
import FormGenerator from '@/components/FormGenerator.vue';
import { generateFormConfigFromModel } from '@/services/formService';

// 表单模式控制
const isEditMode = ref(true);
const saving = ref(false);
const formGeneratorRef = ref();

// 表单配置
const formConfig = reactive<FormConfig>({
  id: 'example-form',
  title: '示例表单',
  description: '这是一个基于数据库配置的动态表单示例',
  labelPosition: 'right',
  labelWidth: '120px',
  layout: 'grid',
  gutter: 20,
  xs: 24,
  sm: 12,
  md: 8,
  lg: 6,
  xl: 6,
  showSubmitButton: true,
  showResetButton: true,
  submitButtonText: '提交',
  resetButtonText: '重置',
  fields: [
    {
      id: 'field-1',
      type: FormFieldType.INPUT,
      prop: 'name',
      label: '姓名',
      placeholder: '请输入姓名',
      required: true,
      databaseType: DatabaseDataType.VARCHAR,
      databaseLength: 50,
      validationRules: [
        { type: 'required', message: '请输入姓名' },
        { type: 'maxLength', value: 50, message: '姓名不能超过50个字符' }
      ]
    },
    {
      id: 'field-2',
      type: FormFieldType.INPUT,
      prop: 'email',
      label: '邮箱',
      placeholder: '请输入邮箱地址',
      required: true,
      databaseType: DatabaseDataType.VARCHAR,
      databaseLength: 100,
      validationRules: [
        { type: 'required', message: '请输入邮箱地址' },
        { type: 'email', message: '请输入有效的邮箱地址' },
        { type: 'maxLength', value: 100, message: '邮箱地址不能超过100个字符' }
      ]
    },
    {
      id: 'field-3',
      type: FormFieldType.INPUT,
      prop: 'phone',
      label: '手机号码',
      placeholder: '请输入手机号码',
      required: true,
      databaseType: DatabaseDataType.VARCHAR,
      databaseLength: 11,
      validationRules: [
        { type: 'required', message: '请输入手机号码' },
        { type: 'phone', message: '请输入有效的手机号码' }
      ]
    },
    {
      id: 'field-4',
      type: FormFieldType.INPUT,
      prop: 'idCard',
      label: '身份证号',
      placeholder: '请输入身份证号码',
      required: true,
      databaseType: DatabaseDataType.VARCHAR,
      databaseLength: 18,
      validationRules: [
        { type: 'required', message: '请输入身份证号码' },
        { type: 'idCard', message: '请输入有效的身份证号码' }
      ]
    },
    {
      id: 'field-5',
      type: FormFieldType.SELECT,
      prop: 'gender',
      label: '性别',
      required: true,
      databaseType: DatabaseDataType.TINYINT,
      options: [
        { label: '男', value: 1 },
        { label: '女', value: 2 }
      ],
      validationRules: [
        { type: 'required', message: '请选择性别' }
      ]
    },
    {
      id: 'field-6',
      type: FormFieldType.DATE_PICKER,
      prop: 'birthDate',
      label: '出生日期',
      placeholder: '请选择出生日期',
      required: true,
      databaseType: DatabaseDataType.DATE,
      format: 'YYYY-MM-DD',
      valueFormat: 'YYYY-MM-DD',
      validationRules: [
        { type: 'required', message: '请选择出生日期' }
      ]
    },
    {
      id: 'field-7',
      type: FormFieldType.TEXTAREA,
      prop: 'address',
      label: '地址',
      placeholder: '请输入详细地址',
      required: true,
      databaseType: DatabaseDataType.TEXT,
      databaseLength: 255,
      rows: 4,
      validationRules: [
        { type: 'required', message: '请输入地址' },
        { type: 'maxLength', value: 255, message: '地址不能超过255个字符' }
      ]
    },
    {
      id: 'field-8',
      type: FormFieldType.CHECKBOX,
      prop: 'hobbies',
      label: '爱好',
      required: true,
      databaseType: DatabaseDataType.VARCHAR,
      databaseLength: 255,
      options: [
        { label: '阅读', value: 'reading' },
        { label: '音乐', value: 'music' },
        { label: '运动', value: 'sports' },
        { label: '旅行', value: 'travel' },
        { label: '摄影', value: 'photography' }
      ],
      validationRules: [
        { type: 'required', message: '请至少选择一项爱好' }
      ]
    },
    {
      id: 'field-9',
      type: FormFieldType.SWITCH,
      prop: 'agreement',
      label: '用户协议',
      required: true,
      databaseType: DatabaseDataType.BOOLEAN,
      activeText: '同意',
      inactiveText: '不同意',
      validationRules: [
        { type: 'required', message: '请同意用户协议' }
      ]
    }
  ],
  initialData: {}
});

// 表单数据
const formData = reactive<Record<string, any>>({});

// 切换编辑/预览模式
function toggleMode() {
  isEditMode.value = !isEditMode.value;
}

// 重置表单
function resetForm() {
  ElMessageBox.confirm('确定要重置表单吗？这将清除所有配置和数据。', '警告', {
    confirmButtonText: '确定',
    cancelButtonText: '取消',
    type: 'warning',
  }).then(() => {
    // 重置表单配置
    formConfig.fields = [];
    formConfig.title = '未命名表单';
    formConfig.description = '';
    
    // 重置表单数据
    Object.keys(formData).forEach(key => {
      delete formData[key];
    });
    
    ElMessage.success('表单已重置');
  }).catch(() => {
    // 取消重置
  });
}

// 加载示例数据
function loadSampleData() {
  // 这里使用我们已经在formConfig中定义的示例字段
  ElMessage.success('示例数据已加载');
}

// 保存表单配置
async function saveFormConfig() {
  if (!formConfig.fields || formConfig.fields.length === 0) {
    ElMessage.warning('表单中没有字段，请先添加字段');
    return;
  }
  
  saving.value = true;
  try {
    // 模拟保存操作
    await new Promise(resolve => setTimeout(resolve, 1000));
    
    // 生成配置的JSON字符串
    const configJson = JSON.stringify(formConfig, null, 2);
    
    // 创建下载链接
    const blob = new Blob([configJson], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `${formConfig.title || 'form-config'}.json`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
    
    ElMessage.success('表单配置已保存');
  } catch (error) {
    console.error('保存表单配置失败:', error);
    ElMessage.error('保存表单配置失败');
  } finally {
    saving.value = false;
  }
}

// 处理表单数据更新
function handleFormDataUpdate(newData: Record<string, any>) {
  Object.assign(formData, newData);
}

// 处理表单提交
function handleFormSubmit(data: Record<string, any>) {
  ElMessage.success('表单提交成功');
  console.log('表单数据:', data);
}

// 处理表单重置
function handleFormReset() {
  ElMessage.info('表单已重置');
}

// 处理字段值变化
function handleFieldChange(field: string, value: any) {
  console.log(`字段 ${field} 值变化:`, value);
}

// 获取字段标签
function getFieldLabel(prop: string): string {
  const field = formConfig.fields?.find(f => f.prop === prop);
  return field?.label || prop;
}

// 格式化值显示
function formatValue(value: any): string {
  if (value === null || value === undefined) return '空';
  if (typeof value === 'object') {
    return JSON.stringify(value, null, 2);
  }
  return String(value);
}
</script>

<style scoped>
.configurable-form-page {
  padding: 20px;
  min-height: 100vh;
  background-color: #f5f7fa;
}

.page-card {
  margin-bottom: 20px;
}

.card-header {
  display: flex;
  flex-direction: column;
}

.card-header h2 {
  margin: 0;
  font-size: 24px;
  font-weight: 600;
}

.subtitle {
  margin: 5px 0 0 0;
  color: #606266;
  font-size: 14px;
}

.form-controls {
  margin-bottom: 20px;
  padding: 10px 0;
}

.edit-mode {
  min-height: 600px;
}

.preview-mode {
  min-height: 600px;
}

.preview-card {
  margin-bottom: 20px;
}

.preview-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.preview-header h3 {
  margin: 0;
  font-size: 18px;
  font-weight: 500;
}

.data-card {
  margin-top: 20px;
}

.data-card pre {
  margin: 0;
  padding: 10px;
  background-color: #f5f7fa;
  border-radius: 4px;
  overflow-x: auto;
}

:deep(.el-descriptions__content) {
  word-break: break-all;
}
</style>
