<template>
  <div class="example-crud-container">
    <h2>CRUD模板示例 - 用户管理</h2>
    
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
import type { CrudConfig } from '../types/crud';
import type { TableColumn } from '../types/table';

// CRUD组件引用
const crudRef = ref();

// 用户状态选项
const statusOptions = [
  { label: '启用', value: 1 },
  { label: '禁用', value: 0 }
];

// 角色选项
const roleOptions = [
  { label: '管理员', value: 'admin' },
  { label: '普通用户', value: 'user' },
  { label: '访客', value: 'guest' }
];

// 表格列配置
const tableColumns: TableColumn[] = [
  {
    title: '用户ID',
    prop: 'id',
    width: 80,
    fixed: 'left'
  },
  {
    title: '用户名',
    prop: 'username',
    width: 180,
    sortable: true
  },
  {
    title: '邮箱',
    prop: 'email',
    width: 220
  },
  {
    title: '角色',
    prop: 'role',
    width: 120,
    formatter: (row: any) => {
      const role = roleOptions.find(option => option.value === row.role);
      return role ? role.label : row.role;
    },
    filterable: true,
    filters: roleOptions.map(option => ({
      text: option.label,
      value: option.value
    }))
  },
  {
    title: '状态',
    prop: 'status',
    width: 100,
    formatter: (row: any) => {
      const status = statusOptions.find(option => option.value === row.status);
      return status ? status.label : row.status;
    },
    component: {
      name: 'el-tag',
      props: {
        type: (row: any) => row.status === 1 ? 'success' : 'danger'
      }
    }
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
    title: '操作',
    width: 200,
    fixed: 'right',
    actions: [
      {
        name: '重置密码',
        type: 'primary',
        icon: 'Key',
        onClick: (row: any) => handleResetPassword(row)
      }
    ]
  }
];

// 生成CRUD配置
const crudConfig: CrudConfig = generateDefaultCrudConfig({
  entityName: '用户',
  apiBaseUrl: '/api/users',
  tableColumns,
  formFields: [
    createInputField('username', '用户名', {
      required: true,
      placeholder: '请输入用户名',
      maxlength: 50,
      rules: [{ required: true, message: '请输入用户名', trigger: 'blur' }]
    }),
    createInputField('email', '邮箱', {
      required: true,
      placeholder: '请输入邮箱',
      type: 'email',
      rules: [
        { required: true, message: '请输入邮箱', trigger: 'blur' },
        { type: 'email', message: '请输入正确的邮箱格式', trigger: 'blur' }
      ]
    }),
    createInputField('password', '密码', {
      // 创建时必填，编辑时可选
      required: false,
      placeholder: '请输入密码',
      type: 'password',
      showPassword: true,
      maxlength: 20,
      rules: [
        { required: false, message: '请输入密码', trigger: 'blur' },
        { min: 6, message: '密码长度至少为6位', trigger: 'blur' }
      ]
    }),
    createSelectField('role', '角色', roleOptions, {
      required: true,
      rules: [{ required: true, message: '请选择角色', trigger: 'change' }]
    }),
    createSelectField('status', '状态', statusOptions, {
      required: true,
      rules: [{ required: true, message: '请选择状态', trigger: 'change' }]
    }),
    createDateField('createdAt', '创建时间', {
      disabled: true
    })
  ],
  showActions: ['create', 'update', 'delete', 'view', 'import', 'export']
});

// 创建CRUD服务实例
const userService = createCrudService(crudConfig);

// 模拟数据
const mockData = {
  data: {
    list: [
      {
        id: 1,
        username: 'admin',
        email: 'admin@example.com',
        role: 'admin',
        status: 1,
        createdAt: new Date('2024-01-01 10:00:00')
      },
      {
        id: 2,
        username: 'user1',
        email: 'user1@example.com',
        role: 'user',
        status: 1,
        createdAt: new Date('2024-01-02 10:00:00')
      },
      {
        id: 3,
        username: 'user2',
        email: 'user2@example.com',
        role: 'user',
        status: 0,
        createdAt: new Date('2024-01-03 10:00:00')
      },
      {
        id: 4,
        username: 'guest1',
        email: 'guest1@example.com',
        role: 'guest',
        status: 1,
        createdAt: new Date('2024-01-04 10:00:00')
      }
    ],
    total: 4,
    pageNum: 1,
    pageSize: 10
  }
};

// 处理数据加载
const handleLoad = async (params: any) => {
  try {
    // 在实际项目中，这里会调用真实的API
    // const response = await userService.getList(params);
    
    // 使用模拟数据
    setTimeout(() => {
      crudRef.value.updateDataList(mockData);
    }, 500);
  } catch (error) {
    console.error('加载数据失败:', error);
    ElMessage.error('加载数据失败');
  }
};

// 处理创建
const handleCreate = async (data: any) => {
  try {
    // 在实际项目中，这里会调用真实的API
    // await userService.create(data);
    
    // 模拟创建成功
    ElMessage.success('创建成功');
    crudRef.value.closeDialogs();
    crudRef.value.loadData();
  } catch (error) {
    console.error('创建失败:', error);
  }
};

// 处理更新
const handleUpdate = async (data: any) => {
  try {
    // 在实际项目中，这里会调用真实的API
    // await userService.update(data.id, data);
    
    // 模拟更新成功
    ElMessage.success('更新成功');
    crudRef.value.closeDialogs();
    crudRef.value.loadData();
  } catch (error) {
    console.error('更新失败:', error);
  }
};

// 处理删除
const handleDelete = async (id: number) => {
  try {
    // 在实际项目中，这里会调用真实的API
    // await userService.delete(id);
    
    // 模拟删除成功
    ElMessage.success('删除成功');
    crudRef.value.loadData();
  } catch (error) {
    console.error('删除失败:', error);
  }
};

// 处理批量删除
const handleBatchDelete = async (ids: number[]) => {
  try {
    // 在实际项目中，这里会调用真实的API
    // await userService.batchDelete(ids);
    
    // 模拟批量删除成功
    ElMessage.success(`成功删除${ids.length}条记录`);
    crudRef.value.loadData();
  } catch (error) {
    console.error('批量删除失败:', error);
  }
};

// 处理查看
const handleView = (data: any) => {
  console.log('查看数据:', data);
};

// 处理刷新
const handleRefresh = () => {
  console.log('刷新数据');
};

// 处理错误
const handleError = (error: any) => {
  console.error('CRUD操作错误:', error);
  ElMessage.error('操作失败');
};

// 处理重置密码
const handleResetPassword = (row: any) => {
  ElMessage.success(`重置用户 ${row.username} 的密码`);
};

// 生命周期
onMounted(() => {
  console.log('CRUD示例页面加载完成');
});
</script>

<style scoped>
.example-crud-container {
  padding: 20px;
  background-color: #fff;
  height: 100%;
  overflow-y: auto;
}

h2 {
  margin-bottom: 20px;
  font-size: 20px;
  font-weight: 600;
  color: #333;
}
</style>