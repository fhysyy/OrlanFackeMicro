<template>
  <div class="designer-toolbar">
    <div class="toolbar-left">
      <el-button type="primary" @click="saveDesign" :loading="saving">
        <el-icon><Save /></el-icon>
        保存设计
      </el-button>
      <el-button @click="previewDesign">
        <el-icon><View /></el-icon>
        预览
      </el-button>
      <el-button @click="exportDesign">
        <el-icon><Download /></el-icon>
        导出
      </el-button>
      <el-upload
        class="upload-btn"
        :show-file-list="false"
        accept=".json"
        :before-upload="handleUpload"
      >
        <el-button>
          <el-icon><Upload /></el-icon>
          导入
        </el-button>
      </el-upload>
    </div>

    <div class="toolbar-center">
      <el-button-group>
        <el-button :disabled="!canUndo" @click="undo">
          <el-icon><Back /></el-icon>
          撤销
        </el-button>
        <el-button :disabled="!canRedo" @click="redo">
          <el-icon><Right /></el-icon>
          重做
        </el-button>
      </el-button-group>
      
      <el-divider direction="vertical" />
      
      <el-button-group>
        <el-button @click="zoomOut" :disabled="zoom <= 50">
          <el-icon><ZoomOut /></el-icon>
        </el-button>
        <el-input-number
          v-model="zoom"
          :min="50"
          :max="200"
          :step="10"
          @change="handleZoomChange"
          size="small"
        />
        <el-button @click="zoomIn" :disabled="zoom >= 200">
          <el-icon><ZoomIn /></el-icon>
        </el-button>
      </el-button-group>
    </div>

    <div class="toolbar-right">
      <el-button @click="toggleGrid" :type="showGrid ? 'primary' : 'default'">
        <el-icon><Grid /></el-icon>
        网格
      </el-button>
      <el-button @click="clearSelection">
        <el-icon><CloseBold /></el-icon>
        清除选择
      </el-button>
      <el-button type="danger" @click="resetDesign" size="small">
        <el-icon><RefreshLeft /></el-icon>
        重置
      </el-button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { Save, View, Download, Upload, Back, Right, ZoomOut, ZoomIn, Grid, CloseBold, RefreshLeft } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { UploadProps } from 'element-plus'

// Props
interface Props {
  canUndo?: boolean
  canRedo?: boolean
  zoom?: number
  showGrid?: boolean
  saving?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  canUndo: false,
  canRedo: false,
  zoom: 100,
  showGrid: false,
  saving: false
})

// Emits
interface Emits {
  save: []
  preview: []
  export: []
  import: [data: any]
  undo: []
  redo: []
  zoomChange: [zoom: number]
  toggleGrid: []
  clearSelection: []
  reset: []
}

const emit = defineEmits<Emits>()

// Local state
const localZoom = ref(props.zoom)

// Computed
const zoom = computed({
  get: () => localZoom.value,
  set: (value) => {
    localZoom.value = value
    emit('zoomChange', value)
  }
})

// Methods
const saveDesign = () => {
  emit('save')
}

const previewDesign = () => {
  emit('preview')
}

const exportDesign = () => {
  emit('export')
}

const handleUpload: UploadProps['beforeUpload'] = (file) => {
  const reader = new FileReader()
  reader.onload = (e) => {
    try {
      const content = e.target?.result as string
      const data = JSON.parse(content)
      emit('import', data)
      ElMessage.success('导入成功')
    } catch (error) {
      ElMessage.error('导入失败，请检查文件格式')
    }
  }
  reader.readAsText(file)
  return false // 阻止自动上传
}

const undo = () => {
  emit('undo')
}

const redo = () => {
  emit('redo')
}

const zoomIn = () => {
  if (zoom.value < 200) {
    zoom.value += 10
  }
}

const zoomOut = () => {
  if (zoom.value > 50) {
    zoom.value -= 10
  }
}

const handleZoomChange = (value: number) => {
  emit('zoomChange', value)
}

const toggleGrid = () => {
  emit('toggleGrid')
}

const clearSelection = () => {
  emit('clearSelection')
}

const resetDesign = async () => {
  try {
    await ElMessageBox.confirm('确定要重置设计吗？这将清除所有已编辑的内容，且无法恢复。', '确认重置', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    emit('reset')
  } catch {
    // 用户取消操作
  }
}
</script>

<style scoped>
.designer-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 16px;
  height: 56px;
  background: #fff;
  border-bottom: 1px solid #e4e7ed;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
}

.toolbar-left,
.toolbar-center,
.toolbar-right {
  display: flex;
  align-items: center;
  gap: 8px;
}

.toolbar-center {
  position: relative;
}

.upload-btn {
  display: inline-block;
}

:deep(.el-divider--vertical) {
  height: 32px;
  margin: 0 8px;
}

:deep(.el-input-number) {
  width: 80px;
}

:deep(.el-input-number__decrease),
:deep(.el-input-number__increase) {
  height: 16px;
}
</style>