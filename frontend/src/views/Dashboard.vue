<template>
  <div class="dashboard-container">
    <div class="dashboard-header">
      <h1>仪表板</h1>
      <p>欢迎回来，{{ authStore.user?.username }}！</p>
    </div>

    <!-- 统计卡片 -->
    <div class="stats-cards">
      <el-card class="stat-card">
        <div class="stat-content">
          <div class="stat-icon user-icon">
            <el-icon><User /></el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ stats.userCount || 0 }}</div>
            <div class="stat-label">总用户数</div>
          </div>
        </div>
      </el-card>

      <el-card class="stat-card">
        <div class="stat-content">
          <div class="stat-icon message-icon">
            <el-icon><Message /></el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ stats.messageCount || 0 }}</div>
            <div class="stat-label">总消息数</div>
          </div>
        </div>
      </el-card>

      <el-card class="stat-card">
        <div class="stat-content">
          <div class="stat-icon file-icon">
            <el-icon><Folder /></el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ stats.fileCount || 0 }}</div>
            <div class="stat-label">文件数量</div>
          </div>
        </div>
      </el-card>

      <el-card class="stat-card">
        <div class="stat-content">
          <div class="stat-icon system-icon">
            <el-icon><Monitor /></el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ stats.activeUsers || 0 }}</div>
            <div class="stat-label">活跃用户</div>
          </div>
        </div>
      </el-card>
    </div>

    <!-- 图表区域 -->
    <div class="charts-section">
      <el-row :gutter="20">
        <el-col :span="12">
          <el-card header="用户增长趋势">
            <div ref="userChartRef" style="height: 300px;"></div>
          </el-card>
        </el-col>
        <el-col :span="12">
          <el-card header="消息发送统计">
            <div ref="messageChartRef" style="height: 300px;"></div>
          </el-card>
        </el-col>
      </el-row>
    </div>

    <!-- 最近活动 -->
    <div class="recent-activities">
      <el-card header="最近活动">
        <el-timeline>
          <el-timeline-item
            v-for="activity in recentActivities"
            :key="activity.id"
            :timestamp="activity.timestamp"
            :type="activity.type"
          >
            {{ activity.content }}
          </el-timeline-item>
        </el-timeline>
      </el-card>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import echarts from '@/plugins/echarts'
import { Folder, Monitor, User, Message } from '@element-plus/icons-vue'
import type { SystemStats } from '@/types/api'
import { systemApi, activityApi } from '@/services/api'

const authStore = useAuthStore()

const stats = ref<SystemStats>({
  userCount: 0,
  activeUsers: 0,
  messageCount: 0,
  pendingMessages: 0,
  fileCount: 0,
  totalFileSize: 0,
  systemUptime: '',
  memoryUsage: 0,
  cpuUsage: 0
})

const userChartRef = ref<HTMLElement>()
const messageChartRef = ref<HTMLElement>()
let userChart: echarts.ECharts | null = null
let messageChart: echarts.ECharts | null = null

const recentActivities = ref([
  {
    id: 1,
    timestamp: '2024-01-15 14:30',
    type: 'primary',
    content: '新用户注册：testuser'
  },
  {
    id: 2,
    timestamp: '2024-01-15 13:45',
    type: 'success',
    content: '系统备份完成'
  },
  {
    id: 3,
    timestamp: '2024-01-15 12:20',
    type: 'warning',
    content: '检测到异常登录尝试'
  },
  {
    id: 4,
    timestamp: '2024-01-15 10:15',
    type: 'info',
    content: '系统性能优化完成'
  }
])

// 初始化图表
const initCharts = () => {
  if (userChartRef.value) {
    userChart = echarts.init(userChartRef.value)
    userChart.setOption({
      tooltip: {
        trigger: 'axis'
      },
      xAxis: {
        type: 'category',
        data: ['1月', '2月', '3月', '4月', '5月', '6月']
      },
      yAxis: {
        type: 'value'
      },
      series: [{
        data: [120, 200, 150, 80, 70, 110],
        type: 'line',
        smooth: true
      }]
    })
  }

  if (messageChartRef.value) {
    messageChart = echarts.init(messageChartRef.value)
    messageChart.setOption({
      tooltip: {
        trigger: 'axis'
      },
      legend: {
        data: ['邮件', '短信', '站内信']
      },
      xAxis: {
        type: 'category',
        data: ['周一', '周二', '周三', '周四', '周五', '周六', '周日']
      },
      yAxis: {
        type: 'value'
      },
      series: [
        {
          name: '邮件',
          type: 'bar',
          data: [320, 332, 301, 334, 390, 330, 320]
        },
        {
          name: '短信',
          type: 'bar',
          data: [220, 182, 191, 234, 290, 330, 310]
        },
        {
          name: '站内信',
          type: 'bar',
          data: [150, 232, 201, 154, 190, 330, 410]
        }
      ]
    })
  }
}

// 获取统计数据
const fetchStats = async () => {
  try {
    const response = await systemApi.getStats()
    if (response.data) {
      stats.value = response.data
    }
  } catch (error) {
    console.error('获取统计数据失败:', error)
    // 使用默认数据作为回退
    stats.value = {
      userCount: 0,
      activeUsers: 0,
      messageCount: 0,
      pendingMessages: 0,
      fileCount: 0,
      totalFileSize: 0,
      systemUptime: '未知',
      memoryUsage: 0,
      cpuUsage: 0
    }
  }
}

// 获取最近活动
const fetchRecentActivities = async () => {
  try {
    const response = await activityApi.getActivities({ page: 1, pageSize: 10 })
    if (response.data?.items) {
      recentActivities.value = response.data.items.map(activity => ({
        id: activity.id,
        timestamp: activity.timestamp,
        type: activity.type === 'Error' ? 'danger' : 
              activity.type === 'Create' ? 'success' : 
              activity.type === 'Update' ? 'warning' : 'primary',
        content: activity.details
      }))
    }
  } catch (error) {
    console.error('获取最近活动失败:', error)
  }
}

onMounted(async () => {
  await Promise.all([
    fetchStats(),
    fetchRecentActivities()
  ])
  setTimeout(() => {
    initCharts()
  }, 100)
})

onUnmounted(() => {
  if (userChart) {
    userChart.dispose()
  }
  if (messageChart) {
    messageChart.dispose()
  }
})
</script>

<style scoped lang="scss">
.dashboard-container {
  padding: 20px;
}

.dashboard-header {
  margin-bottom: 30px;
  
  h1 {
    margin: 0 0 10px 0;
    color: var(--el-text-color-primary);
  }
  
  p {
    margin: 0;
    color: var(--el-text-color-regular);
  }
}

.stats-cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 20px;
  margin-bottom: 30px;
}

.stat-card {
  .stat-content {
    display: flex;
    align-items: center;
  }
  
  .stat-icon {
    width: 60px;
    height: 60px;
    border-radius: 8px;
    display: flex;
    align-items: center;
    justify-content: center;
    margin-right: 15px;
    font-size: 24px;
    color: white;
    
    &.user-icon {
      background: #409eff;
    }
    
    &.message-icon {
      background: #67c23a;
    }
    
    &.file-icon {
      background: #e6a23c;
    }
    
    &.system-icon {
      background: #f56c6c;
    }
  }
  
  .stat-value {
    font-size: 24px;
    font-weight: bold;
    color: var(--el-text-color-primary);
  }
  
  .stat-label {
    font-size: 14px;
    color: var(--el-text-color-regular);
  }
}

.charts-section {
  margin-bottom: 30px;
}

.recent-activities {
  margin-bottom: 30px;
}
</style>