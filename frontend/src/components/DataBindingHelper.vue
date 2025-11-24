<template>
  <div v-if="debug" class="data-binding-helper debug-info">
    <div class="debug-header">
      <h4>数据绑定调试信息</h4>
      <button @click="refreshBindings" class="refresh-btn">刷新绑定</button>
    </div>
    <div class="binding-stats">
      <p>绑定数量: {{ bindings.size }}</p>
      <p>数据源数量: {{ dataSources.size }}</p>
    </div>
    <div v-if="hasErrors" class="binding-errors">
      <h5>错误信息:</h5>
      <ul>
        <li v-for="(error, index) in errors" :key="index">{{ error }}</li>
      </ul>
    </div>
  </div>
</template>

<script lang="ts" setup>
import { ref, reactive, onMounted, onUnmounted, inject, watch } from 'vue'
import type { Ref, ComputedRef } from 'vue'
import type { 
  DataBindingService, 
  DataBindingConfig, 
  DataSourceConfig,
  DataBinding,
  BindingContext,
  DataContext
} from '@/types/dataBinding'

// Props
interface Props {
  // 上下文ID，用于唯一标识绑定上下文
  contextId?: string
  // 初始数据
  initialData?: Record<string, any>
  // 自动初始化
  autoInit?: boolean
  // 调试模式
  debug?: boolean
  // 数据源配置
  dataSources?: Record<string, DataSourceConfig>
  // 绑定配置
  bindings?: Array<{
    target: string
    targetKey: string
    expression: string
    options?: DataBindingConfig
  }>
}

const props = withDefaults(defineProps<Props>(), {
  contextId: () => `binding-context-${Date.now()}`,
  initialData: () => ({}),
  autoInit: true,
  debug: false
})

// Emits
interface Emits {
  (e: 'initialized', context: BindingContext): void
  (e: 'error', error: Error): void
  (e: 'data-changed', data: any): void
}

const emit = defineEmits<Emits>()

// 获取数据绑定服务
const dataBindingService = inject<DataBindingService>('dataBinding')
if (!dataBindingService) {
  throw new Error('DataBindingService not found. Please make sure the plugin is installed.')
}

// 响应式数据
const bindingContext = ref<BindingContext | null>(null)
const bindings = ref<Map<string, DataBinding>>(new Map())
const dataSources = ref<Map<string, any>>(new Map())
const errors = ref<string[]>([])
const initialized = ref(false)

// 计算属性
const hasErrors = computed(() => errors.value.length > 0)

// 初始化绑定上下文
const initialize = async () => {
  try {
    // 创建绑定上下文
    bindingContext.value = dataBindingService.createBindingContext(props.contextId, props.initialData)
    
    // 注册数据源
    if (props.dataSources) {
      await registerDataSources(props.dataSources)
    }
    
    // 创建绑定
    if (props.bindings) {
      createBindings(props.bindings)
    }
    
    initialized.value = true
    emit('initialized', bindingContext.value)
  } catch (error) {
    console.error('Failed to initialize data binding:', error)
    errors.value.push(`初始化失败: ${error instanceof Error ? error.message : String(error)}`)
    emit('error', error as Error)
  }
}

// 注册数据源
const registerDataSources = async (sources: Record<string, DataSourceConfig>) => {
  for (const [name, config] of Object.entries(sources)) {
    try {
      await bindingContext.value!.registerDataSource(name, config)
      dataSources.value.set(name, config)
      
      // 监听数据源变化
      const dataSource = bindingContext.value!.getData(name)
      bindingContext.value!.watchExpression(name, (newValue) => {
        emit('data-changed', {
          source: name,
          value: newValue
        })
      })
    } catch (error) {
      console.error(`Failed to register data source '${name}':`, error)
      errors.value.push(`数据源 '${name}' 注册失败: ${error instanceof Error ? error.message : String(error)}`)
    }
  }
}

// 创建数据绑定
const createBindings = (bindingConfigs: Array<{
  target: string
  targetKey: string
  expression: string
  options?: DataBindingConfig
}>) => {
  for (const config of bindingConfigs) {
    try {
      // 这里需要获取实际的target对象，暂时使用组件实例
      const target = getTargetByName(config.target)
      if (target) {
        const binding = bindingContext.value!.createBinding(
          target,
          config.targetKey,
          config.expression,
          config.options
        )
        const bindingId = `${config.target}-${config.targetKey}`
        bindings.value.set(bindingId, binding)
      } else {
        errors.value.push(`未找到目标对象: ${config.target}`)
      }
    } catch (error) {
      console.error(`Failed to create binding for '${config.target}.${config.targetKey}':`, error)
      errors.value.push(`绑定 '${config.target}.${config.targetKey}' 创建失败: ${error instanceof Error ? error.message : String(error)}`)
    }
  }
}

// 根据名称获取目标对象
const getTargetByName = (name: string): any => {
  // 这里需要实现根据名称查找组件实例的逻辑
  // 可以通过ref或者其他方式获取
  // 暂时返回当前组件实例
  return getCurrentInstance()
}

// 创建新的数据绑定
const createBinding = (target: any, targetKey: string, expression: string, options?: DataBindingConfig): DataBinding | null => {
  if (!bindingContext.value) return null
  
  try {
    const binding = bindingContext.value.createBinding(target, targetKey, expression, options)
    const bindingId = `${targetKey}-${Date.now()}`
    bindings.value.set(bindingId, binding)
    return binding
  } catch (error) {
    console.error('Failed to create binding:', error)
    errors.value.push(`创建绑定失败: ${error instanceof Error ? error.message : String(error)}`)
    return null
  }
}

// 移除数据绑定
const removeBinding = (binding: DataBinding) => {
  if (!bindingContext.value) return
  
  try {
    bindingContext.value.removeBinding(binding)
    // 从映射中删除绑定
    for (const [id, b] of bindings.value.entries()) {
      if (b === binding) {
        bindings.value.delete(id)
        break
      }
    }
  } catch (error) {
    console.error('Failed to remove binding:', error)
    errors.value.push(`移除绑定失败: ${error instanceof Error ? error.message : String(error)}`)
  }
}

// 更新数据
const updateData = (data: Record<string, any>) => {
  if (!bindingContext.value) return
  
  try {
    bindingContext.value.setData(data)
  } catch (error) {
    console.error('Failed to update data:', error)
    errors.value.push(`更新数据失败: ${error instanceof Error ? error.message : String(error)}`)
  }
}

// 获取数据
const getData = <T = any>(path?: string): T => {
  if (!bindingContext.value) return undefined as T
  
  try {
    return bindingContext.value.getData<T>(path)
  } catch (error) {
    console.error('Failed to get data:', error)
    errors.value.push(`获取数据失败: ${error instanceof Error ? error.message : String(error)}`)
    return undefined as T
  }
}

// 计算表达式
const evaluateExpression = <T = any>(expression: string): T => {
  if (!bindingContext.value) return undefined as T
  
  try {
    return bindingContext.value.evaluateExpression<T>(expression)
  } catch (error) {
    console.error('Failed to evaluate expression:', error)
    errors.value.push(`计算表达式失败: ${error instanceof Error ? error.message : String(error)}`)
    return undefined as T
  }
}

// 创建计算属性
const createComputed = <T = any>(expression: string): ComputedRef<T> => {
  if (!bindingContext.value) {
    throw new Error('Binding context not initialized')
  }
  
  return bindingContext.value.createComputed<T>(expression)
}

// 监听表达式变化
const watchExpression = (expression: string, callback: (newValue: any, oldValue: any) => void): (() => void) | null => {
  if (!bindingContext.value) return null
  
  try {
    return bindingContext.value.watchExpression(expression, callback)
  } catch (error) {
    console.error('Failed to watch expression:', error)
    errors.value.push(`监听表达式失败: ${error instanceof Error ? error.message : String(error)}`)
    return null
  }
}

// 刷新所有绑定
const refreshBindings = () => {
  for (const binding of bindings.value.values()) {
    binding.refresh()
  }
}

// 获取绑定上下文
const getContext = (): BindingContext | null => {
  return bindingContext.value
}

// 清理错误
const clearErrors = () => {
  errors.value = []
}

// 生命周期钩子
onMounted(() => {
  if (props.autoInit) {
    initialize()
  }
})

onUnmounted(() => {
  if (bindingContext.value) {
    // 清理所有绑定
    for (const binding of bindings.value.values()) {
      binding.destroy()
    }
    bindings.value.clear()
    
    // 销毁绑定上下文
    dataBindingService.destroyBindingContext(props.contextId)
    bindingContext.value = null
  }
})

// 暴露方法给父组件
defineExpose({
  initialize,
  createBinding,
  removeBinding,
  updateData,
  getData,
  evaluateExpression,
  createComputed,
  watchExpression,
  refreshBindings,
  getContext,
  clearErrors,
  registerDataSources
})
</script>

<style scoped>
.data-binding-helper {
  position: relative;
  border: 1px solid #e0e0e0;
  border-radius: 4px;
  padding: 16px;
  background-color: #f5f5f5;
  margin: 8px 0;
}

.debug-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}

.debug-header h4 {
  margin: 0;
  font-size: 14px;
  color: #333;
}

.refresh-btn {
  padding: 4px 8px;
  font-size: 12px;
  background-color: #409eff;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}

.refresh-btn:hover {
  background-color: #66b1ff;
}

.binding-stats {
  font-size: 12px;
  color: #666;
  margin-bottom: 12px;
}

.binding-stats p {
  margin: 4px 0;
}

.binding-errors {
  background-color: #fef0f0;
  border: 1px solid #fbc4c4;
  border-radius: 4px;
  padding: 8px;
}

.binding-errors h5 {
  margin: 0 0 8px 0;
  font-size: 12px;
  color: #f56c6c;
}

.binding-errors ul {
  margin: 0;
  padding-left: 16px;
}

.binding-errors li {
  font-size: 12px;
  color: #f56c6c;
  margin: 2px 0;
}
</style>