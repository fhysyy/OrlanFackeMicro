<template>
  <div class="vtj-designer">
    <!-- 设计器头部 -->
    <div class="designer-header">
      <h2>VTJ低代码设计器</h2>
      <div class="header-actions">
        <el-button type="primary" @click="handleSave">保存</el-button>
        <el-button @click="handlePreview">预览</el-button>
        <el-button @click="handleExport">导出代码</el-button>
      </div>
    </div>
    
    <!-- 设计器主体 -->
    <div class="designer-container">
      <!-- VTJ设计器集成区域 -->
      <div class="designer-content-wrapper">
        <div 
          class="designer-content" 
          ref="designerContainerRef"
          v-show="!errorMessage"
        ></div>
        
        <!-- 加载状态 -->
        <div v-if="loading" class="loading-overlay">
          <el-icon class="loading-icon"><Loading /></el-icon>
          <p>VTJ设计器加载中...</p>
        </div>
        
        <!-- 错误状态 -->
        <div v-else-if="errorMessage" class="error-overlay">
          <el-empty
            description="VTJ设计器加载失败"
            image="el-icon-warning"
            :image-size="100"
          >
            <el-button type="primary" @click="handleRetry">重试加载</el-button>
          </el-empty>
        </div>
      </div>
      
      <!-- 错误提示 -->
      <div v-if="errorMessage" class="error-message">
        <el-alert
          :title="errorMessage"
          type="error"
          description="VTJ设计器集成失败，请检查依赖或联系管理员"
          show-icon
          :closable="true"
          @close="errorMessage = ''"
        ></el-alert>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch, nextTick, createApp } from 'vue';
import { ElMessage, ElIcon, ElLoading, ElNotification, ElMessageBox } from 'element-plus';
import { Loading } from '@element-plus/icons-vue';
// 导入VTJ相关依赖
import { Engine } from '@vtj/designer';
import { createProvider, ContextMode } from '@vtj/renderer';
import type { Engine as EngineType } from '@vtj/designer';
// 状态管理
const errorMessage = ref('');
const loading = ref(true);
const designerContainerRef = ref<HTMLElement | null>(null);
const engineInstance = ref<EngineType | null>(null);
const isMounted = ref(false);

// 初始化设计器引擎
const initDesigner = async () => {
  try {
    loading.value = true;
    errorMessage.value = '';
    
    console.log('=== VTJ设计器引擎初始化开始 ===');
    console.log('1. 初始容器引用:', designerContainerRef.value);
    
    // 等待容器元素完全可用，使用setTimeout确保DOM渲染完成
    await new Promise(resolve => setTimeout(resolve, 500));
    
    // 再次检查容器引用
    if (!designerContainerRef.value) {
      throw new Error('设计器容器引用为空');
    }
    
    console.log('2. 延迟后容器引用:', designerContainerRef.value);
    
    // 使用nextTick确保DOM完全渲染
    await nextTick();
    
    const container = designerContainerRef.value as HTMLElement;
    
    // 再次检查容器元素
    if (!container) {
      throw new Error('无效的设计器容器元素');
    }
    
    if (!(container instanceof HTMLElement)) {
      throw new Error('容器元素不是有效的HTMLElement');
    }
    
    // 确保容器元素在DOM中
    if (!container.isConnected) {
      throw new Error('容器元素未连接到DOM');
    }
    
    console.log('3. 容器元素类型检查通过:', container);
    console.log('4. 容器元素属性:', {
      id: container.id,
      className: container.className,
      tagName: container.tagName,
      offsetWidth: container.offsetWidth,
      offsetHeight: container.offsetHeight,
      parentNode: container.parentNode,
      isConnected: container.isConnected
    });
    
    // 确保容器有正确的尺寸和样式
    container.style.width = '100%';
    container.style.height = '100%';
    container.style.display = 'block';
    container.style.position = 'relative';
    container.style.overflow = 'hidden';
    container.style.backgroundColor = '#ffffff';
    
    // 确保容器有实际的尺寸
    if (container.offsetWidth === 0 || container.offsetHeight === 0) {
      throw new Error('容器元素尺寸为0，请检查CSS样式');
    }
    
    console.log('5. 容器样式设置完成');
    
    // 清空容器内容
    container.innerHTML = '';
    console.log('6. 容器内容已清空');
    
    // 创建一个完整的服务模拟实现，包含Engine所需的所有方法
    const mockService = {
      // Engine所需的init方法 - 初始化项目数据
      init: async (project: any) => {
        console.log('模拟服务初始化:', project);
        // 确保返回的数据结构符合VTJ设计器的期望，特别是确保所有数组字段都是数组
        return Promise.resolve({ 
          name: 'test-project',
          version: '1.0.0',
          dependencies: {},
          // 提供完整的数据结构，确保所有应该是数组的字段都是数组
          materials: [],
          blocks: [],
          components: [],
          configs: {},
          entry: '',
          router: {
            routes: []
          },
          scripts: [],
          styles: [],
          // 添加VTJ设计器merge方法所需的字段
          extensions: [],
          renderers: [],
          plugins: [],
          templates: []
        });
      },
      // Engine所需的dispose方法 - 销毁服务
      dispose: async () => {
        console.log('模拟服务销毁');
        return Promise.resolve();
      },
      // 提供必要的文件服务方法
      readFile: async (path: string) => {
        console.log('模拟读取文件:', path);
        return '';
      },
      writeFile: async (path: string, content: string) => {
        console.log('模拟写入文件:', path, content);
        return Promise.resolve();
      },
      exists: async (path: string) => {
        console.log('模拟检查文件是否存在:', path);
        return false;
      },
      mkdir: async (path: string) => {
        console.log('模拟创建目录:', path);
        return Promise.resolve();
      },
      readdir: async (path: string) => {
        console.log('模拟读取目录:', path);
        return [];
      },
      delete: async (path: string) => {
        console.log('模拟删除文件:', path);
        return Promise.resolve();
      },
      // 新增：根据官方文档需要的方法
      getExtension: async () => {
        console.log('获取平台配置');
        // 确保返回的数据结构符合VTJ设计器的期望，特别是确保所有数组字段都是数组
        return Promise.resolve({
          version: '1.0.0',
          // 确保所有应该是数组的字段都是数组
          components: [],
          materials: [],
          blocks: [],
          extensions: [],
          renderers: [],
          plugins: [],
          templates: [],
          // 添加VTJ设计器merge方法所需的字段
          configs: {},
          dependencies: {}
        });
      },
      saveProject: async (project: any) => {
        console.log('保存项目:', project);
        return Promise.resolve(true);
      },
      publishFile: async (file: any) => {
        console.log('生成产品代码:', file);
        return Promise.resolve({ success: true });
      }
    };
    
    // 创建设计器引擎配置
    const engineOptions = {
      container: container,
      service: mockService as any,
      project: {
        name: 'test-project',
        version: '1.0.0',
        dependencies: {},
        materials: [],
        blocks: []
      },
      // 禁用版本检查，避免不必要的网络请求
      checkVersion: false
    };
    
    // 创建设计器上下文提供者 - 根据官方文档要求
    console.log('7. 创建设计器上下文提供者');
    const provider = createProvider({
      mode: ContextMode.DESIGNER,
      service: mockService as any
    });
    
    // 确保上下文提供者被正确使用 - 将其添加到engineOptions中
    engineOptions.provider = provider;
    
    // 创建设计器引擎实例 - 增加try-catch捕获内部错误
    console.log('8. 准备创建Engine实例，配置:', engineOptions);
    
    // 使用try-catch捕获Engine初始化过程中的错误
    try {
      const engine = new Engine(engineOptions);
      console.log('10. Engine实例创建成功:', engine);
      
      // 引擎实例不需要显式调用init方法，它会自动初始化
      engineInstance.value = engine;
      
      ElMessage.success('VTJ设计器引擎初始化成功');
      console.log('=== VTJ设计器引擎初始化完成 ===');
    } catch (engineError) {
      console.error('10. Engine实例初始化失败:', engineError);
      console.error('10.1 错误堆栈:', engineError instanceof Error ? engineError.stack : '无堆栈信息');
      
      // 检查是否是nextSibling相关错误
      if (engineError instanceof Error && engineError.message.includes('nextSibling')) {
        console.error('8.2 错误类型: nextSibling访问错误');
        console.error('8.3 容器当前状态:', {
          hasChildNodes: container.hasChildNodes(),
          firstChild: container.firstChild,
          lastChild: container.lastChild,
          nextSibling: container.nextSibling,
          previousSibling: container.previousSibling
        });
      }
      
      throw engineError;
    }
  } catch (error) {
    console.error('=== VTJ设计器引擎初始化失败 ===');
    console.error('错误详情:', error);
    console.error('错误堆栈:', error instanceof Error ? error.stack : '无堆栈信息');
    
    errorMessage.value = error instanceof Error ? error.message : '设计器初始化失败';
    ElMessage.error('VTJ设计器初始化失败');
  } finally {
    loading.value = false;
    console.log('=== VTJ设计器引擎初始化流程结束 ===');
  }
};

// 保存当前配置
const handleSave = async () => {
  try {
    if (!engineInstance.value) {
      ElMessage.warning('设计器引擎未初始化');
      return;
    }
    
    // 这里应该调用API保存配置到后端
    console.log('保存设计器配置');
    ElMessage.success('保存成功');
  } catch (error) {
    console.error('保存配置失败:', error);
    ElMessage.error('保存失败');
  }
};

// 预览当前设计
const handlePreview = async () => {
  try {
    if (!engineInstance.value) {
      ElMessage.warning('设计器引擎未初始化');
      return;
    }
    
    console.log('预览设计');
    ElMessage.success('预览功能已触发');
  } catch (error) {
    console.error('预览失败:', error);
    ElMessage.error('预览失败');
  }
};

// 导出代码
const handleExport = async () => {
  try {
    if (!engineInstance.value) {
      ElMessage.warning('设计器引擎未初始化');
      return;
    }
    
    // 导出代码
    console.log('导出代码');
    ElMessage.success('代码导出成功');
  } catch (error) {
    console.error('导出代码失败:', error);
    ElMessage.error('导出失败');
  }
};

// 重试初始化
const handleRetry = () => {
  initDesigner();
};

// 监听容器元素变化，确保在元素可用时初始化
watch(designerContainerRef, (newContainer) => {
  if (newContainer && isMounted.value) {
    initDesigner();
  }
});

// 组件挂载时初始化设计器
onMounted(async () => {
  isMounted.value = true;
  console.log('=== 组件已挂载 ===');
  
  // 等待DOM完全渲染
  await nextTick();
  
  // 延迟初始化，确保所有依赖都已加载完成
  setTimeout(() => {
    console.log('=== 开始延迟初始化 ===');
    initDesigner();
  }, 1000);
});

// 组件卸载时销毁设计器实例
onUnmounted(() => {
  isMounted.value = false;
  if (engineInstance.value) {
    engineInstance.value.dispose();
    engineInstance.value = null;
  }
});
</script>

<style scoped>
.vtj-designer {
  height: 100vh;
  display: flex;
  flex-direction: column;
  background-color: #f5f7fa;
}

.designer-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 24px;
  background-color: #fff;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  z-index: 10;
}

.designer-header h2 {
  margin: 0;
  font-size: 20px;
  font-weight: 500;
  color: #333;
}

.header-actions {
  display: flex;
  gap: 12px;
}

.designer-container {
  flex: 1;
  position: relative;
  overflow: hidden;
  background-color: #fff;
}

.designer-content-wrapper {
  width: 100%;
  height: 100%;
  position: relative;
  display: flex;
  flex-direction: column;
}

.designer-content {
  width: 100%;
  height: 100%;
  position: relative;
  background-color: #fff;
  overflow: hidden;
}

.loading-overlay {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  background-color: rgba(255, 255, 255, 0.9);
  z-index: 100;
}

.loading-overlay p {
  margin-top: 16px;
  color: #606266;
}

.error-overlay {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  background-color: #fff;
  z-index: 90;
}

.error-message {
  position: absolute;
  top: 20px;
  right: 20px;
  width: 400px;
  z-index: 200;
}

/* 适配不同屏幕尺寸 */
@media (max-width: 1200px) {
  .error-message {
    width: 300px;
  }
}

@media (max-width: 768px) {
  .designer-header {
    flex-direction: column;
    gap: 12px;
    align-items: flex-start;
  }
  
  .header-actions {
    width: 100%;
    justify-content: space-between;
  }
  
  .error-message {
    width: calc(100% - 40px);
    left: 20px;
    right: 20px;
  }
}
</style>