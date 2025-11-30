<template>
  <div class="files-container">
    <div class="page-header">
      <h2>文件管理</h2>
      <el-button
        type="primary"
        @click="handleUpload"
      >
        上传文件
      </el-button>
    </div>

    <!-- 文件统计 -->
    <div class="file-stats">
      <el-row :gutter="20">
        <el-col :span="6">
          <el-statistic
            title="总文件数"
            :value="stats.totalFiles"
          />
        </el-col>
        <el-col :span="6">
          <el-statistic
            title="总大小"
            :value="formatFileSize(stats.totalSize)"
          />
        </el-col>
        <el-col :span="6">
          <el-statistic
            title="公开文件"
            :value="stats.publicFiles"
          />
        </el-col>
        <el-col :span="6">
          <el-statistic
            title="私有文件"
            :value="stats.privateFiles"
          />
        </el-col>
      </el-row>
    </div>

    <!-- 搜索和筛选 -->
    <el-card class="search-card">
      <el-form
        :model="searchForm"
        inline
      >
        <el-form-item label="文件名">
          <el-input
            v-model="searchForm.fileName"
            placeholder="请输入文件名"
            clearable
          />
        </el-form-item>
        <el-form-item label="文件类型">
          <el-select
            v-model="searchForm.contentType"
            placeholder="请选择文件类型"
            clearable
          >
            <el-option
              label="图片"
              value="image"
            />
            <el-option
              label="文档"
              value="document"
            />
            <el-option
              label="视频"
              value="video"
            />
            <el-option
              label="音频"
              value="audio"
            />
            <el-option
              label="其他"
              value="other"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="可见性">
          <el-select
            v-model="searchForm.isPublic"
            placeholder="请选择可见性"
            clearable
          >
            <el-option
              label="公开"
              :value="true"
            />
            <el-option
              label="私有"
              :value="false"
            />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button
            type="primary"
            @click="handleSearch"
          >
            搜索
          </el-button>
          <el-button @click="handleReset">
            重置
          </el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <!-- 文件列表 -->
    <el-card>
      <el-table
        v-loading="loading"
        :data="fileList"
      >
        <el-table-column
          prop="id"
          label="ID"
          width="80"
        />
        <el-table-column
          prop="fileName"
          label="文件名"
          show-overflow-tooltip
        />
        <el-table-column
          prop="fileSize"
          label="大小"
          width="120"
        >
          <template #default="{ row }">
            {{ formatFileSize(row.fileSize) }}
          </template>
        </el-table-column>
        <el-table-column
          prop="contentType"
          label="类型"
          width="120"
        >
          <template #default="{ row }">
            <el-tag size="small">
              {{ getFileType(row.contentType) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column
          prop="isPublic"
          label="可见性"
          width="100"
        >
          <template #default="{ row }">
            <el-tag
              size="small"
              :type="row.isPublic ? 'success' : 'info'"
            >
              {{ row.isPublic ? '公开' : '私有' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column
          prop="uploaderId"
          label="上传者"
          width="120"
        />
        <el-table-column
          prop="createdAt"
          label="上传时间"
          width="180"
        >
          <template #default="{ row }">
            {{ formatDate(row.createdAt) }}
          </template>
        </el-table-column>
        <el-table-column
          label="操作"
          width="200"
        >
          <template #default="{ row }">
            <el-button
              size="small"
              @click="handleDownload(row)"
            >
              下载
            </el-button>
            <el-button
              size="small"
              @click="handlePreview(row)"
            >
              预览
            </el-button>
            <el-button
              size="small"
              type="danger"
              @click="handleDelete(row)"
            >
              删除
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页 -->
      <div class="pagination-container">
        <el-pagination
          :current-page="pagination.current"
          :page-size="pagination.size"
          :total="pagination.total"
          :page-sizes="[10, 20, 50, 100]"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="handleSizeChange"
          @current-change="handleCurrentChange"
        />
      </div>
    </el-card>

    <!-- 文件上传对话框 -->
    <el-dialog
      v-model="uploadVisible"
      title="上传文件"
      width="600px"
    >
      <el-form
        :model="uploadForm"
        label-width="100px"
      >
        <el-form-item label="文件描述">
          <el-input
            v-model="uploadForm.description"
            placeholder="请输入文件描述（可选）"
          />
        </el-form-item>
        <el-form-item label="是否公开">
          <el-switch v-model="uploadForm.isPublic" />
        </el-form-item>
        <el-form-item>
          <el-upload
            ref="uploadRef"
            class="upload-demo"
            drag
            :auto-upload="false"
            :before-upload="beforeUpload"
            :on-change="handleFileChange"
            :limit="1"
          >
            <el-icon class="el-icon--upload">
              <upload-filled />
            </el-icon>
            <div class="el-upload__text">
              拖拽文件到此处或 <em>点击上传</em>
            </div>
            <template #tip>
              <div class="el-upload__tip">
                请上传不超过 10MB 的文件
              </div>
            </template>
          </el-upload>
        </el-form-item>
      </el-form>
      
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="uploadVisible = false">取消</el-button>
          <el-button
            type="primary"
            :loading="uploading"
            @click="submitUpload"
          >
            {{ uploading ? '上传中...' : '上传' }}
          </el-button>
        </span>
      </template>
    </el-dialog>

    <!-- 文件预览对话框 -->
    <el-dialog
      v-model="previewVisible"
      :title="previewFile?.fileName || '文件预览'"
      width="70%"
    >
      <div
        v-if="previewFile"
        class="preview-content"
      >
        <!-- 文本文件预览 -->
        <div
          v-if="isText(previewFile.contentType)"
          class="text-preview"
        >
          <pre>{{ previewContent }}</pre>
        </div>
        <!-- 图片预览 -->
        <div
          v-else-if="isImage(previewFile.contentType)"
          class="image-preview"
        >
          <img
            :src="previewContent || ''"
            alt="预览图片"
          >
        </div>
        <!-- 视频预览 -->
        <div
          v-else-if="previewFile.contentType?.startsWith('video/')"
          class="video-preview"
        >
          <video
            controls
            :src="previewContent || ''"
          >您的浏览器不支持视频播放</video>
        </div>
        <!-- 音频预览 -->
        <div
          v-else-if="previewFile.contentType?.startsWith('audio/')"
          class="audio-preview"
        >
          <audio
            controls
            :src="previewContent || ''"
          >您的浏览器不支持音频播放</audio>
        </div>
        <!-- 不支持的文件类型 -->
        <div
          v-else
          class="unsupported-preview"
        >
          <el-empty description="不支持此文件类型的预览" />
        </div>
        
        <!-- 文件信息 -->
        <div class="file-info">
          <el-descriptions
            :column="2"
            :border="true"
          >
            <el-descriptions-item label="文件名">
              {{ previewFile.fileName }}
            </el-descriptions-item>
            <el-descriptions-item label="文件大小">
              {{ formatFileSize(previewFile.fileSize || 0) }}
            </el-descriptions-item>
            <el-descriptions-item label="文件类型">
              {{ previewFile.contentType }}
            </el-descriptions-item>
            <el-descriptions-item label="上传时间">
              {{ formatDate(previewFile.createdAt) }}
            </el-descriptions-item>
            <el-descriptions-item
              label="是否公开"
              :span="2"
            >
              <el-tag :type="previewFile.isPublic ? 'success' : 'info'">
                {{ previewFile.isPublic ? '公开' : '私有' }}
              </el-tag>
            </el-descriptions-item>
          </el-descriptions>
        </div>
      </div>
      
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="previewVisible = false">关闭</el-button>
          <el-button
            type="primary"
            @click="previewFile && handleDownload(previewFile)"
          >下载文件</el-button>
        </span>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { UploadInstance, UploadRawFile } from 'element-plus'
import type { FileInfo as ApiFileInfo } from '@/types/api'
import { fileService } from '@/services/fileService'
import type { FileInfo } from '@/services/fileService'

const loading = ref(false)
const uploadVisible = ref(false)
const previewVisible = ref(false)
const uploading = ref(false)
const uploadRef = ref<UploadInstance>()
const selectedFiles = ref<File[]>([])
const previewFile = ref<FileInfo | null>(null)
const previewContent = ref('')

const searchForm = reactive({
  fileName: '',
  contentType: '',
  isPublic: null as boolean | null
})

const uploadForm = reactive({
  description: '',
  isPublic: false
})

const pagination = reactive({
  current: 1,
  size: 10,
  total: 0
})

const stats = reactive({
  totalFiles: 0,
  totalSize: 0,
  publicFiles: 0,
  privateFiles: 0
})

const fileList = ref<FileInfo[]>([])

// 格式化文件大小
const formatFileSize = (bytes: number): string => {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i]
}

// 获取文件类型
const getFileType = (mimeType?: string): string => {
  if (!mimeType) return '未知'
  if (mimeType.startsWith('image/')) return '图片'
  if (mimeType.startsWith('video/')) return '视频'
  if (mimeType.startsWith('audio/')) return '音频'
  if (mimeType.includes('pdf')) return 'PDF'
  if (mimeType.includes('word') || mimeType.includes('document')) return '文档'
  if (mimeType.includes('excel') || mimeType.includes('spreadsheet')) return '表格'
  return '其他'
}

// 格式化日期
const formatDate = (date: string) => {
  return new Date(date).toLocaleString('zh-CN')
}

// 检查是否为图片
const isImage = (mimeType?: string): boolean => {
  return mimeType ? mimeType.startsWith('image/') : false
}

// 检查是否为文本文件
const isText = (mimeType?: string): boolean => {
  const textTypes = ['text/plain', 'application/json', 'application/xml']
  return mimeType ? textTypes.includes(mimeType) : false
}

// 获取文件列表
const fetchFiles = async () => {
  loading.value = true
  try {
    // 使用fileService获取文件列表
    const response = await fileService.getMyFiles()
    
    if (response.success && response.data) {
      fileList.value = response.data.files
      pagination.total = fileList.value.length
      
      // 更新统计信息
      stats.totalFiles = fileList.value.length
      stats.totalSize = fileList.value.reduce((sum, file) => sum + (file.fileSize || 0), 0)
      stats.publicFiles = fileList.value.filter(file => file.isPublic).length
      stats.privateFiles = fileList.value.filter(file => !file.isPublic).length
    }
  } catch (error) {
    console.error('获取文件列表失败:', error)
    ElMessage.error('获取文件列表失败')
  } finally {
    loading.value = false
  }
}

// 搜索
const handleSearch = () => {
  pagination.current = 1
  fetchFiles()
}

// 重置搜索
const handleReset = () => {
  Object.assign(searchForm, {
    fileName: '',
    contentType: '',
    isPublic: null
  })
  pagination.current = 1
  fetchFiles()
}

// 分页大小改变
const handleSizeChange = (size: number) => {
  pagination.size = size
  pagination.current = 1
  fetchFiles()
}

// 当前页改变
const handleCurrentChange = (current: number) => {
  pagination.current = current
  fetchFiles()
}

// 上传文件
const handleUpload = () => {
  uploadVisible.value = true
  // 重置上传表单
  uploadForm.description = ''
  uploadForm.isPublic = false
  selectedFiles.value = []
  uploadRef.value?.clearFiles()
}

// 处理文件选择变化
const handleFileChange = (uploadFile: any, uploadFiles: any[]) => {
  if (uploadFile.status === 'ready') {
    selectedFiles.value.push(uploadFile.raw)
  } else if (uploadFile.status === 'removed') {
    selectedFiles.value = selectedFiles.value.filter(file => file.name !== uploadFile.name)
  }
}

// 上传前验证
const beforeUpload = (file: UploadRawFile) => {
  const isLt10M = file.size / 1024 / 1024 < 10
  if (!isLt10M) {
    ElMessage.error('文件大小不能超过 10MB!')
    return false
  }
  return true
}

// 提交上传
const submitUpload = async () => {
  if (selectedFiles.value.length === 0) {
    ElMessage.warning('请选择要上传的文件')
    return
  }

  uploading.value = true
  try {
    // 逐个上传文件
    for (const file of selectedFiles.value) {
      const params = {
        file,
        description: uploadForm.description,
        isPublic: uploadForm.isPublic
      }
      
      await fileService.uploadFile(params)
    }
    
    ElMessage.success('所有文件上传成功')
    uploadVisible.value = false
    fetchFiles() // 重新获取文件列表
  } catch (error) {
    console.error('文件上传失败:', error)
    ElMessage.error('文件上传失败，请重试')
  } finally {
    uploading.value = false
  }
}

// 下载文件
const handleDownload = async (file: FileInfo) => {
  try {
    // 获取文件下载链接
    const response = await fileService.downloadFile(file.id)
    if (response.success) {
      // 使用Blob下载方式
      ElMessage.success(`开始下载文件: ${file.fileName}`)
    }
  } catch (error) {
    console.error('文件下载失败:', error)
    ElMessage.error('文件下载失败')
  }
}

// 预览文件
const handlePreview = async (file: FileInfo) => {
  previewFile.value = file
  previewVisible.value = true
  
  try {
    // 获取文件预览URL（使用正确的方法名getPreviewUrl）
    const response = await fileService.getPreviewUrl(file.id)
    if (response.success && response.data) {
      // 获取previewUrl
      const previewUrl = response.data.previewUrl
      
      // 对于文本文件，获取文件内容
      if (file.contentType?.startsWith('text/')) {
        try {
          const textResponse = await fetch(previewUrl)
          if (textResponse.ok) {
            previewContent.value = await textResponse.text()
          }
        } catch (textError) {
          console.error('获取文本内容失败:', textError)
          previewContent.value = previewUrl // 保留原始URL作为备选
        }
      } else {
        // 对于非文本文件，直接使用预览URL
        previewContent.value = previewUrl
      }
    }
  } catch (error) {
    console.error('获取文件预览失败:', error)
    previewContent.value = ''
    ElMessage.error('获取文件预览失败')
  }
}

// 删除文件
const handleDelete = async (file: FileInfo) => {
  try {
    await ElMessageBox.confirm(`确定要删除文件 "${file.fileName}" 吗？`, '删除确认', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    
    // 调用API删除文件
    const response = await fileService.deleteFile(file.id)
    if (response.success) {
      ElMessage.success('文件删除成功')
      fetchFiles() // 重新获取文件列表
    }
  } catch (error) {
    if (error !== 'cancel') { // 排除用户取消操作
      console.error('文件删除失败:', error)
      ElMessage.error('文件删除失败')
    }
  }
}

onMounted(() => {
  fetchFiles()
})
</script>

<style scoped lang="scss">
.files-container {
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

.file-stats {
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

.file-preview {
  text-align: center;
  
  .image-preview {
    img {
      max-width: 100%;
      max-height: 500px;
    }
  }
  
  .text-preview {
    pre {
      background: var(--el-bg-color-page);
      padding: 15px;
      border-radius: 4px;
      max-height: 400px;
      overflow: auto;
      text-align: left;
    }
  }
  
  .unsupported-preview {
    padding: 40px 0;
    
    .el-icon {
      font-size: 48px;
      color: var(--el-text-color-placeholder);
      margin-bottom: 20px;
    }
    
    p {
      margin: 0 0 20px 0;
      color: var(--el-text-color-secondary);
    }
  }
}

.upload-demo {
  .el-upload-dragger {
    width: 100%;
  }
}
</style>