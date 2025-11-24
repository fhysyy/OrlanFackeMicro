<template>
  <div class="designer-workspace">
    <!-- 顶部工具栏 -->
    <DesignerToolbar
      :can-undo="canUndo"
      :can-redo="canRedo"
      :zoom="zoom"
      :show-grid="showGrid"
      :saving="saving"
      @save="handleSave"
      @preview="handlePreview"
      @export="handleExport"
      @import="handleImport"
      @undo="handleUndo"
      @redo="handleRedo"
      @zoom-change="handleZoomChange"
      @toggle-grid="toggleGrid"
      @clear-selection="clearSelection"
      @reset="handleReset"
    />

    <!-- 主工作区域 -->
    <div class="workspace-main">
      <!-- 左侧组件面板 -->
      <DesignerComponentPanel
        @component-drag-start="handleComponentDragStart"
      />

      <!-- 中间设计区域 -->
      <div class="design-area-wrapper">
        <div class="design-area-controls">
          <div class="device-selector">
            <el-radio-group v-model="deviceType" size="small">
              <el-radio-button label="desktop">
                <el-icon><Monitor /></el-icon>
                桌面
              </el-radio-button>
              <el-radio-button label="tablet">
                <el-icon><Tablet /></el-icon>
                平板
              </el-radio-button>
              <el-radio-button label="mobile">
                <el-icon><Smartphone /></el-icon>
                手机
              </el-radio-button>
            </el-radio-group>
          </div>
          <div class="layout-info">
            {{ selectedComponent ? `选中: ${selectedComponent.displayName || selectedComponent.name}` : '未选中组件' }}
          </div>
        </div>
        
        <div class="design-area"
             :class="{ 'show-grid': showGrid }"
             @dragover="handleDragOver"
             @drop="handleDrop"
             @click="handleDesignAreaClick"
             ref="designAreaRef"
        >
          <!-- 渲染根容器 -->
          <div 
            class="root-container"
            :style="{
              transform: `scale(${zoom / 100})`,
              transformOrigin: 'top left',
              width: getContainerWidth(),
              minHeight: '600px'
            }"
          >
            <!-- 动态渲染组件树 -->
            <ComponentRenderer
              :component-config="currentPageConfig"
              :parent-scope="pageScope"
              @component-select="handleComponentSelect"
              @component-config-change="handleComponentConfigChange"
              @component-move="handleComponentMove"
              @component-delete="handleComponentDelete"
              @component-children-change="handleComponentChildrenChange"
            />
          </div>
        </div>
      </div>

      <!-- 右侧属性面板 -->
      <ComponentPropertyEditor
        v-if="selectedComponent"
        :component-config="selectedComponent"
        @update:component-config="updateSelectedComponent"
      />
      <div v-else class="empty-property-panel">
        <el-empty description="请选择一个组件来编辑属性" />
      </div>
    </div>

    <!-- 预览对话框 -->
    <el-dialog
      v-model="previewVisible"
      title="页面预览"
      :width="'90%'"
      :height="'90%'"
      destroy-on-close
    >
      <ComponentPreviewer
        :page-config="currentPageConfig"
        :device-type="previewDeviceType"
      />
      <template #footer>
        <el-button @click="previewVisible = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, onUnmounted } from 'vue'
import { Monitor, Tablet, Smartphone } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import DesignerToolbar from './DesignerToolbar.vue'
import DesignerComponentPanel from './DesignerComponentPanel.vue'
import ComponentPropertyEditor from './ComponentPropertyEditor.vue'
import ComponentPreviewer from './ComponentPreviewer.vue'
import ComponentRenderer from './ComponentRenderer.vue'
import type { ComponentConfig, PageConfig, DeviceType } from '@/types/page'
import { generateComponentConfig } from '@/services/pageBuilderService'
import { getComponentById } from '@/services/componentRegistry'

// Props
interface Props {
  initialPageConfig?: PageConfig
}

const props = withDefaults(defineProps<Props>(), {
  initialPageConfig: () => ({
    type: 'Container',
    id: 'root',
    properties: {},
    children: []
  })
})

// Emits
interface Emits {
  update: [pageConfig: PageConfig]
  save: [pageConfig: PageConfig]
}

const emit = defineEmits<Emits>()

// Local state
const currentPageConfig = ref<PageConfig>(props.initialPageConfig)
const selectedComponent = ref<ComponentConfig | null>(null)
const selectedComponentPath = ref<string[]>([])
const canUndo = ref(false)
const canRedo = ref(false)
const zoom = ref(100)
const showGrid = ref(true)
const deviceType = ref<DeviceType>('desktop')
const previewVisible = ref(false)
const previewDeviceType = ref<DeviceType>('desktop')
const saving = ref(false)
const designAreaRef = ref<HTMLElement>()

// 页面作用域，用于组件间数据共享
const pageScope = reactive({
  state: {},
  methods: {},
  dataSources: {}
})

// 历史记录管理
const historyStack = ref<PageConfig[]>([])
const historyIndex = ref(-1)
const MAX_HISTORY = 50

// Computed
const getContainerWidth = () => {
  switch (deviceType.value) {
    case 'desktop':
      return '1200px'
    case 'tablet':
      return '768px'
    case 'mobile':
      return '375px'
    default:
      return '1200px'
  }
}

// Methods
const addToHistory = () => {
  // 移除当前索引之后的历史记录
  if (historyIndex.value < historyStack.value.length - 1) {
    historyStack.value = historyStack.value.slice(0, historyIndex.value + 1)
  }
  
  // 添加当前配置到历史记录
  historyStack.value.push(JSON.parse(JSON.stringify(currentPageConfig.value)))
  
  // 限制历史记录数量
  if (historyStack.value.length > MAX_HISTORY) {
    historyStack.value.shift()
  } else {
    historyIndex.value++
  }
  
  updateHistoryState()
}

const updateHistoryState = () => {
  canUndo.value = historyIndex.value > 0
  canRedo.value = historyIndex.value < historyStack.value.length - 1
}

const handleUndo = () => {
  if (canUndo.value) {
    historyIndex.value--
    currentPageConfig.value = JSON.parse(JSON.stringify(historyStack.value[historyIndex.value]))
    clearSelection()
    updateHistoryState()
    emit('update', currentPageConfig.value)
  }
}

const handleRedo = () => {
  if (canRedo.value) {
    historyIndex.value++
    currentPageConfig.value = JSON.parse(JSON.stringify(historyStack.value[historyIndex.value]))
    clearSelection()
    updateHistoryState()
    emit('update', currentPageConfig.value)
  }
}

const handleComponentDragStart = (component: any, event: DragEvent) => {
  // 可以在这里添加拖拽时的额外逻辑
}

const handleDragOver = (event: DragEvent) => {
  event.preventDefault()
  event.dataTransfer!.dropEffect = 'copy'
}

const handleDrop = (event: DragEvent) => {
  event.preventDefault()
  
  try {
    const data = JSON.parse(event.dataTransfer!.getData('application/json'))
    
    if (data.type === 'component') {
      // 生成新组件配置
      const newComponent = generateComponentConfig(data.component.name)
      
      // 添加到根容器
      addComponentToParent(newComponent, currentPageConfig.value)
    }
  } catch (error) {
    console.error('拖拽失败:', error)
  }
}

const addComponentToParent = (component: ComponentConfig, parent: ComponentConfig) => {
  addToHistory()
  
  if (!parent.children) {
    parent.children = []
  }
  
  parent.children.push(component)
  selectedComponent.value = component
  selectedComponentPath.value = ['children', parent.children.length - 1.toString()]
  
  emit('update', currentPageConfig.value)
  ElMessage.success('组件添加成功')
}

const handleDesignAreaClick = (event: MouseEvent) => {
  // 如果点击的是设计区域空白处，清除选择
  const target = event.target as HTMLElement
  if (target === designAreaRef.value || target.closest('.design-area')) {
    clearSelection()
  }
}

const handleComponentSelect = (component: ComponentConfig, path: string[]) => {
  selectedComponent.value = component
  selectedComponentPath.value = path
}

const handleComponentConfigChange = (component: ComponentConfig) => {
  addToHistory()
  emit('update', currentPageConfig.value)
}

const handleComponentMove = (fromIndex: number, toIndex: number, parent: ComponentConfig) => {
  addToHistory()
  
  if (parent.children && fromIndex !== toIndex) {
    const [movedComponent] = parent.children.splice(fromIndex, 1)
    parent.children.splice(toIndex, 0, movedComponent)
    emit('update', currentPageConfig.value)
    ElMessage.success('组件位置已调整')
  }
}

const handleComponentDelete = (component: ComponentConfig, parent: ComponentConfig) => {
  addToHistory()
  
  if (parent.children) {
    const index = parent.children.findIndex(c => c.id === component.id)
    if (index > -1) {
      parent.children.splice(index, 1)
      clearSelection()
      emit('update', currentPageConfig.value)
      ElMessage.success('组件已删除')
    }
  }
}

const handleComponentChildrenChange = (parent: ComponentConfig, children: ComponentConfig[]) => {
  addToHistory()
  parent.children = children
  emit('update', currentPageConfig.value)
}

const updateSelectedComponent = (newConfig: ComponentConfig) => {
  addToHistory()
  
  // 深度合并配置
  Object.assign(selectedComponent.value!, newConfig)
  emit('update', currentPageConfig.value)
}

const clearSelection = () => {
  selectedComponent.value = null
  selectedComponentPath.value = []
}

const handleZoomChange = (newZoom: number) => {
  zoom.value = newZoom
}

const toggleGrid = () => {
  showGrid.value = !showGrid.value
}

const handleSave = async () => {
  saving.value = true
  try {
    emit('save', currentPageConfig.value)
    ElMessage.success('保存成功')
  } catch (error) {
    ElMessage.error('保存失败')
  } finally {
    saving.value = false
  }
}

const handlePreview = () => {
  previewDeviceType.value = deviceType.value
  previewVisible.value = true
}

const handleExport = () => {
  const dataStr = JSON.stringify(currentPageConfig.value, null, 2)
  const dataUri = 'data:application/json;charset=utf-8,'+ encodeURIComponent(dataStr)
  
  const exportFileDefaultName = `page-config-${new Date().getTime()}.json`
  
  const linkElement = document.createElement('a')
  linkElement.setAttribute('href', dataUri)
  linkElement.setAttribute('download', exportFileDefaultName)
  linkElement.click()
}

const handleImport = (data: PageConfig) => {
  addToHistory()
  currentPageConfig.value = data
  clearSelection()
  emit('update', currentPageConfig.value)
}

const handleReset = () => {
  addToHistory()
  currentPageConfig.value = {
    type: 'Container',
    id: 'root',
    properties: {},
    children: []
  }
  clearSelection()
  emit('update', currentPageConfig.value)
}

// 初始化历史记录
onMounted(() => {
  addToHistory()
})
</script>

<style scoped>
.designer-workspace {
  height: 100vh;
  display: flex;
  flex-direction: column;
  background: #f5f7fa;
}

.workspace-main {
  flex: 1;
  display: flex;
  overflow: hidden;
}

.design-area-wrapper {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  background: #f0f2f5;
}

.design-area-controls {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 16px;
  background: #fff;
  border-bottom: 1px solid #e4e7ed;
}

.layout-info {
  font-size: 14px;
  color: #606266;
}

.design-area {
  flex: 1;
  overflow: auto;
  padding: 20px;
  position: relative;
}

.design-area.show-grid {
  background-image: 
    linear-gradient(to right, rgba(200, 200, 200, 0.1) 1px, transparent 1px),
    linear-gradient(to bottom, rgba(200, 200, 200, 0.1) 1px, transparent 1px);
  background-size: 20px 20px;
}

.root-container {
  background: #fff;
  border: 1px solid #e4e7ed;
  border-radius: 4px;
  margin: 0 auto;
  position: relative;
  box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
}

.empty-property-panel {
  width: 320px;
  height: 100%;
  background: #fff;
  border-left: 1px solid #e4e7ed;
  display: flex;
  align-items: center;
  justify-content: center;
}

/* 滚动条样式 */
.design-area::-webkit-scrollbar {
  width: 8px;
  height: 8px;
}

.design-area::-webkit-scrollbar-track {
  background: #f0f2f5;
}

.design-area::-webkit-scrollbar-thumb {
  background: #c0c4cc;
  border-radius: 4px;
}

.design-area::-webkit-scrollbar-thumb:hover {
  background: #909399;
}
</style>