import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormConfig } from '@/types/form'

/**
 * 增强的错误恢复服务
 * 提供表单数据的自动保存、恢复和错误处理功能
 */
export class EnhancedErrorService {
  private static instance: EnhancedErrorService
  
  // 本地存储键
  private readonly STORAGE_KEY_PREFIX = 'form-error-recovery-'
  
  // 自动保存间隔（毫秒）
  private readonly AUTO_SAVE_INTERVAL = 5000
  
  // 最大历史记录数量
  private readonly MAX_HISTORY_COUNT = 10
  
  // 错误历史记录
  private errorHistory: Array<{
    id: string
    timestamp: number
    error: Error
    formData: any
    formConfig: FormConfig
    resolved: boolean
  }> = []
  
  // 表单数据备份
  private formDataBackups: Record<string, Array<{
    timestamp: number
    data: any
  }>> = {}
  
  // 自动保存定时器
  private autoSaveTimers: Record<string, NodeJS.Timeout> = {}
  
  private constructor() {}
  
  /**
   * 获取单例实例
   */
  static getInstance(): EnhancedErrorService {
    if (!EnhancedErrorService.instance) {
      EnhancedErrorService.instance = new EnhancedErrorService()
    }
    return EnhancedErrorService.instance
  }
  
  /**
   * 启用表单的自动保存
   */
  enableAutoSave(formId: string, getFormData: () => any, getFormConfig: () => FormConfig) {
    // 清除已有的定时器
    this.disableAutoSave(formId)
    
    // 立即保存一次
    this.saveFormData(formId, getFormData(), getFormConfig())
    
    // 设置定时保存
    this.autoSaveTimers[formId] = setInterval(() => {
      this.saveFormData(formId, getFormData(), getFormConfig())
    }, this.AUTO_SAVE_INTERVAL)
  }
  
  /**
   * 禁用表单的自动保存
   */
  disableAutoSave(formId: string) {
    if (this.autoSaveTimers[formId]) {
      clearInterval(this.autoSaveTimers[formId])
      delete this.autoSaveTimers[formId]
    }
  }
  
  /**
   * 保存表单数据
   */
  saveFormData(formId: string, formData: any, formConfig: FormConfig) {
    try {
      // 确保备份数组存在
      if (!this.formDataBackups[formId]) {
        this.formDataBackups[formId] = []
      }
      
      // 添加新的备份
      const backup = {
        timestamp: Date.now(),
        data: JSON.parse(JSON.stringify(formData))
      }
      
      this.formDataBackups[formId].push(backup)
      
      // 限制历史记录数量
      if (this.formDataBackups[formId].length > this.MAX_HISTORY_COUNT) {
        this.formDataBackups[formId] = this.formDataBackups[formId].slice(-this.MAX_HISTORY_COUNT)
      }
      
      // 保存到本地存储
      this.saveToLocalStorage(formId, {
        backups: this.formDataBackups[formId],
        formConfig
      })
    } catch (error) {
      console.error('保存表单数据失败:', error)
    }
  }
  
  /**
   * 获取表单数据备份
   */
  getFormDataBackups(formId: string): Array<{
    timestamp: number
    data: any
  }> {
    // 先从内存中获取
    if (this.formDataBackups[formId]) {
      return [...this.formDataBackups[formId]]
    }
    
    // 从本地存储获取
    try {
      const saved = this.loadFromLocalStorage(formId)
      if (saved && saved.backups) {
        this.formDataBackups[formId] = saved.backups
        return [...saved.backups]
      }
    } catch (error) {
      console.error('加载表单备份失败:', error)
    }
    
    return []
  }
  
  /**
   * 恢复表单数据
   */
  recoverFormData(formId: string, backupTimestamp?: number): any {
    const backups = this.getFormDataBackups(formId)
    
    if (backups.length === 0) {
      return null
    }
    
    // 如果没有指定时间戳，使用最新的备份
    let targetBackup = backups[backups.length - 1]
    
    // 如果指定了时间戳，找到最接近的备份
    if (backupTimestamp) {
      targetBackup = backups.reduce((prev, curr) => {
        return Math.abs(curr.timestamp - backupTimestamp) < Math.abs(prev.timestamp - backupTimestamp)
          ? curr
          : prev
      })
    }
    
    return JSON.parse(JSON.stringify(targetBackup.data))
  }
  
  /**
   * 记录错误
   */
  recordError(
    formId: string,
    error: Error,
    formData: any,
    formConfig: FormConfig
  ): string {
    const errorRecord = {
      id: this.generateId(),
      timestamp: Date.now(),
      error,
      formData: JSON.parse(JSON.stringify(formData)),
      formConfig: JSON.parse(JSON.stringify(formConfig)),
      resolved: false
    }
    
    this.errorHistory.push(errorRecord)
    
    // 限制历史记录数量
    if (this.errorHistory.length > this.MAX_HISTORY_COUNT) {
      this.errorHistory = this.errorHistory.slice(-this.MAX_HISTORY_COUNT)
    }
    
    return errorRecord.id
  }
  
  /**
   * 标记错误为已解决
   */
  resolveError(errorId: string) {
    const errorRecord = this.errorHistory.find(r => r.id === errorId)
    if (errorRecord) {
      errorRecord.resolved = true
    }
  }
  
  /**
   * 获取错误历史记录
   */
  getErrorHistory(formId?: string): Array<{
    id: string
    timestamp: number
    error: Error
    formData: any
    formConfig: FormConfig
    resolved: boolean
  }> {
    if (formId) {
      return this.errorHistory.filter(record => {
        try {
          return record.formConfig.id === formId
        } catch (e) {
          return false
        }
      })
    }
    return [...this.errorHistory]
  }
  
  /**
   * 尝试从错误中恢复
   */
  async attemptErrorRecovery(
    formId: string,
    errorId: string
  ): Promise<{ success: boolean, data?: any, message: string }> {
    const errorRecord = this.errorHistory.find(r => r.id === errorId)
    if (!errorRecord) {
      return {
        success: false,
        message: '找不到指定的错误记录'
      }
    }
    
    try {
      // 分析错误类型
      const errorType = this.categorizeError(errorRecord.error)
      
      // 根据错误类型采取不同的恢复策略
      let recoveryData: any
      let message: string
      
      switch (errorType) {
        case 'validation':
          recoveryData = this.recoverFromValidationError(errorRecord)
          message = '已尝试恢复验证错误，请检查表单内容'
          break
          
        case 'network':
          recoveryData = await this.recoverFromNetworkError(errorRecord)
          message = '已恢复离线状态下的表单数据'
          break
          
        case 'data_corruption':
          recoveryData = this.recoverFromDataCorruption(errorRecord, formId)
          message = '已从最近的备份中恢复表单数据'
          break
          
        default:
          recoveryData = errorRecord.formData
          message = '已恢复错误发生前的表单状态'
      }
      
      this.resolveError(errorId)
      
      return {
        success: true,
        data: recoveryData,
        message
      }
    } catch (recoveryError) {
      console.error('恢复过程中发生错误:', recoveryError)
      return {
        success: false,
        message: '恢复失败: ' + (recoveryError as Error).message
      }
    }
  }
  
  /**
   * 分类错误类型
   */
  private categorizeError(error: Error): string {
    const message = error.message.toLowerCase()
    const stack = error.stack?.toLowerCase() || ''
    
    if (message.includes('network') || message.includes('fetch') || stack.includes('network')) {
      return 'network'
    }
    
    if (message.includes('validation') || message.includes('valid') || stack.includes('validation')) {
      return 'validation'
    }
    
    if (message.includes('circular') || message.includes('reference') || stack.includes('circular')) {
      return 'data_corruption'
    }
    
    return 'unknown'
  }
  
  /**
   * 从验证错误中恢复
   */
  private recoverFromValidationError(errorRecord: any): any {
    // 验证错误通常不需要恢复数据，只需要清理无效值
    const formData = errorRecord.formData
    const formConfig = errorRecord.formConfig
    
    if (!formConfig.fields) return formData
    
    // 清理无效字段
    formConfig.fields.forEach((field: any) => {
      if (field.prop && formData[field.prop] !== undefined) {
        // 根据字段类型进行基本验证
        if (field.type === 'number' && isNaN(Number(formData[field.prop]))) {
          formData[field.prop] = field.value || 0
        }
      }
    })
    
    return formData
  }
  
  /**
   * 从网络错误中恢复
   */
  private async recoverFromNetworkError(errorRecord: any): Promise<any> {
    // 网络错误通常意味着表单数据没有成功保存，直接返回原始数据
    return errorRecord.formData
  }
  
  /**
   * 从数据损坏中恢复
   */
  private recoverFromDataCorruption(errorRecord: any, formId: string): any {
    // 尝试从最近的备份中恢复
    const backups = this.getFormDataBackups(formId)
    
    if (backups.length > 1) {
      // 获取倒数第二个备份（最旧的备份）
      return backups[backups.length - 2].data
    }
    
    // 如果没有备份，尝试清理循环引用
    return this.cleanCircularReferences(errorRecord.formData)
  }
  
  /**
   * 清理循环引用
   */
  private cleanCircularReferences(obj: any): any {
    if (obj === null || typeof obj !== 'object') {
      return obj
    }
    
    const seen = new WeakSet()
    const clone = Array.isArray(obj) ? [] : {}
    
    const clean = (source: any, target: any) => {
      if (seen.has(source)) {
        return
      }
      
      seen.add(source)
      
      for (const key in source) {
        if (Object.prototype.hasOwnProperty.call(source, key)) {
          if (typeof source[key] === 'object' && source[key] !== null) {
            target[key] = Array.isArray(source[key]) ? [] : {}
            clean(source[key], target[key])
          } else {
            target[key] = source[key]
          }
        }
      }
    }
    
    clean(obj, clone)
    return clone
  }
  
  /**
   * 显示错误恢复对话框
   */
  async showErrorRecoveryDialog(
    formId: string,
    errorId: string,
    onRecover: (data: any) => void,
    onReset?: () => void
  ): Promise<void> {
    const errorRecord = this.errorHistory.find(r => r.id === errorId)
    if (!errorRecord) {
      ElMessage.error('找不到错误记录')
      return
    }
    
    // 显示备份选项
    const backups = this.getFormDataBackups(formId)
    const backupOptions = backups.map((backup, index) => {
      const date = new Date(backup.timestamp)
      return {
        label: `备份 ${index + 1} (${date.toLocaleString()})`,
        value: backup.timestamp
      }
    })
    
    const action = await ElMessageBox.confirm(
      `表单发生错误: ${errorRecord.error.message}`,
      '表单错误处理',
      {
        confirmButtonText: '尝试恢复',
        cancelButtonText: '重置表单',
        type: 'error',
        showClose: false,
        closeOnClickModal: false,
        closeOnPressEscape: false
      }
    ).catch(() => 'reset')
    
    if (action === 'confirm') {
      const result = await this.attemptErrorRecovery(formId, errorId)
      
      if (result.success && result.data) {
        onRecover(result.data)
        ElMessage.success(result.message)
      } else {
        ElMessage.error(result.message)
      }
    } else if (action === 'reset' && onReset) {
      onReset()
    }
  }
  
  /**
   * 保存到本地存储
   */
  private saveToLocalStorage(formId: string, data: any) {
    try {
      const key = this.STORAGE_KEY_PREFIX + formId
      localStorage.setItem(key, JSON.stringify(data))
    } catch (error) {
      console.error('保存到本地存储失败:', error)
    }
  }
  
  /**
   * 从本地存储加载
   */
  private loadFromLocalStorage(formId: string): any {
    try {
      const key = this.STORAGE_KEY_PREFIX + formId
      const data = localStorage.getItem(key)
      return data ? JSON.parse(data) : null
    } catch (error) {
      console.error('从本地存储加载失败:', error)
      return null
    }
  }
  
  /**
   * 清除表单的本地存储数据
   */
  clearLocalStorage(formId: string) {
    try {
      const key = this.STORAGE_KEY_PREFIX + formId
      localStorage.removeItem(key)
    } catch (error) {
      console.error('清除本地存储失败:', error)
    }
  }
  
  /**
   * 生成唯一ID
   */
  private generateId(): string {
    return Date.now().toString(36) + Math.random().toString(36).substr(2)
  }
}

// 导出单例实例
export const enhancedErrorService = EnhancedErrorService.getInstance()