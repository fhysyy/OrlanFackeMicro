<template>
  <div class="component-panel">
    <div class="panel-header">
      <h3>组件库</h3>
      <el-input
        v-model="searchQuery"
        placeholder="搜索组件"
        prefix-icon="Search"
        clearable
        size="small"
      />
    </div>

    <div class="panel-content">
      <el-tabs v-model="activeCategory" class="component-tabs" type="card">
        <el-tab-pane
          v-for="category in componentCategories"
          :key="category.key"
          :label="category.name"
          :name="category.key"
        >
          <ul class="component-list">
            <li
              v-for="component in filteredComponents(category.key)"
              :key="component.name"
              class="component-item"
              :draggable="true"
              @dragstart="handleDragStart($event, component)"
              @mouseenter="showComponentInfo(component)"
              @mouseleave="hideComponentInfo"
            >
              <div class="component-icon">
                <el-icon v-if="component.icon"><component :is="component.icon" /></el-icon>
                <div v-else class="default-icon">{{ component.name.charAt(0) }}</div>
              </div>
              <div class="component-name">{{ component.displayName || component.name }}</div>
            </li>
          </ul>
        </el-tab-pane>
      </el-tabs>
    </div>

    <!-- 组件信息提示 -->
    <el-tooltip
      v-model="showTooltip"
      :content="tooltipContent"
      placement="right"
      effect="dark"
      :show-after="300"
      :hide-after="3000"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { Search } from '@element-plus/icons-vue'
import type { ComponentType } from '@/types/page'
import { getComponentCategories, getComponentsByCategory } from '@/services/componentRegistry'

// Props
interface Props {
  // 组件注册器中已定义的组件类型
}

const props = defineProps<Props>()

// Emits
interface Emits {
  componentDragStart: [component: any, event: DragEvent]
}

const emit = defineEmits<Emits>()

// Local state
const searchQuery = ref('')
const activeCategory = ref('basic')
const showTooltip = ref(false)
const tooltipContent = ref('')

// Get component categories and components
const componentCategories = ref(getComponentCategories())

// Computed
const filteredComponents = (category: string) => {
  let components = getComponentsByCategory(category)
  
  if (searchQuery.value.trim()) {
    const query = searchQuery.value.toLowerCase()
    components = components.filter(comp => 
      (comp.name && comp.name.toLowerCase().includes(query)) ||
      (comp.displayName && comp.displayName.toLowerCase().includes(query)) ||
      (comp.description && comp.description.toLowerCase().includes(query))
    )
  }
  
  return components
}

// Methods
const handleDragStart = (event: DragEvent, component: any) => {
  if (event.dataTransfer) {
    // 设置拖拽数据
    event.dataTransfer.setData('application/json', JSON.stringify({
      type: 'component',
      component
    }))
    
    // 设置拖拽效果
    event.dataTransfer.effectAllowed = 'copy'
    
    // 触发外部事件
    emit('componentDragStart', component, event)
    
    // 设置拖拽时的样式
    setTimeout(() => {
      const target = event.target as HTMLElement
      if (target) {
        target.style.opacity = '0.5'
      }
    }, 0)
  }
}

const showComponentInfo = (component: any) => {
  const info = []
  if (component.displayName) info.push(component.displayName)
  if (component.description) info.push(component.description)
  if (component.properties && Object.keys(component.properties).length > 0) {
    info.push(`属性数量: ${Object.keys(component.properties).length}`)
  }
  tooltipContent.value = info.join('\n')
  showTooltip.value = true
}

const hideComponentInfo = () => {
  showTooltip.value = false
}

// 拖拽结束时恢复样式
const handleDragEnd = (event: DragEvent) => {
  const target = event.target as HTMLElement
  if (target) {
    target.style.opacity = '1'
  }
}

// 监听拖拽结束事件 (需要在组件挂载后绑定到文档上)
import { onMounted, onUnmounted } from 'vue'

onMounted(() => {
  document.addEventListener('dragend', handleDragEnd)
})

onUnmounted(() => {
  document.removeEventListener('dragend', handleDragEnd)
})
</script>

<style scoped>
.component-panel {
  width: 280px;
  height: 100%;
  background: #fff;
  border-right: 1px solid #e4e7ed;
  display: flex;
  flex-direction: column;
}

.panel-header {
  padding: 16px;
  border-bottom: 1px solid #f0f2f5;
}

.panel-header h3 {
  margin: 0 0 12px 0;
  font-size: 16px;
  font-weight: 500;
  color: #303133;
}

.panel-content {
  flex: 1;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.component-tabs {
  height: 100%;
  display: flex;
  flex-direction: column;
}

:deep(.el-tabs__header) {
  border-bottom: 1px solid #f0f2f5;
  padding: 0 16px;
}

:deep(.el-tabs__content) {
  flex: 1;
  overflow-y: auto;
  padding: 16px;
}

.component-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 12px;
}

.component-item {
  padding: 12px;
  border: 1px solid #e4e7ed;
  border-radius: 4px;
  cursor: move;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 80px;
  transition: all 0.3s;
  position: relative;
}

.component-item:hover {
  border-color: #409eff;
  box-shadow: 0 2px 8px rgba(64, 158, 255, 0.1);
  transform: translateY(-1px);
}

.component-item:active {
  transform: translateY(0);
}

.component-icon {
  font-size: 24px;
  margin-bottom: 8px;
  color: #606266;
}

.default-icon {
  width: 24px;
  height: 24px;
  background: #f0f2f5;
  border-radius: 4px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 16px;
  color: #909399;
}

.component-name {
  font-size: 12px;
  color: #606266;
  text-align: center;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  width: 100%;
}

/* 拖拽时的样式 */
.component-item[draggable="true"] {
  user-select: none;
}

.component-item[draggable="true"]:active {
  opacity: 0.8;
}

/* 滚动条样式 */
:deep(.el-tabs__content::-webkit-scrollbar) {
  width: 6px;
}

:deep(.el-tabs__content::-webkit-scrollbar-track) {
  background: #f0f2f5;
}

:deep(.el-tabs__content::-webkit-scrollbar-thumb) {
  background: #c0c4cc;
  border-radius: 3px;
}

:deep(.el-tabs__content::-webkit-scrollbar-thumb:hover) {
  background: #909399;
}
</style>