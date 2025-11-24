<template>
  <div class="component-previewer">
    <!-- 预览工具栏 -->
    <div class="preview-toolbar">
      <div class="toolbar-left">
        <el-button-group>
          <el-button 
            :type="previewMode === 'desktop' ? 'primary' : 'default'"
            icon="el-icon-monitor"
            @click="previewMode = 'desktop'"
          >
            桌面
          </el-button>
          <el-button 
            :type="previewMode === 'tablet' ? 'primary' : 'default'"
            icon="el-icon-tablet"
            @click="previewMode = 'tablet'"
          >
            平板
          </el-button>
          <el-button 
            :type="previewMode === 'mobile' ? 'primary' : 'default'"
            icon="el-icon-mobile"
            @click="previewMode = 'mobile'"
          >
            手机
          </el-button>
        </el-button-group>
        
        <el-button 
          v-if="showResponsiveControls"
          type="primary"
          icon="el-icon-refresh"
          @click="refreshPreview"
        >
          刷新
        </el-button>
      </div>
      
      <div class="toolbar-right">
        <el-button 
          v-if="showResponsiveControls"
          type="text"
          :icon="showGrid ? 'el-icon-s-grid' : 'el-icon-s-grid-outline'"
          @click="showGrid = !showGrid"
        >
          {{ showGrid ? '隐藏网格' : '显示网格' }}
        </el-button>
        
        <el-select
          v-model="previewTheme"
          placeholder="选择主题"
          size="small"
        >
          <el-option label="默认" value="default" />
          <el-option label="浅色" value="light" />
          <el-option label="深色" value="dark" />
        </el-select>
      </div>
    </div>
    
    <!-- 预览区域 -->
    <div class="preview-container">
      <!-- 响应式预览框架 -->
      <div 
        class="preview-frame"
        :class="{
          'desktop': previewMode === 'desktop',
          'tablet': previewMode === 'tablet',
          'mobile': previewMode === 'mobile',
          'show-grid': showGrid
        }"
      >
        <!-- 加载状态 -->
        <div v-if="loading" class="loading-overlay">
          <el-loading 
            v-loading="loading" 
            text="正在渲染组件..."
          />
        </div>
        
        <!-- 错误提示 -->
        <div v-else-if="error" class="error-container">
          <el-alert
            :title="'渲染错误: ' + error.message"
            type="error"
            description="点击刷新按钮重试"
            show-icon
          />
        </div>
        
        <!-- 空状态 -->
        <div v-else-if="!componentType && !componentConfig" class="empty-container">
          <el-empty description="请选择或配置组件进行预览" />
        </div>
        
        <!-- 组件预览 -->
        <div v-else class="preview-content">
          <!-- 渲染单个组件 -->
          <div v-if="previewSingleComponent" class="single-component-preview">
            <component 
              :is="resolvedComponent" 
              v-bind="resolvedProps" 
              v-on="resolvedEvents"
            >
              <!-- 插槽内容 -->
              <template v-for="(slot, name) in componentSlots" :key="name" #[name]>
                <slot-preview 
                  :slot-config="slot" 
                  :preview-mode="previewMode"
                  :theme="previewTheme"
                />
              </template>
              
              <!-- 默认插槽内容 -->
              <div v-if="componentConfig?.defaultSlotContent" class="default-slot-content">
                {{ componentConfig.defaultSlotContent }}
              </div>
            </component>
          </div>
          
          <!-- 渲染组件树 -->
          <div v-else-if="componentConfig" class="component-tree-preview">
            <dynamic-component-tree
              :components="[componentConfig]"
              :context="previewContext"
              :theme="previewTheme"
              @error="handleComponentError"
            />
          </div>
          
          <!-- 渲染整个页面 -->
          <div v-else-if="pageMetadata" class="page-preview">
            <dynamic-page
              :metadata="pageMetadata"
              :editable="false"
              :context="previewContext"
              @error="handleComponentError"
            />
          </div>
        </div>
      </div>
    </div>
    
    <!-- 性能统计 -->
    <div v-if="showPerformanceStats" class="performance-stats">
      <el-descriptions :column="4" size="small">
        <el-descriptions-item label="渲染时间">
          {{ renderTime }}ms
        </el-descriptions-item>
        <el-descriptions-item label="组件数">
          {{ componentCount }}
        </el-descriptions-item>
        <el-descriptions-item label="渲染次数">
          {{ renderCount }}
        </el-descriptions-item>
        <el-descriptions-item label="内存占用">
          {{ memoryUsage }}MB
        </el-descriptions-item>
      </el-descriptions>
    </div>
    
    <!-- 响应式尺寸指示器 -->
    <div class="dimension-indicator" :class="previewMode">
      {{ getCurrentDimensions() }}px
    </div>
  </div>
</template>

<script lang="ts" setup>
import { ref, reactive, computed, watch, onMounted, nextTick } from 'vue';
import { ElMessage } from 'element-plus';
import type { ComponentConfig, PageMetadata } from '../types/page';

// 组件属性
interface ComponentPreviewerProps {
  componentType?: string;
  componentProps?: Record<string, any>;
  componentConfig?: ComponentConfig;
  pageMetadata?: PageMetadata;
  showResponsiveControls?: boolean;
  showPerformanceStats?: boolean;
  previewContext?: Record<string, any>;
}

const props = withDefaults(defineProps<ComponentPreviewerProps>(), {
  componentType: '',
  componentProps: () => ({}),
  componentConfig: undefined,
  pageMetadata: undefined,
  showResponsiveControls: true,
  showPerformanceStats: false,
  previewContext: () => ({})
});

// 事件
const emit = defineEmits<{
  'preview': [component: any];
  'error': [error: Error];
  'resize': [width: number, height: number];
}>();

// 响应式数据
const previewMode = ref<'desktop' | 'tablet' | 'mobile'>('desktop');
const previewTheme = ref('default');
const showGrid = ref(false);
const loading = ref(false);
const error = ref<Error | null>(null);
const renderTime = ref(0);
const componentCount = ref(0);
const renderCount = ref(0);
const memoryUsage = ref(0);
const lastResizeTime = ref(0);

// 预览上下文
const mergedContext = reactive<Record<string, any>>({
  // 全局API模拟
  api: {
    get: async (url: string, params?: any) => {
      console.log('API GET:', url, params);
      return { success: true, data: [] };
    },
    post: async (url: string, data?: any) => {
      console.log('API POST:', url, data);
      return { success: true, data: {} };
    },
    put: async (url: string, data?: any) => {
      console.log('API PUT:', url, data);
      return { success: true, data: {} };
    },
    delete: async (url: string) => {
      console.log('API DELETE:', url);
      return { success: true };
    }
  },
  
  // 工具函数
  utils: {
    formatDate: (date: Date, format: string) => {
      // 简化的日期格式化函数
      return new Date(date).toLocaleString();
    },
    formatNumber: (num: number, options?: any) => {
      return new Intl.NumberFormat('zh-CN', options).format(num);
    },
    debounce: (func: Function, wait: number) => {
      let timeout: any;
      return function executedFunction(...args: any[]) {
        const later = () => {
          clearTimeout(timeout);
          func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
      };
    },
    throttle: (func: Function, limit: number) => {
      let inThrottle: boolean;
      return function(...args: any[]) {
        if (!inThrottle) {
          func.apply(this, args);
          inThrottle = true;
          setTimeout(() => inThrottle = false, limit);
        }
      };
    }
  },
  
  // 状态管理
  state: {},
  
  // 消息通知
  message: {
    success: (message: string) => ElMessage.success(message),
    warning: (message: string) => ElMessage.warning(message),
    error: (message: string) => ElMessage.error(message),
    info: (message: string) => ElMessage.info(message)
  }
});

// 计算属性

// 是否预览单个组件
const previewSingleComponent = computed(() => {
  return props.componentType && !props.componentConfig && !props.pageMetadata;
});

// 预览上下文
const previewContext = computed(() => {
  return { ...mergedContext, ...props.previewContext };
});

// 解析后的组件
const resolvedComponent = computed(() => {
  if (!props.componentType) return 'div';
  
  // 这里应该从组件注册表中获取组件
  // 简化实现，返回一个模拟的组件
  const componentMap: Record<string, any> = {
    'div': 'div',
    'span': 'span',
    'h1': 'h1',
    'h2': 'h2',
    'h3': 'h3',
    'p': 'p',
    'button': 'button',
    'img': 'img',
    'input': 'input',
    'select': 'select',
    'textarea': 'textarea',
    'form': 'form',
    'table': 'table',
    'tr': 'tr',
    'td': 'td',
    'th': 'th',
    'ul': 'ul',
    'ol': 'ol',
    'li': 'li',
    'a': 'a'
  };
  
  return componentMap[props.componentType] || 'div';
});

// 解析后的属性
const resolvedProps = computed(() => {
  const propsCopy = { ...props.componentProps };
  
  // 处理样式
  if (propsCopy.style && typeof propsCopy.style === 'object') {
    // 转换样式对象为字符串
    const styleString = Object.entries(propsCopy.style)
      .map(([key, value]) => `${key}: ${value}`)
      .join('; ');
    propsCopy.style = styleString;
  }
  
  // 处理计算属性
  if (propsCopy.computedProperties && Array.isArray(propsCopy.computedProperties)) {
    propsCopy.computedProperties.forEach((computedProp: any) => {
      try {
        // 这里简化处理，实际应该用Function构造函数或eval
        // 注意：在生产环境中应避免使用eval
        propsCopy[computedProp.name] = computedProp.expression;
      } catch (err) {
        console.error('Error evaluating computed property:', err);
      }
    });
  }
  
  return propsCopy;
});

// 解析后的事件
const resolvedEvents = computed(() => {
  const events: Record<string, any> = {};
  
  if (props.componentProps?.events && Array.isArray(props.componentProps.events)) {
    props.componentProps.events.forEach((event: any) => {
      events[event.name] = (e: any) => {
        try {
          // 这里简化处理，实际应该用Function构造函数
          console.log(`Event ${event.name} triggered with:`, e);
          
          // 如果有事件体，尝试执行
          if (event.body) {
            console.log('Event body:', event.body);
            // 在实际实现中，这里应该安全地执行事件代码
          }
        } catch (err) {
          console.error(`Error executing event ${event.name}:`, err);
        }
      };
    });
  }
  
  return events;
});

// 组件插槽
const componentSlots = computed(() => {
  return props.componentProps?.slots || {};
});

// 监听属性变化，重新渲染
watch(
  [() => props.componentType, () => props.componentProps, () => props.componentConfig, () => props.pageMetadata],
  () => {
    renderComponent();
  },
  { deep: true, immediate: true }
);

// 监听预览模式变化
watch(
  previewMode,
  () => {
    nextTick(() => {
      emitResizeEvent();
    });
  }
);

// 生命周期
onMounted(() => {
  // 初始化性能监控
  if (props.showPerformanceStats) {
    startPerformanceMonitoring();
  }
  
  // 监听窗口大小变化
  window.addEventListener('resize', handleResize);
});

// 方法定义

// 渲染组件
const renderComponent = async () => {
  loading.value = true;
  error.value = null;
  
  const startTime = performance.now();
  
  try {
    // 模拟异步渲染过程
    await nextTick();
    
    // 更新渲染统计
    const endTime = performance.now();
    renderTime.value = Math.round(endTime - startTime);
    renderCount.value++;
    
    // 估算组件数量
    estimateComponentCount();
    
    // 触发预览事件
    emit('preview', null);
  } catch (err) {
    error.value = err as Error;
    emit('error', err as Error);
    console.error('Component preview error:', err);
  } finally {
    loading.value = false;
  }
};

// 刷新预览
const refreshPreview = () => {
  renderComponent();
};

// 处理组件错误
const handleComponentError = (err: Error) => {
  error.value = err;
  emit('error', err);
};

// 获取当前尺寸
const getCurrentDimensions = (): string => {
  const dimensions = {
    desktop: '1280 × 800',
    tablet: '768 × 1024',
    mobile: '375 × 667'
  };
  
  return dimensions[previewMode.value];
};

// 处理窗口大小变化
const handleResize = () => {
  const now = Date.now();
  
  // 节流处理
  if (now - lastResizeTime.value < 100) return;
  
  lastResizeTime.value = now;
  emitResizeEvent();
};

// 触发尺寸变化事件
const emitResizeEvent = () => {
  const frameElement = document.querySelector('.preview-frame') as HTMLElement;
  if (frameElement) {
    const width = frameElement.offsetWidth;
    const height = frameElement.offsetHeight;
    emit('resize', width, height);
  }
};

// 估算组件数量
const estimateComponentCount = () => {
  // 这里简化实现，实际应该遍历组件树计算
  if (props.pageMetadata) {
    componentCount.value = countComponents(props.pageMetadata.components || []);
  } else if (props.componentConfig) {
    componentCount.value = countComponents([props.componentConfig]);
  } else if (props.componentType) {
    componentCount.value = 1;
  }
};

// 递归计算组件数量
const countComponents = (components: ComponentConfig[]): number => {
  let count = components.length;
  components.forEach(comp => {
    if (comp.children && comp.children.length > 0) {
      count += countComponents(comp.children);
    }
  });
  return count;
};

// 开始性能监控
const startPerformanceMonitoring = () => {
  // 定期更新内存使用情况
  setInterval(() => {
    updateMemoryUsage();
  }, 5000);
};

// 更新内存使用情况
const updateMemoryUsage = () => {
  if (performance && 'memory' in performance) {
    const memoryInfo = (performance as any).memory;
    memoryUsage.value = Math.round(memoryInfo.usedJSHeapSize / 1024 / 1024 * 100) / 100;
  }
};

// 插槽预览组件（简化实现）
const SlotPreview = {
  props: {
    slotConfig: Object,
    previewMode: String,
    theme: String
  },
  setup(props) {
    return () => {
      if (typeof props.slotConfig === 'string') {
        return props.slotConfig;
      }
      if (props.slotConfig.type) {
        // 渲染子组件
        return h(resolvedComponent.value, resolvedProps.value, {
          default: () => props.slotConfig.children || []
        });
      }
      return null;
    };
  }
};

// 动态组件树渲染器（简化实现）
const DynamicComponentTree = {
  props: {
    components: Array,
    context: Object,
    theme: String
  },
  emits: ['error'],
  setup(props, { emit }) {
    const renderComponentTree = (components: ComponentConfig[]) => {
      return components.map((comp, index) => {
        try {
          const componentType = comp.type;
          const componentProps = { ...comp.props, key: index };
          const children = comp.children && comp.children.length > 0 
            ? renderComponentTree(comp.children) 
            : [];
          
          return h(componentType, componentProps, {
            default: () => children
          });
        } catch (err) {
          emit('error', err);
          return h('div', { style: { color: 'red' } }, `Error rendering ${comp.type}`);
        }
      });
    };
    
    return () => renderComponentTree(props.components);
  }
};
</script>

<style scoped>
.component-previewer {
  height: 100%;
  display: flex;
  flex-direction: column;
  background-color: #f5f7fa;
}

/* 预览工具栏 */
.preview-toolbar {
  height: 50px;
  background-color: #fff;
  border-bottom: 1px solid #e4e7ed;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 16px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
}

.toolbar-left,
.toolbar-right {
  display: flex;
  align-items: center;
  gap: 12px;
}

/* 预览容器 */
.preview-container {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: auto;
  padding: 20px;
  position: relative;
}

/* 预览框架 */
.preview-frame {
  background-color: #fff;
  border-radius: 8px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
  overflow: hidden;
  position: relative;
  transition: all 0.3s ease;
  min-height: 400px;
}

/* 响应式预览尺寸 */
.preview-frame.desktop {
  width: 100%;
  max-width: 1280px;
  height: 100%;
  max-height: 800px;
}

.preview-frame.tablet {
  width: 768px;
  height: 1024px;
  border: 10px solid #333;
  border-radius: 20px;
}

.preview-frame.tablet:before {
  content: '';
  position: absolute;
  top: -10px;
  left: 50%;
  transform: translateX(-50%);
  width: 100px;
  height: 15px;
  background-color: #333;
  border-bottom-left-radius: 8px;
  border-bottom-right-radius: 8px;
}

.preview-frame.mobile {
  width: 375px;
  height: 667px;
  border: 12px solid #333;
  border-radius: 30px;
}

.preview-frame.mobile:before {
  content: '';
  position: absolute;
  top: -12px;
  left: 50%;
  transform: translateX(-50%);
  width: 120px;
  height: 20px;
  background-color: #333;
  border-bottom-left-radius: 10px;
  border-bottom-right-radius: 10px;
}

/* 网格背景 */
.preview-frame.show-grid {
  background-image: 
    linear-gradient(to right, rgba(200, 200, 200, 0.1) 1px, transparent 1px),
    linear-gradient(to bottom, rgba(200, 200, 200, 0.1) 1px, transparent 1px);
  background-size: 20px 20px;
}

/* 预览内容 */
.preview-content {
  width: 100%;
  height: 100%;
  overflow: auto;
  position: relative;
}

.single-component-preview,
.component-tree-preview,
.page-preview {
  width: 100%;
  height: 100%;
  padding: 20px;
}

/* 加载覆盖层 */
.loading-overlay {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(255, 255, 255, 0.9);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

/* 错误容器 */
.error-container {
  width: 100%;
  height: 100%;
  padding: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
}

/* 空状态容器 */
.empty-container {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
}

/* 性能统计 */
.performance-stats {
  padding: 10px 16px;
  background-color: #fff;
  border-top: 1px solid #e4e7ed;
  font-size: 12px;
}

/* 尺寸指示器 */
.dimension-indicator {
  position: absolute;
  bottom: 10px;
  right: 10px;
  background-color: rgba(0, 0, 0, 0.7);
  color: #fff;
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 12px;
  z-index: 100;
}

.dimension-indicator.desktop {
  display: none;
}

/* 深色主题样式 */
.preview-frame.dark {
  background-color: #1f2937;
  color: #f3f4f6;
}

/* 浅色主题样式 */
.preview-frame.light {
  background-color: #ffffff;
  color: #1f2937;
}

/* 默认插槽内容样式 */
.default-slot-content {
  padding: 10px;
  background-color: #f3f4f6;
  border: 1px dashed #d1d5db;
  border-radius: 4px;
  color: #6b7280;
  font-style: italic;
}

/* 滚动条样式 */
.preview-content::-webkit-scrollbar,
.preview-container::-webkit-scrollbar {
  width: 8px;
  height: 8px;
}

.preview-content::-webkit-scrollbar-track,
.preview-container::-webkit-scrollbar-track {
  background: #f1f1f1;
  border-radius: 4px;
}

.preview-content::-webkit-scrollbar-thumb,
.preview-container::-webkit-scrollbar-thumb {
  background: #c0c4cc;
  border-radius: 4px;
}

.preview-content::-webkit-scrollbar-thumb:hover,
.preview-container::-webkit-scrollbar-thumb:hover {
  background: #a0a4ac;
}

/* 响应式调整 */
@media (max-width: 768px) {
  .preview-toolbar {
    flex-direction: column;
    height: auto;
    padding: 10px;
    gap: 10px;
  }
  
  .toolbar-left,
  .toolbar-right {
    width: 100%;
    justify-content: center;
  }
  
  .preview-container {
    padding: 10px;
  }
  
  .preview-frame.desktop {
    width: 100%;
    height: 100%;
    max-width: none;
    max-height: none;
  }
  
  .preview-frame.tablet,
  .preview-frame.mobile {
    width: 100%;
    height: 100%;
    border: none;
    border-radius: 0;
  }
  
  .preview-frame.tablet:before,
  .preview-frame.mobile:before {
    display: none;
  }
  
  .dimension-indicator {
    display: none;
  }
}
</style>