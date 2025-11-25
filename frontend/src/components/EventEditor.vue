<template>
  <div class="event-editor">
    <div class="editor-header">
      <h3>{{ title || '事件编辑器' }}</h3>
      <div class="header-actions">
        <el-button 
          type="primary" 
          size="small" 
          @click="addNewEvent"
          icon="el-icon-plus"
        >
          添加事件
        </el-button>
      </div>
    </div>
    
    <!-- 事件列表 -->
    <div class="event-list">
      <el-empty 
        v-if="events.length === 0" 
        description="暂无事件配置"
      />
      
      <div 
        v-for="(event, index) in events" 
        :key="`${event.name}-${index}`"
        class="event-item"
        :class="{ 'selected': selectedEventIndex === index }"
        @click="selectEvent(index)"
      >
        <div class="event-header">
          <div class="event-info">
            <div class="event-name">{{ event.name }}</div>
            <div class="event-description">{{ event.description || '无描述' }}</div>
          </div>
          <div class="event-actions">
            <el-button 
              type="text" 
              size="mini" 
              icon="el-icon-edit" 
              @click.stop="editEvent(index)"
              title="编辑事件"
            />
            <el-button 
              type="text" 
              size="mini" 
              icon="el-icon-copy-document" 
              @click.stop="duplicateEvent(index)"
              title="复制事件"
            />
            <el-button 
              type="text" 
              size="mini" 
              icon="el-icon-delete" 
              @click.stop="deleteEvent(index)"
              title="删除事件"
              :confirm-message="`确定要删除事件 ${event.name} 吗？`"
            />
          </div>
        </div>
        
        <!-- 条件配置 -->
        <div class="event-condition" v-if="event.condition">
          <div class="condition-label">执行条件:</div>
          <div class="condition-expression">{{ event.condition.expression }}</div>
        </div>
        
        <!-- 动作配置摘要 -->
        <div class="event-actions-summary" v-if="event.actions && event.actions.length > 0">
          <div class="actions-label">执行动作 ({{ event.actions.length }}):</div>
          <div class="action-list">
            <div 
              v-for="(action, actionIndex) in event.actions.slice(0, 3)" 
              :key="actionIndex"
              class="action-item-summary"
            >
              <span class="action-type">{{ getActionTypeName(action.type) }}</span>
              <span class="action-content">{{ getActionSummary(action) }}</span>
            </div>
            <div v-if="event.actions.length > 3" class="more-actions">
              还有 {{ event.actions.length - 3 }} 个动作...
            </div>
          </div>
        </div>
      </div>
    </div>
    
    <!-- 事件编辑对话框 -->
    <el-dialog
      v-model="eventDialogVisible"
      :title="isEditing ? '编辑事件' : '添加事件'"
      width="700px"
      :close-on-click-modal="false"
    >
      <div v-if="currentEvent" class="event-editor-form">
        <el-form 
          ref="eventFormRef" 
          :model="currentEvent" 
          label-width="100px"
          class="event-form"
        >
          <!-- 基本信息 -->
          <el-form-item label="事件名称" prop="name" required>
            <el-input 
              v-model="currentEvent.name" 
              placeholder="请输入事件名称（如：click, change）"
              @input="validateEventName"
            />
            <div v-if="eventNameError" class="error-message">{{ eventNameError }}</div>
          </el-form-item>
          
          <el-form-item label="事件描述">
            <el-input 
              v-model="currentEvent.description" 
              placeholder="请输入事件描述"
              type="textarea"
              :rows="2"
            />
          </el-form-item>
          
          <!-- 触发条件 -->
          <el-form-item label="触发条件">
            <el-collapse>
              <el-collapse-item title="设置条件" name="condition">
                <div class="condition-editor">
                  <el-input 
                    v-model="currentEvent.condition.expression" 
                    placeholder="例如: $event.value > 0"
                    type="textarea"
                    :rows="3"
                  />
                  <div class="condition-help">
                    <el-tooltip content="条件表达式将在事件触发时执行，返回true时才执行后续动作">
                      <i class="el-icon-question"></i>
                    </el-tooltip>
                    <span class="help-text">支持使用 $event, $component, $context 等变量</span>
                  </div>
                </div>
              </el-collapse-item>
            </el-collapse>
          </el-form-item>
          
          <!-- 执行动作 -->
          <el-form-item label="执行动作">
            <div class="actions-editor">
              <!-- 动作列表 -->
              <div class="action-list">
                <div 
                  v-for="(action, index) in currentEvent.actions" 
                  :key="index"
                  class="action-config"
                >
                  <div class="action-header">
                    <div class="action-type-label">
                      <el-select 
                        v-model="action.type" 
                        size="small"
                        @change="onActionTypeChange(index)"
                      >
                        <el-option 
                          v-for="type in actionTypes" 
                          :key="type.value" 
                          :label="type.label" 
                          :value="type.value"
                        />
                      </el-select>
                    </div>
                    <div class="action-actions">
                      <el-button 
                        type="text" 
                        size="mini" 
                        icon="el-icon-top" 
                        @click="moveActionUp(index)"
                        :disabled="index === 0"
                        title="上移"
                      />
                      <el-button 
                        type="text" 
                        size="mini" 
                        icon="el-icon-bottom" 
                        @click="moveActionDown(index)"
                        :disabled="index === currentEvent.actions.length - 1"
                        title="下移"
                      />
                      <el-button 
                        type="text" 
                        size="mini" 
                        icon="el-icon-delete" 
                        @click="removeAction(index)"
                        title="删除"
                      />
                    </div>
                  </div>
                  
                  <!-- 动作配置详情 -->
                  <div class="action-details">
                    <!-- API调用配置 -->
                    <div v-if="action.type === 'apiCall'" class="api-config">
                      <el-form-item label="API类型">
                        <el-radio-group v-model="action.apiConfig.method">
                          <el-radio-button label="GET" />
                          <el-radio-button label="POST" />
                          <el-radio-button label="PUT" />
                          <el-radio-button label="DELETE" />
                        </el-radio-group>
                      </el-form-item>
                      
                      <el-form-item label="API地址">
                        <el-input 
                          v-model="action.apiConfig.url" 
                          placeholder="例如: /api/users"
                        />
                      </el-form-item>
                      
                      <el-form-item label="请求参数">
                        <el-input 
                          v-model="action.apiConfig.params" 
                          placeholder="JSON格式的参数"
                          type="textarea"
                          :rows="3"
                        />
                      </el-form-item>
                      
                      <el-form-item label="数据处理">
                        <el-input 
                          v-model="action.apiConfig.dataHandler" 
                          placeholder="例如: $context.users = $result.data"
                        />
                      </el-form-item>
                    </div>
                    
                    <!-- 方法调用配置 -->
                    <div v-else-if="action.type === 'methodCall'" class="method-config">
                      <el-form-item label="方法名称">
                        <el-input 
                          v-model="action.methodConfig.name" 
                          placeholder="例如: handleSubmit"
                        />
                      </el-form-item>
                      
                      <el-form-item label="参数列表">
                        <el-input 
                          v-model="action.methodConfig.params" 
                          placeholder="JSON格式的参数数组"
                          type="textarea"
                          :rows="2"
                        />
                      </el-form-item>
                    </div>
                    
                    <!-- 状态更新配置 -->
                    <div v-else-if="action.type === 'stateUpdate'" class="state-config">
                      <el-form-item label="状态路径">
                        <el-input 
                          v-model="action.stateConfig.path" 
                          placeholder="例如: formData.name"
                        />
                      </el-form-item>
                      
                      <el-form-item label="状态值">
                        <el-input 
                          v-model="action.stateConfig.value" 
                          placeholder="例如: $event.target.value"
                          type="textarea"
                          :rows="2"
                        />
                      </el-form-item>
                    </div>
                    
                    <!-- 消息提示配置 -->
                    <div v-else-if="action.type === 'showMessage'" class="message-config">
                      <el-form-item label="消息类型">
                        <el-select v-model="action.messageConfig.type">
                          <el-option label="成功" value="success" />
                          <el-option label="警告" value="warning" />
                          <el-option label="错误" value="error" />
                          <el-option label="信息" value="info" />
                        </el-select>
                      </el-form-item>
                      
                      <el-form-item label="消息内容">
                        <el-input 
                          v-model="action.messageConfig.content" 
                          placeholder="消息内容"
                          type="textarea"
                          :rows="2"
                        />
                      </el-form-item>
                    </div>
                    
                    <!-- 页面跳转配置 -->
                    <div v-else-if="action.type === 'navigate'" class="navigate-config">
                      <el-form-item label="跳转类型">
                        <el-select v-model="action.navigateConfig.type">
                          <el-option label="内部路由" value="router" />
                          <el-option label="外部链接" value="external" />
                        </el-select>
                      </el-form-item>
                      
                      <el-form-item label="目标地址">
                        <el-input 
                          v-model="action.navigateConfig.url" 
                          placeholder="例如: /dashboard 或 https://example.com"
                        />
                      </el-form-item>
                      
                      <el-form-item v-if="action.navigateConfig.type === 'router'" label="路由参数">
                        <el-input 
                          v-model="action.navigateConfig.params" 
                          placeholder="JSON格式的路由参数"
                          type="textarea"
                          :rows="2"
                        />
                      </el-form-item>
                    </div>
                    
                    <!-- 自定义脚本配置 -->
                    <div v-else-if="action.type === 'script'" class="script-config">
                      <el-form-item label="脚本内容">
                        <el-input 
                          v-model="action.scriptConfig.code" 
                          placeholder="编写JavaScript代码"
                          type="textarea"
                          :rows="6"
                        />
                        <div class="script-help">
                          <span class="help-text">可使用 $event, $component, $context, $api 等变量</span>
                        </div>
                      </el-form-item>
                    </div>
                  </div>
                </div>
              </div>
              
              <!-- 添加动作按钮 -->
              <div class="add-action-section">
                <el-button 
                  type="primary" 
                  size="small" 
                  @click="addAction"
                  icon="el-icon-plus"
                >
                  添加动作
                </el-button>
              </div>
            </div>
          </el-form-item>
        </el-form>
      </div>
      
      <template #footer>
        <el-button @click="closeEventDialog">取消</el-button>
        <el-button type="primary" @click="saveEvent">保存</el-button>
      </template>
    </el-dialog>
    
    <!-- 代码编辑器配置对话框 -->
    <el-dialog
      v-model="codeEditorVisible"
      title="代码编辑"
      width="800px"
    >
      <div class="code-editor-container">
        <el-select v-model="codeLanguage" style="margin-bottom: 10px; width: 120px;">
          <el-option label="JavaScript" value="javascript" />
          <el-option label="TypeScript" value="typescript" />
          <el-option label="JSON" value="json" />
        </el-select>
        <div 
          ref="codeEditorRef"
          class="code-editor"
          style="height: 400px;"
        ></div>
      </div>
      
      <template #footer>
        <el-button @click="closeCodeEditor">取消</el-button>
        <el-button type="primary" @click="saveCode">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script lang="ts" setup>
import { ref, reactive, computed, watch, onMounted, nextTick } from 'vue';
import { deepClone } from '../utils/deepCloneUtils';
import type { EventConfig, ActionConfig } from '@/types/event'
import { eventBusService, useEventBus } from '@/services/eventBusService';
import type { Component } from 'vue';

// 类型定义
interface EventCondition {
  expression: string;
}

interface ApiCallConfig {
  method: string;
  url: string;
  params: string;
  dataHandler: string;
}

interface MethodCallConfig {
  name: string;
  params: string;
}

interface StateUpdateConfig {
  path: string;
  value: string;
}

interface MessageConfig {
  type: string;
  content: string;
}

interface NavigateConfig {
  type: string;
  url: string;
  params: string;
}

interface ScriptConfig {
  code: string;
}

interface Action {
  type: string;
  apiConfig?: ApiCallConfig;
  methodConfig?: MethodCallConfig;
  stateConfig?: StateUpdateConfig;
  messageConfig?: MessageConfig;
  navigateConfig?: NavigateConfig;
  scriptConfig?: ScriptConfig;
}

interface EventConfig {
  name: string;
  description: string;
  condition: EventCondition;
  actions: Action[];
}

// 组件属性
interface EventEditorProps {
  events: EventConfig[];
  title?: string;
  disabled?: boolean;
}

const props = withDefaults(defineProps<EventEditorProps>(), {
  events: () => [],
  title: '事件编辑器',
  disabled: false
});

// 事件
const emit = defineEmits<{
  'update:events': [events: EventConfig[]];
  'event-select': [event: EventConfig];
  'event-add': [event: EventConfig];
  'event-update': [event: EventConfig, index: number];
  'event-delete': [index: number];
}>();

// 响应式数据
const selectedEventIndex = ref<number | null>(null);
const eventDialogVisible = ref(false);
const codeEditorVisible = ref(false);
const codeLanguage = ref('javascript');
const currentEvent = ref<EventConfig | null>(null);
const isEditing = ref(false);
const eventNameError = ref('');
const eventFormRef = ref<any>(null);
const codeEditorRef = ref<HTMLElement | null>(null);

// 动作类型
const actionTypes = [
  { label: 'API调用', value: 'apiCall' },
  { label: '方法调用', value: 'methodCall' },
  { label: '状态更新', value: 'stateUpdate' },
  { label: '消息提示', value: 'showMessage' },
  { label: '页面跳转', value: 'navigate' },
  { label: '自定义脚本', value: 'script' }
];

// 方法定义
const { createAutoContext } = useEventBus()
const context = ref(createAutoContext())

// 选择事件
const selectEvent = (index: number) => {
  selectedEventIndex.value = index;
  emit('event-select', props.events[index]);
};

// 添加新事件
const addNewEvent = () => {
  currentEvent.value = createEmptyEvent();
  isEditing.value = false;
  eventDialogVisible.value = true;
};

// 编辑事件
const editEvent = (index: number) => {
  currentEvent.value = deepClone(props.events[index]);
  isEditing.value = true;
  eventDialogVisible.value = true;
};

// 复制事件
const duplicateEvent = (index: number) => {
  const originalEvent = props.events[index];
  const newEvent = deepClone(originalEvent);
  newEvent.name = `${newEvent.name}_copy`;
  
  const updatedEvents = [...props.events];
  updatedEvents.splice(index + 1, 0, newEvent);
  
  emit('update:events', updatedEvents);
  emit('event-add', newEvent);
};

// 删除事件
const deleteEvent = (index: number) => {
  const updatedEvents = [...props.events];
  updatedEvents.splice(index, 1);
  
  emit('update:events', updatedEvents);
  emit('event-delete', index);
  
  if (selectedEventIndex.value === index) {
    selectedEventIndex.value = null;
  }
};

// 验证事件名称
const validateEventName = () => {
  if (!currentEvent.value) return;
  
  const name = currentEvent.value.name.trim();
  
  // 检查是否为空
  if (!name) {
    eventNameError.value = '事件名称不能为空';
    return false;
  }
  
  // 检查是否包含特殊字符
  if (!/^[a-zA-Z_$][a-zA-Z0-9_$]*$/.test(name)) {
    eventNameError.value = '事件名称只能包含字母、数字、下划线和$，且不能以数字开头';
    return false;
  }
  
  // 检查是否重复
  const isDuplicate = props.events.some((event, index) => 
    event.name === name && (!isEditing.value || index !== selectedEventIndex.value)
  );
  
  if (isDuplicate) {
    eventNameError.value = '事件名称已存在';
    return false;
  }
  
  eventNameError.value = '';
  return true;
};

// 保存事件
const saveEvent = () => {
  if (!currentEvent.value) return;
  
  // 验证事件名称
  if (!validateEventName()) return;
  
  const updatedEvents = [...props.events];
  
  if (isEditing.value && selectedEventIndex.value !== null) {
    // 更新现有事件
    updatedEvents[selectedEventIndex.value] = { ...currentEvent.value };
    emit('update:events', updatedEvents);
    emit('event-update', currentEvent.value, selectedEventIndex.value);
  } else {
    // 添加新事件
    updatedEvents.push({ ...currentEvent.value });
    emit('update:events', updatedEvents);
    emit('event-add', currentEvent.value);
  }
  
  closeEventDialog();
};

// 关闭事件对话框
const closeEventDialog = () => {
  eventDialogVisible.value = false;
  currentEvent.value = null;
  eventNameError.value = '';
};

// 添加动作
const addAction = () => {
  if (!currentEvent.value) return;
  
  currentEvent.value.actions.push(createEmptyAction());
};

// 移除动作
const removeAction = (index: number) => {
  if (!currentEvent.value) return;
  
  currentEvent.value.actions.splice(index, 1);
};

// 移动动作上移
const moveActionUp = (index: number) => {
  if (!currentEvent.value || index <= 0) return;
  
  const action = currentEvent.value.actions.splice(index, 1)[0];
  currentEvent.value.actions.splice(index - 1, 0, action);
};

// 移动动作下移
const moveActionDown = (index: number) => {
  if (!currentEvent.value || index >= currentEvent.value.actions.length - 1) return;
  
  const action = currentEvent.value.actions.splice(index, 1)[0];
  currentEvent.value.actions.splice(index + 1, 0, action);
};

// 动作类型改变
const onActionTypeChange = (index: number) => {
  if (!currentEvent.value) return;
  
  const action = currentEvent.value.actions[index];
  // 根据动作类型创建相应的配置
  switch (action.type) {
    case 'apiCall':
      action.apiConfig = action.apiConfig || createEmptyApiConfig();
      break;
    case 'methodCall':
      action.methodConfig = action.methodConfig || createEmptyMethodConfig();
      break;
    case 'stateUpdate':
      action.stateConfig = action.stateConfig || createEmptyStateConfig();
      break;
    case 'showMessage':
      action.messageConfig = action.messageConfig || createEmptyMessageConfig();
      break;
    case 'navigate':
      action.navigateConfig = action.navigateConfig || createEmptyNavigateConfig();
      break;
    case 'script':
      action.scriptConfig = action.scriptConfig || createEmptyScriptConfig();
      break;
  }
};

// 打开代码编辑器
const openCodeEditor = (code: string, language: string = 'javascript') => {
  codeLanguage.value = language;
  // 这里应该初始化代码编辑器
  codeEditorVisible.value = true;
  
  nextTick(() => {
    // 实际项目中这里应该初始化 Monaco Editor 或其他代码编辑器
    console.log('Code editor initialized with:', code);
  });
};

// 关闭代码编辑器
const closeCodeEditor = () => {
  codeEditorVisible.value = false;
};

// 保存代码
const saveCode = () => {
  // 实际项目中这里应该从代码编辑器获取代码并保存
  console.log('Code saved');
  closeCodeEditor();
};

// 创建空事件
const createEmptyEvent = (): EventConfig => ({
  name: '',
  description: '',
  condition: {
    expression: ''
  },
  actions: []
});

// 创建空动作
const createEmptyAction = (): Action => ({
  type: 'apiCall',
  apiConfig: createEmptyApiConfig()
});

// 创建空API配置
const createEmptyApiConfig = (): ApiCallConfig => ({
  method: 'GET',
  url: '',
  params: '',
  dataHandler: ''
});

// 创建空方法配置
const createEmptyMethodConfig = (): MethodCallConfig => ({
  name: '',
  params: ''
});

// 创建空状态配置
const createEmptyStateConfig = (): StateUpdateConfig => ({
  path: '',
  value: ''
});

// 创建空消息配置
const createEmptyMessageConfig = (): MessageConfig => ({
  type: 'success',
  content: ''
});

// 创建空导航配置
const createEmptyNavigateConfig = (): NavigateConfig => ({
  type: 'router',
  url: '',
  params: ''
});

// 创建空脚本配置
const createEmptyScriptConfig = (): ScriptConfig => ({
  code: ''
});

// 获取动作类型名称
const getActionTypeName = (type: string): string => {
  const actionType = actionTypes.find(t => t.value === type);
  return actionType ? actionType.label : type;
};

// 获取动作摘要
const getActionSummary = (action: Action): string => {
  switch (action.type) {
    case 'apiCall':
      return `${action.apiConfig?.method} ${action.apiConfig?.url || 'api地址'}`;
    case 'methodCall':
      return `${action.methodConfig?.name || '方法名'}`;
    case 'stateUpdate':
      return `${action.stateConfig?.path || '状态路径'} = ${action.stateConfig?.value || '值'}`;
    case 'showMessage':
      return `${action.messageConfig?.content || '消息内容'}`;
    case 'navigate':
      return `${action.navigateConfig?.url || '地址'}`;
    case 'script':
      return '自定义脚本';
    default:
      return '未知动作';
  }
};

// 监听事件列表变化
watch(
  () => props.events,
  () => {
    // 如果选中的事件不存在了，重置选中状态
    if (selectedEventIndex.value !== null && selectedEventIndex.value >= props.events.length) {
      selectedEventIndex.value = null;
    }
  },
  { deep: true }
);

// 使用事件总线执行事件处理
const executeTestEvent = async (eventConfig, eventData) => {
  try {
    const parsedData = typeof eventData === 'string' ? JSON.parse(eventData) : eventData;
    
    // 使用事件总线执行事件处理
    const result = await context.value.executeActions(
      eventConfig.actions || [],
      parsedData
    );
    
    return {
      success: true,
      result
    };
  } catch (error) {
    console.error('执行事件失败:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : String(error)
    };
  }
};
</script>

<style scoped>
.event-editor {
  height: 100%;
  display: flex;
  flex-direction: column;
  background-color: #f5f7fa;
}

/* 编辑器头部 */
.editor-header {
  height: 60px;
  background-color: #fff;
  border-bottom: 1px solid #e4e7ed;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 16px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
}

.editor-header h3 {
  margin: 0;
  font-size: 16px;
  font-weight: 500;
  color: #303133;
}

/* 事件列表 */
.event-list {
  flex: 1;
  overflow-y: auto;
  padding: 16px;
}

/* 事件项 */
.event-item {
  background-color: #fff;
  border-radius: 6px;
  padding: 16px;
  margin-bottom: 12px;
  cursor: pointer;
  transition: all 0.3s ease;
  border: 1px solid #e4e7ed;
}

.event-item:hover {
  border-color: #409eff;
  box-shadow: 0 2px 8px rgba(64, 158, 255, 0.1);
}

.event-item.selected {
  border-color: #409eff;
  background-color: #ecf5ff;
  box-shadow: 0 2px 8px rgba(64, 158, 255, 0.2);
}

/* 事件头部 */
.event-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}

.event-info {
  flex: 1;
  min-width: 0;
}

.event-name {
  font-size: 16px;
  font-weight: 500;
  color: #303133;
  margin-bottom: 4px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.event-description {
  font-size: 12px;
  color: #909399;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.event-actions {
  display: flex;
  gap: 8px;
}

/* 事件条件 */
.event-condition {
  background-color: #f5f7fa;
  border-left: 3px solid #409eff;
  padding: 8px 12px;
  border-radius: 0 4px 4px 0;
  margin-bottom: 12px;
}

.condition-label {
  font-size: 12px;
  color: #606266;
  margin-bottom: 4px;
  font-weight: 500;
}

.condition-expression {
  font-size: 13px;
  color: #303133;
  font-family: 'Consolas', 'Monaco', monospace;
}

/* 事件动作摘要 */
.event-actions-summary {
  background-color: #f0f9eb;
  padding: 8px 12px;
  border-radius: 4px;
}

.actions-label {
  font-size: 12px;
  color: #606266;
  margin-bottom: 8px;
  font-weight: 500;
}

.action-list {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.action-item-summary {
  display: flex;
  align-items: center;
  font-size: 13px;
  color: #606266;
}

.action-type {
  background-color: #67c23a;
  color: #fff;
  padding: 2px 6px;
  border-radius: 3px;
  font-size: 11px;
  margin-right: 8px;
}

.action-content {
  flex: 1;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.more-actions {
  font-size: 12px;
  color: #909399;
  font-style: italic;
}

/* 事件编辑器表单 */
.event-editor-form {
  max-height: 600px;
  overflow-y: auto;
}

.event-form {
  background-color: #fff;
}

.error-message {
  color: #f56c6c;
  font-size: 12px;
  margin-top: 4px;
}

/* 条件编辑器 */
.condition-editor {
  padding: 10px 0;
}

.condition-help {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-top: 8px;
  font-size: 12px;
  color: #909399;
}

/* 动作编辑器 */
.actions-editor {
  padding: 10px 0;
}

.action-config {
  background-color: #fafafa;
  border: 1px solid #ebeef5;
  border-radius: 4px;
  padding: 16px;
  margin-bottom: 12px;
}

.action-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}

.action-type-label {
  flex: 1;
  max-width: 200px;
}

.action-details {
  background-color: #fff;
  border-radius: 4px;
  padding: 12px;
  border-left: 3px solid #409eff;
}

.api-config,
.method-config,
.state-config,
.message-config,
.navigate-config,
.script-config {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.script-help {
  margin-top: 8px;
  font-size: 12px;
  color: #909399;
}

.help-text {
  font-size: 12px;
  color: #909399;
}

.add-action-section {
  margin-top: 12px;
  text-align: center;
}

/* 代码编辑器容器 */
.code-editor-container {
  padding: 10px 0;
}

.code-editor {
  border: 1px solid #ebeef5;
  border-radius: 4px;
  overflow: hidden;
}

/* 响应式调整 */
@media (max-width: 768px) {
  .editor-header {
    flex-direction: column;
    height: auto;
    padding: 10px;
    gap: 10px;
  }
  
  .event-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 10px;
  }
  
  .event-actions {
    width: 100%;
    justify-content: flex-end;
  }
  
  .action-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 10px;
  }
  
  .action-type-label {
    max-width: none;
    width: 100%;
  }
}
</style>