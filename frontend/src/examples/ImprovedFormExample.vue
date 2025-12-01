<template>
  <div class="improved-form-example">
    <div class="example-header">
      <h2>改进后的表单示例</h2>
      
      <!-- 语言切换器 -->
      <div class="language-selector">
        <span>语言设置：</span>
        <LanguageSwitcher />
      </div>
      
      <!-- 配置选项 -->
      <div class="config-options">
        <el-checkbox v-model="useVirtualization">
          启用虚拟化（超过50个字段时）
        </el-checkbox>
        <el-checkbox v-model="showErrorNotification">
          显示错误通知
        </el-checkbox>
        <el-checkbox v-model="enableAutoRecovery">
          启用自动恢复
        </el-checkbox>
      </div>
    </div>

    <!-- 改进后的表单生成器 -->
    <FormGeneratorImproved
      ref="formRef"
      :config="formConfig"
      :model-value="formData"
      :use-virtualization="useVirtualization"
      :show-error-notification="showErrorNotification"
      :enable-auto-recovery="enableAutoRecovery"
      :virtualization-threshold="50"
      @update:model-value="handleModelValueUpdate"
      @submit="handleSubmit"
      @reset="handleReset"
      @field-change="handleFieldChange"
      @error="handleError"
    />
    
    <!-- 数据显示区域 -->
    <div class="data-display">
      <el-card header="当前表单数据">
        <pre>{{ JSON.stringify(formData, null, 2) }}</pre>
      </el-card>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import type { FormConfig } from '@/types/form'
import { FormFieldType } from '@/types/form'
import { createInputField, createSelectField, createDateField } from '@/services/formService'
import FormGeneratorImproved from '@/components/FormGeneratorImproved.vue'
import { LanguageSwitcher, t } from '@/components/form-parts'

// 配置选项
const useVirtualization = ref(true)
const showErrorNotification = ref(true)
const enableAutoRecovery = ref(true)

// 表单引用
const formRef = ref()

// 表单数据
const formData = reactive<Record<string, any>>({})

// 表单配置
const formConfig = reactive<FormConfig>({
  fields: [],
  labelPosition: 'right',
  labelWidth: '120px',
  layout: 'grid',
  gutter: 20,
  showSubmitButton: true,
  showResetButton: true,
  initialData: {
    name: '张三',
    email: 'zhangsan@example.com',
    age: 30,
    gender: 'male',
    birthDate: '1990-01-01',
    interests: ['reading', 'coding'],
    description: '这是一段描述文字'
  }
})

// 模拟生成大量字段（用于测试虚拟化）
const generateLargeFormFields = () => {
  const fields = []
  
  // 基本信息字段
  fields.push(createInputField('name', t('field.input'), {
    required: true,
    placeholder: t('placeholder.input', { field: t('field.input') })
  }))
  
  fields.push(createInputField('email', '邮箱', {
    type: FormFieldType.INPUT,
    inputType: 'email',
    required: true,
    placeholder: t('placeholder.input', { field: '邮箱' })
  }))
  
  fields.push(createInputField('phone', '手机号', {
    type: FormFieldType.INPUT,
    inputType: 'tel',
    placeholder: t('placeholder.input', { field: '手机号' })
  }))
  
  fields.push(createInputField('age', '年龄', {
    type: FormFieldType.INPUT_NUMBER,
    min: 0,
    max: 150
  }))
  
  fields.push(createSelectField('gender', '性别', [
    { label: '男', value: 'male' },
    { label: '女', value: 'female' },
    { label: '其他', value: 'other' }
  ]))
  
  fields.push(createDateField('birthDate', '出生日期'))
  
  fields.push(createSelectField('interests', '兴趣爱好', [
    { label: '阅读', value: 'reading' },
    { label: '编程', value: 'coding' },
    { label: '运动', value: 'sports' },
    { label: '音乐', value: 'music' },
    { label: '旅行', value: 'travel' }
  ], {
    type: FormFieldType.CHECKBOX,
    multiple: true
  }))
  
  // 为了测试虚拟化，添加大量额外字段
  for (let i = 0; i < 60; i++) {
    fields.push(createInputField(`extra_field_${i}`, `额外字段 ${i+1}`, {
      placeholder: `请输入额外字段 ${i+1}`
    }))
  }
  
  fields.push({
    type: FormFieldType.TEXTAREA,
    prop: 'description',
    label: '个人描述',
    placeholder: '请输入个人描述',
    rows: 4,
    maxlength: 500,
    showWordLimit: true
  } as any)
  
  return fields
}

// 处理模型值更新
const handleModelValueUpdate = (value: Record<string, any>) => {
  Object.assign(formData, value)
}

// 处理字段变化
const handleFieldChange = (field: string, value: any) => {
  console.log(`字段 ${field} 的值已变更为:`, value)
}

// 处理表单提交
const handleSubmit = (data: Record<string, any>) => {
  console.log('表单提交数据:', data)
  ElMessage.success(t('success.submit'))
}

// 处理表单重置
const handleReset = () => {
  console.log('表单已重置')
  ElMessage.info(t('success.reset'))
}

// 处理错误
const handleError = (error: Error, formData: Record<string, any>) => {
  console.error('表单错误:', error, formData)
  ElMessage.error(error.message)
}

// 初始化表单
onMounted(() => {
  formConfig.fields = generateLargeFormFields()
})
</script>

<style scoped>
.improved-form-example {
  padding: 20px;
  max-width: 1200px;
  margin: 0 auto;
}

.example-header {
  margin-bottom: 20px;
  padding: 16px;
  background-color: #f5f7fa;
  border-radius: 4px;
}

.example-header h2 {
  margin-top: 0;
  margin-bottom: 16px;
}

.language-selector,
.config-options {
  margin-bottom: 12px;
  display: flex;
  align-items: center;
  gap: 10px;
}

.data-display {
  margin-top: 20px;
}

.data-display pre {
  background-color: #f5f5f5;
  padding: 12px;
  border-radius: 4px;
  max-height: 300px;
  overflow-y: auto;
  font-size: 12px;
  line-height: 1.4;
}
</style>