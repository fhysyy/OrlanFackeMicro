<template>
  <div class="form-config-management">
    <el-card class="page-card">
      <template #header>
        <div class="card-header">
          <span>表单配置管理</span>
          <el-button
            type="primary"
            icon="el-icon-plus"
            @click="handleAddConfig"
          >
            添加表单配置
          </el-button>
        </div>
      </template>

      <!-- 搜索和筛选区域 -->
      <div class="search-area">
        <el-input
          v-model="searchKeyword"
          placeholder="搜索表单名称"
          prefix-icon="el-icon-search"
          style="width: 300px; margin-right: 16px"
          @keyup.enter="handleSearch"
        />
        <el-select
          v-model="searchStatus"
          placeholder="状态筛选"
          style="width: 150px; margin-right: 16px"
        >
          <el-option
            label="全部"
            value=""
          />
          <el-option
            label="启用"
            :value="true"
          />
          <el-option
            label="禁用"
            :value="false"
          />
        </el-select>
        <el-button
          type="primary"
          icon="el-icon-search"
          @click="handleSearch"
        >
          搜索
        </el-button>
        <el-button
          icon="el-icon-refresh-right"
          @click="resetSearch"
        >
          重置
        </el-button>
      </div>

      <!-- 表单配置列表 -->
      <el-table
        v-loading="loading"
        :data="formConfigsData"
        style="width: 100%; margin-top: 20px"
        border
      >
        <el-table-column
          prop="id"
          label="表单ID"
          width="120"
        />
        <el-table-column
          prop="name"
          label="表单名称"
          min-width="150"
        >
          <template #default="{row}">
            <el-link
              type="primary"
              @click="handleViewConfig(row)"
            >
              {{ row.name }}
            </el-link>
          </template>
        </el-table-column>
        <el-table-column
          prop="description"
          label="描述"
          min-width="200"
          show-overflow-tooltip
        />
        <el-table-column
          prop="version"
          label="版本"
          width="80"
        />
        <el-table-column
          prop="status"
          label="状态"
          width="100"
        >
          <template #default="{row}">
            <el-switch
              :model-value="row.status"
              active-text="启用"
              inactive-text="禁用"
              @change="handleStatusChange(row, $event)"
            />
          </template>
        </el-table-column>
        <el-table-column
          prop="createdAt"
          label="创建时间"
          width="180"
        />
        <el-table-column
          prop="updatedAt"
          label="更新时间"
          width="180"
        />
        <el-table-column
          label="操作"
          width="200"
          fixed="right"
        >
          <template #default="{row}">
            <el-button
              type="primary"
              size="small"
              icon="el-icon-edit"
              style="margin-right: 8px"
              @click="handleEditConfig(row)"
            >
              编辑
            </el-button>
            <el-button
              type="success"
              size="small"
              icon="el-icon-view"
              style="margin-right: 8px"
              @click="handlePreviewForm(row)"
            >
              预览
            </el-button>
            <el-button
              type="danger"
              size="small"
              icon="el-icon-delete"
              :disabled="row.status"
              @click="handleDeleteConfig(row)"
            >
              删除
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页 -->
      <div class="pagination-area">
        <el-pagination
          v-model:current-page="pagination.currentPage"
          v-model:page-size="pagination.pageSize"
          :page-sizes="[10, 20, 50, 100]"
          layout="total, sizes, prev, pager, next, jumper"
          :total="total"
          @size-change="handleSizeChange"
          @current-change="handleCurrentChange"
        />
      </div>
    </el-card>

    <!-- 添加/编辑表单配置对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="dialogTitle"
      width="800px"
      destroy-on-close
    >
      <el-form
        ref="formConfigForm"
        :model="formConfigForm"
        :rules="formRules"
        label-width="100px"
      >
        <el-form-item
          label="表单名称"
          prop="name"
          required
        >
          <el-input
            v-model="formConfigForm.name"
            placeholder="请输入表单名称（必填，2-50个字符）"
            show-word-limit
            maxlength="50"
          />
        </el-form-item>
        
        <el-form-item
          label="表单描述"
          prop="description"
        >
          <el-input
            v-model="formConfigForm.description"
            placeholder="请输入表单描述（可选，最多200个字符）"
            type="textarea"
            :rows="3"
            show-word-limit
            maxlength="200"
          />
        </el-form-item>

        <el-form-item
          label="表单配置"
          prop="fields"
        >
          <el-button
            type="primary"
            icon="el-icon-edit-outline"
            style="margin-bottom: 16px"
            @click="openFormBuilder"
          >
            打开表单构建器
          </el-button>
          <el-alert
            title="提示"
            description="点击上方按钮打开表单构建器，可视化配置表单字段和验证规则。"
            type="info"
            show-icon
          />
        </el-form-item>
      </el-form>

      <template #footer>
        <div class="dialog-footer">
          <el-button @click="dialogVisible = false">
            取消
          </el-button>
          <el-button
            type="primary"
            @click="handleSaveConfig"
          >
            保存
          </el-button>
        </div>
      </template>
    </el-dialog>

    <!-- 表单构建器对话框 -->
    <el-dialog
      v-model="formBuilderVisible"
      title="表单构建器"
      width="90%"
      height="80vh"
      destroy-on-close
      fullscreen
    >
      <FormBuilder
        v-if="formBuilderVisible"
        v-model="formConfigForm"
        @save="handleSaveFormBuilder"
      />
    </el-dialog>

    <!-- 表单预览对话框 -->
    <el-dialog
      v-model="previewVisible"
      :title="`预览: ${previewFormName}`"
      width="90%"
      top="5vh"
      destroy-on-close
      class="form-preview-dialog"
    >
      <div v-if="previewFormConfig" class="preview-content">
        <FormPreview 
          v-if="previewFormConfig"
          :config="previewFormConfig"
          @submit="handlePreviewSubmit"
        />
        
        <!-- 空状态提示 -->
        <el-empty v-else description="暂无表单配置" />
      </div>

      <template #footer>
        <div class="dialog-footer">
          <el-button @click="previewVisible = false">
            关闭
          </el-button>
        </div>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, nextTick } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useRouter } from 'vue-router'
import FormBuilder from '@/components/FormBuilder.vue'
import FormGenerator from '@/components/FormGenerator.vue'
import FormPreview from '@/components/form-parts/FormPreview.vue'
import { formConfigService } from '@/services/formConfigService'
import type { FormConfigCreateRequest, CompleteFormConfig, FormConfigListItem } from '@/types/formConfig'
import type { FormField } from '@/types/form'

// 路由实例
const router = useRouter()

// 状态管理
const loading = ref(false)
const dialogVisible = ref(false)
const formBuilderVisible = ref(false)
const previewVisible = ref(false)
const previewFormName = ref('')
const previewFormConfig = ref<FormConfig | null>(null)
const previewFormData = ref<Record<string, any>>({})
const formConfigForm = ref<FormConfigCreateRequest>({
  name: '',
  description: '',
  version: '1.0.0',
  status: 'draft',
  fields: [],
  enabled: true
})

// 搜索条件
const searchKeyword = ref('')
const searchStatus = ref<any>('')

// 分页配置
const pagination = reactive({
  currentPage: 1,
  pageSize: 20
})

// 表单配置列表数据
const formConfigs = ref<FormConfigListItem[]>([])
const total = ref(0)

// 计算过滤后的数据
const formConfigsData = computed(() => {
  let filtered = formConfigs.value
  
  // 按名称搜索
  if (searchKeyword.value) {
    filtered = filtered.filter(config => 
      config.name.toLowerCase().includes(searchKeyword.value.toLowerCase())
    )
  }
  
  // 按状态筛选
  if (searchStatus.value !== '') {
    filtered = filtered.filter(config => config.status === searchStatus.value)
  }
  
  return filtered
})

// 表单验证规则
const formRules = reactive({
  name: [
    { required: true, message: '请输入表单名称', trigger: 'blur' },
    { min: 2, message: '表单名称至少需要2个字符', trigger: 'blur' },
    { max: 50, message: '表单名称不能超过50个字符', trigger: 'blur' }
  ],
  description: [
    { max: 200, message: '表单描述不能超过200个字符', trigger: 'blur' }
  ]
})

// 加载表单配置列表
const loadFormConfigs = async () => {
  loading.value = true
  try {
    const response = await formConfigService.getFormConfigList()
    formConfigs.value = response.list || []
    total.value = response.total
  } catch (error) {
    ElMessage.error('加载表单配置失败')
    console.error('加载表单配置失败:', error)
  } finally {
    loading.value = false
  }
}

// 处理添加表单配置
const handleAddConfig = () => {
  // 重置表单 - 使用深拷贝创建新对象
  formConfigForm.value = JSON.parse(JSON.stringify({
    name: '',
    description: '',
    version: '1.0.0',
    status: 'draft',
    fields: [],
    enabled: true
  }))
  dialogTitle.value = '添加表单配置'
  dialogVisible.value = true
}

// 处理编辑表单配置
const handleEditConfig = (config: CompleteFormConfig) => {
  // 使用FormUtils安全深拷贝，避免直接修改原数据
  import('@/utils/formUtils').then(({ FormUtils }) => {
    const configCopy = FormUtils.safeDeepClone(config)
    
    // 确保id字段和formId字段一致
    if (configCopy.formId && !configCopy.id) {
      configCopy.id = configCopy.formId
    } else if (configCopy.id && !configCopy.formId) {
      configCopy.formId = configCopy.id
    }
    
    formConfigForm.value = configCopy
    dialogTitle.value = '编辑表单配置'
    dialogVisible.value = true
  }).catch(error => {
    console.error('导入FormUtils失败:', error)
    // 降级处理：使用原生JSON方法
    const configCopy = JSON.parse(JSON.stringify(config))
    formConfigForm.value = configCopy
    dialogTitle.value = '编辑表单配置'
    dialogVisible.value = true
  })
}

// 处理查看表单配置
const handleViewConfig = (config: FormConfigListItem) => {
  router.push({
    name: 'ConfigurableForm',
    query: { id: config.id }
  })
}

// 删除表单配置
const handleDeleteConfig = async (config: FormConfigListItem) => {
  try {
    await ElMessageBox.confirm(
      `确定要删除表单配置「${config.name}」吗？删除后不可恢复。`,
      '确认删除',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }
    )
    
    await formConfigService.deleteFormConfig(config.id)
    ElMessage.success('删除成功')
    
    // 从列表中移除
    const index = formConfigs.value.findIndex(item => item.id === config.id)
    if (index !== -1) {
      formConfigs.value.splice(index, 1)
      total.value--
    }
  } catch (error) {
    // 用户取消操作或发生错误
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}

// 处理状态变更
const handleStatusChange = async (config: FormConfigListItem, newStatus: boolean) => {
  try {
    await formConfigService.updateFormConfig(config.id, { enabled: newStatus })
    ElMessage.success('状态更新成功')
    
    // 更新本地数据
    const index = formConfigs.value.findIndex(item => item.id === config.id)
    if (index !== -1) {
      formConfigs.value[index].enabled = newStatus
    }
  } catch (error) {
    ElMessage.error('状态更新失败')
    // 恢复原状态
    config.enabled = !newStatus
  }
}

// 导入表单工具，避免重复代码
// 这些函数已移至 /utils/formUtils.ts 中的 FormUtils 类

// 打开表单构建器
const openFormBuilder = () => {
  try {
    // 使用动态导入，避免同步导入问题
    import('@/utils/formUtils').then(({ FormUtils }) => {
      // 检测并处理循环引用
      if (FormUtils.hasCircularReference(formConfigForm.value)) {
        console.warn('检测到循环引用，使用安全克隆...')
        const safeForm = FormUtils.safeDeepClone(formConfigForm.value)
        formConfigForm.value = safeForm
      }
      
      // 确保formConfigForm有正确的id和formId字段
      if (!formConfigForm.value.formId) {
        formConfigForm.value.formId = formConfigForm.value.id || ''
      }
      if (!formConfigForm.value.id) {
        formConfigForm.value.id = formConfigForm.value.formId || ''
      }
      
      // 规范化表单配置
      formConfigForm.value = FormUtils.normalizeFormConfig(formConfigForm.value)
      
      formBuilderVisible.value = true
    }).catch(error => {
      console.error('导入FormUtils失败，使用降级方案:', error)
      // 降级处理：不进行特殊处理，直接打开表单构建器
      formBuilderVisible.value = true
    })
  } catch (error) {
    console.error('打开表单构建器时出错:', error)
    ElMessage.error('打开表单构建器失败，请检查表单配置是否正确')
  }
}

// 保存表单构建器配置
const handleSaveFormBuilder = () => {
  formBuilderVisible.value = false
  ElMessage.success('表单配置已更新')
}

// 保存表单配置
const handleSaveConfig = async () => {
  try {
    if (formConfigForm.value.id) {
      // 更新现有配置
      await formConfigService.updateFormConfig(
        formConfigForm.value.id,
        formConfigForm.value
      )
      ElMessage.success('更新成功')
    } else {
      // 创建新配置
      await formConfigService.createFormConfig(formConfigForm.value)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    loadFormConfigs()
  } catch (error) {
    ElMessage.error('保存失败')
    console.error('保存表单配置失败:', error)
  }
}

// 预览表单
const handlePreviewForm = async (config: FormConfig) => {
  previewFormName.value = config.name || config.title || '表单预览'
  
  // 防御性检查，确保配置存在
  if (!config) {
    ElMessage.error('表单配置不存在')
    return
  }
  
  try {
    // 确保每个字段都有prop属性
    const normalizedConfig = {
      ...config,
      fields: config.fields?.map(field => {
        // 确保字段有prop属性
        if (!field.prop && field.name) {
          return { ...field, prop: field.name }
        } else if (!field.prop && field.fieldKey) {
          return { ...field, prop: field.fieldKey }
        } else if (!field.prop) {
          return { ...field, prop: `field-${Math.random().toString(36).substr(2, 9)}` }
        }
        
        // 确保字段有type属性
        if (!field.type) {
          return { ...field, type: FormFieldType.INPUT }
        }
        
        return field
      }) || []
    }
    
    previewFormConfig.value = normalizedConfig
    previewFormData.value = {}
    
    // 初始化表单数据
    normalizedConfig.fields?.forEach(field => {
      if (field.prop) {
        if (field.defaultValue !== undefined && field.defaultValue !== null) {
          previewFormData.value[field.prop] = field.defaultValue
        } else {
          // 根据字段类型设置默认值
          switch (field.type) {
            case FormFieldType.CHECKBOX:
              previewFormData.value[field.prop] = []
              break
            case FormFieldType.INPUT_NUMBER:
            case FormFieldType.SLIDER:
            case FormFieldType.RATE:
              previewFormData.value[field.prop] = 0
              break
            case FormFieldType.DATE_PICKER:
            case FormFieldType.DATETIME_PICKER:
            case FormFieldType.TIME_PICKER:
              previewFormData.value[field.prop] = null
              break
            case FormFieldType.SWITCH:
              previewFormData.value[field.prop] = false
              break
            default:
              previewFormData.value[field.prop] = ''
          }
        }
      }
    })
    
    // 使用nextTick确保DOM更新后再显示预览
    nextTick(() => {
      previewVisible.value = true
    })
  } catch (error) {
    console.error('处理表单预览失败:', error)
    ElMessage.error('表单预览处理失败')
  }
}

// 预览表单提交
const handlePreviewSubmit = (data: Record<string, any>) => {
  ElMessage.success('表单提交成功！')
  console.log('预览表单数据:', data)
}

// 搜索
const handleSearch = () => {
  pagination.currentPage = 1
  // 搜索逻辑已经在computed中处理
}

// 重置搜索
const resetSearch = () => {
  searchKeyword.value = ''
  searchStatus.value = ''
  pagination.currentPage = 1
}

// 分页处理
const handleSizeChange = (size: number) => {
  pagination.pageSize = size
}

const handleCurrentChange = (current: number) => {
  pagination.currentPage = current
}

// 对话框标题
const dialogTitle = computed(() => {
  return formConfigForm.value.id ? '编辑表单配置' : '添加表单配置'
})

// 初始化
onMounted(() => {
  loadFormConfigs()
})
</script>

<style scoped>
.form-config-management {
  padding: 20px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.search-area {
  display: flex;
  align-items: center;
  margin-top: 16px;
}

.pagination-area {
  display: flex;
  justify-content: flex-end;
  margin-top: 20px;
}

.dialog-footer {
  display: flex;
  justify-content: flex-end;
}

/* 表单预览对话框样式 */
:deep(.form-preview-dialog) {
  .el-dialog__body {
    padding: 10px;
  }
  
  .preview-content {
    height: 70vh;
    min-height: 400px;
  }
}
</style>