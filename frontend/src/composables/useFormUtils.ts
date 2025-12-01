import { ref, computed, onMounted, onUnmounted } from 'vue'
import { FormUtils } from '@/utils/formUtils'

/**
 * 表单工具组合式API
 */
export function useFormUtils() {
  // 加载状态
  const loading = ref(false)
  const error = ref<string | null>(null)

  /**
   * 安全深拷贝
   */
  const safeClone = <T>(obj: T): T => {
    try {
      return FormUtils.safeDeepClone(obj)
    } catch (err) {
      error.value = `深拷贝失败: ${err instanceof Error ? err.message : '未知错误'}`
      console.error(error.value, err)
      // 降级处理
      return JSON.parse(JSON.stringify(obj))
    }
  }

  /**
   * 检测循环引用
   */
  const checkCircularReference = (obj: any): boolean => {
    try {
      return FormUtils.hasCircularReference(obj)
    } catch (err) {
      error.value = `循环引用检测失败: ${err instanceof Error ? err.message : '未知错误'}`
      console.error(error.value, err)
      return true // 安全起见，假设存在循环引用
    }
  }

  /**
   * 安全JSON序列化
   */
  const safeStringify = (obj: any, space?: number): string => {
    try {
      return FormUtils.safeJsonStringify(obj, space)
    } catch (err) {
      error.value = `序列化失败: ${err instanceof Error ? err.message : '未知错误'}`
      console.error(error.value, err)
      // 降级处理
      try {
        return JSON.stringify(obj, null, space)
      } catch (fallbackErr) {
        console.error('降级序列化也失败:', fallbackErr)
        return '{"error": "无法序列化对象"}'
      }
    }
  }

  /**
   * 规范化表单配置
   */
  const normalizeConfig = (config: any) => {
    try {
      return FormUtils.normalizeFormConfig(config)
    } catch (err) {
      error.value = `规范化失败: ${err instanceof Error ? err.message : '未知错误'}`
      console.error(error.value, err)
      return config // 返回原始配置
    }
  }

  /**
   * 验证表单配置
   */
  const validateConfig = (config: any) => {
    try {
      return FormUtils.validateFormConfig(config)
    } catch (err) {
      error.value = `验证失败: ${err instanceof Error ? err.message : '未知错误'}`
      console.error(error.value, err)
      return { valid: false, errors: ['验证过程中发生错误'] }
    }
  }

  /**
   * 清除错误状态
   */
  const clearError = () => {
    error.value = null
  }

  return {
    loading: computed(() => loading.value),
    error: computed(() => error.value),
    safeClone,
    checkCircularReference,
    safeStringify,
    normalizeConfig,
    validateConfig,
    clearError
  }
}