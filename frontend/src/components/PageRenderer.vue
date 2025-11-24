<template>
  <div 
    class="page-renderer"
    :class="{ 'loading': loading, 'error': hasError }"
  >
    <!-- 加载状态 -->
    <div v-if="loading" class="loading-state">
      <el-skeleton :rows="6" animated />
    </div>

    <!-- 错误状态 -->
    <div v-else-if="hasError" class="error-state">
      <el-empty description="页面渲染失败" />
      <el-button type="primary" @click="retryRender">重新加载</el-button>
      <div v-if="errorMessage" class="error-message">{{ errorMessage }}</div>
    </div>

    <!-- 页面内容 -->
    <div v-else-if="pageConfig" class="page-content" v-loading="pageLoading">
      <!-- 页面头部 -->
      <div v-if="pageConfig.header" class="page-header">
        <ComponentRenderer
          :component-config="pageConfig.header"
          :parent-scope="pageScope"
          :editable="false"
        />
      </div>

      <!-- 页面主体 -->
      <div class="page-main">
        <!-- 侧边栏 -->
        <div v-if="pageConfig.sidebar" class="page-sidebar">
          <ComponentRenderer
            :component-config="pageConfig.sidebar"
            :parent-scope="pageScope"
            :editable="false"
          />
        </div>

        <!-- 主内容区 -->
        <div class="page-center">
          <ComponentRenderer
            :component-config="pageConfig.main || pageConfig"
            :parent-scope="pageScope"
            :editable="false"
          />
        </div>

        <!-- 右侧边栏 -->
        <div v-if="pageConfig.rightSidebar" class="page-right-sidebar">
          <ComponentRenderer
            :component-config="pageConfig.rightSidebar"
            :parent-scope="pageScope"
            :editable="false"
          />
        </div>
      </div>

      <!-- 页面底部 -->
      <div v-if="pageConfig.footer" class="page-footer">
        <ComponentRenderer
          :component-config="pageConfig.footer"
          :parent-scope="pageScope"
          :editable="false"
        />
      </div>
    </div>

    <!-- 空状态 -->
    <div v-else class="empty-state">
      <el-empty description="暂无页面配置" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, watch, nextTick, onUnmounted } from 'vue'
import { ElMessage } from 'element-plus'
import ComponentRenderer from './ComponentRenderer.vue'
import type { PageConfig, ComponentConfig } from '@/types/page'
import { loadComponent } from '@/services/componentRegistry'
import { processPageData } from '@/services/pageBuilderService'

// Props
interface Props {
  pageConfig?: PageConfig | null
  pageId?: string
  dataSources?: Record<string, any>
  context?: Record<string, any>
  deviceType?: 'desktop' | 'tablet' | 'mobile'
}

const props = withDefaults(defineProps<Props>(), {
  pageConfig: null,
  pageId: '',
  dataSources: () => ({}),
  context: () => ({}),
  deviceType: 'desktop'
})

// Emits
interface Emits {
  pageLoaded: [pageConfig: PageConfig]
  pageError: [error: Error]
  componentEvent: [componentId: string, eventName: string, eventData: any]
}

const emit = defineEmits<Emits>()

// Local state
const loading = ref(false)
const pageLoading = ref(false)
const hasError = ref(false)
const errorMessage = ref('')
const resolvedPageConfig = ref<PageConfig | null>(null)
const componentInstances = ref<Map<string, any>>(new Map())
const renderCount = ref(0)
const performanceStats = ref({
  renderTime: 0,
  componentCount: 0,
  dataLoadTime: 0
})

// 页面作用域
const pageScope = reactive({
  state: {},
  methods: {
    $emit: (eventName: string, eventData: any) => {
      handlePageEvent(eventName, eventData)
    },
    $setState: (updates: Record<string, any>) => {
      Object.assign(pageScope.state, updates)
    },
    $getData: async (dataSourceKey: string, params?: any) => {
      return await fetchDataSource(dataSourceKey, params)
    }
  },
  dataSources: { ...props.dataSources },
  context: { ...props.context }
})

// Computed
const pageConfig = computed(() => {
  return props.pageConfig || resolvedPageConfig.value
})

const isResponsive = computed(() => {
  return pageConfig.value?.responsive === true
})

const layoutClass = computed(() => {
  const classes: string[] = []
  if (isResponsive.value) {
    classes.push(`device-${props.deviceType}`)
  }
  if (pageConfig.value?.layout) {
    classes.push(`layout-${pageConfig.value.layout}`)
  }
  return classes
})

// Methods
const initializePage = async () => {
  loading.value = true
  hasError.value = false
  errorMessage.value = ''
  
  const startTime = performance.now()
  
  try {
    // 如果有pageId但没有pageConfig，则从服务加载配置
    if (props.pageId && !props.pageConfig) {
      resolvedPageConfig.value = await loadPageConfig(props.pageId)
    } else {
      resolvedPageConfig.value = props.pageConfig || null
    }

    if (pageConfig.value) {
      // 预加载所有需要的组件
      await preloadComponents(pageConfig.value)
      
      // 初始化页面数据
      await initializePageData()
      
      // 应用页面配置的响应式设置
      applyResponsiveSettings()
      
      // 触发页面生命周期钩子
      await callPageHook('onLoad')
      
      // 记录性能数据
      performanceStats.value.renderTime = performance.now() - startTime
      performanceStats.value.componentCount = componentInstances.value.size
      
      // 通知页面加载完成
      emit('pageLoaded', pageConfig.value)
    }
  } catch (error) {
    handleError(error as Error)
  } finally {
    loading.value = false
    renderCount.value++
  }
}

const loadPageConfig = async (pageId: string): Promise<PageConfig> => {
  // 这里应该从API或服务中加载页面配置
  // 暂时返回一个默认配置
  const { getPageMetadata } = await import('@/services/metadataService')
  return await getPageMetadata(pageId)
}

const preloadComponents = async (config: ComponentConfig | PageConfig): Promise<void> => {
  const componentQueue: string[] = []
  
  // 收集所有组件类型
  const collectComponents = (comp: ComponentConfig | PageConfig) => {
    if (comp.type) {
      componentQueue.push(comp.type)
    }
    
    if (comp.children) {
      comp.children.forEach(collectComponents)
    }
    
    // 处理布局配置的组件
    if ('header' in comp && comp.header) collectComponents(comp.header)
    if ('sidebar' in comp && comp.sidebar) collectComponents(comp.sidebar)
    if ('rightSidebar' in comp && comp.rightSidebar) collectComponents(comp.rightSidebar)
    if ('footer' in comp && comp.footer) collectComponents(comp.footer)
  }
  
  collectComponents(config)
  
  // 去重并加载组件
  const uniqueTypes = [...new Set(componentQueue)]
  await Promise.all(uniqueTypes.map(type => loadComponent(type)))
}

const initializePageData = async () => {
  if (!pageConfig.value?.dataSources) return
  
  const dataLoadStartTime = performance.now()
  
  try {
    // 加载页面配置的数据源
    const dataPromises = Object.entries(pageConfig.value.dataSources).map(async ([key, dataSource]) => {
      try {
        const data = await fetchDataSource(key, dataSource.params)
        pageScope.dataSources[key] = data
        return { key, data, success: true }
      } catch (error) {
        console.error(`Failed to load data source ${key}:`, error)
        ElMessage.error(`数据源 ${key} 加载失败`)
        return { key, error, success: false }
      }
    })
    
    await Promise.all(dataPromises)
    
    // 处理页面数据转换
    if (pageConfig.value.dataProcessors) {
      await processPageData(pageConfig.value, pageScope.dataSources)
    }
    
    performanceStats.value.dataLoadTime = performance.now() - dataLoadStartTime
  } catch (error) {
    console.error('Failed to initialize page data:', error)
  }
}

const fetchDataSource = async (key: string, params?: any): Promise<any> => {
  // 检查是否有预定义的数据源处理函数
  if (pageConfig.value?.dataSourceHandlers && pageConfig.value.dataSourceHandlers[key]) {
    const handler = pageConfig.value.dataSourceHandlers[key]
    return await handler(params)
  }
  
  // 否则使用默认的API调用
  const { apiService } = await import('@/utils/apiService')
  return await apiService.get(key, params)
}

const applyResponsiveSettings = () => {
  if (!isResponsive.value || !pageConfig.value?.responsiveSettings) return
  
  const settings = pageConfig.value.responsiveSettings[props.deviceType]
  if (settings) {
    // 应用响应式设置到页面作用域
    Object.assign(pageScope.context, settings)
  }
}

const callPageHook = async (hookName: string) => {
  if (!pageConfig.value?.lifecycleHooks || !pageConfig.value.lifecycleHooks[hookName]) {
    return
  }
  
  const hook = pageConfig.value.lifecycleHooks[hookName]
  try {
    await hook(pageScope)
  } catch (error) {
    console.error(`Failed to execute ${hookName} hook:`, error)
  }
}

const handleError = (error: Error) => {
  hasError.value = true
  errorMessage.value = error.message
  emit('pageError', error)
  console.error('Page rendering error:', error)
}

const handlePageEvent = (eventName: string, eventData: any) => {
  // 处理页面级事件
  console.log(`Page event: ${eventName}`, eventData)
  
  // 查找对应的事件处理器
  if (pageConfig.value?.events) {
    const eventHandler = pageConfig.value.events.find(e => e.name === eventName)
    if (eventHandler && eventHandler.actions) {
      eventHandler.actions.forEach(action => {
        executeAction(action, eventData)
      })
    }
  }
}

const executeAction = async (action: any, eventData: any) => {
  switch (action.type) {
    case 'apiCall':
      await executeApiCall(action, eventData)
      break
    case 'stateUpdate':
      executeStateUpdate(action)
      break
    case 'navigate':
      executeNavigate(action)
      break
    case 'showMessage':
      executeShowMessage(action)
      break
    case 'customScript':
      executeCustomScript(action, eventData)
      break
    default:
      console.warn('Unknown action type:', action.type)
  }
}

const executeApiCall = async (action: any, eventData: any) => {
  try {
    const { apiService } = await import('@/utils/apiService')
    const result = await apiService[action.method || 'get'](
      action.url,
      action.params,
      action.headers
    )
    
    if (action.onSuccess) {
      executeAction(action.onSuccess, { ...eventData, result })
    }
  } catch (error) {
    console.error('API call failed:', error)
    if (action.onError) {
      executeAction(action.onError, { ...eventData, error })
    }
  }
}

const executeStateUpdate = (action: any) => {
  Object.assign(pageScope.state, action.updates)
}

const executeNavigate = (action: any) => {
  const { useRouter } = await import('vue-router')
  const router = useRouter()
  
  if (action.replace) {
    router.replace(action.path)
  } else {
    router.push(action.path)
  }
}

const executeShowMessage = (action: any) => {
  ElMessage[action.level || 'info'](action.message)
}

const executeCustomScript = (action: any, eventData: any) => {
  try {
    // 使用更安全的方式执行脚本，避免eval的安全风险
    // 可以考虑使用Function构造函数，但仍然需要注意安全问题
    const scriptFunction = new Function('scope', 'eventData', action.script)
    scriptFunction(pageScope, eventData)
  } catch (error) {
    console.error('Failed to execute custom script:', error)
  }
}

const retryRender = () => {
  initializePage()
}

const registerComponentInstance = (componentId: string, instance: any) => {
  componentInstances.value.set(componentId, instance)
}

const unregisterComponentInstance = (componentId: string) => {
  componentInstances.value.delete(componentId)
}

// 响应式适配
const handleResize = () => {
  if (!isResponsive.value) return
  
  const width = window.innerWidth
  let newDeviceType: 'desktop' | 'tablet' | 'mobile' = 'desktop'
  
  if (width < 768) {
    newDeviceType = 'mobile'
  } else if (width < 1024) {
    newDeviceType = 'tablet'
  }
  
  // 如果设备类型改变，重新应用响应式设置
  if (newDeviceType !== props.deviceType) {
    // 这里可以通过emit事件通知父组件更新deviceType
    applyResponsiveSettings()
  }
}

// 监听属性变化
watch(() => [props.pageConfig, props.pageId], () => {
  initializePage()
}, { deep: true })

watch(() => props.dataSources, (newDataSources) => {
  Object.assign(pageScope.dataSources, newDataSources)
}, { deep: true })

watch(() => props.context, (newContext) => {
  Object.assign(pageScope.context, newContext)
}, { deep: true })

watch(() => props.deviceType, () => {
  applyResponsiveSettings()
})

// Lifecycle
onMounted(() => {
  initializePage()
  
  // 添加窗口大小变化监听
  if (isResponsive.value) {
    window.addEventListener('resize', handleResize)
  }
})

onUnmounted(() => {
  // 触发页面卸载钩子
  callPageHook('onUnmount')
  
  // 移除事件监听器
  window.removeEventListener('resize', handleResize)
  
  // 清理组件实例引用
  componentInstances.value.clear()
})

// 暴露方法给父组件
defineExpose({
  refresh: initializePage,
  getPageScope: () => pageScope,
  getPerformanceStats: () => performanceStats.value,
  callMethod: (methodName: string, ...args: any[]) => {
    if (pageScope.methods[methodName]) {
      return pageScope.methods[methodName](...args)
    }
  }
})
</script>

<style scoped>
.page-renderer {
  width: 100%;
  min-height: 100vh;
  background: #f5f7fa;
}

.page-content {
  width: 100%;
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

.page-header,
.page-footer {
  width: 100%;
}

.page-main {
  flex: 1;
  display: flex;
  width: 100%;
}

.page-sidebar,
.page-right-sidebar {
  width: 240px;
  flex-shrink: 0;
}

.page-center {
  flex: 1;
  min-width: 0;
}

/* 响应式布局 */
:deep(.device-mobile) .page-sidebar,
:deep(.device-mobile) .page-right-sidebar {
  display: none;
}

:deep(.device-tablet) .page-sidebar {
  width: 200px;
}

:deep(.device-tablet) .page-right-sidebar {
  display: none;
}

/* 加载和错误状态 */
.loading-state,
.error-state,
.empty-state {
  width: 100%;
  min-height: 400px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 40px;
}

.error-message {
  margin-top: 16px;
  color: #f56c6c;
  font-size: 14px;
  text-align: center;
}

/* 布局变体 */
.layout-one-column .page-main {
  flex-direction: column;
}

.layout-one-column .page-sidebar,
.layout-one-column .page-right-sidebar {
  display: none;
}

.layout-two-column .page-right-sidebar {
  display: none;
}

/* 性能优化 */
.page-renderer {
  contain: layout style size;
}

/* 动画效果 */
.page-content {
  animation: fadeIn 0.3s ease-in;
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
</style>