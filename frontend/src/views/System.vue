<template>
  <div class="system-container">
    <div class="page-header">
      <h2>系统监控</h2>
      <div class="header-actions">
        <el-button @click="refreshStats">
          刷新
        </el-button>
        <el-button
          type="primary"
          @click="exportReport"
        >
          导出报告
        </el-button>
      </div>
    </div>

    <!-- 系统概览 -->
    <el-row
      :gutter="20"
      class="system-overview"
    >
      <el-col :span="6">
        <el-card class="metric-card">
          <div class="metric-content">
            <div class="metric-icon cpu-icon">
              <el-icon><Cpu /></el-icon>
            </div>
            <div class="metric-info">
              <div class="metric-value">
                {{ systemStats.cpuUsage }}%
              </div>
              <div class="metric-label">
                CPU使用率
              </div>
            </div>
          </div>
          <el-progress 
            :percentage="systemStats.cpuUsage" 
            :color="getProgressColor(systemStats.cpuUsage)"
            :show-text="false"
          />
        </el-card>
      </el-col>
      
      <el-col :span="6">
        <el-card class="metric-card">
          <div class="metric-content">
            <div class="metric-icon memory-icon">
              <el-icon><DataBoard /></el-icon>
            </div>
            <div class="metric-info">
              <div class="metric-value">
                {{ systemStats.memoryUsage }}%
              </div>
              <div class="metric-label">
                内存使用率
              </div>
            </div>
          </div>
          <el-progress 
            :percentage="systemStats.memoryUsage" 
            :color="getProgressColor(systemStats.memoryUsage)"
            :show-text="false"
          />
        </el-card>
      </el-col>
      
      <el-col :span="6">
        <el-card class="metric-card">
          <div class="metric-content">
            <div class="metric-icon disk-icon">
              <el-icon><HardDisk /></el-icon>
            </div>
            <div class="metric-info">
              <div class="metric-value">
                {{ systemStats.diskUsage }}%
              </div>
              <div class="metric-label">
                磁盘使用率
              </div>
            </div>
          </div>
          <el-progress 
            :percentage="systemStats.diskUsage" 
            :color="getProgressColor(systemStats.diskUsage)"
            :show-text="false"
          />
        </el-card>
      </el-col>
      
      <el-col :span="6">
        <el-card class="metric-card">
          <div class="metric-content">
            <div class="metric-icon uptime-icon">
              <el-icon><Clock /></el-icon>
            </div>
            <div class="metric-info">
              <div class="metric-value">
                {{ systemStats.uptime }}
              </div>
              <div class="metric-label">
                系统运行时间
              </div>
            </div>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 服务状态 -->
    <el-card
      class="services-card"
      header="服务状态"
    >
      <el-table
        v-loading="loading"
        :data="serviceStatus"
      >
        <el-table-column
          prop="name"
          label="服务名称"
        />
        <el-table-column
          prop="status"
          label="状态"
          width="100"
        >
          <template #default="{ row }">
            <el-tag :type="row.status === 'Running' ? 'success' : 'danger'">
              {{ row.status }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column
          prop="uptime"
          label="运行时间"
        />
        <el-table-column
          prop="cpu"
          label="CPU使用率"
          width="120"
        >
          <template #default="{ row }">
            <el-progress 
              :percentage="row.cpu" 
              :color="getProgressColor(row.cpu)"
              :show-text="false"
            />
            <span style="margin-left: 8px">{{ row.cpu }}%</span>
          </template>
        </el-table-column>
        <el-table-column
          prop="memory"
          label="内存使用"
          width="120"
        >
          <template #default="{ row }">
            <el-progress 
              :percentage="row.memory" 
              :color="getProgressColor(row.memory)"
              :show-text="false"
            />
            <span style="margin-left: 8px">{{ row.memory }}%</span>
          </template>
        </el-table-column>
        <el-table-column
          label="操作"
          width="120"
        >
          <template #default="{ row }">
            <el-button 
              size="small" 
              :disabled="row.status !== 'Running'"
              @click="restartService(row.name)"
            >
              重启
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- 性能图表 -->
    <el-row
      :gutter="20"
      class="charts-row"
    >
      <el-col :span="12">
        <el-card header="CPU使用率趋势">
          <div
            ref="cpuChartRef"
            style="height: 300px;"
          />
        </el-card>
      </el-col>
      <el-col :span="12">
        <el-card header="内存使用趋势">
          <div
            ref="memoryChartRef"
            style="height: 300px;"
          />
        </el-card>
      </el-col>
    </el-row>

    <!-- 系统日志 -->
    <el-card
      class="logs-card"
      header="系统日志"
    >
      <div class="logs-controls">
        <el-select
          v-model="logLevel"
          placeholder="日志级别"
          style="width: 120px"
        >
          <el-option
            label="全部"
            value="all"
          />
          <el-option
            label="信息"
            value="info"
          />
          <el-option
            label="警告"
            value="warn"
          />
          <el-option
            label="错误"
            value="error"
          />
        </el-select>
        <el-button @click="clearLogs">
          清空日志
        </el-button>
        <el-button
          type="primary"
          @click="refreshLogs"
        >
          刷新
        </el-button>
      </div>
      
      <div class="logs-content">
        <el-timeline>
          <el-timeline-item
            v-for="log in filteredLogs"
            :key="log.id"
            :timestamp="log.timestamp"
            :type="getLogType(log.level)"
          >
            <div class="log-item">
              <span
                class="log-level"
                :class="`level-${log.level}`"
              >{{ log.level }}</span>
              <span class="log-message">{{ log.message }}</span>
            </div>
          </el-timeline-item>
        </el-timeline>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, onUnmounted, computed } from 'vue'
import { ElMessage } from 'element-plus'
import echarts from '@/plugins/echarts'

const loading = ref(false)
const logLevel = ref('all')

const systemStats = reactive({
  cpuUsage: 0,
  memoryUsage: 0,
  diskUsage: 0,
  uptime: '0天0小时0分钟'
})

const serviceStatus = ref([
  {
    name: 'API Gateway',
    status: 'Running',
    uptime: '15天8小时',
    cpu: 23,
    memory: 45
  },
  {
    name: 'Orleans Silo',
    status: 'Running',
    uptime: '15天8小时',
    cpu: 18,
    memory: 32
  },
  {
    name: 'PostgreSQL',
    status: 'Running',
    uptime: '15天8小时',
    cpu: 12,
    memory: 28
  },
  {
    name: 'Redis',
    status: 'Running',
    uptime: '15天8小时',
    cpu: 8,
    memory: 15
  },
  {
    name: 'RabbitMQ',
    status: 'Running',
    uptime: '15天8小时',
    cpu: 6,
    memory: 22
  }
])

const systemLogs = ref([
  {
    id: 1,
    level: 'info',
    message: '系统启动完成',
    timestamp: '2024-01-15 14:30:00'
  },
  {
    id: 2,
    level: 'info',
    message: '数据库连接成功',
    timestamp: '2024-01-15 14:30:05'
  },
  {
    id: 3,
    level: 'warn',
    message: '检测到异常登录尝试',
    timestamp: '2024-01-15 14:35:20'
  },
  {
    id: 4,
    level: 'error',
    message: '邮件发送失败: 连接超时',
    timestamp: '2024-01-15 14:40:15'
  },
  {
    id: 5,
    level: 'info',
    message: '系统备份完成',
    timestamp: '2024-01-15 15:00:00'
  }
])

const cpuChartRef = ref<HTMLElement>()
const memoryChartRef = ref<HTMLElement>()
let cpuChart: echarts.ECharts | null = null
let memoryChart: echarts.ECharts | null = null

// 过滤日志
const filteredLogs = computed(() => {
  if (logLevel.value === 'all') return systemLogs.value
  return systemLogs.value.filter(log => log.level === logLevel.value)
})

// 获取进度条颜色
const getProgressColor = (percentage: number) => {
  if (percentage < 50) return '#67c23a'
  if (percentage < 80) return '#e6a23c'
  return '#f56c6c'
}

// 获取日志类型
const getLogType = (level: string) => {
  const types = {
    info: 'primary',
    warn: 'warning',
    error: 'danger'
  }
  return types[level] || 'info'
}

// 初始化图表
const initCharts = () => {
  if (cpuChartRef.value) {
    cpuChart = echarts.init(cpuChartRef.value)
    cpuChart.setOption({
      tooltip: {
        trigger: 'axis'
      },
      xAxis: {
        type: 'category',
        data: ['14:00', '14:15', '14:30', '14:45', '15:00', '15:15', '15:30']
      },
      yAxis: {
        type: 'value',
        max: 100
      },
      series: [{
        data: [23, 28, 25, 32, 29, 26, 24],
        type: 'line',
        smooth: true,
        areaStyle: {}
      }]
    })
  }

  if (memoryChartRef.value) {
    memoryChart = echarts.init(memoryChartRef.value)
    memoryChart.setOption({
      tooltip: {
        trigger: 'axis'
      },
      xAxis: {
        type: 'category',
        data: ['14:00', '14:15', '14:30', '14:45', '15:00', '15:15', '15:30']
      },
      yAxis: {
        type: 'value',
        max: 100
      },
      series: [{
        data: [45, 48, 52, 49, 47, 50, 46],
        type: 'line',
        smooth: true,
        areaStyle: {}
      }]
    })
  }
}

// 刷新统计数据
const refreshStats = async () => {
  loading.value = true
  try {
    // 模拟API调用
    await new Promise(resolve => setTimeout(resolve, 1000))
    
    // 更新系统统计
    Object.assign(systemStats, {
      cpuUsage: Math.floor(Math.random() * 30) + 10,
      memoryUsage: Math.floor(Math.random() * 40) + 20,
      diskUsage: Math.floor(Math.random() * 20) + 60,
      uptime: '15天8小时23分钟'
    })
    
    // 更新服务状态
    serviceStatus.value = serviceStatus.value.map(service => ({
      ...service,
      cpu: Math.floor(Math.random() * 20) + 5,
      memory: Math.floor(Math.random() * 30) + 10
    }))
    
    ElMessage.success('数据刷新成功')
  } catch (error) {
    ElMessage.error('数据刷新失败')
  } finally {
    loading.value = false
  }
}

// 重启服务
const restartService = async (serviceName: string) => {
  try {
    ElMessage.info(`正在重启服务: ${serviceName}`)
    // 模拟重启操作
    await new Promise(resolve => setTimeout(resolve, 2000))
    ElMessage.success(`服务 ${serviceName} 重启成功`)
    refreshStats()
  } catch (error) {
    ElMessage.error(`服务 ${serviceName} 重启失败`)
  }
}

// 导出报告
const exportReport = () => {
  ElMessage.info('导出功能开发中')
}

// 清空日志
const clearLogs = () => {
  systemLogs.value = []
  ElMessage.success('日志已清空')
}

// 刷新日志
const refreshLogs = () => {
  ElMessage.info('日志刷新成功')
}

// 定时刷新数据
let refreshInterval: number

onMounted(() => {
  refreshStats()
  setTimeout(() => {
    initCharts()
  }, 100)
  
  // 每30秒自动刷新一次
  refreshInterval = setInterval(refreshStats, 30000)
})

onUnmounted(() => {
  if (refreshInterval) {
    clearInterval(refreshInterval)
  }
  if (cpuChart) {
    cpuChart.dispose()
  }
  if (memoryChart) {
    memoryChart.dispose()
  }
})
</script>

<style scoped lang="scss">
.system-container {
  padding: 0;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
  
  h2 {
    margin: 0;
    color: var(--el-text-color-primary);
  }
  
  .header-actions {
    display: flex;
    gap: 10px;
  }
}

.system-overview {
  margin-bottom: 20px;
}

.metric-card {
  .metric-content {
    display: flex;
    align-items: center;
    margin-bottom: 15px;
  }
  
  .metric-icon {
    width: 50px;
    height: 50px;
    border-radius: 8px;
    display: flex;
    align-items: center;
    justify-content: center;
    margin-right: 15px;
    font-size: 24px;
    color: white;
    
    &.cpu-icon {
      background: #409eff;
    }
    
    &.memory-icon {
      background: #67c23a;
    }
    
    &.disk-icon {
      background: #e6a23c;
    }
    
    &.uptime-icon {
      background: #909399;
    }
  }
  
  .metric-value {
    font-size: 24px;
    font-weight: bold;
    color: var(--el-text-color-primary);
  }
  
  .metric-label {
    font-size: 14px;
    color: var(--el-text-color-regular);
  }
}

.services-card {
  margin-bottom: 20px;
}

.charts-row {
  margin-bottom: 20px;
}

.logs-card {
  .logs-controls {
    display: flex;
    gap: 10px;
    margin-bottom: 15px;
  }
  
  .logs-content {
    max-height: 400px;
    overflow: auto;
    
    .log-item {
      display: flex;
      align-items: center;
      gap: 10px;
      
      .log-level {
        padding: 2px 6px;
        border-radius: 3px;
        font-size: 12px;
        font-weight: bold;
        text-transform: uppercase;
        
        &.level-info {
          background: #e6f7ff;
          color: #1890ff;
        }
        
        &.level-warn {
          background: #fff7e6;
          color: #fa8c16;
        }
        
        &.level-error {
          background: #fff2f0;
          color: #ff4d4f;
        }
      }
      
      .log-message {
        flex: 1;
      }
    }
  }
}
</style>