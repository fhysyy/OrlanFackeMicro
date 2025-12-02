<template>
  <div class="vtj-designer-container">
    <div
      v-if="loading"
      class="loading-state"
    >
      <div class="loading-spinner" />
      <p class="loading-text">
        VTJ设计器加载中...
      </p>
    </div>
    <div
      v-else-if="error"
      class="error-state"
    >
      <div class="error-icon">
        !
      </div>
      <p class="error-text">
        VTJ设计器加载失败
      </p>
      <p class="error-details">
        {{ errorMessage }}
      </p>
      <button
        class="retry-button"
        @click="retryLoading"
      >
        重试
      </button>
    </div>
    <div
      v-else
      class="designer-container"
    >
      <Designer
        :config="designerConfig"
        @error="handleDesignerError"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, defineAsyncComponent, defineEmits } from 'vue'
import { ElMessage } from 'element-plus'

// 定义属性
const props = defineProps<{
  initialConfig?: Record<string, any>
}>()

// 定义事件
const emit = defineEmits<{
  (e: 'error', error: Error): void
  (e: 'loaded'): void
}>()

// 定义加载状态
const loading = ref(true)
const error = ref<Error | null>(null)
const errorMessage = ref('')

// 设计器配置
const designerConfig = ref({
  // 基础配置
  theme: 'light',
  // 可以从props传入的初始配置
  ...props.initialConfig
})

// 使用defineAsyncComponent直接导入Designer组件，确保正确处理ES6类
const Designer = defineAsyncComponent({
  // 使用动态导入 - 添加错误处理
  loader: async () => {
    try {
      // 动态导入VTJ设计器组件
      const module = await import('@vtj/designer')
      // 确保样式也被加载
      await import('@vtj/designer/dist/style.css')
      return module.Designer
    } catch (importError) {
      console.error('VTJ设计器模块导入失败:', importError)
      throw new Error('无法加载VTJ设计器模块，请检查网络连接或联系管理员')
    }
  },
  // 加载时显示的组件
  loadingComponent: {
    template: `
      <div class="loading-state">
        <div class="loading-spinner"></div>
        <p class="loading-text">VTJ设计器加载中...</p>
      </div>
    `
  },
  // 加载失败时显示的组件
  errorComponent: {
    props: ['error'],
    template: `
      <div class="error-state">
        <div class="error-icon">!</div>
        <p class="error-text">VTJ设计器加载失败</p>
        <p class="error-details">{{ error?.message }}</p>
        <button class="retry-button" @click="$emit('retry')">重试</button>
      </div>
    `,
    emits: ['retry']
  },
  // 超时时间（毫秒）
  timeout: 30000,
  // 延迟显示加载组件的时间（毫秒）
  delay: 0
})

// 处理设计器内部错误
const handleDesignerError = (err: Error) => {
  console.error('VTJ设计器运行时错误:', err)
  ElMessage.error('VTJ设计器运行时发生错误')
  emit('error', err)
}

// 加载设计器的函数
const loadDesigner = async () => {
  loading.value = true
  error.value = null
  errorMessage.value = ''
  
  try {
    console.log('VTJ设计器开始加载...')
    
    // 设计器组件已经通过defineAsyncComponent加载，这里只需要设置状态
    // 等待一个短暂的时间确保组件加载完成
    await new Promise(resolve => setTimeout(resolve, 100))
    
    loading.value = false
    console.log('VTJ设计器加载完成')
    emit('loaded')
  } catch (err: any) {
    console.error('VTJ设计器加载错误:', err)
    error.value = err
    errorMessage.value = err.message || '未知错误'
    loading.value = false
    emit('error', err)
  }
}

// 重试加载函数
const retryLoading = () => {
  loadDesigner()
}

onMounted(() => {
  loadDesigner()
})
</script>

<style scoped>
.vtj-designer-container {
  width: 100%;
  height: 100vh;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  background-color: #f5f7fa;
}

.designer-container {
  width: 100%;
  height: 100%;
}

.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  width: 100%;
  background-color: #ffffff;
}

.loading-spinner {
  width: 48px;
  height: 48px;
  border: 4px solid #f3f3f3;
  border-top: 4px solid #409EFF;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.loading-text {
  margin-top: 16px;
  font-size: 16px;
  color: #606266;
}

.error-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  width: 100%;
  background-color: #ffffff;
  padding: 20px;
}

.error-icon {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  background-color: #f56c6c;
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 32px;
  font-weight: bold;
  margin-bottom: 16px;
}

.error-text {
  font-size: 18px;
  color: #303133;
  margin-bottom: 8px;
}

.error-details {
  font-size: 14px;
  color: #909399;
  margin-bottom: 20px;
  text-align: center;
  max-width: 80%;
  word-break: break-word;
}

.retry-button {
  padding: 8px 16px;
  background-color: #409EFF;
  color: white;
  border: none;
  border-radius: 4px;
  font-size: 14px;
  cursor: pointer;
  transition: background-color 0.3s;
}

.retry-button:hover {
  background-color: #66b1ff;
}
</style>