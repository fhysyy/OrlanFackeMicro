<template>
  <div class="performance-monitor">
    <el-card class="monitor-card">
      <template #header>
        <div class="card-header">
          <h3>性能监控面板</h3>
          <el-button size="small" type="info" @click="refreshData">刷新数据</el-button>
        </div>
      </template>

      <!-- 性能概览 -->
      <div class="monitor-section">
        <h4>性能概览</h4>
        <div class="stats-grid">
          <div class="stat-item">
            <span class="stat-label">平均渲染时间</span>
            <span class="stat-value">{{ performanceStats.averageRenderTime }}ms</span>
          </div>
          <div class="stat-item">
            <span class="stat-label">平均绑定更新时间</span>
            <span class="stat-value">{{ performanceStats.averageBindingUpdateTime }}ms</span>
          </div>
          <div class="stat-item">
            <span class="stat-label">慢渲染次数</span>
            <span class="stat-value">{{ performanceStats.slowRenderCount }}</span>
          </div>
          <div class="stat-item">
            <span class="stat-label">错误数量</span>
            <span class="stat-value error">{{ performanceStats.errorCount }}</span>
          </div>
        </div>
      </div>

      <!-- 资源使用情况 -->
      <div v-if="currentResourceUsage" class="monitor-section">
        <h4>资源使用情况</h4>
        <div class="resource-grid">
          <div class="resource-item">
            <span class="resource-label">内存使用</span>
            <div class="resource-details">
              <span class="resource-value">{{ currentResourceUsage.memoryUsage.used.toFixed(2) }}MB</span>
              <el-progress
                v-if="currentResourceUsage.memoryUsage.percentage"
                :percentage="currentResourceUsage.memoryUsage.percentage"
                :status="getMemoryStatus(currentResourceUsage.memoryUsage.percentage)"
                size="small"
                class="resource-progress"
              />
            </div>
          </div>
          <div class="resource-item">
            <span class="resource-label">DOM节点数量</span>
            <div class="resource-details">
              <span class="resource-value">{{ currentResourceUsage.domNodes }}</span>
              <el-progress
                :percentage="Math.min((currentResourceUsage.domNodes / 10000) * 100, 100)"
                :status="getDomStatus(currentResourceUsage.domNodes)"
                size="small"
                class="resource-progress"
              />
            </div>
          </div>
          <div class="resource-item">
            <span class="resource-label">事件监听器数量</span>
            <div class="resource-details">
              <span class="resource-value">{{ currentResourceUsage.eventListeners }}</span>
              <el-progress
                :percentage="Math.min((currentResourceUsage.eventListeners / 2000) * 100, 100)"
                :status="getEventListenerStatus(currentResourceUsage.eventListeners)"
                size="small"
                class="resource-progress"
              />
            </div>
          </div>
        </div>
      </div>

      <!-- 性能警告 -->
      <div v-if="performanceWarnings.length > 0" class="monitor-section">
        <h4>性能警告 <span class="warning-count">({{ performanceWarnings.length }})</span></h4>
        <el-table :data="performanceWarnings" border stripe size="small">
          <el-table-column prop="severity" label="级别" width="80">
            <template #default="scope">
              <el-tag :type="getSeverityType(scope.row.severity)">
                {{ scope.row.severity === 'high' ? '严重' : scope.row.severity === 'medium' ? '中等' : '低' }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="message" label="消息"></el-table-column>
            <el-table-column prop="timestamp" label="时间" width="160">
              <template #default="scope">
                {{ formatTime(scope.row.timestamp) }}</template>
              </el-table-column>
            <el-table-column label="操作" width="80">
              <template #default="scope">
                <el-button size="small" type="danger" @click="clearWarning(scope.row.id)">清除</el-button>
              </template>
            </el-table-column>
          </el-table>
      </div>

      <!-- 最近操作 -->
      <div class="monitor-section">
        <h4>最近操作</h4>
        <el-table :data="recentOperations" border stripe size="small">
          <el-table-column prop="operationName" label="操作名称"></el-table-column>
          <el-table-column prop="status" label="状态" width="80">
            <template #default="scope">
              <el-tag :type="getStatusType(scope.row.status)">
                {{ scope.row.status === 'completed' ? '成功' : scope.row.status === 'failed' ? '失败' : '进行中' }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="duration" label="耗时" width="80">
              <template #default="scope">
                {{ scope.row.duration ? scope.row.duration + 'ms' : '-' }}</template>
              </el-table-column>
            <el-table-column prop="timestamp" label="时间" width="160">
              <template #default="scope">
                {{ formatTime(scope.row.startTime) }}</template>
              </el-table-column>
          </el-table>
      </div>
    </el-card>
  </div>
</template>

<script lang="ts" setup>
import { ref, onMounted, onUnmounted, computed } from 'vue'
import { performanceService } from '@/services/performanceService'

// 响应式数据
const performanceStats = ref({
  averageRenderTime: 0,
  averageBindingUpdateTime: 0,
  slowRenderCount: 0,
  slowBindingCount: 0,
  resourceUsage: undefined,
  errorCount: 0
})

const currentResourceUsage = ref(undefined)
const performanceWarnings = ref([])
const recentOperations = ref([])
let updateTimer = null

// 刷新性能数据
const refreshData = () => {
  // 获取性能统计
  performanceStats.value = performanceService.getPerformanceStats()
  
  // 获取资源使用情况
  currentResourceUsage.value = performanceService.getCurrentResourceUsage()
  
  // 获取性能警告
  performanceWarnings.value = performanceService.getPerformanceWarnings()
  
  // 获取最近操作
  recentOperations.value = performanceService.getOperations().slice(0, 10)
}

// 清除特定警告
const clearWarning = (warningId) => {
  // 在实际实现中，需要在performanceService中添加清除单个警告的方法
  // 这里暂时过滤掉
  performanceWarnings.value = performanceWarnings.value.filter(warn => warn.id !== warningId)
}

// 获取内存状态
const getMemoryStatus = (percentage) => {
  if (percentage >= 80) return 'exception'
  if (percentage >= 60) return 'warning'
  return 'success'
}

// 获取DOM节点状态
const getDomStatus = (count) => {
  if (count >= 8000) return 'exception'
  if (count >= 5000) return 'warning'
  return 'success'
}

// 获取事件监听器状态
const getEventListenerStatus = (count) => {
  if (count >= 1500) return 'exception'
  if (count >= 1000) return 'warning'
  return 'success'
}

// 获取严重程度类型
const getSeverityType = (severity) => {
  switch (severity) {
    case 'high': return 'danger'
    case 'medium': return 'warning'
    case 'low': return 'info'
    default: return 'info'
  }
}

// 获取状态类型
const getStatusType = (status) => {
  switch (status) {
    case 'completed': return 'success'
    case 'failed': return 'danger'
    case 'pending': return 'warning'
    default: return 'info'
  }
}

// 格式化时间
const formatTime = (timestamp) => {
  const date = new Date(timestamp)
  return `${date.getHours().toString().padStart(2, '0')}:${date.getMinutes().toString().padStart(2, '0')}:${date.getSeconds().toString().padStart(2, '0')}`
}

// 定时更新数据
const startAutoUpdate = () => {
  updateTimer = setInterval(refreshData, 5000)
}

// 生命周期钩子
onMounted(() => {
  refreshData()
  startAutoUpdate()
})

onUnmounted(() => {
  if (updateTimer) {
    clearInterval(updateTimer)
  }
})
</script>

<style scoped>
.performance-monitor {
  padding: 20px;
}

.monitor-card {
  max-width: 1200px;
  margin: 0 auto;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.monitor-section {
  margin-bottom: 24px;
}

.monitor-section h4 {
  margin-bottom: 16px;
  color: #303133;
  font-weight: 500;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 16px;
  margin-bottom: 20px;
}

.stat-item {
  background: #f5f7fa;
  padding: 16px;
  border-radius: 8px;
  text-align: center;
}

.stat-label {
  display: block;
  font-size: 14px;
  color: #606266;
  margin-bottom: 8px;
}

.stat-value {
  display: block;
  font-size: 24px;
  font-weight: 600;
  color: #303133;
}

.stat-value.error {
  color: #f56c6c;
}

.resource-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 16px;
  margin-bottom: 20px;
}

.resource-item {
  background: #f5f7fa;
  padding: 16px;
  border-radius: 8px;
}

.resource-label {
  display: block;
  font-size: 14px;
  color: #606266;
  margin-bottom: 8px;
}

.resource-details {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.resource-value {
  font-size: 18px;
  font-weight: 600;
  color: #303133;
}

.resource-progress {
  margin-top: 4px;
}

.warning-count {
  color: #f56c6c;
  font-size: 14px;
  font-weight: normal;
}

:deep(.el-table) {
  margin-bottom: 16px;
}

@media (max-width: 768px) {
  .stats-grid,
  .resource-grid {
    grid-template-columns: 1fr;
  }
  
  .monitor-card {
    margin: 0;
  }
  
  .performance-monitor {
    padding: 10px;
  }
}
</style>