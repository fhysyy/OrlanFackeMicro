<template>
  <div class="property-editor">
    <!-- 基本信息区域 -->
    <div v-if="componentConfig" class="property-section">
      <div class="section-header">
        <h4>基本信息</h4>
      </div>
      <el-descriptions :column="1" :border="true" class="component-info">
        <el-descriptions-item label="组件类型">
          {{ componentConfig.name }}
        </el-descriptions-item>
        <el-descriptions-item label="组件ID">
          <el-input 
            v-model="componentProps.id" 
            placeholder="请输入组件ID" 
            @change="handlePropertyChange('id')"
          />
        </el-descriptions-item>
        <el-descriptions-item label="显示名称">
          <el-input 
            v-model="componentProps.title" 
            placeholder="请输入显示名称" 
            @change="handlePropertyChange('title')"
          />
        </el-descriptions-item>
      </el-descriptions>
    </div>

    <!-- 布局属性区域 -->
    <div class="property-section">
      <div class="section-header">
        <h4>布局属性</h4>
        <el-switch v-model="showLayoutAdvanced" size="small" />
      </div>
      
      <el-form :model="layoutProps" label-width="100px">
        <!-- 基础布局属性 -->
        <el-form-item label="宽度">
          <el-input 
            v-model="layoutProps.width" 
            placeholder="如: 100%, 200px, auto"
            @change="handleLayoutChange('width')"
          />
        </el-form-item>
        
        <el-form-item label="高度">
          <el-input 
            v-model="layoutProps.height" 
            placeholder="如: 200px, auto"
            @change="handleLayoutChange('height')"
          />
        </el-form-item>
        
        <el-form-item label="对齐方式">
          <el-select 
            v-model="layoutProps.align" 
            placeholder="选择对齐方式"
            @change="handleLayoutChange('align')"
          >
            <el-option label="左对齐" value="left" />
            <el-option label="居中" value="center" />
            <el-option label="右对齐" value="right" />
          </el-select>
        </el-form-item>
        
        <!-- 高级布局属性 -->
        <template v-if="showLayoutAdvanced">
          <el-form-item label="内边距">
            <el-input 
              v-model="layoutProps.padding" 
              placeholder="如: 10px, 10px 20px"
              @change="handleLayoutChange('padding')"
            />
          </el-form-item>
          
          <el-form-item label="外边距">
            <el-input 
              v-model="layoutProps.margin" 
              placeholder="如: 10px, 10px 20px"
              @change="handleLayoutChange('margin')"
            />
          </el-form-item>
          
          <el-form-item label="定位方式">
            <el-select 
              v-model="layoutProps.position" 
              placeholder="选择定位方式"
              @change="handleLayoutChange('position')"
            >
              <el-option label="静态" value="static" />
              <el-option label="相对" value="relative" />
              <el-option label="绝对" value="absolute" />
              <el-option label="固定" value="fixed" />
              <el-option label="粘性" value="sticky" />
            </el-select>
          </el-form-item>
          
          <el-form-item label="Z轴索引">
            <el-input-number 
              v-model="layoutProps.zIndex" 
              :min="-9999" 
              :max="9999"
              @change="handleLayoutChange('zIndex')"
            />
          </el-form-item>
        </template>
      </el-form>
    </div>

    <!-- 样式属性区域 -->
    <div class="property-section">
      <div class="section-header">
        <h4>样式属性</h4>
        <el-switch v-model="showStyleAdvanced" size="small" />
      </div>
      
      <el-form :model="styleProps" label-width="100px">
        <!-- 基础样式属性 -->
        <el-form-item label="背景颜色">
          <div class="color-picker-wrapper">
            <el-color-picker 
              v-model="styleProps.backgroundColor" 
              show-alpha
              @change="handleStyleChange('backgroundColor')"
            />
            <el-input 
              v-model="styleProps.backgroundColor" 
              :style="{ marginLeft: '10px' }"
              placeholder="如: #ffffff, rgba(255,255,255,0.5)"
              @change="handleStyleChange('backgroundColor')"
            />
          </div>
        </el-form-item>
        
        <el-form-item label="文字颜色">
          <div class="color-picker-wrapper">
            <el-color-picker 
              v-model="styleProps.color" 
              show-alpha
              @change="handleStyleChange('color')"
            />
            <el-input 
              v-model="styleProps.color" 
              :style="{ marginLeft: '10px' }"
              placeholder="如: #333333"
              @change="handleStyleChange('color')"
            />
          </div>
        </el-form-item>
        
        <el-form-item label="字体大小">
          <el-input 
            v-model="styleProps.fontSize" 
            placeholder="如: 14px, 1rem"
            @change="handleStyleChange('fontSize')"
          />
        </el-form-item>
        
        <!-- 高级样式属性 -->
        <template v-if="showStyleAdvanced">
          <el-form-item label="字体">
            <el-select 
              v-model="styleProps.fontFamily" 
              placeholder="选择字体"
              @change="handleStyleChange('fontFamily')"
            >
              <el-option label="默认" value="" />
              <el-option label="宋体" value="SimSun" />
              <el-option label="微软雅黑" value="Microsoft YaHei" />
              <el-option label="Arial" value="Arial" />
              <el-option label="Times New Roman" value="Times New Roman" />
            </el-select>
          </el-form-item>
          
          <el-form-item label="字体样式">
            <el-select 
              v-model="styleProps.fontWeight" 
              placeholder="选择字体样式"
              @change="handleStyleChange('fontWeight')"
            >
              <el-option label="正常" value="normal" />
              <el-option label="加粗" value="bold" />
              <el-option label="更细" value="lighter" />
              <el-option label="100" value="100" />
              <el-option label="200" value="200" />
              <el-option label="300" value="300" />
              <el-option label="400" value="400" />
              <el-option label="500" value="500" />
              <el-option label="600" value="600" />
              <el-option label="700" value="700" />
              <el-option label="800" value="800" />
              <el-option label="900" value="900" />
            </el-select>
          </el-form-item>
          
          <el-form-item label="边框样式">
            <el-input 
              v-model="styleProps.border" 
              placeholder="如: 1px solid #ddd"
              @change="handleStyleChange('border')"
            />
          </el-form-item>
          
          <el-form-item label="边框圆角">
            <el-input 
              v-model="styleProps.borderRadius" 
              placeholder="如: 4px, 50%"
              @change="handleStyleChange('borderRadius')"
            />
          </el-form-item>
          
          <el-form-item label="阴影">
            <el-input 
              v-model="styleProps.boxShadow" 
              placeholder="如: 0 2px 8px rgba(0,0,0,0.1)"
              @change="handleStyleChange('boxShadow')"
            />
          </el-form-item>
        </template>
      </el-form>
    </div>

    <!-- 自定义属性区域 -->
    <div v-if="customPropsConfig && customPropsConfig.length > 0" class="property-section">
      <div class="section-header">
        <h4>组件属性</h4>
      </div>
      
      <el-form :model="customProps" label-width="100px">
        <template v-for="prop in customPropsConfig" :key="prop.name">
          <!-- 字符串输入 -->
          <el-form-item v-if="prop.type === 'string'" :label="prop.label">
            <el-input
              v-model="customProps[prop.name]"
              :placeholder="prop.placeholder || `请输入${prop.label}`"
              :disabled="prop.disabled"
              @change="handleCustomPropertyChange(prop.name)"
            />
            <div v-if="prop.description" class="prop-description">
              {{ prop.description }}
            </div>
          </el-form-item>
          
          <!-- 数值输入 -->
          <el-form-item v-else-if="prop.type === 'number'" :label="prop.label">
            <el-input-number
              v-model="customProps[prop.name]"
              :min="prop.min"
              :max="prop.max"
              :step="prop.step || 1"
              :disabled="prop.disabled"
              @change="handleCustomPropertyChange(prop.name)"
            />
            <div v-if="prop.description" class="prop-description">
              {{ prop.description }}
            </div>
          </el-form-item>
          
          <!-- 布尔值选择 -->
          <el-form-item v-else-if="prop.type === 'boolean'" :label="prop.label">
            <el-switch
              v-model="customProps[prop.name]"
              :disabled="prop.disabled"
              @change="handleCustomPropertyChange(prop.name)"
            />
            <div v-if="prop.description" class="prop-description">
              {{ prop.description }}
            </div>
          </el-form-item>
          
          <!-- 下拉选择 -->
          <el-form-item v-else-if="prop.type === 'select'" :label="prop.label">
            <el-select
              v-model="customProps[prop.name]"
              :placeholder="prop.placeholder || '请选择'"
              :disabled="prop.disabled"
              @change="handleCustomPropertyChange(prop.name)"
            >
              <el-option
                v-for="option in prop.options"
                :key="option.value || option"
                :label="option.label || option"
                :value="option.value || option"
              />
            </el-select>
            <div v-if="prop.description" class="prop-description">
              {{ prop.description }}
            </div>
          </el-form-item>
          
          <!-- 多选框 -->
          <el-form-item v-else-if="prop.type === 'checkbox'" :label="prop.label">
            <el-checkbox-group
              v-model="customProps[prop.name]"
              :disabled="prop.disabled"
              @change="handleCustomPropertyChange(prop.name)"
            >
              <el-checkbox
                v-for="option in prop.options"
                :key="option.value || option"
                :label="option.value || option"
              >
                {{ option.label || option }}
              </el-checkbox>
            </el-checkbox-group>
            <div v-if="prop.description" class="prop-description">
              {{ prop.description }}
            </div>
          </el-form-item>
          
          <!-- 单选框 -->
          <el-form-item v-else-if="prop.type === 'radio'" :label="prop.label">
            <el-radio-group
              v-model="customProps[prop.name]"
              :disabled="prop.disabled"
              @change="handleCustomPropertyChange(prop.name)"
            >
              <el-radio
                v-for="option in prop.options"
                :key="option.value || option"
                :label="option.value || option"
              >
                {{ option.label || option }}
              </el-radio>
            </el-radio-group>
            <div v-if="prop.description" class="prop-description">
              {{ prop.description }}
            </div>
          </el-form-item>
          
          <!-- 文本域 -->
          <el-form-item v-else-if="prop.type === 'textarea'" :label="prop.label">
            <el-input
              v-model="customProps[prop.name]"
              type="textarea"
              :rows="prop.rows || 3"
              :placeholder="prop.placeholder || '请输入'"
              :disabled="prop.disabled"
              @change="handleCustomPropertyChange(prop.name)"
            />
            <div v-if="prop.description" class="prop-description">
              {{ prop.description }}
            </div>
          </el-form-item>
          
          <!-- 对象编辑器 -->
          <el-form-item v-else-if="prop.type === 'object'" :label="prop.label">
            <el-input
              v-model="customProps[prop.name]"
              type="textarea"
              :rows="prop.rows || 3"
              :placeholder="prop.placeholder || 'JSON格式'"
              :disabled="prop.disabled"
              @change="handleCustomPropertyChange(prop.name)"
            />
            <div class="prop-description">
              {{ prop.description || '请输入JSON格式的对象' }}
            </div>
          </el-form-item>
          
          <!-- 数组编辑器 -->
          <el-form-item v-else-if="prop.type === 'array'" :label="prop.label">
            <el-input
              v-model="customProps[prop.name]"
              type="textarea"
              :rows="prop.rows || 3"
              :placeholder="prop.placeholder || 'JSON格式数组'"
              :disabled="prop.disabled"
              @change="handleCustomPropertyChange(prop.name)"
            />
            <div class="prop-description">
              {{ prop.description || '请输入JSON格式的数组' }}
            </div>
          </el-form-item>
          
          <!-- 日期选择器 -->
          <el-form-item v-else-if="prop.type === 'date'" :label="prop.label">
            <el-date-picker
              v-model="customProps[prop.name]"
              type="date"
              :placeholder="prop.placeholder || '选择日期'"
              style="width: 100%"
              :disabled="prop.disabled"
              @change="handleCustomPropertyChange(prop.name)"
            />
            <div v-if="prop.description" class="prop-description">
              {{ prop.description }}
            </div>
          </el-form-item>
          
          <!-- 时间选择器 -->
          <el-form-item v-else-if="prop.type === 'time'" :label="prop.label">
            <el-time-picker
              v-model="customProps[prop.name]"
              :placeholder="prop.placeholder || '选择时间'"
              style="width: 100%"
              :disabled="prop.disabled"
              @change="handleCustomPropertyChange(prop.name)"
            />
            <div v-if="prop.description" class="prop-description">
              {{ prop.description }}
            </div>
          </el-form-item>
          
          <!-- 日期时间选择器 -->
          <el-form-item v-else-if="prop.type === 'datetime'" :label="prop.label">
            <el-date-picker
              v-model="customProps[prop.name]"
              type="datetime"
              :placeholder="prop.placeholder || '选择日期时间'"
              style="width: 100%"
              :disabled="prop.disabled"
              @change="handleCustomPropertyChange(prop.name)"
            />
            <div v-if="prop.description" class="prop-description">
              {{ prop.description }}
            </div>
          </el-form-item>
          
          <!-- 颜色选择器 -->
          <el-form-item v-else-if="prop.type === 'color'" :label="prop.label">
            <div class="color-picker-wrapper">
              <el-color-picker 
                v-model="customProps[prop.name]" 
                show-alpha
                :disabled="prop.disabled"
                @change="handleCustomPropertyChange(prop.name)"
              />
              <el-input 
                v-model="customProps[prop.name]" 
                :style="{ marginLeft: '10px' }"
                :placeholder="prop.placeholder || '如: #ffffff, rgba(255,255,255,0.5)'"
                :disabled="prop.disabled"
                @change="handleCustomPropertyChange(prop.name)"
              />
            </div>
            <div v-if="prop.description" class="prop-description">
              {{ prop.description }}
            </div>
          </el-form-item>
          
          <!-- 选择器组 -->
          <el-form-item v-else-if="prop.type === 'option-group'" :label="prop.label">
            <el-cascader
              v-model="customProps[prop.name]"
              :options="prop.options"
              :props="{ multiple: prop.multiple }"
              :placeholder="prop.placeholder || '请选择'"
              :disabled="prop.disabled"
              @change="handleCustomPropertyChange(prop.name)"
            />
            <div v-if="prop.description" class="prop-description">
              {{ prop.description }}
            </div>
          </el-form-item>
          
          <!-- 自定义组件 -->
          <el-form-item v-else-if="prop.component" :label="prop.label">
            <component
              :is="prop.component"
              v-model="customProps[prop.name]"
              v-bind="prop.componentProps || {}"
              :disabled="prop.disabled"
              @change="handleCustomPropertyChange(prop.name)"
            />
            <div v-if="prop.description" class="prop-description">
              {{ prop.description }}
            </div>
          </el-form-item>
          
          <!-- 默认处理 -->
          <el-form-item v-else :label="prop.label">
            <el-input
              v-model="customProps[prop.name]"
              :placeholder="`请输入${prop.label}`"
              :disabled="prop.disabled"
              @change="handleCustomPropertyChange(prop.name)"
            />
            <div v-if="prop.description" class="prop-description">
              {{ prop.description }}
            </div>
          </el-form-item>
        </template>
      </el-form>
    </div>

    <!-- 高级配置区域 -->
    <div class="property-section">
      <div class="section-header">
        <h4>高级配置</h4>
      </div>
      
      <el-form :model="advancedProps" label-width="100px">
        <el-form-item label="条件显示">
          <el-input
            v-model="advancedProps.condition"
            type="textarea"
            rows="2"
            placeholder="输入JavaScript表达式，返回true/false"
            @change="handleAdvancedPropertyChange('condition')"
          />
          <div class="prop-description">
            支持使用变量: {{ availableVariables.join(', ') }}
          </div>
        </el-form-item>
        
        <el-form-item label="计算属性">
          <el-button 
            type="text" 
            @click="showComputedPropsDialog = true"
          >
            配置计算属性 ({{ computedProperties.length }})
          </el-button>
        </el-form-item>
        
        <el-form-item label="自定义样式">
          <el-input
            v-model="advancedProps.customStyles"
            type="textarea"
            rows="3"
            placeholder="JSON格式的样式对象"
            @change="handleAdvancedPropertyChange('customStyles')"
          />
        </el-form-item>
      </el-form>
    </div>
  </div>

  <!-- 计算属性配置对话框 -->
  <el-dialog
    v-model="showComputedPropsDialog"
    title="配置计算属性"
    width="800px"
    :close-on-click-modal="false"
  >
    <div class="computed-props-dialog">
      <div class="dialog-toolbar">
        <el-button type="primary" @click="addComputedProp">添加计算属性</el-button>
      </div>
      
      <el-table :data="computedProperties" border>
        <el-table-column prop="name" label="属性名" width="120" />
        <el-table-column prop="expression" label="表达式">
          <template #default="scope">
            <el-input
              v-model="scope.row.expression"
              type="textarea"
              rows="2"
              @change="updateComputedProp(scope.row)"
            />
          </template>
        </el-table-column>
        <el-table-column label="操作" width="100" fixed="right">
          <template #default="scope">
            <el-button 
              type="danger" 
              size="small" 
              @click="removeComputedProp(scope.$index)"
            >
              删除
            </el-button>
          </template>
        </el-table-column>
      </el-table>
      
      <div class="dialog-help">
        <h4>计算属性说明:</h4>
        <ul>
          <li>计算属性可以使用 <code>props</code> 访问组件的属性</li>
          <li>可以使用 <code>state</code> 访问页面状态</li>
          <li>可以使用 <code>context</code> 访问页面上下文</li>
          <li>表达式将在组件渲染时执行</li>
        </ul>
      </div>
    </div>
    
    <template #footer>
      <el-button @click="showComputedPropsDialog = false">关闭</el-button>
    </template>
  </el-dialog>
</template>

<script lang="ts" setup>
import { ref, reactive, computed, watch, nextTick } from 'vue';

// 定义PropConfig接口
interface PropConfig {
  name: string;
  label?: string;
  type?: string;
  default?: any;
  description?: string;
  component?: any;
  componentProps?: Record<string, any>;
  disabled?: boolean;
  [key: string]: any;
}

// 组件属性
interface ComponentPropertyEditorProps {
  componentType: string;
  componentProps?: Record<string, any>;
  componentConfig?: {
    name: string;
    props?: PropConfig[];
  };
  availableVariables?: string[];
}

const props = withDefaults(defineProps<ComponentPropertyEditorProps>(), {
  componentProps: () => ({}),
  componentConfig: () => ({ name: '' }),
  availableVariables: () => ['props', 'state', 'context']
});

// 事件
const emit = defineEmits<{
  'update:props': [props: Record<string, any>];
  'propertyChange': [property: string, value: any];
}>();

// 响应式数据
const showLayoutAdvanced = ref(false);
const showStyleAdvanced = ref(false);
const showComputedPropsDialog = ref(false);

// 组件属性
const componentProps = reactive<Record<string, any>>({
  id: '',
  title: '',
  ...props.componentProps
});

// 布局属性
const layoutProps = reactive<{
  width?: string;
  height?: string;
  align?: string;
  padding?: string;
  margin?: string;
  position?: string;
  zIndex?: number;
}>({
  width: props.componentProps.style?.width || '',
  height: props.componentProps.style?.height || '',
  align: props.componentProps.align || 'left',
  padding: props.componentProps.style?.padding || '',
  margin: props.componentProps.style?.margin || '',
  position: props.componentProps.style?.position || 'static',
  zIndex: props.componentProps.style?.zIndex || 0
});

// 样式属性
const styleProps = reactive<{
  backgroundColor?: string;
  color?: string;
  fontSize?: string;
  fontFamily?: string;
  fontWeight?: string | number;
  border?: string;
  borderRadius?: string;
  boxShadow?: string;
}>({
  backgroundColor: props.componentProps.style?.backgroundColor || '',
  color: props.componentProps.style?.color || '',
  fontSize: props.componentProps.style?.fontSize || '',
  fontFamily: props.componentProps.style?.fontFamily || '',
  fontWeight: props.componentProps.style?.fontWeight || 'normal',
  border: props.componentProps.style?.border || '',
  borderRadius: props.componentProps.style?.borderRadius || '',
  boxShadow: props.componentProps.style?.boxShadow || ''
});

// 自定义属性
const customProps = reactive<Record<string, any>>({});

// 高级属性
const advancedProps = reactive<{
  condition?: string;
  customStyles?: string;
}>>({
  condition: props.componentProps.condition || '',
  customStyles: JSON.stringify(props.componentProps.customStyles || {}, null, 2)
});

// 计算属性配置
const computedProperties = reactive<Array<{
  name: string;
  expression: string;
}>>([...(props.componentProps.computedProperties || [])]);

// 计算属性
const customPropsConfig = computed(() => {
  return props.componentConfig?.props || [];
});

// 监听组件属性变化
watch(
  () => props.componentProps,
  (newProps) => {
    // 更新基本属性
    Object.assign(componentProps, newProps);
    
    // 更新布局属性
    layoutProps.width = newProps.style?.width || '';
    layoutProps.height = newProps.style?.height || '';
    layoutProps.align = newProps.align || 'left';
    layoutProps.padding = newProps.style?.padding || '';
    layoutProps.margin = newProps.style?.margin || '';
    layoutProps.position = newProps.style?.position || 'static';
    layoutProps.zIndex = newProps.style?.zIndex || 0;
    
    // 更新样式属性
    styleProps.backgroundColor = newProps.style?.backgroundColor || '';
    styleProps.color = newProps.style?.color || '';
    styleProps.fontSize = newProps.style?.fontSize || '';
    styleProps.fontFamily = newProps.style?.fontFamily || '';
    styleProps.fontWeight = newProps.style?.fontWeight || 'normal';
    styleProps.border = newProps.style?.border || '';
    styleProps.borderRadius = newProps.style?.borderRadius || '';
    styleProps.boxShadow = newProps.style?.boxShadow || '';
    
    // 更新自定义属性
    customPropsConfig.value.forEach(prop => {
      customProps[prop.name] = newProps[prop.name] ?? prop.default;
    });
    
    // 更新高级属性
    advancedProps.condition = newProps.condition || '';
    advancedProps.customStyles = JSON.stringify(newProps.customStyles || {}, null, 2);
    
    // 更新计算属性
    if (newProps.computedProperties) {
      computedProperties.splice(0, computedProperties.length, ...newProps.computedProperties);
    }
  },
  { deep: true, immediate: true }
);

// 方法定义

// 处理基本属性变化
const handlePropertyChange = (property: string) => {
  const value = componentProps[property];
  emit('propertyChange', property, value);
  updateComponentProps();
};

// 处理布局属性变化
const handleLayoutChange = (property: string) => {
  const value = layoutProps[property];
  updateStyle(property, value);
};

// 处理样式属性变化
const handleStyleChange = (property: string) => {
  const value = styleProps[property];
  updateStyle(property, value);
};

// 处理自定义属性变化
const handleCustomPropertyChange = (property: string) => {
  const value = customProps[property];
  emit('propertyChange', property, value);
  updateComponentProps();
};

// 处理高级属性变化
const handleAdvancedPropertyChange = (property: keyof typeof advancedProps) => {
  const value = advancedProps[property];
  emit('propertyChange', property, value);
  updateComponentProps();
};

// 更新样式
const updateStyle = (property: string, value: any) => {
  // 创建一个新的样式对象
  const newStyle: Record<string, any> = { ...componentProps.style };
  
  // 设置新的样式属性
  newStyle[property] = value;
  
  // 移除空值
  Object.keys(newStyle).forEach(key => {
    if (newStyle[key] === '' || newStyle[key] === null || newStyle[key] === undefined) {
      delete newStyle[key];
    }
  });
  
  // 更新组件属性
  componentProps.style = Object.keys(newStyle).length > 0 ? newStyle : undefined;
  emit('propertyChange', 'style', componentProps.style);
  updateComponentProps();
};

// 更新组件属性
const updateComponentProps = () => {
  // 构建完整的属性对象
  const updatedProps: Record<string, any> = { ...componentProps };
  
  // 添加布局相关的非样式属性
  if (layoutProps.align) {
    updatedProps.align = layoutProps.align;
  }
  
  // 添加自定义属性
  customPropsConfig.value.forEach(prop => {
    updatedProps[prop.name] = customProps[prop.name];
  });
  
  // 添加高级属性
  if (advancedProps.condition && typeof advancedProps.condition === 'string') {
    updatedProps.condition = advancedProps.condition;
  }
  
  if (advancedProps.customStyles && typeof advancedProps.customStyles === 'string') {
    try {
      updatedProps.customStyles = JSON.parse(advancedProps.customStyles);
    } catch (error) {
      console.error('Invalid JSON in customStyles:', error);
    }
  }
  
  // 添加计算属性
  if (computedProperties.length > 0) {
    updatedProps.computedProperties = [...computedProperties];
  }
  
  // 发送更新事件
  emit('update:props', updatedProps);
};

// 添加计算属性
const addComputedProp = () => {
  computedProperties.push({
    name: `computed_${Date.now()}`,
    expression: ''
  });
};

// 更新计算属性
const updateComputedProp = (prop: any) => {
  updateComponentProps();
};

// 移除计算属性
const removeComputedProp = (index: number) => {
  computedProperties.splice(index, 1);
  updateComponentProps();
};

// 初始化自定义属性
nextTick(() => {
  customPropsConfig.value.forEach(prop => {
    customProps[prop.name] = componentProps[prop.name] ?? prop.default;
  });
});
</script>

<style scoped>
.property-editor {
  height: 100%;
  overflow-y: auto;
  padding-bottom: 20px;
}

.property-section {
  margin-bottom: 24px;
  padding: 0 16px;
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
  padding-bottom: 8px;
  border-bottom: 1px solid #ebeef5;
}

.section-header h4 {
  margin: 0;
  font-size: 14px;
  font-weight: 500;
  color: #303133;
}

.component-info {
  margin-bottom: 0;
}

.color-picker-wrapper {
  display: flex;
  align-items: center;
}

.prop-description {
  margin-top: 4px;
  font-size: 12px;
  color: #909399;
}

/* 计算属性对话框样式 */
.computed-props-dialog {
  height: 400px;
  display: flex;
  flex-direction: column;
}

.dialog-toolbar { 
  margin-bottom: 16px;
}

.dialog-help {
  margin-top: 20px;
  padding: 12px;
  background-color: #f5f7fa;
  border-radius: 4px;
  font-size: 13px;
}

.dialog-help h4 {
  margin: 0 0 8px 0;
  font-size: 13px;
  color: #303133;
}

.dialog-help ul {
  margin: 0;
  padding-left: 20px;
}

.dialog-help li {
  margin-bottom: 4px;
  color: #606266;
}

.dialog-help code {
  background-color: #e9ecef;
  padding: 2px 4px;
  border-radius: 3px;
  font-size: 12px;
}

/* 滚动条样式 */
.property-editor::-webkit-scrollbar {
  width: 6px;
}

.property-editor::-webkit-scrollbar-track {
  background: #f1f1f1;
}

.property-editor::-webkit-scrollbar-thumb {
  background: #c0c4cc;
  border-radius: 3px;
}

.property-editor::-webkit-scrollbar-thumb:hover {
  background: #a0a4ac;
}
</style>