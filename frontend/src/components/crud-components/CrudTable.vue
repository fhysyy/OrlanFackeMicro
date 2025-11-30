<template>
  <el-table
    ref="tableRef"
    :data="dataList"
    :loading="loading"
    :stripe="true"
    :border="true"
    :row-key="rowKey || 'id'"
    @selection-change="handleSelectionChange"
    @sort-change="handleSortChange"
  >
    <!-- 选择列 -->
    <el-table-column
      v-if="showSelection"
      type="selection"
      width="55"
      :selectable="selectable"
    />

    <!-- 序号列 -->
    <el-table-column
      v-if="showIndex"
      type="index"
      width="50"
      label="序号"
    />

    <!-- 自定义列 -->
    <template v-for="column in columns" :key="column.key || column.prop">
      <table-column-renderer
        :column="column"
        @cell-action="handleCellAction"
      />
    </template>

    <!-- 操作列 -->
    <el-table-column
      v-if="showActions"
      label="操作"
      :width="actionWidth || 200"
      fixed="right"
    >
      <template #default="{ row, $index }">
        <template v-for="action in rowActions" :key="action.name">
          <el-button
            v-if="isActionVisible(action, row)"
            :type="action.type || 'default'"
            :icon="action.icon"
            :size="'small'"
            :disabled="isActionDisabled(action, row)"
            @click="handleRowAction(action, row, $index)"
            style="margin-right: 5px;"
          >
            {{ action.name }}
          </el-button>
        </template>
      </template>
    </el-table-column>
  </el-table>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { ElMessageBox } from 'element-plus';
import TableColumnRenderer from '../TableColumnRenderer.vue';
import type { TableColumn } from '../../types/table';

// Props定义
const props = defineProps<{
  dataList: any[];
  loading: boolean;
  columns: TableColumn[];
  rowActions: any[];
  showSelection?: boolean;
  showIndex?: boolean;
  showActions?: boolean;
  rowKey?: string;
  actionWidth?: number;
  selectable?: (row: any, index: number) => boolean;
}>();

// Emits定义
const emit = defineEmits<{
  (e: 'selection-change', selection: any[]): void;
  (e: 'sort-change', sort: { prop: string; order: string }): void;
  (e: 'row-action', action: any, row: any, index: number): void;
  (e: 'cell-action', action: any, row: any): void;
}>();

// 表格引用
const tableRef = ref();

// 判断操作是否显示
const isActionVisible = (action: any, row: any): boolean => {
  if (typeof action.show === 'function') {
    return action.show(row);
  }
  return action.show !== false;
};

// 判断操作是否禁用
const isActionDisabled = (action: any, row: any): boolean => {
  if (typeof action.disabled === 'function') {
    return action.disabled(row);
  }
  return action.disabled || false;
};

// 处理选择变化
const handleSelectionChange = (selection: any[]) => {
  emit('selection-change', selection);
};

// 处理排序变化
const handleSortChange = ({ prop, order }: { prop: string; order: string }) => {
  emit('sort-change', { prop, order });
};

// 处理行操作
const handleRowAction = async (action: any, row: any, index: number) => {
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

  emit('row-action', action, row, index);
};

// 处理单元格操作
const handleCellAction = (action: any, row: any) => {
  emit('cell-action', action, row);
};

// 暴露方法给父组件
defineExpose({
  tableRef,
  clearSelection: () => tableRef.value?.clearSelection(),
  toggleRowSelection: (row: any, selected?: boolean) => 
    tableRef.value?.toggleRowSelection(row, selected),
  setCurrentRow: (row: any) => tableRef.value?.setCurrentRow(row)
});
</script>