// 生产环境安全的日志工具
const isDevelopment = import.meta.env.DEV

export const logger = {
  debug: (...args: any[]) => {
    if (isDevelopment) {
      console.debug('[DEBUG]', ...args)
    }
  },
  
  log: (...args: any[]) => {
    if (isDevelopment) {
      console.log('[LOG]', ...args)
    }
  },
  
  info: (...args: any[]) => {
    if (isDevelopment) {
      console.info('[INFO]', ...args)
    }
  },
  
  warn: (...args: any[]) => {
    if (isDevelopment) {
      console.warn('[WARN]', ...args)
    } else {
      // 生产环境中仍然输出警告，但可以通过远程日志服务收集
      console.warn('[WARN]', ...args)
    }
  },
  
  error: (...args: any[]) => {
    // 错误信息在所有环境中都输出
    console.error('[ERROR]', ...args)
  },
  
  group: (label: string) => {
    if (isDevelopment) {
      console.group(label)
    }
  },
  
  groupEnd: () => {
    if (isDevelopment) {
      console.groupEnd()
    }
  },
  
  time: (label: string) => {
    if (isDevelopment) {
      console.time(label)
    }
  },
  
  timeEnd: (label: string) => {
    if (isDevelopment) {
      console.timeEnd(label)
    }
  }
}