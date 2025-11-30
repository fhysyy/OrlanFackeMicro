<template>
  <div class="crud-template">
    <!-- 工具栏 -->
    <CrudToolbar
      :toolbar-actions="toolbarActions"
      :batch-actions="batchActions"
      :selected-rows="selectedRows"
      @toolbar-action="handleToolbarAction"
      @batch-action="handleBatchAction"
    />

    <!-- 搜索表单 -->
    <CrudSearchForm
      v-if="config.table.showSearch && searchFormConfig"
      :show-search="config.table.showSearch"
      :search-form-config="searchFormConfig"
      :initial-search-params="searchParams"
      @search="handleSearch"
      @reset="handleReset"
    />

    <!-- 表格 -->
    <CrudTable
      ref="tableRef"
      :data-list="dataList"
      :loading="loading"
      :columns="tableColumns"
      :row-actions="rowActions"
      :show-selection="config.table.showSelection"
      :show-index="config.table.showIndex"
      :show-actions="config.table.showActions"
      :row-key="config.table.rowKey || 'id'"
      :action-width="config.table.actionWidth"
      :selectable="config.table.selectable"
      @selection-change="handleSelectionChange"
      @sort-change="handleSortChange"
      @row-action="handleRowAction"
      @cell-action="handleCellAction"
    />

    <!-- 分页 -->
    <CrudPagination
      v-if="config.table.showPagination"
      :show-pagination="config.table.showPagination"
      :initial-current-page="pagination.currentPage"
      :initial-page-size="pagination.pageSize"
      :initial-total="pagination.total"
      @size-change="handleSizeChange"
      @current-change="handleCurrentChange"
    />

    <!-- 创建对话框 -->
    <CrudDialog
      v-model:visible="dialogVisible.create"
      dialog-type="create"
      :dialog-config="createFormConfig"
      :entity-name="config.entityName"
      :dialog-width="config.form.dialogWidth"
      :fullscreen="config.form.fullscreen"
      @submit="handleCreateSubmit"
      @cancel="dialogVisible.create = false"
    />

    <!-- 编辑对话框 -->
    <CrudDialog
      v-model:visible="dialogVisible.update"
      dialog-type="update"
      :dialog-config="updateFormConfig"
      :initial-data="currentData"
      :entity-name="config.entityName"
      :dialog-width="config.form.dialogWidth"
      :fullscreen="config.form.fullscreen"
      @submit="handleUpdateSubmit"
      @cancel="dialogVisible.update = false"
    />

    <!-- 查看对话框 -->
    <CrudDialog
      v-model:visible="dialogVisible.view"
      dialog-type="view"
      :dialog-config="viewFormConfig"
      :initial-data="currentData"
      :entity-name="config.entityName"
      :dialog-width="config.form.dialogWidth"
      @cancel="dialogVisible.view = false"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, onUnmounted } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import FormGenerator from './FormGenerator.vue';
import TableColumnRenderer from './TableColumnRenderer.vue';
import { CrudToolbar, CrudSearchForm, CrudTable, CrudPagination, CrudDialog } from './crud-components';
import type { CrudConfig, CrudContext, CrudAction } from '../types/crud';
import type { TableColumn } from '../types/table';
import type { FormConfig } from '../types/form';

// Props定义
const props = defineProps<{
  config: CrudConfig;
}>();

// Emits定义
const emit = defineEmits<{
  (e: 'load', params: any): void;
  (e: 'create', data: any): void;
  (e: 'update', data: any): void;
  (e: 'delete', id: any): void;
  (e: 'batch-delete', ids: any[]): void;
  (e: 'view', data: any): void;
  (e: 'refresh'): void;
  (e: 'error', error: any): void;
}>();

// 表格引用
const tableRef = ref();

// 组件引用 - 不需要表单引用，由CrudDialog组件内部管理

// 页面状态
const loading = ref(false);
const dataList = ref([]);
const searchParams = reactive({});
const pagination = reactive({
  currentPage: 1,
  pageSize: 10,
  total: 0
});
const sort = reactive({
  prop: '',
  order: ''
});
const selectedRows = ref([]);
const dialogVisible = reactive({
  create: false,
  update: false,
  view: false,
  import: false
});
const currentData = reactive({});

// 计算属性
const searchFormConfig = computed(() => props.config.table.searchFormConfig);
// 搜索表单配置直接使用searchFormConfig
const createFormConfig = computed(() => props.config.form.createFormConfig);
const updateFormConfig = computed(() => props.config.form.updateFormConfig);
const viewFormConfig = computed(() => props.config.form.viewFormConfig);
const tableColumns = computed(() => props.config.table.columns);
const toolbarActions = computed(() => props.config.actions.toolbar);
const rowActions = computed(() => props.config.actions.tableRow);
const batchActions = computed(() => props.config.actions.batchActions);

// 初始化默认搜索参数由CrudSearchForm组件内部处理

// 加载数据
const loadData = async () => {
  loading.value = true;
  try {
    // 构建请求参数
    const params = {
      ...searchParams,
      [props.config.data.requestMap?.pageNum || 'pageNum']: pagination.currentPage,
      [props.config.data.requestMap?.pageSize || 'pageSize']: pagination.pageSize
    };

    // 添加排序参数
    if (sort.prop && sort.order) {
      params[props.config.data.requestMap?.sortField || 'sortField'] = sort.prop;
      params[props.config.data.requestMap?.sortOrder || 'sortOrder'] = 
        sort.order === 'ascending' ? 'asc' : 'desc';
    }

    // 应用请求格式化
    const formattedParams = props.config.data.formatters?.requestFormatter 
      ? props.config.data.formatters.requestFormatter(params) 
      : params;

    // 触发加载事件
    emit('load', formattedParams);
  } catch (error) {
    emit('error', error);
    ElMessage.error('数据加载失败');
  } finally {
    loading.value = false;
  }
};

// 处理搜索
const handleSearch = () => {
  pagination.currentPage = 1;
  loadData();
};

// 处理重置
const handleReset = () => {
  // 清空搜索参数
  Object.keys(searchParams).forEach(key => {
    delete searchParams[key];
  });
  // 重置为默认值
  initDefaultSearchParams();
  // 重置分页
  pagination.currentPage = 1;
  // 重新加载
  loadData();
};

// 处理分页大小变化
const handleSizeChange = (size: number) => {
  pagination.pageSize = size;
  loadData();
};

// 处理当前页变化
const handleCurrentChange = (current: number) => {
  pagination.currentPage = current;
  loadData();
};

// 处理排序变化
const handleSortChange = ({ prop, order }: { prop: string; order: string }) => {
  sort.prop = prop;
  sort.order = order;
  loadData();
};

// 处理选择变化
const handleSelectionChange = (selection: any[]) => {
  selectedRows.value = selection;
};

// 判断操作是否显示
const isActionVisible = (action: any, row?: any): boolean => {
  if (typeof action.show === 'function') {
    return action.show(row);
  }
  return action.show !== false;
};

// 判断操作是否禁用
const isActionDisabled = (action: any, row?: any): boolean => {
  if (typeof action.disabled === 'function') {
    return action.disabled(row);
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
  if (action.onClick) {
    action.onClick();
    return;
  }

  switch (action.name) {
    case '新增':
    case '添加':
      handleCreate();
      break;
    case '导入':
      handleImport();
      break;
    case '导出':
      handleExport();
      break;
    case '刷新':
      handleRefresh();
      break;
  }
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

  if (action.onClick) {
    action.onClick(row, index);
    return;
  }

  switch (action.name) {
    case '编辑':
      handleEdit(row);
      break;
    case '删除':
      handleDelete(row);
      break;
    case '查看':
      handleView(row);
      break;
  }
};

// 处理单元格操作
const handleCellAction = (action: any, row: any) => {
  if (action.onClick) {
    action.onClick(row);
  }
};

// 处理批量操作
const handleBatchAction = async (action: string) => {
  switch (action) {
    case CrudAction.BATCH_DELETE:
      await handleBatchDelete();
      break;
    case CrudAction.EXPORT:
      handleExportSelected();
      break;
  }
};

// 创建操作
const handleCreate = () => {
  // 清空当前数据
  Object.keys(currentData).forEach(key => {
    delete currentData[key];
  });
  // 设置默认值
  if (createFormConfig.value) {
    createFormConfig.value.fields.forEach(field => {
      if (field.defaultValue !== undefined) {
        currentData[field.name] = field.defaultValue;
      }
    });
  }
  // 显示创建对话框
  dialogVisible.create = true;
};

// 编辑操作
const handleEdit = (row: any) => {
  // 复制数据到当前数据
  Object.keys(currentData).forEach(key => {
    delete currentData[key];
  });
  Object.assign(currentData, { ...row });
  // 显示编辑对话框
  dialogVisible.update = true;
};

// 查看操作
const handleView = (row: any) => {
  // 复制数据到当前数据
  Object.keys(currentData).forEach(key => {
    delete currentData[key];
  });
  Object.assign(currentData, { ...row });
  // 触发查看事件
  emit('view', row);
  // 显示查看对话框
  dialogVisible.view = true;
};

// 删除操作
const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(
      `确定要删除该${props.config.entityName}吗？`,
      '确认删除',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }
    );
    const id = row[props.config.table.rowKey || 'id'];
    emit('delete', id);
  } catch {
    // 取消删除
  }
};

// 批量删除操作
const handleBatchDelete = async () => {
  if (selectedRows.value.length === 0) {
    ElMessage.warning('请选择要删除的数据');
    return;
  }

  try {
    await ElMessageBox.confirm(
      `确定要删除选中的${selectedRows.value.length}条${props.config.entityName}吗？`,
      '确认删除',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }
    );
    const ids = selectedRows.value.map(row => 
      row[props.config.table.rowKey || 'id']
    );
    emit('batch-delete', ids);
  } catch {
    // 取消删除
  }
};

// 导入操作
const handleImport = () => {
  dialogVisible.import = true;
};

// 导出操作
const handleExport = () => {
  // 导出全部数据
  emit('export', searchParams);
};

// 导出选中操作
const handleExportSelected = () => {
  if (selectedRows.value.length === 0) {
    ElMessage.warning('请选择要导出的数据');
    return;
  }
  const ids = selectedRows.value.map(row => 
    row[props.config.table.rowKey || 'id']
  );
  emit('export', { ...searchParams, ids });
};

// 刷新操作
const handleRefresh = () => {
  loadData();
  emit('refresh');
};

// 创建提交 - 从CrudDialog组件接收验证后的数据
const handleCreateSubmit = async (formData: any) => {
  emit('create', formData);
};

// 编辑提交 - 从CrudDialog组件接收验证后的数据
const handleUpdateSubmit = async (formData: any) => {
  emit('update', formData);
};

// 更新数据列表
const updateDataList = (data: any) => {
  const responseMap = props.config.data.responseMap || {};
  
  if (props.config.data.formatters?.responseFormatter) {
    const formattedData = props.config.data.formatters.responseFormatter(data);
    dataList.value = formattedData[responseMap.list || 'list'] || [];
    pagination.total = formattedData[responseMap.total || 'total'] || 0;
  } else {
    dataList.value = data[responseMap.list || 'list'] || [];
    pagination.total = data[responseMap.total || 'total'] || 0;
  }
};

// 关闭对话框
const closeDialogs = () => {
  dialogVisible.create = false;
  dialogVisible.update = false;
  dialogVisible.view = false;
  dialogVisible.import = false;
};

// 暴露方法给父组件
defineExpose({
  loadData,
  updateDataList,
  closeDialogs,
  handleCreate,
  handleRefresh
});

// 生命周期
onMounted(() => {
  loadData();
});
</script>

<style scoped>
.crud-template {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.crud-toolbar {
  margin-bottom: 16px;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

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

.crud-pagination {
  margin-top: 16px;
  display: flex;
  justify-content: flex-end;
}
</style>