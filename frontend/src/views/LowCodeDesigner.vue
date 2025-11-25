<template>
  <div class="low-code-designer">
    <!-- 顶部工具栏 -->
    <div class="designer-header">
      <div class="header-left">
        <el-button type="primary" @click="savePage">保存页面</el-button>
        <el-button @click="previewPage">预览</el-button>
        <el-button @click="exportPage">导出</el-button>
        <el-upload
          class="upload-demo"
          action
          :auto-upload="false"
          :on-change="handleImport"
          accept=".json"
          :show-file-list="false"
        >
          <el-button>导入</el-button>
        </el-upload>
      </div>
      
      <div class="header-center">
        <el-input
          v-model="currentPageName"
          placeholder="页面名称"
          size="small"
          :style="{ width: '200px' }"
          @change="updatePageName"
        />
        <el-input
          v-model="currentPagePath"
          placeholder="路由路径"
          size="small"
          :style="{ width: '200px' }"
          @change="updatePagePath"
        />
      </div>
      
      <div class="header-right">
        <el-button-group>
          <el-button :icon="isMobile ? 'el-icon-refresh-left' : 'el-icon-mobile'" @click="toggleDevice">
            {{ isMobile ? '桌面' : '手机' }}
          </el-button>
          <el-button icon="el-icon-s-tools" @click="showSettings = !showSettings">
            设置
          </el-button>
        </el-button-group>
      </div>
    </div>
    
    <div class="designer-content">
      <!-- 左侧组件面板 -->
      <div class="components-panel">
        <div class="panel-header">
          <h3>组件库</h3>
        </div>
        <div class="panel-content">
          <el-tabs v-model="activeCategory">
            <el-tab-pane
              v-for="(components, category) in componentCategories"
              :key="category"
              :label="category"
            >
              <div class="component-list">
                <div
                  v-for="component in components"
                  :key="component.type"
                  class="component-item"
                  draggable="true"
                  @dragstart="handleDragStart($event, component.type)"
                  @mouseenter="hoverComponent = component.type"
                  @mouseleave="hoverComponent = null"
                >
                  <i :class="component.icon || 'el-icon-document'" />
                  <span>{{ component.name }}</span>
                </div>
              </div>
            </el-tab-pane>
          </el-tabs>
        </div>
      </div>
      
      <!-- 中间设计区域 -->
      <div class="design-area">
        <div class="device-frame" :class="{ 'mobile': isMobile }">
          <div 
            class="preview-container"
            @dragover.prevent
            @drop="handleDrop"
          >
            <dynamic-page
              ref="dynamicPage"
              :metadata="currentPageMetadata"
              :editable="true"
              @update:metadata="handleMetadataUpdate"
            />
          </div>
        </div>
        
        <!-- 悬浮的操作按钮 -->
        <div v-if="selectedComponentPath" class="component-actions">
          <el-tooltip content="添加子组件">
            <el-button
              size="mini"
              icon="el-icon-plus"
              @click="showAddComponentDialog"
            />
          </el-tooltip>
          <el-tooltip content="编辑组件">
            <el-button
              size="mini"
              icon="el-icon-edit"
              @click="showComponentConfigDialog"
            />
          </el-tooltip>
          <el-tooltip content="删除组件">
            <el-button
              size="mini"
              type="danger"
              icon="el-icon-delete"
              @click="deleteSelectedComponent"
            />
          </el-tooltip>
        </div>
      </div>
      
      <!-- 右侧属性面板 -->
      <div class="properties-panel">
        <div class="panel-header">
          <h3>属性配置</h3>
        </div>
        <div class="panel-content">
          <div v-if="!selectedComponentPath" class="empty-properties">
            请选择一个组件进行配置
          </div>
          
          <div v-else-if="selectedComponentConfig">
            <!-- 组件基本信息 -->
            <el-descriptions :column="1" :border="true" class="component-info">
              <el-descriptions-item label="组件类型">
                {{ selectedComponentConfig.name }}
              </el-descriptions-item>
              <el-descriptions-item label="组件路径">
                <code>{{ selectedComponentPath }}</code>
              </el-descriptions-item>
            </el-descriptions>
            
            <!-- 属性表单 -->
            <el-form :model="componentProps" label-width="100px">
              <template v-for="prop in selectedComponentConfig.props" :key="prop.name">
                <!-- 字符串输入 -->
                <el-form-item v-if="prop.type === 'string'" :label="prop.label">
                  <el-input
                    v-model="componentProps[prop.name]"
                    :placeholder="`请输入${prop.label}`"
                    @change="updateComponentProps"
                  />
                </el-form-item>
                
                <!-- 数值输入 -->
                <el-form-item v-else-if="prop.type === 'number'" :label="prop.label">
                  <el-input-number
                    v-model="componentProps[prop.name]"
                    :min="prop.min"
                    :max="prop.max"
                    @change="updateComponentProps"
                  />
                </el-form-item>
                
                <!-- 布尔值选择 -->
                <el-form-item v-else-if="prop.type === 'boolean'" :label="prop.label">
                  <el-switch
                    v-model="componentProps[prop.name]"
                    @change="updateComponentProps"
                  />
                </el-form-item>
                
                <!-- 下拉选择 -->
                <el-form-item v-else-if="prop.type === 'select'" :label="prop.label">
                  <el-select
                    v-model="componentProps[prop.name]"
                    placeholder="请选择"
                    @change="updateComponentProps"
                  >
                    <el-option
                      v-for="option in prop.options"
                      :key="option"
                      :label="option"
                      :value="option"
                    />
                  </el-select>
                </el-form-item>
                
                <!-- 文本域 -->
                <el-form-item v-else-if="prop.type === 'textarea'" :label="prop.label">
                  <el-input
                    v-model="componentProps[prop.name]"
                    type="textarea"
                    :rows="3"
                    placeholder="请输入"
                    @change="updateComponentProps"
                  />
                </el-form-item>
                
                <!-- 对象编辑器 -->
                <el-form-item v-else-if="prop.type === 'object'" :label="prop.label">
                  <el-input
                    v-model="componentProps[prop.name]"
                    type="textarea"
                    :rows="3"
                    placeholder="JSON格式"
                    @change="updateComponentProps"
                  />
                  <div class="form-hint">请输入JSON格式的对象</div>
                </el-form-item>
              </template>
            </el-form>
            
            <!-- 事件配置 -->
            <div v-if="selectedComponentConfig.events && selectedComponentConfig.events.length > 0" class="event-config">
              <h4>事件配置</h4>
              <div v-for="event in selectedComponentConfig.events" :key="event.name" class="event-item">
                <div class="event-header">
                  <span>{{ event.label }}</span>
                  <el-button size="mini" @click="showEventEditor(event)">
                    编辑
                  </el-button>
                </div>
                <div v-if="componentEvents[event.name]" class="event-code">
                  <pre>{{ componentEvents[event.name] }}</pre>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
    
    <!-- 页面设置对话框 -->
    <el-dialog
      v-model="showSettings"
      title="页面设置"
      width="600px"
    >
      <el-form :model="currentPageMetadata" label-width="100px">
        <el-form-item label="页面ID">
          <el-input v-model="currentPageMetadata.id" disabled />
        </el-form-item>
        <el-form-item label="页面名称">
          <el-input v-model="currentPageMetadata.name" @change="updatePageName" />
        </el-form-item>
        <el-form-item label="路由路径">
          <el-input v-model="currentPageMetadata.route.path" @change="updatePagePath" />
        </el-form-item>
        <el-form-item label="页面描述">
          <el-input v-model="currentPageMetadata.description" type="textarea" :rows="2" />
        </el-form-item>
        <el-form-item label="显示在菜单">
          <el-switch v-model="currentPageMetadata.route.meta.showInMenu" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showSettings = false">取消</el-button>
        <el-button type="primary" @click="savePageSettings">保存</el-button>
      </template>
    </el-dialog>
    
    <!-- 添加组件对话框 -->
    <el-dialog
      v-model="showAddComponent"
      title="添加组件"
      width="400px"
    >
      <el-select
        v-model="selectedComponentType"
        placeholder="选择组件类型"
        style="width: 100%"
      >
        <template v-for="(components, category) in componentCategories" :key="category">
          <el-option-group :label="category">
            <el-option
              v-for="component in components"
              :key="component.type"
              :label="component.name"
              :value="component.type"
            />
          </el-option-group>
        </template>
      </el-select>
      <template #footer>
        <el-button @click="showAddComponent = false">取消</el-button>
        <el-button type="primary" @click="confirmAddComponent">确定</el-button>
      </template>
    </el-dialog>
    
    <!-- 组件配置对话框 -->
    <el-dialog
      v-model="showComponentConfig"
      title="组件配置"
      width="800px"
      fullscreen
    >
      <el-tabs v-model="activeConfigTab">
        <el-tab-pane label="属性">
          <!-- 这里显示的内容与右侧属性面板相同 -->
          <el-form :model="componentProps" label-width="100px">
            <!-- 属性表单内容... -->
          </el-form>
        </el-tab-pane>
        <el-tab-pane label="事件">
          <!-- 事件配置内容... -->
        </el-tab-pane>
        <el-tab-pane label="高级">
          <el-input
            v-model="selectedComponentJson"
            type="textarea"
            :rows="20"
            placeholder="组件JSON配置"
          />
          <el-button type="primary" @click="updateComponentFromJson" style="margin-top: 20px">
            应用配置
          </el-button>
        </el-tab-pane>
      </el-tabs>
      <template #footer>
        <el-button @click="showComponentConfig = false">关闭</el-button>
      </template>
    </el-dialog>
    
    <!-- 事件编辑器对话框 -->
    <el-dialog
      v-model="showEventEditorDialog"
      :title="`编辑事件: ${currentEvent?.label}`"
      width="800px"
      fullscreen
    >
      <div class="event-editor">
        <div class="editor-toolbar">
          <el-button size="mini" @click="insertCodeSnippet('this.')">this.</el-button>
          <el-button size="mini" @click="insertCodeSnippet('$event')">$event</el-button>
          <el-button size="mini" @click="insertCodeSnippet('arguments')">arguments</el-button>
          <el-button size="mini" @click="insertCodeSnippet('api.')">api.</el-button>
          <el-button size="mini" @click="insertCodeSnippet('ElMessage.')">ElMessage.</el-button>
        </div>
        <el-input
          v-model="eventCode"
          type="textarea"
          :rows="20"
          placeholder="在此编写JavaScript代码"
        />
        <div class="editor-help">
          <h4>可用变量:</h4>
          <ul>
            <li><code>this</code> - 当前页面上下文</li>
            <li><code>$event</code> - 事件对象</li>
            <li><code>arguments</code> - 函数参数数组</li>
            <li><code>api</code> - API服务</li>
            <li><code>ElMessage</code> - 消息提示服务</li>
          </ul>
        </div>
      </div>
      <template #footer>
        <el-button @click="showEventEditorDialog = false">取消</el-button>
        <el-button type="primary" @click="saveEventCode">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script lang="ts" setup>
import { ref, reactive, computed, onMounted, nextTick, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import DynamicPage from '../components/DynamicPage.vue';
import { PageBuilderService } from '../services/pageBuilderService';
import { MetadataService } from '../services/metadataService';
import { getComponentCategories, getComponentConfig } from '../services/componentRegistry';
import type { PageMetadata, ComponentConfig } from '../types/page';

// 路由和导航
const route = useRoute();
const router = useRouter();

// 动态页面组件引用
const dynamicPage = ref<any>(null);

// 响应式数据
const isMobile = ref(false);
const showSettings = ref(false);
const showAddComponent = ref(false);
const showComponentConfig = ref(false);
const showEventEditorDialog = ref(false);
const activeCategory = ref('基础');
const activeConfigTab = ref('属性');
const hoverComponent = ref<string | null>(null);
const selectedComponentPath = ref('');
const selectedComponentType = ref('');
const currentEvent = ref<any>(null);
const eventCode = ref('');
const componentProps = ref<any>({});
const componentEvents = ref<Record<string, string>>({});
const selectedComponentJson = ref('');

// 页面数据
const currentPageMetadata = ref<PageMetadata>({
  id: `page_${Date.now()}`,
  name: '新页面',
  description: '低代码平台创建的页面',
  version: '1.0.0',
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  layout: {
    type: 'single-column'
  },
  route: {
    path: `/page/${Date.now()}`,
    name: `page_${Date.now()}`,
    meta: {
      title: '新页面',
      showInMenu: true
    }
  },
  components: [
    {
      type: 'card',
      props: {
        title: '页面内容',
        style: {
          margin: '20px'
        }
      },
      children: []
    }
  ]
});

// 计算属性
const currentPageName = computed({
  get: () => currentPageMetadata.value.name,
  set: (value) => {
    currentPageMetadata.value.name = value;
    if (currentPageMetadata.value.route?.meta) {
      currentPageMetadata.value.route.meta.title = value;
    }
  }
});

const currentPagePath = computed({
  get: () => currentPageMetadata.value.route?.path || '',
  set: (value) => {
    if (!currentPageMetadata.value.route) {
      currentPageMetadata.value.route = {} as any;
    }
    currentPageMetadata.value.route.path = value;
  }
});

const componentCategories = computed(() => {
  return getComponentCategories();
});

const selectedComponentConfig = computed(() => {
  if (!selectedComponentPath.value) return null;
  
  // 获取选中的组件类型
  const component = findComponentByPath(selectedComponentPath.value);
  if (!component) return null;
  
  return getComponentConfig(component.type);
});

// 初始化
onMounted(async () => {
  // 加载页面数据
  const pageId = route.params.pageId as string;
  if (pageId) {
    try {
      const metadata = await MetadataService.getMetadata(pageId);
      if (metadata) {
        currentPageMetadata.value = metadata;
      }
    } catch (error) {
      console.error('加载页面失败:', error);
      ElMessage.error('加载页面失败');
    }
  }
});

// 监听选中的组件变化
watch(selectedComponentPath, () => {
  if (selectedComponentPath.value) {
    loadComponentProps();
  }
});

// 方法定义

// 保存页面
const savePage = async () => {
  try {
    await MetadataService.saveMetadata(currentPageMetadata.value);
    ElMessage.success('页面保存成功');
  } catch (error) {
    console.error('保存页面失败:', error);
    ElMessage.error('保存页面失败');
  }
};

// 预览页面
const previewPage = () => {
  const previewWindow = window.open('', '_blank');
  if (previewWindow) {
      // 创建预览页面内容 - 使用变量和字符串拼接，完全避免HTML标签被Vue编译器解析
      const lt = '\u003c';
      const gt = '\u003e';
      const slash = '/';
      const metadata = JSON.stringify(currentPageMetadata.value);
      
      const html = 
        lt + '!DOCTYPE html' + gt + '\n' +
        lt + 'html lang="zh-CN"' + gt + '\n' +
        lt + 'head' + gt + '\n' +
        '  ' + lt + 'meta charset="UTF-8"' + gt + '\n' +
        '  ' + lt + 'meta name="viewport" content="width=device-width, initial-scale=1.0"' + gt + '\n' +
        '  ' + lt + 'title' + gt + '预览 - ' + currentPageName + lt + slash + 'title' + gt + '\n' +
        '  ' + lt + 'style' + gt + 'body { margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; }' + lt + slash + 'style' + gt + '\n' +
        lt + slash + 'head' + gt + '\n' +
        lt + 'body' + gt + '\n' +
        '  ' + lt + 'div id="app"' + gt + '正在加载...' + lt + slash + 'div' + gt + '\n' +
        '  ' + lt + 'script' + gt + 'window.__PAGE_METADATA__ = ' + metadata + ';' + lt + slash + 'script' + gt + '\n' +
        '  ' + lt + 'script src="/preview.js"' + gt + lt + slash + 'script' + gt + '\n' +
        lt + slash + 'body' + gt + '\n' +
        lt + slash + 'html' + gt;
    
    previewWindow.document.open();
    previewWindow.document.write(html);
    previewWindow.document.close();
  }
};

// 导出页面
const exportPage = () => {
  try {
    const content = MetadataService.exportMetadata(currentPageMetadata.value);
    const blob = new Blob([content], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${currentPageMetadata.value.name || 'page'}_${Date.now()}.json`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
    ElMessage.success('导出成功');
  } catch (error) {
    console.error('导出失败:', error);
    ElMessage.error('导出失败');
  }
};

// 导入页面
const handleImport = (file: any) => {
  const reader = new FileReader();
  reader.onload = (e) => {
    try {
      const content = e.target?.result as string;
      const metadata = MetadataService.importMetadata(content);
      currentPageMetadata.value = metadata;
      ElMessage.success('导入成功');
    } catch (error) {
      console.error('导入失败:', error);
      ElMessage.error('导入失败，请检查文件格式');
    }
  };
  reader.readAsText(file.raw);
};

// 切换设备预览模式
const toggleDevice = () => {
  isMobile.value = !isMobile.value;
};

// 保存页面设置
const savePageSettings = () => {
  savePage();
  showSettings.value = false;
};

// 处理拖拽开始
const handleDragStart = (event: DragEvent, componentType: string) => {
  if (event.dataTransfer) {
    event.dataTransfer.setData('componentType', componentType);
    event.dataTransfer.effectAllowed = 'copy';
  }
};

// 处理拖拽放下
const handleDrop = (event: DragEvent) => {
  event.preventDefault();
  
  if (event.dataTransfer) {
    const componentType = event.dataTransfer.getData('componentType');
    if (componentType) {
      const targetElement = event.target as HTMLElement;
      const targetPath = findComponentPathFromElement(targetElement);
      
      // 创建新组件配置
      const componentConfig: ComponentConfig = {
        type: componentType,
        props: getDefaultProps(componentType),
        children: []
      };
      
      // 添加组件
      if (targetPath) {
        dynamicPage.value.addComponent(targetPath, componentConfig);
      } else {
        // 如果没有找到目标，添加到根组件
        dynamicPage.value.addComponent('components[0]', componentConfig);
      }
    }
  }
};

// 获取组件默认属性
const getDefaultProps = (componentType: string): any => {
  const config = getComponentConfig(componentType);
  if (!config) return {};
  
  const props: any = {};
  config.props.forEach((prop: any) => {
    props[prop.name] = prop.default;
  });
  return props;
};

// 查找组件路径
const findComponentPathFromElement = (element: HTMLElement): string | null => {
  // 这里简化实现，实际需要根据元素层级计算路径
  return 'components[0].children';
};

// 查找组件
const findComponentByPath = (path: string): ComponentConfig | null => {
  // 简化实现，实际需要根据路径解析组件
  const pathParts = path.split('.');
  let current: any = currentPageMetadata.value;
  
  try {
    pathParts.forEach(part => {
      const match = part.match(/^(\w+)\[(\d+)\]$/);
      if (match) {
        const [, key, index] = match;
        current = current[key][parseInt(index)];
      } else {
        current = current[part];
      }
    });
    return current;
  } catch (error) {
    console.error('查找组件失败:', error);
    return null;
  }
};

// 加载组件属性
const loadComponentProps = () => {
  const component = findComponentByPath(selectedComponentPath.value);
  if (!component) return;
  
  componentProps.value = { ...component.props };
  
  // 加载事件配置
  componentEvents.value = {};
  if (component.events) {
    component.events.forEach((event: any) => {
      componentEvents.value[event.name] = event.body || '';
    });
  }
  
  // 加载JSON配置
  selectedComponentJson.value = JSON.stringify(component, null, 2);
};

// 更新组件属性
const updateComponentProps = () => {
  if (!selectedComponentPath.value) return;
  
  dynamicPage.value.updateComponent(selectedComponentPath.value, {
    props: componentProps.value
  });
};

// 更新页面名称
const updatePageName = () => {
  currentPageMetadata.value.updatedAt = new Date().toISOString();
};

// 更新页面路径
const updatePagePath = () => {
  currentPageMetadata.value.updatedAt = new Date().toISOString();
};

// 显示添加组件对话框
const showAddComponentDialog = () => {
  showAddComponent.value = true;
};

// 确认添加组件
const confirmAddComponent = () => {
  if (!selectedComponentType.value) {
    ElMessage.warning('请选择组件类型');
    return;
  }
  
  const componentConfig: ComponentConfig = {
    type: selectedComponentType.value,
    props: getDefaultProps(selectedComponentType.value),
    children: []
  };
  
  dynamicPage.value.addComponent(selectedComponentPath.value, componentConfig);
  showAddComponent.value = false;
  selectedComponentType.value = '';
};

// 显示组件配置对话框
const showComponentConfigDialog = () => {
  showComponentConfig.value = true;
};

// 删除选中的组件
const deleteSelectedComponent = () => {
  ElMessageBox.confirm('确定要删除这个组件吗？', '提示', {
    confirmButtonText: '确定',
    cancelButtonText: '取消',
    type: 'warning'
  }).then(() => {
    dynamicPage.value.removeComponent(selectedComponentPath.value);
    selectedComponentPath.value = '';
    ElMessage.success('删除成功');
  }).catch(() => {});
};

// 显示事件编辑器
const showEventEditor = (event: any) => {
  currentEvent.value = event;
  eventCode.value = componentEvents.value[event.name] || '';
  showEventEditorDialog.value = true;
};

// 保存事件代码
const saveEventCode = () => {
  if (!currentEvent.value) return;
  
  componentEvents.value[currentEvent.value.name] = eventCode.value;
  
  // 更新组件事件
  const events = Object.entries(componentEvents.value).map(([name, body]) => ({
    name,
    body
  }));
  
  dynamicPage.value.updateComponent(selectedComponentPath.value, {
    events
  });
  
  showEventEditorDialog.value = false;
  ElMessage.success('事件保存成功');
};

// 插入代码片段
const insertCodeSnippet = (snippet: string) => {
  const input = document.querySelector('.el-textarea__inner') as HTMLTextAreaElement;
  if (input) {
    const start = input.selectionStart;
    const end = input.selectionEnd;
    const value = input.value;
    input.value = value.substring(0, start) + snippet + value.substring(end);
    input.focus();
    input.setSelectionRange(start + snippet.length, start + snippet.length);
    eventCode.value = input.value;
  }
};

// 更新组件从JSON
const updateComponentFromJson = () => {
  try {
    const component = JSON.parse(selectedComponentJson.value);
    dynamicPage.value.updateComponent(selectedComponentPath.value, component);
    ElMessage.success('配置更新成功');
  } catch (error) {
    console.error('JSON格式错误:', error);
    ElMessage.error('JSON格式错误');
  }
};

// 处理元数据更新
const handleMetadataUpdate = (metadata: PageMetadata) => {
  currentPageMetadata.value = metadata;
};
</script>

<style scoped>
.low-code-designer {
  height: 100vh;
  display: flex;
  flex-direction: column;
  background-color: #f0f2f5;
}

/* 顶部工具栏 */
.designer-header {
  height: 60px;
  background-color: #fff;
  border-bottom: 1px solid #dcdfe6;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 20px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.08);
}

.header-left, .header-center, .header-right {
  display: flex;
  align-items: center;
  gap: 10px;
}

.header-center .el-input {
  margin: 0 10px;
}

/* 主内容区域 */
.designer-content {
  flex: 1;
  display: flex;
  overflow: hidden;
}

/* 组件面板 */
.components-panel {
  width: 280px;
  background-color: #fff;
  border-right: 1px solid #dcdfe6;
  display: flex;
  flex-direction: column;
}

.panel-header {
  padding: 15px;
  border-bottom: 1px solid #dcdfe6;
  background-color: #f5f7fa;
}

.panel-header h3 {
  margin: 0;
  font-size: 16px;
  color: #303133;
}

.panel-content {
  flex: 1;
  overflow-y: auto;
  padding: 15px;
}

.component-list {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 10px;
}

.component-item {
  padding: 10px;
  border: 1px solid #ebeef5;
  border-radius: 4px;
  cursor: move;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  transition: all 0.3s;
  min-height: 80px;
}

.component-item:hover {
  border-color: #409eff;
  background-color: #ecf5ff;
}

.component-item i {
  font-size: 24px;
  margin-bottom: 8px;
  color: #606266;
}

.component-item span {
  font-size: 12px;
  color: #606266;
}

/* 设计区域 */
.design-area {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20px;
  position: relative;
  overflow: auto;
}

.device-frame {
  background-color: #fff;
  border-radius: 8px;
  box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
  width: 100%;
  max-width: 1200px;
  height: 100%;
  overflow: auto;
  transition: all 0.3s;
  position: relative;
}

.device-frame.mobile {
  max-width: 375px;
  max-height: 667px;
  border: 12px solid #333;
  border-radius: 30px;
}

.device-frame.mobile:before {
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

.preview-container {
  width: 100%;
  height: 100%;
  min-height: 500px;
  position: relative;
}

/* 组件操作按钮 */
.component-actions {
  position: absolute;
  top: 10px;
  right: 10px;
  background-color: rgba(255, 255, 255, 0.95);
  border-radius: 4px;
  padding: 5px;
  box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
  z-index: 1000;
  display: flex;
  gap: 5px;
}

/* 属性面板 */
.properties-panel {
  width: 320px;
  background-color: #fff;
  border-left: 1px solid #dcdfe6;
  display: flex;
  flex-direction: column;
}

.empty-properties {
  padding: 40px 20px;
  text-align: center;
  color: #909399;
}

.component-info {
  margin-bottom: 20px;
}

.component-info code {
  background-color: #f5f7fa;
  padding: 2px 4px;
  border-radius: 3px;
  font-size: 12px;
  word-break: break-all;
}

.form-hint {
  font-size: 12px;
  color: #909399;
  margin-top: 5px;
}

.event-config {
  margin-top: 30px;
}

.event-config h4 {
  margin: 0 0 15px 0;
  font-size: 14px;
  color: #303133;
}

.event-item {
  margin-bottom: 15px;
  padding-bottom: 15px;
  border-bottom: 1px solid #ebeef5;
}

.event-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
}

.event-code {
  background-color: #f5f7fa;
  border-radius: 4px;
  padding: 10px;
  overflow-x: auto;
}

.event-code pre {
  margin: 0;
  font-size: 12px;
  color: #606266;
  white-space: pre-wrap;
  word-break: break-all;
}

/* 事件编辑器 */
.event-editor {
  height: calc(100vh - 200px);
  display: flex;
  flex-direction: column;
}

.editor-toolbar {
  margin-bottom: 10px;
  display: flex;
  gap: 5px;
  flex-wrap: wrap;
}

.editor-help {
  margin-top: 20px;
  padding: 15px;
  background-color: #f5f7fa;
  border-radius: 4px;
}

.editor-help h4 {
  margin: 0 0 10px 0;
  font-size: 14px;
}

.editor-help ul {
  margin: 0;
  padding-left: 20px;
}

.editor-help li {
  margin-bottom: 5px;
  font-size: 13px;
  color: #606266;
}

.editor-help code {
  background-color: #e9ecef;
  padding: 2px 4px;
  border-radius: 3px;
  font-size: 12px;
}

/* 响应式调整 */
@media (max-width: 1200px) {
  .components-panel {
    width: 240px;
  }
  
  .properties-panel {
    width: 280px;
  }
}

@media (max-width: 768px) {
  .designer-content {
    flex-direction: column;
  }
  
  .components-panel,
  .properties-panel {
    width: 100%;
    height: 300px;
  }
  
  .design-area {
    flex: 1;
  }
}
</style>