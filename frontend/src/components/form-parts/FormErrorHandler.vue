<template>
  <div class="form-error-handler">
    <!-- 错误提示 -->
    <transition name="error-fade">
      <div
        v-if="hasErrors"
        class="error-notification"
      >
        <el-alert
          :title="errorMessage"
          type="error"
          show-icon
          :closable="true"
          @close="dismissError"
        >
          <div class="error-actions">
            <el-button
              type="primary"
              size="small"
              @click="tryRecover"
            >
              尝试恢复
            </el-button>
            <el-button
              size="small"
              @click="resetForm"
            >
              重置表单
            </el-button>
            <el-button
              size="small"
              @click="showDetails = !showDetails"
            >
              {{ showDetails ? '隐藏' : '显示' }}详情
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
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onBeforeUnmount } from 'vue'
import { ElMessage } from 'element-plus'

// Props
const props = defineProps<{
  formData?: Record<string, any>
  showErrorNotification?: boolean
  enableAutoRecovery?: boolean
}>()

// Emits
const emit = defineEmits<{
  (e: 'recover', formData: Record<string, any>): void
  (e: 'reset'): void
  (e: 'error', error: Error, formData: Record<string, any>): void
}>()

// 状态
const error = ref<Error | null>(null)
const showDetails = ref(false)
const hasErrors = ref(false)
const errorMessage = ref('')
const errorDetails = ref('')
const lastValidFormData = ref<Record<string, any> | null>(null)

// 计算属性
const hasErrorsComputed = computed(() => hasErrors.value && props.showErrorNotification !== false)

// 错误监听
const errorHandler = (event: ErrorEvent) => {
  handleGlobalError(event.error)
}

// 未处理的Promise拒绝监听
const rejectionHandler = (event: PromiseRejectionEvent) => {
  handleGlobalError(new Error(event.reason))
}

// 全局错误处理
const handleGlobalError = (err: any) => {
  if (!err || !(err instanceof Error)) return
  
  error.value = err
  hasErrors.value = true
  errorMessage.value = err.message || '发生了未知错误'
  errorDetails.value = err.stack || JSON.stringify(err, null, 2)
  
  emit('error', err, props.formData || {})
  
  // 尝试自动恢复
  if (props.enableAutoRecovery !== false) {
    setTimeout(() => {
      tryAutoRecover()
    }, 1000)
  }
}

// 尝试自动恢复
const tryAutoRecover = () => {
  if (!lastValidFormData.value) return
  
  try {
    emit('recover', lastValidFormData.value)
    hasErrors.value = false
    error.value = null
    ElMessage.success('表单已自动恢复')
  } catch (err) {
    console.error('自动恢复失败:', err)
  }
}

// 手动尝试恢复
const tryRecover = () => {
  if (!lastValidFormData.value) {
    ElMessage.warning('没有可用的备份数据')
    return
  }
  
  tryAutoRecover()
}

// 重置表单
const resetForm = () => {
  hasErrors.value = false
  error.value = null
  emit('reset')
}

// 关闭错误提示
const dismissError = () => {
  hasErrors.value = false
  error.value = null
}

// 监听表单数据变化
watch(
  () => props.formData,
  (newData) => {
    if (newData && !hasErrors.value) {
      // 深拷贝保存最后有效的表单数据
      try {
        lastValidFormData.value = JSON.parse(JSON.stringify(newData))
      } catch (e) {
        console.error('保存表单数据失败:', e)
      }
    }
  },
  { deep: true }
)

// 添加全局错误监听
window.addEventListener('error', errorHandler)
window.addEventListener('unhandledrejection', rejectionHandler)

// 组件卸载前清理
onBeforeUnmount(() => {
  window.removeEventListener('error', errorHandler)
  window.removeEventListener('unhandledrejection', rejectionHandler)
})

// 暴露方法
defineExpose({
  handleError: handleGlobalError,
  hasError: () => hasErrors.value,
  getError: () => error.value,
  recover: tryRecover,
  reset: resetForm
})
</script>

<style scoped>
.form-error-handler {
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