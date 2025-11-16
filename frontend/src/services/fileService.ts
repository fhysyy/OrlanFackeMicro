import { api } from './api'
import type { ApiResponse } from '@/types/api'

// 文件上传请求参数
export interface FileUploadParams {
  file: File
  description?: string
  isPublic?: boolean
  previewExpiryMinutes?: number
}

// 文件上传响应
export interface FileUploadResponse {
  fileId: number
  previewUrl: string
  message: string
}

// 文件信息
export interface FileInfo {
  id: number
  fileName: string
  fileSize: number
  contentType: string
  uploadTime: string
  description: string
  isPublic: boolean
}

// 文件预览响应
export interface FilePreviewResponse {
  previewUrl: string
  expiryTime: string
  message: string
}

/**
 * 文件管理服务
 */
export const fileService = {
  /**
   * 上传文件
   * @param params 上传参数
   * @returns 上传结果
   */
  async uploadFile(params: FileUploadParams): Promise<ApiResponse<FileUploadResponse>> {
    const formData = new FormData()
    formData.append('file', params.file)
    
    if (params.description) {
      formData.append('description', params.description)
    }
    
    formData.append('isPublic', params.isPublic?.toString() || 'false')
    
    if (params.previewExpiryMinutes) {
      formData.append('previewExpiryMinutes', params.previewExpiryMinutes.toString())
    }

    try {
      const response = await api.post('/api/files/upload', formData, {
        headers: {
          'Content-Type': 'multipart/form-data'
        },
        timeout: 30000 // 上传文件设置更长的超时时间
      })
      
      return {
        success: response.data.success,
        data: response.data,
        message: response.data.message
      }
    } catch (error) {
      throw error
    }
  },

  /**
   * 下载文件
   * @param fileId 文件ID
   * @returns 文件下载链接（通过blob实现）
   */
  async downloadFile(fileId: number): Promise<void> {
    try {
      const response = await api.get(`/api/files/download/${fileId}`, {
        responseType: 'blob',
        timeout: 30000
      })

      // 从响应头获取文件名
      const contentDisposition = response.headers['content-disposition']
      let fileName = `file_${fileId}`
      
      if (contentDisposition) {
        const match = contentDisposition.match(/filename="([^"]+)"/)
        if (match && match[1]) {
          fileName = decodeURIComponent(match[1])
        }
      }

      // 创建下载链接
      const blob = new Blob([response.data])
      const url = window.URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = fileName
      document.body.appendChild(link)
      link.click()
      
      // 清理
      document.body.removeChild(link)
      window.URL.revokeObjectURL(url)
    } catch (error) {
      throw error
    }
  },

  /**
   * 获取文件预览地址
   * @param fileId 文件ID
   * @returns 预览信息
   */
  async getPreviewUrl(fileId: number): Promise<ApiResponse<FilePreviewResponse>> {
    try {
      const response = await api.get(`/api/files/preview/${fileId}`)
      
      return {
        success: response.data.success,
        data: response.data,
        message: response.data.message
      }
    } catch (error) {
      throw error
    }
  },

  /**
   * 获取公开预览文件
   * @param token 预览令牌
   * @returns 预览URL
   */
  getPublicPreviewUrl(token: string): string {
    return `/api/files/preview?token=${token}`
  },

  /**
   * 获取用户文件列表
   * @returns 文件列表
   */
  async getMyFiles(): Promise<ApiResponse<{ files: FileInfo[] }>> {
    try {
      const response = await api.get('/api/files/myfiles')
      
      return {
        success: response.data.success,
        data: response.data,
        message: response.data.message
      }
    } catch (error) {
      throw error
    }
  },

  /**
   * 删除文件
   * @param fileId 文件ID
   * @returns 删除结果
   */
  async deleteFile(fileId: number): Promise<ApiResponse<{ message: string }>> {
    try {
      const response = await api.delete(`/api/files/${fileId}`)
      
      return {
        success: response.data.success,
        data: response.data,
        message: response.data.message
      }
    } catch (error) {
      throw error
    }
  }
}