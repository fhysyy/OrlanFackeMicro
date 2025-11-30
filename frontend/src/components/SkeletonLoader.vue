<template>
  <div
    class="skeleton-container"
    :class="containerClass"
  >
    <!-- 自定义内容区域 -->
    <template v-if="$slots.default">
      <slot />
    </template>
    <!-- 预定义类型的骨架屏 -->
    <template v-else>
      <!-- 卡片类型 -->
      <div
        v-if="type === 'card'"
        class="skeleton-card"
      >
        <div
          v-if="cardConfig.showHeader"
          class="skeleton-card-header"
        >
          <div
            v-if="cardConfig.showAvatar"
            class="skeleton-avatar"
          />
          <div
            v-if="cardConfig.showTitle"
            class="skeleton-card-title"
          />
        </div>
        <div class="skeleton-card-content">
          <div
            v-for="i in cardConfig.lineCount"
            :key="i"
            class="skeleton-line"
            :class="{ 'skeleton-line-last': i === cardConfig.lineCount }"
          />
        </div>
        <div
          v-if="cardConfig.showFooter"
          class="skeleton-card-footer"
        >
          <div class="skeleton-button" />
          <div class="skeleton-button" />
        </div>
      </div>

      <!-- 列表类型 -->
      <div
        v-else-if="type === 'list'"
        class="skeleton-list"
      >
        <div
          v-for="i in listConfig.itemCount"
          :key="i"
          class="skeleton-list-item"
          :class="{ 'skeleton-list-item-last': i === listConfig.itemCount }"
        >
          <div
            v-if="listConfig.showAvatar"
            class="skeleton-avatar"
          />
          <div class="skeleton-list-content">
            <div
              v-if="listConfig.showTitle"
              class="skeleton-list-title"
            />
            <div
              v-for="j in listConfig.lineCount"
              :key="j"
              class="skeleton-line"
              :class="{ 'skeleton-line-last': j === listConfig.lineCount }"
            />
          </div>
          <div
            v-if="listConfig.showAction"
            class="skeleton-list-action"
          >
            <div class="skeleton-button" />
          </div>
        </div>
      </div>

      <!-- 表格类型 -->
      <div
        v-else-if="type === 'table'"
        class="skeleton-table"
      >
        <div
          v-if="tableConfig.showHeader"
          class="skeleton-table-header"
        >
          <div
            v-for="i in tableConfig.columnCount"
            :key="i"
            class="skeleton-table-header-cell"
          />
        </div>
        <div class="skeleton-table-body">
          <div
            v-for="i in tableConfig.rowCount"
            :key="i"
            class="skeleton-table-row"
          >
            <div
              v-for="j in tableConfig.columnCount"
              :key="j"
              class="skeleton-table-cell"
              :class="getTableCellClass(j, tableConfig.cellTypes[j - 1])"
            >
              <div
                v-if="tableConfig.cellTypes[j - 1] === 'avatar'"
                class="skeleton-avatar-small"
              />
              <div
                v-else-if="tableConfig.cellTypes[j - 1] === 'button'"
                class="skeleton-button-small"
              />
              <div
                v-else-if="tableConfig.cellTypes[j - 1] === 'switch'"
                class="skeleton-switch"
              />
              <div
                v-else
                class="skeleton-table-cell-content"
              />
            </div>
          </div>
        </div>
      </div>

      <!-- 表单类型 -->
      <div
        v-else-if="type === 'form'"
        class="skeleton-form"
      >
        <div
          v-for="i in formConfig.itemCount"
          :key="i"
          class="skeleton-form-item"
        >
          <div class="skeleton-form-label" />
          <div
            v-if="formConfig.fieldTypes[i - 1] === 'select'"
            class="skeleton-select"
          />
          <div
            v-else-if="formConfig.fieldTypes[i - 1] === 'checkbox'"
            class="skeleton-checkbox"
          />
          <div
            v-else-if="formConfig.fieldTypes[i - 1] === 'radio'"
            class="skeleton-radio"
          />
          <div
            v-else-if="formConfig.fieldTypes[i - 1] === 'slider'"
            class="skeleton-slider"
          />
          <div
            v-else
            class="skeleton-input"
          />
        </div>
        <div class="skeleton-form-actions">
          <div class="skeleton-button" />
          <div class="skeleton-button" />
        </div>
      </div>

      <!-- 详情页类型 -->
      <div
        v-else-if="type === 'detail'"
        class="skeleton-detail"
      >
        <div
          v-if="detailConfig.showHeader"
          class="skeleton-detail-header"
        >
          <div class="skeleton-detail-title" />
          <div
            v-if="detailConfig.showMeta"
            class="skeleton-detail-meta"
          />
        </div>
        <div class="skeleton-detail-content">
          <div
            v-for="i in detailConfig.sectionCount"
            :key="i"
            class="skeleton-detail-section"
          >
            <div class="skeleton-detail-section-title" />
            <div
              v-for="j in detailConfig.fieldCountPerSection"
              :key="j"
              class="skeleton-detail-field"
            >
              <div class="skeleton-detail-field-label" />
              <div class="skeleton-detail-field-value" />
            </div>
          </div>
        </div>
      </div>

      <!-- 默认骨架屏 -->
      <div
        v-else
        class="skeleton-default"
      >
        <div class="skeleton-avatar" />
        <div class="skeleton-title" />
        <div class="skeleton-line" />
        <div class="skeleton-line" />
        <div class="skeleton-line skeleton-line-last" />
      </div>
    </template>
  </div>
</template>

<script lang="ts" setup>
import { computed, ref, onMounted, onUnmounted } from 'vue'
import { performanceService } from '@/services/performanceService'

// 骨架屏类型
type SkeletonType = 'card' | 'list' | 'table' | 'form' | 'detail' | 'default'

// 卡片配置
interface CardConfig {
  showHeader: boolean
  showAvatar: boolean
  showTitle: boolean
  lineCount: number
  showFooter: boolean
}

// 列表配置
interface ListConfig {
  itemCount: number
  showAvatar: boolean
  showTitle: boolean
  lineCount: number
  showAction: boolean
}

// 表格配置
interface TableConfig {
  showHeader: boolean
  rowCount: number
  columnCount: number
  cellTypes: Array<'text' | 'avatar' | 'button' | 'switch'>
}

// 表单配置
interface FormConfig {
  itemCount: number
  fieldTypes: Array<'input' | 'select' | 'checkbox' | 'radio' | 'slider'>
}

// 详情页配置
interface DetailConfig {
  showHeader: boolean
  showMeta: boolean
  sectionCount: number
  fieldCountPerSection: number
}

// Props定义
interface Props {
  // 骨架屏类型
  type?: SkeletonType
  // 卡片配置
  cardConfig?: Partial<CardConfig>
  // 列表配置
  listConfig?: Partial<ListConfig>
  // 表格配置
  tableConfig?: Partial<TableConfig>
  // 表单配置
  formConfig?: Partial<FormConfig>
  // 详情页配置
  detailConfig?: Partial<DetailConfig>
  // 是否显示动画
  animated?: boolean
  // 动画延迟（毫秒）
  animationDelay?: number
  // 动画持续时间（毫秒）
  animationDuration?: number
  // 是否显示脉动效果
  pulse?: boolean
  // 自定义类名
  customClass?: string
  // 是否使用深色模式
  darkMode?: boolean
  // 是否响应式
  responsive?: boolean
  // 是否显示圆角
  rounded?: boolean
  // 骨架屏宽度
  width?: string | number
  // 骨架屏高度
  height?: string | number
  // 是否内联显示
  inline?: boolean
  // 加载状态（用于外部控制）
  loading?: boolean
  // 性能监控ID
  performanceId?: string
}

// 默认配置
const defaultCardConfig: CardConfig = {
  showHeader: true,
  showAvatar: true,
  showTitle: true,
  lineCount: 3,
  showFooter: true
}

const defaultListConfig: ListConfig = {
  itemCount: 5,
  showAvatar: true,
  showTitle: true,
  lineCount: 2,
  showAction: false
}

const defaultTableConfig: TableConfig = {
  showHeader: true,
  rowCount: 5,
  columnCount: 4,
  cellTypes: ['text', 'text', 'text', 'button']
}

const defaultFormConfig: FormConfig = {
  itemCount: 4,
  fieldTypes: ['input', 'select', 'input', 'input']
}

const defaultDetailConfig: DetailConfig = {
  showHeader: true,
  showMeta: true,
  sectionCount: 2,
  fieldCountPerSection: 3
}

// Props实现
const props = withDefaults(defineProps<Props>(), {
  type: 'default',
  cardConfig: () => ({}),
  listConfig: () => ({}),
  tableConfig: () => ({}),
  formConfig: () => ({}),
  detailConfig: () => ({}),
  animated: true,
  animationDelay: 0,
  animationDuration: 1500,
  pulse: true,
  customClass: '',
  darkMode: false,
  responsive: true,
  rounded: true,
  width: '100%',
  height: 'auto',
  inline: false,
  loading: true,
  performanceId: `skeleton_${Date.now()}`
})

// 计算合并后的配置
const cardConfig = computed((): CardConfig => ({ ...defaultCardConfig, ...props.cardConfig }))
const listConfig = computed((): ListConfig => ({ ...defaultListConfig, ...props.listConfig }))
const tableConfig = computed((): TableConfig => ({ ...defaultTableConfig, ...props.tableConfig }))
const formConfig = computed((): FormConfig => ({ ...defaultFormConfig, ...props.formConfig }))
const detailConfig = computed((): DetailConfig => ({ ...defaultDetailConfig, ...props.detailConfig }))

// 容器类名
const containerClass = computed(() => {
  return [
    props.customClass,
    { 'skeleton-animated': props.animated && props.pulse },
    { 'skeleton-dark': props.darkMode },
    { 'skeleton-responsive': props.responsive },
    { 'skeleton-rounded': props.rounded },
    { 'skeleton-inline': props.inline },
    { [`skeleton-${props.type}`]: !props.$slots.default }
  ]
})

// 骨架屏元素可见性
const isVisible = ref(true)

// 性能监控数据
const performanceData = ref({
  startTime: 0,
  endTime: 0,
  duration: 0
})

// 获取表格单元格类名
const getTableCellClass = (index: number, type: string) => {
  return {
    [`skeleton-table-cell-${type}`]: true,
    ['skeleton-table-cell-last']: index === tableConfig.value.columnCount
  }
}

// 设置动画样式
const setAnimationStyle = () => {
  if (!props.animated || !props.pulse) return
  
  const styleId = 'skeleton-animation-style'
  let styleElement = document.getElementById(styleId)
  
  if (!styleElement) {
    styleElement = document.createElement('style')
    styleElement.id = styleId
    document.head.appendChild(styleElement)
  }
  
  const animationKeyframes = `
    @keyframes skeleton-loading {
      0% { opacity: 0.4; }
      50% { opacity: 0.7; }
      100% { opacity: 0.4; }
    }
  `
  
  styleElement.textContent = animationKeyframes
}

// 检测元素是否在视口中
const isInViewport = (element: HTMLElement): boolean => {
  const rect = element.getBoundingClientRect()
  return (
    rect.top >= 0 &&
    rect.left >= 0 &&
    rect.bottom <= (window.innerHeight || document.documentElement.clientHeight) &&
    rect.right <= (window.innerWidth || document.documentElement.clientWidth)
  )
}

// 初始化性能监控
const initPerformanceMonitoring = () => {
  performanceData.value.startTime = performance.now()
}

// 结束性能监控
const endPerformanceMonitoring = () => {
  performanceData.value.endTime = performance.now()
  performanceData.value.duration = performanceData.value.endTime - performanceData.value.startTime
  
  // 记录性能指标
  performanceService.recordMetric({
    id: props.performanceId,
    name: 'Skeleton Display Time',
    value: performanceData.value.duration,
    unit: 'ms',
    metadata: {
      type: props.type,
      animated: props.animated,
      duration: props.animationDuration
    }
  })
}

// 生命周期钩子
onMounted(() => {
  // 初始化性能监控
  initPerformanceMonitoring()
  
  // 设置动画样式
  setAnimationStyle()
  
  // 模拟内容加载完成
  const simulateLoading = () => {
    // 这里仅用于演示，实际项目中应根据真实加载状态控制
    if (!props.loading) {
      endPerformanceMonitoring()
    }
  }
  
  simulateLoading()
})

onUnmounted(() => {
  // 如果组件卸载时尚未结束性能监控，则结束
  if (performanceData.value.duration === 0) {
    endPerformanceMonitoring()
  }
})

// 导出服务
defineExpose({
  // 方法
  isInViewport,
  initPerformanceMonitoring,
  endPerformanceMonitoring,
  
  // 状态
  isVisible,
  performanceData
})
</script>

<style scoped>
/* 基础样式 */
.skeleton-container {
  display: flex;
  flex-direction: column;
  width: v-bind('props.width');
  height: v-bind('props.height');
  position: relative;
  background-color: transparent;
}

.skeleton-animated {
  animation: skeleton-loading var(--animation-duration, 1.5s) ease-in-out infinite;
  animation-delay: var(--animation-delay, 0ms);
}

/* 深色模式 */
.skeleton-dark .skeleton-avatar,
.skeleton-dark .skeleton-title,
.skeleton-dark .skeleton-line,
.skeleton-dark .skeleton-card-title,
.skeleton-dark .skeleton-card-footer,
.skeleton-dark .skeleton-list-title,
.skeleton-dark .skeleton-list-action,
.skeleton-dark .skeleton-button,
.skeleton-dark .skeleton-table-header-cell,
.skeleton-dark .skeleton-table-cell,
.skeleton-dark .skeleton-form-label,
.skeleton-dark .skeleton-input,
.skeleton-dark .skeleton-select,
.skeleton-dark .skeleton-checkbox,
.skeleton-dark .skeleton-radio,
.skeleton-dark .skeleton-slider,
.skeleton-dark .skeleton-detail-title,
.skeleton-dark .skeleton-detail-meta,
.skeleton-dark .skeleton-detail-section-title,
.skeleton-dark .skeleton-detail-field-label,
.skeleton-dark .skeleton-detail-field-value {
  background-color: rgba(255, 255, 255, 0.1);
}

/* 响应式 */
.skeleton-responsive {
  @media (max-width: 768px) {
    width: 100%;
  }
}

/* 圆角 */
.skeleton-rounded .skeleton-avatar,
.skeleton-rounded .skeleton-title,
.skeleton-rounded .skeleton-line,
.skeleton-rounded .skeleton-card,
.skeleton-rounded .skeleton-list-item,
.skeleton-rounded .skeleton-table,
.skeleton-rounded .skeleton-form-item,
.skeleton-rounded .skeleton-detail-section {
  border-radius: 6px;
}

/* 内联 */
.skeleton-inline {
  display: inline-flex;
}

/* 通用骨架元素 */
.skeleton-avatar {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  background-color: rgba(0, 0, 0, 0.1);
  margin-right: 16px;
}

.skeleton-avatar-small {
  width: 24px;
  height: 24px;
  border-radius: 50%;
  background-color: rgba(0, 0, 0, 0.1);
}

.skeleton-title {
  height: 20px;
  width: 60%;
  background-color: rgba(0, 0, 0, 0.1);
  margin-bottom: 12px;
}

.skeleton-line {
  height: 14px;
  background-color: rgba(0, 0, 0, 0.1);
  margin-bottom: 8px;
  width: 100%;
}

.skeleton-line-last {
  width: 70%;
  margin-bottom: 0;
}

.skeleton-button {
  height: 32px;
  width: 80px;
  background-color: rgba(0, 0, 0, 0.1);
  border-radius: 4px;
}

.skeleton-button-small {
  height: 24px;
  width: 60px;
  background-color: rgba(0, 0, 0, 0.1);
  border-radius: 4px;
}

/* 卡片类型 */
.skeleton-card {
  background-color: #ffffff;
  border-radius: 8px;
  padding: 16px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
  display: flex;
  flex-direction: column;
}

.skeleton-dark .skeleton-card {
  background-color: rgba(255, 255, 255, 0.05);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.2);
}

.skeleton-card-header {
  display: flex;
  align-items: center;
  margin-bottom: 16px;
}

.skeleton-card-title {
  height: 20px;
  width: 150px;
  background-color: rgba(0, 0, 0, 0.1);
}

.skeleton-card-content {
  flex: 1;
  margin-bottom: 16px;
}

.skeleton-card-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}

/* 列表类型 */
.skeleton-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.skeleton-list-item {
  display: flex;
  align-items: flex-start;
  padding: 12px 0;
  border-bottom: 1px solid rgba(0, 0, 0, 0.06);
}

.skeleton-dark .skeleton-list-item {
  border-bottom-color: rgba(255, 255, 255, 0.1);
}

.skeleton-list-item-last {
  border-bottom: none;
}

.skeleton-list-content {
  flex: 1;
}

.skeleton-list-title {
  height: 18px;
  width: 200px;
  background-color: rgba(0, 0, 0, 0.1);
  margin-bottom: 8px;
}

.skeleton-list-action {
  margin-left: 16px;
}

/* 表格类型 */
.skeleton-table {
  background-color: #ffffff;
  border-radius: 8px;
  overflow: hidden;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.skeleton-dark .skeleton-table {
  background-color: rgba(255, 255, 255, 0.05);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.2);
}

.skeleton-table-header {
  display: flex;
  border-bottom: 1px solid rgba(0, 0, 0, 0.1);
  padding: 12px 16px;
}

.skeleton-dark .skeleton-table-header {
  border-bottom-color: rgba(255, 255, 255, 0.2);
}

.skeleton-table-header-cell {
  height: 18px;
  background-color: rgba(0, 0, 0, 0.1);
  flex: 1;
  margin-right: 16px;
}

.skeleton-table-header-cell:last-child {
  margin-right: 0;
}

.skeleton-table-body {
  display: flex;
  flex-direction: column;
}

.skeleton-table-row {
  display: flex;
  padding: 12px 16px;
  border-bottom: 1px solid rgba(0, 0, 0, 0.06);
}

.skeleton-dark .skeleton-table-row {
  border-bottom-color: rgba(255, 255, 255, 0.1);
}

.skeleton-table-row:last-child {
  border-bottom: none;
}

.skeleton-table-cell {
  flex: 1;
  margin-right: 16px;
  display: flex;
  align-items: center;
  min-height: 24px;
}

.skeleton-table-cell:last-child {
  margin-right: 0;
}

.skeleton-table-cell-content {
  height: 14px;
  background-color: rgba(0, 0, 0, 0.1);
  width: 70%;
}

.skeleton-table-cell-avatar .skeleton-avatar-small {
  margin: 0;
}

.skeleton-table-cell-button .skeleton-button-small {
  margin: 0;
}

.skeleton-switch {
  width: 40px;
  height: 20px;
  background-color: rgba(0, 0, 0, 0.1);
  border-radius: 10px;
}

/* 表单类型 */
.skeleton-form {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.skeleton-form-item {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.skeleton-form-label {
  height: 16px;
  width: 100px;
  background-color: rgba(0, 0, 0, 0.1);
}

.skeleton-input {
  height: 36px;
  background-color: rgba(0, 0, 0, 0.1);
  border-radius: 4px;
}

.skeleton-select {
  height: 36px;
  background-color: rgba(0, 0, 0, 0.1);
  border-radius: 4px;
}

.skeleton-checkbox {
  width: 20px;
  height: 20px;
  background-color: rgba(0, 0, 0, 0.1);
  border-radius: 4px;
}

.skeleton-radio {
  width: 20px;
  height: 20px;
  background-color: rgba(0, 0, 0, 0.1);
  border-radius: 50%;
}

.skeleton-slider {
  height: 4px;
  background-color: rgba(0, 0, 0, 0.1);
  border-radius: 2px;
  margin-top: 10px;
}

.skeleton-form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  margin-top: 12px;
}

/* 详情页类型 */
.skeleton-detail {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.skeleton-detail-header {
  border-bottom: 1px solid rgba(0, 0, 0, 0.06);
  padding-bottom: 16px;
}

.skeleton-dark .skeleton-detail-header {
  border-bottom-color: rgba(255, 255, 255, 0.1);
}

.skeleton-detail-title {
  height: 28px;
  width: 300px;
  background-color: rgba(0, 0, 0, 0.1);
  margin-bottom: 8px;
}

.skeleton-detail-meta {
  height: 14px;
  width: 150px;
  background-color: rgba(0, 0, 0, 0.1);
}

.skeleton-detail-section {
  background-color: #ffffff;
  border-radius: 8px;
  padding: 20px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.skeleton-dark .skeleton-detail-section {
  background-color: rgba(255, 255, 255, 0.05);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.2);
}

.skeleton-detail-section-title {
  height: 20px;
  width: 150px;
  background-color: rgba(0, 0, 0, 0.1);
  margin-bottom: 16px;
}

.skeleton-detail-field {
  display: flex;
  margin-bottom: 12px;
}

.skeleton-detail-field:last-child {
  margin-bottom: 0;
}

.skeleton-detail-field-label {
  width: 120px;
  height: 16px;
  background-color: rgba(0, 0, 0, 0.1);
  margin-right: 16px;
  flex-shrink: 0;
}

.skeleton-detail-field-value {
  flex: 1;
  height: 16px;
  background-color: rgba(0, 0, 0, 0.1);
}

/* 默认骨架屏 */
.skeleton-default {
  display: flex;
  flex-direction: column;
  background-color: #ffffff;
  border-radius: 8px;
  padding: 20px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
  max-width: 400px;
}

.skeleton-dark .skeleton-default {
  background-color: rgba(255, 255, 255, 0.05);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.2);
}

.skeleton-default .skeleton-avatar {
  align-self: center;
  margin-right: 0;
  margin-bottom: 16px;
}

/* 动画定义 */
@keyframes skeleton-loading {
  0% {
    opacity: 0.4;
  }
  50% {
    opacity: 0.7;
  }
  100% {
    opacity: 0.4;
  }
}

/* 响应式设计 */
@media (max-width: 768px) {
  .skeleton-card,
  .skeleton-list-item,
  .skeleton-table,
  .skeleton-form,
  .skeleton-detail-section,
  .skeleton-default {
    padding: 12px;
  }
  
  .skeleton-avatar {
    width: 36px;
    height: 36px;
    margin-right: 12px;
  }
  
  .skeleton-title {
    width: 80%;
  }
  
  .skeleton-card-title {
    width: 120px;
  }
  
  .skeleton-list-title {
    width: 150px;
  }
  
  .skeleton-detail-title {
    width: 200px;
    height: 24px;
  }
  
  .skeleton-detail-field {
    flex-direction: column;
    gap: 4px;
  }
  
  .skeleton-detail-field-label {
    width: 100px;
    margin-right: 0;
  }
  
  .skeleton-table-header,
  .skeleton-table-row {
    padding: 8px 12px;
  }
  
  .skeleton-table-cell {
    margin-right: 8px;
  }
  
  .skeleton-table-cell-content {
    width: 100%;
  }
}

@media (max-width: 480px) {
  .skeleton-card,
  .skeleton-list-item,
  .skeleton-table,
  .skeleton-form,
  .skeleton-detail-section,
  .skeleton-default {
    padding: 8px;
  }
  
  .skeleton-card-footer,
  .skeleton-form-actions {
    flex-direction: column;
    gap: 8px;
  }
  
  .skeleton-button {
    width: 100%;
  }
  
  .skeleton-card-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 8px;
  }
  
  .skeleton-avatar {
    margin-right: 0;
  }
  
  .skeleton-table {
    overflow-x: auto;
  }
  
  .skeleton-table-header,
  .skeleton-table-row {
    min-width: 320px;
  }
}

/* 深色模式媒体查询 */
@media (prefers-color-scheme: dark) {
  .skeleton-avatar,
  .skeleton-title,
  .skeleton-line,
  .skeleton-card-title,
  .skeleton-card-footer,
  .skeleton-list-title,
  .skeleton-list-action,
  .skeleton-button,
  .skeleton-table-header-cell,
  .skeleton-table-cell,
  .skeleton-form-label,
  .skeleton-input,
  .skeleton-select,
  .skeleton-checkbox,
  .skeleton-radio,
  .skeleton-slider,
  .skeleton-detail-title,
  .skeleton-detail-meta,
  .skeleton-detail-section-title,
  .skeleton-detail-field-label,
  .skeleton-detail-field-value {
    background-color: rgba(255, 255, 255, 0.1);
  }
  
  .skeleton-card,
  .skeleton-table,
  .skeleton-detail-section,
  .skeleton-default {
    background-color: rgba(255, 255, 255, 0.05);
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.2);
  }
  
  .skeleton-list-item,
  .skeleton-detail-header {
    border-bottom-color: rgba(255, 255, 255, 0.1);
  }
  
  .skeleton-table-header {
    border-bottom-color: rgba(255, 255, 255, 0.2);
  }
}

/* 减少动画偏好设置 */
@media (prefers-reduced-motion: reduce) {
  .skeleton-animated {
    animation: none;
  }
}

/* 高对比度模式 */
@media (prefers-contrast: high) {
  .skeleton-avatar,
  .skeleton-title,
  .skeleton-line,
  .skeleton-card-title,
  .skeleton-card-footer,
  .skeleton-list-title,
  .skeleton-list-action,
  .skeleton-button,
  .skeleton-table-header-cell,
  .skeleton-table-cell,
  .skeleton-form-label,
  .skeleton-input,
  .skeleton-select,
  .skeleton-checkbox,
  .skeleton-radio,
  .skeleton-slider,
  .skeleton-detail-title,
  .skeleton-detail-meta,
  .skeleton-detail-section-title,
  .skeleton-detail-field-label,
  .skeleton-detail-field-value {
    background-color: rgba(0, 0, 0, 0.3);
  }
  
  .skeleton-dark .skeleton-avatar,
  .skeleton-dark .skeleton-title,
  .skeleton-dark .skeleton-line,
  .skeleton-dark .skeleton-card-title,
  .skeleton-dark .skeleton-card-footer,
  .skeleton-dark .skeleton-list-title,
  .skeleton-dark .skeleton-list-action,
  .skeleton-dark .skeleton-button,
  .skeleton-dark .skeleton-table-header-cell,
  .skeleton-dark .skeleton-table-cell,
  .skeleton-dark .skeleton-form-label,
  .skeleton-dark .skeleton-input,
  .skeleton-dark .skeleton-select,
  .skeleton-dark .skeleton-checkbox,
  .skeleton-dark .skeleton-radio,
  .skeleton-dark .skeleton-slider,
  .skeleton-dark .skeleton-detail-title,
  .skeleton-dark .skeleton-detail-meta,
  .skeleton-dark .skeleton-detail-section-title,
  .skeleton-dark .skeleton-detail-field-label,
  .skeleton-dark .skeleton-detail-field-value {
    background-color: rgba(255, 255, 255, 0.3);
  }
}
</style>