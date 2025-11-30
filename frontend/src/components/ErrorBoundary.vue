<template>
  <div
    v-if="hasError"
    class="error-boundary"
  >
    <div class="error-container">
      <el-icon class="error-icon">
        <WarningFilled />
      </el-icon>
      <h3>组件渲染出错</h3>
      <p>{{ errorMessage }}</p>
      <el-button
        type="primary"
        @click="resetError"
      >
        重新加载
      </el-button>
    </div>
  </div>
  <slot v-else />
</template>

<script lang="ts" setup>
import { ref, onErrorCaptured, VNode, VNodeProps, PropType, defineComponent } from 'vue'
import { WarningFilled } from '@element-plus/icons-vue'
import { recordError } from '@/utils/errorHandler'
import { performanceService } from '@/services/performanceService'

const props = defineProps({
  fallbackComponent: {
    type: [Object, Function] as PropType<any>,
    default: null
  },
  componentName: {
    type: String,
    default: 'UnknownComponent'
  }
})

const emit = defineEmits(['error', 'reset'])

const hasError = ref(false)
const errorMessage = ref('未知错误')
const errorInfo = ref<any>(null)

// 重置错误状态
const resetError = () => {
  hasError.value = false
  errorMessage.value = '未知错误'
  errorInfo.value = null
  emit('reset')
}

// 捕获子组件错误
onErrorCaptured((error, instance, info) => {
  hasError.value = true
  
  // 格式化错误信息
  const errorDetails = {
    component: props.componentName,
    message: error instanceof Error ? error.message : String(error),
    stack: error instanceof Error ? error.stack : undefined,
    info
  }
  
  errorMessage.value = errorDetails.message
  errorInfo.value = errorDetails
  
  // 记录错误到性能服务
  performanceService.recordError(
    'component_render_error',
    `组件渲染错误: ${props.componentName}`,
    errorDetails
  )
  
  // 使用统一的错误处理工具
  recordError({
    code: 'COMPONENT_ERROR',
    message: `组件 ${props.componentName} 渲染错误`,
    details: errorDetails
  }, {
    showMessage: true,
    critical: false
  })
  
  // 发出错误事件
  emit('error', errorDetails)
  
  // 阻止错误继续向上传播
  return false
})
</script>

<style scoped>
.error-boundary {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 200px;
  width: 100%;
}

.error-container {
  text-align: center;
  padding: 40px;
  background-color: #fef0f0;
  border: 1px solid #fbc4c4;
  border-radius: 8px;
  max-width: 500px;
  width: 100%;
}

.error-icon {
  font-size: 48px;
  color: #f56c6c;
  margin-bottom: 16px;
}

h3 {
  color: #f56c6c;
  margin-bottom: 12px;
}

p {
  color: #606266;
  margin-bottom: 20px;
  word-break: break-word;
}
</style>