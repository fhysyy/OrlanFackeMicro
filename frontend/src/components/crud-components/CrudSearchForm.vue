<template>
  <div class="crud-search-container" v-if="showSearch && searchFormConfig">
    <el-form
      :model="searchParams"
      :size="'small'"
      class="crud-search"
    >
      <FormGenerator
        :config="searchFormFullConfig"
        :model="searchParams"
      />
      <!-- 将按钮也融入栅格布局，保持整体一致性 -->
      <el-row :gutter="20" :justify="'end'" style="margin-top: 16px;">
        <el-col :span="6" :style="{ minWidth: '100px' }">
          <el-form-item>
            <el-button type="primary" @click="handleSearch" style="width: 80px;">搜索</el-button>
            <el-button @click="handleReset" style="width: 80px; margin-left: 8px;">重置</el-button>
          </el-form-item>
        </el-col>
      </el-row>
    </el-form>
  </div>
</template>

<script setup lang="ts">
import { computed, reactive, onMounted } from 'vue';
import FormGenerator from '../FormGenerator.vue';
import type { FormConfig } from '../../types/form';

// Props定义
const props = defineProps<{
  showSearch: boolean;
  searchFormConfig?: FormConfig;
  initialSearchParams?: Record<string, any>;
}>();

// Emits定义
const emit = defineEmits<{
  (e: 'search', params: any): void;
  (e: 'reset'): void;
}>();

// 响应式数据
const searchParams = reactive(props.initialSearchParams || {});

// 计算属性
const searchFormFullConfig = computed(() => ({
  fields: props.searchFormConfig?.fields || [],
  labelWidth: '100px',
  layout: 'grid-right' // 使用栅格布局，每行最多四个条件，居右对齐
}));

// 初始化默认搜索参数
const initDefaultSearchParams = () => {
  if (props.searchFormConfig) {
    props.searchFormConfig.fields.forEach(field => {
      if (field.defaultValue !== undefined) {
        searchParams[field.name] = field.defaultValue;
      }
    });
  }
  
  // 如果有初始搜索参数，合并它们
  if (props.initialSearchParams) {
    Object.assign(searchParams, props.initialSearchParams);
  }
};

// 处理搜索
const handleSearch = () => {
  emit('search', { ...searchParams });
};

// 处理重置
const handleReset = () => {
  // 清空搜索参数
  Object.keys(searchParams).forEach(key => {
    delete searchParams[key];
  });
  // 重置为默认值
  initDefaultSearchParams();
  // 触发重置事件
  emit('reset');
};

// 暴露方法给父组件
defineExpose({
  getSearchParams: () => ({ ...searchParams }),
  setSearchParams: (params: Record<string, any>) => {
    Object.assign(searchParams, params);
  },
  resetSearchParams: handleReset
});

// 生命周期
onMounted(() => {
  initDefaultSearchParams();
});
</script>

<style scoped>
.crud-search {
  margin-bottom: 16px;
  padding: 16px;
  background: #f5f7fa;
  border-radius: 4px;
  display: flex;
  flex-wrap: wrap;
  align-items: flex-end;
}

.search-buttons {
  margin-left: auto;
  margin-bottom: 20px;
}
</style>