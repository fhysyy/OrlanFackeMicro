<template>
  <div class="event-trigger">
    <div class="event-trigger-header">
      <h3>事件触发器</h3>
      <el-button type="primary" size="small" @click="showCreateEventDrawer = true">
        <el-icon><Plus /></el-icon> 创建事件
      </el-button>
    </div>
    
    <!-- 事件列表 -->
    <el-card shadow="never" class="event-list-card">
      <template #header>
        <div class="event-list-header">
          <span>可用事件</span>
          <el-input
            v-model="searchKeyword"
            placeholder="搜索事件..."
            clearable
            prefix-icon="el-icon-search"
            size="small"
          />
        </div>
      </template>
      
      <el-empty v-if="filteredEvents.length === 0" description="暂无可用事件" />
      
      <el-scrollbar v-else class="event-list-scrollbar">
        <el-tree
          ref="eventTreeRef"
          :data="filteredEvents"
          :props="eventTreeProps"
          node-key="id"
          default-expand-all
          @node-click="handleEventClick"
          class="event-tree"
        >
          <template #default="{ node, data }">
            <div class="tree-node-content">
              <span class="event-label">
                <el-tag
                  v-if="data.category"
                  size="small"
                  :class="`category-tag category-${data.category}`"
                >
                  {{ data.category }}
                </el-tag>
                {{ node.label }}
              </span>
              <div class="event-actions">
                <el-tooltip effect="dark" content="查看详情" placement="top">
                  <el-button
                    type="text"
                    size="small"
                    @click.stop="viewEventDetails(data)"
                  >
                    <el-icon><View /></el-icon>
                  </el-button>
                </el-tooltip>
                <el-tooltip effect="dark" content="测试触发" placement="top">
                  <el-button
                    type="primary"
                    size="small"
                    @click.stop="triggerEvent(data)"
                    :loading="triggeringEventId === data.id"
                  >
                    <el-icon><Send /></el-icon>
                  </el-button>
                </el-tooltip>
                <el-tooltip effect="dark" content="编辑" placement="top">
                  <el-button
                    type="text"
                    size="small"
                    @click.stop="editEvent(data)"
                  >
                    <el-icon><Edit /></el-icon>
                  </el-button>
                </el-tooltip>
                <el-tooltip effect="dark" content="删除" placement="top">
                  <el-button
                    type="text"
                    size="small"
                    @click.stop="deleteEvent(data)"
                    text-color="#f56c6c"
                  >
                    <el-icon><Delete /></el-icon>
                  </el-button>
                </el-tooltip>
              </div>
            </div>
          </template>
        </el-tree>
      </el-scrollbar>
    </el-card>
    
    <!-- 最近触发的事件 -->
    <el-card shadow="never" class="recent-events-card">
      <template #header>
        <div class="recent-events-header">
          <span>最近触发的事件</span>
          <el-button type="text" size="small" @click="clearRecentEvents">
            <el-icon><RefreshRight /></el-icon> 清空
          </el-button>
        </div>
      </template>
      
      <el-empty v-if="recentEvents.length === 0" description="暂无事件记录" />
      
      <el-scrollbar v-else class="recent-events-scrollbar">
        <el-timeline>
          <el-timeline-item
            v-for="event in recentEvents"
            :key="event.id"
            :timestamp="formatTime(event.timestamp)"
            :type="getEventTypeColor(event.type)"
          >
            <div class="timeline-content">
              <h4>{{ event.name }}</h4>
              <p class="event-id">ID: {{ event.id }}</p>
              <div v-if="event.data" class="event-data">
                <el-divider content-position="left">事件数据</el-divider>
                <pre class="data-json">{{ JSON.stringify(event.data, null, 2) }}</pre>
              </div>
              <div v-if="event.result" class="event-result">
                <el-divider content-position="left">执行结果</el-divider>
                <pre class="data-json" :class="{ 'success': event.result.success, 'error': !event.result.success }">
                  {{ JSON.stringify(event.result, null, 2) }}
                </pre>
              </div>
            </div>
          </el-timeline-item>
        </el-timeline>
      </el-scrollbar>
    </el-card>
    
    <!-- 创建/编辑事件抽屉 -->
    <el-drawer
      v-model="showCreateEventDrawer"
      :title="isEditing ? '编辑事件' : '创建新事件'"
      size="500px"
    >
      <el-form
        ref="eventFormRef"
        :model="currentEvent"
        :rules="eventRules"
        label-width="100px"
        class="event-form"
      >
        <el-form-item label="事件名称" prop="name">
          <el-input v-model="currentEvent.name" placeholder="请输入事件名称" />
        </el-form-item>
        
        <el-form-item label="事件ID" prop="id">
          <el-input
            v-model="currentEvent.id"
            placeholder="请输入唯一的事件ID"
            :disabled="isEditing"
          />
        </el-form-item>
        
        <el-form-item label="事件类别" prop="category">
          <el-select v-model="currentEvent.category" placeholder="请选择事件类别">
            <el-option label="系统" value="system" />
            <el-option label="用户" value="user" />
            <el-option label="组件" value="component" />
            <el-option label="自定义" value="custom" />
          </el-select>
        </el-form-item>
        
        <el-form-item label="事件描述">
          <el-input
            v-model="currentEvent.description"
            type="textarea"
            rows="3"
            placeholder="请输入事件描述"
          />
        </el-form-item>
        
        <el-form-item label="默认数据">
          <el-input
            v-model="currentEvent.defaultData"
            type="textarea"
            rows="4"
            placeholder="请输入默认事件数据 (JSON格式)"
            monospaced
          />
          <div class="form-tips">
            <el-tag size="small" type="info">提示</el-tag>
            请输入有效的JSON格式数据
          </div>
        </el-form-item>
        
        <el-form-item>
          <el-button type="primary" @click="submitEventForm">
            {{ isEditing ? '更新' : '创建' }}
          </el-button>
          <el-button @click="cancelEventForm">取消</el-button>
        </el-form-item>
      </el-form>
    </el-drawer>
    
    <!-- 事件详情对话框 -->
    <el-dialog
      v-model="showEventDetailDialog"
      title="事件详情"
      width="600px"
    >
      <div v-if="selectedEvent" class="event-details">
        <el-descriptions border column="1">
          <el-descriptions-item label="事件名称">
            {{ selectedEvent.name }}
          </el-descriptions-item>
          <el-descriptions-item label="事件ID">
            <el-tag>{{ selectedEvent.id }}</el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="事件类别">
            <el-tag :class="`category-${selectedEvent.category}`">
              {{ selectedEvent.category }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="事件描述">
            {{ selectedEvent.description || '无描述' }}
          </el-descriptions-item>
          <el-descriptions-item label="默认数据">
            <pre v-if="selectedEvent.defaultData" class="data-json">
              {{ JSON.stringify(parseJSON(selectedEvent.defaultData), null, 2) }}
            </pre>
            <div v-else class="no-data">暂无默认数据</div>
          </el-descriptions-item>
          <el-descriptions-item label="创建时间">
            {{ formatTime(selectedEvent.createdAt) }}
          </el-descriptions-item>
          <el-descriptions-item label="更新时间">
            {{ formatTime(selectedEvent.updatedAt) }}
          </el-descriptions-item>
        </el-descriptions>
        
        <div class="event-usage-stats">
          <el-divider content-position="left">使用统计</el-divider>
          <el-row :gutter="20">
            <el-col :span="8">
              <div class="stat-item">
                <div class="stat-value">{{ eventUsageStats.totalCalls }}</div>
                <div class="stat-label">总调用次数</div>
              </div>
            </el-col>
            <el-col :span="8">
              <div class="stat-item">
                <div class="stat-value">{{ eventUsageStats.successCalls }}</div>
                <div class="stat-label">成功次数</div>
              </div>
            </el-col>
            <el-col :span="8">
              <div class="stat-item">
                <div class="stat-value">{{ eventUsageStats.failCalls }}</div>
                <div class="stat-label">失败次数</div>
              </div>
            </el-col>
          </el-row>
        </div>
      </div>
      
      <template #footer>
        <el-button @click="showEventDetailDialog = false">关闭</el-button>
        <el-button type="primary" @click="triggerEvent(selectedEvent)">
          触发事件
        </el-button>
      </template>
    </el-dialog>
    
    <!-- 事件触发对话框 -->
    <el-dialog
      v-model="showTriggerDialog"
      title="触发事件"
      width="500px"
    >
      <div v-if="triggerEventConfig" class="trigger-dialog">
        <h4>{{ triggerEventConfig.name }}</h4>
        <p class="event-id">ID: {{ triggerEventConfig.id }}</p>
        
        <el-form-item label="事件数据">
          <el-input
            v-model="triggerEventData"
            type="textarea"
            rows="6"
            placeholder="请输入事件数据 (JSON格式)"
            monospaced
          />
          <div class="form-tips">
            <el-tag size="small" type="info">提示</el-tag>
            输入有效的JSON格式数据，留空则使用默认数据
          </div>
        </el-form-item>
        
        <div v-if="triggerResult" class="trigger-result">
          <el-divider content-position="left">触发结果</el-divider>
          <pre
            class="data-json"
            :class="{ 'success': triggerResult.success, 'error': !triggerResult.success }"
          >
            {{ JSON.stringify(triggerResult, null, 2) }}
          </pre>
        </div>
      </div>
      
      <template #footer>
        <el-button @click="showTriggerDialog = false">关闭</el-button>
        <el-button
          type="primary"
          @click="confirmTriggerEvent"
          :loading="isTriggering"
        >
          确认触发
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, nextTick } from 'vue'
import { ElMessage } from 'element-plus'
import {
  Plus,
  View,
  Send,
  Edit,
  Delete,
  RefreshRight
} from '@element-plus/icons-vue'
import type { FormInstance, FormRules, ElTreeRef } from 'element-plus'
import type { EventConfig, ActionConfig } from '@/types/event'
import { eventBusService, useEventBus } from '@/services/eventBusService'
import { validateEventConfig } from '@/utils/eventUtils'

// 类型定义
interface EventItem extends EventConfig {
  createdAt?: number
  updatedAt?: number
}

interface RecentEvent {
  id: string
  name: string
  type: string
  timestamp: number
  data: any
  result?: {
    success: boolean
    data?: any
    error?: string
  }
}

interface EventUsageStats {
  totalCalls: number
  successCalls: number
  failCalls: number
}

// 响应式数据
const searchKeyword = ref('')
const showCreateEventDrawer = ref(false)
const showEventDetailDialog = ref(false)
const showTriggerDialog = ref(false)
const triggeringEventId = ref<string | null>(null)
const isEditing = ref(false)
const isTriggering = ref(false)
const triggerResult = ref<any>(null)
const triggerEventConfig = ref<EventItem | null>(null)
const triggerEventData = ref('')
const selectedEvent = ref<EventItem | null>(null)
const recentEvents = ref<RecentEvent[]>([])
const eventUsageStats = ref<EventUsageStats>({
  totalCalls: 0,
  successCalls: 0,
  failCalls: 0
})

// 组件引用
const eventFormRef = ref<FormInstance>()
const eventTreeRef = ref<ElTreeRef>()

// 表单数据
const currentEvent = reactive<EventItem>({
  id: '',
  name: '',
  category: 'custom',
  description: '',
  defaultData: '{}',
  handlers: [],
  actions: [],
  filters: [],
  transformers: [],
  enabled: true
})

// 表单验证规则
const eventRules = reactive<FormRules>({
  name: [
    { required: true, message: '请输入事件名称', trigger: 'blur' },
    { min: 2, max: 50, message: '名称长度应在 2 到 50 个字符之间', trigger: 'blur' }
  ],
  id: [
    { required: true, message: '请输入事件ID', trigger: 'blur' },
    {
      pattern: /^[a-zA-Z][a-zA-Z0-9_\-]*$/,
      message: 'ID必须以字母开头，只能包含字母、数字、下划线和连字符',
      trigger: 'blur'
    }
  ]
})

// 事件树配置
const eventTreeProps = {
  label: 'name',
  children: 'children',
  isLeaf: (data: any) => !data.children || data.children.length === 0
}

// 计算属性
const filteredEvents = computed(() => {
  if (!searchKeyword.value) {
    return groupedEvents.value
  }
  
  const keyword = searchKeyword.value.toLowerCase()
  const filtered = []
  
  // 递归过滤事件树
  const filterTree = (tree: any[]) => {
    return tree.map(node => {
      const hasMatch = 
        node.name.toLowerCase().includes(keyword) ||
        (node.description && node.description.toLowerCase().includes(keyword)) ||
        node.id.toLowerCase().includes(keyword)
      
      // 过滤子节点
      const filteredChildren = node.children ? filterTree(node.children) : []
      
      // 如果节点匹配或有匹配的子节点，则包含该节点
      if (hasMatch || filteredChildren.length > 0) {
        return {
          ...node,
          children: filteredChildren
        }
      }
      
      return null
    }).filter(Boolean)
  }
  
  return filterTree(groupedEvents.value)
})

// 按类别分组的事件
const groupedEvents = computed(() => {
  const groups: Record<string, EventItem[]> = {}
  
  // 初始化默认分组
  eventList.value.forEach(event => {
    const category = event.category || 'custom'
    if (!groups[category]) {
      groups[category] = []
    }
    groups[category].push(event)
  })
  
  // 构建树结构
  const tree: any[] = []
  Object.entries(groups).forEach(([category, events]) => {
    const categoryName = getCategoryName(category)
    tree.push({
      id: `category_${category}`,
      name: `${categoryName} (${events.length})`,
      category: 'group',
      children: events.map(event => ({
        ...event,
        children: []
      }))
    })
  })
  
  return tree
})

// 事件列表
const eventList = ref<EventItem[]>([])

// 方法
const getCategoryName = (category: string): string => {
  const categoryMap: Record<string, string> = {
    system: '系统事件',
    user: '用户事件',
    component: '组件事件',
    custom: '自定义事件',
    group: '事件分组'
  }
  return categoryMap[category] || '其他'
}

const getEventTypeColor = (type: string): string => {
  const colorMap: Record<string, string> = {
    system: 'primary',
    user: 'success',
    component: 'warning',
    custom: 'info'
  }
  return colorMap[type] || 'info'
}

const formatTime = (timestamp?: number): string => {
  if (!timestamp) return '未知'
  return new Date(timestamp).toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit'
  })
}

const parseJSON = (jsonStr: string): any => {
  try {
    return JSON.parse(jsonStr)
  } catch (error) {
    return jsonStr
  }
}

const validateJSON = (jsonStr: string): boolean => {
  if (!jsonStr) return true
  try {
    JSON.parse(jsonStr)
    return true
  } catch (error) {
    return false
  }
}

const handleEventClick = (data: EventItem) => {
  if (data.category === 'group') return // 忽略分组节点
  viewEventDetails(data)
}

const viewEventDetails = (event: EventItem) => {
  selectedEvent.value = { ...event }
  
  // 模拟加载使用统计数据
  eventUsageStats.value = {
    totalCalls: Math.floor(Math.random() * 100),
    successCalls: Math.floor(Math.random() * 80),
    failCalls: Math.floor(Math.random() * 20)
  }
  
  showEventDetailDialog.value = true
}

const triggerEvent = async (event: EventItem) => {
  triggerEventConfig.value = { ...event }
  triggerEventData.value = event.defaultData || '{}'
  triggerResult.value = null
  showTriggerDialog.value = true
}

const confirmTriggerEvent = async () => {
  if (!triggerEventConfig.value) return
  
  // 验证JSON格式
  if (triggerEventData.value && !validateJSON(triggerEventData.value)) {
    ElMessage.error('事件数据格式错误，请输入有效的JSON')
    return
  }
  
  try {
    isTriggering.value = true
    triggeringEventId.value = triggerEventConfig.value.id
    
    // 解析事件数据
    const eventData = triggerEventData.value ? JSON.parse(triggerEventData.value) : {}
    
    // 使用事件总线触发事件
    const { eventBus, createAutoContext } = useEventBus()
    const context = createAutoContext()
    
    // 触发事件并获取结果
    const result = await eventBus.emit(
      triggerEventConfig.value.name,
      eventData,
      context
    )
    
    // 记录触发结果
    triggerResult.value = {
      success: true,
      data: result
    }
    
    // 添加到最近触发的事件列表
    addToRecentEvents(triggerEventConfig.value, eventData, triggerResult.value)
    
    ElMessage.success('事件触发成功')
  } catch (error: any) {
    // 记录错误结果
    triggerResult.value = {
      success: false,
      error: error.message || String(error)
    }
    
    // 添加到最近触发的事件列表
    if (triggerEventConfig.value) {
      addToRecentEvents(
        triggerEventConfig.value,
        triggerEventData.value ? JSON.parse(triggerEventData.value) : {},
        triggerResult.value
      )
    }
    
    ElMessage.error(`事件触发失败: ${error.message || String(error)}`)
  } finally {
    isTriggering.value = false
    triggeringEventId.value = null
  }
}

const addToRecentEvents = (event: EventItem, data: any, result?: any) => {
  const recentEvent: RecentEvent = {
    id: `trigger_${Date.now()}`,
    name: event.name,
    type: event.category || 'custom',
    timestamp: Date.now(),
    data
  }
  
  if (result) {
    recentEvent.result = result
  }
  
  // 添加到列表开头
  recentEvents.value.unshift(recentEvent)
  
  // 限制最多显示20条记录
  if (recentEvents.value.length > 20) {
    recentEvents.value = recentEvents.value.slice(0, 20)
  }
}

const editEvent = (event: EventItem) => {
  Object.assign(currentEvent, { ...event })
  isEditing.value = true
  showCreateEventDrawer.value = true
}

const deleteEvent = (event: EventItem) => {
  ElMessageBox.confirm(`确定要删除事件「${event.name}」吗？此操作不可撤销。`, '确认删除', {
    confirmButtonText: '确定',
    cancelButtonText: '取消',
    type: 'warning'
  }).then(() => {
    // 从列表中移除事件
    const index = eventList.value.findIndex(item => item.id === event.id)
    if (index !== -1) {
      eventList.value.splice(index, 1)
      ElMessage.success('事件删除成功')
    }
  }).catch(() => {
    // 取消删除
  })
}

const submitEventForm = async () => {
  if (!eventFormRef.value) return
  
  await eventFormRef.value.validate((valid, fields) => {
    if (!valid) {
      console.log('表单验证失败:', fields)
      return false
    }
    
    // 验证默认数据的JSON格式
    if (currentEvent.defaultData && !validateJSON(currentEvent.defaultData)) {
      ElMessage.error('默认数据格式错误，请输入有效的JSON')
      return false
    }
    
    // 验证事件配置
    const validation = validateEventConfig(currentEvent)
    if (!validation.valid) {
      validation.errors.forEach(error => ElMessage.error(error))
      return false
    }
    
    // 检查ID是否已存在（仅创建时）
    if (!isEditing.value) {
      const exists = eventList.value.some(event => event.id === currentEvent.id)
      if (exists) {
        ElMessage.error('事件ID已存在，请使用其他ID')
        return false
      }
    }
    
    // 添加时间戳
    const now = Date.now()
    if (!isEditing.value) {
      currentEvent.createdAt = now
    }
    currentEvent.updatedAt = now
    
    // 更新或添加事件
    if (isEditing.value) {
      const index = eventList.value.findIndex(event => event.id === currentEvent.id)
      if (index !== -1) {
        eventList.value.splice(index, 1, { ...currentEvent })
        ElMessage.success('事件更新成功')
      }
    } else {
      eventList.value.push({ ...currentEvent })
      ElMessage.success('事件创建成功')
    }
    
    // 重置表单并关闭抽屉
    cancelEventForm()
    
    return true
  })
}

const cancelEventForm = () => {
  // 重置表单
  if (eventFormRef.value) {
    eventFormRef.value.resetFields()
  }
  
  // 重置数据
  Object.assign(currentEvent, {
    id: '',
    name: '',
    category: 'custom',
    description: '',
    defaultData: '{}',
    handlers: [],
    actions: [],
    filters: [],
    transformers: [],
    enabled: true
  })
  
  // 关闭抽屉并重置状态
  showCreateEventDrawer.value = false
  isEditing.value = false
}

const clearRecentEvents = () => {
  ElMessageBox.confirm('确定要清空所有事件记录吗？', '确认清空', {
    confirmButtonText: '确定',
    cancelButtonText: '取消',
    type: 'info'
  }).then(() => {
    recentEvents.value = []
    ElMessage.success('事件记录已清空')
  }).catch(() => {
    // 取消清空
  })
}

// 初始化示例事件数据
const initSampleEvents = () => {
  const sampleEvents: EventItem[] = [
    {
      id: 'system_page_loaded',
      name: '页面加载完成',
      category: 'system',
      description: '当页面完全加载时触发',
      defaultData: '{"pageId": "", "timestamp": 0}',
      createdAt: Date.now() - 86400000,
      updatedAt: Date.now() - 3600000,
      enabled: true
    },
    {
      id: 'user_login',
      name: '用户登录',
      category: 'user',
      description: '当用户成功登录时触发',
      defaultData: '{"userId": "", "username": "", "roles": []}',
      createdAt: Date.now() - 7200000,
      updatedAt: Date.now() - 3600000,
      enabled: true
    },
    {
      id: 'component_button_click',
      name: '按钮点击',
      category: 'component',
      description: '当按钮组件被点击时触发',
      defaultData: '{"componentId": "", "eventData": {}}',
      createdAt: Date.now() - 3600000,
      updatedAt: Date.now() - 1800000,
      enabled: true
    },
    {
      id: 'custom_data_updated',
      name: '数据更新',
      category: 'custom',
      description: '自定义数据更新事件',
      defaultData: '{"dataId": "", "newValue": null, "oldValue": null}',
      createdAt: Date.now() - 1800000,
      updatedAt: Date.now() - 900000,
      enabled: true
    }
  ]
  
  eventList.value = sampleEvents
}

// 生命周期钩子
onMounted(() => {
  // 初始化示例事件数据
  initSampleEvents()
  
  // 模拟添加一些最近触发的事件
  recentEvents.value = [
    {
      id: 'trigger_1',
      name: '用户登录',
      type: 'user',
      timestamp: Date.now() - 300000,
      data: { userId: 'user123', username: 'testuser', roles: ['admin', 'user'] },
      result: { success: true, data: { token: 'sample_token' } }
    },
    {
      id: 'trigger_2',
      name: '按钮点击',
      type: 'component',
      timestamp: Date.now() - 600000,
      data: { componentId: 'btn_submit', eventData: { clicked: true } },
      result: { success: true }
    }
  ]
})
</script>

<style scoped>
.event-trigger {
  display: flex;
  flex-direction: column;
  height: 100%;
  padding: 16px;
}

.event-trigger-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}

.event-trigger-header h3 {
  margin: 0;
}

.event-list-card,
.recent-events-card {
  margin-bottom: 16px;
}

.event-list-header,
.recent-events-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.event-list-scrollbar,
.recent-events-scrollbar {
  max-height: 300px;
}

.event-tree {
  --el-tree-indent: 20px;
}

.tree-node-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
  width: 100%;
}

.event-label {
  display: flex;
  align-items: center;
  flex: 1;
}

.category-tag {
  margin-right: 8px;
}

.category-system {
  background-color: var(--el-color-primary-light-9);
  border-color: var(--el-color-primary-light-7);
}

.category-user {
  background-color: var(--el-color-success-light-9);
  border-color: var(--el-color-success-light-7);
}

.category-component {
  background-color: var(--el-color-warning-light-9);
  border-color: var(--el-color-warning-light-7);
}

.category-custom {
  background-color: var(--el-color-info-light-9);
  border-color: var(--el-color-info-light-7);
}

.event-actions {
  display: flex;
  gap: 4px;
}

.event-form {
  padding: 8px 0;
}

.form-tips {
  margin-top: 8px;
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.data-json {
  background-color: var(--el-bg-color);
  padding: 12px;
  border-radius: 4px;
  font-family: var(--el-font-family-mono);
  font-size: 12px;
  line-height: 1.5;
  overflow-x: auto;
  margin: 0;
  white-space: pre-wrap;
  word-break: break-word;
  border: 1px solid var(--el-border-color);
}

.data-json.success {
  border-color: var(--el-color-success);
}

.data-json.error {
  border-color: var(--el-color-danger);
}

.event-details {
  padding: 8px 0;
}

.event-usage-stats {
  margin-top: 16px;
}

.stat-item {
  text-align: center;
  padding: 16px;
  background-color: var(--el-bg-color);
  border-radius: 4px;
}

.stat-value {
  font-size: 24px;
  font-weight: bold;
  color: var(--el-text-color-primary);
  margin-bottom: 8px;
}

.stat-label {
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.trigger-dialog {
  padding: 8px 0;
}

.event-id {
  color: var(--el-text-color-secondary);
  font-size: 12px;
  margin-bottom: 16px;
}

.no-data {
  text-align: center;
  color: var(--el-text-color-secondary);
  padding: 16px;
  background-color: var(--el-bg-color);
  border-radius: 4px;
}

.timeline-content {
  min-width: 0;
}

.timeline-content h4 {
  margin: 0 0 4px 0;
  font-size: 14px;
}

.timeline-content p {
  margin: 0 0 12px 0;
}
</style>