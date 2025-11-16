<template>
  <div class="dictionary-type-list">
    <div class="page-header">
      <h2>字典类型管理</h2>
      <button class="btn-primary" @click="handleAdd">新增字典类型</button>
    </div>

    <div class="search-bar">
      <el-input
        v-model="searchForm.keyword"
        placeholder="输入编码或名称搜索"
        style="width: 300px; margin-right: 10px"
        @keyup.enter="handleSearch"
      />
      <el-button type="primary" @click="handleSearch">搜索</el-button>
      <el-button @click="handleReset">重置</el-button>
    </div>

    <el-table
      v-loading="loading"
      :data="tableData"
      style="width: 100%"
      @selection-change="handleSelectionChange"
    >
      <el-table-column type="selection" width="55" />
      <el-table-column prop="code" label="编码" min-width="180" />
      <el-table-column prop="name" label="名称" min-width="180" />
      <el-table-column prop="description" label="描述" min-width="200" />
      <el-table-column prop="isEnabled" label="状态" width="100">
        <template #default="scope">
          <el-tag :type="scope.row.isEnabled ? 'success' : 'danger'">
            {{ scope.row.isEnabled ? '启用' : '禁用' }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="sortOrder" label="排序" width="80" />
      <el-table-column prop="createdAt" label="创建时间" width="180" />
      <el-table-column label="操作" width="180" fixed="right">
        <template #default="scope">
          <el-button size="small" @click="handleEdit(scope.row)">编辑</el-button>
          <el-button size="small" type="danger" @click="handleDelete(scope.row.id)">删除</el-button>
          <el-button size="small" @click="handleViewItems(scope.row)">字典项</el-button>
        </template>
      </el-table-column>
    </el-table>

    <div class="pagination-container">
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

    <!-- 新增/编辑对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="dialogTitle"
      width="500px"
      @close="handleDialogClose"
    >
      <el-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        label-width="100px"
      >
        <el-form-item label="编码" prop="code">
          <el-input v-model="formData.code" placeholder="请输入字典类型编码" />
        </el-form-item>
        <el-form-item label="名称" prop="name">
          <el-input v-model="formData.name" placeholder="请输入字典类型名称" />
        </el-form-item>
        <el-form-item label="描述" prop="description">
          <el-input
            v-model="formData.description"
            placeholder="请输入字典类型描述"
            type="textarea"
            rows="3"
          />
        </el-form-item>
        <el-form-item label="状态" prop="isEnabled">
          <el-switch v-model="formData.isEnabled" />
        </el-form-item>
        <el-form-item label="排序" prop="sortOrder">
          <el-input-number v-model="formData.sortOrder" :min="0" :step="1" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="handleDialogClose">取消</el-button>
        <el-button type="primary" @click="handleSubmit">确定</el-button>
      </template>
    </el-dialog>

    <!-- 批量删除确认对话框 -->
    <el-dialog
      v-model="batchDeleteDialogVisible"
      title="批量删除确认"
      width="400px"
    >
      <p>确定要删除选中的 {{ selectedRows.length }} 条字典类型吗？</p>
      <template #footer>
        <el-button @click="batchDeleteDialogVisible = false">取消</el-button>
        <el-button type="danger" @click="handleBatchDelete">确定删除</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import { ElMessage, ElMessageBox, FormInstance } from 'element-plus';
import { 
  dictionaryTypeService 
} from '@/services/dictionaryService';
import type {
  DictionaryType,
  DictionaryTypeCreateRequest,
  DictionaryTypeUpdateRequest
} from '@/types/dictionary';
import type { PaginationParams } from '@/types/api';

// 响应式数据
const loading = ref(false);
const dialogVisible = ref(false);
const batchDeleteDialogVisible = ref(false);
const dialogTitle = ref('');
const formRef = ref<FormInstance>();
const tableData = ref<DictionaryType[]>([]);
const selectedRows = ref<DictionaryType[]>([]);

// 搜索表单
const searchForm = reactive({
  keyword: ''
});

// 分页参数
const pagination = reactive({
  currentPage: 1,
  pageSize: 10,
  total: 0
});

// 表单数据
const formData = reactive<DictionaryTypeCreateRequest & DictionaryTypeUpdateRequest>({
  code: '',
  name: '',
  description: '',
  isEnabled: true,
  sortOrder: 0
});

// 表单规则
const formRules = reactive({
  code: [
    { required: true, message: '请输入字典类型编码', trigger: 'blur' },
    { 
      validator: async (rule: any, value: string, callback: any) => {
        if (value && editingId.value) {
          const exists = await dictionaryTypeService.checkCodeExists(value, editingId.value);
          if (exists) {
            callback(new Error('编码已存在'));
          } else {
            callback();
          }
        } else if (value) {
          const exists = await dictionaryTypeService.checkCodeExists(value);
          if (exists) {
            callback(new Error('编码已存在'));
          } else {
            callback();
          }
        } else {
          callback();
        }
      },
      trigger: 'blur'
    }
  ],
  name: [
    { required: true, message: '请输入字典类型名称', trigger: 'blur' }
  ]
});

const editingId = ref<string>('');

// 加载数据
const loadData = async () => {
  loading.value = true;
  try {
    const params: PaginationParams & { keyword?: string } = {
      page: pagination.currentPage,
      pageSize: pagination.pageSize,
      keyword: searchForm.keyword
    };
    const response = await dictionaryTypeService.getList(params);
    tableData.value = response.data.list;
    pagination.total = response.data.total;
  } catch (error) {
    ElMessage.error('获取数据失败');
  } finally {
    loading.value = false;
  }
};

// 搜索
const handleSearch = () => {
  pagination.currentPage = 1;
  loadData();
};

// 重置
const handleReset = () => {
  searchForm.keyword = '';
  pagination.currentPage = 1;
  loadData();
};

// 分页大小变化
const handleSizeChange = (size: number) => {
  pagination.pageSize = size;
  loadData();
};

// 页码变化
const handleCurrentChange = (current: number) => {
  pagination.currentPage = current;
  loadData();
};

// 选择变化
const handleSelectionChange = (rows: DictionaryType[]) => {
  selectedRows.value = rows;
};

// 新增
const handleAdd = () => {
  editingId.value = '';
  dialogTitle.value = '新增字典类型';
  resetForm();
  dialogVisible.value = true;
};

// 编辑
const handleEdit = async (row: DictionaryType) => {
  editingId.value = row.id;
  dialogTitle.value = '编辑字典类型';
  try {
    const response = await dictionaryTypeService.getDetail(row.id);
    const data = response.data;
    formData.code = data.code;
    formData.name = data.name;
    formData.description = data.description;
    formData.isEnabled = data.isEnabled;
    formData.sortOrder = data.sortOrder;
    dialogVisible.value = true;
  } catch (error) {
    ElMessage.error('获取详情失败');
  }
};

// 查看字典项
const handleViewItems = (row: DictionaryType) => {
  // 使用路由跳转或其他方式切换到字典项管理页面
  // 这里简单实现为提示信息
  ElMessage.info(`请在字典项管理中查看类型: ${row.name}`);
};

// 删除
const handleDelete = async (id: string) => {
  try {
    await ElMessageBox.confirm('确定要删除该字典类型吗？', '删除确认', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    });
    await dictionaryTypeService.delete(id);
    ElMessage.success('删除成功');
    loadData();
  } catch (error) {
    // 用户取消不显示错误
    if (error !== 'cancel') {
      ElMessage.error('删除失败');
    }
  }
};

// 批量删除
const handleBatchDelete = async () => {
  if (selectedRows.value.length === 0) {
    ElMessage.warning('请选择要删除的字典类型');
    return;
  }
  try {
    const ids = selectedRows.value.map(row => row.id);
    await dictionaryTypeService.batchDelete(ids);
    ElMessage.success('批量删除成功');
    batchDeleteDialogVisible.value = false;
    selectedRows.value = [];
    loadData();
  } catch (error) {
    ElMessage.error('批量删除失败');
  }
};

// 表单提交
const handleSubmit = async () => {
  if (!formRef.value) return;
  try {
    await formRef.value.validate();
    if (editingId.value) {
      // 更新
      await dictionaryTypeService.update(editingId.value, formData);
      ElMessage.success('更新成功');
    } else {
      // 新增
      await dictionaryTypeService.create(formData);
      ElMessage.success('创建成功');
    }
    dialogVisible.value = false;
    loadData();
  } catch (error) {
    // 验证失败不显示错误
    if (error !== false) {
      ElMessage.error(editingId.value ? '更新失败' : '创建失败');
    }
  }
};

// 关闭对话框
const handleDialogClose = () => {
  dialogVisible.value = false;
  resetForm();
};

// 重置表单
const resetForm = () => {
  if (formRef.value) {
    formRef.value.resetFields();
  }
  formData.code = '';
  formData.name = '';
  formData.description = '';
  formData.isEnabled = true;
  formData.sortOrder = 0;
};

// 初始加载
onMounted(() => {
  loadData();
});
</script>

<style scoped>
.dictionary-type-list {
  padding: 20px;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.page-header h2 {
  margin: 0;
}

.search-bar {
  margin-bottom: 20px;
}

.pagination-container {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}
</style>