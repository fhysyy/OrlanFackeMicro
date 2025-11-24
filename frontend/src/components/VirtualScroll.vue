  <template>
  <div 
    class="virtual-scroll-container"
    :style="containerStyle"
    @scroll="handleScroll"
    ref="scrollContainer"
  >
    <!-- 占位元素，保持滚动条高度 -->
    <div 
      class="virtual-scroll-placeholder"
      :style="{ height: `${totalHeight}px` }"
    ></div>
    
    <!-- 可视区域内容 -->
    <div 
      class="virtual-scroll-content"
      :style="contentStyle"
    >
      <template v-if="visibleItems.length > 0">
        <component
          v-for="(item, index) in visibleItems"
          :key="getItemKey(item, index)"
          :is="itemComponent"
          :item="item"
          :index="startIndex + index"
          :data-key="getItemKey(item, index)"
          v-bind="componentProps"
          v-on="componentEvents"
          class="virtual-scroll-item"
          :style="getItemStyle(item, index)"
        />
      </template>
      <div v-else class="virtual-scroll-empty">
        <slot name="empty">暂无数据</slot>
      </div>
    </div>
    
    <!-- 加载更多指示器 -->
    <div 
      v-if="loading" 
      class="virtual-scroll-loading"
    >
      <slot name="loading">
        <el-skeleton animated :rows="1" style="width: 80%" />
      </slot>
    </div>
    
    <!-- 错误提示 -->
    <div 
      v-if="error" 
      class="virtual-scroll-error"
    >
      <slot name="error">
        <el-empty description="加载失败" :image-size="60">
          <el-button type="primary" size="small" @click="refresh">重试</el-button>
        </el-empty>
      </slot>
    </div>
    
    <!-- 滚动到底部提示 -->
    <div 
      v-if="hasReachedEnd && !loading && !error" 
      class="virtual-scroll-end"
    >
      <slot name="end">已加载全部数据</slot>
    </div>
  </div>
</template>

<script lang="ts" setup>
import { ref, computed, watch, onMounted, onUnmounted, nextTick } from 'vue'
import { ElMessage } from 'element-plus'
import { performanceService } from '@/services/performanceService'

// Props定义
interface Props {
  // 数据列表
  items: any[]
  // 组件或组件名称
  itemComponent: any
  // 项高度，或者返回高度的函数
  itemHeight: number | ((item: any, index: number) => number)
  // 额外渲染的缓冲区数量（上下各增加的项数）
  buffer?: number
  // 是否启用虚拟滚动
  enabled?: boolean
  // 键值获取函数或属性名
  keyField?: string | ((item: any, index: number) => string | number)
  // 是否监听窗口调整
  observeResize?: boolean
  // 加载更多的阈值（滚动到底部多少像素时触发）
  loadMoreThreshold?: number
  // 是否自动加载更多
  autoLoadMore?: boolean
  // 预渲染比例
  prerenderRatio?: number
  // 组件Props
  componentProps?: Record<string, any>
  // 是否启用动画
  animation?: boolean
  // 动画持续时间
  animationDuration?: number
}

const props = withDefaults(defineProps<Props>(), {
  buffer: 5,
  enabled: true,
  keyField: 'id',
  observeResize: true,
  loadMoreThreshold: 100,
  autoLoadMore: false,
  prerenderRatio: 1.5,
  componentProps: () => ({}),
  animation: true,
  animationDuration: 300
})

// Emits定义
interface Emits {
  (e: 'scroll', scrollEvent: Event, info: { startIndex: number, endIndex: number, visibleItems: any[] }): void
  (e: 'loadMore'): void
  (e: 'refresh'): void
  (e: 'reachedEnd'): void
  (e: 'update:items', items: any[]): void
}

const emit = defineEmits<Emits>()

// 组件事件转发
const componentEvents = defineEmits()

// 响应式数据
const scrollContainer = ref<HTMLElement>()
const containerHeight = ref(0)
const scrollTop = ref(0)
const loading = ref(false)
const error = ref(false)
const hasReachedEnd = ref(false)
const resizeObserver = ref<ResizeObserver | null>(null)

// 计算属性

// 计算总高度
const totalHeight = computed(() => {
  if (!props.enabled || !props.items.length) return 0
  
  // 如果是固定高度
  if (typeof props.itemHeight === 'number') {
    return props.items.length * props.itemHeight
  }
  
  // 如果是动态高度，计算所有项的高度之和
  return props.items.reduce((total, item, index) => {
    return total + props.itemHeight(item, index)
  }, 0)
})

// 计算起始索引
const startIndex = computed(() => {
  if (!props.enabled || !props.items.length) return 0
  
  // 如果是固定高度
  if (typeof props.itemHeight === 'number') {
    return Math.max(0, Math.floor(scrollTop.value / props.itemHeight) - props.buffer)
  }
  
  // 如果是动态高度，需要找到起始位置
  let accumulatedHeight = 0
  let index = 0
  
  while (index < props.items.length && accumulatedHeight <= scrollTop.value) {
    accumulatedHeight += props.itemHeight(props.items[index], index)
    index++
  }
  
  return Math.max(0, index - 1 - props.buffer)
})

// 计算可见项结束索引
const endIndex = computed(() => {
  if (!props.enabled || !props.items.length) return 0
  
  // 如果是固定高度
  if (typeof props.itemHeight === 'number') {
    const visibleCount = Math.ceil(containerHeight.value / props.itemHeight)
    return Math.min(
      props.items.length - 1,
      Math.ceil(scrollTop.value / props.itemHeight) + visibleCount + props.buffer
    )
  }
  
  // 如果是动态高度，需要计算可见区域内的项
  let accumulatedHeight = 0
  let index = startIndex.value
  const visibleEnd = scrollTop.value + containerHeight.value + props.itemHeight(props.items[0] || {}, 0) * props.buffer
  
  while (index < props.items.length && accumulatedHeight < visibleEnd) {
    accumulatedHeight += props.itemHeight(props.items[index], index)
    index++
  }
  
  return Math.min(props.items.length - 1, index - 1)
})

// 计算可见项
const visibleItems = computed(() => {
  if (!props.enabled) {
    return props.items
  }
  
  // 限制索引范围
  const start = Math.max(0, startIndex.value)
  const end = Math.min(props.items.length - 1, endIndex.value)
  
  return props.items.slice(start, end + 1)
})

// 计算内容偏移量
const contentOffset = computed(() => {
  if (!props.enabled || !props.items.length) return 0
  
  // 如果是固定高度
  if (typeof props.itemHeight === 'number') {
    return startIndex.value * props.itemHeight
  }
  
  // 如果是动态高度，计算偏移量
  let offset = 0
  for (let i = 0; i < startIndex.value && i < props.items.length; i++) {
    offset += props.itemHeight(props.items[i], i)
  }
  
  return offset
})

// 容器样式
const containerStyle = computed(() => ({
  height: '100%',
  overflow: 'auto',
  position: 'relative' as const,
  contain: 'layout style paint'
}))

// 内容样式
const contentStyle = computed(() => {
  if (!props.enabled) {
    return {}
  }
  
  return {
    position: 'absolute' as const,
    top: '0',
    left: '0',
    right: '0',
    transform: `translateY(${contentOffset.value}px)`,
    transition: props.animation ? `transform ${props.animationDuration}ms ease-out` : 'none'
  }
})

// 方法定义

// 处理滚动事件
const handleScroll = (event: Event) => {
  const target = event.target as HTMLElement
  scrollTop.value = target.scrollTop
  
  // 记录性能指标
  performanceService.recordMetric({
    id: 'virtual_scroll_event',
    name: 'Virtual Scroll Event',
    value: Date.now(),
    unit: 'timestamp',
    metadata: {
      startIndex: startIndex.value,
      endIndex: endIndex.value,
      visibleCount: visibleItems.value.length
    }
  })
  
  // 触发滚动事件
  emit('scroll', event, {
    startIndex: startIndex.value,
    endIndex: endIndex.value,
    visibleItems: visibleItems.value
  })
  
  // 检查是否需要加载更多
  if (props.autoLoadMore && !loading.value && !error.value && !hasReachedEnd.value) {
    checkLoadMore()
  }
  
  // 检查是否滚动到底部
  checkReachedEnd(target)
}

// 检查是否需要加载更多
const checkLoadMore = () => {
  if (!scrollContainer.value) return
  
  const { scrollHeight, scrollTop, clientHeight } = scrollContainer.value
  const remainingScroll = scrollHeight - scrollTop - clientHeight
  
  if (remainingScroll <= props.loadMoreThreshold) {
    emit('loadMore')
  }
}

// 检查是否滚动到底部
const checkReachedEnd = (target?: HTMLElement) => {
  const container = target || scrollContainer.value
  if (!container) return
  
  const { scrollHeight, scrollTop, clientHeight } = container
  const isAtBottom = scrollHeight - scrollTop - clientHeight <= 1
  
  if (isAtBottom) {
    emit('reachedEnd')
  }
}

// 获取项的键
const getItemKey = (item: any, index: number): string | number => {
  if (typeof props.keyField === 'function') {
    return props.keyField(item, index)
  }
  
  return item[props.keyField] !== undefined ? item[props.keyField] : index
}

// 获取项的样式
const getItemStyle = (item: any, index: number): Record<string, any> => {
  const style: Record<string, any> = {}
  
  // 设置高度
  if (typeof props.itemHeight === 'number') {
    style.height = `${props.itemHeight}px`
  } else {
    style.height = `${props.itemHeight(item, index)}px`
  }
  
  // 添加动画
  if (props.animation) {
    style.transition = `opacity ${props.animationDuration}ms ease-out`
  }
  
  return style
}

// 刷新数据
const refresh = () => {
  error.value = false
  loading.value = true
  emit('refresh')
  
  // 重置滚动位置
  scrollToTop()
}

// 滚动到顶部
const scrollToTop = () => {
  if (scrollContainer.value) {
    scrollContainer.value.scrollTop = 0
    scrollTop.value = 0
  }
}

// 滚动到底部
const scrollToBottom = () => {
  if (scrollContainer.value) {
    scrollContainer.value.scrollTop = scrollContainer.value.scrollHeight
  }
}

// 滚动到指定索引
const scrollToIndex = (index: number, options?: { behavior?: ScrollBehavior; offset?: number }) => {
  if (!scrollContainer.value || index < 0 || index >= props.items.length) return
  
  let scrollPosition = 0
  
  // 计算目标位置
  if (typeof props.itemHeight === 'number') {
    scrollPosition = index * props.itemHeight
  } else {
    for (let i = 0; i < index; i++) {
      scrollPosition += props.itemHeight(props.items[i], i)
    }
  }
  
  // 应用偏移量
  if (options?.offset !== undefined) {
    scrollPosition += options.offset
  }
  
  // 执行滚动
  if (options?.behavior === 'smooth') {
    scrollContainer.value.scrollTo({
      top: scrollPosition,
      behavior: 'smooth'
    })
  } else {
    scrollContainer.value.scrollTop = scrollPosition
  }
}

// 滚动到指定项
const scrollToItem = (item: any, options?: { behavior?: ScrollBehavior; offset?: number }) => {
  const index = props.items.indexOf(item)
  if (index !== -1) {
    scrollToIndex(index, options)
  }
}

// 预渲染可视区域外的项
const prerenderItems = () => {
  // 预渲染策略：根据预渲染比例计算需要提前渲染的项数
  const visibleCount = endIndex.value - startIndex.value + 1
  const prerenderCount = Math.floor(visibleCount * props.prerenderRatio)
  
  // 这里可以实现更复杂的预渲染逻辑
  // 例如：优先预渲染视口附近的项，或者根据用户滚动方向预测
}

// 处理窗口大小变化
const handleResize = () => {
  if (!scrollContainer.value) return
  
  // 更新容器高度
  const rect = scrollContainer.value.getBoundingClientRect()
  containerHeight.value = rect.height
  
  // 记录性能指标
  performanceService.recordMetric({
    id: 'virtual_scroll_resize',
    name: 'Virtual Scroll Resize',
    value: Date.now(),
    unit: 'timestamp',
    metadata: { containerHeight: containerHeight.value }
  })
}

// 设置加载状态
const setLoading = (status: boolean) => {
  loading.value = status
}

// 设置错误状态
const setError = (status: boolean) => {
  error.value = status
}

// 设置是否到达末尾
const setHasReachedEnd = (status: boolean) => {
  hasReachedEnd.value = status
}

// 重新计算布局
const recalculateLayout = async () => {
  await nextTick()
  handleResize()
}

// 测量性能
const measurePerformance = () => {
  const startTime = performance.now()
  
  // 触发可见项计算
  const visible = visibleItems.value
  
  const endTime = performance.now()
  const calculationTime = endTime - startTime
  
  performanceService.recordMetric({
    id: 'virtual_scroll_calculation',
    name: 'Virtual Scroll Calculation Time',
    value: calculationTime,
    unit: 'ms',
    metadata: {
      visibleCount: visible.length,
      totalCount: props.items.length
    }
  })
  
  // 如果计算时间过长，发出警告
  if (calculationTime > 5) {
    performanceService.recordWarning('virtual_scroll_performance', {
      message: 'Virtual scroll calculation time exceeds threshold',
      calculationTime,
      visibleCount: visible.length,
      totalCount: props.items.length
    })
  }
}

// 暴露方法给父组件
defineExpose({
  refresh,
  scrollToTop,
  scrollToBottom,
  scrollToIndex,
  scrollToItem,
  prerenderItems,
  recalculateLayout,
  setLoading,
  setError,
  setHasReachedEnd,
  measurePerformance
})

// 生命周期钩子

onMounted(async () => {
  await nextTick()
  
  // 初始化容器高度
  handleResize()
  
  // 设置ResizeObserver
  if (props.observeResize && window.ResizeObserver) {
    resizeObserver.value = new ResizeObserver(() => {
      handleResize()
    })
    
    if (scrollContainer.value) {
      resizeObserver.value.observe(scrollContainer.value)
    }
  }
  
  // 监听窗口大小变化（降级方案）
  if (props.observeResize && !window.ResizeObserver) {
    window.addEventListener('resize', handleResize)
  }
  
  // 记录初始化
  performanceService.recordMetric({
    id: 'virtual_scroll_init',
    name: 'Virtual Scroll Initialized',
    value: Date.now(),
    unit: 'timestamp',
    metadata: {
      itemCount: props.items.length,
      enabled: props.enabled
    }
  })
  
  // 检查是否需要立即加载更多
  if (props.autoLoadMore && !hasReachedEnd.value && props.items.length === 0) {
    emit('loadMore')
  }
})

onUnmounted(() => {
  // 清理ResizeObserver
  if (resizeObserver.value) {
    resizeObserver.value.disconnect()
    resizeObserver.value = null
  }
  
  // 清理事件监听器
  window.removeEventListener('resize', handleResize)
  
  // 记录卸载
  performanceService.recordMetric({
    id: 'virtual_scroll_unmount',
    name: 'Virtual Scroll Unmounted',
    value: Date.now(),
    unit: 'timestamp'
  })
})

// 监听数据变化
watch(
  () => props.items,
  async () => {
    await nextTick()
    
    // 测量性能
    measurePerformance()
    
    // 检查是否需要重新计算布局
    if (containerHeight.value === 0) {
      handleResize()
    }
    
    // 如果数据加载完成，重置加载状态
    if (loading.value) {
      loading.value = false
    }
  },
  { deep: true }
)

// 监听启用状态变化
watch(
  () => props.enabled,
  () => {
    recalculateLayout()
  }
)

// 监听buffer变化
watch(
  () => props.buffer,
  () => {
    // 重新计算可见项
    measurePerformance()
  }
)
</script>

<style scoped>
.virtual-scroll-container {
  width: 100%;
  height: 100%;
  -webkit-overflow-scrolling: touch;
  scroll-behavior: smooth;
}

.virtual-scroll-container::-webkit-scrollbar {
  width: 6px;
  height: 6px;
}

.virtual-scroll-container::-webkit-scrollbar-track {
  background: #f1f1f1;
  border-radius: 3px;
}

.virtual-scroll-container::-webkit-scrollbar-thumb {
  background: #c1c1c1;
  border-radius: 3px;
}

.virtual-scroll-container::-webkit-scrollbar-thumb:hover {
  background: #a8a8a8;
}

.virtual-scroll-placeholder {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  opacity: 0;
  pointer-events: none;
}

.virtual-scroll-content {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  width: 100%;
}

.virtual-scroll-item {
  width: 100%;
  box-sizing: border-box;
  overflow: hidden;
}

.virtual-scroll-empty,
.virtual-scroll-loading,
.virtual-scroll-error,
.virtual-scroll-end {
  padding: 20px;
  text-align: center;
  color: #909399;
  font-size: 14px;
}

.virtual-scroll-empty {
  min-height: 100px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.virtual-scroll-loading {
  background-color: #fafafa;
  border-radius: 4px;
  margin: 10px;
}

.virtual-scroll-error {
  background-color: #fef0f0;
  border-radius: 4px;
  margin: 10px;
}

.virtual-scroll-end {
  color: #c0c4cc;
  font-size: 12px;
  padding: 10px;
}

/* 动画效果 */
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

.virtual-scroll-item {
  animation: fadeIn 0.3s ease-out;
}

/* 性能优化：使用 contain 属性 */
@supports (contain: layout style paint) {
  .virtual-scroll-container {
    contain: layout style paint;
  }
  
  .virtual-scroll-content {
    contain: layout style paint size;
  }
}
</style>