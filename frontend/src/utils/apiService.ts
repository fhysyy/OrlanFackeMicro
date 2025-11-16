// API服务工具
// 统一处理API请求，集成错误处理和加载状态管理

import { api } from '@/services/api';
import { ElMessage, ElMessageBox } from 'element-plus';
import type { AxiosRequestConfig, AxiosResponse } from 'axios';
import { apiCall, handleApiError } from './errorHandler';

/**
 * 带加载状态的API请求配置
 */
export interface ApiRequestOptions extends AxiosRequestConfig {
  showLoading?: boolean;
  loadingMessage?: string;
  showSuccessMessage?: boolean;
  successMessage?: string;
  showErrorMessage?: boolean;
  errorMessage?: string;
  critical?: boolean;
}

/**
 * 统一API服务类
 */
export class ApiService {
  /**
   * 基础GET请求
   */
  static async get<T>(url: string, options: ApiRequestOptions = {}): Promise<T | null> {
    const {
      showLoading = false,
      loadingMessage = '加载中...',
      showSuccessMessage = false,
      successMessage = '获取成功',
      showErrorMessage = true,
      errorMessage = '获取失败',
      critical = false,
      ...axiosOptions
    } = options;

    let loadingInstance: any = null;
    
    try {
      // 显示加载状态
      if (showLoading) {
        loadingInstance = ElMessage.loading(loadingMessage, { duration: 0 });
      }

      // 发送请求
      const response = await api.get<T>(url, axiosOptions);
      
      // 显示成功消息
      if (showSuccessMessage) {
        ElMessage.success(successMessage);
      }

      return response.data;
    } catch (error) {
      await handleApiError(error, {
        showMessage: showErrorMessage,
        defaultMessage: errorMessage,
        critical
      });
      return null;
    } finally {
      // 隐藏加载状态
      if (loadingInstance) {
        loadingInstance.close();
      }
    }
  }

  /**
   * 基础POST请求
   */
  static async post<T>(url: string, data?: any, options: ApiRequestOptions = {}): Promise<T | null> {
    const {
      showLoading = false,
      loadingMessage = '处理中...',
      showSuccessMessage = true,
      successMessage = '操作成功',
      showErrorMessage = true,
      errorMessage = '操作失败',
      critical = false,
      ...axiosOptions
    } = options;

    let loadingInstance: any = null;
    
    try {
      // 显示加载状态
      if (showLoading) {
        loadingInstance = ElMessage.loading(loadingMessage, { duration: 0 });
      }

      // 发送请求
      const response = await api.post<T>(url, data, axiosOptions);
      
      // 显示成功消息
      if (showSuccessMessage) {
        ElMessage.success(successMessage);
      }

      return response.data;
    } catch (error) {
      await handleApiError(error, {
        showMessage: showErrorMessage,
        defaultMessage: errorMessage,
        critical
      });
      return null;
    } finally {
      // 隐藏加载状态
      if (loadingInstance) {
        loadingInstance.close();
      }
    }
  }

  /**
   * 基础PUT请求
   */
  static async put<T>(url: string, data?: any, options: ApiRequestOptions = {}): Promise<T | null> {
    const {
      showLoading = false,
      loadingMessage = '更新中...',
      showSuccessMessage = true,
      successMessage = '更新成功',
      showErrorMessage = true,
      errorMessage = '更新失败',
      critical = false,
      ...axiosOptions
    } = options;

    let loadingInstance: any = null;
    
    try {
      // 显示加载状态
      if (showLoading) {
        loadingInstance = ElMessage.loading(loadingMessage, { duration: 0 });
      }

      // 发送请求
      const response = await api.put<T>(url, data, axiosOptions);
      
      // 显示成功消息
      if (showSuccessMessage) {
        ElMessage.success(successMessage);
      }

      return response.data;
    } catch (error) {
      await handleApiError(error, {
        showMessage: showErrorMessage,
        defaultMessage: errorMessage,
        critical
      });
      return null;
    } finally {
      // 隐藏加载状态
      if (loadingInstance) {
        loadingInstance.close();
      }
    }
  }

  /**
   * 基础DELETE请求
   */
  static async delete<T>(url: string, options: ApiRequestOptions = {}): Promise<T | null> {
    const {
      showLoading = false,
      loadingMessage = '删除中...',
      showSuccessMessage = true,
      successMessage = '删除成功',
      showErrorMessage = true,
      errorMessage = '删除失败',
      critical = false,
      ...axiosOptions
    } = options;

    let loadingInstance: any = null;
    
    try {
      // 显示加载状态
      if (showLoading) {
        loadingInstance = ElMessage.loading(loadingMessage, { duration: 0 });
      }

      // 发送请求
      const response = await api.delete<T>(url, axiosOptions);
      
      // 显示成功消息
      if (showSuccessMessage) {
        ElMessage.success(successMessage);
      }

      return response.data;
    } catch (error) {
      await handleApiError(error, {
        showMessage: showErrorMessage,
        defaultMessage: errorMessage,
        critical
      });
      return null;
    } finally {
      // 隐藏加载状态
      if (loadingInstance) {
        loadingInstance.close();
      }
    }
  }

  /**
   * 带确认对话框的DELETE请求
   */
  static async confirmDelete<T>(
    url: string,
    confirmMessage: string = '确定要删除这条数据吗？此操作不可撤销。',
    options: ApiRequestOptions = {}
  ): Promise<T | null> {
    try {
      await ElMessageBox.confirm(confirmMessage, '确认删除', {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      });
      
      return await this.delete<T>(url, options);
    } catch (error: any) {
      // 如果是用户取消操作，不显示错误消息
      if (error.name !== 'Error') {
        await handleApiError(error, {
          showMessage: options.showErrorMessage !== undefined ? options.showErrorMessage : true,
          defaultMessage: options.errorMessage || '删除取消',
          critical: options.critical
        });
      }
      return null;
    }
  }

  /**
   * 批量删除操作
   */
  static async batchDelete<T>(
    baseUrl: string,
    ids: string[],
    options: ApiRequestOptions = {}
  ): Promise<T | null> {
    if (!ids || ids.length === 0) {
      ElMessage.warning('请选择要删除的数据');
      return null;
    }

    return this.confirmDelete<T>(
      `${baseUrl}/batch`,
      `确定要删除选中的 ${ids.length} 条数据吗？此操作不可撤销。`,
      {
        ...options,
        method: 'delete',
        data: { ids },
        successMessage: `成功删除 ${ids.length} 条数据`
      }
    );
  }
}

// 导出实例
export const apiService = ApiService;
