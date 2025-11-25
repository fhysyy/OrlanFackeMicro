<template>
  <div class="component-drag-drop-manager">
    <!-- 拖拽源（组件列表） -->
    <div class="drag-source" v-if="showComponentList">
      <div class="source-header">
        <h3>{{ title || '组件库' }}</h3>
        <div class="source-actions">
          <el-button 
            type="text" 
            icon="el-icon-search" 
            @click="toggleSearch"
          />
          <el-button 
            type="text" 
            icon="el-icon-s-unfold" 
            @click="toggleExpand"
          />
        </div>
      </div>
      
      <!-- 搜索框 -->
      <div v-if="showSearch" class="search-box">
        <el-input
          v-model="searchQuery"
          placeholder="搜索组件..."
          size="small"
          prefix-icon="el-icon-search"
          @input="handleSearch"
        />
      </div>
      
      <!-- 分类标签 -->
      <div class="category-tabs" v-if="categories.length > 0">
        <el-scrollbar wrap-class="category-scrollbar">
          <div class="category-list">
            <el-tag
              v-for="category in categories"
              :key="category.key"
              :type="activeCategory === category.key ? 'primary' : 'info'"
              :closable="false"
              :disable-transitions="false"
              @click="activeCategory = category.key"
              class="category-tag"
            >
              {{ category.name }}
              <span class="component-count">({{ getCategoryComponentCount(category.key) }})</span>
            </el-tag>
          </div>
        </el-scrollbar>
      </div>
      
      <!-- 组件列表 -->
      <el-scrollbar class="component-list-scrollbar">
        <div class="component-list">
          <!-- 无组件提示 -->
          <div v-if="filteredComponents.length === 0" class="no-components">
            <el-empty description="没有找到匹配的组件" />
          </div>
          
          <!-- 组件项 -->
          <div
            v-for="component in filteredComponents"
            :key="component.type"
            class="component-item"
            :draggable="true"
            @dragstart="handleDragStart($event, component)"
            @dragend="handleDragEnd"
            @mouseenter="hoveredComponent = component.type"
            @mouseleave="hoveredComponent = null"
          >
            <div class="component-icon">
              <i :class="['el-icon-' + (component.icon || 'menu')]"></i>
            </div>
            <div class="component-info">
              <div class="component-name">{{ component.name }}</div>
              <div class="component-desc">{{ component.description || '无描述' }}</div>
            </div>
            <div class="component-actions">
              <el-button
                type="text"
                size="mini"
                icon="el-icon-question"
                @click.stop="showComponentInfo(component)"
                title="查看组件信息"
              />
            </div>
          </div>
        </div>
      </el-scrollbar>
    </div>
    
    <!-- 拖放目标区域 -->
    <div 
      class="drop-target"
      :class="{
        'active': isDropZoneActive,
        'dragging-over': isDraggingOver,
        'disabled': !canDrop
      }"
      @dragenter="handleDragEnter"
      @dragover="handleDragOver"
      @dragleave="handleDragLeave"
      @drop="handleDrop"
    >
      <!-- 拖放提示 -->
      <div 
        v-if="showDropHint && (isDraggingOver || isDropZoneActive)"
        class="drop-hint"
      >
        <div class="hint-content">
          <i class="el-icon-upload2"></i>
          <span>{{ dropHintText }}</span>
        </div>
      </div>
      
      <!-- 子内容插槽 -->
      <slot></slot>
    </div>
    
    <!-- 拖拽预览 -->
    <div 
      v-if="isDragging && draggedComponent"
      class="drag-preview"
      :style="{
        left: dragPosition.x + 'px',
        top: dragPosition.y + 'px'
      }"
    >
      <div class="preview-content">
        <div class="preview-icon">
          <i :class="['el-icon-' + (draggedComponent.icon || 'menu')]"></i>
        </div>
        <div class="preview-name">{{ draggedComponent.name }}</div>
      </div>
    </div>
    
    <!-- 组件信息对话框 -->
    <el-dialog
      v-model="componentInfoVisible"
      title="组件信息"
      width="500px"
    >
      <div v-if="selectedComponent" class="component-info-dialog">
        <el-descriptions :column="1" border>
          <el-descriptions-item label="组件名称">
            {{ selectedComponent.name }}
          </el-descriptions-item>
          <el-descriptions-item label="组件类型">
            {{ selectedComponent.type }}
          </el-descriptions-item>
          <el-descriptions-item label="组件描述">
            {{ selectedComponent.description || '无描述' }}
          </el-descriptions-item>
          <el-descriptions-item label="分类">
            {{ getCategoryName(selectedComponent.category) }}
          </el-descriptions-item>
          <el-descriptions-item label="版本">
            {{ selectedComponent.version || '1.0.0' }}
          </el-descriptions-item>
          <el-descriptions-item label="是否可用">
            <el-tag :type="selectedComponent.disabled ? 'danger' : 'success'">
              {{ selectedComponent.disabled ? '不可用' : '可用' }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="作者">
            {{ selectedComponent.author || '未知' }}
          </el-descriptions-item>
        </el-descriptions>
        
        <!-- 属性列表 -->
        <div v-if="selectedComponent.props && selectedComponent.props.length > 0" class="props-section">
          <h4>属性列表</h4>
          <el-table :data="selectedComponent.props" style="width: 100%">
            <el-table-column prop="name" label="属性名" width="120" />
            <el-table-column prop="type" label="类型" width="100" />
            <el-table-column prop="default" label="默认值" width="100" />
            <el-table-column prop="description" label="描述" />
          </el-table>
        </div>
        
        <!-- 事件列表 -->
        <div v-if="selectedComponent.events && selectedComponent.events.length > 0" class="events-section">
          <h4>事件列表</h4>
          <el-table :data="selectedComponent.events" style="width: 100%">
            <el-table-column prop="name" label="事件名" width="120" />
            <el-table-column prop="description" label="描述" />
            <el-table-column prop="params" label="参数" />
          </el-table>
        </div>
      </div>
    </el-dialog>
  </div>
</template>

<script lang="ts" setup>
import { ref, reactive, computed, watch, onMounted, onUnmounted } from 'vue';
import type { ComponentConfig, ComponentType } from '../types/page';

// 简单实现dragDropOptimizer对象以支持拖拽功能
const dragDropOptimizer = {
  _state: {
    isDragging: false,
    isDraggingOver: false,
    isDropZoneActive: false,
    dragPosition: { x: 0, y: 0 },
    draggedItem: null,
    dragCounter: 0
  },
  
  getState() {
    return this._state;
  },
  
  startDrag(component: ComponentType, event: DragEvent) {
    this._state.isDragging = true;
    this._state.draggedItem = component;
  },
  
  endDrag(event: DragEvent) {
    // 不在这里清除draggedItem，让handleDrop方法能正确获取
    this._state.isDragging = false;
    this._state.isDraggingOver = false;
    this._state.isDropZoneActive = false;
    this._state.dragCounter = 0;
  },
  
  dragEnter(event: DragEvent, zone: string) {
    this._state.dragCounter++;
    this._state.isDropZoneActive = true;
    this._state.isDraggingOver = true;
    event.preventDefault();
  },
  
  dragOver(event: DragEvent) {
    this._state.dragPosition = { x: event.clientX, y: event.clientY };
    event.preventDefault();
  },
  
  dragLeave(event: DragEvent, zone: string) {
    this._state.dragCounter--;
    if (this._state.dragCounter === 0) {
      this._state.isDraggingOver = false;
      this._state.isDropZoneActive = false;
    }
  },
  
  drop(event: DragEvent, validator: (component: any, target: any) => boolean) {
    // 简化实现，不实际进行数据解析，因为我们会在handleDrop中使用draggedComponent
    event.preventDefault();
    return true; // 返回成功状态
  },
  
  on(event: string, handler: Function) {
    // 简化实现，不实际绑定事件
  },
  
  off(event: string) {
    // 简化实现，不实际解绑事件
  }
};

// 组件属性
interface ComponentDragDropManagerProps {
  // 组件列表
  components: ComponentType[];
  // 分类列表
  categories?: Array<{ key: string; name: string }>;
  // 是否显示组件列表
  showComponentList?: boolean;
  // 是否显示拖放提示
  showDropHint?: boolean;
  // 拖放提示文本
  dropHintText?: string;
  // 是否启用拖放
  draggable?: boolean;
  // 是否禁用放置
  disabled?: boolean;
  // 标题
  title?: string;
  // 初始激活的分类
  defaultCategory?: string;
  // 拖放验证函数
  validateDrop?: (component: ComponentType, target: any) => boolean;
}

const props = withDefaults(defineProps<ComponentDragDropManagerProps>(), {
  components: () => [],
  categories: () => [
    { key: 'all', name: '全部' },
    { key: 'basic', name: '基础组件' },
    { key: 'layout', name: '布局组件' },
    { key: 'form', name: '表单组件' },
    { key: 'data', name: '数据展示' },
    { key: 'feedback', name: '反馈组件' },
    { key: 'other', name: '其他组件' }
  ],
  showComponentList: true,
  showDropHint: true,
  dropHintText: '将组件拖放到此处',
  draggable: true,
  disabled: false,
  title: '组件库',
  defaultCategory: 'all',
  validateDrop: () => true
});

// 事件
const emit = defineEmits<{
  // 组件放置事件
  'component-drop': [component: ComponentType, event: DragEvent, target?: any];
  // 拖拽开始事件
  'drag-start': [component: ComponentType, event: DragEvent];
  // 拖拽结束事件
  'drag-end': [event: DragEvent];
  // 拖拽进入事件
  'drag-enter': [event: DragEvent];
  // 拖拽离开事件
  'drag-leave': [event: DragEvent];
  // 组件选择事件
  'component-select': [component: ComponentType];
  // 搜索事件
  'search': [query: string];
}>();

// 移除了拖拽优化器，简化拖拽逻辑

// 响应式数据
const activeCategory = ref(props.defaultCategory);
const showSearch = ref(false);
const searchQuery = ref('');
const hoveredComponent = ref<string | null>(null);
const componentInfoVisible = ref(false);
const selectedComponent = ref<ComponentType | null>(null);

// 获取拖拽状态
const dragState = computed(() => dragDropOptimizer.getState());
const isDragging = computed(() => dragState.value.isDragging);
const isDraggingOver = computed(() => dragState.value.isDraggingOver);
const isDropZoneActive = computed(() => dragState.value.isDropZoneActive);
const dragPosition = computed(() => dragState.value.dragPosition);
const draggedComponent = computed(() => dragState.value.draggedItem);

// 计算属性

// 是否可以放置
const canDrop = computed(() => {
  return props.draggable && !props.disabled;
});

// 过滤后的组件列表
const filteredComponents = computed(() => {
  let result = [...props.components];
  
  // 按分类过滤
  if (activeCategory.value !== 'all') {
    result = result.filter(comp => comp.category === activeCategory.value);
  }
  
  // 按搜索关键词过滤
  if (searchQuery.value.trim()) {
    const query = searchQuery.value.toLowerCase().trim();
    result = result.filter(comp => 
      comp.name.toLowerCase().includes(query) ||
      comp.type.toLowerCase().includes(query) ||
      (comp.description && comp.description.toLowerCase().includes(query))
    );
  }
  
  // 过滤禁用的组件
  result = result.filter(comp => !comp.disabled);
  
  return result;
});

// 获取分类下的组件数量
const getCategoryComponentCount = (categoryKey: string): number => {
  if (categoryKey === 'all') {
    return props.components.filter(comp => !comp.disabled).length;
  }
  return props.components.filter(comp => 
    comp.category === categoryKey && !comp.disabled
  ).length;
};

// 获取分类名称
const getCategoryName = (categoryKey: string): string => {
  const category = props.categories.find(cat => cat.key === categoryKey);
  return category ? category.name : categoryKey;
};

// 方法定义

// 处理拖拽开始
const handleDragStart = (event: DragEvent, component: ComponentType) => {
  if (!props.draggable || component.disabled) {
    event.preventDefault();
    return;
  }
  
  // 设置拖拽数据
  if (event.dataTransfer) {
    event.dataTransfer.effectAllowed = 'copy';
    event.dataTransfer.setData('application/json', JSON.stringify(component));
    event.dataTransfer.setDragImage(event.target as Element, 0, 0);
  }
  
  // 添加拖拽样式
  if (event.target) {
    (event.target as Element).classList.add('dragging');
  }
  
  // 使用优化器处理拖拽开始
  dragDropOptimizer.startDrag(component, event);
  
  // 触发事件
  emit('drag-start', component, event);
  emit('component-select', component);
};

// 处理拖拽结束
const handleDragEnd = (event: DragEvent) => {
  // 移除拖拽样式
  if (event.target) {
    (event.target as Element).classList.remove('dragging');
  }
  
  // 使用优化器处理拖拽结束
  dragDropOptimizer.endDrag(event);
  
  // 触发事件
  emit('drag-end', event);
};

// 处理拖拽进入
const handleDragEnter = (event: DragEvent) => {
  if (!canDrop.value) return;
  
  // 使用优化器处理拖拽进入
  dragDropOptimizer.dragEnter(event, 'main-drop-zone');
  
  // 触发事件
  if (dragState.value.dragCounter === 1) {
    emit('drag-enter', event);
  }
};

// 处理拖拽经过
const handleDragOver = (event: DragEvent) => {
  if (!canDrop.value) return;
  
  // 使用优化器处理拖拽经过
  dragDropOptimizer.dragOver(event);
};

// 处理拖拽离开
const handleDragLeave = (event: DragEvent) => {
  if (!canDrop.value) return;
  
  // 使用优化器处理拖拽离开
  dragDropOptimizer.dragLeave(event, 'main-drop-zone');
  
  // 触发事件
  if (dragState.value.dragCounter === 0) {
    emit('drag-leave', event);
  }
};

// 处理放置
const handleDrop = (event: DragEvent) => {
  if (!canDrop.value) return;
  
  // 阻止默认行为
  event.preventDefault();
  event.stopPropagation();
  
  try {
    // 从dataTransfer获取拖拽数据，这是最可靠的方式
    let component;
    if (event.dataTransfer) {
      try {
        component = JSON.parse(event.dataTransfer.getData('application/json'));
      } catch (jsonError) {
        console.warn('无法从dataTransfer解析数据，尝试使用draggedComponent');
        // 如果解析失败，回退到draggedComponent
        component = draggedComponent.value;
      }
    } else {
      // 如果没有dataTransfer，使用draggedComponent
      component = draggedComponent.value;
    }
    
    if (component) {
      console.log('放置组件:', component.type);
      // 验证放置
      if (props.validateDrop(component, event.currentTarget)) {
        // 触发事件
        emit('component-drop', component, event, event.currentTarget);
      } else {
        console.warn('Drop validation failed for component:', component.type);
      }
    } else {
      console.warn('没有拖拽组件数据');
    }
  } catch (error) {
    console.error('处理组件放置时出错:', error);
  } finally {
    // 清理拖拽状态
    dragDropOptimizer._state.draggedItem = null; // 确保清除draggedItem
    dragDropOptimizer.endDrag(event);
    emit('drop', event);
  }
};

// 优化器已经处理了拖拽预览相关逻辑，不再需要这些方法
// 但保留空函数以避免引用错误
const startDragPreview = () => {};
const updateDragPreviewPosition = () => {};
const stopDragPreview = () => {};

// 切换搜索框显示
const toggleSearch = () => {
  showSearch.value = !showSearch.value;
  if (!showSearch.value) {
    searchQuery.value = '';
  }
};

// 切换展开/折叠
const toggleExpand = () => {
  // 这里可以实现展开/折叠功能
  // 例如，切换组件列表的显示模式
};

// 处理搜索
const handleSearch = () => {
  emit('search', searchQuery.value);
};

// 显示组件信息
const showComponentInfo = (component: ComponentType) => {
  selectedComponent.value = component;
  componentInfoVisible.value = true;
};

// 生命周期
onMounted(() => {
  // 初始化事件监听
  document.addEventListener('dragover', preventDefaultDragOver, true);
  
  // 注册优化器事件处理
  dragDropOptimizer.on('position-updated', () => {
    // 位置更新由优化器内部处理，这里可以添加额外逻辑
  });
});

onUnmounted(() => {
  // 清理事件监听
  document.removeEventListener('dragover', preventDefaultDragOver, true);
  
  // 清理优化器事件监听
  dragDropOptimizer.off('position-updated');
});

// 阻止默认的拖拽行为
const preventDefaultDragOver = (event: DragEvent) => {
  // 只阻止非目标区域的拖拽行为
  if (!event.target || !(event.target as Element).closest('.drop-target')) {
    event.preventDefault();
  }
};

// 监听组件列表变化
watch(
  () => props.components,
  () => {
    // 当组件列表变化时，可以进行一些清理工作
    if (!filteredComponents.value.length) {
      activeCategory.value = 'all';
    }
  },
  { deep: true }
);
</script>

<style scoped>
.component-drag-drop-manager {
  display: flex;
  height: 100%;
  position: relative;
}

/* 拖拽源（组件列表） */
.drag-source {
  width: 300px;
  height: 100%;
  background-color: #fff;
  border-right: 1px solid #e4e7ed;
  display: flex;
  flex-direction: column;
  box-shadow: 2px 0 8px rgba(0, 0, 0, 0.05);
}

/* 源头部 */
.source-header {
  height: 60px;
  padding: 0 16px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  border-bottom: 1px solid #e4e7ed;
  background-color: #fafafa;
}

.source-header h3 {
  margin: 0;
  font-size: 16px;
  font-weight: 500;
  color: #303133;
}

.source-actions {
  display: flex;
  gap: 8px;
}

/* 搜索框 */
.search-box {
  padding: 10px 16px;
  border-bottom: 1px solid #e4e7ed;
}

/* 分类标签 */
.category-tabs {
  border-bottom: 1px solid #e4e7ed;
  max-height: 80px;
}

.category-scrollbar {
  height: 100%;
}

.category-list {
  display: flex;
  flex-wrap: wrap;
  padding: 10px;
  gap: 8px;
}

.category-tag {
  cursor: pointer;
  margin-bottom: 8px;
}

.category-tag:hover {
  transform: translateY(-1px);
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.component-count {
  font-size: 12px;
  opacity: 0.7;
}

/* 组件列表 */
.component-list-scrollbar {
  flex: 1;
  height: 0;
}

.component-list {
  padding: 10px;
}

/* 无组件提示 */
.no-components {
  padding: 40px 20px;
}

/* 组件项 */
.component-item {
  display: flex;
  align-items: center;
  padding: 12px;
  margin-bottom: 8px;
  background-color: #fff;
  border: 1px solid #e4e7ed;
  border-radius: 6px;
  cursor: grab;
  transition: all 0.3s ease;
  user-select: none;
}

.component-item:hover,
.component-item:hover.hovered {
  background-color: #f5f7fa;
  border-color: #409eff;
  box-shadow: 0 2px 8px rgba(64, 158, 255, 0.2);
  transform: translateY(-1px);
}

.component-item.dragging {
  opacity: 0.5;
  cursor: grabbing;
}

/* 组件图标 */
.component-icon {
  width: 40px;
  height: 40px;
  background-color: #ecf5ff;
  color: #409eff;
  border-radius: 4px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  margin-right: 12px;
  flex-shrink: 0;
}

/* 组件信息 */
.component-info {
  flex: 1;
  min-width: 0;
}

.component-name {
  font-size: 14px;
  font-weight: 500;
  color: #303133;
  margin-bottom: 4px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.component-desc {
  font-size: 12px;
  color: #909399;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

/* 组件操作 */
.component-actions {
  flex-shrink: 0;
  opacity: 0;
  transition: opacity 0.3s ease;
}

.component-item:hover .component-actions {
  opacity: 1;
}

/* 拖放目标区域 */
.drop-target {
  flex: 1;
  min-height: 200px;
  position: relative;
  transition: all 0.3s ease;
  overflow: hidden;
}

.drop-target.active {
  border: 2px dashed #409eff;
  background-color: #ecf5ff;
}

.drop-target.dragging-over {
  border-color: #67c23a;
  background-color: #f0f9eb;
  box-shadow: inset 0 0 20px rgba(103, 194, 58, 0.1);
}

.drop-target.disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

/* 拖放提示 */
.drop-hint {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background-color: rgba(103, 194, 58, 0.05);
  z-index: 10;
}

.hint-content {
  text-align: center;
  color: #67c23a;
  font-size: 16px;
  padding: 20px;
  border: 2px dashed #67c23a;
  border-radius: 8px;
  background-color: #fff;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
  animation: pulse 2s infinite;
}

.hint-content i {
  font-size: 32px;
  margin-bottom: 10px;
  display: block;
}

@keyframes pulse {
  0% {
    box-shadow: 0 0 0 0 rgba(103, 194, 58, 0.4);
  }
  70% {
    box-shadow: 0 0 0 10px rgba(103, 194, 58, 0);
  }
  100% {
    box-shadow: 0 0 0 0 rgba(103, 194, 58, 0);
  }
}

/* 拖拽预览 */
.drag-preview {
  position: fixed;
  pointer-events: none;
  z-index: 9999;
  opacity: 0.9;
  animation: float 0.5s infinite alternate;
}

.preview-content {
  display: flex;
  align-items: center;
  padding: 8px 12px;
  background-color: #409eff;
  color: #fff;
  border-radius: 4px;
  box-shadow: 0 2px 8px rgba(64, 158, 255, 0.3);
  font-size: 14px;
}

.preview-icon {
  margin-right: 8px;
  font-size: 16px;
}

@keyframes float {
  from {
    transform: translateY(0px);
  }
  to {
    transform: translateY(-4px);
  }
}

/* 组件信息对话框 */
.component-info-dialog {
  max-height: 500px;
  overflow-y: auto;
}

.props-section,
.events-section {
  margin-top: 20px;
}

.props-section h4,
.events-section h4 {
  margin: 0 0 10px 0;
  font-size: 14px;
  color: #606266;
  border-bottom: 1px solid #ebeef5;
  padding-bottom: 5px;
}

/* 响应式调整 */
@media (max-width: 768px) {
  .component-drag-drop-manager {
    flex-direction: column;
  }
  
  .drag-source {
    width: 100%;
    height: 300px;
    border-right: none;
    border-bottom: 1px solid #e4e7ed;
  }
  
  .drop-target {
    min-height: 300px;
  }
}
</style>