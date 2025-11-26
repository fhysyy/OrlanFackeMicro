<template>
  <div class="simple-vtj-designer">
    <h1>简化版VTJ设计器</h1>
    
    <!-- 简单的设计器容器 -->
    <div id="designer-container" ref="designerContainer"></div>
    
    <!-- 状态信息 -->
    <div class="status-info">
      <h3>初始化状态：{{ status }}</h3>
      <p v-if="error">{{ error }}</p>
      <button @click="initDesigner">重新初始化</button>
    </div>
  </div>
</template>

<script setup lang="ts">
// 简化版VTJ设计器集成示例
// 2025-05-29
import { ref, onMounted, onUnmounted, watch, nextTick } from 'vue';
import { ElMessage, ElIcon } from 'element-plus';
import { Loading } from '@element-plus/icons-vue';
// 导入VTJ相关依赖
import { Engine } from '@vtj/designer';
import { createProvider, ContextMode } from '@vtj/renderer';
import type { Engine as EngineType } from '@vtj/designer';

// 设计器状态
const status = ref('未初始化');
const error = ref('');
const designerContainer = ref<HTMLElement | null>(null);
let engineInstance: EngineType | null = null;

// 初始化设计器
const initDesigner = async () => {
  try {
    status.value = '初始化中...';
    error.value = '';
    
    console.log('=== 简化版VTJ设计器初始化开始 ===');
    
    // 确保容器元素存在
    if (!designerContainer.value) {
      throw new Error('容器元素不存在');
    }
    
    const container = designerContainer.value;
    
    // 确保容器元素在DOM中
    if (!container.isConnected) {
      throw new Error('容器元素未连接到DOM');
    }
    
    container.innerHTML = '';
    
    // 确保容器有基本样式
    container.style.width = '100%';
    container.style.height = '500px';
    container.style.border = '1px solid #ccc';
    container.style.position = 'relative';
    container.style.overflow = 'hidden';
    container.style.backgroundColor = '#ffffff';
    
    // 确保容器有实际的尺寸
    if (container.offsetWidth === 0 || container.offsetHeight === 0) {
      throw new Error('容器元素尺寸为0，请检查CSS样式');
    }
    
    console.log('容器元素:', container);
    console.log('容器元素属性:', {
      hasChildNodes: container.hasChildNodes(),
      firstChild: container.firstChild,
      lastChild: container.lastChild,
      nextSibling: container.nextSibling,
      previousSibling: container.previousSibling,
      offsetWidth: container.offsetWidth,
      offsetHeight: container.offsetHeight,
      isConnected: container.isConnected
    });
    
    // 创建一个简化的服务模拟实现，包含Engine所需的基本方法
    const mockService = {
      // Engine所需的init方法 - 初始化项目数据
      init: async (project: any) => {
        console.log('简化服务初始化:', project);
        // 确保返回的数据结构符合VTJ设计器的期望，特别是确保所有数组字段都是数组
        return Promise.resolve({ 
          name: 'simple-test-project',
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
        console.log('简化服务销毁');
        return Promise.resolve();
      },
      // 提供必要的文件服务方法
      readFile: async (path: string) => {
        console.log('简化读取文件:', path);
        return '';
      },
      writeFile: async (path: string, content: string) => {
        console.log('简化写入文件:', path, content);
        return Promise.resolve();
      },
      exists: async (path: string) => {
        console.log('简化检查文件是否存在:', path);
        return false;
      },
      mkdir: async (path: string) => {
        console.log('简化创建目录:', path);
        return Promise.resolve();
      },
      readdir: async (path: string) => {
        console.log('简化读取目录:', path);
        return [];
      },
      delete: async (path: string) => {
        console.log('简化删除文件:', path);
        return Promise.resolve();
      },
      // 确保getExtension返回的数据结构符合VTJ设计器的期望
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
        console.log('保存简化项目:', project);
        return Promise.resolve(true);
      },
      publishFile: async (file: any) => {
        console.log('生成简化产品代码:', file);
        return Promise.resolve({ success: true });
      }
    };
    
    // 创建设计器引擎配置
    const engineOptions = {
      container: container,
      service: mockService as any,
      project: {
        name: 'simple-test-project',
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
    try {
      const engine = new Engine(engineOptions);
      engineInstance = engine;
      
      console.log('设计器引擎实例创建成功:', engine);
      status.value = '初始化成功';
      console.log('VTJ设计器引擎初始化成功');
    } catch (engineError) {
      console.error('Engine实例初始化失败:', engineError);
      console.error('错误堆栈:', engineError instanceof Error ? engineError.stack : '无堆栈信息');
      
      // 检查是否是nextSibling相关错误
      if (engineError instanceof Error && engineError.message.includes('nextSibling')) {
        console.error('错误类型: nextSibling访问错误');
        console.error('容器当前状态:', {
          hasChildNodes: container.hasChildNodes(),
          firstChild: container.firstChild,
          lastChild: container.lastChild,
          nextSibling: container.nextSibling,
          previousSibling: container.previousSibling
        });
      }
      
      throw engineError;
    }
    
    console.log('=== 简化版VTJ设计器初始化完成 ===');
  } catch (err) {
    console.error('=== 简化版VTJ设计器初始化失败 ===');
    console.error('错误详情:', err);
    console.error('错误堆栈:', err instanceof Error ? err.stack : '无堆栈信息');
    
    status.value = '初始化失败';
    error.value = err instanceof Error ? err.message : String(err);
    console.error('VTJ设计器初始化失败');
  }
};

// 组件挂载后初始化
onMounted(() => {
  // 延迟更长时间初始化，确保所有资源加载完成
  setTimeout(() => {
    initDesigner();
  }, 1500);
});

// 组件卸载时销毁设计器
onUnmounted(() => {
  if (engineInstance) {
    engineInstance.dispose();
    engineInstance = null;
  }
});
</script>

<style scoped>
.simple-vtj-designer {
  padding: 20px;
}

#designer-container {
  margin: 20px 0;
}

.status-info {
  margin-top: 20px;
  padding: 10px;
  background-color: #f0f0f0;
  border-radius: 4px;
}
</style>