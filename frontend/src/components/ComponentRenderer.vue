<template>
  <div
    class="component-renderer"
    :class="{
      'component-selected': isSelected,
      'component-hover': isHovered,
      'component-editable': editable
    }"
    :style="componentStyles"
    @click.stop="handleSelect"
    @mouseenter="isHovered = true"
    @mouseleave="isHovered = false"
    ref="componentRef"
  >
    <!-- 组件操作工具栏 -->
    <div v-if="editable && (isSelected || isHovered)" class="component-actions">
      <el-tooltip content="上移" placement="top">
        <el-button
          size="mini"
          @click.stop="moveUp"
          :disabled="!canMoveUp"
          circle
        >
          <el-icon><Top /></el-icon>
        </el-button>
      </el-tooltip>
      <el-tooltip content="下移" placement="top">
        <el-button
          size="mini"
          @click.stop="moveDown"
          :disabled="!canMoveDown"
          circle
        >
          <el-icon><Bottom /></el-icon>
        </el-button>
      </el-tooltip>
      <el-tooltip content="删除" placement="top">
        <el-button
          size="mini"
          type="danger"
          @click.stop="deleteComponent"
          circle
        >
          <el-icon><Delete /></el-icon>
        </el-button>
      </el-tooltip>
    </div>

    <!-- 渲染实际组件 -->
    <component
      v-if="componentInstance"
      :is="componentInstance"
      v-bind="processedProps"
      v-on="eventHandlers"
      ref="actualComponentRef"
    >
      <!-- 处理默认插槽 -->
      <template v-if="hasChildren">
        <template v-for="(child, index) in componentConfig.children" :key="child.id">
          <ComponentRenderer
            :component-config="child"
            :parent-config="componentConfig"
            :parent-scope="parentScope"
            :path="[...path, 'children', index.toString()]"
            :index="index"
            :siblings-count="componentConfig.children?.length || 0"
            :editable="editable"
            :selected-component-id="selectedComponentId"
            @component-select="handleChildSelect"
            @component-config-change="handleChildConfigChange"
            @component-move="handleChildMove"
            @component-delete="handleChildDelete"
            @component-children-change="handleChildChildrenChange"
          />
        </template>
      </template>
      
      <!-- 处理具名插槽 -->
      <template
        v-for="(slotContent, slotName) in slots"
        :key="slotName"
        :slot="slotName"
      >
        {{ slotContent }}
      </template>
    </component>

    <!-- 渲染占位内容 -->
    <div v-else class="component-placeholder">
      <div class="placeholder-icon">
        <el-icon v-if="componentConfig.icon"><component :is="componentConfig.icon" /></el-icon>
        <el-icon v-else><HelpFilled /></el-icon>
      </div>
      <div class="placeholder-text">
        {{ componentConfig.displayName || componentConfig.type || '未知组件' }}
      </div>
      <div v-if="!componentInstance" class="placeholder-error">
        组件未找到
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { Top, Bottom, Delete, HelpFilled } from '@element-plus/icons-vue'
import { ElMessageBox } from 'element-plus'
import type { ComponentConfig, ComponentProps } from '@/types/page'
import { getComponentDefinition } from '@/services/componentRegistry'

// Props
interface Props {
  componentConfig: ComponentConfig
  parentConfig?: ComponentConfig
  parentScope?: any
  path?: string[]
  index?: number
  siblingsCount?: number
  editable?: boolean
  selectedComponentId?: string
}

const props = withDefaults(defineProps<Props>(), {
  path: () => [],
  index: -1,
  siblingsCount: 0,
  editable: true,
  selectedComponentId: ''
})

// Emits
interface Emits {
  componentSelect: [component: ComponentConfig, path: string[]]
  componentConfigChange: [component: ComponentConfig]
  componentMove: [fromIndex: number, toIndex: number, parent: ComponentConfig]
  componentDelete: [component: ComponentConfig, parent: ComponentConfig]
  componentChildrenChange: [parent: ComponentConfig, children: ComponentConfig[]]
}

const emit = defineEmits<Emits>()

// Refs
const componentRef = ref<HTMLElement>()
const actualComponentRef = ref<any>()
const isHovered = ref(false)

// Computed
const componentDefinition = computed(() => {
  return getComponentDefinition(props.componentConfig.type)
})

const componentInstance = computed(() => {
  return componentDefinition.value?.component || null
})

const isSelected = computed(() => {
  return props.selectedComponentId === props.componentConfig.id
})

const canMoveUp = computed(() => {
  return props.editable && props.index > 0
})

const canMoveDown = computed(() => {
  return props.editable && props.index < props.siblingsCount - 1
})

const hasChildren = computed(() => {
  return props.componentConfig.children && props.componentConfig.children.length > 0
})

const processedProps = computed(() => {
  const props = { ...props.componentConfig.properties }
  
  // 处理绑定的属性
  Object.keys(props).forEach(key => {
    const value = props[key]
    if (typeof value === 'string' && value.startsWith('{{') && value.endsWith('}}')) {
      try {
        // 简单的表达式解析
        const expression = value.slice(2, -2).trim()
        if (expression.includes('.')) {
          const [scope, prop] = expression.split('.')
          if (props.parentScope && props.parentScope[scope] && props.parentScope[scope][prop] !== undefined) {
            props[key] = props.parentScope[scope][prop]
          }
        }
      } catch (error) {
        console.warn('Failed to process binding expression:', value)
      }
    }
  })
  
  return props
})

const componentStyles = computed(() => {
  const styles: any = {}
  
  // 添加设计时样式
  if (props.editable) {
    styles.position = 'relative'
  }
  
  // 应用组件配置的样式
  if (props.componentConfig.styles) {
    Object.assign(styles, props.componentConfig.styles)
  }
  
  return styles
})

const eventHandlers = computed(() => {
  const handlers: any = {}
  
  // 添加内置事件处理
  if (props.editable) {
    handlers.click = (event: Event) => {
      event.stopPropagation()
      handleSelect()
    }
  }
  
  // 添加组件配置的事件
  if (props.componentConfig.events) {
    props.componentConfig.events.forEach(event => {
      handlers[event.name] = (eventData: any) => {
        // 这里可以根据事件配置执行相应的动作
        console.log(`Event ${event.name} triggered on ${props.componentConfig.type}`, eventData)
        
        // 如果配置了动作，执行动作
        if (event.actions) {
          event.actions.forEach(action => {
            executeAction(action, eventData)
          })
        }
      }
    })
  }
  
  return handlers
})

const slots = computed(() => {
  return props.componentConfig.slots || {}
})

// Methods
const handleSelect = () => {
  if (props.editable) {
    emit('componentSelect', props.componentConfig, props.path)
  }
}

const moveUp = () => {
  if (canMoveUp.value && props.parentConfig) {
    emit('componentMove', props.index, props.index - 1, props.parentConfig)
  }
}

const moveDown = () => {
  if (canMoveDown.value && props.parentConfig) {
    emit('componentMove', props.index, props.index + 1, props.parentConfig)
  }
}

const deleteComponent = async () => {
  if (!props.parentConfig) return
  
  try {
    await ElMessageBox.confirm(
      `确定要删除组件「${props.componentConfig.displayName || props.componentConfig.type}」吗？`,
      '确认删除',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }
    )
    emit('componentDelete', props.componentConfig, props.parentConfig)
  } catch {
    // 用户取消操作
  }
}

const handleChildSelect = (childComponent: ComponentConfig, childPath: string[]) => {
  emit('componentSelect', childComponent, childPath)
}

const handleChildConfigChange = (childComponent: ComponentConfig) => {
  emit('componentConfigChange', props.componentConfig)
}

const handleChildMove = (fromIndex: number, toIndex: number, childParent: ComponentConfig) => {
  emit('componentMove', fromIndex, toIndex, childParent)
}

const handleChildDelete = (childComponent: ComponentConfig, childParent: ComponentConfig) => {
  emit('componentDelete', childComponent, childParent)
}

const handleChildChildrenChange = (childParent: ComponentConfig, children: ComponentConfig[]) => {
  emit('componentChildrenChange', childParent, children)
}

const executeAction = (action: any, eventData: any) => {
  switch (action.type) {
    case 'apiCall':
      // 执行API调用
      console.log('Executing API call:', action.config)
      break
    case 'methodCall':
      // 执行方法调用
      if (props.parentScope && props.parentScope.methods && props.parentScope.methods[action.method]) {
        props.parentScope.methods[action.method](action.params)
      }
      break
    case 'stateUpdate':
      // 更新状态
      if (props.parentScope && props.parentScope.state) {
        Object.assign(props.parentScope.state, action.updates)
      }
      break
    case 'showMessage':
      // 显示消息
      console.log('Show message:', action.message)
      break
    case 'navigate':
      // 页面导航
      console.log('Navigate to:', action.path)
      break
    case 'customScript':
      // 执行自定义脚本
      try {
        // 注意：这里应该使用更安全的方式执行脚本，避免eval的安全风险
        console.log('Executing custom script:', action.script)
      } catch (error) {
        console.error('Failed to execute custom script:', error)
      }
      break
    default:
      console.warn('Unknown action type:', action.type)
  }
}

// Watch for configuration changes
watch(() => props.componentConfig, () => {
  // 配置变化时的处理
}, { deep: true })

// Lifecycle
onMounted(() => {
  // 组件挂载后的初始化
})
</script>

<style scoped>
.component-renderer {
  display: inline-block;
  min-width: 20px;
  min-height: 20px;
}

.component-renderer.component-editable {
  cursor: pointer;
}

.component-renderer.component-hover:not(.component-selected) {
  outline: 1px dashed #409eff;
}

.component-renderer.component-selected {
  outline: 2px solid #409eff;
  box-shadow: 0 0 0 2px rgba(64, 158, 255, 0.2);
}

.component-actions {
  position: absolute;
  top: -32px;
  right: 0;
  display: flex;
  gap: 4px;
  background: #fff;
  padding: 4px;
  border-radius: 4px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
  z-index: 1000;
}

:deep(.el-button--mini) {
  padding: 4px;
}

.component-placeholder {
  min-width: 100px;
  min-height: 60px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  background: #f5f7fa;
  border: 1px dashed #dcdfe6;
  border-radius: 4px;
  color: #909399;
  padding: 16px;
}

.placeholder-icon {
  font-size: 24px;
  margin-bottom: 8px;
  opacity: 0.5;
}

.placeholder-text {
  font-size: 14px;
  font-weight: 500;
  color: #606266;
}

.placeholder-error {
  font-size: 12px;
  color: #f56c6c;
  margin-top: 4px;
}

/* 确保嵌套组件的操作栏位置正确 */
:deep(.component-renderer) {
  position: relative;
}

/* 防止组件内容溢出 */
:deep(.el-button) {
  white-space: nowrap;
}
</style>