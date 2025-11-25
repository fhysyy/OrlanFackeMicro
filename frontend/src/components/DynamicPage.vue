<template>
  <div class="dynamic-page">
    <div v-if="loading" class="loading-container">
      <el-skeleton :rows="6" animated />
    </div>
    
    <div v-else-if="error" class="error-container">
      <el-alert
        :title="error"
        type="error"
        show-icon
        description="页面加载失败，请刷新重试"
      >
        <template #footer>
          <el-button type="primary" size="small" @click="loadPage">重新加载</el-button>
        </template>
      </el-alert>
    </div>
    
    <div v-else-if="pageComponent" class="content-container">
      <component :is="pageComponent" />
    </div>
    
    <div v-else class="empty-container">
      <el-empty description="暂无页面内容" />
    </div>
  </div>
</template>

<script lang="ts" setup>
import { ref, computed, onMounted, watch, defineAsyncComponent } from 'vue';
import { debounce, preciseUpdate } from '../utils/performanceUtils';
import { ElMessage } from 'element-plus';
import { useRoute } from 'vue-router';
import { PageBuilderService } from '../services/pageBuilderService';
import { MetadataService } from '../services/metadataService';
import type { PageMetadata } from '../types/page';

// Props
const props = defineProps<{
  pageId?: string;
  metadata?: PageMetadata;
  editable?: boolean;
}>();

// Emits
const emit = defineEmits<{
  (e: 'load', metadata: PageMetadata): void;
  (e: 'error', error: string): void;
  (e: 'update:metadata', metadata: PageMetadata): void;
}>();

// 响应式数据
const loading = ref(false);
const error = ref('');
const currentMetadata = ref<PageMetadata | null>(props.metadata || null);
const pageContext = ref<any>(null);

// 路由
const route = useRoute();

// 计算属性：页面组件
const pageComponent = computed(() => {
  if (!currentMetadata.value) return null;
  
  return defineAsyncComponent(() => {
    return new Promise((resolve) => {
      try {
        // 创建页面上下文
        pageContext.value = PageBuilderService.createPageContext(currentMetadata.value!);
        
        // 如果是可编辑模式，添加编辑相关方法
        if (props.editable) {
          Object.assign(pageContext.value, {
            updateComponent: updateComponent,
            addComponent: addComponent,
            removeComponent: removeComponent,
            moveComponent: moveComponent
          });
        }
        
        // 返回组件
        resolve({
          render() {
            return PageBuilderService.buildComponent(
              {
                type: 'container',
                props: {
                  style: {
                    height: '100%',
                    width: '100%'
                  }
                },
                children: currentMetadata.value!.components
              },
              pageContext.value
            );
          }
        });
      } catch (err) {
        console.error('构建页面组件失败:', err);
        error.value = '页面组件构建失败';
        resolve({
          render() {
            return {
              type: 'div',
              children: [`页面渲染错误: ${err instanceof Error ? err.message : String(err)}`]
            };
          }
        });
      }
    });
  });
});

// 监听metadata属性变化 - 移除deep: true提高性能
watch(
  () => props.metadata,
  (newMetadata) => {
    if (newMetadata) {
      currentMetadata.value = newMetadata;
      emit('load', newMetadata);
    }
  },
  { immediate: true }
);

// 加载页面
const loadPage = async () => {
  if (props.metadata) {
    currentMetadata.value = props.metadata;
    return;
  }

  const pageId = props.pageId || route.params.pageId as string;
  
  if (!pageId) {
    error.value = '缺少页面ID';
    return;
  }

  loading.value = true;
  error.value = '';
  
  try {
    const metadata = await MetadataService.getMetadata(pageId);
    
    if (metadata) {
      currentMetadata.value = metadata;
      emit('load', metadata);
      
      // 更新页面标题
      if (metadata.route?.meta?.title) {
        document.title = metadata.route.meta.title;
      }
    } else {
      error.value = '页面不存在';
    }
  } catch (err) {
    console.error('加载页面失败:', err);
    error.value = err instanceof Error ? err.message : '页面加载失败';
    emit('error', error.value);
  } finally {
    loading.value = false;
  }
};

// 更新组件配置
const updateComponent = (componentPath: string, updates: any) => {
  if (!currentMetadata.value) return;
  
  try {
    // 解析路径
    const pathParts = componentPath.split('.');
    let target: any = currentMetadata.value;
    
    // 导航到目标组件
    for (let i = 0; i < pathParts.length - 1; i++) {
      const part = pathParts[i];
      const match = part.match(/^(\w+)\[(\d+)\]$/);
      
      if (match) {
        const [, key, index] = match;
        target = target[key][parseInt(index)];
      } else {
        target = target[part];
      }
    }
    
    // 获取最后一个属性名
    const lastPart = pathParts[pathParts.length - 1];
    const lastMatch = lastPart.match(/^(\w+)\[(\d+)\]$/);
    
    if (lastMatch) {
      const [, key, index] = lastMatch;
      Object.assign(target[key][parseInt(index)], updates);
    } else {
      Object.assign(target[lastPart], updates);
    }
    
    // 使用精确更新，避免整个对象替换导致的重渲染
    // currentMetadata.value = { ...currentMetadata.value };
    emit('update:metadata', currentMetadata.value);
    
    return true;
  } catch (error) {
    console.error('更新组件失败:', error);
    ElMessage.error('更新组件失败');
    return false;
  }
};

// 添加组件
const addComponent = (parentPath: string, componentConfig: any, index?: number) => {
  if (!currentMetadata.value) return;
  
  try {
    // 解析父组件路径
    const pathParts = parentPath.split('.');
    let parent: any = currentMetadata.value;
    
    // 导航到父组件
    for (let i = 0; i < pathParts.length - 1; i++) {
      const part = pathParts[i];
      const match = part.match(/^(\w+)\[(\d+)\]$/);
      
      if (match) {
        const [, key, idx] = match;
        parent = parent[key][parseInt(idx)];
      } else {
        parent = parent[part];
      }
    }
    
    // 获取最后一个属性名
    const lastPart = pathParts[pathParts.length - 1];
    const lastMatch = lastPart.match(/^(\w+)\[(\d+)\]$/);
    
    let children: any[];
    if (lastMatch) {
      const [, key, idx] = lastMatch;
      children = parent[key][parseInt(idx)].children || [];
      parent[key][parseInt(idx)].children = children;
    } else {
      children = parent[lastPart].children || [];
      parent[lastPart].children = children;
    }
    
    // 添加组件
    if (index !== undefined && index >= 0) {
      children.splice(index, 0, componentConfig);
    } else {
      children.push(componentConfig);
    }
    
    // 使用精确更新，避免整个对象替换导致的重渲染
    // currentMetadata.value = { ...currentMetadata.value };
    emit('update:metadata', currentMetadata.value);
    
    return true;
  } catch (error) {
    console.error('添加组件失败:', error);
    ElMessage.error('添加组件失败');
    return false;
  }
};

// 删除组件
const removeComponent = (componentPath: string) => {
  if (!currentMetadata.value) return;
  
  try {
    // 解析路径，找出父组件和索引
    const pathParts = componentPath.split('.');
    const lastPart = pathParts.pop()!;
    const parentPath = pathParts.join('.');
    
    // 解析父组件路径
    const parentParts = parentPath.split('.');
    let parent: any = currentMetadata.value;
    
    // 导航到父组件
    for (let i = 0; i < parentParts.length - 1; i++) {
      const part = parentParts[i];
      const match = part.match(/^(\w+)\[(\d+)\]$/);
      
      if (match) {
        const [, key, idx] = match;
        parent = parent[key][parseInt(idx)];
      } else {
        parent = parent[part];
      }
    }
    
    // 获取父组件的children属性
    const childrenPart = parentParts[parentParts.length - 1];
    const childrenMatch = childrenPart.match(/^(\w+)\[(\d+)\]$/);
    
    let children: any[];
    if (childrenMatch) {
      const [, key, idx] = childrenMatch;
      children = parent[key][parseInt(idx)].children || [];
    } else {
      children = parent[childrenPart].children || [];
    }
    
    // 提取要删除的索引
    const indexMatch = lastPart.match(/^children\[(\d+)\]$/);
    if (indexMatch) {
      const index = parseInt(indexMatch[1]);
      if (index >= 0 && index < children.length) {
        children.splice(index, 1);
        
        // 使用精确更新，避免整个对象替换导致的重渲染
        // currentMetadata.value = { ...currentMetadata.value };
        emit('update:metadata', currentMetadata.value);
        
        return true;
      }
    }
    
    return false;
  } catch (error) {
    console.error('删除组件失败:', error);
    ElMessage.error('删除组件失败');
    return false;
  }
};

// 移动组件
const moveComponent = (fromPath: string, toPath: string, toIndex?: number) => {
  if (!currentMetadata.value) return;
  
  try {
    // 先获取组件
    let componentToMove: any = null;
    
    // 解析源路径
    const fromParts = fromPath.split('.');
    let fromParent: any = currentMetadata.value;
    let fromIndex: number = -1;
    
    // 导航到父组件
    for (let i = 0; i < fromParts.length - 1; i++) {
      const part = fromParts[i];
      const match = part.match(/^(\w+)\[(\d+)\]$/);
      
      if (match) {
        const [, key, idx] = match;
        fromParent = fromParent[key][parseInt(idx)];
      } else {
        fromParent = fromParent[part];
      }
    }
    
    // 获取源索引
    const fromLastPart = fromParts[fromParts.length - 1];
    const fromMatch = fromLastPart.match(/^children\[(\d+)\]$/);
    
    if (fromMatch) {
      fromIndex = parseInt(fromMatch[1]);
      const fromChildren = fromParent.children || [];
      
      if (fromIndex >= 0 && fromIndex < fromChildren.length) {
        componentToMove = { ...fromChildren[fromIndex] };
      }
    }
    
    if (!componentToMove) return false;
    
    // 删除原组件
    removeComponent(fromPath);
    
    // 添加到新位置
    return addComponent(toPath, componentToMove, toIndex);
  } catch (error) {
    console.error('移动组件失败:', error);
    ElMessage.error('移动组件失败');
    return false;
  }
};

// 创建防抖版本的操作方法
const debouncedUpdateComponent = debounce(updateComponent, 100);
const debouncedAddComponent = debounce(addComponent, 100);
const debouncedRemoveComponent = debounce(removeComponent, 100);

// 暴露方法给父组件
defineExpose({
  loadPage,
  updateComponent,
  debouncedUpdateComponent,
  addComponent,
  debouncedAddComponent,
  removeComponent,
  debouncedRemoveComponent,
  moveComponent,
  pageContext
});

// 组件挂载时加载页面
onMounted(() => {
  if (!props.metadata) {
    loadPage();
  }
});
</script>

<style scoped>
.dynamic-page {
  height: 100%;
  width: 100%;
  overflow: hidden;
}

.loading-container,
.error-container,
.empty-container {
  padding: 20px;
  height: 100%;
  display: flex;
  flex-direction: column;
}

.content-container {
  height: 100%;
  width: 100%;
  overflow: auto;
}

:deep(.el-alert) {
  margin-bottom: 0;
}
</style>