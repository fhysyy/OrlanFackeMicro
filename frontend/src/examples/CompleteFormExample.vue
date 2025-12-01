<template>
  <div class="complete-form-example">
    <div class="example-header">
      <h2>完整表单示例</h2>
      
      <!-- 语言切换器 -->
      <div class="header-controls">
        <div class="language-selector">
          <span>语言设置：</span>
          <LanguageSwitcher @change="handleLanguageChange" />
        </div>
        
        <!-- 表单配置 -->
        <div class="config-options">
          <el-checkbox v-model="useVirtualization">
            启用虚拟化（超过50个字段时）
          </el-checkbox>
          <el-checkbox v-model="showErrorNotification">
            显示错误通知
          </el-checkbox>
          <el-checkbox v-model="enableAutoSave">
            启用自动保存
          </el-checkbox>
          <el-checkbox v-model="showAutoSaveIndicator">
            显示自动保存指示器
          </el-checkbox>
        </div>
        
        <!-- 字段数量控制 -->
        <div class="field-count-control">
          <span>字段数量：</span>
          <el-slider
            v-model="fieldCount"
            :min="10"
            :max="200"
            :step="10"
            show-input
            @change="regenerateFields"
          />
        </div>
      </div>
    </div>

    <!-- 国际化提供者 -->
    <FormI18nProvider>
      <!-- 增强错误处理 -->
      <EnhancedErrorHandler
        ref="errorHandler"
        :form-id="formId"
        :show-error-notification="showErrorNotification"
        :enable-auto-save="enableAutoSave"
        :show-auto-save-indicator="showAutoSaveIndicator"
        :get-form-data="getFormData"
        :get-form-config="getFormConfig"
        @recover="handleRecover"
        @reset="handleReset"
        @error="handleError"
      >
        <!-- 虚拟化表单 -->
        <VirtualizedFormOptimized
          v-if="useVirtualization"
          ref="formRef"
          :config="formConfig"
          :model-value="formData"
          :item-height="80"
          :buffer-size="5"
          :estimated-item-height="80"
          :enable-intersection-observer="true"
          :scroll-debounce-time="16"
          @update:model-value="handleModelValueUpdate"
          @submit="handleSubmit"
          @reset="handleReset"
          @field-change="handleFieldChange"
        >
          <template
            v-for="(_, name) in $slots"
            #[name]="slotData"
          >
            <slot
              :name="name"
              v-bind="slotData"
            />
          </template>
        </VirtualizedFormOptimized>
        
        <!-- 普通表单 -->
        <el-form
          v-else
          ref="formRef"
          :model="formData"
          :rules="formRules"
          :label-position="formConfig.labelPosition || 'right'"
          :label-width="formConfig.labelWidth || '120px'"
          :inline="formConfig.layout === 'inline'"
          class="dynamic-form"
        >
          <template v-if="formConfig.fields">
            <!-- 使用表单布局组件渲染字段 -->
            <FormLayout
              :layout="formConfig.layout || 'grid'"
              :gutter="formConfig.gutter || 20"
              :xs="formConfig.xs || 12"
              :sm="formConfig.sm || 8"
              :md="formConfig.md || 6"
              :lg="formConfig.lg || 6"
              :xl="formConfig.xl || 6"
              :min-col-width="'100px'"
              :children="visibleFields"
            >
              <template #item-="{ item, index }">
                <FormFormItemRenderer
                  :field="item"
                  :model-value="formData"
                  :label-width="formConfig.labelWidth || '80px'"
                  @change="handleFieldChange"
                >
                  <template
                    v-for="(_, name) in $slots"
                    #[name]="slotData"
                  >
                    <slot
                      :name="`field-${item.prop}`"
                      v-bind="slotData"
                    />
                  </template>
                </FormFormItemRenderer>
              </template>
            </FormLayout>
          </template>

          <!-- 表单操作按钮 -->
          <FormActionsRenderer
            :submitting="submitting"
            :submit-button-text="$t('form.submit')"
            :reset-button-text="$t('form.reset')"
            :show-reset-button="formConfig.showResetButton"
            @submit="handleSubmit"
            @reset="handleReset"
          >
            <template #actions>
              <slot name="actions" />
            </template>
          </FormActionsRenderer>
        </el-form>
      </EnhancedErrorHandler>
    </FormI18nProvider>
    
    <!-- 数据显示区域 -->
    <div class="data-display">
      <el-card header="当前表单数据">
        <pre>{{ JSON.stringify(formData, null, 2) }}</pre>
      </el-card>
      
      <!-- 错误历史 -->
      <el-card header="错误历史" v-if="errorHistory.length > 0">
        <el-timeline>
          <el-timeline-item
            v-for="(error, index) in errorHistory"
            :key="index"
            :timestamp="formatDate(error.timestamp)"
            placement="top"
            :type="error.resolved ? 'success' : 'danger'"
          >
            <div class="error-history-item">
              <p>{{ error.error.message }}</p>
              <el-button
                size="small"
                @click="recoverFromError(error)"
              >
                恢复此错误
              </el-button>
            </div>
          </el-timeline-item>
        </el-timeline>
      </el-card>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, nextTick } from 'vue'
import { ElMessage } from 'element-plus'
import type { FormConfig } from '@/types/form'
import { FormFieldType } from '@/types/form'
import { createInputField, createSelectField, createDateField } from '@/services/formService'
import { FormValidationManager } from './form-parts/FormValidationManager'
import { FormDataManager } from './form-parts/FormDataManager'
import { t } from './form-parts/FormI18n'
import {
  FormI18nProvider,
  LanguageSwitcher,
  FormFormItemRenderer,
  FormActionsRenderer,
  VirtualizedFormOptimized,
  EnhancedErrorHandler
} from './form-parts'
import { FormLayout } from './form-fields'
import { enhancedErrorService } from '@/services/enhancedErrorService'

// 组件引用
const formRef = ref()
const errorHandler = ref()

// 表单ID
const formId = ref(`complete-form-${Date.now()}`)

// 配置选项
const useVirtualization = ref(true)
const showErrorNotification = ref(true)
const enableAutoSave = ref(true)
const showAutoSaveIndicator = ref(true)
const fieldCount = ref(50)

// 提交状态
const submitting = ref(false)

// 表单数据
const formData = reactive<Record<string, any>>({})

// 错误历史
const errorHistory = ref<any[]>([])

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

// 表单验证规则
const formRules = computed(() => {
  return FormValidationManager.generateFormRules(formConfig.fields)
})

// 可见字段
const visibleFields = computed(() => {
  return formConfig.fields?.filter(field => !field.hidden) || []
})

// 生成表单字段
const generateFormFields = (count: number) => {
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
  
  // 添加额外字段
  for (let i = 0; i < count - 7; i++) {
    fields.push(createInputField(`field_${i}`, `字段 ${i+1}`, {
      placeholder: `请输入字段 ${i+1}`
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

// 重新生成字段
const regenerateFields = () => {
  formConfig.fields = generateFormFields(fieldCount.value)
}

// 获取表单数据
const getFormData = () => {
  return { ...formData }
}

// 获取表单配置
const getFormConfig = () => {
  return { ...formConfig }
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
const handleSubmit = async () => {
  if (!formRef.value) return

  try {
    const isValid = await FormValidationManager.validateForm(formRef.value)
    if (!isValid) return

    submitting.value = true

    // 调用自定义提交函数
    if (formConfig.onSubmit) {
      await formConfig.onSubmit(formData)
    }

    ElMessage.success(t('success.submit'))
  } catch (error) {
    console.error('表单提交失败:', error)
  } finally {
    submitting.value = false
  }
}

// 处理表单重置
const handleReset = () => {
  console.log('表单已重置')
  ElMessage.info(t('success.reset'))
  
  // 重新初始化表单数据
  Object.assign(
    formData,
    FormDataManager.initFormData(
      formConfig.fields,
      undefined,
      formConfig.initialData
    )
  )
}

// 处理错误
const handleError = (error: Error, data: any) => {
  console.error('表单错误:', error, data)
  
  // 更新错误历史
  errorHistory.value = enhancedErrorService.getErrorHistory(formId.value)
}

// 处理恢复
const handleRecover = (data: any) => {
  Object.assign(formData, data)
  ElMessage.success(t('success.recover'))
}

// 从错误恢复
const recoverFromError = async (errorRecord: any) => {
  if (!errorHandler.value) return
  
  try {
    const result = await enhancedErrorService.attemptErrorRecovery(
      formId.value,
      errorRecord.id
    )
    
    if (result.success && result.data) {
      handleRecover(result.data)
    } else {
      ElMessage.error(result.message)
    }
  } catch (error) {
    console.error('恢复失败:', error)
  }
}

// 处理语言变化
const handleLanguageChange = (locale: string) => {
  console.log('语言已切换到:', locale)
  // 可以在这里更新表单配置中的文本
}

// 格式化日期
const formatDate = (timestamp: number) => {
  return new Date(timestamp).toLocaleString()
}

// 初始化表单
onMounted(async () => {
  await nextTick()
  regenerateFields()
  
  // 初始化表单数据
  Object.assign(
    formData,
    FormDataManager.initFormData(
      formConfig.fields,
      undefined,
      formConfig.initialData
    )
  )
})
</script>

<style scoped>
.complete-form-example {
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

.header-controls {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.language-selector,
.config-options,
.field-count-control {
  display: flex;
  align-items: center;
  gap: 10px;
}

.field-count-control {
  min-width: 300px;
}

.data-display {
  margin-top: 20px;
  display: flex;
  flex-direction: column;
  gap: 16px;
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

.error-history-item {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.error-history-item p {
  margin: 0;
  word-break: break-word;
}

.dynamic-form {
  width: 100%;
}
</style>