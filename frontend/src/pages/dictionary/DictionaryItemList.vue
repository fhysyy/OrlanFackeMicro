<template>
  <div class="dictionary-item-list">
    <div class="page-header">
      <h2>字典项管理</h2>
      <button class="btn-primary" @click="handleAdd">新增字典项</button>
    </div>

    <div class="search-bar">
      <el-select
        v-model="searchForm.dictionaryTypeId"
        placeholder="选择字典类型"
        style="width: 200px; margin-right: 10px"
        @change="handleTypeChange"
      >
        <el-option
          v-for="type in dictionaryTypes"
          :key="type.id"
          :label="type.name"
          :value="type.id"
        />
      </el-select>
      <el-input
        v-model="searchForm.keyword"
        placeholder="输入值或文本搜索"
        style="width: 300px; margin-right: 10px"
        @keyup.enter="handleSearch"
      />
      <el-select
        v-model="searchForm.isEnabled"
        placeholder="状态"
        style="width: 120px; margin-right: 10px"
      >
        <el-option label="全部" :value="undefined" />
        <el-option label="启用" :value="true" />
        <el-option label="禁用" :value="false" />
      </el-select>
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
      <el-table-column prop="dictionaryTypeName" label="字典类型" min-width="150" />
      <el-table-column prop="value" label="值" min-width="150" />
      <el-table-column prop="text" label="文本" min-width="150" />
      <el-table-column prop="description" label="描述" min-width="200" />
      <el-table-column prop="isEnabled" label="状态" width="100">
        <template #default="scope">
          <el-tag :type="scope.row.isEnabled ? 'success' : 'danger'">
            {{ scope.row.isEnabled ? '启用' : '禁用' }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="sortOrder" label="排序" width="80" />
      <el-table-column prop="extraData" label="扩展数据" min-width="150" />
      <el-table-column prop="createdAt" label="创建时间" width="180" />
      <el-table-column label="操作" width="140" fixed="right">
        <template #default="scope">
          <el-button size="small" @click="handleEdit(scope.row)">编辑</el-button>
          <el-button size="small" type="danger" @click="handleDelete(scope.row.id)">删除</el-button>
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
        <el-form-item label="字典类型" prop="dictionaryTypeId">
          <el-select v-model="formData.dictionaryTypeId" placeholder="请选择字典类型">
            <el-option
              v-for="type in dictionaryTypes"
              :key="type.id"
              :label="type.name"
              :value="type.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="值" prop="value">
          <el-input v-model="formData.value" placeholder="请输入字典项值" />
        </el-form-item>
        <el-form-item label="文本" prop="text">
          <el-input v-model="formData.text" placeholder="请输入字典项文本" />
        </el-form-item>
        <el-form-item label="描述" prop="description">
          <el-input
            v-model="formData.description"
            placeholder="请输入字典项描述"
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
        <el-form-item label="扩展数据" prop="extraData">
          <el-input
            v-model="formData.extraData"
            placeholder="请输入扩展数据（JSON格式）"
            type="textarea"
            rows="2"
          />
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
      <p>确定要删除选中的 {{ selectedRows.length }} 条字典项吗？</p>
      <template #footer>
        <el-button @click="batchDeleteDialogVisible = false">取消</el-button>
        <el-button type="danger" @click="handleBatchDelete">确定删除</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, computed } from 'vue';
import { ElMessage, ElMessageBox, FormInstance } from 'element-plus';
import { 
  dictionaryTypeService, 
  dictionaryItemService 
} from '@/services/dictionaryService';
import type {
  DictionaryItem,
  DictionaryItemCreateRequest,
  DictionaryItemUpdateRequest,
  DictionaryType
} from '@/types/dictionary';
import type { PaginationParams } from '@/types/api';

// 响应式数据
const loading = ref(false);
const dialogVisible = ref(false);
const batchDeleteDialogVisible = ref(false);
const dialogTitle = ref('');
const formRef = ref<FormInstance>();
const tableData = ref<(DictionaryItem & { dictionaryTypeName?: string })[]>([]);
const selectedRows = ref<DictionaryItem[]>([]);
const dictionaryTypes = ref<DictionaryType[]>([]);

// 搜索表单
const searchForm = reactive({
  dictionaryTypeId: '',
  keyword: '',
  isEnabled: undefined as boolean | undefined
});

// 分页参数
const pagination = reactive({
  currentPage: 1,
  pageSize: 10,
  total: 0
});

// 表单数据
const formData = reactive<DictionaryItemCreateRequest & DictionaryItemUpdateRequest>({
  dictionaryTypeId: '',
  value: '',
  text: '',
  description: '',
  isEnabled: true,
  sortOrder: 0,
  extraData: ''
});

// 表单规则
const formRules = reactive({
  dictionaryTypeId: [
    { required: true, message: '请选择字典类型', trigger: 'change' }
  ],
  value: [
    { required: true, message: '请输入字典项值', trigger: 'blur' },
    { 
      validator: async (rule: any, value: string, callback: any) => {
        if (value && formData.dictionaryTypeId) {
          const exists = await dictionaryItemService.checkValueExists(
            formData.dictionaryTypeId, 
            value, 
            editingId.value
          );
          if (exists) {
            callback(new Error('值已存在'));
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
  text: [
    { required: true, message: '请输入字典项文本', trigger: 'blur' }
  ],
  extraData: [
    { 
      validator: (rule: any, value: string, callback: any) => {
        if (value) {
          try {
            JSON.parse(value);
            callback();
          } catch {
            callback(new Error('请输入有效的JSON格式'));
          }
        } else {
          callback();
        }
      },
      trigger: 'blur'
    }
  ]
});

const editingId = ref<string>('');

// 加载字典类型
const loadDictionaryTypes = async () => {
  try {
    const response = await dictionaryTypeService.getAllEnabled();
    dictionaryTypes.value = response.data;
  } catch (error) {
    ElMessage.error('获取字典类型失败');
  }
};

// 加载数据
const loadData = async () => {
  loading.value = true;
  try {
    const params: PaginationParams & { 
      dictionaryTypeId?: string; 
      keyword?: string;
      isEnabled?: boolean;
    } = {
      page: pagination.currentPage,
      pageSize: pagination.pageSize,
      dictionaryTypeId: searchForm.dictionaryTypeId,
      keyword: searchForm.keyword,
      isEnabled: searchForm.isEnabled
    };
    const response = await dictionaryItemService.getList(params);
    
    // 丰富字典类型名称
    const enrichedData = response.data.list.map(item => {
      const type = dictionaryTypes.value.find(t => t.id === item.dictionaryTypeId);
      return {
        ...item,
        dictionaryTypeName: type?.name || ''
      };
    });
    
    tableData.value = enrichedData;
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
  searchForm.dictionaryTypeId = '';
  searchForm.keyword = '';
  searchForm.isEnabled = undefined;
  pagination.currentPage = 1;
  loadData();
};

// 字典类型变化
const handleTypeChange = () => {
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
const handleSelectionChange = (rows: DictionaryItem[]) => {
  selectedRows.value = rows;
};

// 新增
const handleAdd = () => {
  editingId.value = '';
  dialogTitle.value = '新增字典项';
  resetForm();
  // 如果搜索框中已选择字典类型，则默认选择该类型
  if (searchForm.dictionaryTypeId) {
    formData.dictionaryTypeId = searchForm.dictionaryTypeId;
  }
  dialogVisible.value = true;
};

// 编辑
const handleEdit = async (row: DictionaryItem) => {
  editingId.value = row.id;
  dialogTitle.value = '编辑字典项';
  try {
    const response = await dictionaryItemService.getDetail(row.id);
    const data = response.data;
    formData.dictionaryTypeId = data.dictionaryTypeId;
    formData.value = data.value;
    formData.text = data.text;
    formData.description = data.description;
    formData.isEnabled = data.isEnabled;
    formData.sortOrder = data.sortOrder;
    formData.extraData = data.extraData || '';
    dialogVisible.value = true;
  } catch (error) {
    ElMessage.error('获取详情失败');
  }
};

// 删除
const handleDelete = async (id: string) => {
  try {
    await ElMessageBox.confirm('确定要删除该字典项吗？', '删除确认', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    });
    await dictionaryItemService.delete(id);
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
    ElMessage.warning('请选择要删除的字典项');
    return;
  }
  try {
    const ids = selectedRows.value.map(row => row.id);
    await dictionaryItemService.batchDelete(ids);
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
      await dictionaryItemService.update(editingId.value, formData);
      ElMessage.success('更新成功');
    } else {
      // 新增
      await dictionaryItemService.create(formData);
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
  formData.dictionaryTypeId = '';
  formData.value = '';
  formData.text = '';
  formData.description = '';
  formData.isEnabled = true;
  formData.sortOrder = 0;
  formData.extraData = '';
};

// 初始加载
onMounted(async () => {
  await loadDictionaryTypes();
  loadData();
});
</script>

<style scoped>
.dictionary-item-list {
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