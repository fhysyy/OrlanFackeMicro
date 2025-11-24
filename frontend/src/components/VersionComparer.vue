<template>
  <div class="version-comparer">
    <!-- 版本选择区域 -->
    <div class="comparer-header">
      <div class="version-selector-container">
        <div class="version-selector">
          <el-form-item label="左侧版本" label-width="80px">
            <el-select
              v-model="leftVersionId"
              placeholder="选择左侧版本"
              @change="handleVersionSelectionChange"
              filterable
              clearable
            >
              <el-option
                v-for="version in versions"
                :key="version.id"
                :label="getVersionLabel(version)"
                :value="version.id"
              >
                <div class="version-option-content">
                  <div class="option-name">{{ version.name }}</div>
                  <div class="option-meta">
                    {{ formatDateTime(version.timestamp) }} · {{ version.userName }}
                  </div>
                </div>
              </el-option>
            </el-select>
          </el-form-item>
        </div>
        
        <div class="comparer-arrow">
          <el-button
            type="text"
            @click="swapVersions"
            icon="el-icon-swap-left"
            :loading="isLoading || isSwapping"
            title="交换版本"
          ></el-button>
        </div>
        
        <div class="version-selector">
          <el-form-item label="右侧版本" label-width="80px">
            <el-select
              v-model="rightVersionId"
              placeholder="选择右侧版本"
              @change="handleVersionSelectionChange"
              filterable
              clearable
            >
              <el-option
                v-for="version in versions"
                :key="version.id"
                :label="getVersionLabel(version)"
                :value="version.id"
              >
                <div class="version-option-content">
                  <div class="option-name">{{ version.name }}</div>
                  <div class="option-meta">
                    {{ formatDateTime(version.timestamp) }} · {{ version.userName }}
                  </div>
                </div>
              </el-option>
            </el-select>
          </el-form-item>
        </div>
      </div>
      
      <div class="comparer-actions">
        <el-button
          type="primary"
          @click="compareVersions"
          :disabled="!canCompare"
          :loading="isLoading"
        >
          比较版本
        </el-button>
        <el-button
          @click="showPreview = !showPreview"
          :disabled="!hasDiff"
          :type="showPreview ? 'primary' : 'default'"
        >
          {{ showPreview ? '隐藏预览' : '显示预览' }}
        </el-button>
      </div>
    </div>

    <!-- 加载状态 -->
    <div v-if="isLoading" class="loading-container">
      <el-skeleton :rows="5" animated />
    </div>

    <!-- 无差异提示 -->
    <div v-else-if="hasCompared && !hasDiff" class="no-diff-container">
      <el-empty
        description="两个版本之间没有差异"
        :image-size="100"
      />
      <div class="no-diff-message">
        您选择的两个版本完全相同，无需进行比较。
      </div>
    </div>

    <!-- 比较结果区域 -->
    <div v-else-if="hasCompared" class="comparison-result">
      <!-- 差异统计 -->
      <div class="diff-summary">
        <el-steps :active="-1" finish-status="success">
          <el-step
            title="组件变更"
            :description="`${diffStats.addedComponents} 新增 · ${diffStats.modifiedComponents} 修改 · ${diffStats.deletedComponents} 删除`"
          ></el-step>
          <el-step
            title="属性变更"
            :description="`${diffStats.modifiedProperties} 处属性修改`"
          ></el-step>
          <el-step
            title="样式变更"
            :description="`${diffStats.modifiedStyles} 处样式修改`"
          ></el-step>
        </el-steps>

        <!-- 过滤选项 -->
        <div class="diff-filters">
          <el-tag
            v-for="filter in filterOptions"
            :key="filter.value"
            :effect="filter.active ? 'dark' : 'plain'"
            :type="getFilterTypeColor(filter.value)"
            @click="toggleFilter(filter.value)"
            closable
            :closable="false"
          >
            {{ filter.label }}
          </el-tag>
          <el-button
            type="text"
            size="small"
            @click="clearAllFilters"
            v-if="activeFilters.length > 0"
          >
            清除筛选
          </el-button>
        </div>
      </div>

      <!-- 比较结果主区域 -->
      <div class="diff-content">
        <!-- 预览模式 -->
        <div v-if="showPreview" class="preview-mode">
          <div class="preview-container">
            <div class="preview-header">
              <div class="preview-title">{{ getVersionName(leftVersion) }}</div>
              <div class="preview-meta">{{ formatDateTime(leftVersion?.timestamp) }}</div>
            </div>
            <div class="preview-content">
              <el-button
                type="primary"
                size="small"
                @click="restoreVersion(leftVersionId)"
                :disabled="!leftVersionId"
              >
                恢复此版本
              </el-button>
              <pre class="preview-code">{{ getPreviewJson(leftConfig) }}</pre>
            </div>
          </div>
          
          <div class="preview-separator">→</div>
          
          <div class="preview-container">
            <div class="preview-header">
              <div class="preview-title">{{ getVersionName(rightVersion) }}</div>
              <div class="preview-meta">{{ formatDateTime(rightVersion?.timestamp) }}</div>
            </div>
            <div class="preview-content">
              <el-button
                type="primary"
                size="small"
                @click="restoreVersion(rightVersionId)"
                :disabled="!rightVersionId"
              >
                恢复此版本
              </el-button>
              <pre class="preview-code">{{ getPreviewJson(rightConfig) }}</pre>
            </div>
          </div>
        </div>

        <!-- 差异列表模式 -->
        <div v-else class="diff-list">
          <!-- 组件新增 -->
          <div v-if="filteredChanges.added.length > 0" class="diff-section">
            <h3 class="diff-section-title">
              <el-tag type="success" size="small">新增</el-tag>
              <span>组件</span>
              <el-badge :value="filteredChanges.added.length" class="section-count" />
            </h3>
            
            <el-tree
              :data="filteredChanges.added"
              :props="treeProps"
              node-key="id"
              default-expand-all
              @node-click="handleNodeClick"
            >
              <template #default="{ node, data }">
                <span class="tree-node-content" :class="{ 'node-selected': selectedNode === data }">
                  <i class="el-icon-plus" style="color: #67c23a; margin-right: 5px;"></i>
                  {{ getNodeLabel(data) }}
                </span>
              </template>
            </el-tree>
          </div>

          <!-- 组件修改 -->
          <div v-if="filteredChanges.modified.length > 0" class="diff-section">
            <h3 class="diff-section-title">
              <el-tag type="warning" size="small">修改</el-tag>
              <span>组件</span>
              <el-badge :value="filteredChanges.modified.length" class="section-count" />
            </h3>
            
            <el-tree
              :data="filteredChanges.modified"
              :props="treeProps"
              node-key="id"
              @node-click="handleNodeClick"
            >
              <template #default="{ node, data }">
                <span class="tree-node-content" :class="{ 'node-selected': selectedNode === data }">
                  <i class="el-icon-edit" style="color: #e6a23c; margin-right: 5px;"></i>
                  {{ getNodeLabel(data) }}
                </span>
              </template>
            </el-tree>
          </div>

          <!-- 组件删除 -->
          <div v-if="filteredChanges.deleted.length > 0" class="diff-section">
            <h3 class="diff-section-title">
              <el-tag type="danger" size="small">删除</el-tag>
              <span>组件</span>
              <el-badge :value="filteredChanges.deleted.length" class="section-count" />
            </h3>
            
            <el-tree
              :data="filteredChanges.deleted"
              :props="treeProps"
              node-key="id"
              @node-click="handleNodeClick"
            >
              <template #default="{ node, data }">
                <span class="tree-node-content" :class="{ 'node-selected': selectedNode === data }">
                  <i class="el-icon-delete" style="color: #f56c6c; margin-right: 5px;"></i>
                  {{ getNodeLabel(data) }}
                </span>
              </template>
            </el-tree>
          </div>

          <!-- 空状态 -->
          <el-empty
            v-if="!hasFilteredChanges"
            description="没有符合条件的差异"
            :image-size="80"
          />
        </div>
      </div>

      <!-- 属性差异详情 -->
      <div v-if="selectedNode && selectedNode.diffs && selectedNode.diffs.length > 0" class="property-diff-detail">
        <h3 class="diff-section-title">属性差异</h3>
        <el-table :data="selectedNode.diffs" style="width: 100%;">
          <el-table-column prop="property" label="属性" width="180">
            <template #default="{ row }">
              <code>{{ row.property }}</code>
            </template>
          </el-table-column>
          <el-table-column prop="oldValue" label="左侧值">
            <template #default="{ row }">
              <div class="diff-value old-value">
                <pre>{{ formatDiffValue(row.oldValue) }}</pre>
              </div>
            </template>
          </el-table-column>
          <el-table-column prop="newValue" label="右侧值">
            <template #default="{ row }">
              <div class="diff-value new-value">
                <pre>{{ formatDiffValue(row.newValue) }}</pre>
              </div>
            </template>
          </el-table-column>
          <el-table-column label="操作" width="100" fixed="right">
            <template #default="{ row }">
              <el-button
                type="text"
                size="small"
                @click="applyDiff(row, 'left')"
                :disabled="!rightVersionId"
                title="应用左侧值到右侧"
              >
                <i class="el-icon-arrow-right"></i>
              </el-button>
              <el-button
                type="text"
                size="small"
                @click="applyDiff(row, 'right')"
                :disabled="!leftVersionId"
                title="应用右侧值到左侧"
              >
                <i class="el-icon-arrow-left"></i>
              </el-button>
            </template>
          </el-table-column>
        </el-table>
      </div>
    </div>

    <!-- 提示信息 -->
    <div v-else class="initial-message">
      <el-empty
        description="请选择两个版本进行比较"
        :image-size="100"
      />
      <div class="initial-hint">
        从上方选择左侧版本（基准版本）和右侧版本（目标版本），然后点击"比较版本"按钮。
      </div>
    </div>

    <!-- 确认对话框 -->
    <el-dialog
      v-model="showConfirmDialog"
      :title="confirmDialog.title"
      width="400px"
      :close-on-click-modal="false"
    >
      <div class="confirm-content">
        {{ confirmDialog.content }}
      </div>
      <template #footer>
        <el-button @click="showConfirmDialog = false">取消</el-button>
        <el-button
          type="primary"
          @click="handleConfirmAction"
          :loading="isConfirming"
        >
          确认
        </el-button>
      </template>
    </el-dialog>

    <!-- 差异导出对话框 -->
    <el-dialog
      v-model="showExportDialog"
      title="导出差异报告"
      width="400px"
    >
      <el-form
        ref="exportFormRef"
        :model="exportForm"
        :rules="exportFormRules"
        label-width="80px"
      >
        <el-form-item label="报告名称" prop="name">
          <el-input
            v-model="exportForm.name"
            placeholder="请输入报告名称"
            maxlength="50"
            show-word-limit
          />
        </el-form-item>
        <el-form-item label="导出格式">
          <el-radio-group v-model="exportForm.format">
            <el-radio label="json">JSON</el-radio>
            <el-radio label="html">HTML</el-radio>
            <el-radio label="txt">文本</el-radio>
          </el-radio-group>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showExportDialog = false">取消</el-button>
        <el-button
          type="primary"
          @click="handleExportDiff"
          :loading="isExporting"
        >
          导出
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { versionControlService } from '@/services/versionControlService'
import { PageConfig } from '@/types/page'
import type { PageVersion, VersionDiff } from '@/services/versionControlService'

// Props
interface Props {
  pageId: string
  getCurrentConfig: () => PageConfig
  initialLeftVersionId?: string
  initialRightVersionId?: string
}

const props = withDefaults(defineProps<Props>(), {
  initialLeftVersionId: '',
  initialRightVersionId: ''
})

// Emits
interface Emits {
  versionRestored: [version: PageVersion]
  diffExported: [fileName: string]
}

const emit = defineEmits<Emits>()

// 响应式数据
const versions = ref<PageVersion[]>([])
const leftVersionId = ref(props.initialLeftVersionId)
const rightVersionId = ref(props.initialRightVersionId)
const leftVersion = ref<PageVersion | null>(null)
const rightVersion = ref<PageVersion | null>(null)
const leftConfig = ref<PageConfig | null>(null)
const rightConfig = ref<PageConfig | null>(null)
const versionDiff = ref<VersionDiff | null>(null)

// 状态
const isLoading = ref(false)
const isSwapping = ref(false)
const isConfirming = ref(false)
const isExporting = ref(false)
const hasCompared = ref(false)
const showPreview = ref(false)
const selectedNode = ref<any>(null)

// 对话框
const showConfirmDialog = ref(false)
const confirmDialog = reactive({
  title: '',
  content: '',
  action: ''
})

const showExportDialog = ref(false)

// 表单
const exportForm = reactive({
  name: '',
  format: 'json'
})

const exportFormRules = reactive({
  name: [
    { required: true, message: '请输入报告名称', trigger: 'blur' }
  ]
})

// 过滤
const activeFilters = ref<string[]>([])
const filterOptions = ref([
  { value: 'added', label: '新增组件', active: true },
  { value: 'modified', label: '修改组件', active: true },
  { value: 'deleted', label: '删除组件', active: true }
])

// 树形结构配置
const treeProps = {
  children: 'children',
  label: 'name'
}

// 计算属性
const canCompare = computed(() => {
  return leftVersionId.value && rightVersionId.value && leftVersionId.value !== rightVersionId.value
})

const hasDiff = computed(() => {
  return versionDiff.value ? 
    versionDiff.value.added.length > 0 || 
    versionDiff.value.modified.length > 0 || 
    versionDiff.value.deleted.length > 0 : 
    false
})

const filteredChanges = computed(() => {
  if (!versionDiff.value) {
    return { added: [], modified: [], deleted: [] }
  }
  
  return {
    added: activeFilters.value.includes('added') ? versionDiff.value.added : [],
    modified: activeFilters.value.includes('modified') ? versionDiff.value.modified : [],
    deleted: activeFilters.value.includes('deleted') ? versionDiff.value.deleted : []
  }
})

const hasFilteredChanges = computed(() => {
  return filteredChanges.value.added.length > 0 ||
    filteredChanges.value.modified.length > 0 ||
    filteredChanges.value.deleted.length > 0
})

// 方法
const loadVersions = async () => {
  try {
    versions.value = versionControlService.getVersions(props.pageId)
    
    // 如果有初始版本ID，加载对应的版本信息
    if (props.initialLeftVersionId) {
      leftVersion.value = versions.value.find(v => v.id === props.initialLeftVersionId) || null
    }
    if (props.initialRightVersionId) {
      rightVersion.value = versions.value.find(v => v.id === props.initialRightVersionId) || null
    }
  } catch (error) {
    console.error('Failed to load versions:', error)
    ElMessage.error('加载版本列表失败')
  }
}

const handleVersionSelectionChange = async () => {
  // 清除之前的比较结果
  hasCompared.value = false
  versionDiff.value = null
  selectedNode.value = null
  
  // 加载版本信息
  if (leftVersionId.value) {
    leftVersion.value = versions.value.find(v => v.id === leftVersionId.value) || null
  }
  if (rightVersionId.value) {
    rightVersion.value = versions.value.find(v => v.id === rightVersionId.value) || null
  }
}

const swapVersions = () => {
  const temp = leftVersionId.value
  leftVersionId.value = rightVersionId.value
  rightVersionId.value = temp
  handleVersionSelectionChange()
}

const compareVersions = async () => {
  if (!canCompare.value) return
  
  try {
    isLoading.value = true
    hasCompared.value = false
    
    // 获取版本配置
    leftConfig.value = await versionControlService.getVersionConfig(props.pageId, leftVersionId.value)
    rightConfig.value = await versionControlService.getVersionConfig(props.pageId, rightVersionId.value)
    
    // 执行比较
    versionDiff.value = versionControlService.compareVersions(
      leftConfig.value,
      rightConfig.value
    )
    
    hasCompared.value = true
    selectedNode.value = null
    
    // 初始化导出表单名称
    exportForm.name = `版本比较_${getVersionName(leftVersion.value)}_vs_${getVersionName(rightVersion.value)}`
  } catch (error) {
    console.error('Failed to compare versions:', error)
    ElMessage.error('比较版本失败')
  } finally {
    isLoading.value = false
  }
}

const restoreVersion = (versionId: string) => {
  const version = versions.value.find(v => v.id === versionId)
  if (!version) return
  
  confirmDialog.title = '确认恢复版本'
  confirmDialog.content = `确定要恢复版本 "${version.name}" 吗？这将创建一个新的版本，不会覆盖现有版本。`
  confirmDialog.action = 'restore'
  confirmDialog.versionId = versionId
  showConfirmDialog.value = true
}

const handleConfirmAction = async () => {
  if (confirmDialog.action === 'restore' && confirmDialog.versionId) {
    try {
      isConfirming.value = true
      
      const restoredVersion = await versionControlService.restoreVersion(
        props.pageId,
        confirmDialog.versionId
      )
      
      showConfirmDialog.value = false
      ElMessage.success('版本恢复成功')
      emit('versionRestored', restoredVersion)
      
      // 重新加载版本列表
      await loadVersions()
    } catch (error) {
      console.error('Failed to restore version:', error)
      ElMessage.error('版本恢复失败')
    } finally {
      isConfirming.value = false
    }
  }
}

const handleExportDiff = async () => {
  try {
    isExporting.value = true
    
    if (!versionDiff.value || !leftVersion.value || !rightVersion.value) {
      throw new Error('No diff data available')
    }
    
    const exportData = {
      leftVersion: {
        id: leftVersion.value.id,
        name: leftVersion.value.name,
        timestamp: leftVersion.value.timestamp
      },
      rightVersion: {
        id: rightVersion.value.id,
        name: rightVersion.value.name,
        timestamp: rightVersion.value.timestamp
      },
      diff: versionDiff.value,
      generatedAt: Date.now()
    }
    
    let content = ''
    let fileName = `${exportForm.name}.${exportForm.format}`
    let mimeType = ''
    
    switch (exportForm.format) {
      case 'json':
        content = JSON.stringify(exportData, null, 2)
        mimeType = 'application/json'
        break
      case 'html':
        content = generateHtmlReport(exportData)
        mimeType = 'text/html'
        break
      case 'txt':
        content = generateTextReport(exportData)
        mimeType = 'text/plain'
        break
    }
    
    const blob = new Blob([content], { type: mimeType })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = fileName
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
    URL.revokeObjectURL(url)
    
    showExportDialog.value = false
    ElMessage.success('差异报告导出成功')
    emit('diffExported', fileName)
  } catch (error) {
    console.error('Failed to export diff:', error)
    ElMessage.error('差异报告导出失败')
  } finally {
    isExporting.value = false
  }
}

const handleNodeClick = (data: any) => {
  selectedNode.value = data
}

const applyDiff = (diff: any, direction: 'left' | 'right') => {
  // 在实际应用中，这里可以实现将一个版本的属性值应用到另一个版本的逻辑
  ElMessage.info(`将${direction === 'left' ? '左侧' : '右侧'}的值应用到${direction === 'left' ? '右侧' : '左侧'}`)
}

const toggleFilter = (filterValue: string) => {
  const filterIndex = activeFilters.value.indexOf(filterValue)
  if (filterIndex > -1) {
    activeFilters.value.splice(filterIndex, 1)
  } else {
    activeFilters.value.push(filterValue)
  }
  
  // 更新过滤选项状态
  const filter = filterOptions.value.find(f => f.value === filterValue)
  if (filter) {
    filter.active = !filter.active
  }
}

const clearAllFilters = () => {
  activeFilters.value = []
  filterOptions.value.forEach(filter => {
    filter.active = false
  })
}

// 工具方法
const getVersionLabel = (version: PageVersion): string => {
  let label = version.name
  if (version.isCurrent) {
    label += ' (当前)'
  }
  if (version.isPublished) {
    label += ' (已发布)'
  }
  return label
}

const getVersionName = (version: PageVersion | null): string => {
  if (!version) return '未知版本'
  let name = version.name
  if (version.isCurrent) {
    name += ' (当前)'
  }
  return name
}

const formatDateTime = (timestamp?: number): string => {
  if (!timestamp) return ''
  const date = new Date(timestamp)
  return date.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  })
}

const getFilterTypeColor = (filterValue: string): string => {
  const colors: Record<string, string> = {
    added: 'success',
    modified: 'warning',
    deleted: 'danger'
  }
  return colors[filterValue] || 'primary'
}

const getNodeLabel = (data: any): string => {
  if (data.componentName) {
    return `${data.componentName} (${data.id})`
  }
  if (data.property) {
    return data.property
  }
  return data.name || data.id || '未命名'
}

const formatDiffValue = (value: any): string => {
  if (value === undefined) return 'undefined'
  if (value === null) return 'null'
  if (typeof value === 'object') {
    return JSON.stringify(value, null, 2)
  }
  return String(value)
}

const getPreviewJson = (config: PageConfig | null): string => {
  if (!config) return 'null'
  return JSON.stringify(config, null, 2)
}

const generateHtmlReport = (data: any): string => {
  return `
    <!DOCTYPE html>
    <html>
    <head>
      <meta charset="UTF-8">
      <title>版本差异报告</title>
      <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        h1, h2, h3 { color: #333; }
        .metadata { background: #f5f5f5; padding: 10px; margin-bottom: 20px; }
        .diff-section { margin-bottom: 30px; }
        .added { color: #4CAF50; }
        .modified { color: #FF9800; }
        .deleted { color: #F44336; }
        table { border-collapse: collapse; width: 100%; }
        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
        th { background-color: #f2f2f2; }
        pre { background: #f8f8f8; padding: 10px; border-radius: 4px; overflow-x: auto; }
      </style>
    </head>
    <body>
      <h1>版本差异报告</h1>
      <div class="metadata">
        <p>生成时间: ${new Date().toLocaleString('zh-CN')}</p>
        <p>左侧版本: ${data.leftVersion.name} (${new Date(data.leftVersion.timestamp).toLocaleString('zh-CN')})</p>
        <p>右侧版本: ${data.rightVersion.name} (${new Date(data.rightVersion.timestamp).toLocaleString('zh-CN')})</p>
      </div>
      
      <div class="diff-section">
        <h2>新增组件 (${data.diff.added.length})</h2>
        ${generateComponentList(data.diff.added, 'added')}
      </div>
      
      <div class="diff-section">
        <h2>修改组件 (${data.diff.modified.length})</h2>
        ${generateComponentList(data.diff.modified, 'modified')}
      </div>
      
      <div class="diff-section">
        <h2>删除组件 (${data.diff.deleted.length})</h2>
        ${generateComponentList(data.diff.deleted, 'deleted')}
      </div>
    </body>
    </html>
  `
}

const generateComponentList = (components: any[], type: string): string => {
  if (components.length === 0) return '<p>无</p>'
  
  return components.map(comp => `
    <div class="component-item ${type}">
      <h3>${comp.componentName} (${comp.id})</h3>
      ${comp.diffs && comp.diffs.length > 0 ? `
        <table>
          <tr>
            <th>属性</th>
            <th>左侧值</th>
            <th>右侧值</th>
          </tr>
          ${comp.diffs.map((diff: any) => `
            <tr>
              <td><code>${diff.property}</code></td>
              <td><pre>${formatDiffValue(diff.oldValue)}</pre></td>
              <td><pre>${formatDiffValue(diff.newValue)}</pre></td>
            </tr>
          `).join('')}
        </table>
      ` : '<p>无属性变更</p>'}
    </div>
  `).join('')
}

const generateTextReport = (data: any): string => {
  let report = `版本差异报告\n`
  report += `===================\n`
  report += `生成时间: ${new Date().toLocaleString('zh-CN')}\n`
  report += `左侧版本: ${data.leftVersion.name} (${new Date(data.leftVersion.timestamp).toLocaleString('zh-CN')})\n`
  report += `右侧版本: ${data.rightVersion.name} (${new Date(data.rightVersion.timestamp).toLocaleString('zh-CN')})\n\n`
  
  report += `新增组件 (${data.diff.added.length})\n`
  report += `-`.repeat(50) + `\n`
  data.diff.added.forEach((comp: any) => {
    report += `- ${comp.componentName} (${comp.id})\n`
  })
  report += `\n`
  
  report += `修改组件 (${data.diff.modified.length})\n`
  report += `-`.repeat(50) + `\n`
  data.diff.modified.forEach((comp: any) => {
    report += `- ${comp.componentName} (${comp.id})\n`
    if (comp.diffs && comp.diffs.length > 0) {
      comp.diffs.forEach((diff: any) => {
        report += `  - ${diff.property}: ${formatDiffValue(diff.oldValue)} -> ${formatDiffValue(diff.newValue)}\n`
      })
    }
  })
  report += `\n`
  
  report += `删除组件 (${data.diff.deleted.length})\n`
  report += `-`.repeat(50) + `\n`
  data.diff.deleted.forEach((comp: any) => {
    report += `- ${comp.componentName} (${comp.id})\n`
  })
  
  return report
}

// 初始化
onMounted(async () => {
  await loadVersions()
  
  // 如果有初始版本，自动进行比较
  if (canCompare.value) {
    // 延迟比较，让UI先渲染
    setTimeout(() => {
      compareVersions()
    }, 500)
  }
})
</script>

<style scoped>
.version-comparer {
  background: #fff;
  border-radius: 4px;
  padding: 20px;
  height: 100%;
  display: flex;
  flex-direction: column;
}

.comparer-header {
  display: flex;
  flex-direction: column;
  gap: 20px;
  margin-bottom: 20px;
  padding-bottom: 20px;
  border-bottom: 1px solid #ebeef5;
}

.version-selector-container {
  display: flex;
  align-items: flex-end;
  gap: 20px;
  flex-wrap: wrap;
}

.version-selector {
  flex: 1;
  min-width: 200px;
}

.comparer-arrow {
  display: flex;
  align-items: center;
  justify-content: center;
  padding-bottom: 20px;
}

.comparer-actions {
  display: flex;
  gap: 10px;
  justify-content: flex-end;
  flex-wrap: wrap;
}

.loading-container,
.no-diff-container,
.initial-message {
  padding: 40px 20px;
  text-align: center;
}

.no-diff-message,
.initial-hint {
  margin-top: 20px;
  color: #606266;
  font-size: 14px;
}

.comparison-result {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.diff-summary {
  margin-bottom: 20px;
}

.diff-filters {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
  margin-top: 15px;
  padding: 10px;
  background-color: #f5f7fa;
  border-radius: 4px;
}

.diff-content {
  flex: 1;
  overflow: auto;
  padding: 10px;
  border: 1px solid #ebeef5;
  border-radius: 4px;
}

/* 预览模式样式 */
.preview-mode {
  display: flex;
  gap: 20px;
  height: 100%;
}

.preview-container {
  flex: 1;
  display: flex;
  flex-direction: column;
  background-color: #f5f7fa;
  border-radius: 4px;
  padding: 15px;
}

.preview-header {
  margin-bottom: 15px;
  padding-bottom: 10px;
  border-bottom: 1px solid #ebeef5;
}

.preview-title {
  font-size: 16px;
  font-weight: 600;
  color: #303133;
}

.preview-meta {
  font-size: 12px;
  color: #909399;
}

.preview-content {
  flex: 1;
  overflow: auto;
}

.preview-code {
  background-color: #fff;
  border: 1px solid #ebeef5;
  border-radius: 4px;
  padding: 15px;
  margin-top: 10px;
  font-size: 13px;
  line-height: 1.6;
  overflow: auto;
  white-space: pre-wrap;
  word-break: break-all;
}

.preview-separator {
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 24px;
  color: #909399;
  font-weight: bold;
}

/* 差异列表模式样式 */
.diff-list {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.diff-section {
  background-color: #fafafa;
  border-radius: 4px;
  padding: 15px;
}

.diff-section-title {
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 16px;
  font-weight: 600;
  color: #303133;
  margin-bottom: 15px;
  padding-bottom: 10px;
  border-bottom: 1px solid #ebeef5;
}

.section-count {
  margin-left: auto;
}

.tree-node-content {
  display: inline-flex;
  align-items: center;
  padding: 4px 0;
  width: 100%;
}

.node-selected {
  background-color: #ecf5ff;
  color: #409eff;
}

/* 属性差异详情样式 */
.property-diff-detail {
  margin-top: 20px;
  padding-top: 20px;
  border-top: 1px solid #ebeef5;
}

.diff-value {
  max-height: 100px;
  overflow: auto;
}

.diff-value pre {
  margin: 0;
  padding: 4px;
  background-color: #f5f7fa;
  border-radius: 2px;
  font-size: 12px;
  white-space: pre-wrap;
  word-break: break-all;
}

.old-value pre {
  border-left: 3px solid #f56c6c;
}

.new-value pre {
  border-left: 3px solid #67c23a;
}

/* 版本选项样式 */
:deep(.version-option-content) {
  padding: 5px 0;
}

.option-name {
  font-weight: 500;
  color: #303133;
}

.option-meta {
  font-size: 12px;
  color: #909399;
}

/* 确认对话框样式 */
.confirm-content {
  line-height: 1.6;
  color: #606266;
}

/* 响应式调整 */
@media (max-width: 768px) {
  .version-comparer {
    padding: 10px;
  }
  
  .version-selector-container {
    flex-direction: column;
    align-items: stretch;
    gap: 10px;
  }
  
  .version-selector {
    min-width: auto;
  }
  
  .comparer-arrow {
    padding-bottom: 0;
    transform: rotate(90deg);
  }
  
  .comparer-actions {
    justify-content: center;
  }
  
  .preview-mode {
    flex-direction: column;
  }
  
  .preview-separator {
    transform: rotate(270deg);
    height: 40px;
  }
  
  .diff-filters {
    justify-content: center;
  }
}

/* 深色模式适配 */
:deep(.el-tree) {
  background-color: transparent;
}

:deep(.el-tree-node__content:hover) {
  background-color: #ecf5ff;
}

:deep(.el-tree--highlight-current .el-tree-node.is-current > .el-tree-node__content) {
  background-color: #ecf5ff;
}

/* 滚动条样式 */
.diff-content::-webkit-scrollbar,
.preview-code::-webkit-scrollbar,
.diff-value::-webkit-scrollbar {
  width: 8px;
  height: 8px;
}

.diff-content::-webkit-scrollbar-track,
.preview-code::-webkit-scrollbar-track,
.diff-value::-webkit-scrollbar-track {
  background: #f1f1f1;
  border-radius: 4px;
}

.diff-content::-webkit-scrollbar-thumb,
.preview-code::-webkit-scrollbar-thumb,
.diff-value::-webkit-scrollbar-thumb {
  background: #c0c4cc;
  border-radius: 4px;
}

.diff-content::-webkit-scrollbar-thumb:hover,
.preview-code::-webkit-scrollbar-thumb:hover,
.diff-value::-webkit-scrollbar-thumb:hover {
  background: #909399;
}
</style>