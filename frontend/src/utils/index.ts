// 通用工具函数

/**
 * 格式化日期时间
 * @param date 日期字符串或Date对象
 * @param format 格式模板，默认 'YYYY-MM-DD HH:mm:ss'
 */
export const formatDate = (date: string | Date, format = 'YYYY-MM-DD HH:mm:ss'): string => {
  if (!date) return ''
  
  const d = new Date(date)
  if (isNaN(d.getTime())) return ''
  
  const year = d.getFullYear()
  const month = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  const hours = String(d.getHours()).padStart(2, '0')
  const minutes = String(d.getMinutes()).padStart(2, '0')
  const seconds = String(d.getSeconds()).padStart(2, '0')
  
  return format
    .replace('YYYY', String(year))
    .replace('MM', month)
    .replace('DD', day)
    .replace('HH', hours)
    .replace('mm', minutes)
    .replace('ss', seconds)
}

/**
 * 格式化文件大小
 * @param bytes 字节数
 */
export const formatFileSize = (bytes: number): string => {
  if (bytes === 0) return '0 B'
  
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i]
}

/**
 * 防抖函数
 * @param func 要执行的函数
 * @param wait 等待时间（毫秒）
 */
export const debounce = <T extends (...args: any[]) => any>(
  func: T,
  wait: number
): ((...args: Parameters<T>) => void) => {
  let timeout: NodeJS.Timeout | null = null
  
  return (...args: Parameters<T>) => {
    if (timeout) clearTimeout(timeout)
    timeout = setTimeout(() => func(...args), wait)
  }
}

/**
 * 节流函数
 * @param func 要执行的函数
 * @param wait 等待时间（毫秒）
 */
export const throttle = <T extends (...args: any[]) => any>(
  func: T,
  wait: number
): ((...args: Parameters<T>) => void) => {
  let timeout: NodeJS.Timeout | null = null
  let previous = 0
  
  return (...args: Parameters<T>) => {
    const now = Date.now()
    const remaining = wait - (now - previous)
    
    if (remaining <= 0 || remaining > wait) {
      if (timeout) {
        clearTimeout(timeout)
        timeout = null
      }
      previous = now
      func(...args)
    } else if (!timeout) {
      timeout = setTimeout(() => {
        previous = Date.now()
        timeout = null
        func(...args)
      }, remaining)
    }
  }
}

/**
 * 深拷贝对象
 * @param obj 要拷贝的对象
 */
export const deepClone = <T>(obj: T): T => {
  if (obj === null || typeof obj !== 'object') return obj
  
  if (obj instanceof Date) return new Date(obj.getTime()) as T
  if (obj instanceof Array) return obj.map(item => deepClone(item)) as T
  if (obj instanceof Object) {
    const cloned = {} as T
    for (const key in obj) {
      if (obj.hasOwnProperty(key)) {
        cloned[key] = deepClone(obj[key])
      }
    }
    return cloned
  }
  
  return obj
}

/**
 * 生成随机ID
 * @param length ID长度，默认8
 */
export const generateId = (length = 8): string => {
  const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789'
  let result = ''
  for (let i = 0; i < length; i++) {
    result += chars.charAt(Math.floor(Math.random() * chars.length))
  }
  return result
}

/**
 * 验证邮箱格式
 * @param email 邮箱地址
 */
export const validateEmail = (email: string): boolean => {
  const regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  return regex.test(email)
}

/**
 * 验证手机号格式
 * @param phone 手机号码
 */
export const validatePhone = (phone: string): boolean => {
  const regex = /^1[3-9]\d{9}$/
  return regex.test(phone)
}

/**
 * 验证密码强度
 * @param password 密码
 * @returns 强度等级：0-弱，1-中，2-强
 */
export const validatePasswordStrength = (password: string): number => {
  if (!password) return 0
  
  let strength = 0
  if (password.length >= 8) strength++
  if (/[a-z]/.test(password) && /[A-Z]/.test(password)) strength++
  if (/\d/.test(password)) strength++
  if (/[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(password)) strength++
  
  return Math.min(Math.floor(strength / 2), 2)
}

/**
 * 获取文件扩展名
 * @param filename 文件名
 */
export const getFileExtension = (filename: string): string => {
  return filename.slice((filename.lastIndexOf('.') - 1 >>> 0) + 2)
}

/**
 * 下载文件
 * @param data 文件数据
 * @param filename 文件名
 * @param mimeType MIME类型
 */
export const downloadFile = (data: BlobPart, filename: string, mimeType = 'application/octet-stream'): void => {
  const blob = new Blob([data], { type: mimeType })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = filename
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  URL.revokeObjectURL(url)
}

/**
 * 复制文本到剪贴板
 * @param text 要复制的文本
 */
export const copyToClipboard = async (text: string): Promise<boolean> => {
  try {
    if (navigator.clipboard && window.isSecureContext) {
      await navigator.clipboard.writeText(text)
      return true
    } else {
      // 降级方案
      const textArea = document.createElement('textarea')
      textArea.value = text
      textArea.style.position = 'fixed'
      textArea.style.opacity = '0'
      document.body.appendChild(textArea)
      textArea.select()
      const successful = document.execCommand('copy')
      document.body.removeChild(textArea)
      return successful
    }
  } catch {
    return false
  }
}

/**
 * 获取URL参数
 * @param name 参数名
 */
export const getUrlParam = (name: string): string | null => {
  const urlParams = new URLSearchParams(window.location.search)
  return urlParams.get(name)
}

/**
 * 设置URL参数
 * @param params 参数对象
 */
export const setUrlParams = (params: Record<string, string>): void => {
  const url = new URL(window.location.href)
  Object.keys(params).forEach(key => {
    url.searchParams.set(key, params[key])
  })
  window.history.replaceState({}, '', url.toString())
}

/**
 * 移除URL参数
 * @param names 要移除的参数名数组
 */
export const removeUrlParams = (names: string[]): void => {
  const url = new URL(window.location.href)
  names.forEach(name => {
    url.searchParams.delete(name)
  })
  window.history.replaceState({}, '', url.toString())
}

/**
 * 生成颜色渐变
 * @param startColor 起始颜色
 * @param endColor 结束颜色
 * @param steps 步数
 */
export const generateGradient = (startColor: string, endColor: string, steps: number): string[] => {
  const start = parseInt(startColor.slice(1), 16)
  const end = parseInt(endColor.slice(1), 16)
  
  const startR = (start >> 16) & 0xff
  const startG = (start >> 8) & 0xff
  const startB = start & 0xff
  
  const endR = (end >> 16) & 0xff
  const endG = (end >> 8) & 0xff
  const endB = end & 0xff
  
  const gradient = []
  
  for (let i = 0; i < steps; i++) {
    const r = Math.round(startR + (endR - startR) * (i / (steps - 1)))
    const g = Math.round(startG + (endG - startG) * (i / (steps - 1)))
    const b = Math.round(startB + (endB - startB) * (i / (steps - 1)))
    
    gradient.push(`#${((1 << 24) + (r << 16) + (g << 8) + b).toString(16).slice(1)}`)
  }
  
  return gradient
}

/**
 * 数字格式化（千分位）
 * @param num 数字
 */
export const formatNumber = (num: number): string => {
  return num.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ',')
}

/**
 * 延迟函数
 * @param ms 延迟时间（毫秒）
 */
export const sleep = (ms: number): Promise<void> => {
  return new Promise(resolve => setTimeout(resolve, ms))
}

export default {
  formatDate,
  formatFileSize,
  debounce,
  throttle,
  deepClone,
  generateId,
  validateEmail,
  validatePhone,
  validatePasswordStrength,
  getFileExtension,
  downloadFile,
  copyToClipboard,
  getUrlParam,
  setUrlParams,
  removeUrlParams,
  generateGradient,
  formatNumber,
  sleep
}