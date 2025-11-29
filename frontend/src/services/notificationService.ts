import { ElNotification } from 'element-plus'
import type { NotificationParams } from 'element-plus'

export interface NotificationOptions extends Partial<NotificationParams> {
  title: string;
  message: string;
  type?: 'success' | 'warning' | 'info' | 'error';
  duration?: number;
  showClose?: boolean;
}

class NotificationService {
  private defaultDuration = 3000

  // 显示普通通知
  show(options: NotificationOptions) {
    const { 
      title, 
      message, 
      type = 'info', 
      duration = this.defaultDuration,
      showClose = true,
      ...rest
    } = options

    ElNotification({
      title,
      message,
      type,
      duration,
      showClose,
      ...rest
    })
  }

  // 显示成功通知
  success(message: string, title = '成功', options?: Partial<NotificationOptions>) {
    this.show({
      title,
      message,
      type: 'success',
      ...options
    })
  }

  // 显示警告通知
  warning(message: string, title = '警告', options?: Partial<NotificationOptions>) {
    this.show({
      title,
      message,
      type: 'warning',
      ...options
    })
  }

  // 显示信息通知
  info(message: string, title = '信息', options?: Partial<NotificationOptions>) {
    this.show({
      title,
      message,
      type: 'info',
      ...options
    })
  }

  // 显示错误通知
  error(message: string, title = '错误', options?: Partial<NotificationOptions>) {
    this.show({
      title,
      message,
      type: 'error',
      duration: 5000, // 错误通知显示时间更长
      ...options
    })
  }

  // 清除所有通知
  clearAll() {
    ElNotification.closeAll()
  }
}

// 导出单例实例
export const notificationService = new NotificationService()

// 导出useNotification组合式API
export function useNotification() {
  return notificationService
}