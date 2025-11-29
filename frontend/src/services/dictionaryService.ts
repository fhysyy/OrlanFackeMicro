import { api } from './api'
import type {
  DictionaryType,
  DictionaryItem,
  DictionaryTypeCreateRequest,
  DictionaryTypeUpdateRequest,
  DictionaryItemCreateRequest,
  DictionaryItemUpdateRequest
} from '../types/dictionary'
import type { ApiResponse, PaginationParams, PaginationResponse } from '../types/api'

/**
 * 字典类型服务
 */
export const dictionaryTypeService = {
  /**
   * 获取字典类型列表（分页）
   */
  getList(params: PaginationParams & { keyword?: string }) {
    return api.get<ApiResponse<PaginationResponse<DictionaryType>>>('/api/dictionary/types', { params })
  },

  /**
   * 获取字典类型详情
   */
  getDetail(id: string) {
    return api.get<ApiResponse<DictionaryType>>(`/api/dictionary/types/${id}`)
  },

  /**
   * 创建字典类型
   */
  create(data: DictionaryTypeCreateRequest) {
    return api.post<ApiResponse<string>>('/api/dictionary/types', data)
  },

  /**
   * 更新字典类型
   */
  update(id: string, data: DictionaryTypeUpdateRequest) {
    return api.put<ApiResponse<void>>(`/api/dictionary/types/${id}`, data)
  },

  /**
   * 删除字典类型
   */
  delete(id: string) {
    return api.delete<ApiResponse<void>>(`/api/dictionary/types/${id}`)
  },

  /**
   * 批量删除字典类型
   */
  batchDelete(ids: string[]) {
    return api.delete<ApiResponse<void>>('/api/dictionary/types/batch', { params: { ids: ids.join(',') } })
  },

  /**
   * 检查编码是否已存在
   */
  checkCodeExists(code: string, excludeId?: string) {
    return api.get<ApiResponse<boolean>>('/api/dictionary/types/check-code', { 
      params: { code, excludeId } 
    })
  },

  /**
   * 获取所有启用的字典类型（用于下拉选择）
   */
  getAllEnabled() {
    return api.get<ApiResponse<DictionaryType[]>>('/api/dictionary/types/enabled')
  }
}

/**
 * 字典项服务
 */
export const dictionaryItemService = {
  /**
   * 获取字典项列表（分页）
   */
  getList(params: PaginationParams & { 
    dictionaryTypeId?: string; 
    keyword?: string;
    isEnabled?: boolean;
  }) {
    return api.get<ApiResponse<PaginationResponse<DictionaryItem>>>('/api/dictionary/items', { params })
  },

  /**
   * 获取字典项详情
   */
  getDetail(id: string) {
    return api.get<ApiResponse<DictionaryItem>>(`/api/dictionary/items/${id}`)
  },

  /**
   * 根据字典类型获取字典项列表
   */
  getByDictionaryTypeId(dictionaryTypeId: string) {
    return api.get<ApiResponse<DictionaryItem[]>>(`/api/dictionary/items/by-type/${dictionaryTypeId}`)
  },

  /**
   * 根据字典类型编码获取字典项列表
   */
  getByDictionaryTypeCode(dictionaryTypeCode: string) {
    return api.get<ApiResponse<DictionaryItem[]>>(`/api/dictionary/items/by-code/${dictionaryTypeCode}`)
  },

  /**
   * 创建字典项
   */
  create(data: DictionaryItemCreateRequest) {
    return api.post<ApiResponse<string>>('/api/dictionary/items', data)
  },

  /**
   * 更新字典项
   */
  update(id: string, data: DictionaryItemUpdateRequest) {
    return api.put<ApiResponse<void>>(`/api/dictionary/items/${id}`, data)
  },

  /**
   * 删除字典项
   */
  delete(id: string) {
    return api.delete<ApiResponse<void>>(`/api/dictionary/items/${id}`)
  },

  /**
   * 批量删除字典项
   */
  batchDelete(ids: string[]) {
    return api.delete<ApiResponse<void>>('/api/dictionary/items/batch', { params: { ids: ids.join(',') } })
  },

  /**
   * 检查字典项值是否已存在
   */
  checkValueExists(dictionaryTypeId: string, value: string, excludeId?: string) {
    return api.get<ApiResponse<boolean>>('/api/dictionary/items/check-value', { 
      params: { dictionaryTypeId, value, excludeId } 
    })
  },

  /**
   * 批量创建字典项
   */
  batchCreate(dictionaryTypeId: string, items: Omit<DictionaryItemCreateRequest, 'dictionaryTypeId'>[]) {
    return api.post<ApiResponse<void>>('/api/dictionary/items/batch', { dictionaryTypeId, items })
  }
}

export default {
  dictionaryType: dictionaryTypeService,
  dictionaryItem: dictionaryItemService
}