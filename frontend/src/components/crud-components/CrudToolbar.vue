<template>
  <div class="crud-toolbar">
    <el-button-group>
      <template v-for="action in toolbarActions" :key="action.name">
        <el-button
          v-if="isActionVisible(action)"
          :type="action.type"
          :icon="action.icon"
          :disabled="isActionDisabled(action)"
          @click="handleToolbarAction(action)"
        >
          {{ action.name }}
        </el-button>
      </template>
    </el-button-group>
    
    <!-- 批量操作 -->
    <el-dropdown
      v-if="selectedRows.length > 0 && batchActions.length > 0"
      @command="handleBatchAction"
    >
      <el-button type="primary" plain>
        批量操作 <el-icon class="el-icon--right"><arrow-down /></el-icon>
      </el-button>
      <template #dropdown>
        <el-dropdown-menu>
          <el-dropdown-item
            v-for="action in batchActions"
            :key="action"
            :command="action"
          >
            {{ getActionName(action) }}
          </el-dropdown-item>
        </el-dropdown-menu>
      </template>
    </el-dropdown>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { ArrowDown } from '@element-plus/icons-vue';
import type { CrudAction } from '../../types/crud';

// Props定义
const props = defineProps<{
  toolbarActions: any[];
  batchActions: CrudAction[];
  selectedRows: any[];
}>();

// Emits定义
const emit = defineEmits<{
  (e: 'toolbar-action', action: any): void;
  (e: 'batch-action', action: CrudAction): void;
}>();

// 判断操作是否显示
const isActionVisible = (action: any): boolean => {
  if (typeof action.show === 'function') {
    return action.show();
  }
  return action.show !== false;
};

// 判断操作是否禁用
const isActionDisabled = (action: any): boolean => {
  if (typeof action.disabled === 'function') {
    return action.disabled();
  }
  return action.disabled || false;
};

// 获取操作名称
const getActionName = (action: CrudAction): string => {
  const actionMap: Record<CrudAction, string> = {
    [CrudAction.BATCH_DELETE]: '批量删除',
    [CrudAction.EXPORT]: '导出选中',
    [CrudAction.IMPORT]: '导入'
  };
  return actionMap[action] || action;
};

// 处理工具栏操作
const handleToolbarAction = (action: any) => {
  emit('toolbar-action', action);
};

// 处理批量操作
const handleBatchAction = (action: CrudAction) => {
  emit('batch-action', action);
};
</script>

<style scoped>
.crud-toolbar {
  margin-bottom: 16px;
  display: flex;
  justify-content: space-between;
  align-items: center;
}
</style>