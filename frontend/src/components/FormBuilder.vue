<template>
  <div class="form-builder">
    <!-- 表单构建器头部 -->
    <el-card
      shadow="never"
      class="builder-header"
    >
      <template #header>
        <div class="builder-header-content">
          <h2 class="builder-title">
            {{ title || '表单构建器' }}
          </h2>
          <el-button-group>
            <el-button
              type="primary"
              @click="saveConfig"
            >
              保存配置
            </el-button>
            <el-button @click="resetConfig">
              重置
            </el-button>
            <el-button @click="previewForm = !previewForm">
              {{ previewForm ? '编辑' : '预览' }}
            </el-button>
          </el-button-group>
        </div>
      </template>
      
      <!-- 表单基本配置 -->
      <el-form
        :model="formConfig"
        label-position="top"
        class="basic-config"
      >
        <el-row :gutter="20">
          <el-col :span="8">
            <el-form-item label="表单名称">
              <el-input
                v-model="formConfig.name"
                placeholder="请输入表单名称"
              />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="表单描述">
              <el-input
                v-model="formConfig.description"
                placeholder="请输入表单描述"
              />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="表单状态">
              <el-select v-model="formConfig.status">
                <el-option
                  label="草稿"
                  value="draft"
                />
                <el-option
                  label="已发布"
                  value="published"
                />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        
        <el-row :gutter="20">
          <el-col :span="8">
            <el-form-item label="标签位置">
              <el-radio-group v-model="formConfig.labelPosition">
                <el-radio label="left">
                  左对齐
                </el-radio>
                <el-radio label="right">
                  右对齐
                </el-radio>
                <el-radio label="top">
                  顶部
                </el-radio>
              </el-radio-group>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="标签宽度">
              <el-input
                v-model="formConfig.labelWidth"
                placeholder="如：120px"
              />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="表单布局">
              <el-radio-group v-model="formConfig.layout">
                <el-radio label="horizontal">
                  水平
                </el-radio>
                <el-radio label="vertical">
                  垂直
                </el-radio>
              </el-radio-group>
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
    </el-card>

    <!-- 预览模式 -->
    <template v-if="previewForm">
      <el-card
        shadow="never"
        class="preview-section"
      >
        <template #header>
          <div class="preview-header">
            <h3>表单预览</h3>
          </div>
        </template>
        <FormGenerator 
          :config="{
            fields: formConfig.fields || [],
            labelPosition: formConfig.labelPosition || 'right',
            labelWidth: formConfig.labelWidth || '120px',
            layout: formConfig.layout || 'grid',
            gutter: formConfig.gutter || 20,
            showSubmitButton: true,
            showResetButton: true,
            submitButtonText: '提交',
            resetButtonText: '重置'
          }"
          @submit="handlePreviewSubmit"
        />
      </el-card>
    </template>

    <!-- 编辑模式 -->
    <template v-else>
      <!-- 字段工具箱 -->
      <el-card
        shadow="never"
        class="toolbox-section"
      >
        <template #header>
          <div class="toolbox-header">
            <h3>字段工具箱</h3>
            <span class="toolbox-desc">拖拽或点击添加字段</span>
          </div>
        </template>
        <div class="toolbox-fields">
          <div 
            v-for="fieldType in fieldTypes" 
            :key="fieldType.value"
            class="toolbox-field-item"
            @click="addField(fieldType.value)"
          >
            <el-icon><component :is="fieldType.icon" /></el-icon>
            <span>{{ fieldType.label }}</span>
          </div>
        </div>
      </el-card>

      <!-- 表单画布 -->
      <el-card
        shadow="never"
        class="canvas-section"
      >
        <template #header>
          <div class="canvas-header">
            <h3>表单画布</h3>
            <span class="field-count">字段数量: {{ formConfig.fields.length }}</span>
          </div>
        </template>
        
        <div class="canvas-fields">
          <!-- 空状态 -->
          <div
            v-if="formConfig.fields.length === 0"
            class="empty-canvas"
          >
            <el-empty description="从左侧工具箱添加字段" />
          </div>
          
          <!-- 字段列表 -->
          <div 
            v-for="(field, index) in formConfig.fields" 
            :key="field.prop || `field-${index}`"
            class="canvas-field-item"
            :class="{ 'active': selectedFieldIndex === index }"
            @click="editField(index)"
          >
            <div class="field-item-header">
              <div class="field-item-title">
                <el-icon><component :is="getFieldIcon(field.type)" /></el-icon>
                <span>{{ field.label || '未命名字段' }}</span>
                <el-tag
                  v-if="field.required"
                  size="small"
                  type="danger"
                  effect="plain"
                >
                  必填
                </el-tag>
              </div>
              <div class="field-item-actions">
                <el-button 
                  size="small" 
                  :disabled="index === 0"
                  @click.stop="moveField(index, index - 1)"
                >
                  <el-icon><ArrowUp /></el-icon>
                </el-button>
                <el-button 
                  size="small" 
                  :disabled="index === formConfig.fields.length - 1"
                  @click.stop="moveField(index, index + 1)"
                >
                  <el-icon><ArrowDown /></el-icon>
                </el-button>
                <el-button 
                  size="small" 
                  @click.stop="editField(index)"
                >
                  <el-icon><Edit /></el-icon>
                </el-button>
                <el-button 
                  size="small" 
                  type="danger" 
                  @click.stop="deleteField(index)"
                >
                  <el-icon><Delete /></el-icon>
                </el-button>
              </div>
            </div>
            <div class="field-item-preview">
              <FormGenerator 
                :config="{
                  fields: [field],
                  labelPosition: 'top'
                }"
                :disabled="true"
              />
            </div>
          </div>
        </div>
      </el-card>

      <!-- 字段配置面板 -->
      <el-card 
        v-if="selectedFieldIndex !== -1 && formConfig.fields[selectedFieldIndex]" 
        shadow="never"
        class="config-panel-section"
      >
        <template #header>
          <div class="config-panel-header">
            <h3>字段配置</h3>
            <span class="field-type-label">{{ getFieldTypeName(selectedFieldIndex) }}</span>
          </div>
        </template>
        
        <div class="config-panel-content">
          <el-form
            :model="selectedField"
            label-position="top"
            class="field-config-form"
          >
            <!-- 基本信息 -->
            <el-collapse v-model="activeConfigPanels">
              <el-collapse-item
                name="basic"
                title="基本信息"
              >
                <el-row :gutter="20">
                  <el-col :span="24">
                    <el-form-item label="字段标签">
                      <el-input
                        v-model="selectedField.label"
                        placeholder="请输入字段标签"
                      />
                    </el-form-item>
                  </el-col>
                  <el-col :span="24">
                    <el-form-item label="字段属性名">
                      <el-input
                        v-model="selectedField.prop"
                        placeholder="请输入字段属性名（英文）"
                      />
                    </el-form-item>
                  </el-col>
                  <el-col :span="24">
                    <el-form-item label="占位符">
                      <el-input
                        v-model="selectedField.placeholder"
                        placeholder="请输入占位符文本"
                      />
                    </el-form-item>
                  </el-col>
                  <el-col :span="24">
                    <el-form-item label="帮助文本">
                      <el-input
                        v-model="selectedField.helpText"
                        placeholder="请输入帮助文本"
                        type="textarea"
                        :rows="2"
                      />
                    </el-form-item>
                  </el-col>
                  <el-col :span="12">
                    <el-form-item label="是否必填">
                      <el-switch v-model="selectedField.required" />
                    </el-form-item>
                  </el-col>
                  <el-col :span="12">
                    <el-form-item label="是否禁用">
                      <el-switch v-model="selectedField.disabled" />
                    </el-form-item>
                  </el-col>
                  <el-col :span="12">
                    <el-form-item label="数据库类型">
                      <el-select v-model="selectedField.databaseType">
                        <el-option 
                          v-for="type in databaseTypes" 
                          :key="type.value" 
                          :label="type.label" 
                          :value="type.value" 
                        />
                      </el-select>
                    </el-form-item>
                  </el-col>
                  <el-col :span="12">
                    <el-form-item label="字段长度">
                      <el-input-number
                        v-model="selectedField.databaseLength"
                        :min="1"
                      />
                    </el-form-item>
                  </el-col>
                </el-row>
              </el-collapse-item>

              <!-- 验证规则 -->
              <el-collapse-item
                name="validation"
                title="验证规则"
              >
                <div class="validation-rules">
                  <div 
                    v-for="(rule, ruleIndex) in selectedField.validationRules" 
                    :key="ruleIndex"
                    class="validation-rule-item"
                  >
                    <el-row
                      :gutter="10"
                      align="middle"
                    >
                      <el-col :span="8">
                        <el-select
                          v-model="rule.type"
                          placeholder="选择验证类型"
                        >
                          <el-option 
                            v-for="ruleType in validationRuleTypes" 
                            :key="ruleType.value" 
                            :label="ruleType.label" 
                            :value="ruleType.value" 
                          />
                        </el-select>
                      </el-col>
                      <el-col :span="8">
                        <el-input 
                          v-if="['min', 'max', 'len', 'pattern'].includes(rule.type)" 
                          v-model="rule.value" 
                          :placeholder="getRuleValuePlaceholder(rule.type)" 
                        />
                      </el-col>
                      <el-col :span="6">
                        <el-select
                          v-model="rule.trigger"
                          placeholder="触发方式"
                        >
                          <el-option
                            label="失去焦点"
                            value="blur"
                          />
                          <el-option
                            label="内容变更"
                            value="change"
                          />
                          <el-option
                            label="获取焦点"
                            value="focus"
                          />
                        </el-select>
                      </el-col>
                      <el-col :span="2">
                        <el-button 
                          size="small" 
                          type="danger" 
                          @click="deleteValidationRule(ruleIndex)"
                        >
                          <el-icon><Delete /></el-icon>
                        </el-button>
                      </el-col>
                    </el-row>
                    <el-input 
                      v-model="rule.message" 
                      placeholder="请输入错误提示信息" 
                      style="margin-top: 5px;"
                    />
                  </div>
                  
                  <el-button
                    type="primary"
                    plain
                    class="add-rule-btn"
                    @click="addValidationRule"
                  >
                    <el-icon><Plus /></el-icon> 添加验证规则
                  </el-button>
                </div>
              </el-collapse-item>

              <!-- 高级配置 -->
              <el-collapse-item
                name="advanced"
                title="高级配置"
              >
                <el-row :gutter="20">
                  <el-col :span="12">
                    <el-form-item label="栅格列数">
                      <el-input-number
                        v-model="selectedField.span"
                        :min="1"
                        :max="24"
                      />
                    </el-form-item>
                  </el-col>
                  <el-col :span="12">
                    <el-form-item label="字段宽度">
                      <el-input
                        v-model="selectedField.width"
                        placeholder="如：300px"
                      />
                    </el-form-item>
                  </el-col>
                  
                  <!-- 针对不同字段类型的特定配置 -->
                  <template v-if="selectedField.type === 'input'">
                    <el-col :span="24">
                      <el-form-item label="输入框类型">
                        <el-select v-model="selectedField.inputType">
                          <el-option
                            label="文本"
                            value="text"
                          />
                          <el-option
                            label="密码"
                            value="password"
                          />
                          <el-option
                            label="邮箱"
                            value="email"
                          />
                          <el-option
                            label="URL"
                            value="url"
                          />
                          <el-option
                            label="手机号"
                            value="tel"
                          />
                          <el-option
                            label="数字"
                            value="number"
                          />
                        </el-select>
                      </el-form-item>
                    </el-col>
                    <el-col :span="12">
                      <el-form-item label="最大长度">
                        <el-input-number
                          v-model="selectedField.maxlength"
                          :min="1"
                        />
                      </el-form-item>
                    </el-col>
                    <el-col :span="12">
                      <el-form-item label="是否可清空">
                        <el-switch v-model="selectedField.clearable" />
                      </el-form-item>
                    </el-col>
                  </template>
                  
                  <template v-if="['date-picker', 'datetime-picker'].includes(selectedField.type)">
                    <el-col :span="24">
                      <el-form-item label="日期格式">
                        <el-input
                          v-model="selectedField.format"
                          placeholder="如：YYYY-MM-DD"
                        />
                      </el-form-item>
                    </el-col>
                  </template>
                </el-row>
              </el-collapse-item>
            </el-collapse>
          </el-form>
        </div>
      </el-card>
    </template>
  </div>
</template>

<script lang="ts" setup>
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { ArrowUp, ArrowDown, Edit, Delete, Plus } from '@element-plus/icons-vue'
import FormGenerator from './FormGenerator.vue'
import type { CompleteFormConfig, FormField, ValidationRule } from '../types/formConfig'
import { FormFieldType } from '../types/form'
import { formService } from '../services/formService'

// Props
const props = defineProps<{
  modelValue?: CompleteFormConfig
  title?: string
  disabled?: boolean
}>()

// Emits
const emit = defineEmits<{
  'update:modelValue': [value: CompleteFormConfig]
  'save': [config: CompleteFormConfig]
  'preview-submit': [data: Record<string, any>]
}>()

// 响应式数据
const formConfig = reactive<CompleteFormConfig>({
  id: '',
  formId: '', // 添加formId字段以兼容FormConfigManagement.vue中的使用
  name: '未命名表单',
  description: '',
  type: 'default',
  version: '1.0.0',
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  status: 'draft',
  fields: [],
  initialData: {},
  labelPosition: 'right',
  labelWidth: '120px',
  showResetButton: true,
  showSubmitButton: true,
  submitButtonText: '提交',
  resetButtonText: '重置',
  layout: 'horizontal'
})

const selectedFieldIndex = ref(-1)
const previewForm = ref(false)
const activeConfigPanels = ref(['basic', 'validation'])

// 字段类型配置
const fieldTypes = [
  { label: '单行文本', value: FormFieldType.INPUT, icon: 'Document' },
  { label: '多行文本', value: FormFieldType.TEXTAREA, icon: 'EditPen' },
  { label: '数字输入', value: FormFieldType.INPUT_NUMBER, icon: 'Calculator' },
  { label: '下拉选择', value: FormFieldType.SELECT, icon: 'ArrowDown' },
  { label: '单选框组', value: FormFieldType.RADIO, icon: 'Radio' },
  { label: '复选框组', value: FormFieldType.CHECKBOX, icon: 'Checkbox' },
  { label: '开关', value: FormFieldType.SWITCH, icon: 'Switch' },
  { label: '日期选择', value: FormFieldType.DATE_PICKER, icon: 'Calendar' },
  { label: '时间选择', value: FormFieldType.TIME_PICKER, icon: 'Timer' },
  { label: '日期时间选择', value: FormFieldType.DATETIME_PICKER, icon: 'Bell' },
  { label: '评分', value: FormFieldType.RATE, icon: 'StarFilled' },
  { label: '滑块', value: FormFieldType.SLIDER, icon: 'Operation' }
]

// 数据库类型配置
const databaseTypes = [
  { label: '字符串', value: 'varchar' },
  { label: '文本', value: 'text' },
  { label: '整数', value: 'int' },
  { label: '长整数', value: 'bigint' },
  { label: '小数', value: 'decimal' },
  { label: '布尔值', value: 'boolean' },
  { label: '日期', value: 'date' },
  { label: '时间', value: 'time' },
  { label: '日期时间', value: 'datetime' },
  { label: '时间戳', value: 'timestamp' },
  { label: 'JSON', value: 'json' },
  { label: '枚举', value: 'enum' }
]

// 验证规则类型配置
const validationRuleTypes = [
  { label: '必填', value: 'required' },
  { label: '最小长度', value: 'min' },
  { label: '最大长度', value: 'max' },
  { label: '固定长度', value: 'len' },
  { label: '邮箱', value: 'email' },
  { label: '手机号', value: 'phone' },
  { label: '身份证号', value: 'idcard' },
  { label: '不含中文', value: 'noChinese' },
  { label: '包含数字', value: 'hasNumber' },
  { label: '包含特殊字符', value: 'hasSpecialChar' },
  { label: '正则表达式', value: 'pattern' }
]

// 计算属性 - 直接返回原始字段对象引用以确保双向绑定正常工作
const selectedField = computed({
  get: () => {
    // 添加额外的安全检查，确保在组件完全初始化后再访问
    if (selectedFieldIndex.value === -1 || !formConfig.fields || selectedFieldIndex.value >= formConfig.fields.length || !formConfig.fields[selectedFieldIndex.value]) {
      return {} as FormField
    }
    // 直接返回原始字段对象引用，确保v-model能够正确更新
    return formConfig.fields[selectedFieldIndex.value]
  },
  set: (value) => {
    // 确保所有依赖都已正确初始化
    if (selectedFieldIndex.value === -1 || !formConfig.fields || selectedFieldIndex.value >= formConfig.fields.length) {
      return
    }
    // 使用深度合并确保所有嵌套属性都能被正确更新
    const deepMerge = (target: any, source: any) => {
      for (const key in source) {
        if (typeof source[key] === 'object' && source[key] !== null && key in target) {
          deepMerge(target[key], source[key])
        } else {
          target[key] = source[key]
        }
      }
    }
    deepMerge(formConfig.fields[selectedFieldIndex.value], value)
  }
})

// 检测循环引用的辅助函数
function hasCircularReference(obj: any, seen = new Set()): boolean {
  if (obj === null || typeof obj !== 'object') {
    return false
  }
  
  if (seen.has(obj)) {
    return true
  }
  
  seen.add(obj)
  
  for (const key in obj) {
    if (Object.prototype.hasOwnProperty.call(obj, key)) {
      if (hasCircularReference(obj[key], seen)) {
        return true
      }
    }
  }
  
  seen.delete(obj)
  return false
}

// 安全地复制对象，避免循环引用
function safeClone(obj: any): any {
  if (obj === null || typeof obj !== 'object') {
    return obj
  }
  
  // 如果是数组
  if (Array.isArray(obj)) {
    const clonedArray: any[] = []
    for (let i = 0; i < obj.length; i++) {
      clonedArray[i] = safeClone(obj[i])
    }
    return clonedArray
  }
  
  // 如果是对象
  const clonedObj: Record<string, any> = {}
  for (const key in obj) {
    if (Object.prototype.hasOwnProperty.call(obj, key)) {
      // 跳过可能导致循环引用的属性
      if (key === 'fieldValue' && typeof obj[key] === 'object') {
        continue
      }
      clonedObj[key] = safeClone(obj[key])
    }
  }
  return clonedObj
}

// 初始化表单配置
const initFormConfig = () => {
  if (props.modelValue) {
    try {
      // 检测并处理循环引用
      if (hasCircularReference(props.modelValue)) {
        console.warn('检测到循环引用，尝试修复...')
        // 使用安全的深拷贝避免循环引用
        const configCopy = safeClone(props.modelValue)
        // 确保id和formId字段都存在
        if (configCopy.formId && !configCopy.id) {
          configCopy.id = configCopy.formId
        }
        if (configCopy.id && !configCopy.formId) {
          configCopy.formId = configCopy.id
        }
        Object.assign(formConfig, configCopy)
      } else {
        // 使用标准深拷贝
        const configCopy = JSON.parse(JSON.stringify(props.modelValue))
        // 确保id和formId字段都存在
        if (configCopy.formId && !configCopy.id) {
          configCopy.id = configCopy.formId
        }
        if (configCopy.id && !configCopy.formId) {
          configCopy.formId = configCopy.id
        }
        Object.assign(formConfig, configCopy)
      }
    } catch (error) {
      console.error('初始化表单配置时出错:', error)
      ElMessage.error('初始化表单配置失败，请检查表单配置是否正确')
    }
  }
}

const addField = (fieldType: FormFieldType) => {
  const newField = formService.createInputField(`field_${Date.now()}`, '新字段', {
    type: fieldType,
    validationRules: []
  })
  
  // 根据字段类型创建不同的字段
  switch (fieldType) {
  case FormFieldType.INPUT:
    break
  case FormFieldType.TEXTAREA:
    newField.type = FormFieldType.TEXTAREA
    break
  case FormFieldType.INPUT_NUMBER:
    newField.type = FormFieldType.INPUT_NUMBER
    break
  case FormFieldType.SELECT:
    newField.type = FormFieldType.SELECT
    newField.options = []
    break
  case FormFieldType.RADIO:
    newField.type = FormFieldType.RADIO
    newField.options = []
    break
  case FormFieldType.CHECKBOX:
    newField.type = FormFieldType.CHECKBOX
    newField.options = []
    break
  case FormFieldType.SWITCH:
    newField.type = FormFieldType.SWITCH
    break
  case FormFieldType.DATE_PICKER:
    newField.type = FormFieldType.DATE_PICKER
    newField.pickerType = 'date'
    newField.format = 'YYYY-MM-DD'
    break
  case FormFieldType.TIME_PICKER:
    newField.type = FormFieldType.TIME_PICKER
    newField.pickerType = 'time'
    newField.format = 'HH:mm:ss'
    break
  case FormFieldType.DATETIME_PICKER:
    newField.type = FormFieldType.DATETIME_PICKER
    newField.pickerType = 'datetime'
    newField.format = 'YYYY-MM-DD HH:mm:ss'
    break
  case FormFieldType.RATE:
    newField.type = FormFieldType.RATE
    break
  case FormFieldType.SLIDER:
    newField.type = FormFieldType.SLIDER
    break
  }
  
  formConfig.fields.push(newField)
  selectedFieldIndex.value = formConfig.fields.length - 1
}

const editField = (index: number) => {
  selectedFieldIndex.value = index
}

const deleteField = (index: number) => {
  ElMessageBox.confirm('确定要删除这个字段吗？', '提示', {
    confirmButtonText: '确定',
    cancelButtonText: '取消',
    type: 'warning'
  }).then(() => {
    formConfig.fields.splice(index, 1)
    if (selectedFieldIndex.value === index) {
      selectedFieldIndex.value = -1
    } else if (selectedFieldIndex.value > index) {
      selectedFieldIndex.value--
    }
    ElMessage.success('字段删除成功')
  }).catch(() => {
    // 取消删除
  })
}

const moveField = (fromIndex: number, toIndex: number) => {
  if (toIndex < 0 || toIndex >= formConfig.fields.length) return
  
  const [movedField] = formConfig.fields.splice(fromIndex, 1)
  formConfig.fields.splice(toIndex, 0, movedField)
  
  if (selectedFieldIndex.value === fromIndex) {
    selectedFieldIndex.value = toIndex
  } else if (selectedFieldIndex.value > fromIndex && selectedFieldIndex.value <= toIndex) {
    selectedFieldIndex.value--
  } else if (selectedFieldIndex.value < fromIndex && selectedFieldIndex.value >= toIndex) {
    selectedFieldIndex.value++
  }
}

const addValidationRule = () => {
  if (!selectedField.value.validationRules) {
    selectedField.value.validationRules = []
  }
  
  const newRule: ValidationRule = {
    type: 'required',
    message: '请输入错误提示信息',
    trigger: 'blur'
  }
  
  selectedField.value.validationRules.push(newRule)
}

const deleteValidationRule = (index: number) => {
  if (selectedField.value.validationRules) {
    selectedField.value.validationRules.splice(index, 1)
  }
}

const getFieldIcon = (fieldType: FormFieldType) => {
  const field = fieldTypes.find(f => f.value === fieldType)
  return field?.icon || 'Document'
}

const getFieldTypeName = (index: number) => {
  if (index === -1 || !formConfig.fields[index]) return ''
  const fieldType = formConfig.fields[index].type
  const field = fieldTypes.find(f => f.value === fieldType)
  return field?.label || '未知字段类型'
}

const getRuleValuePlaceholder = (ruleType: string) => {
  const placeholders: Record<string, string> = {
    min: '最小值',
    max: '最大值',
    len: '固定长度',
    pattern: '正则表达式'
  }
  return placeholders[ruleType] || ''
}

const saveConfig = () => {
  formConfig.updatedAt = new Date().toISOString()
  emit('update:modelValue', formConfig)
  emit('save', formConfig)
  ElMessage.success('表单配置保存成功')
}

const resetConfig = () => {
  ElMessageBox.confirm('确定要重置表单配置吗？这将清空所有已配置的字段。', '提示', {
    confirmButtonText: '确定',
    cancelButtonText: '取消',
    type: 'warning'
  }).then(() => {
    initFormConfig()
    selectedFieldIndex.value = -1
    ElMessage.success('表单配置已重置')
  }).catch(() => {
    // 取消重置
  })
}

const handlePreviewSubmit = (data: Record<string, any>) => {
  emit('preview-submit', data)
  ElMessage.success('表单提交成功，这是预览数据：' + JSON.stringify(data))
}

// 生命周期
onMounted(() => {
  initFormConfig()
})

// 监听属性变化
watch(() => props.modelValue, (newValue) => {
  if (newValue) {
    try {
      // 检测并处理循环引用
      if (hasCircularReference(newValue)) {
        console.warn('检测到循环引用，尝试修复...')
        // 使用安全的深拷贝避免循环引用
        const configCopy = safeClone(newValue)
        // 确保id和formId字段都存在
        if (configCopy.formId && !configCopy.id) {
          configCopy.id = configCopy.formId
        }
        if (configCopy.id && !configCopy.formId) {
          configCopy.formId = configCopy.id
        }
        Object.assign(formConfig, configCopy)
      } else {
        // 使用标准深拷贝
        const configCopy = JSON.parse(JSON.stringify(newValue))
        // 确保id和formId字段都存在
        if (configCopy.formId && !configCopy.id) {
          configCopy.id = configCopy.formId
        }
        if (configCopy.id && !configCopy.formId) {
          configCopy.formId = configCopy.id
        }
        Object.assign(formConfig, configCopy)
      }
      // 重置选中的字段索引
      selectedFieldIndex.value = -1
    } catch (error) {
      console.error('处理modelValue变化时出错:', error)
      ElMessage.error('处理表单配置变化失败，请检查表单配置是否正确')
    }
  }
}, { deep: true })
</script>

<style scoped>
.form-builder {
  padding: 20px;
  background-color: #f5f7fa;
  min-height: 100vh;
}

/* 头部样式 */
.builder-header {
  margin-bottom: 20px;
}

.builder-header-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.builder-title {
  margin: 0;
  font-size: 20px;
  font-weight: 500;
}

/* 基本配置样式 */
.basic-config {
  margin-top: 20px;
}

/* 工具箱样式 */
.toolbox-section {
  margin-bottom: 20px;
}

.toolbox-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.toolbox-desc {
  font-size: 12px;
  color: #909399;
}

.toolbox-fields {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  margin-top: 10px;
}

.toolbox-field-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 15px;
  background-color: #fff;
  border: 1px solid #dcdfe6;
  border-radius: 4px;
  cursor: pointer;
  transition: all 0.3s;
  min-width: 80px;
}

.toolbox-field-item:hover {
  border-color: #409eff;
  color: #409eff;
  box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
}

.toolbox-field-item .el-icon {
  font-size: 24px;
  margin-bottom: 5px;
}

/* 画布样式 */
.canvas-section {
  margin-bottom: 20px;
}

.canvas-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.field-count {
  font-size: 12px;
  color: #909399;
}

.canvas-fields {
  min-height: 200px;
  padding: 20px;
  background-color: #fafafa;
  border: 2px dashed #dcdfe6;
  border-radius: 4px;
  margin-top: 10px;
}

.empty-canvas {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 200px;
}

.canvas-field-item {
  background-color: #fff;
  border: 1px solid #dcdfe6;
  border-radius: 4px;
  margin-bottom: 15px;
  transition: all 0.3s;
}

.canvas-field-item:hover {
  border-color: #409eff;
  box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
}

.canvas-field-item.active {
  border-color: #409eff;
  box-shadow: 0 0 0 2px rgba(64, 158, 255, 0.2);
}

.field-item-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 15px;
  background-color: #f5f7fa;
  border-bottom: 1px solid #ebeef5;
}

.field-item-title {
  display: flex;
  align-items: center;
  gap: 8px;
}

.field-item-title .el-icon {
  color: #409eff;
}

.field-item-actions {
  display: flex;
  gap: 5px;
}

.field-item-preview {
  padding: 15px;
}

/* 配置面板样式 */
.config-panel-section {
  margin-bottom: 20px;
}

.config-panel-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.field-type-label {
  font-size: 12px;
  color: #909399;
  background-color: #f5f7fa;
  padding: 2px 8px;
  border-radius: 10px;
}

.config-panel-content {
  margin-top: 10px;
}

/* 验证规则样式 */
.validation-rules {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.validation-rule-item {
  padding: 10px;
  background-color: #f5f7fa;
  border-radius: 4px;
}

.add-rule-btn {
  margin-top: 10px;
}

/* 预览模式样式 */
.preview-section {
  margin-bottom: 20px;
}

.preview-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
</style>
