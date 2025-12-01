<template>
  <div class="form-preview">
    <!-- 预览头部 -->
    <div class="preview-header">
      <h3>{{ normalizedConfig.title || '表单预览' }}</h3>
      <p v-if="normalizedConfig.description" class="preview-description">
        {{ normalizedConfig.description }}
      </p>
    </div>

    <!-- 预览工具栏 -->
    <div class="preview-toolbar">
      <el-button-group>
        <el-button 
          :type="previewMode === 'desktop' ? 'primary' : ''"
          size="small"
          @click="previewMode = 'desktop'"
        >
          <el-icon><Monitor /></el-icon>
          桌面端
        </el-button>
        <el-button 
          :type="previewMode === 'tablet' ? 'primary' : ''"
          size="small"
          @click="previewMode = 'tablet'"
        >
          <el-icon><Iphone /></el-icon>
          平板
        </el-button>
        <el-button 
          :type="previewMode === 'mobile' ? 'primary' : ''"
          size="small"
          @click="previewMode = 'mobile'"
        >
          <el-icon><Cellphone /></el-icon>
          手机
        </el-button>
      </el-button-group>
      
      <el-button size="small" @click="resetForm">
        <el-icon><RefreshRight /></el-icon>
        重置
      </el-button>
      
      <el-button size="small" @click="showFormData = !showFormData">
        <el-icon><View /></el-icon>
        {{ showFormData ? '隐藏' : '显示' }}数据
      </el-button>
    </div>

    <!-- 表单预览区域 -->
    <div class="preview-container" :class="`preview-${previewMode}`">
      <div class="preview-form-wrapper">
        <FormGenerator
          v-if="normalizedConfig && normalizedConfig.fields"
          ref="formGeneratorRef"
          v-model="formData"
          :config="normalizedConfig"
          @submit="handleSubmit"
          @reset="handleReset"
          @field-change="handleFieldChange"
        />
        
        <!-- 空状态提示 -->
        <el-empty v-else description="暂无表单配置" />
      </div>
    </div>

    <!-- 表单数据展示 -->
    <el-collapse-transition>
      <div v-show="showFormData" class="form-data-container">
        <el-card>
          <template #header>
            <div class="form-data-header">
              <h4>表单数据</h4>
              <el-button size="small" @click="copyFormData">
                <el-icon><DocumentCopy /></el-icon>
                复制
              </el-button>
            </div>
          </template>
          <div class="form-data-content">
            <el-empty v-if="!hasFormData" description="暂无数据" />
            <el-descriptions v-else :column="1" border>
              <el-descriptions-item 
                v-for="(value, key) in formData" 
                :key="key"
                :label="getFieldLabel(key)"
              >
                <pre class="data-value">{{ formatValue(value) }}</pre>
              </el-descriptions-item>
            </el-descriptions>
          </div>
        </el-card>
      </div>
    </el-collapse-transition>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, nextTick } from 'vue'
import { ElMessage } from 'element-plus'
import { 
  Monitor, 
  Iphone, 
  Cellphone, 
  RefreshRight, 
  View, 
  DocumentCopy 
} from '@element-plus/icons-vue'
import type { FormConfig } from '@/types/form'
import { FormFieldType } from '@/types/form'
import FormGenerator from '@/components/FormGenerator.vue'

// Props
interface Props {
  config: FormConfig
}
const props = defineProps<Props>()

// Emits
const emit = defineEmits<{
  submit: [data: Record<string, any>]
}>()

// 预览模式
const previewMode = ref<'desktop' | 'tablet' | 'mobile'>('desktop')

// 是否显示表单数据
const showFormData = ref(true)

// 表单引用
const formGeneratorRef = ref()

// 表单数据
const formData = ref<Record<string, any>>({})

// 规范化后的表单配置
const normalizedConfig = computed(() => {
  // 防御性检查，确保config存在
  if (!props.config) {
    return {
      id: 'preview-form',
      title: '表单预览',
      description: '',
      fields: []
    }
  }
  
  const config = { ...props.config }
  
  // 确保fields属性存在
  if (!config.fields || !Array.isArray(config.fields)) {
    config.fields = []
  }
  
  // 确保title属性存在
  if (!config.title) {
    config.title = config.name || '表单预览'
  }
  
  // 确保description属性存在
  if (!config.description) {
    config.description = ''
  }
  
  // 确保其他必要属性存在
  if (!config.labelPosition) {
    config.labelPosition = 'right'
  }
  
  if (!config.labelWidth) {
    config.labelWidth = '120px'
  }
  
  if (!config.layout) {
    config.layout = 'grid'
  }
  
  config.fields = config.fields.map(field => {
    // 确保字段有prop属性
    if (!field.prop && field.name) {
      return { ...field, prop: field.name }
    } else if (!field.prop && field.fieldKey) {
      return { ...field, prop: field.fieldKey }
    } else if (!field.prop) {
      return { ...field, prop: `field-${Math.random().toString(36).substr(2, 9)}` }
    }
    return field
  })
  
  return config
})

// 是否有表单数据
const hasFormData = computed(() => {
  return Object.keys(formData.value).length > 0
})

// 初始化表单数据
function initFormData() {
  // 清空表单数据
  formData.value = {}
  
  // 防御性检查，确保配置存在
  if (!props.config || !props.config.fields) {
    return
  }
  
  // 根据表单字段初始化数据
  const fields = props.config.fields.map(field => {
    // 确保字段有prop属性
    if (!field.prop && field.name) {
      return { ...field, prop: field.name }
    } else if (!field.prop && field.fieldKey) {
      return { ...field, prop: field.fieldKey }
    } else if (!field.prop) {
      return { ...field, prop: `field-${Math.random().toString(36).substr(2, 9)}` }
    }
    return field
  })
  
  fields.forEach(field => {
    if (field.prop) {
      if (field.defaultValue !== undefined && field.defaultValue !== null) {
        formData.value[field.prop] = field.defaultValue
      } else {
        // 根据字段类型设置默认值
        switch (field.type) {
          case FormFieldType.CHECKBOX:
            formData.value[field.prop] = []
            break
          case FormFieldType.INPUT_NUMBER:
          case FormFieldType.SLIDER:
          case FormFieldType.RATE:
            formData.value[field.prop] = 0
            break
          case FormFieldType.DATE_PICKER:
          case FormFieldType.DATETIME_PICKER:
          case FormFieldType.TIME_PICKER:
            formData.value[field.prop] = null
            break
          case FormFieldType.SWITCH:
            formData.value[field.prop] = false
            break
          default:
            formData.value[field.prop] = ''
        }
      }
    }
  })
}

// 监听配置变化，初始化表单数据
watch(() => props.config, (newConfig) => {
  if (newConfig) {
    // 使用nextTick确保DOM更新后再初始化数据
    nextTick(() => {
      initFormData()
    })
  }
}, { deep: true, immediate: true })

// 获取字段标签
function getFieldLabel(prop: string): string {
  const field = normalizedConfig.value.fields?.find(f => f.prop === prop)
  return field?.label || prop
}

// 格式化值显示
function formatValue(value: any): string {
  if (value === null || value === undefined) {
    return '空'
  }
  
  if (typeof value === 'object') {
    return JSON.stringify(value, null, 2)
  }
  
  return String(value)
}

// 重置表单
function resetForm() {
  initFormData()
  ElMessage.success('表单已重置')
}

// 处理表单提交
function handleSubmit(data: Record<string, any>) {
  emit('submit', data)
  ElMessage.success('表单提交成功')
}

// 处理表单重置
function handleReset() {
  initFormData()
  ElMessage.success('表单已重置')
}

// 处理字段变化
function handleFieldChange(field: string, value: any) {
  console.log('字段变化:', field, value)
}

// 复制表单数据
function copyFormData() {
  const dataStr = JSON.stringify(formData.value, null, 2)
  
  // 创建临时文本区域
  const textarea = document.createElement('textarea')
  textarea.value = dataStr
  document.body.appendChild(textarea)
  textarea.select()
  
  try {
    document.execCommand('copy')
    ElMessage.success('表单数据已复制到剪贴板')
  } catch (error) {
    console.error('复制失败:', error)
    ElMessage.error('复制失败')
  } finally {
    document.body.removeChild(textarea)
  }
}
</script>

<style scoped>
.form-preview {
  display: flex;
  flex-direction: column;
  height: 100%;
}

.preview-header {
  margin-bottom: 16px;
  text-align: center;
}

.preview-header h3 {
  margin: 0 0 8px 0;
  font-size: 18px;
  color: #303133;
}

.preview-description {
  margin: 0;
  font-size: 14px;
  color: #606266;
}

.preview-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
  padding: 8px 0;
  border-bottom: 1px solid #ebeef5;
}

.preview-container {
  flex: 1;
  display: flex;
  justify-content: center;
  overflow: auto;
}

.preview-form-wrapper {
  width: 100%;
  max-width: 100%;
}

/* 响应式预览样式 */
.preview-desktop .preview-form-wrapper {
  max-width: 100%;
}

.preview-tablet .preview-form-wrapper {
  max-width: 768px;
  margin: 0 auto;
}

.preview-mobile .preview-form-wrapper {
  max-width: 375px;
  margin: 0 auto;
}

.form-data-container {
  margin-top: 16px;
}

.form-data-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.form-data-header h4 {
  margin: 0;
  font-size: 16px;
  color: #303133;
}

.form-data-content {
  max-height: 300px;
  overflow: auto;
}

.data-value {
  margin: 0;
  font-family: 'Courier New', monospace;
  font-size: 12px;
  white-space: pre-wrap;
  word-break: break-word;
}
</style>