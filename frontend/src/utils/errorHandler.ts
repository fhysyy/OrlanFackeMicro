// API错误处理工具
// 为所有API调用提供统一的错误处理机制

import { ElMessage, ElMessageBox } from 'element-plus';
import type { AxiosError } from 'axios';

/**
 * API响应错误类型
 */
export interface ApiError {
  code: string;
  message: string;
  details?: any;
}

/**
 * 错误处理配置选项
 */
export interface ErrorHandlerOptions {
  showMessage?: boolean;
  showDetail?: boolean;
  defaultMessage?: string;
  critical?: boolean;
  onError?: (error: ApiError) => void;
  onSuccess?: () => void;
}

/**
 * 从Axios错误中提取API错误信息
 */
export function extractApiError(error: any): ApiError {
  // 如果是Axios错误
  if (error.isAxiosError) {
    const axiosError = error as AxiosError;
    
    // 处理响应错误
    if (axiosError.response) {
      const data = axiosError.response.data as any;
      
      // 如果响应包含标准错误格式
      if (data && typeof data === 'object') {
        return {
          code: data.code || String(axiosError.response.status),
          message: data.message || data.error || axiosError.message,
          details: data.details
        };
      }
      
      // HTTP状态码处理
      switch (axiosError.response.status) {
        case 400:
          return { code: 'BAD_REQUEST', message: '请求参数错误', details: axiosError.response.data };
        case 401:
          return { code: 'UNAUTHORIZED', message: '未授权访问', details: axiosError.response.data };
        case 403:
          return { code: 'FORBIDDEN', message: '没有权限执行此操作', details: axiosError.response.data };
        case 404:
          return { code: 'NOT_FOUND', message: '请求的资源不存在', details: axiosError.response.data };
        case 500:
          return { code: 'SERVER_ERROR', message: '服务器内部错误', details: axiosError.response.data };
        default:
          return {
            code: String(axiosError.response.status),
            message: `请求失败: ${axiosError.message}`,
            details: axiosError.response.data
          };
      }
    }
    
    // 处理请求错误（如网络问题）
    if (axiosError.request) {
      return { code: 'NETWORK_ERROR', message: '网络连接失败，请检查网络设置', details: axiosError };
    }
  }
  
  // 其他类型的错误
  return {
    code: 'UNKNOWN_ERROR',
    message: error.message || '未知错误',
    details: error
  };
}

/**
 * 统一的API错误处理函数
 */
export async function handleApiError(
  error: any,
  options: ErrorHandlerOptions = {}
): Promise<ApiError> {
  const {
    showMessage = true,
    showDetail = false,
    defaultMessage = '操作失败',
    critical = false,
    onError,
    onSuccess
  } = options;
  
  // 提取错误信息
  const apiError = extractApiError(error);
  
  // 记录错误日志
  console.error('API Error:', apiError);
  
  // 处理特定错误码
  switch (apiError.code) {
    case 'UNAUTHORIZED':
      // 未授权处理 - 清除token并重定向到登录页
      if (typeof window !== 'undefined') {
        // 这里可以触发登出逻辑
        ElMessageBox.alert('登录已过期，请重新登录', '提示', {
          confirmButtonText: '确定',
          callback: () => {
            // 触发登出
            window.location.href = '/login';
          }
        });
      }
      break;
    
    case 'FORBIDDEN':
      // 权限不足处理
      if (showMessage) {
        ElMessage.error('您没有权限执行此操作');
      }
      break;
    
    case 'NETWORK_ERROR':
      // 网络错误处理
      if (showMessage) {
        ElMessage.error('网络连接失败，请检查您的网络设置');
      }
      break;
    
    default:
      // 默认错误处理
      if (showMessage) {
        const message = apiError.message || defaultMessage;
        if (showDetail && apiError.details) {
          ElMessage.error(`${message}: ${JSON.stringify(apiError.details)}`);
        } else {
          ElMessage.error(message);
        }
      }
  }
  
  // 处理严重错误
  if (critical) {
    await ElMessageBox.alert(
      `${apiError.message || defaultMessage}\n错误代码: ${apiError.code}`,
      '操作失败',
      {
        confirmButtonText: '确定',
        type: 'error'
      }
    );
  }
  
  // 调用自定义错误处理函数
  if (onError) {
    onError(apiError);
  }
  
  return apiError;
}

/**
 * API调用包装函数，提供自动错误处理
 */
export async function apiCall<T>(
  apiFn: () => Promise<T>,
  options: ErrorHandlerOptions = {}
): Promise<T | null> {
  try {
    const result = await apiFn();
    
    // 调用成功回调
    if (options.onSuccess) {
      options.onSuccess();
    }
    
    return result;
  } catch (error) {
    await handleApiError(error, options);
    return null;
  }
}

/**
 * 创建带加载状态的API调用包装函数
 */
export function createLoadingApiCall<T>(
  loading: { value: boolean }
) {
  return async (
    apiFn: () => Promise<T>,
    options: ErrorHandlerOptions = {}
  ): Promise<T | null> => {
    loading.value = true;
    try {
      return await apiCall(apiFn, options);
    } finally {
      loading.value = false;
    }
  };
}
