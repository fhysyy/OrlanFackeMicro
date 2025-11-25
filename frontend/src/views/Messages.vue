<template>
  <div class="messages-container">
    <div class="page-header">
      <h2>消息管理</h2>
      <el-button type="primary" @click="handleSend">发送消息</el-button>
    </div>

    <!-- 消息统计 -->
    <div class="message-stats">
      <el-row :gutter="20">
        <el-col :span="6">
          <el-statistic title="今日发送" :value="stats.todaySent" />
        </el-col>
        <el-col :span="6">
          <el-statistic title="待发送" :value="stats.pending" />
        </el-col>
        <el-col :span="6">
          <el-statistic title="发送成功" :value="stats.sent" />
        </el-col>
        <el-col :span="6">
          <el-statistic title="发送失败" :value="stats.failed" />
        </el-col>
      </el-row>
    </div>

    <!-- 搜索和筛选 -->
    <el-card class="search-card">
      <el-form :model="searchForm" inline>
        <el-form-item label="消息类型">
          <el-select v-model="searchForm.messageType" placeholder="请选择类型" clearable>
            <el-option label="系统消息" value="System" />
            <el-option label="通知" value="Notification" />
            <el-option label="提醒" value="Reminder" />
            <el-option label="营销" value="Marketing" />
          </el-select>
        </el-form-item>
        <el-form-item label="发送渠道">
          <el-select v-model="searchForm.channel" placeholder="请选择渠道" clearable>
            <el-option label="站内信" value="InApp" />
            <el-option label="邮件" value="Email" />
            <el-option label="短信" value="SMS" />
            <el-option label="推送" value="Push" />
          </el-select>
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="searchForm.status" placeholder="请选择状态" clearable>
            <el-option label="草稿" value="Draft" />
            <el-option label="待发送" value="Pending" />
            <el-option label="发送中" value="Sending" />
            <el-option label="已发送" value="Sent" />
            <el-option label="已送达" value="Delivered" />
            <el-option label="已读" value="Read" />
            <el-option label="失败" value="Failed" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleSearch">搜索</el-button>
          <el-button @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <!-- 消息列表 -->
      <el-card>
        <!-- 骨架屏 -->
        <div v-if="loading" class="skeleton-container">
          <div v-for="i in 6" :key="i" class="skeleton-message-item">
            <div class="skeleton-message-main">
              <EasySkeleton type="line" width="200px" height="20px" />
              <div class="skeleton-message-meta">
                <EasySkeleton type="rect" rectWidth="80px" rectHeight="24px" />
                <EasySkeleton type="rect" rectWidth="80px" rectHeight="24px" />
                <EasySkeleton type="rect" rectWidth="80px" rectHeight="24px" />
              </div>
              <EasySkeleton type="line" width="90%" height="16px" />
            </div>
            <div class="skeleton-message-actions">
              <EasySkeleton type="rect" rectWidth="60px" rectHeight="32px" />
              <EasySkeleton type="rect" rectWidth="60px" rectHeight="32px" />
            </div>
          </div>
        </div>

        <!-- 实际表格 -->
        <virtual-scroll-table
          v-else
          :data="messageList"
          :columns="tableColumns"
          :row-height="50"
          height="500px"
        >
          <template #messageType="{ row }">
            <el-tag size="small">{{ row.messageType }}</el-tag>
          </template>
          <template #channel="{ row }">
            <el-tag size="small" :type="getChannelType(row.channel)">{{ row.channel }}</el-tag>
          </template>
          <template #status="{ row }">
            <el-tag size="small" :type="getStatusType(row.status)">{{ row.status }}</el-tag>
          </template>
          <template #sentAt="{ row }">
            {{ formatDate(row.sentAt) }}
          </template>
          <template #operation="{ row }">
            <el-button size="small" @click="handleView(row)">查看</el-button>
            <el-button size="small" type="danger" @click="handleCancel(row)" v-if="row.status === 'Pending'">取消</el-button>
          </template>
        </virtual-scroll-table>

        <!-- 分页 -->
        <div class="pagination-container">
          <el-pagination
            v-model:current-page="pagination.current"
            v-model:page-size="pagination.size"
            :total="pagination.total"
            :page-sizes="[10, 20, 50, 100]"
            layout="total, sizes, prev, pager, next, jumper"
            @size-change="handleSizeChange"
            @current-change="handleCurrentChange"
          />
        </div>
      </el-card>

    <!-- 消息详情对话框 -->
    <el-dialog v-model="detailVisible" title="消息详情" width="600px">
      <el-descriptions :column="2" border>
        <el-descriptions-item label="消息ID">{{ currentMessage?.id }}</el-descriptions-item>
        <el-descriptions-item label="标题">{{ currentMessage?.title }}</el-descriptions-item>
        <el-descriptions-item label="类型">{{ currentMessage?.messageType }}</el-descriptions-item>
        <el-descriptions-item label="渠道">{{ currentMessage?.channel }}</el-descriptions-item>
        <el-descriptions-item label="状态">{{ currentMessage?.status }}</el-descriptions-item>
        <el-descriptions-item label="发送者">{{ currentMessage?.senderId }}</el-descriptions-item>
        <el-descriptions-item label="接收者">{{ currentMessage?.receiverId }}</el-descriptions-item>
        <el-descriptions-item label="发送时间">{{ formatDate(currentMessage?.sentAt) }}</el-descriptions-item>
        <el-descriptions-item label="创建时间">{{ formatDate(currentMessage?.createdAt) }}</el-descriptions-item>
      </el-descriptions>
      
      <el-divider>消息内容</el-divider>
      <div class="message-content">
        <pre>{{ currentMessage?.content }}</pre>
      </div>
      
      <template #footer>
        <el-button @click="detailVisible = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { Message, MessageType, MessageChannel, MessageStatus } from '@/types/api'
import VirtualScrollTable from '@/components/VirtualScrollTable.vue'
import EasySkeleton from '@/components/EasySkeleton.vue'

const loading = ref(false)
const detailVisible = ref(false)
const currentMessage = ref<Message | null>(null)

const searchForm = reactive({
  messageType: '' as MessageType | '',
  channel: '' as MessageChannel | '',
  status: '' as MessageStatus | ''
})

const pagination = reactive({
  current: 1,
  size: 10,
  total: 0
})

const stats = reactive({
  todaySent: 156,
  pending: 23,
  sent: 1234,
  failed: 12
})

const messageList = ref<Message[]>([])

// 表格列配置
const tableColumns = [
  { prop: 'id', label: 'ID', width: 80 },
  { prop: 'title', label: '标题', showOverflowTooltip: true },
  { prop: 'messageType', label: '类型', width: 100 },
  { prop: 'channel', label: '渠道', width: 100 },
  { prop: 'status', label: '状态', width: 100 },
  { prop: 'senderId', label: '发送者', width: 120 },
  { prop: 'receiverId', label: '接收者', width: 120 },
  { prop: 'sentAt', label: '发送时间', width: 180 },
  { prop: 'operation', label: '操作', width: 150, slot: 'operation' }
]

// 获取渠道标签类型
const getChannelType = (channel: MessageChannel) => {
  const types = {
    InApp: '',
    Email: 'success',
    SMS: 'warning',
    Push: 'info'
  }
  return types[channel] || ''
}

// 获取状态标签类型
const getStatusType = (status: MessageStatus) => {
  const types = {
    Draft: 'info',
    Pending: 'warning',
    Sending: 'primary',
    Sent: 'success',
    Delivered: 'success',
    Read: 'success',
    Failed: 'danger'
  }
  return types[status] || ''
}

// 格式化日期
const formatDate = (date?: string) => {
  return date ? new Date(date).toLocaleString('zh-CN') : '-'
}

// 获取消息列表
const fetchMessages = async () => {
  loading.value = true
  try {
    // 模拟API调用
    await new Promise(resolve => setTimeout(resolve, 1000))
    
    messageList.value = [
      {
        id: '1',
        senderId: 'system',
        receiverId: 'user1',
        title: '欢迎加入 FakeMicro',
        content: '欢迎您加入 FakeMicro 平台！',
        messageType: 'System',
        channel: 'Email',
        status: 'Sent',
        sentAt: '2024-01-15T10:30:00Z',
        createdAt: '2024-01-15T10:00:00Z',
        updatedAt: '2024-01-15T10:30:00Z',
        retryCount: 0
      },
      {
        id: '2',
        senderId: 'admin',
        receiverId: 'user2',
        title: '系统维护通知',
        content: '系统将于今晚进行维护，请提前保存工作。',
        messageType: 'Notification',
        channel: 'InApp',
        status: 'Pending',
        sentAt: undefined,
        createdAt: '2024-01-15T14:00:00Z',
        updatedAt: '2024-01-15T14:00:00Z',
        retryCount: 0
      }
    ]
    
    pagination.total = messageList.value.length
  } catch (error) {
    ElMessage.error('获取消息列表失败')
  } finally {
    loading.value = false
  }
}

// 搜索
const handleSearch = () => {
  pagination.current = 1
  fetchMessages()
}

// 重置搜索
const handleReset = () => {
  Object.assign(searchForm, {
    messageType: '',
    channel: '',
    status: ''
  })
  pagination.current = 1
  fetchMessages()
}

// 分页大小改变
const handleSizeChange = (size: number) => {
  pagination.size = size
  pagination.current = 1
  fetchMessages()
}

// 当前页改变
const handleCurrentChange = (current: number) => {
  pagination.current = current
  fetchMessages()
}

// 发送消息
const handleSend = () => {
  ElMessage.info('发送消息功能开发中')
}

// 查看消息详情
const handleView = (message: Message) => {
  currentMessage.value = message
  detailVisible.value = true
}

// 取消发送
const handleCancel = async (message: Message) => {
  try {
    await ElMessageBox.confirm('确定要取消发送这条消息吗？', '提示', {
      type: 'warning'
    })
    
    // 模拟取消操作
    await new Promise(resolve => setTimeout(resolve, 500))
    ElMessage.success('取消成功')
    fetchMessages()
  } catch {
    // 用户取消操作
  }
}

onMounted(() => {
  fetchMessages()
})
</script>

<style scoped lang="scss">
.messages-container {
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
}

.message-stats {
  margin-bottom: 20px;
  
  .el-col {
    margin-bottom: 20px;
  }
}

.search-card {
  margin-bottom: 20px;
}

.pagination-container {
  display: flex;
  justify-content: flex-end;
  margin-top: 20px;
}

.message-content {
  background: var(--el-bg-color-page);
  padding: 15px;
  border-radius: 4px;
  max-height: 200px;
  overflow: auto;
  
  pre {
    margin: 0;
    white-space: pre-wrap;
    word-wrap: break-word;
  }
}

/* 骨架屏样式 */
.skeleton-container {
  background: #fff;
  border-radius: 8px;
  padding: 16px;
}

.skeleton-message-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 0;
  border-bottom: 1px solid #f0f0f0;
}

.skeleton-message-item:last-child {
  border-bottom: none;
}

.skeleton-message-main {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.skeleton-message-meta {
  display: flex;
  gap: 12px;
  margin: 8px 0;
}

.skeleton-message-actions {
  display: flex;
  gap: 8px;
}

.skeleton-line,
.skeleton-rect {
  background: linear-gradient(90deg, #f0f0f0 25%, #f8f8f8 50%, #f0f0f0 75%);
  background-size: 200% 100%;
  animation: skeleton-loading 1.5s infinite;
  border-radius: 4px;
}

@keyframes skeleton-loading {
  0% {
    background-position: 200% 0;
  }
  100% {
    background-position: -200% 0;
  }
}
</style>