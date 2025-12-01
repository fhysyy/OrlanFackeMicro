<template>
  <div class="enhanced-error-handler">
    <!-- 错误提示 -->
    <transition name="error-fade">
      <div
        v-if="hasErrors"
        class="error-notification"
      >
        <el-alert
          :title="currentError?.message || t('error.unknown')"
          type="error"
          show-icon
          :closable="true"
          @close="dismissError"
        >
          <div class="error-actions">
            <el-button
              type="primary"
              size="small"
              @click="tryAutoRecover"
              :loading="recovering"
            >
              {{ t('action.retry') }}
            </el-button>
            <el-button
              size="small"
              @click="showBackupDialog"
            >
              选择备份恢复
            </el-button>
            <el-button
              size="small"
              @click="resetForm"
            >
              {{ t('action.reset') }}
            </el-button>
            <el-button
              size="small"
              @click="showDetails = !showDetails"
            >
              {{ showDetails ? t('action.cancel') : t('action.continue') }}
            </el-button>
          </div>
          
          <!-- 错误详情 -->
          <collapse-transition>
            <div
              v-if="showDetails"
              class="error-details"
            >
              <pre>{{ errorDetails }}</pre>
            </div>
          </collapse-transition>
        </el-alert>
      </div>
    </transition>

    <!-- 表单内容 -->
    <div class="form-content">
      <slot />
    </div>
    
    <!-- 备份选择对话框 -->
    <el-dialog
      v-model="backupDialogVisible"
      title="选择备份恢复"
      width="600px"
    >
      <div v-if="formDataBackups.length > 0">
        <el-timeline>
          <el-timeline-item
            v-for="(backup, index) in formDataBackups"
            :key="backup.timestamp"
            :timestamp="formatDate(backup.timestamp)"
            :type="index === formDataBackups.length - 1 ? 'primary' : ''"
          >
            <div class="backup-item">
              <span>{{ `备份 ${index + 1}` }}</span>
              <el-button
                type="primary"
                size="small"
                @click="recoverFromBackup(backup.timestamp)"
              >
                {{ t('action.continue') }}
              </el-button>
            </div>
          </el-timeline-item>
        </el-timeline>
      </div>
      <el-empty
        v-else
        description="没有可用的备份"
      />
    </el-dialog>
    
    <!-- 自动保存状态指示器 -->
    <div
      v-if="showAutoSaveIndicator"
      class="auto-save-indicator"
    >
      <el-tooltip
        :content="`上次保存: ${formatDate(lastAutoSave)}`"
        placement="top"
      >
        <div class="auto-save-icon">
          <el-icon>
            <component :is="autoSaveIcon" />
          </el-icon>
        </div>
      </el-tooltip>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted, nextTick } from 'vue'
import { ElMessage } from 'element-plus'
import { Check, Loading, Warning } from '@element-plus/icons-vue'
import { t } from './FormI18n'
import { enhancedErrorService } from '@/services/enhancedErrorService'

// Props
const props = defineProps<{
  formId?: string
  formData?: Record<string, any>
  formConfig?: any
  showErrorNotification?: boolean
  enableAutoSave?: boolean
  showAutoSaveIndicator?: boolean
  getFormData?: () => Record<string, any>
  getFormConfig?: () => any
}>()

// Emits
const emit = defineEmits<{
  (e: 'recover', formData: Record<string, any>): void
  (e: 'reset'): void
  (e: 'error', error: Error, formData: Record<string, any>): void
}>()

// 状态
const currentError = ref<Error | null>(null)
const showDetails = ref(false)
const hasErrors = ref(false)
const errorDetails = ref('')
const recovering = ref(false)
const backupDialogVisible = ref(false)
const formDataBackups = ref<Array<{timestamp: number, data: any}>>([])
const lastAutoSave = ref(0)
const autoSaveIcon = ref(Check)

// 计算属性
const hasErrorsComputed = computed(() => hasErrors.value && props.showErrorNotification !== false)

// 获取表单ID
const formId = computed(() => {
  return props.formId || `form-${Date.now()}`
})

// 错误处理
const handleError = (err: any) => {
  if (!err || !(err instanceof Error)) return
  
  currentError.value = err
  hasErrors.value = true
  errorDetails.value = err.stack || JSON.stringify(err, null, 2)
  
  // 记录错误
  if (props.formData && props.getFormConfig) {
    const errorId = enhancedErrorService.recordError(
      formId.value,
      err,
      props.formData,
      props.getFormConfig()
    )
    
    emit('error', err, props.formData)
  }
  
  // 尝试自动恢复
  if (props.enableAutoSave !== false) {
    setTimeout(() => {
      tryAutoRecover()
    }, 1000)
  }
}

// 尝试自动恢复
const tryAutoRecover = async () => {
  if (!currentError.value || !props.formConfig) return
  
  recovering.value = true
  autoSaveIcon.value = Loading
  
  try {
    // 获取最近的备份
    const backupData = enhancedErrorService.recoverFormData(formId.value)
    
    if (backupData) {
      emit('recover', backupData)
      hasErrors.value = false
      currentError.value = null
      ElMessage.success(t('success.recover'))
    } else {
      ElMessage.warning('没有可用的备份数据')
    }
  } catch (err) {
    console.error('自动恢复失败:', err)
    ElMessage.error('自动恢复失败')
  } finally {
    recovering.value = false
    autoSaveIcon.value = Check
  }
}

// 重置表单
const resetForm = () => {
  hasErrors.value = false
  currentError.value = null
  emit('reset')
}

// 关闭错误提示
const dismissError = () => {
  hasErrors.value = false
  currentError.value = null
}

// 显示备份选择对话框
const showBackupDialog = () => {
  formDataBackups.value = enhancedErrorService.getFormDataBackups(formId.value)
  backupDialogVisible.value = true
}

// 从备份恢复
const recoverFromBackup = (backupTimestamp: number) => {
  const backupData = enhancedErrorService.recoverFormData(formId.value, backupTimestamp)
  
  if (backupData) {
    emit('recover', backupData)
    hasErrors.value = false
    currentError.value = null
    backupDialogVisible.value = false
    ElMessage.success('已从备份恢复')
  } else {
    ElMessage.error('恢复失败')
  }
}

// 格式化日期
const formatDate = (timestamp: number) => {
  return new Date(timestamp).toLocaleString()
}

// 启用自动保存
const enableAutoSave = () => {
  if (!props.formId || !props.getFormData || !props.getFormConfig) return
  
  enhancedErrorService.enableAutoSave(
    formId.value,
    props.getFormData,
    props.getFormConfig
  )
  
  // 更新自动保存时间
  const updateAutoSaveTime = () => {
    lastAutoSave.value = Date.now()
    autoSaveIcon.value = Check
    
    // 2秒后恢复为普通图标
    setTimeout(() => {
      if (autoSaveIcon.value === Check) {
        autoSaveIcon.value = Check
      }
    }, 2000)
  }
  
  // 定期更新自动保存状态
  const timer = setInterval(() => {
    if (props.enableAutoSave !== false) {
      updateAutoSaveTime()
    } else {
      clearInterval(timer)
    }
  }, 5000)
  
  // 组件卸载时清理定时器
  onUnmounted(() => {
    clearInterval(timer)
    enhancedErrorService.disableAutoSave(formId.value)
  })
}

// 全局错误监听
const errorHandler = (event: ErrorEvent) => {
  handleError(event.error)
}

// 未处理的Promise拒绝监听
const rejectionHandler = (event: PromiseRejectionEvent) => {
  handleError(new Error(event.reason))
}

// 组件挂载时初始化
onMounted(async () => {
  // 添加全局错误监听
  window.addEventListener('error', errorHandler)
  window.addEventListener('unhandledrejection', rejectionHandler)
  
  // 启用自动保存
  if (props.enableAutoSave !== false) {
    await nextTick()
    enableAutoSave()
  }
})

// 监听enableAutoSave变化
watch(
  () => props.enableAutoSave,
  (newValue) => {
    if (newValue !== false) {
      enableAutoSave()
    } else {
      enhancedErrorService.disableAutoSave(formId.value)
    }
  }
)

// 暴露方法
defineExpose({
  handleError,
  hasError: () => hasErrors.value,
  getError: () => currentError.value,
  recover: tryAutoRecover,
  reset: resetForm,
  showBackupDialog,
  getFormDataBackups: () => enhancedErrorService.getFormDataBackups(formId.value),
  getErrorHistory: () => enhancedErrorService.getErrorHistory(formId.value)
})
</script>

<style scoped>
.enhanced-error-handler {
  width: 100%;
  height: 100%;
  position: relative;
}

.form-content {
  width: 100%;
  height: 100%;
}

.error-notification {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  z-index: 1000;
  margin-bottom: 10px;
}

.error-actions {
  margin-top: 10px;
  display: flex;
  gap: 8px;
}

.error-details {
  margin-top: 10px;
  padding: 10px;
  background-color: #f5f5f5;
  border-radius: 4px;
  max-height: 200px;
  overflow-y: auto;
}

.error-details pre {
  margin: 0;
  font-size: 12px;
  line-height: 1.4;
  white-space: pre-wrap;
  word-wrap: break-word;
}

.backup-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.auto-save-indicator {
  position: absolute;
  bottom: 16px;
  right: 16px;
  z-index: 100;
}

.auto-save-icon {
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  background-color: rgba(64, 158, 255, 0.1);
  border-radius: 50%;
  color: #409eff;
  font-size: 16px;
  cursor: pointer;
  transition: all 0.3s;
}

.auto-save-icon:hover {
  background-color: rgba(64, 158, 255, 0.2);
}

.error-fade-enter-active,
.error-fade-leave-active {
  transition: all 0.3s ease;
}

.error-fade-enter-from {
  opacity: 0;
  transform: translateY(-20px);
}

.error-fade-leave-to {
  opacity: 0;
  transform: translateY(-20px);
}
</style>