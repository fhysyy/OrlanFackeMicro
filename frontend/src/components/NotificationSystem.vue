<template>
  <div class="notification-container" :class="containerClass">
    <transition-group
      name="notification"
      tag="div"
      class="notification-list"
    >
      <div
        v-for="notification in notifications"
        :key="notification.id"
        class="notification-item"
        :class="[
          `notification-${notification.type}`,
          { 'notification-closable': notification.showClose },
          { 'notification-with-icon': notification.icon },
          { 'notification-with-progress': notification.showProgress },
          notification.customClass
        ]"
        :style="getNotificationStyle(notification)"
        @mouseenter="pauseTimer(notification.id)"
        @mouseleave="resumeTimer(notification.id)"
      >
        <!-- 图标 -->
        <div v-if="notification.icon" class="notification-icon">
          <el-icon :size="notification.iconSize || 20">
            <component :is="notification.icon" />
          </el-icon>
        </div>
        
        <!-- 内容区域 -->
        <div class="notification-content">
          <!-- 标题 -->
          <div v-if="notification.title" class="notification-title">
            {{ notification.title }}
          </div>
          
          <!-- 消息内容 -->
          <div class="notification-message" v-html="formatMessage(notification.message)"></div>
          
          <!-- 操作按钮 -->
          <div v-if="notification.actions && notification.actions.length > 0" class="notification-actions">
            <el-button
              v-for="(action, index) in notification.actions"
              :key="index"
              :type="action.type || 'default'"
              :size="action.size || 'small'"
              :icon="action.icon"
              :loading="action.loading"
              :disabled="action.disabled"
              @click.stop="handleAction(notification.id, action, index)"
              class="notification-action-btn"
            >
              {{ action.text }}
            </el-button>
          </div>
          
          <!-- 进度条 -->
          <div v-if="notification.showProgress && notification.duration" class="notification-progress">
            <el-progress
              :percentage="getProgress(notification)"
              :stroke-width="2"
              :status="notification.type === 'error' ? 'exception' : 'success'"
              :show-text="false"
              class="notification-progress-bar"
            />
          </div>
        </div>
        
        <!-- 关闭按钮 -->
        <button
          v-if="notification.showClose"
          type="button"
          class="notification-close-btn"
          @click.stop="closeNotification(notification.id)"
          :aria-label="'关闭通知'"
        >
          <el-icon :size="16">
            <Close />
          </el-icon>
        </button>
      </div>
    </transition-group>
  </div>
</template>

<script lang="ts" setup>
import { ref, computed, onUnmounted, nextTick } from 'vue'
import { ElButton, ElProgress, ElIcon } from 'element-plus'
import { 
  Close, 
  Success, 
  Warning, 
  InfoFilled as Info, 
  ErrorFilled as Error,
  Loading
} from '@element-plus/icons-vue'
import { performanceService } from '@/services/performanceService'

// 通知类型定义
export type NotificationType = 'success' | 'warning' | 'info' | 'error' | 'loading'

// 通知配置接口
export interface NotificationOptions {
  // 通知类型
  type?: NotificationType
  // 通知标题
  title?: string
  // 通知消息
  message: string | JSX.Element
  // 持续时间（毫秒）
  duration?: number | null
  // 是否显示关闭按钮
  showClose?: boolean
  // 自定义图标
  icon?: any
  // 图标大小
  iconSize?: number
  // 位置
  position?: 'top-right' | 'top-left' | 'bottom-right' | 'bottom-left' | 'top-center' | 'bottom-center'
  // 偏移量
  offset?: number
  // 自定义类名
  customClass?: string
  // 是否显示进度条
  showProgress?: boolean
  // 是否可以点击关闭
  clickToClose?: boolean
  // 是否在点击其他地方时关闭
  closeOnClickOutside?: boolean
  // 最大通知数量
  maxCount?: number
  // 是否使用HTML内容
  dangerouslyUseHTMLString?: boolean
  // 是否居中显示
  center?: boolean
  // 是否使用覆盖模式
  overlay?: boolean
  // 是否禁止滚动
  forbidClick?: boolean
  // 操作按钮
  actions?: Array<{
    text: string
    type?: 'primary' | 'success' | 'warning' | 'danger' | 'info'
    size?: 'default' | 'small' | 'large'
    icon?: any
    loading?: boolean
    disabled?: boolean
    callback?: (notificationId: string) => void
  }>
  // 关闭回调
  onClose?: (notificationId: string) => void
  // 点击回调
  onClick?: (notificationId: string) => void
  // 鼠标悬停回调
  onHover?: (notificationId: string) => void
  // 鼠标离开回调
  onLeave?: (notificationId: string) => void
}

// 内部通知接口
interface InternalNotification extends NotificationOptions {
  id: string
  type: NotificationType
  startTime: number
  pauseTime: number
  paused: boolean
  duration: number | null
  position: 'top-right' | 'top-left' | 'bottom-right' | 'bottom-left' | 'top-center' | 'bottom-center'
  offset: number
  maxCount?: number
}

// 响应式数据
const notifications = ref<InternalNotification[]>([])
const timers = ref<Map<string, number>>(new Map())
const pauseTimers = ref<Map<string, number>>(new Map())
const clickOutsideListener = ref<((event: MouseEvent) => void) | null>(null)

// 配置默认值
const defaultOptions: Partial<NotificationOptions> = {
  type: 'info',
  duration: 3000,
  showClose: true,
  showProgress: false,
  clickToClose: false,
  closeOnClickOutside: false,
  position: 'top-right',
  offset: 20,
  dangerouslyUseHTMLString: false,
  center: false,
  overlay: false,
  forbidClick: false,
  maxCount: 10
}

// 计算属性

// 容器样式类
const containerClass = computed(() => {
  const positions = new Set(notifications.value.map(n => n.position))
  return Array.from(positions).map(pos => `position-${pos}`)
})

// 方法定义

// 创建唯一ID
const generateId = (): string => {
  return `notification_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`
}

// 获取默认图标
const getDefaultIcon = (type: NotificationType) => {
  switch (type) {
    case 'success':
      return Success
    case 'warning':
      return Warning
    case 'info':
      return Info
    case 'error':
      return Error
    case 'loading':
      return Loading
    default:
      return Info
  }
}

// 显示通知
const showNotification = (options: NotificationOptions | string): string => {
  // 处理字符串参数
  const normalizedOptions: NotificationOptions = typeof options === 'string'
    ? { message: options }
    : options

  // 合并默认选项
  const mergedOptions = { ...defaultOptions, ...normalizedOptions }
  
  // 确保有类型和消息
  if (!mergedOptions.type) mergedOptions.type = 'info'
  if (!mergedOptions.message && !mergedOptions.title) {
    console.error('Notification message or title is required')
    return ''
  }

  // 生成通知ID
  const id = generateId()
  
  // 创建通知对象
  const notification: InternalNotification = {
    ...mergedOptions,
    id,
    type: mergedOptions.type,
    startTime: Date.now(),
    pauseTime: 0,
    paused: false,
    duration: mergedOptions.duration,
    position: mergedOptions.position || 'top-right',
    offset: mergedOptions.offset || 20
  }
  
  // 设置默认图标（如果未提供）
  if (!notification.icon && notification.type !== 'loading') {
    notification.icon = getDefaultIcon(notification.type)
  }

  // 检查最大通知数量
  const positionNotifications = notifications.value.filter(
    n => n.position === notification.position
  )
  
  if (notification.maxCount && positionNotifications.length >= notification.maxCount) {
    // 移除最早的通知
    const oldestNotification = positionNotifications[0]
    if (oldestNotification) {
      closeNotification(oldestNotification.id)
    }
  }

  // 添加通知
  notifications.value.push(notification)
  
  // 记录性能
  performanceService.recordMetric({
    id: `notification_${id}`,
    name: 'Notification Created',
    value: Date.now(),
    unit: 'timestamp',
    metadata: {
      type: notification.type,
      position: notification.position
    }
  })

  // 设置定时器
  if (notification.duration !== null && notification.duration > 0) {
    startTimer(notification)
  }

  // 添加点击外部关闭的监听器
  if (notification.closeOnClickOutside && !clickOutsideListener.value) {
    setupClickOutsideListener()
  }

  // 处理禁止滚动
  if (notification.forbidClick) {
    document.body.style.pointerEvents = 'none'
  }

  // 处理覆盖模式
  if (notification.overlay) {
    // 可以在这里添加覆盖层逻辑
  }

  return id
}

// 开始定时器
const startTimer = (notification: InternalNotification) => {
  if (notification.duration === null || notification.duration <= 0) return
  
  const timerId = window.setTimeout(() => {
    if (!notification.paused) {
      closeNotification(notification.id)
    }
  }, notification.duration) as unknown as number
  
  timers.value.set(notification.id, timerId)
}

// 暂停定时器
const pauseTimer = (id: string) => {
  const notification = notifications.value.find(n => n.id === id)
  if (!notification || notification.duration === null || notification.paused) return
  
  // 清除定时器
  const timerId = timers.value.get(id)
  if (timerId) {
    clearTimeout(timerId)
    timers.value.delete(id)
  }
  
  // 记录暂停时间
  notification.paused = true
  notification.pauseTime = Date.now()
  
  // 触发悬停回调
  if (notification.onHover) {
    notification.onHover(id)
  }
}

// 恢复定时器
const resumeTimer = (id: string) => {
  const notification = notifications.value.find(n => n.id === id)
  if (!notification || notification.duration === null || !notification.paused) return
  
  // 计算剩余时间
  const pauseDuration = Date.now() - notification.pauseTime
  const remainingTime = notification.duration - pauseDuration
  
  // 更新持续时间
  notification.duration = remainingTime > 0 ? remainingTime : 0
  notification.paused = false
  
  // 重新启动定时器
  if (notification.duration > 0) {
    startTimer(notification)
  } else {
    closeNotification(id)
  }
  
  // 触发离开回调
  if (notification.onLeave) {
    notification.onLeave(id)
  }
}

// 关闭通知
const closeNotification = (id: string) => {
  const index = notifications.value.findIndex(n => n.id === id)
  if (index === -1) return
  
  const notification = notifications.value[index]
  
  // 清除定时器
  const timerId = timers.value.get(id)
  if (timerId) {
    clearTimeout(timerId)
    timers.value.delete(id)
  }
  
  // 清除暂停定时器
  const pauseTimerId = pauseTimers.value.get(id)
  if (pauseTimerId) {
    clearTimeout(pauseTimerId)
    pauseTimers.value.delete(id)
  }
  
  // 触发关闭回调
  if (notification.onClose) {
    notification.onClose(id)
  }
  
  // 移除通知
  notifications.value.splice(index, 1)
  
  // 记录性能
  performanceService.recordMetric({
    id: `notification_close_${id}`,
    name: 'Notification Closed',
    value: Date.now(),
    unit: 'timestamp',
    metadata: {
      type: notification.type,
      displayTime: Date.now() - notification.startTime
    }
  })
  
  // 检查是否需要移除点击外部关闭的监听器
  if (clickOutsideListener.value && !notifications.value.some(n => n.closeOnClickOutside)) {
    removeClickOutsideListener()
  }
  
  // 恢复指针事件
  if (notification.forbidClick && !notifications.value.some(n => n.forbidClick)) {
    document.body.style.pointerEvents = ''
  }
}

// 关闭所有通知
const closeAllNotifications = () => {
  const notificationIds = [...notifications.value.map(n => n.id)]
  notificationIds.forEach(id => closeNotification(id))
}

// 关闭指定类型的通知
const closeNotificationsByType = (type: NotificationType) => {
  const notificationIds = notifications.value
    .filter(n => n.type === type)
    .map(n => n.id)
  
  notificationIds.forEach(id => closeNotification(id))
}

// 获取进度百分比
const getProgress = (notification: InternalNotification): number => {
  if (!notification.duration || notification.duration <= 0) return 0
  
  const elapsed = notification.paused
    ? notification.pauseTime - notification.startTime
    : Date.now() - notification.startTime
  
  const percentage = (elapsed / notification.duration) * 100
  return Math.min(100, Math.max(0, percentage))
}

// 处理通知点击
const handleNotificationClick = (id: string) => {
  const notification = notifications.value.find(n => n.id === id)
  if (!notification || notification.forbidClick) return
  
  // 触发点击回调
  if (notification.onClick) {
    notification.onClick(id)
  }
  
  // 如果配置了点击关闭
  if (notification.clickToClose) {
    closeNotification(id)
  }
}

// 处理操作按钮点击
const handleAction = (id: string, action: any, index: number) => {
  const notification = notifications.value.find(n => n.id === id)
  if (!notification || action.disabled || action.loading) return
  
  // 触发回调
  if (action.callback) {
    action.callback(id)
  }
}

// 设置点击外部关闭的监听器
const setupClickOutsideListener = () => {
  if (clickOutsideListener.value) return
  
  clickOutsideListener.value = (event: MouseEvent) => {
    const target = event.target as HTMLElement
    const isClickInside = target.closest('.notification-item')
    
    if (!isClickInside) {
      notifications.value
        .filter(n => n.closeOnClickOutside)
        .forEach(n => closeNotification(n.id))
    }
  }
  
  document.addEventListener('click', clickOutsideListener.value)
}

// 移除点击外部关闭的监听器
const removeClickOutsideListener = () => {
  if (clickOutsideListener.value) {
    document.removeEventListener('click', clickOutsideListener.value)
    clickOutsideListener.value = null
  }
}

// 格式化消息内容
const formatMessage = (message: string | JSX.Element): string => {
  if (typeof message === 'string') {
    return message
  }
  // 对于JSX元素，我们无法直接在v-html中使用，这里简化处理
  return String(message)
}

// 获取通知样式
const getNotificationStyle = (notification: InternalNotification): Record<string, string> => {
  const style: Record<string, string> = {}
  
  // 计算定位偏移
  const positionNotifications = notifications.value
    .filter(n => n.position === notification.position && n.id <= notification.id)
  
  // 计算垂直偏移
  let verticalOffset = notification.offset
  
  positionNotifications.forEach((n, index) => {
    if (n.id !== notification.id && index < positionNotifications.length - 1) {
      // 估算每个通知的高度（实际项目中可能需要更精确的计算）
      verticalOffset += 80 // 假设每个通知高度约80px
      verticalOffset += 16 // 通知之间的间距
    }
  })
  
  // 根据位置设置样式
  switch (notification.position) {
    case 'top-right':
      style.top = `${verticalOffset}px`
      style.right = `${notification.offset}px`
      break
    case 'top-left':
      style.top = `${verticalOffset}px`
      style.left = `${notification.offset}px`
      break
    case 'bottom-right':
      style.bottom = `${verticalOffset}px`
      style.right = `${notification.offset}px`
      break
    case 'bottom-left':
      style.bottom = `${verticalOffset}px`
      style.left = `${notification.offset}px`
      break
    case 'top-center':
      style.top = `${verticalOffset}px`
      style.left = '50%'
      style.transform = 'translateX(-50%)'
      break
    case 'bottom-center':
      style.bottom = `${verticalOffset}px`
      style.left = '50%'
      style.transform = 'translateX(-50%)'
      break
  }
  
  // 如果居中显示
  if (notification.center) {
    style.textAlign = 'center'
  }
  
  return style
}

// 创建便捷方法
const success = (options: NotificationOptions | string): string => {
  const normalizedOptions = typeof options === 'string' ? { message: options } : options
  return showNotification({ ...normalizedOptions, type: 'success' })
}

const warning = (options: NotificationOptions | string): string => {
  const normalizedOptions = typeof options === 'string' ? { message: options } : options
  return showNotification({ ...normalizedOptions, type: 'warning' })
}

const info = (options: NotificationOptions | string): string => {
  const normalizedOptions = typeof options === 'string' ? { message: options } : options
  return showNotification({ ...normalizedOptions, type: 'info' })
}

const error = (options: NotificationOptions | string): string => {
  const normalizedOptions = typeof options === 'string' ? { message: options } : options
  return showNotification({ ...normalizedOptions, type: 'error' })
}

const loading = (options: NotificationOptions | string): string => {
  const normalizedOptions = typeof options === 'string' ? { message: options } : options
  return showNotification({ 
    ...normalizedOptions, 
    type: 'loading',
    duration: null, // 加载通知默认不自动关闭
    showProgress: false
  })
}

// 更新通知
const updateNotification = (id: string, options: Partial<NotificationOptions>) => {
  const index = notifications.value.findIndex(n => n.id === id)
  if (index === -1) return
  
  // 更新通知
  notifications.value[index] = { ...notifications.value[index], ...options }
}

// 通知服务
const notificationService = {
  // 显示通知
  show: showNotification,
  
  // 便捷方法
  success,
  warning,
  info,
  error,
  loading,
  
  // 关闭通知
  close: closeNotification,
  closeAll: closeAllNotifications,
  closeByType: closeNotificationsByType,
  
  // 更新通知
  update: updateNotification,
  
  // 获取通知列表
  getNotifications: () => [...notifications.value]
}

// 暴露给全局
if (typeof window !== 'undefined') {
  // @ts-ignore
  window.$notification = notificationService
}

// 暴露给父组件
defineExpose(notificationService)

// 清理函数
onUnmounted(() => {
  // 清除所有定时器
  timers.value.forEach((timerId) => {
    clearTimeout(timerId)
  })
  timers.value.clear()
  
  pauseTimers.value.forEach((timerId) => {
    clearTimeout(timerId)
  })
  pauseTimers.value.clear()
  
  // 移除事件监听器
  removeClickOutsideListener()
  
  // 恢复指针事件
  document.body.style.pointerEvents = ''
})
</script>

<style scoped>
.notification-container {
  position: fixed;
  z-index: 9999;
  width: 330px;
  max-width: 90vw;
}

/* 位置样式 */
.notification-container.position-top-right {
  top: 0;
  right: 0;
}

.notification-container.position-top-left {
  top: 0;
  left: 0;
}

.notification-container.position-bottom-right {
  bottom: 0;
  right: 0;
}

.notification-container.position-bottom-left {
  bottom: 0;
  left: 0;
}

.notification-container.position-top-center {
  top: 0;
  left: 50%;
  transform: translateX(-50%);
}

.notification-container.position-bottom-center {
  bottom: 0;
  left: 50%;
  transform: translateX(-50%);
}

.notification-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding: 16px;
}

.notification-item {
  display: flex;
  align-items: flex-start;
  background: #ffffff;
  border-radius: 8px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  padding: 16px;
  transition: all 0.3s ease;
  word-break: break-word;
  position: relative;
  overflow: hidden;
  max-height: 300px;
  animation: slideIn 0.3s ease-out;
}

/* 类型样式 */
.notification-success {
  border-left: 4px solid #67c23a;
}

.notification-warning {
  border-left: 4px solid #e6a23c;
}

.notification-info {
  border-left: 4px solid #409eff;
}

.notification-error {
  border-left: 4px solid #f56c6c;
}

.notification-loading {
  border-left: 4px solid #409eff;
}

/* 图标样式 */
.notification-icon {
  margin-right: 12px;
  flex-shrink: 0;
}

.notification-success .notification-icon {
  color: #67c23a;
}

.notification-warning .notification-icon {
  color: #e6a23c;
}

.notification-info .notification-icon {
  color: #409eff;
}

.notification-error .notification-icon {
  color: #f56c6c;
}

.notification-loading .notification-icon {
  color: #409eff;
}

/* 内容样式 */
.notification-content {
  flex: 1;
  min-width: 0;
}

.notification-title {
  font-size: 16px;
  font-weight: 600;
  margin-bottom: 8px;
  color: #303133;
}

.notification-message {
  font-size: 14px;
  color: #606266;
  line-height: 1.5;
}

/* 操作按钮样式 */
.notification-actions {
  display: flex;
  gap: 8px;
  margin-top: 12px;
  flex-wrap: wrap;
}

.notification-action-btn {
  padding: 4px 12px;
  font-size: 12px;
}

/* 关闭按钮样式 */
.notification-close-btn {
  position: absolute;
  top: 12px;
  right: 12px;
  width: 20px;
  height: 20px;
  border: none;
  background: transparent;
  color: #909399;
  cursor: pointer;
  border-radius: 4px;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s ease;
}

.notification-close-btn:hover {
  background-color: #f0f2f5;
  color: #606266;
}

/* 进度条样式 */
.notification-progress {
  margin-top: 12px;
}

.notification-progress-bar {
  height: 2px !important;
}

/* 动画 */
@keyframes slideIn {
  from {
    opacity: 0;
    transform: translateX(100%);
  }
  to {
    opacity: 1;
    transform: translateX(0);
  }
}

/* 左侧进入动画 */
.notification-container.position-top-left .notification-enter-active,
.notification-container.position-bottom-left .notification-enter-active {
  animation-name: slideInLeft;
}

@keyframes slideInLeft {
  from {
    opacity: 0;
    transform: translateX(-100%);
  }
  to {
    opacity: 1;
    transform: translateX(0);
  }
}

/* 顶部中心进入动画 */
.notification-container.position-top-center .notification-enter-active {
  animation-name: slideInTop;
}

@keyframes slideInTop {
  from {
    opacity: 0;
    transform: translate(-50%, -100%);
  }
  to {
    opacity: 1;
    transform: translate(-50%, 0);
  }
}

/* 底部中心进入动画 */
.notification-container.position-bottom-center .notification-enter-active {
  animation-name: slideInBottom;
}

@keyframes slideInBottom {
  from {
    opacity: 0;
    transform: translate(-50%, 100%);
  }
  to {
    opacity: 1;
    transform: translate(-50%, 0);
  }
}

/* 退出动画 */
.notification-leave-active {
  animation: slideOut 0.3s ease-in;
}

@keyframes slideOut {
  from {
    opacity: 1;
    transform: translateX(0);
  }
  to {
    opacity: 0;
    transform: translateX(100%);
  }
}

/* 左侧退出动画 */
.notification-container.position-top-left .notification-leave-active,
.notification-container.position-bottom-left .notification-leave-active {
  animation-name: slideOutLeft;
}

@keyframes slideOutLeft {
  from {
    opacity: 1;
    transform: translateX(0);
  }
  to {
    opacity: 0;
    transform: translateX(-100%);
  }
}

/* 顶部中心退出动画 */
.notification-container.position-top-center .notification-leave-active {
  animation-name: slideOutTop;
}

@keyframes slideOutTop {
  from {
    opacity: 1;
    transform: translate(-50%, 0);
  }
  to {
    opacity: 0;
    transform: translate(-50%, -100%);
  }
}

/* 底部中心退出动画 */
.notification-container.position-bottom-center .notification-leave-active {
  animation-name: slideOutBottom;
}

@keyframes slideOutBottom {
  from {
    opacity: 1;
    transform: translate(-50%, 0);
  }
  to {
    opacity: 0;
    transform: translate(-50%, 100%);
  }
}

/* 移动设备响应式 */
@media (max-width: 768px) {
  .notification-container {
    width: 90vw;
    max-width: 90vw;
    left: 50% !important;
    transform: translateX(-50%) !important;
  }
  
  .notification-item {
    max-height: 250px;
    overflow-y: auto;
  }
}

/* 深色模式支持 */
@media (prefers-color-scheme: dark) {
  .notification-item {
    background: #1f1f1f;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
  }
  
  .notification-title {
    color: #ffffff;
  }
  
  .notification-message {
    color: #cccccc;
  }
  
  .notification-close-btn:hover {
    background-color: #333333;
  }
}

/* 高对比度模式支持 */
@media (prefers-contrast: high) {
  .notification-item {
    border: 2px solid;
  }
  
  .notification-success {
    border-color: #67c23a;
  }
  
  .notification-warning {
    border-color: #e6a23c;
  }
  
  .notification-info {
    border-color: #409eff;
  }
  
  .notification-error {
    border-color: #f56c6c;
  }
}

/* 减少动画偏好设置支持 */
@media (prefers-reduced-motion: reduce) {
  .notification-enter-active,
  .notification-leave-active,
  .notification-item {
    animation: none !important;
    transition: none !important;
  }
}
</style>