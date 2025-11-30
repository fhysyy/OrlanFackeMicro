<template>
  <div class="form-config-management">
    <el-card class="page-card">
      <template #header>
        <div class="card-header">
          <span>表单配置管理</span>
          <el-button type="primary" @click="handleAddConfig" icon="el-icon-plus">添加表单配置</el-button>
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
          <el-option label="全部" value="" />
          <el-option label="启用" :value="true" />
          <el-option label="禁用" :value="false" />
        </el-select>
        <el-button type="primary" @click="handleSearch" icon="el-icon-search">搜索</el-button>
        <el-button @click="resetSearch" icon="el-icon-refresh-right">重置</el-button>
      </div>

      <!-- 表单配置列表 -->
      <el-table
        v-loading="loading"
        :data="formConfigsData"
        style="width: 100%; margin-top: 20px"
        border
      >
        <el-table-column prop="formId" label="表单ID" width="120" />
        <el-table-column prop="name" label="表单名称" min-width="150">
          <template #default="{row}">
            <el-link type="primary" @click="handleViewConfig(row)">{{ row.name }}</el-link>
          </template>
        </el-table-column>
        <el-table-column prop="description" label="描述" min-width="200" show-overflow-tooltip />
        <el-table-column prop="version" label="版本" width="80" />
        <el-table-column prop="status" label="状态" width="100">
          <template #default="{row}">
            <el-switch
              v-model="row.status"
              active-text="启用"
              inactive-text="禁用"
              @change="handleStatusChange(row)"
            />
          </template>
        </el-table-column>
        <el-table-column prop="createdAt" label="创建时间" width="180" />
        <el-table-column prop="updatedAt" label="更新时间" width="180" />
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="{row}">
            <el-button
              type="primary"
              size="small"
              @click="handleEditConfig(row)"
              icon="el-icon-edit"
              style="margin-right: 8px"
            >
              编辑
            </el-button>
            <el-button
              type="success"
              size="small"
              @click="handlePreviewForm(row)"
              icon="el-icon-view"
              style="margin-right: 8px"
            >
              预览
            </el-button>
            <el-button
              type="danger"
              size="small"
              @click="handleDeleteConfig(row)"
              icon="el-icon-delete"
              :disabled="row.status"
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
        <el-form-item label="表单名称" prop="name">
          <el-input v-model="formConfigForm.name" placeholder="请输入表单名称" />
        </el-form-item>
        
        <el-form-item label="表单描述" prop="description">
          <el-input
            v-model="formConfigForm.description"
            placeholder="请输入表单描述"
            type="textarea"
            rows="3"
          />
        </el-form-item>

        <el-form-item label="表单配置" prop="fields">
          <el-button
            type="primary"
            @click="openFormBuilder"
            icon="el-icon-edit-outline"
            style="margin-bottom: 16px"
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
          <el-button @click="dialogVisible = false">取消</el-button>
          <el-button type="primary" @click="handleSaveConfig">保存</el-button>
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
        v-model="formConfigForm.fields"
        :model="formConfigForm"
        @save="handleSaveFormBuilder"
        @close="formBuilderVisible = false"
      />
    </el-dialog>

    <!-- 表单预览对话框 -->
    <el-dialog
      v-model="previewVisible"
      :title="`预览: ${previewFormName}`"
      width="800px"
      destroy-on-close
    >
      <div v-if="previewFormConfig">
        <FormGenerator
          :form-config="previewFormConfig"
          :model="previewFormData"
          @submit="handlePreviewSubmit"
        />
      </div>

      <template #footer>
        <div class="dialog-footer">
          <el-button @click="previewVisible = false">关闭</el-button>
        </div>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useRouter } from 'vue-router'
import FormBuilder from '@/components/FormBuilder.vue'
import FormGenerator from '@/components/FormGenerator.vue'
import { formConfigService } from '@/services/formConfigService'
import type { FormConfig } from '@/types/formConfig'
import type { FormField } from '@/types/formConfig'

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
const formConfigForm = ref<FormConfig>({
  formId: '',
  name: '',
  description: '',
  version: 1,
  status: true,
  fields: [],
  createdAt: '',
  updatedAt: ''
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
const formConfigs = ref<FormConfig[]>([])
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
    { min: 2, max: 50, message: '表单名称长度应在2-50个字符之间', trigger: 'blur' }
  ],
  description: [
    { max: 200, message: '表单描述长度不能超过200个字符', trigger: 'blur' }
  ]
})

// 加载表单配置列表
const loadFormConfigs = async () => {
  loading.value = true
  try {
    const response = await formConfigService.getFormConfigs()
    formConfigs.value = response.data || []
    total.value = formConfigs.value.length
  } catch (error) {
    ElMessage.error('加载表单配置失败')
    console.error('加载表单配置失败:', error)
  } finally {
    loading.value = false
  }
}

// 处理添加表单配置
const handleAddConfig = () => {
  // 重置表单
  formConfigForm.value = {
    formId: '',
    name: '',
    description: '',
    version: 1,
    status: true,
    fields: [],
    createdAt: '',
    updatedAt: ''
  }
  dialogTitle.value = '添加表单配置'
  dialogVisible.value = true
}

// 处理编辑表单配置
const handleEditConfig = (config: FormConfig) => {
  // 深拷贝，避免直接修改原数据
  formConfigForm.value = JSON.parse(JSON.stringify(config))
  dialogTitle.value = '编辑表单配置'
  dialogVisible.value = true
}

// 处理查看表单配置
const handleViewConfig = (config: FormConfig) => {
  router.push({
    name: 'ConfigurableForm',
    query: { formId: config.formId }
  })
}

// 处理删除表单配置
const handleDeleteConfig = async (config: FormConfig) => {
  try {
    await ElMessageBox.confirm(
      `确定要删除表单配置「${config.name}」吗？此操作不可撤销。`,
      '删除确认',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }
    )

    await formConfigService.deleteFormConfig(config.formId)
    ElMessage.success('删除成功')
    loadFormConfigs()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
      console.error('删除表单配置失败:', error)
    }
  }
}

// 处理状态变更
const handleStatusChange = async (config: FormConfig) => {
  try {
    await formConfigService.updateFormConfig(config.formId, { status: config.status })
    ElMessage.success(config.status ? '表单已启用' : '表单已禁用')
  } catch (error) {
    // 回滚状态
    config.status = !config.status
    ElMessage.error('更新状态失败')
    console.error('更新表单状态失败:', error)
  }
}

// 打开表单构建器
const openFormBuilder = () => {
  formBuilderVisible.value = true
}

// 保存表单构建器配置
const handleSaveFormBuilder = () => {
  formBuilderVisible.value = false
  ElMessage.success('表单配置已更新')
}

// 保存表单配置
const handleSaveConfig = async () => {
  try {
    if (formConfigForm.value.formId) {
      // 更新现有配置
      await formConfigService.updateFormConfig(
        formConfigForm.value.formId,
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
const handlePreviewForm = (config: FormConfig) => {
  previewFormName.value = config.name
  previewFormConfig.value = config
  previewFormData.value = {}
  // 初始化表单数据
  config.fields.forEach(field => {
    if (field.defaultValue !== undefined && field.defaultValue !== null) {
      previewFormData.value[field.fieldKey] = field.defaultValue
    } else {
      // 根据字段类型设置默认值
      switch (field.type) {
        case 'checkbox':
          previewFormData.value[field.fieldKey] = []
          break
        case 'number':
          previewFormData.value[field.fieldKey] = 0
          break
        case 'date':
        case 'datetime':
        case 'time':
          previewFormData.value[field.fieldKey] = null
          break
        default:
          previewFormData.value[field.fieldKey] = ''
      }
    }
  })
  previewVisible.value = true
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
  return formConfigForm.value.formId ? '编辑表单配置' : '添加表单配置'
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
</style>