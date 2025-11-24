<template>
  <div class="version-manager">
    <!-- 顶部操作栏 -->
    <div class="version-manager-header">
      <h2 class="version-manager-title">版本管理</h2>
      <el-row :gutter="10" type="flex" justify="end" align="middle">
        <el-col>
          <el-input
            v-model="searchQuery"
            placeholder="搜索版本名称、描述或标签"
            clearable
            prefix-icon="el-icon-search"
            style="width: 240px;"
          />
        </el-col>
        <el-col>
          <el-button
            type="primary"
            @click="showCreateVersionDialog = true"
            :loading="isCreating"
            icon="el-icon-plus"
          >
            创建新版本
          </el-button>
        </el-col>
      </el-row>
    </div>

    <!-- 版本统计信息 -->
    <div class="version-stats">
      <el-card shadow="hover" :body-style="{ padding: '15px' }">
        <el-row :gutter="20">
          <el-col :span="6">
            <div class="stat-item">
              <div class="stat-value">{{ versionStats.totalVersions }}</div>
              <div class="stat-label">总版本数</div>
            </div>
          </el-col>
          <el-col :span="6">
            <div class="stat-item">
              <div class="stat-value">{{ versionStats.publishedVersions }}</div>
              <div class="stat-label">已发布版本</div>
            </div>
          </el-col>
          <el-col :span="6">
            <div class="stat-item">
              <div class="stat-value">{{ versionStats.autoSavedVersions }}</div>
              <div class="stat-label">自动保存</div>
            </div>
          </el-col>
          <el-col :span="6">
            <div class="stat-item">
              <div class="stat-value">{{ formatLastModified(versionStats.lastModified) }}</div>
              <div class="stat-label">最近修改</div>
            </div>
          </el-col>
        </el-row>
      </el-card>
    </div>

    <!-- 版本列表 -->
    <div class="version-list-container">
      <el-card shadow="hover">
        <div class="version-list-header">
          <span class="version-list-title">版本历史</span>
          <el-button-group>
            <el-button
              type="text"
              @click="refreshVersions"
              :loading="isLoading"
              icon="el-icon-refresh"
            >
              刷新
            </el-button>
            <el-button
              type="text"
              @click="showImportDialog = true"
              icon="el-icon-upload"
            >
              导入
            </el-button>
            <el-button
              type="text"
              @click="exportAllVersions"
              :disabled="filteredVersions.length === 0"
              icon="el-icon-download"
            >
              导出全部
            </el-button>
          </el-button-group>
        </div>

        <div class="version-list">
          <el-empty
            v-if="filteredVersions.length === 0 && !isLoading"
            description="暂无版本记录"
          />
          
          <el-skeleton
            v-else-if="isLoading"
            :rows="5"
            animated
            style="padding: 20px 0;"
          />

          <div
            v-else
            class="version-item"
            v-for="version in filteredVersions"
            :key="version.id"
            :class="{
              'version-item-current': version.isCurrent,
              'version-item-published': version.isPublished
            }"
          >
            <!-- 版本信息 -->
            <div class="version-info">
              <div class="version-main-info">
                <div class="version-name">
                  {{ version.name }}
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
                  <el-tag
                    v-if="version.tags.includes('自动保存')"
                    size="mini"
                    type="info"
                    style="margin-left: 8px;"
                  >
                    自动保存
                  </el-tag>
                </div>
                <div class="version-meta">
                  <span class="version-version">版本号: {{ version.versionNumber }}</span>
                  <span class="version-separator">•</span>
                  <span class="version-user">{{ version.userName }}</span>
                  <span class="version-separator">•</span>
                  <span class="version-time">{{ formatDateTime(version.timestamp) }}</span>
                </div>
              </div>
              
              <!-- 版本描述 -->
              <div v-if="version.description" class="version-description">
                {{ version.description }}
              </div>
              
              <!-- 变更摘要 -->
              <div v-if="version.changeSummary && version.changeSummary.length > 0" class="version-changes">
                <div class="version-changes-title">变更内容:</div>
                <div class="version-changes-list">
                  <div
                    v-for="(change, index) in version.changeSummary.slice(0, 3)"
                    :key="index"
                    class="version-change-item"
                  >
                    <span class="change-type" :class="`change-type-${change.type}`">
                      {{ getChangeTypeLabel(change.type) }}
                    </span>
                    <span class="change-content">
                      {{ getChangeContent(change) }}
                    </span>
                  </div>
                  <div v-if="version.changeSummary.length > 3" class="version-change-more">
                    还有 {{ version.changeSummary.length - 3 }} 项变更...
                  </div>
                </div>
              </div>
              
              <!-- 标签 -->
              <div v-if="version.tags && version.tags.length > 0" class="version-tags">
                <el-tag
                  v-for="tag in version.tags"
                  :key="tag"
                  size="mini"
                  type="warning"
                  effect="plain"
                >
                  {{ tag }}
                </el-tag>
              </div>
            </div>

            <!-- 操作按钮 -->
            <div class="version-actions">
              <el-dropdown trigger="click" @command="handleVersionCommand(version, $event)">
                <el-button type="text" icon="el-icon-more"></el-button>
                <template #dropdown>
                  <el-dropdown-menu>
                    <el-dropdown-item command="view">查看详情</el-dropdown-item>
                    <el-dropdown-item command="edit">编辑信息</el-dropdown-item>
                    <el-dropdown-item
                      command="restore"
                      :disabled="version.isCurrent"
                    >
                      恢复到此版本
                    </el-dropdown-item>
                    <el-dropdown-item
                      command="publish"
                      :disabled="version.isPublished"
                    >
                      标记为已发布
                    </el-dropdown-item>
                    <el-dropdown-item command="export">导出版本</el-dropdown-item>
                    <el-dropdown-item
                      command="compare"
                      :disabled="filteredVersions.length <= 1"
                    >
                      比较版本
                    </el-dropdown-item>
                    <el-dropdown-item
                      command="delete"
                      :disabled="version.isCurrent"
                      divided
                      type="danger"
                    >
                      删除版本
                    </el-dropdown-item>
                  </el-dropdown-menu>
                </template>
              </el-dropdown>
            </div>
          </div>
        </div>

        <!-- 分页 -->
        <div v-if="filteredVersions.length > 0" class="version-pagination">
          <el-pagination
            v-model:current-page="pagination.currentPage"
            v-model:page-size="pagination.pageSize"
            :page-sizes="[5, 10, 20, 50]"
            layout="total, sizes, prev, pager, next, jumper"
            :total="filteredVersions.length"
            @size-change="handleSizeChange"
            @current-change="handleCurrentChange"
          />
        </div>
      </el-card>
    </div>

    <!-- 创建新版本对话框 -->
    <el-dialog
      v-model="showCreateVersionDialog"
      title="创建新版本"
      width="500px"
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
        <el-form-item label="版本描述" prop="description">
          <el-input
            v-model="createVersionForm.description"
            type="textarea"
            placeholder="请输入版本描述（可选）"
            maxlength="200"
            show-word-limit
            :rows="3"
          />
        </el-form-item>
        <el-form-item label="标签">
          <el-input
            v-model="newTag"
            placeholder="输入标签，按回车添加"
            @keyup.enter.prevent="addTag"
            style="width: 200px; margin-right: 10px;"
          />
          <el-button type="primary" size="small" @click="addTag">添加</el-button>
          <div v-if="createVersionForm.tags.length > 0" class="tag-list">
            <el-tag
              v-for="(tag, index) in createVersionForm.tags"
              :key="index"
              closable
              @close="removeTag(index)"
              size="small"
            >
              {{ tag }}
            </el-tag>
          </div>
        </el-form-item>
        <el-form-item>
          <el-checkbox v-model="createVersionForm.isPublished">标记为已发布版本</el-checkbox>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showCreateVersionDialog = false">取消</el-button>
        <el-button
          type="primary"
          @click="handleCreateVersion"
          :loading="isCreating"
        >
          创建版本
        </el-button>
      </template>
    </el-dialog>

    <!-- 编辑版本对话框 -->
    <el-dialog
      v-model="showEditVersionDialog"
      title="编辑版本信息"
      width="500px"
      @close="resetEditForm"
    >
      <el-form
        ref="editVersionFormRef"
        :model="editVersionForm"
        :rules="editVersionRules"
        label-width="80px"
      >
        <el-form-item label="版本名称" prop="name">
          <el-input
            v-model="editVersionForm.name"
            placeholder="请输入版本名称"
            maxlength="50"
            show-word-limit
          />
        </el-form-item>
        <el-form-item label="版本描述" prop="description">
          <el-input
            v-model="editVersionForm.description"
            type="textarea"
            placeholder="请输入版本描述（可选）"
            maxlength="200"
            show-word-limit
            :rows="3"
          />
        </el-form-item>
        <el-form-item label="标签">
          <el-input
            v-model="editNewTag"
            placeholder="输入标签，按回车添加"
            @keyup.enter.prevent="addEditTag"
            style="width: 200px; margin-right: 10px;"
          />
          <el-button type="primary" size="small" @click="addEditTag">添加</el-button>
          <div v-if="editVersionForm.tags.length > 0" class="tag-list">
            <el-tag
              v-for="(tag, index) in editVersionForm.tags"
              :key="index"
              closable
              @close="removeEditTag(index)"
              size="small"
            >
              {{ tag }}
            </el-tag>
          </div>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showEditVersionDialog = false">取消</el-button>
        <el-button
          type="primary"
          @click="handleEditVersion"
          :loading="isEditing"
        >
          保存修改
        </el-button>
      </template>
    </el-dialog>

    <!-- 版本详情对话框 -->
    <el-dialog
      v-model="showVersionDetailDialog"
      :title="`版本详情: ${selectedVersion?.name}`"
      width="600px"
    >
      <div v-if="selectedVersion" class="version-detail">
        <div class="detail-section">
          <h3 class="section-title">基本信息</h3>
          <el-descriptions :column="1" border>
            <el-descriptions-item label="版本ID">{{ selectedVersion.id }}</el-descriptions-item>
            <el-descriptions-item label="版本号">{{ selectedVersion.versionNumber }}</el-descriptions-item>
            <el-descriptions-item label="创建时间">{{ formatDateTime(selectedVersion.timestamp) }}</el-descriptions-item>
            <el-descriptions-item label="创建用户">{{ selectedVersion.userName }}</el-descriptions-item>
            <el-descriptions-item label="状态">
              <el-tag
                v-if="selectedVersion.isCurrent"
                type="success"
                size="small"
              >
                当前版本
              </el-tag>
              <el-tag
                v-else
                type="info"
                size="small"
              >
                历史版本
              </el-tag>
              <el-tag
                v-if="selectedVersion.isPublished"
                type="primary"
                size="small"
                style="margin-left: 8px;"
              >
                已发布
              </el-tag>
            </el-descriptions-item>
          </el-descriptions>
        </div>

        <div class="detail-section">
          <h3 class="section-title">版本描述</h3>
          <div class="detail-description">
            {{ selectedVersion.description || '无描述' }}
          </div>
        </div>

        <div class="detail-section">
          <h3 class="section-title">变更内容</h3>
          <div v-if="selectedVersion.changeSummary && selectedVersion.changeSummary.length > 0" class="change-details">
            <el-table :data="selectedVersion.changeSummary" style="width: 100%">
              <el-table-column prop="type" label="类型" width="100">
                <template #default="scope">
                  <span class="change-type" :class="`change-type-${scope.row.type}`">
                    {{ getChangeTypeLabel(scope.row.type) }}
                  </span>
                </template>
              </el-table-column>
              <el-table-column prop="componentName" label="组件"></el-table-column>
              <el-table-column prop="property" label="属性" width="120"></el-table-column>
              <el-table-column label="详细信息">
                <template #default="scope">
                  <div class="change-detail-text">
                    {{ getChangeContent(scope.row, true) }}
                  </div>
                </template>
              </el-table-column>
            </el-table>
          </div>
          <div v-else class="no-changes">
            无变更记录
          </div>
        </div>

        <div class="detail-section">
          <h3 class="section-title">标签</h3>
          <div v-if="selectedVersion.tags && selectedVersion.tags.length > 0" class="detail-tags">
            <el-tag
              v-for="tag in selectedVersion.tags"
              :key="tag"
              size="small"
              style="margin: 5px;"
            >
              {{ tag }}
            </el-tag>
          </div>
          <div v-else class="no-tags">
            无标签
          </div>
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
          title="警告"
          type="warning"
          description="恢复此版本将创建一个基于当前版本的新版本，不会覆盖现有版本。是否继续？"
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

    <!-- 删除版本确认对话框 -->
    <el-dialog
      v-model="showDeleteDialog"
      title="确认删除版本"
      width="400px"
      :close-on-click-modal="false"
      :close-on-press-escape="false"
    >
      <div class="delete-warning">
        <el-alert
          title="警告"
          type="error"
          description="删除版本操作不可撤销，是否确认删除此版本？"
          show-icon
          :closable="false"
        />
        <div class="delete-info">
          <div class="delete-version-name">{{ deleteTargetVersion?.name }}</div>
          <div class="delete-version-meta">
            版本 {{ deleteTargetVersion?.versionNumber }} · {{ formatDateTime(deleteTargetVersion?.timestamp) }}
          </div>
        </div>
      </div>
      <template #footer>
        <el-button @click="showDeleteDialog = false">取消</el-button>
        <el-button
          type="danger"
          @click="handleDeleteVersion"
          :loading="isDeleting"
        >
          确认删除
        </el-button>
      </template>
    </el-dialog>

    <!-- 版本比较对话框 -->
    <el-dialog
      v-model="showCompareDialog"
      title="比较版本"
      width="800px"
    >
      <div class="compare-container">
        <div class="compare-selectors">
          <el-form label-width="60px" inline>
            <el-form-item label="从版本：">
              <el-select
                v-model="compareVersion1Id"
                placeholder="选择版本"
                style="width: 200px;"
              >
                <el-option
                  v-for="version in filteredVersions"
                  :key="version.id"
                  :label="`${version.name} (v${version.versionNumber})`"
                  :value="version.id"
                >
                  <div class="option-content">
                    <div class="option-name">{{ version.name }}</div>
                    <div class="option-meta">
                      版本 {{ version.versionNumber }} · {{ formatDateTime(version.timestamp) }}
                    </div>
                  </div>
                </el-option>
              </el-select>
            </el-form-item>
            <el-form-item>
              <el-button @click="swapCompareVersions" icon="el-icon-refresh-left"></el-button>
            </el-form-item>
            <el-form-item label="到版本：">
              <el-select
                v-model="compareVersion2Id"
                placeholder="选择版本"
                style="width: 200px;"
              >
                <el-option
                  v-for="version in filteredVersions"
                  :key="version.id"
                  :label="`${version.name} (v${version.versionNumber})`"
                  :value="version.id"
                >
                  <div class="option-content">
                    <div class="option-name">{{ version.name }}</div>
                    <div class="option-meta">
                      版本 {{ version.versionNumber }} · {{ formatDateTime(version.timestamp) }}
                    </div>
                  </div>
                </el-option>
              </el-select>
            </el-form-item>
            <el-form-item>
              <el-button
                type="primary"
                @click="compareVersions"
                :disabled="!compareVersion1Id || !compareVersion2Id"
              >
                比较
              </el-button>
            </el-form-item>
          </el-form>
        </div>

        <div v-if="comparisonResult" class="compare-result">
          <div class="compare-summary">
            <el-alert
              :title="`比较结果: ${comparisonResult.changes.length} 项变更`"
              type="info"
              show-icon
            />
          </div>

          <div class="compare-details">
            <el-table :data="comparisonResult.changes" style="width: 100%">
              <el-table-column prop="type" label="类型" width="100">
                <template #default="scope">
                  <span class="change-type" :class="`change-type-${scope.row.type}`">
                    {{ getChangeTypeLabel(scope.row.type) }}
                  </span>
                </template>
              </el-table-column>
              <el-table-column prop="componentName" label="组件"></el-table-column>
              <el-table-column prop="property" label="属性" width="120"></el-table-column>
              <el-table-column label="变更详情">
                <template #default="scope">
                  <div class="compare-change-detail">
                    <div v-if="scope.row.oldValue" class="change-value-old">
                      <span class="change-value-label">旧值:</span>
                      <pre>{{ formatChangeValue(scope.row.oldValue) }}</pre>
                    </div>
                    <div v-if="scope.row.newValue" class="change-value-new">
                      <span class="change-value-label">新值:</span>
                      <pre>{{ formatChangeValue(scope.row.newValue) }}</pre>
                    </div>
                  </div>
                </template>
              </el-table-column>
            </el-table>
          </div>
        </div>

        <div v-else-if="isComparing" class="compare-loading">
          <el-skeleton :rows="3" animated />
        </div>

        <div v-else class="compare-empty">
          <el-empty description="请选择两个版本进行比较" />
        </div>
      </div>
      <template #footer>
        <el-button @click="showCompareDialog = false">关闭</el-button>
      </template>
    </el-dialog>

    <!-- 导入版本对话框 -->
    <el-dialog
      v-model="showImportDialog"
      title="导入版本"
      width="500px"
    >
      <div class="import-container">
        <el-upload
          ref="uploadRef"
          class="upload-demo"
          drag
          action=""
          :auto-upload="false"
          :on-change="handleFileChange"
          :before-upload="beforeUpload"
          :show-file-list="true"
          accept=".json"
        >
          <i class="el-icon-upload"></i>
          <div class="el-upload__text">
            将文件拖到此处，或<span class="el-upload__text--highlight">点击上传</span>
          </div>
          <template #tip>
            <div class="el-upload__tip">
              请上传JSON格式的版本文件
            </div>
          </template>
        </el-upload>

        <div v-if="importFileContent" class="import-content-preview">
          <el-button
            type="text"
            @click="showImportContent = !showImportContent"
            icon="el-icon-view"
          >
            {{ showImportContent ? '隐藏' : '预览' }}导入内容
          </el-button>
          <div v-if="showImportContent" class="content-preview">
            <pre>{{ importFileContent }}</pre>
          </div>
        </div>
      </div>
      <template #footer>
        <el-button @click="closeImportDialog">取消</el-button>
        <el-button
          type="primary"
          @click="handleImportVersion"
          :disabled="!importFileContent || isImporting"
          :loading="isImporting"
        >
          导入版本
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, nextTick, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { versionControlService } from '@/services/versionControlService'
import { PageConfig } from '@/types/page'
import type { PageVersion } from '@/services/versionControlService'

// Props
interface Props {
  pageId: string
  getCurrentConfig: () => PageConfig
}

const props = defineProps<Props>()

// Emits
interface Emits {
  versionRestored: [version: PageVersion]
  versionCreated: [version: PageVersion]
}

const emit = defineEmits<Emits>()

// 响应式数据
const versions = ref<PageVersion[]>([])
const searchQuery = ref('')
const isLoading = ref(false)
const isCreating = ref(false)
const isEditing = ref(false)
const isRestoring = ref(false)
const isDeleting = ref(false)
const isComparing = ref(false)
const isImporting = ref(false)

// 分页数据
const pagination = reactive({
  currentPage: 1,
  pageSize: 10
})

// 对话框状态
const showCreateVersionDialog = ref(false)
const showEditVersionDialog = ref(false)
const showVersionDetailDialog = ref(false)
const showRestoreDialog = ref(false)
const showDeleteDialog = ref(false)
const showCompareDialog = ref(false)
const showImportDialog = ref(false)

// 表单数据
const createVersionForm = reactive({
  name: '',
  description: '',
  tags: [] as string[],
  isPublished: false
})

const editVersionForm = reactive({
  id: '',
  name: '',
  description: '',
  tags: [] as string[]
})

// 表单验证规则
const createVersionRules = reactive({
  name: [
    { required: true, message: '请输入版本名称', trigger: 'blur' },
    { min: 1, max: 50, message: '版本名称长度在 1 到 50 个字符', trigger: 'blur' }
  ]
})

const editVersionRules = reactive({
  name: [
    { required: true, message: '请输入版本名称', trigger: 'blur' },
    { min: 1, max: 50, message: '版本名称长度在 1 到 50 个字符', trigger: 'blur' }
  ]
})

// 临时数据
const newTag = ref('')
const editNewTag = ref('')
const selectedVersion = ref<PageVersion | null>(null)
const restoreTargetVersion = ref<PageVersion | null>(null)
const deleteTargetVersion = ref<PageVersion | null>(null)
const comparisonResult = ref<any>(null)
const compareVersion1Id = ref('')
const compareVersion2Id = ref('')
const importFileContent = ref('')
const showImportContent = ref(false)

// 计算属性
const filteredVersions = computed(() => {
  let result = [...versions.value]
  
  // 搜索过滤
  if (searchQuery.value.trim()) {
    const query = searchQuery.value.toLowerCase()
    result = result.filter(version => 
      version.name.toLowerCase().includes(query) ||
      version.description.toLowerCase().includes(query) ||
      version.tags.some(tag => tag.toLowerCase().includes(query)) ||
      version.userName.toLowerCase().includes(query) ||
      new Date(version.timestamp).toLocaleString().includes(query)
    )
  }
  
  return result
})

const paginatedVersions = computed(() => {
  const startIndex = (pagination.currentPage - 1) * pagination.pageSize
  const endIndex = startIndex + pagination.pageSize
  return filteredVersions.value.slice(startIndex, endIndex)
})

const versionStats = computed(() => {
  return versionControlService.getVersionStats(props.pageId)
})

// 方法
const refreshVersions = async () => {
  try {
    isLoading.value = true
    versions.value = versionControlService.getVersions(props.pageId)
  } catch (error) {
    ElMessage.error('获取版本列表失败')
    console.error('Failed to refresh versions:', error)
  } finally {
    isLoading.value = false
  }
}

const resetCreateForm = () => {
  createVersionForm.name = ''
  createVersionForm.description = ''
  createVersionForm.tags = []
  createVersionForm.isPublished = false
  newTag.value = ''
}

const resetEditForm = () => {
  editVersionForm.id = ''
  editVersionForm.name = ''
  editVersionForm.description = ''
  editVersionForm.tags = []
  editNewTag.value = ''
}

const addTag = () => {
  if (newTag.value.trim() && !createVersionForm.tags.includes(newTag.value.trim())) {
    createVersionForm.tags.push(newTag.value.trim())
    newTag.value = ''
  }
}

const removeTag = (index: number) => {
  createVersionForm.tags.splice(index, 1)
}

const addEditTag = () => {
  if (editNewTag.value.trim() && !editVersionForm.tags.includes(editNewTag.value.trim())) {
    editVersionForm.tags.push(editNewTag.value.trim())
    editNewTag.value = ''
  }
}

const removeEditTag = (index: number) => {
  editVersionForm.tags.splice(index, 1)
}

const handleCreateVersion = async () => {
  try {
    isCreating.value = true
    
    const version = await versionControlService.createVersion(props.pageId, props.getCurrentConfig(), {
      name: createVersionForm.name,
      description: createVersionForm.description,
      tags: createVersionForm.tags,
      isPublished: createVersionForm.isPublished
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

const handleEditVersion = async () => {
  try {
    isEditing.value = true
    
    await versionControlService.renameVersion(
      props.pageId,
      editVersionForm.id,
      editVersionForm.name,
      editVersionForm.description
    )
    
    await refreshVersions()
    showEditVersionDialog.value = false
    ElMessage.success('版本信息更新成功')
  } catch (error) {
    ElMessage.error('版本信息更新失败')
    console.error('Failed to edit version:', error)
  } finally {
    isEditing.value = false
  }
}

const handleVersionCommand = (version: PageVersion, command: string) => {
  switch (command) {
    case 'view':
      selectedVersion.value = version
      showVersionDetailDialog.value = true
      break
    case 'edit':
      editVersionForm.id = version.id
      editVersionForm.name = version.name
      editVersionForm.description = version.description
      editVersionForm.tags = [...version.tags]
      showEditVersionDialog.value = true
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
    case 'compare':
      compareVersion1Id.value = version.id
      showCompareDialog.value = true
      break
    case 'delete':
      deleteTargetVersion.value = version
      showDeleteDialog.value = true
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

const handleDeleteVersion = async () => {
  if (!deleteTargetVersion.value) return
  
  try {
    isDeleting.value = true
    
    await versionControlService.deleteVersion(
      props.pageId,
      deleteTargetVersion.value.id
    )
    
    await refreshVersions()
    showDeleteDialog.value = false
    ElMessage.success('版本删除成功')
  } catch (error) {
    ElMessage.error('版本删除失败')
    console.error('Failed to delete version:', error)
  } finally {
    isDeleting.value = false
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

const exportAllVersions = () => {
  try {
    const allVersions = versionControlService.getVersions(props.pageId)
    const content = JSON.stringify(allVersions, null, 2)
    const blob = new Blob([content], { type: 'application/json' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `all_versions_${props.pageId}_${new Date().toISOString().split('T')[0]}.json`
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
    URL.revokeObjectURL(url)
  } catch (error) {
    ElMessage.error('导出全部版本失败')
    console.error('Failed to export all versions:', error)
  }
}

const compareVersions = async () => {
  if (!compareVersion1Id.value || !compareVersion2Id.value) return
  
  try {
    isComparing.value = true
    comparisonResult.value = versionControlService.compareVersions(
      props.pageId,
      compareVersion1Id.value,
      compareVersion2Id.value
    )
  } catch (error) {
    ElMessage.error('版本比较失败')
    console.error('Failed to compare versions:', error)
  } finally {
    isComparing.value = false
  }
}

const swapCompareVersions = () => {
  const temp = compareVersion1Id.value
  compareVersion1Id.value = compareVersion2Id.value
  compareVersion2Id.value = temp
  if (comparisonResult.value) {
    compareVersions()
  }
}

const handleFileChange = (file: any) => {
  const reader = new FileReader()
  reader.onload = (e) => {
    if (e.target?.result) {
      importFileContent.value = e.target.result as string
    }
  }
  reader.readAsText(file.raw)
}

const beforeUpload = (file: any) => {
  const isJSON = file.type === 'application/json' || file.name.endsWith('.json')
  if (!isJSON) {
    ElMessage.error('请上传JSON格式的文件')
    return false
  }
  return false // 阻止自动上传
}

const handleImportVersion = async () => {
  if (!importFileContent.value) return
  
  try {
    isImporting.value = true
    
    const version = await versionControlService.importVersion(props.pageId, importFileContent.value)
    
    await refreshVersions()
    closeImportDialog()
    ElMessage.success('版本导入成功')
    emit('versionCreated', version)
  } catch (error) {
    ElMessage.error('版本导入失败')
    console.error('Failed to import version:', error)
  } finally {
    isImporting.value = false
  }
}

const closeImportDialog = () => {
  showImportDialog.value = false
  importFileContent.value = ''
  showImportContent.value = false
  if (import('@/components/VersionManager.vue').then(m => m.uploadRef)) {
    // 清空上传文件列表
  }
}

const formatDateTime = (timestamp: number): string => {
  const date = new Date(timestamp)
  return date.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit'
  })
}

const formatLastModified = (timestamp: number): string => {
  if (!timestamp) return '暂无'
  
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

const getChangeContent = (change: any, detailed = false): string => {
  if (change.componentName) {
    switch (change.type) {
      case 'added':
        return `${change.componentName}`
      case 'deleted':
        return `${change.componentName}`
      case 'modified':
        return `${change.componentName}${change.property ? ` - ${change.property}` : ''}`
      case 'moved':
        return `${change.componentName}`
    }
  }
  return change.property || '未知变更'
}

const formatChangeValue = (value: any): string => {
  if (typeof value === 'object') {
    return JSON.stringify(value, null, 2)
  }
  return String(value)
}

const handleSizeChange = (size: number) => {
  pagination.pageSize = size
  pagination.currentPage = 1
}

const handleCurrentChange = (current: number) => {
  pagination.currentPage = current
}

// 生命周期
onMounted(async () => {
  await refreshVersions()
  
  // 设置自动保存
  versionControlService.setupAutoSave(props.pageId, props.getCurrentConfig)
})

// 监听
watch(() => props.pageId, async (newPageId) => {
  await refreshVersions()
  
  // 重置分页
  pagination.currentPage = 1
})
</script>

<style scoped>
.version-manager {
  padding: 20px;
}

.version-manager-header {
  margin-bottom: 20px;
}

.version-manager-title {
  font-size: 20px;
  font-weight: 600;
  margin-bottom: 15px;
  color: #303133;
}

.version-stats {
  margin-bottom: 20px;
}

.stat-item {
  text-align: center;
}

.stat-value {
  font-size: 24px;
  font-weight: 600;
  color: #409eff;
  margin-bottom: 5px;
}

.stat-label {
  font-size: 14px;
  color: #606266;
}

.version-list-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 15px;
}

.version-list-title {
  font-size: 16px;
  font-weight: 600;
  color: #303133;
}

.version-item {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  padding: 15px;
  border: 1px solid #ebeef5;
  border-radius: 4px;
  margin-bottom: 10px;
  transition: all 0.3s;
  background: #fff;
}

.version-item:hover {
  box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
}

.version-item-current {
  border-color: #67c23a;
  background-color: #f0f9ff;
}

.version-item-published {
  border-left: 4px solid #409eff;
}

.version-info {
  flex: 1;
  margin-right: 15px;
}

.version-main-info {
  margin-bottom: 10px;
}

.version-name {
  font-size: 16px;
  font-weight: 500;
  color: #303133;
  margin-bottom: 5px;
}

.version-meta {
  font-size: 12px;
  color: #909399;
}

.version-separator {
  margin: 0 8px;
}

.version-description {
  font-size: 14px;
  color: #606266;
  margin-bottom: 10px;
  line-height: 1.5;
}

.version-changes {
  margin-bottom: 10px;
}

.version-changes-title {
  font-size: 12px;
  color: #909399;
  margin-bottom: 5px;
}

.version-changes-list {
  font-size: 13px;
}

.version-change-item {
  margin-bottom: 3px;
}

.change-type {
  padding: 2px 6px;
  border-radius: 3px;
  font-size: 12px;
  margin-right: 8px;
}

.change-type-added {
  background-color: #f0f9ff;
  color: #409eff;
}

.change-type-modified {
  background-color: #fdf6ec;
  color: #e6a23c;
}

.change-type-deleted {
  background-color: #fef0f0;
  color: #f56c6c;
}

.change-type-moved {
  background-color: #f0f9ff;
  color: #909399;
}

.change-content {
  color: #606266;
}

.version-change-more {
  font-size: 12px;
  color: #909399;
  margin-top: 5px;
}

.version-tags {
  margin-top: 8px;
}

.version-actions {
  display: flex;
  align-items: flex-start;
}

.version-pagination {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}

/* 详情对话框样式 */
.version-detail {
  padding: 10px 0;
}

.detail-section {
  margin-bottom: 20px;
}

.section-title {
  font-size: 16px;
  font-weight: 600;
  margin-bottom: 10px;
  color: #303133;
}

.detail-description {
  padding: 10px;
  background-color: #f5f7fa;
  border-radius: 4px;
  color: #606266;
  line-height: 1.5;
}

.change-details {
  max-height: 400px;
  overflow-y: auto;
}

.change-detail-text {
  font-size: 12px;
  color: #606266;
  line-height: 1.4;
}

.detail-tags {
  padding: 10px 0;
}

.no-changes,
.no-tags {
  color: #909399;
  padding: 20px;
  text-align: center;
}

/* 恢复/删除对话框样式 */
.restore-warning,
.delete-warning {
  padding: 10px 0;
}

.restore-info,
.delete-info {
  margin-top: 15px;
  padding: 10px;
  background-color: #f5f7fa;
  border-radius: 4px;
}

.restore-version-name,
.delete-version-name {
  font-weight: 500;
  color: #303133;
  margin-bottom: 5px;
}

.restore-version-meta,
.delete-version-meta {
  font-size: 12px;
  color: #909399;
}

/* 版本比较对话框样式 */
.compare-container {
  padding: 10px 0;
}

.compare-selectors {
  margin-bottom: 20px;
}

.option-content {
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

.compare-result {
  padding: 10px 0;
}

.compare-summary {
  margin-bottom: 20px;
}

.compare-details {
  max-height: 400px;
  overflow-y: auto;
}

.compare-change-detail {
  font-size: 12px;
}

.change-value-old,
.change-value-new {
  margin-bottom: 8px;
}

.change-value-old {
  background-color: #fef0f0;
  padding: 8px;
  border-radius: 4px;
}

.change-value-new {
  background-color: #f0f9ff;
  padding: 8px;
  border-radius: 4px;
}

.change-value-label {
  font-weight: 500;
  margin-right: 5px;
}

.change-value-old pre {
  color: #f56c6c;
  margin: 0;
  white-space: pre-wrap;
  word-break: break-all;
}

.change-value-new pre {
  color: #409eff;
  margin: 0;
  white-space: pre-wrap;
  word-break: break-all;
}

.compare-loading {
  padding: 20px 0;
}

.compare-empty {
  padding: 40px 0;
}

/* 导入对话框样式 */
.import-container {
  padding: 10px 0;
}

.upload-demo {
  margin-bottom: 20px;
}

.import-content-preview {
  margin-top: 20px;
}

.content-preview {
  max-height: 300px;
  overflow-y: auto;
  background-color: #f5f7fa;
  padding: 10px;
  border-radius: 4px;
  margin-top: 10px;
}

.content-preview pre {
  margin: 0;
  font-size: 12px;
  white-space: pre-wrap;
  word-break: break-all;
}

.tag-list {
  margin-top: 10px;
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}
</style>