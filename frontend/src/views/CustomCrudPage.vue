<template>
  <div class="custom-crud-container">
    <h2>自定义CRUD页面</h2>
    
    <CrudTemplate
      ref="crudRef"
      :config="crudConfig"
      @load="handleLoad"
      @create="handleCreate"
      @update="handleUpdate"
      @delete="handleDelete"
      @batch-delete="handleBatchDelete"
      @view="handleView"
      @refresh="handleRefresh"
      @error="handleError"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { ElMessage } from 'element-plus';
import CrudTemplate from '../components/CrudTemplate.vue';
import { createCrudService, generateDefaultCrudConfig } from '../services/crudService';
import { createInputField, createSelectField, createDateField } from '../services/formService';
import { formService } from '../services/formService';
import type { CrudConfig } from '../types/crud';
import type { TableColumn } from '../types/table';
import { FormFieldType } from '../types/form';

// CRUD组件引用
const crudRef = ref();

// 状态选项
const statusOptions = [
  { label: '启用', value: 1 },
  { label: '禁用', value: 0 },
  { label: '审核中', value: 2 },
  { label: '已删除', value: 3 }
];

// 表格列配置 - 六个字段（含更新和新增时间）
const tableColumns: TableColumn[] = [
  {
    title: 'ID',
    prop: 'id',
    width: 80,
    fixed: 'left'
  },
  {
    title: '姓名',
    prop: 'name',
    width: 180,
    sortable: true
  },
  {
    title: '状态',
    prop: 'status',
    width: 120,
    formatter: (row: any) => {
      const status = statusOptions.find(option => option.value === row.status);
      return status ? status.label : row.status;
    },
    component: {
      name: 'el-tag',
      props: {
        type: (row: any) => {
          switch (row.status) {
            case 1: return 'success';
            case 0: return 'danger';
            case 2: return 'warning';
            case 3: return 'info';
            default: return 'default';
          }
        }
      }
    }
  },
  {
    title: '描述',
    prop: 'description',
    minWidth: 200
  },
  {
    title: '创建时间',
    prop: 'createdAt',
    width: 180,
    sortable: true,
    formatter: (row: any) => {
      if (!row.createdAt) return '';
      const date = new Date(row.createdAt);
      return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')} ${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}:${String(date.getSeconds()).padStart(2, '0')}`;
    }
  },
  {
    title: '更新时间',
    prop: 'updatedAt',
    width: 180,
    sortable: true,
    formatter: (row: any) => {
      if (!row.updatedAt) return '';
      const date = new Date(row.updatedAt);
      return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')} ${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}:${String(date.getSeconds()).padStart(2, '0')}`;
    }
  }
];

// 生成CRUD配置
const crudConfig: CrudConfig = generateDefaultCrudConfig({
  entityName: '自定义数据',
  apiBaseUrl: '/api/custom-data',
  tableColumns,
  formFields: [
    createInputField('name', '姓名', {
      required: true,
      placeholder: '请输入姓名',
      maxlength: 50,
      rules: [{ required: true, message: '请输入姓名', trigger: 'blur' }]
    }),
    createSelectField('status', '状态', statusOptions, {
      required: true,
      rules: [{ required: true, message: '请选择状态', trigger: 'change' }]
    }),
    createInputField('description', '描述', {
      required: false,
      placeholder: '请输入描述信息',
      maxlength: 200,
      type: 'textarea',
      rows: 4
    }),
    {
      prop: 'createdAt',
      label: '创建时间',
      type: FormFieldType.DATETIME_PICKER,
      disabled: true,
      pickerType: 'datetime',
      format: 'YYYY-MM-DD HH:mm:ss',
      placeholder: '请选择创建时间'
    },
    {
      prop: 'updatedAt',
      label: '更新时间',
      type: FormFieldType.DATETIME_PICKER,
      disabled: true,
      pickerType: 'datetime',
      format: 'YYYY-MM-DD HH:mm:ss',
      placeholder: '请选择更新时间'
    }
  ],
  showActions: ['create', 'update', 'delete', 'view']
});

// 配置搜索表单 - 包含开始时间、结束时间、姓名、状态四个查询条件
crudConfig.table.showSearch = true;
crudConfig.table.searchFormConfig = {
  fields: [
    createInputField('name', '姓名', {
      placeholder: '请输入姓名关键词',
      clearable: true
    }),
    createSelectField('status', '状态', statusOptions, {
      placeholder: '请选择状态',
      clearable: true
    }),
    {
      prop: 'startTime',
      label: '开始时间',
      type: FormFieldType.DATETIME_PICKER,
      placeholder: '请选择开始时间',
      pickerType: 'datetime',
      format: 'YYYY-MM-DD HH:mm:ss'
    },
    {
      prop: 'endTime',
      label: '结束时间',
      type: FormFieldType.DATETIME_PICKER,
      placeholder: '请选择结束时间',
      pickerType: 'datetime',
      format: 'YYYY-MM-DD HH:mm:ss'
    }
  ]
};

// 创建CRUD服务实例
const customDataService = createCrudService(crudConfig);

// 模拟数据
const mockData = {
  data: {
    list: [
      {
        id: 1,
        name: '张三',
        status: 1,
        description: '这是张三的描述信息',
        createdAt: new Date('2024-01-01 10:00:00'),
        updatedAt: new Date('2024-01-02 15:30:00')
      },
      {
        id: 2,
        name: '李四',
        status: 0,
        description: '这是李四的描述信息',
        createdAt: new Date('2024-01-03 09:15:00'),
        updatedAt: new Date('2024-01-03 09:15:00')
      },
      {
        id: 3,
        name: '王五',
        status: 2,
        description: '这是王五的描述信息',
        createdAt: new Date('2024-01-04 14:20:00'),
        updatedAt: new Date('2024-01-05 11:45:00')
      },
      {
        id: 4,
        name: '赵六',
        status: 1,
        description: '这是赵六的描述信息',
        createdAt: new Date('2024-01-06 16:50:00'),
        updatedAt: new Date('2024-01-07 08:25:00')
      },
      {
        id: 5,
        name: '钱七',
        status: 3,
        description: '这是钱七的描述信息',
        createdAt: new Date('2024-01-08 13:10:00'),
        updatedAt: new Date('2024-01-09 17:40:00')
      }
    ],
    total: 5,
    pageNum: 1,
    pageSize: 10
  }
};

// 处理加载数据
const handleLoad = async (params: any) => {
  try {
    // 在实际项目中，这里应该调用API获取数据
    // const response = await customDataService.getList(params);
    // return response;
    
    // 使用模拟数据
    console.log('加载数据参数:', params);
    return mockData.data;
  } catch (error) {
    console.error('加载数据失败:', error);
    throw error;
  }
};

// 处理创建数据
const handleCreate = async (data: any) => {
  try {
    // 在实际项目中，这里应该调用API创建数据
    // const response = await customDataService.create(data);
    // return response;
    
    // 模拟创建数据
    console.log('创建数据:', data);
    const newItem = {
      ...data,
      id: Date.now(),
      createdAt: new Date(),
      updatedAt: new Date()
    };
    mockData.data.list.unshift(newItem);
    mockData.data.total += 1;
    ElMessage.success('创建成功');
    return newItem;
  } catch (error) {
    console.error('创建数据失败:', error);
    throw error;
  }
};

// 处理更新数据
const handleUpdate = async (data: any) => {
  try {
    // 在实际项目中，这里应该调用API更新数据
    // const response = await customDataService.update(data.id, data);
    // return response;
    
    // 模拟更新数据
    console.log('更新数据:', data);
    const index = mockData.data.list.findIndex(item => item.id === data.id);
    if (index !== -1) {
      mockData.data.list[index] = {
        ...mockData.data.list[index],
        ...data,
        updatedAt: new Date()
      };
      ElMessage.success('更新成功');
      return mockData.data.list[index];
    } else {
      throw new Error('数据不存在');
    }
  } catch (error) {
    console.error('更新数据失败:', error);
    throw error;
  }
};

// 处理删除数据
const handleDelete = async (id: any) => {
  try {
    // 在实际项目中，这里应该调用API删除数据
    // await customDataService.delete(id);
    
    // 模拟删除数据
    console.log('删除数据ID:', id);
    const index = mockData.data.list.findIndex(item => item.id === id);
    if (index !== -1) {
      mockData.data.list.splice(index, 1);
      mockData.data.total -= 1;
      ElMessage.success('删除成功');
    }
  } catch (error) {
    console.error('删除数据失败:', error);
    throw error;
  }
};

// 处理批量删除数据
const handleBatchDelete = async (ids: any[]) => {
  try {
    // 在实际项目中，这里应该调用API批量删除数据
    // await customDataService.batchDelete(ids);
    
    // 模拟批量删除数据
    console.log('批量删除数据ID:', ids);
    mockData.data.list = mockData.data.list.filter(item => !ids.includes(item.id));
    mockData.data.total -= ids.length;
    ElMessage.success(`成功删除 ${ids.length} 条数据`);
  } catch (error) {
    console.error('批量删除数据失败:', error);
    throw error;
  }
};

// 处理查看数据
const handleView = async (id: any) => {
  try {
    // 在实际项目中，这里应该调用API获取详情数据
    // const response = await customDataService.getDetail(id);
    // return response;
    
    // 模拟查看数据
    console.log('查看数据ID:', id);
    const item = mockData.data.list.find(item => item.id === id);
    if (item) {
      return item;
    } else {
      throw new Error('数据不存在');
    }
  } catch (error) {
    console.error('查看数据失败:', error);
    throw error;
  }
};

// 处理刷新数据
const handleRefresh = () => {
  console.log('刷新数据');
  // 刷新操作由CrudTemplate内部处理
};

// 处理错误
const handleError = (error: any) => {
  console.error('CRUD操作错误:', error);
  ElMessage.error('操作失败，请稍后重试');
};

// 组件挂载时初始化
onMounted(() => {
  console.log('自定义CRUD页面已挂载');
});
</script>

<style scoped>
.custom-crud-container {
  padding: 20px;
  background-color: #f5f7fa;
  min-height: 100vh;
}

h2 {
  margin-bottom: 20px;
  color: #303133;
  font-size: 24px;
  font-weight: 500;
}

:deep(.el-form--inline .el-form-item) {
  margin-right: 20px;
  margin-bottom: 20px;
}

:deep(.el-table) {
  margin-top: 20px;
  border-radius: 4px;
  overflow: hidden;
}

:deep(.el-table__header-wrapper th) {
  background-color: #fafafa;
  font-weight: 500;
}

:deep(.el-button--primary) {
  background-color: #409eff;
}

:deep(.el-dialog__header) {
  background-color: #fafafa;
  padding: 15px 20px;
  border-bottom: 1px solid #ebeef5;
}

:deep(.el-dialog__title) {
  font-size: 18px;
  font-weight: 500;
}

:deep(.el-dialog__body) {
  padding: 20px;
}

:deep(.el-form-item__label) {
  font-weight: 400;
}
</style>