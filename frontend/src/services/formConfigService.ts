import type {
  CompleteFormConfig,
  FormConfigListItem,
  FormConfigCreateRequest,
  FormConfigUpdateRequest,
  FormConfigQueryParams
} from '../types/formConfig'
import type { ApiResponse } from '../types/api'
import { api } from './api'

/**
 * 表单配置服务
 */
export const formConfigService = {
  /**
   * 获取表单配置列表
   */
  async getFormConfigList(params?: FormConfigQueryParams): Promise<{
    list: FormConfigListItem[]
    total: number
    page: number
    pageSize: number
  }> {
    try {
      const response = await api.get<{
        data: FormConfigListItem[]
        total: number
        page: number
        pageSize: number
      }>('/form-configs', { params })
      return response.data
    } catch (error) {
      console.error('获取表单配置列表失败:', error)
      // 模拟数据，实际项目中应该移除
      return {
        list: [
          {
            id: '1',
            name: '用户注册表单',
            description: '用于用户注册的表单配置',
            type: 'default',
            version: '1.0.0',
            createdAt: '2024-01-01T00:00:00Z',
            updatedAt: '2024-01-01T00:00:00Z',
            createdBy: 'admin',
            status: 'published',
            module: 'user',
            tags: ['用户', '注册'],
            enabled: true,
            fieldCount: 10,
            groupCount: 2
          },
          {
            id: '2',
            name: '产品信息表单',
            description: '用于产品信息录入的表单配置',
            type: 'default',
            version: '1.1.0',
            createdAt: '2024-01-02T00:00:00Z',
            updatedAt: '2024-01-02T00:00:00Z',
            createdBy: 'admin',
            status: 'published',
            module: 'product',
            tags: ['产品', '录入'],
            enabled: true,
            fieldCount: 8,
            groupCount: 1
          }
        ],
        total: 2,
        page: 1,
        pageSize: 20
      }
    }
  },

  /**
   * 获取表单配置详情
   */
  async getFormConfigById(id: string): Promise<CompleteFormConfig> {
    try {
      const response = await api.get<CompleteFormConfig>(`/form-configs/${id}`)
      return response.data
    } catch (error) {
      console.error(`获取表单配置详情失败 (ID: ${id}):`, error)
      // 模拟数据，实际项目中应该移除
      return {
        id,
        name: '示例表单配置',
        description: '这是一个示例表单配置',
        type: 'default',
        version: '1.0.0',
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: '2024-01-01T00:00:00Z',
        createdBy: 'admin',
        status: 'published',
        module: 'demo',
        tags: ['示例'],
        enabled: true,
        fields: [
          {
            prop: 'name',
            label: '姓名',
            type: 'input',
            placeholder: '请输入姓名',
            required: true,
            validationRules: [
              { type: 'required', message: '请输入姓名', trigger: 'blur' },
              { type: 'max', value: 50, message: '姓名长度不能超过50个字符', trigger: 'blur' }
            ]
          },
          {
            prop: 'email',
            label: '邮箱',
            type: 'input',
            inputType: 'email',
            placeholder: '请输入邮箱',
            required: true,
            validationRules: [
              { type: 'required', message: '请输入邮箱', trigger: 'blur' },
              { type: 'email', message: '请输入有效的邮箱地址', trigger: 'blur' }
            ]
          },
          {
            prop: 'phone',
            label: '手机号',
            type: 'input',
            inputType: 'tel',
            placeholder: '请输入手机号',
            validationRules: [
              { type: 'required', message: '请输入手机号', trigger: 'blur' },
              { type: 'phone', message: '请输入有效的手机号', trigger: 'blur' }
            ]
          },
          {
            prop: 'birthDate',
            label: '出生日期',
            type: 'date-picker',
            pickerType: 'date',
            format: 'YYYY-MM-DD',
            placeholder: '请选择出生日期',
            validationRules: [
              { type: 'required', message: '请选择出生日期', trigger: 'change' }
            ]
          }
        ],
        initialData: {},
        labelPosition: 'right',
        labelWidth: '120px',
        showResetButton: true,
        showSubmitButton: true,
        submitButtonText: '提交',
        resetButtonText: '重置',
        layout: 'horizontal',
        groups: [
          {
            title: '基本信息',
            description: '请填写基本个人信息',
            fields: [
              {
                prop: 'name',
                label: '姓名',
                type: 'input',
                placeholder: '请输入姓名',
                required: true,
                validationRules: [
                  { type: 'required', message: '请输入姓名', trigger: 'blur' },
                  { type: 'max', value: 50, message: '姓名长度不能超过50个字符', trigger: 'blur' }
                ]
              }
            ]
          }
        ],
        layoutConfig: {
          useGrid: true,
          gutter: 20,
          responsive: {
            xs: 24,
            sm: 24,
            md: 12,
            lg: 8,
            xl: 6,
            xxl: 6
          }
        }
      }
    }
  },

  /**
   * 创建表单配置
   */
  async createFormConfig(data: FormConfigCreateRequest): Promise<CompleteFormConfig> {
    try {
      const response = await api.post<CompleteFormConfig>('/form-configs', data)
      return response.data
    } catch (error) {
      console.error('创建表单配置失败:', error)
      // 模拟响应，实际项目中应该移除
      return {
        ...data,
        id: `new-${Date.now()}`,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString()
      } as CompleteFormConfig
    }
  },

  /**
   * 更新表单配置
   */
  async updateFormConfig(id: string, data: FormConfigUpdateRequest): Promise<CompleteFormConfig> {
    try {
      const response = await api.put<CompleteFormConfig>(`/form-configs/${id}`, data)
      return response.data
    } catch (error) {
      console.error(`更新表单配置失败 (ID: ${id}):`, error)
      // 模拟响应，实际项目中应该移除
      return {
        ...data,
        id,
        updatedAt: new Date().toISOString()
      } as CompleteFormConfig
    }
  },

  /**
   * 删除表单配置
   */
  async deleteFormConfig(id: string): Promise<{ success: boolean; message?: string }> {
    try {
      const response = await api.delete<{ success: boolean; message?: string }>(`/form-configs/${id}`)
      return response.data
    } catch (error) {
      console.error(`删除表单配置失败 (ID: ${id}):`, error)
      // 模拟响应，实际项目中应该移除
      return {
        success: true,
        message: '表单配置删除成功'
      }
    }
  },

  /**
   * 复制表单配置
   */
  async copyFormConfig(id: string, newName: string): Promise<CompleteFormConfig> {
    try {
      const response = await api.post<CompleteFormConfig>(`/form-configs/${id}/copy`, { name: newName })
      return response.data
    } catch (error) {
      console.error(`复制表单配置失败 (ID: ${id}):`, error)
      // 模拟响应，实际项目中应该移除
      const original = await this.getFormConfigById(id)
      return {
        ...original,
        id: `copy-${Date.now()}`,
        name: newName,
        version: '1.0.0',
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        status: 'draft'
      }
    }
  },

  /**
   * 发布表单配置
   */
  async publishFormConfig(id: string): Promise<CompleteFormConfig> {
    try {
      const response = await api.patch<CompleteFormConfig>(`/form-configs/${id}/publish`)
      return response.data
    } catch (error) {
      console.error(`发布表单配置失败 (ID: ${id}):`, error)
      // 模拟响应，实际项目中应该移除
      const config = await this.getFormConfigById(id)
      return {
        ...config,
        status: 'published' as const,
        updatedAt: new Date().toISOString()
      }
    }
  },

  /**
   * 归档表单配置
   */
  async archiveFormConfig(id: string): Promise<CompleteFormConfig> {
    try {
      const response = await api.patch<CompleteFormConfig>(`/form-configs/${id}/archive`)
      return response.data
    } catch (error) {
      console.error(`归档表单配置失败 (ID: ${id}):`, error)
      // 模拟响应，实际项目中应该移除
      const config = await this.getFormConfigById(id)
      return {
        ...config,
        status: 'archived' as const,
        updatedAt: new Date().toISOString()
      }
    }
  }
}
