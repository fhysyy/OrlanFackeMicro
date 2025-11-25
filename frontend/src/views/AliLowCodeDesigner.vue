<template>
  <div class="ali-lowcode-designer">
    <!-- 设计器头部 -->
    <div class="designer-header">
      <h2>阿里低代码设计器</h2>
      <div class="header-actions">
        <el-button type="primary" @click="handleSave">保存</el-button>
        <el-button @click="handlePreview">预览</el-button>
        <el-button @click="handleDesign" v-if="isPreviewMode">返回设计</el-button>
        <el-button @click="handleExport">导出代码</el-button>
      </div>
    </div>
    
    <!-- 设计器主体 -->
    <div class="designer-container">
      <!-- 低代码引擎容器 -->
      <div ref="engineContainer" class="engine-container"></div>
      
      <!-- 状态提示 -->
      <div v-if="isLoading" class="loading-overlay">
        <div class="el-loading-spinner">
          <svg class="circular" viewBox="25 25 50 50">
            <circle class="path" cx="50" cy="50" r="20" fill="none" stroke-width="2" stroke="currentColor"></circle>
          </svg>
        </div>
        <p>正在初始化低代码引擎...</p>
      </div>
      
      <!-- 引擎状态提示 -->
      <div v-if="engineStatus && !engineStatus.hasEngine" class="status-notice">
        <el-alert
          title="低代码引擎模拟模式"
          type="info"
          description="依赖未完全安装，当前运行在模拟模式下"
          show-icon
          :closable="false"
        ></el-alert>
      </div>
      
      <!-- 错误提示 -->
      <div v-if="errorMessage" class="error-message">
        <el-alert
          :title="errorMessage"
          type="error"
          description="请检查低代码引擎依赖是否正确安装"
          show-icon
          :closable="true"
          @close="errorMessage = ''"
        ></el-alert>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, nextTick } from 'vue';
import { ElMessage, ElLoading } from 'element-plus';
import { aliLowCodeService, type LowCodeSchema } from '@/services/aliLowCodeService';

// 状态管理
const isLoading = ref(true);
const errorMessage = ref('');
const currentSchema = ref<LowCodeSchema | null>(null);
const engineContainer = ref<HTMLElement | null>(null);
const isPreviewMode = ref(false);
const engineStatus = ref<{ initialized: boolean; hasEngine: boolean } | null>(null);

// 初始化低代码引擎
const initializeEngine = async () => {
  try {
    isLoading.value = true;
    
    // 等待DOM更新后获取容器元素
    await nextTick();
    
    // 获取引擎容器
    if (!engineContainer.value) {
      throw new Error('未找到低代码引擎容器');
    }
    
    // 初始化低代码引擎服务
    await aliLowCodeService.initialize(engineContainer.value);
    
    // 获取引擎状态
    engineStatus.value = aliLowCodeService.getStatus();
    
    // 加载初始配置
    currentSchema.value = aliLowCodeService.exportSchema();
    
    isLoading.value = false;
    
    if (engineStatus.value.hasEngine) {
      ElMessage.success('低代码引擎初始化成功');
    } else {
      ElMessage.warning('低代码引擎运行在模拟模式下');
    }
  } catch (error) {
    console.error('初始化低代码引擎失败:', error);
    errorMessage.value = error instanceof Error ? error.message : '初始化失败';
    isLoading.value = false;
  }
};

// 保存当前配置
const handleSave = async () => {
  try {
    // 获取最新的schema
    currentSchema.value = aliLowCodeService.exportSchema();
    
    if (!currentSchema.value) {
      ElMessage.warning('没有可保存的配置');
      return;
    }
    
    // 这里应该调用API保存配置到后端
    console.log('保存配置:', currentSchema.value);
    
    // 模拟API调用延迟
    await new Promise(resolve => setTimeout(resolve, 500));
    
    ElMessage.success('配置保存成功');
  } catch (error) {
    console.error('保存配置失败:', error);
    ElMessage.error('配置保存失败');
  }
};

// 预览当前设计
const handlePreview = async () => {
  try {
    await aliLowCodeService.preview();
    isPreviewMode.value = true;
    ElMessage.success('进入预览模式');
  } catch (error) {
    console.error('预览失败:', error);
    ElMessage.error('预览失败');
  }
};

// 返回设计模式
const handleDesign = async () => {
  try {
    await aliLowCodeService.design();
    isPreviewMode.value = false;
    ElMessage.success('返回设计模式');
  } catch (error) {
    console.error('返回设计模式失败:', error);
    ElMessage.error('返回设计模式失败');
  }
};

// 导出代码
const handleExport = async () => {
  try {
    // 获取最新的schema
    const schema = aliLowCodeService.exportSchema();
    
    // 这里应该调用API导出代码
    console.log('导出代码:', schema);
    
    // 模拟API调用延迟
    await new Promise(resolve => setTimeout(resolve, 1000));
    
    // 模拟下载代码文件
    const codeContent = JSON.stringify(schema, null, 2);
    const blob = new Blob([codeContent], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `lowcode-schema-${new Date().getTime()}.json`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
    
    ElMessage.success('代码导出成功');
  } catch (error) {
    console.error('导出代码失败:', error);
    ElMessage.error('代码导出失败');
  }
};

// 监听窗口大小变化，重新布局引擎
const handleResize = () => {
  if (engineStatus.value?.hasEngine && engineContainer.value) {
    // 当引擎可用时，这里可以调用引擎的重新布局方法
    console.log('窗口大小变化，触发引擎重新布局');
  }
};

// 生命周期钩子
onMounted(() => {
  initializeEngine();
  window.addEventListener('resize', handleResize);
});

onBeforeUnmount(() => {
  // 移除事件监听
  window.removeEventListener('resize', handleResize);
  
  // 销毁低代码引擎实例
  try {
    aliLowCodeService.destroy();
  } catch (error) {
    console.error('销毁引擎失败:', error);
  }
});
</script>

<style scoped>
.ali-lowcode-designer {
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
}

.engine-container {
  width: 100%;
  height: 100%;
  background-color: #fff;
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

.status-notice {
  position: absolute;
  top: 20px;
  right: 20px;
  width: 400px;
  z-index: 150;
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
  .status-notice,
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
  
  .status-notice,
  .error-message {
    width: calc(100% - 40px);
    left: 20px;
    right: 20px;
  }
}
</style>