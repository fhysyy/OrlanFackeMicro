<template>
  <el-table-column
    v-if="!isHidden"
    :label="column.title"
    :prop="column.prop"
    :width="column.width"
    :min-width="column.minWidth"
    :sortable="column.sortable"
    :fixed="column.fixed"
    :align="column.align"
    :formatter="column.formatter"
  >
    <!-- 自定义渲染 -->
    <template v-if="hasCustomRender" #default="{ row, column: tableColumn, $index }">
      <!-- 自定义组件渲染 -->
      <component
        v-if="column.component"
        :is="column.component.name"
        v-bind="column.component.props"
        v-model="row[column.prop || '']"
        v-on="column.component.events"
        :row-data="row"
        :column-data="tableColumn"
        :row-index="$index"
      />
      <!-- 渲染函数 -->
      <template v-else-if="column.renderCell">
        <render-function
          :render="column.renderCell"
          :row="row"
          :column="tableColumn"
          :index="$index"
        />
      </template>
      <!-- 操作按钮 -->
      <template v-else-if="column.actions && column.actions.length > 0">
        <el-button
          v-for="action in visibleActions"
          :key="action.name"
          :type="action.type || 'default'"
          :icon="action.icon"
          :size="'small'"
          :disabled="isActionDisabled(action, row)"
          @click="handleAction(action, row, $index)"
          style="margin-right: 5px;"
        >
          {{ action.name }}
        </el-button>
      </template>
    </template>
    
    <!-- 子列 -->
    <template v-if="column.children && column.children.length > 0">
      <table-column-renderer
        v-for="child in column.children"
        :key="child.key || child.prop"
        :column="child"
        @cell-action="handleCellAction"
      />
    </template>
  </el-table-column>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { ElMessageBox } from 'element-plus';
import type { TableColumn } from '../types/table';

// Props定义
const props = defineProps<{
  column: TableColumn;
}>();

// Emits定义
const emit = defineEmits<{
  (e: 'cell-action', action: any, row: any): void;
}>();

// 计算属性
const isHidden = computed(() => props.column.hidden || false);

const hasCustomRender = computed(() => {
  return props.column.component || 
         props.column.renderCell || 
         (props.column.actions && props.column.actions.length > 0);
});

const visibleActions = computed(() => {
  if (!props.column.actions) return [];
  
  return props.column.actions.filter(action => {
    if (typeof action.show === 'function') {
      return action.show();
    }
    return action.show !== false;
  });
});

// 判断操作是否禁用
const isActionDisabled = (action: any, row: any): boolean => {
  if (typeof action.disabled === 'function') {
    return action.disabled(row);
  }
  return action.disabled || false;
};

// 处理操作点击
const handleAction = async (action: any, row: any, index: number) => {
  // 处理确认对话框
  if (action.confirm) {
    try {
      await ElMessageBox.confirm(
        action.confirm.message || '确定要执行此操作吗？',
        action.confirm.title || '确认操作',
        {
          confirmButtonText: '确定',
          cancelButtonText: '取消',
          type: action.confirm.type || 'warning'
        }
      );
    } catch {
      return;
    }
  }

  // 触发操作事件
  emit('cell-action', action, row);
};

// 处理单元格操作
const handleCellAction = (action: any, row: any) => {
  emit('cell-action', action, row);
};

// 渲染函数组件
const RenderFunction = {
  props: {
    render: {
      type: Function,
      required: true
    },
    row: {
      type: Object,
      required: true
    },
    column: {
      type: Object,
      required: true
    },
    index: {
      type: Number,
      required: true
    }
  },
  render() {
    return this.render(this.row, this.column, this.index);
  }
};
</script>

<style scoped>
/* 表格列渲染器样式 */
:deep(.el-button) {
  margin-right: 5px;
}

:deep(.el-button:last-child) {
  margin-right: 0;
}
</style>