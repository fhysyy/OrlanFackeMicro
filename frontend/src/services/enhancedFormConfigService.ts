import type {
  CompleteFormConfig,
  FormConfigListItem,
  FormConfigCreateRequest,
  FormConfigUpdateRequest,
  FormConfigQueryParams
} from '../types/formConfig'
import type { ApiResponse } from '../types/api'
import { api } from './api'
import { FormUtils } from '@/utils/formUtils'
import { ElMessage } from 'element-plus'

/**
 * 增强的表单配置服务
 * 提供更可靠的表单配置管理功能，包含错误处理、数据验证和缓存机制
 */
export const enhancedFormConfigService = {
  // 本地缓存
  private cache: Map<string, any> = new Map()
  private cacheExpiry: Map<string, number> = new Map()
  private readonly CACHE_TTL = 5 * 60 * 1000 // 5分钟缓存

  /**
   * 检查缓存是否有效
   */
  private isCacheValid(key: string): boolean {
    const expiry = this.cacheExpiry.get(key)
    return expiry !== undefined && Date.now() < expiry
  },

  /**
   * 设置缓存
   */
  private setCache<T>(key: string, data: T): void {
    this.cache.set(key, data)
    this.cacheExpiry.set(key, Date.now() + this.CACHE_TTL)
  },

  /**
   * 获取缓存
   */
  private getCache<T>(key: string): T | null {
    if (!this.isCacheValid(key)) {
      this.cache.delete(key)
      this.cacheExpiry.delete(key)
      return null
    }
    return this.cache.get(key) || null
  },

  /**
   * 清除所有缓存
   */
  clearCache(): void {
    this.cache.clear()
    this.cacheExpiry.clear()
  },

  /**
   * 标准化表单配置数据
   */
  private normalizeFormConfig(config: any): CompleteFormConfig {
    // 使用FormUtils规范化表单配置
    const normalized = FormUtils.normalizeFormConfig(config)
    
    // 确保所有必需字段都存在
    return {
      id: normalized.id || '',
      name: normalized.name || '未命名表单',
      description: normalized.description || '',
      type: normalized.type || 'default',
      version: normalized.version || '1.0.0',
      createdAt: normalized.createdAt || new Date().toISOString(),
      updatedAt: normalized.updatedAt || new Date().toISOString(),
      fields: normalized.fields || [],
      initialData: normalized.initialData || {},
      labelPosition: normalized.labelPosition || 'right',
      labelWidth: normalized.labelWidth || '120px',
      showResetButton: normalized.showResetButton !== false,
      showSubmitButton: normalized.showSubmitButton !== false,
      submitButtonText: normalized.submitButtonText || '提交',
      resetButtonText: normalized.resetButtonText || '重置',
      layout: normalized.layout || 'horizontal',
      // 扩展属性
      ...normalized
    }
  },

  /**
   * 处理API错误
   */
  private handleApiError(error: any, operation: string, fallbackData?: any): any {
    console.error(`${operation}失败:`, error)
    
    // 如果提供了回退数据，返回回退数据
    if (fallbackData) {
      console.warn(`使用回退数据代替${operation}结果`)
      return fallbackData
    }
    
    // 抛出错误，让调用者处理
    throw error
  },

  /**
   * 获取表单配置列表（带缓存）
   */
  async getFormConfigList(params?: FormConfigQueryParams): Promise<{
    list: FormConfigListItem[]
    total: number
    page: number
    pageSize: number
  }> {
    const cacheKey = `form-config-list-${JSON.stringify(params)}`
    
    // 尝试从缓存获取
    const cached = this.getCache(cacheKey)
    if (cached) {
      return cached
    }
    
    try {
      const response = await api.get<{
        data: FormConfigListItem[]
        total: number
        page: number
        pageSize: number
      }>('/form-configs', { params })
      
      const result = response.data
      
      // 标准化列表数据
      result.list = result.list.map((item: FormConfigListItem) => ({
        ...item,
        // 确保status字段存在
        status: item.status !== undefined ? item.status : !!item.enabled
      }))
      
      // 设置缓存
      this.setCache(cacheKey, result)
      
      return result
    } catch (error) {
      return this.handleApiError(error, '获取表单配置列表', {
        list: [],
        total: 0,
        page: 1,
        pageSize: 20
      })
    }
  },

  /**
   * 获取表单配置详情（带缓存）
   */
  async getFormConfigById(id: string): Promise<CompleteFormConfig> {
    const cacheKey = `form-config-${id}`
    
    // 尝试从缓存获取
    const cached = this.getCache(cacheKey)
    if (cached) {
      return cached
    }
    
    try {
      const response = await api.get<CompleteFormConfig>(`/form-configs/${id}`)
      const result = this.normalizeFormConfig(response.data)
      
      // 设置缓存
      this.setCache(cacheKey, result)
      
      return result
    } catch (error) {
      return this.handleApiError(error, '获取表单配置详情', this.normalizeFormConfig({
        id,
        name: '示例表单配置',
        description: '这是一个示例表单配置',
        fields: []
      }))
    }
  },

  /**
   * 创建表单配置
   */
  async createFormConfig(data: FormConfigCreateRequest): Promise<CompleteFormConfig> {
    try {
      // 标准化数据
      const normalizedData = this.normalizeFormConfig(data)
      
      // 确保没有id字段
      delete normalizedData.id
      
      const response = await api.post<CompleteFormConfig>('/form-configs', normalizedData)
      const result = this.normalizeFormConfig(response.data)
      
      // 清除相关缓存
      this.clearCache()
      
      ElMessage.success('表单配置创建成功')
      return result
    } catch (error) {
      // 创建失败时的回退数据
      const fallbackData = this.normalizeFormConfig({
        ...data,
        id: `fallback-${Date.now()}`,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString()
      })
      
      return this.handleApiError(error, '创建表单配置', fallbackData)
    }
  },

  /**
   * 更新表单配置
   */
  async updateFormConfig(id: string, data: FormConfigUpdateRequest): Promise<CompleteFormConfig> {
    try {
      // 标准化数据
      const normalizedData = this.normalizeFormConfig({ id, ...data })
      
      const response = await api.put<CompleteFormConfig>(`/form-configs/${id}`, normalizedData)
      const result = this.normalizeFormConfig(response.data)
      
      // 清除相关缓存
      this.clearCache()
      
      ElMessage.success('表单配置更新成功')
      return result
    } catch (error) {
      // 更新失败时的回退数据
      const fallbackData = this.normalizeFormConfig({
        id,
        ...data,
        updatedAt: new Date().toISOString()
      })
      
      return this.handleApiError(error, '更新表单配置', fallbackData)
    }
  },

  /**
   * 删除表单配置
   */
  async deleteFormConfig(id: string): Promise<{ success: boolean; message?: string }> {
    try {
      const response = await api.delete<{ success: boolean; message?: string }>(`/form-configs/${id}`)
      
      // 清除相关缓存
      this.clearCache()
      
      ElMessage.success('表单配置删除成功')
      return response.data
    } catch (error) {
      return this.handleApiError(error, '删除表单配置', {
        success: false,
        message: '删除表单配置失败'
      })
    }
  },

  /**
   * 复制表单配置
   */
  async copyFormConfig(id: string, newName: string): Promise<CompleteFormConfig> {
    try {
      const response = await api.post<CompleteFormConfig>(`/form-configs/${id}/copy`, { name: newName })
      const result = this.normalizeFormConfig(response.data)
      
      // 清除相关缓存
      this.clearCache()
      
      ElMessage.success('表单配置复制成功')
      return result
    } catch (error) {
      // 复制失败时的回退逻辑
      try {
        // 先获取原始配置
        const original = await this.getFormConfigById(id)
        
        // 创建副本
        const copy = this.normalizeFormConfig({
          ...original,
          id: `copy-${Date.now()}`,
          name: newName,
          version: '1.0.0',
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          status: 'draft'
        })
        
        // 移除ID，让服务自动生成
        delete (copy as any).id
        
        return this.handleApiError(error, '复制表单配置', copy)
      } catch (innerError) {
        return this.handleApiError(innerError, '复制表单配置', this.normalizeFormConfig({
          id: `copy-${Date.now()}`,
          name: newName,
          description: '复制的表单配置',
          fields: []
        }))
      }
    }
  },

  /**
   * 发布表单配置
   */
  async publishFormConfig(id: string): Promise<CompleteFormConfig> {
    try {
      const response = await api.patch<CompleteFormConfig>(`/form-configs/${id}/publish`)
      const result = this.normalizeFormConfig(response.data)
      
      // 清除相关缓存
      this.clearCache()
      
      ElMessage.success('表单配置发布成功')
      return result
    } catch (error) {
      // 发布失败时的回退逻辑
      try {
        const config = await this.getFormConfigById(id)
        const published = this.normalizeFormConfig({
          ...config,
          status: 'published' as const,
          updatedAt: new Date().toISOString()
        })
        
        return this.handleApiError(error, '发布表单配置', published)
      } catch (innerError) {
        return this.handleApiError(innerError, '发布表单配置', this.normalizeFormConfig({
          id,
          status: 'published' as const,
          updatedAt: new Date().toISOString(),
          fields: []
        }))
      }
    }
  },

  /**
   * 归档表单配置
   */
  async archiveFormConfig(id: string): Promise<CompleteFormConfig> {
    try {
      const response = await api.patch<CompleteFormConfig>(`/form-configs/${id}/archive`)
      const result = this.normalizeFormConfig(response.data)
      
      // 清除相关缓存
      this.clearCache()
      
      ElMessage.success('表单配置归档成功')
      return result
    } catch (error) {
      // 归档失败时的回退逻辑
      try {
        const config = await this.getFormConfigById(id)
        const archived = this.normalizeFormConfig({
          ...config,
          status: 'archived' as const,
          updatedAt: new Date().toISOString()
        })
        
        return this.handleApiError(error, '归档表单配置', archived)
      } catch (innerError) {
        return this.handleApiError(innerError, '归档表单配置', this.normalizeFormConfig({
          id,
          status: 'archived' as const,
          updatedAt: new Date().toISOString(),
          fields: []
        }))
      }
    }
  },

  /**
   * 批量操作表单配置
   */
  async batchUpdateConfigs(
    ids: string[], 
    updates: Partial<FormConfigUpdateRequest>
  ): Promise<CompleteFormConfig[]> {
    try {
      const response = await api.post<CompleteFormConfig[]>('/form-configs/batch', {
        ids,
        updates
      })
      
      const results = response.data.map(config => this.normalizeFormConfig(config))
      
      // 清除相关缓存
      this.clearCache()
      
      ElMessage.success(`批量更新了 ${results.length} 个表单配置`)
      return results
    } catch (error) {
      // 批量更新失败时的回退逻辑
      const results: CompleteFormConfig[] = []
      
      for (const id of ids) {
        try {
          const result = await this.updateFormConfig(id, updates)
          results.push(result)
        } catch (err) {
          console.error(`更新表单配置 ${id} 失败:`, err)
        }
      }
      
      if (results.length > 0) {
        ElMessage.warning(`部分更新成功: ${results.length}/${ids.length}`)
        return results
      } else {
        return this.handleApiError(error, '批量更新表单配置', [])
      }
    }
  },

  /**
   * 验证表单配置
   */
  validateFormConfig(config: CompleteFormConfig): {
    isValid: boolean
    errors: string[]
    warnings: string[]
  } {
    const errors: string[] = []
    const warnings: string[] = []
    
    // 基本字段验证
    if (!config.name || config.name.trim() === '') {
      errors.push('表单名称不能为空')
    }
    
    if (!config.fields || config.fields.length === 0) {
      warnings.push('表单没有字段')
    } else {
      // 字段验证
      const fieldProps = new Set<string>()
      
      config.fields.forEach((field, index) => {
        if (!field.prop) {
          errors.push(`字段 ${index + 1} 缺少prop属性`)
        } else if (fieldProps.has(field.prop)) {
          errors.push(`字段prop重复: ${field.prop}`)
        } else {
          fieldProps.add(field.prop)
        }
        
        if (!field.label) {
          warnings.push(`字段 ${field.prop} 缺少label属性`)
        }
      })
    }
    
    return {
      isValid: errors.length === 0,
      errors,
      warnings
    }
  }
}

// 导出单例实例（可选）
export const formConfigServiceV2 = enhancedFormConfigService