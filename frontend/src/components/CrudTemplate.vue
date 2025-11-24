<template>
  <div class="crud-template">
    <!-- 工具栏 -->
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

    <!-- 搜索表单 -->
    <el-form
      v-if="config.table.showSearch && searchFormConfig"
      :model="searchParams"
      :inline="true"
      :size="'small'"
      class="crud-search"
    >
      <FormGenerator
        :config="searchFormConfig"
        :model="searchParams"
      />
      <el-form-item>
        <el-button type="primary" @click="handleSearch">搜索</el-button>
        <el-button @click="handleReset">重置</el-button>
      </el-form-item>
    </el-form>

    <!-- 表格 -->
    <el-table
      ref="tableRef"
      :data="dataList"
      :loading="loading"
      :stripe="true"
      :border="true"
      :row-key="config.table.rowKey || 'id'"
      @selection-change="handleSelectionChange"
      @sort-change="handleSortChange"
    >
      <!-- 选择列 -->
      <el-table-column
        v-if="config.table.showSelection"
        type="selection"
        width="55"
        :selectable="config.table.selectable"
      />

      <!-- 序号列 -->
      <el-table-column
        v-if="config.table.showIndex"
        type="index"
        width="50"
        label="序号"
      />

      <!-- 自定义列 -->
      <template v-for="column in tableColumns" :key="column.key || column.prop">
        <table-column-renderer
          :column="column"
          @cell-action="handleCellAction"
        />
      </template>

      <!-- 操作列 -->
      <el-table-column
        v-if="config.table.showActions"
        label="操作"
        width="config.table.actionWidth || 200"
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

    <!-- 分页 -->
    <div class="crud-pagination" v-if="config.table.showPagination">
      <el-pagination
        v-model:current-page="pagination.currentPage"
        v-model:page-size="pagination.pageSize"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        :total="pagination.total"
        @size-change="handleSizeChange"
        @current-change="handleCurrentChange"
      />
    </div>

    <!-- 创建对话框 -->
    <el-dialog
      v-model="dialogVisible.create"
      :title="`创建${config.entityName}`"
      :width="config.form.dialogWidth || '50%'"
      :fullscreen="config.form.fullscreen"
    >
      <template #default>
        <el-form
          ref="formRef.create"
          :model="currentData"
          :rules="createFormConfig?.rules"
          :label-width="'100px'"
        >
          <FormGenerator
            v-if="createFormConfig"
            :config="createFormConfig"
            :model="currentData"
            :form-ref="formRef.create"
          />
        </el-form>
      </template>
      <template #footer>
        <el-button @click="dialogVisible.create = false">取消</el-button>
        <el-button type="primary" @click="handleCreateSubmit">确定</el-button>
      </template>
    </el-dialog>

    <!-- 编辑对话框 -->
    <el-dialog
      v-model="dialogVisible.update"
      :title="`编辑${config.entityName}`"
      :width="config.form.dialogWidth || '50%'"
      :fullscreen="config.form.fullscreen"
    >
      <template #default>
        <el-form
          ref="formRef.update"
          :model="currentData"
          :rules="updateFormConfig?.rules"
          :label-width="'100px'"
        >
          <FormGenerator
            v-if="updateFormConfig"
            :config="updateFormConfig"
            :model="currentData"
            :form-ref="formRef.update"
          />
        </el-form>
      </template>
      <template #footer>
        <el-button @click="dialogVisible.update = false">取消</el-button>
        <el-button type="primary" @click="handleUpdateSubmit">确定</el-button>
      </template>
    </el-dialog>

    <!-- 查看对话框 -->
    <el-dialog
      v-model="dialogVisible.view"
      :title="`查看${config.entityName}`"
      :width="config.form.dialogWidth || '50%'"
    >
      <template #default>
        <el-form
          :model="currentData"
          :label-width="'100px'"
          :disabled="true"
        >
          <FormGenerator
            v-if="viewFormConfig"
            :config="viewFormConfig"
            :model="currentData"
            :disabled="true"
          />
        </el-form>
      </template>
      <template #footer>
        <el-button @click="dialogVisible.view = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, onUnmounted } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import FormGenerator from './FormGenerator.vue';
import TableColumnRenderer from './TableColumnRenderer.vue';
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

// 表单引用
const formRef = reactive({
  create: ref(),
  update: ref()
});

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
const createFormConfig = computed(() => props.config.form.createFormConfig);
const updateFormConfig = computed(() => props.config.form.updateFormConfig);
const viewFormConfig = computed(() => props.config.form.viewFormConfig);
const tableColumns = computed(() => props.config.table.columns);
const toolbarActions = computed(() => props.config.actions.toolbar);
const rowActions = computed(() => props.config.actions.tableRow);
const batchActions = computed(() => props.config.actions.batchActions);

// 初始化默认搜索参数
const initDefaultSearchParams = () => {
  if (searchFormConfig.value) {
    searchFormConfig.value.fields.forEach(field => {
      if (field.defaultValue !== undefined) {
        searchParams[field.name] = field.defaultValue;
      }
    });
  }
};

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

// 创建提交
const handleCreateSubmit = async () => {
  if (formRef.create.value) {
    try {
      await formRef.create.value.validate();
      emit('create', { ...currentData });
    } catch (error) {
      return;
    }
  } else {
    emit('create', { ...currentData });
  }
};

// 编辑提交
const handleUpdateSubmit = async () => {
  if (formRef.update.value) {
    try {
      await formRef.update.value.validate();
      emit('update', { ...currentData });
    } catch (error) {
      return;
    }
  } else {
    emit('update', { ...currentData });
  }
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
  initDefaultSearchParams();
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
}

.crud-pagination {
  margin-top: 16px;
  display: flex;
  justify-content: flex-end;
}
</style>