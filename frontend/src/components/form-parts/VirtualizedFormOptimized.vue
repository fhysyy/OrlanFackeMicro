<template>
  <div class="virtualized-form-optimized">
    <el-form
      ref="formRef"
      :model="formData"
      :rules="formRules"
      :label-position="formConfig.labelPosition || 'right'"
      :label-width="formConfig.labelWidth || '120px'"
      :inline="formConfig.layout === 'inline'"
      class="dynamic-form"
    >
      <!-- 虚拟滚动容器 -->
      <div
        ref="scrollContainer"
        class="scroll-container"
        @scroll="handleScroll"
        @wheel="handleWheel"
      >
        <!-- 滚动位置指示器 -->
        <div class="scroll-indicator">
          <div class="scroll-bar" :style="{ width: `${scrollPercentage}%` }"></div>
        </div>
        
        <!-- 可视区域高度 -->
        <div
          class="scroll-content"
          :style="{ height: `${totalHeight}px` }"
        >
          <!-- 渲染可见区域内的表单项 -->
          <div
            v-for="item in visibleItems"
            :key="item.field.prop"
            ref="itemElements"
            class="form-item-container"
            :style="{ 
              transform: `translateY(${item.top}px)`,
              height: `${item.height}px`
            }"
            :data-index="item.index"
          >
            <!-- 使用IntersectionObserver进行懒加载 -->
            <div
              ref="itemContent"
              class="item-content-wrapper"
              :data-lazy="true"
            >
              <!-- 动态加载状态 -->
              <div
                v-if="item.loading"
                class="item-loading"
              >
                <el-skeleton
                  animated
                  :rows="1"
                />
              </div>
              
              <!-- 实际表单项内容 -->
              <FormFormItemRenderer
                v-else
                :field="item.field"
                :model-value="formData"
                :label-width="formConfig.labelWidth || '120px'"
                :custom-components="customComponents"
                @change="handleFieldChange"
              >
                <template
                  v-for="(_, name) in $slots"
                  #[name]="slotData"
                >
                  <slot
                    :name="name"
                    v-bind="slotData"
                  />
                </template>
              </FormFormItemRenderer>
            </div>
          </div>
        </div>
      </div>

      <!-- 表单操作按钮 -->
      <FormActionsRenderer
        :submitting="submitting"
        :submit-button-text="t('form.submit')"
        :reset-button-text="t('form.reset')"
        :show-reset-button="formConfig.showResetButton"
        @submit="handleSubmit"
        @reset="handleReset"
      >
        <template #actions>
          <slot name="actions" />
        </template>
      </FormActionsRenderer>
    </el-form>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, onUnmounted, nextTick, watch } from 'vue'
import { ElMessage } from 'element-plus'
import type { FormInstance } from 'element-plus'
import type { FormConfig, FormField } from '@/types/form'
import { t } from './FormI18n'
import FormFormItemRenderer from './FormFormItemRenderer.vue'
import FormActionsRenderer from './FormActionsRenderer.vue'
import { FormValidationManager } from './FormValidationManager'
import { FormDataManager } from './FormDataManager'
import { throttle } from '@/utils/performanceUtils'

// Props
const props = defineProps<{
  config: FormConfig
  modelValue?: Record<string, any>
  customComponents?: Record<string, any>
  itemHeight?: number // 单个表单项高度
  bufferSize?: number // 缓冲区大小，渲染额外项数
  estimatedItemHeight?: number // 预估表单项高度，用于高度自适应
  enableIntersectionObserver?: boolean // 是否启用Intersection Observer
  scrollDebounceTime?: number // 滚动防抖时间
}>()

// Emits
const emit = defineEmits<{
  (e: 'update:modelValue', value: Record<string, any>): void
  (e: 'submit', value: Record<string, any>): void
  (e: 'reset'): void
  (e: 'fieldChange', field: string, value: any): void
}>()

// 配置项
const ITEM_HEIGHT = props.itemHeight || 80 // 单个表单项高度
const BUFFER_SIZE = props.bufferSize || 5 // 缓冲区大小
const ESTIMATED_ITEM_HEIGHT = props.estimatedItemHeight || 80
const ENABLE_INTERSECTION_OBSERVER = props.enableIntersectionObserver !== false
const SCROLL_DEBOUNCE_TIME = props.scrollDebounceTime || 16

// 表单引用
const formRef = ref<FormInstance>()
const scrollContainer = ref<HTMLElement>()
const itemElements = ref<HTMLElement[]>([])
const itemContent = ref<HTMLElement[]>([])

// 提交状态
const submitting = ref(false)

// 表单数据
const formData = reactive<Record<string, any>>({})

// 可见字段
const visibleFields = computed(() => {
  return formConfig.value.fields?.filter(field => !field.hidden) || []
})

// 表单配置
const formConfig = computed(() => ({
  fields: [],
  labelPosition: 'right',
  labelWidth: '120px',
  layout: '',
  showSubmitButton: false,
  showResetButton: false,
  ...props.config
}))

// 表单验证规则
const formRules = computed(() => {
  return FormValidationManager.generateFormRules(formConfig.value.fields)
})

// 虚拟滚动相关
const scrollTop = ref(0)
const containerHeight = ref(0)
const itemHeights = ref<number[]>([]) // 每个项目的实际高度
const itemPositions = ref<number[]>([]) // 每个项目的累计高度位置
const itemLoading = ref<Record<string, boolean>>({}) // 项目加载状态

// Intersection Observer
let intersectionObserver: IntersectionObserver | null = null

// 初始化Intersection Observer
const initIntersectionObserver = () => {
  if (!ENABLE_INTERSECTION_OBSERVER || typeof IntersectionObserver === 'undefined') {
    return
  }

  intersectionObserver = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
      const index = parseInt(entry.target.getAttribute('data-index') || '0', 10)
      if (entry.isIntersecting) {
        // 项目进入可视区域，设置为已加载
        itemLoading.value[index] = false
      }
    })
  }, {
    root: scrollContainer.value,
    rootMargin: '50px' // 提前50px开始加载
  })
}

// 总高度（基于实际高度）
const totalHeight = computed(() => {
  if (itemPositions.value.length === 0) {
    return visibleFields.value.length * ITEM_HEIGHT
  }
  return itemPositions.value[itemPositions.value.length - 1] + 
    (itemHeights.value[itemHeights.value.length - 1] || ITEM_HEIGHT)
})

// 滚动百分比
const scrollPercentage = computed(() => {
  if (totalHeight.value <= containerHeight.value) return 100
  return (scrollTop.value / (totalHeight.value - containerHeight.value)) * 100
})

// 可见区域
const visibleRange = computed(() => {
  // 使用二分查找找到第一个可见项
  let start = 0
  let end = visibleFields.value.length
  
  while (start < end) {
    const mid = Math.floor((start + end) / 2)
    if ((itemPositions.value[mid] || mid * ITEM_HEIGHT) < scrollTop.value + containerHeight.value) {
      start = mid + 1
    } else {
      end = mid
    }
  }
  
  // 找到最后一个可见项
  let visibleStart = Math.max(0, start - BUFFER_SIZE * 2)
  let visibleEnd = start + BUFFER_SIZE
  
  // 确保不越界
  visibleEnd = Math.min(visibleFields.value.length, visibleEnd)
  visibleStart = Math.max(0, visibleStart)
  
  return { start: visibleStart, end: visibleEnd }
})

// 可见项
const visibleItems = computed(() => {
  const { start, end } = visibleRange.value
  
  return visibleFields.value.slice(start, end).map((field, index) => {
    const actualIndex = start + index
    const top = itemPositions.value[actualIndex] || actualIndex * ITEM_HEIGHT
    const height = itemHeights.value[actualIndex] || ITEM_HEIGHT
    
    return {
      field,
      index: actualIndex,
      top,
      height,
      loading: itemLoading.value[actualIndex] !== false // 默认为加载状态
    }
  })
})

// 更新项目高度
const updateItemHeight = (index: number, height: number) => {
  if (itemHeights.value[index] === height) return
  
  // 更新高度
  itemHeights.value[index] = height
  
  // 更新位置
  itemPositions.value[index] = (index > 0 ? itemPositions.value[index - 1] : 0) + height
  
  // 更新后续项目的位置
  for (let i = index + 1; i < visibleFields.value.length; i++) {
    itemPositions.value[i] = itemPositions.value[i - 1] + (itemHeights.value[i] || ITEM_HEIGHT)
  }
}

// 防抖处理的滚动事件
const handleScroll = throttle((event: Event) => {
  if (!scrollContainer.value) return
  scrollTop.value = scrollContainer.value.scrollTop
}, SCROLL_DEBOUNCE_TIME)

// 处理鼠标滚轮事件
const handleWheel = (event: WheelEvent) => {
  // 如果是水平滚动，阻止默认行为
  if (event.deltaX !== 0) {
    event.preventDefault()
  }
}

// 处理字段值变化
const handleFieldChange = (field: string, value: any) => {
  // 获取当前字段对象
  const currentField = formConfig.value.fields?.find(f => f.prop === field)
  if (currentField) {
    // 使用安全克隆的字段对象
    const safeField = FormDataManager.safeCloneField(currentField)
    emit('fieldChange', safeField, value)
  } else {
    // 如果找不到字段，仍然发出事件（保持兼容性）
    emit('fieldChange', field, value)
  }
  
  // 更新表单数据
  Object.assign(formData, FormDataManager.updateFormData(formData, field, value))
  
  // 检查formData是否存在循环引用
  if (FormDataManager.hasCircularReference(formData)) {
    console.warn('检测到循环引用，尝试修复...')
    const safeFormData = FormDataManager.safeClone(formData)
    emit('update:modelValue', safeFormData)
  } else {
    emit('update:modelValue', { ...formData })
  }
  
  // 如果字段有验证规则，触发验证
  if (currentField && currentField.validationRules?.length) {
    FormValidationManager.validateField(formRef.value!, field)
  }
}

// 提交表单
const handleSubmit = async () => {
  if (!formRef.value) return

  try {
    const isValid = await FormValidationManager.validateForm(formRef.value)
    if (!isValid) return

    submitting.value = true

    // 调用自定义提交函数
    if (formConfig.value.onSubmit) {
      await formConfig.value.onSubmit(formData)
    }

    // 触发提交事件
    emit('submit', { ...formData })
  } catch (error) {
    console.error('表单验证失败:', error)
  } finally {
    submitting.value = false
  }
}

// 重置表单
const handleReset = () => {
  if (formRef.value) {
    formRef.value.resetFields()
  }
  
  // 重新初始化表单数据
  Object.assign(
    formData,
    FormDataManager.initFormData(
      formConfig.value.fields,
      undefined,
      formConfig.value.initialData
    )
  )
  
  // 调用自定义重置函数
  if (formConfig.value.onReset) {
    formConfig.value.onReset()
  }
  
  // 触发重置事件
  emit('reset')
}

// 设置表单数据
const setFormData = (data: Record<string, any>) => {
  Object.assign(formData, data)
  emit('update:modelValue', { ...formData })
}

// 初始化表单数据
const initFormData = () => {
  Object.assign(
    formData,
    FormDataManager.initFormData(
      formConfig.value.fields,
      props.modelValue,
      formConfig.value.initialData
    )
  )
}

// 更新容器高度
const updateContainerHeight = () => {
  if (scrollContainer.value) {
    containerHeight.value = scrollContainer.value.clientHeight
  }
}

// 初始化虚拟滚动
const initVirtualScroll = () => {
  // 初始化高度数组
  itemHeights.value = new Array(visibleFields.value.length).fill(ESTIMATED_ITEM_HEIGHT)
  itemPositions.value = visibleFields.value.map((_, index) => 
    index * ESTIMATED_ITEM_HEIGHT
  )
  
  // 初始化加载状态
  const loadingState: Record<string, boolean> = {}
  visibleFields.value.forEach((_, index) => {
    loadingState[index] = true
  })
  itemLoading.value = loadingState
  
  // 初始化Intersection Observer
  initIntersectionObserver()
}

// 监听itemContent变化，设置Intersection Observer
watch(
  () => itemContent.value,
  (newElements) => {
    if (!intersectionObserver || !newElements.length) return
    
    // 清除旧的观察
    intersectionObserver.disconnect()
    
    // 设置新的观察
    newElements.forEach(element => {
      if (element) {
        intersectionObserver?.observe(element)
      }
    })
  }
)

// 组件挂载时初始化
onMounted(async () => {
  initFormData()
  initVirtualScroll()
  
  // 等待DOM更新后设置容器高度
  await nextTick()
  updateContainerHeight()
  
  // 监听窗口大小变化
  window.addEventListener('resize', updateContainerHeight)
})

// 组件卸载时清理
onUnmounted(() => {
  window.removeEventListener('resize', updateContainerHeight)
  
  // 清理Intersection Observer
  if (intersectionObserver) {
    intersectionObserver.disconnect()
    intersectionObserver = null
  }
})

// 暴露方法给父组件
defineExpose({
  formRef,
  formData,
  handleSubmit,
  handleReset,
  setFormData,
  validate: () => FormValidationManager.validateForm(formRef.value!),
  updateItemHeight,
  scrollToField: (fieldProp: string) => {
    const index = visibleFields.value.findIndex(field => field.prop === fieldProp)
    if (index === -1 || !scrollContainer.value) return
    
    const top = itemPositions.value[index] || index * ITEM_HEIGHT
    scrollContainer.value.scrollTo({ top, behavior: 'smooth' })
  }
})
</script>

<style scoped>
.virtualized-form-optimized {
  width: 100%;
  height: 100%;
}

.dynamic-form {
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
}

.scroll-container {
  flex: 1;
  overflow-y: auto;
  overflow-x: hidden;
  position: relative;
}

.scroll-indicator {
  position: absolute;
  top: 0;
  right: 0;
  width: 4px;
  height: 100%;
  background-color: #f0f0f0;
  z-index: 10;
}

.scroll-bar {
  height: 100%;
  background-color: #c0c0c0;
  transition: width 0.2s;
}

.scroll-content {
  position: relative;
  width: 100%;
}

.form-item-container {
  position: absolute;
  width: 100%;
  box-sizing: border-box;
  padding: 10px 0;
}

.item-content-wrapper {
  width: 100%;
  height: 100%;
}

.item-loading {
  padding: 10px;
  width: 100%;
}
</style>