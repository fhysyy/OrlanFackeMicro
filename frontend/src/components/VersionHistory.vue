<template>
  <div class="version-history">
    <!-- 版本历史面板头部 -->
    <div class="version-history-header">
      <h3 class="version-history-title">版本历史</h3>
      <div class="version-history-actions">
        <el-button
          type="primary"
          size="small"
          @click="showCreateVersionDialog = true"
          :loading="isCreating"
          icon="el-icon-plus"
        >
          创建版本
        </el-button>
        <el-button
          type="text"
          size="small"
          @click="showFullVersionManager = true"
          icon="el-icon-more"
        >
          更多
        </el-button>
      </div>
    </div>

    <!-- 自动保存状态提示 -->
    <div v-if="isAutoSaving" class="auto-save-notice">
      <el-tag size="small" type="info">自动保存中...</el-tag>
    </div>

    <!-- 最近版本列表 -->
    <div class="version-list">
      <el-empty
        v-if="recentVersions.length === 0 && !isLoading"
        description="暂无版本记录"
        :image-size="100"
      />

      <el-skeleton
        v-else-if="isLoading"
        :rows="3"
        animated
        style="padding: 10px 0;"
      />

      <div
        v-else
        class="version-item"
        v-for="version in recentVersions"
        :key="version.id"
        :class="{
          'version-item-current': version.isCurrent,
          'version-item-published': version.isPublished,
          'version-item-auto-saved': version.tags.includes('自动保存')
        }"
      >
        <!-- 版本信息 -->
        <div class="version-info" @click="handleVersionClick(version)">
          <div class="version-name-container">
            <span class="version-name">{{ version.name }}</span>
            <el-tag
              v-if="version.isCurrent"
              size="mini"
              type="success"
              style="margin-left: 8px;"
            >
              当前
            </el-tag>
            <el-tag
              v-if="version.isPublished"
              size="mini"
              type="primary"
              style="margin-left: 8px;"
            >
              已发布
            </el-tag>
          </div>
          
          <div class="version-meta">
            <span class="version-time">{{ formatRelativeTime(version.timestamp) }}</span>
            <span class="version-separator">•</span>
            <span class="version-user">{{ version.userName }}</span>
          </div>
          
          <!-- 变更摘要（简短） -->
          <div v-if="version.changeSummary && version.changeSummary.length > 0" class="version-change-summary">
            <span class="change-indicator">
              <el-tag size="mini" plain type="info">
                {{ version.changeSummary.length }} 项变更
              </el-tag>
            </span>
          </div>
        </div>
        
        <!-- 操作按钮 -->
        <div class="version-actions">
          <el-dropdown trigger="click" @command="handleVersionCommand(version, $event)">
            <el-button type="text" size="small" icon="el-icon-more"></el-button>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item command="view">查看详情</el-dropdown-item>
                <el-dropdown-item
                  command="restore"
                  :disabled="version.isCurrent"
                >
                  恢复此版本
                </el-dropdown-item>
                <el-dropdown-item
                  command="publish"
                  :disabled="version.isPublished"
                >
                  标记为已发布
                </el-dropdown-item>
                <el-dropdown-item command="export">导出版本</el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </div>
    </div>

    <!-- 底部提示 -->
    <div class="version-history-footer">
      <el-button
        type="text"
        size="small"
        @click="showFullVersionManager = true"
        v-if="recentVersions.length >= maxDisplayVersions"
      >
        查看全部版本（{{ totalVersions }}）
      </el-button>
      <div v-else class="version-count">
        共 {{ totalVersions }} 个版本
      </div>
    </div>

    <!-- 创建新版本对话框 -->
    <el-dialog
      v-model="showCreateVersionDialog"
      title="创建新版本"
      width="400px"
      @close="resetCreateForm"
    >
      <el-form
        ref="createVersionFormRef"
        :model="createVersionForm"
        :rules="createVersionRules"
        label-width="80px"
      >
        <el-form-item label="版本名称" prop="name">
          <el-input
            v-model="createVersionForm.name"
            placeholder="请输入版本名称"
            maxlength="50"
            show-word-limit
          />
        </el-form-item>
        <el-form-item label="描述（可选）">
          <el-input
            v-model="createVersionForm.description"
            type="textarea"
            placeholder="请输入版本描述"
            maxlength="100"
            show-word-limit
            :rows="2"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showCreateVersionDialog = false">取消</el-button>
        <el-button
          type="primary"
          @click="handleCreateVersion"
          :loading="isCreating"
        >
          创建
        </el-button>
      </template>
    </el-dialog>

    <!-- 版本详情对话框 -->
    <el-dialog
      v-model="showVersionDetailDialog"
      :title="`版本详情: ${selectedVersion?.name}`"
      width="500px"
    >
      <div v-if="selectedVersion" class="version-detail">
        <el-descriptions :column="1" border>
          <el-descriptions-item label="版本号">{{ selectedVersion.versionNumber }}</el-descriptions-item>
          <el-descriptions-item label="创建时间">{{ formatDateTime(selectedVersion.timestamp) }}</el-descriptions-item>
          <el-descriptions-item label="创建用户">{{ selectedVersion.userName }}</el-descriptions-item>
          <el-descriptions-item label="描述">
            {{ selectedVersion.description || '无描述' }}
          </el-descriptions-item>
        </el-descriptions>
        
        <div v-if="selectedVersion.changeSummary && selectedVersion.changeSummary.length > 0" class="detail-changes">
          <h4 class="detail-section-title">变更内容</h4>
          <el-timeline>
            <el-timeline-item
              v-for="(change, index) in selectedVersion.changeSummary.slice(0, 10)"
              :key="index"
              :timestamp="getChangeTypeLabel(change.type)"
              :type="getChangeTypeColor(change.type)"
            >
              <div class="timeline-content">
                {{ getChangeContent(change) }}
              </div>
            </el-timeline-item>
            <el-timeline-item
              v-if="selectedVersion.changeSummary.length > 10"
              type="info"
            >
              <div class="timeline-more">
                还有 {{ selectedVersion.changeSummary.length - 10 }} 项变更...
                <el-button type="text" size="small" @click="showFullVersionManager = true">查看全部</el-button>
              </div>
            </el-timeline-item>
          </el-timeline>
        </div>
        
        <div v-if="selectedVersion.tags && selectedVersion.tags.length > 0" class="detail-tags">
          <h4 class="detail-section-title">标签</h4>
          <el-tag
            v-for="tag in selectedVersion.tags"
            :key="tag"
            size="mini"
            style="margin: 5px 5px 5px 0;"
          >
            {{ tag }}
          </el-tag>
        </div>
      </div>
      <template #footer>
        <el-button @click="showVersionDetailDialog = false">关闭</el-button>
      </template>
    </el-dialog>

    <!-- 恢复版本确认对话框 -->
    <el-dialog
      v-model="showRestoreDialog"
      title="确认恢复版本"
      width="400px"
      :close-on-click-modal="false"
      :close-on-press-escape="false"
    >
      <div class="restore-warning">
        <el-alert
          title="注意"
          type="warning"
          description="恢复版本将创建一个新的版本，不会覆盖现有版本。是否继续？"
          show-icon
          :closable="false"
        />
        <div class="restore-info">
          <div class="restore-version-name">{{ restoreTargetVersion?.name }}</div>
          <div class="restore-version-meta">
            版本 {{ restoreTargetVersion?.versionNumber }} · {{ formatDateTime(restoreTargetVersion?.timestamp) }}
          </div>
        </div>
      </div>
      <template #footer>
        <el-button @click="showRestoreDialog = false">取消</el-button>
        <el-button
          type="primary"
          @click="handleRestoreVersion"
          :loading="isRestoring"
        >
          确认恢复
        </el-button>
      </template>
    </el-dialog>

    <!-- 完整版本管理器对话框 -->
    <el-dialog
      v-model="showFullVersionManager"
      title="版本管理"
      width="800px"
      fullscreen
      @close="onCloseFullManager"
    >
      <version-manager
        :page-id="pageId"
        :get-current-config="getCurrentConfig"
        @version-restored="onVersionRestored"
        @version-created="onVersionCreated"
      />
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { versionControlService } from '@/services/versionControlService'
import { PageConfig } from '@/types/page'
import VersionManager from '@/components/VersionManager.vue'
import type { PageVersion } from '@/services/versionControlService'

// Props
interface Props {
  pageId: string
  getCurrentConfig: () => PageConfig
  maxDisplayVersions?: number
  compact?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  maxDisplayVersions: 5,
  compact: false
})

// Emits
interface Emits {
  versionRestored: [version: PageVersion]
  versionCreated: [version: PageVersion]
  versionSelected: [version: PageVersion]
}

const emit = defineEmits<Emits>()

// 响应式数据
const versions = ref<PageVersion[]>([])
const isLoading = ref(false)
const isCreating = ref(false)
const isRestoring = ref(false)
const isAutoSaving = ref(false)

// 对话框状态
const showCreateVersionDialog = ref(false)
const showVersionDetailDialog = ref(false)
const showRestoreDialog = ref(false)
const showFullVersionManager = ref(false)

// 表单数据
const createVersionForm = reactive({
  name: '',
  description: ''
})

// 表单验证规则
const createVersionRules = reactive({
  name: [
    { required: true, message: '请输入版本名称', trigger: 'blur' },
    { min: 1, max: 50, message: '版本名称长度在 1 到 50 个字符', trigger: 'blur' }
  ]
})

// 临时数据
const selectedVersion = ref<PageVersion | null>(null)
const restoreTargetVersion = ref<PageVersion | null>(null)
const autoSaveTimer = ref<number | null>(null)

// 计算属性
const recentVersions = computed(() => {
  return versions.value.slice(0, props.maxDisplayVersions)
})

const totalVersions = computed(() => {
  return versions.value.length
})

// 方法
const refreshVersions = async () => {
  try {
    isLoading.value = true
    versions.value = versionControlService.getVersions(props.pageId)
  } catch (error) {
    console.error('Failed to refresh versions:', error)
  } finally {
    isLoading.value = false
  }
}

const resetCreateForm = () => {
  createVersionForm.name = ''
  createVersionForm.description = ''
}

const handleCreateVersion = async () => {
  try {
    isCreating.value = true
    
    const version = await versionControlService.createVersion(props.pageId, props.getCurrentConfig(), {
      name: createVersionForm.name,
      description: createVersionForm.description
    })
    
    await refreshVersions()
    showCreateVersionDialog.value = false
    ElMessage.success('版本创建成功')
    emit('versionCreated', version)
  } catch (error) {
    ElMessage.error('版本创建失败')
    console.error('Failed to create version:', error)
  } finally {
    isCreating.value = false
  }
}

const handleVersionClick = (version: PageVersion) => {
  if (props.compact) {
    // 在紧凑模式下点击显示详情
    selectedVersion.value = version
    showVersionDetailDialog.value = true
  } else {
    // 在普通模式下点击可以选择版本
    emit('versionSelected', version)
  }
}

const handleVersionCommand = (version: PageVersion, command: string) => {
  switch (command) {
    case 'view':
      selectedVersion.value = version
      showVersionDetailDialog.value = true
      break
    case 'restore':
      restoreTargetVersion.value = version
      showRestoreDialog.value = true
      break
    case 'publish':
      handlePublishVersion(version)
      break
    case 'export':
      handleExportVersion(version)
      break
  }
}

const handleRestoreVersion = async () => {
  if (!restoreTargetVersion.value) return
  
  try {
    isRestoring.value = true
    
    const version = await versionControlService.restoreVersion(
      props.pageId,
      restoreTargetVersion.value.id
    )
    
    await refreshVersions()
    showRestoreDialog.value = false
    ElMessage.success('版本恢复成功')
    emit('versionRestored', version)
  } catch (error) {
    ElMessage.error('版本恢复失败')
    console.error('Failed to restore version:', error)
  } finally {
    isRestoring.value = false
  }
}

const handlePublishVersion = async (version: PageVersion) => {
  try {
    await versionControlService.publishVersion(props.pageId, version.id)
    await refreshVersions()
    ElMessage.success('版本发布成功')
  } catch (error) {
    ElMessage.error('版本发布失败')
    console.error('Failed to publish version:', error)
  }
}

const handleExportVersion = (version: PageVersion) => {
  try {
    const content = versionControlService.exportVersion(props.pageId, version.id)
    const blob = new Blob([content], { type: 'application/json' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `version_${version.pageId}_${version.versionNumber}_${version.name.replace(/[^a-zA-Z0-9]/g, '_')}.json`
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
    URL.revokeObjectURL(url)
  } catch (error) {
    ElMessage.error('版本导出失败')
    console.error('Failed to export version:', error)
  }
}

const onVersionRestored = async (version: PageVersion) => {
  await refreshVersions()
  showFullVersionManager.value = false
  emit('versionRestored', version)
}

const onVersionCreated = async (version: PageVersion) => {
  await refreshVersions()
  emit('versionCreated', version)
}

const onCloseFullManager = async () => {
  await refreshVersions()
}

// 设置自动保存状态监听
const setupAutoSaveListener = () => {
  // 监听自动保存状态变化
  const checkAutoSaveStatus = () => {
    // 这里可以通过服务的配置来判断是否处于自动保存中
    // 模拟自动保存状态
    const autoSaveEnabled = versionControlService.config.enableAutoSave
    if (autoSaveEnabled) {
      // 每30秒检查一次是否有新的自动保存版本
      autoSaveTimer.value = window.setInterval(() => {
        refreshVersions()
      }, 30000) as unknown as number
    }
  }
  
  checkAutoSaveStatus()
}

// 工具方法
const formatDateTime = (timestamp: number): string => {
  const date = new Date(timestamp)
  return date.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  })
}

const formatRelativeTime = (timestamp: number): string => {
  const now = Date.now()
  const diff = now - timestamp
  const minutes = Math.floor(diff / 60000)
  const hours = Math.floor(diff / 3600000)
  const days = Math.floor(diff / 86400000)
  
  if (minutes < 1) return '刚刚'
  if (minutes < 60) return `${minutes}分钟前`
  if (hours < 24) return `${hours}小时前`
  if (days < 7) return `${days}天前`
  
  return formatDateTime(timestamp)
}

const getChangeTypeLabel = (type: string): string => {
  const labels: Record<string, string> = {
    added: '新增',
    modified: '修改',
    deleted: '删除',
    moved: '移动'
  }
  return labels[type] || type
}

const getChangeTypeColor = (type: string): string => {
  const colors: Record<string, string> = {
    added: 'success',
    modified: 'warning',
    deleted: 'danger',
    moved: 'info'
  }
  return colors[type] || 'primary'
}

const getChangeContent = (change: any): string => {
  if (change.componentName) {
    switch (change.type) {
      case 'added':
        return `新增组件: ${change.componentName}`
      case 'deleted':
        return `删除组件: ${change.componentName}`
      case 'modified':
        return `修改组件: ${change.componentName}${change.property ? ` (${change.property})` : ''}`
      case 'moved':
        return `移动组件: ${change.componentName}`
    }
  }
  if (change.property) {
    return `修改: ${change.property}`
  }
  return '未知变更'
}

// 生命周期
onMounted(async () => {
  await refreshVersions()
  setupAutoSaveListener()
})

// 监听
watch(() => props.pageId, async (newPageId) => {
  await refreshVersions()
})

// 清理
const cleanup = () => {
  if (autoSaveTimer.value) {
    clearInterval(autoSaveTimer.value)
  }
}

// 组件卸载时清理
onUnmounted(() => {
  cleanup()
})

// 导出清理函数以便父组件可以调用
defineExpose({
  cleanup,
  refreshVersions
})
</script>

<style scoped>
.version-history {
  background: #fff;
  border-radius: 4px;
  padding: 15px;
  height: 100%;
  display: flex;
  flex-direction: column;
}

.version-history-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 15px;
  padding-bottom: 10px;
  border-bottom: 1px solid #ebeef5;
}

.version-history-title {
  font-size: 16px;
  font-weight: 600;
  color: #303133;
  margin: 0;
}

.version-history-actions {
  display: flex;
  gap: 8px;
}

.auto-save-notice {
  margin-bottom: 10px;
}

.version-list {
  flex: 1;
  overflow-y: auto;
  margin-bottom: 15px;
}

.version-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px;
  border: 1px solid #ebeef5;
  border-radius: 4px;
  margin-bottom: 8px;
  transition: all 0.3s;
  cursor: pointer;
}

.version-item:hover {
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  transform: translateY(-1px);
}

.version-item-current {
  border-color: #67c23a;
  background-color: #f0f9ff;
}

.version-item-published {
  border-left: 3px solid #409eff;
}

.version-item-auto-saved {
  opacity: 0.8;
}

.version-info {
  flex: 1;
  min-width: 0;
}

.version-name-container {
  display: flex;
  align-items: center;
  margin-bottom: 4px;
}

.version-name {
  font-size: 14px;
  font-weight: 500;
  color: #303133;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  flex: 1;
}

.version-meta {
  font-size: 12px;
  color: #909399;
  display: flex;
  align-items: center;
}

.version-time {
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.version-separator {
  margin: 0 6px;
}

.version-change-summary {
  margin-top: 6px;
}

.change-indicator {
  font-size: 12px;
}

.version-actions {
  margin-left: 10px;
}

.version-history-footer {
  padding-top: 10px;
  border-top: 1px solid #ebeef5;
  text-align: center;
  font-size: 12px;
}

.version-count {
  color: #909399;
}

/* 详情对话框样式 */
.version-detail {
  padding: 10px 0;
}

.detail-section-title {
  font-size: 14px;
  font-weight: 600;
  margin: 15px 0 10px;
  color: #303133;
}

.timeline-content {
  font-size: 13px;
  color: #606266;
  padding: 5px 0;
}

.timeline-more {
  font-size: 13px;
  color: #909399;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.detail-tags {
  padding-top: 5px;
}

/* 恢复对话框样式 */
.restore-warning {
  padding: 10px 0;
}

.restore-info {
  margin-top: 15px;
  padding: 10px;
  background-color: #f5f7fa;
  border-radius: 4px;
}

.restore-version-name {
  font-weight: 500;
  color: #303133;
  margin-bottom: 5px;
}

.restore-version-meta {
  font-size: 12px;
  color: #909399;
}

/* 响应式调整 */
@media (max-width: 768px) {
  .version-history {
    padding: 10px;
  }
  
  .version-history-header {
    flex-direction: column;
    align-items: stretch;
    gap: 10px;
  }
  
  .version-history-actions {
    justify-content: center;
  }
  
  .version-item {
    flex-direction: column;
    align-items: stretch;
    gap: 8px;
  }
  
  .version-actions {
    margin-left: 0;
    display: flex;
    justify-content: flex-end;
  }
}

/* 紧凑模式样式 */
:deep(.version-history-compact) .version-history {
  padding: 10px;
}

:deep(.version-history-compact) .version-history-header {
  margin-bottom: 10px;
  padding-bottom: 8px;
}

:deep(.version-history-compact) .version-history-title {
  font-size: 14px;
}

:deep(.version-history-compact) .version-item {
  padding: 10px;
  margin-bottom: 6px;
}

:deep(.version-history-compact) .version-name {
  font-size: 13px;
}

:deep(.version-history-compact) .version-meta {
  font-size: 11px;
}
</style>