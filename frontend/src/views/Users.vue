<template>
  <div class="users-container">
    <div class="page-header">
      <h2>用户管理</h2>
      <el-button type="primary" @click="handleCreate">新增用户</el-button>
    </div>

    <!-- 搜索和筛选 -->
    <el-card class="search-card">
      <el-form :model="searchForm" inline>
        <el-form-item label="用户名">
          <el-input v-model="searchForm.username" placeholder="请输入用户名" clearable />
        </el-form-item>
        <el-form-item label="邮箱">
          <el-input v-model="searchForm.email" placeholder="请输入邮箱" clearable />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="searchForm.status" placeholder="请选择状态" clearable>
            <el-option label="待激活" value="Pending" />
            <el-option label="活跃" value="Active" />
            <el-option label="禁用" value="Disabled" />
            <el-option label="锁定" value="Locked" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleSearch">搜索</el-button>
          <el-button @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <!-- 用户列表 -->
    <el-card>
      <!-- 骨架屏 -->
      <div v-if="loading" class="skeleton-container">
        <div v-for="i in 6" :key="i" class="skeleton-user-item">
          <div class="skeleton-user-info">
            <EasySkeleton type="line" width="120px" height="20px" />
            <EasySkeleton type="line" width="180px" height="16px" />
            <EasySkeleton type="line" width="100px" height="14px" />
          </div>
          <div class="skeleton-user-actions">
            <EasySkeleton type="rect" rectWidth="60px" rectHeight="32px" />
            <EasySkeleton type="rect" rectWidth="60px" rectHeight="32px" />
          </div>
        </div>
      </div>

      <!-- 实际表格 -->
      <virtual-scroll-table
      v-else
      v-model:data="users"
      :columns="tableColumns"
      :row-height="50"
      height="500px"
    >
      <template #roles="{ row }">
        <div class="role-tags">
          <el-tag 
            v-for="role in ((row as any).roles || [row.role])" 
            :key="role"
            :type="getRoleType(role)"
            size="small"
            effect="plain"
            class="role-tag"
          >
            {{ getRoleLabel(role) }}
          </el-tag>
        </div>
      </template>
      <template #status="{ row }">
        <el-tag :type="getStatusType(row.status)">{{ row.status }}</el-tag>
      </template>
      <template #createdAt="{ row }">
        {{ formatDate(row.createdAt) }}
      </template>
      <template #actions="{ row }">
        <el-button size="small" @click="handleEdit(row)">编辑</el-button>
        <el-button size="small" type="danger" @click="handleDelete(row)">删除</el-button>
      </template>
    </virtual-scroll-table>

      <!-- 分页 -->
      <div class="pagination-container">
        <el-pagination
          v-model:current-page="pagination.current"
          v-model:page-size="pagination.size"
          :total="pagination.total"
          :page-sizes="[10, 20, 50, 100]"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="handleSizeChange"
          @current-change="handleCurrentChange"
        />
      </div>
    </el-card>

    <!-- 用户编辑对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="dialogTitle"
      width="500px"
      @close="handleDialogClose"
    >
      <el-form ref="userFormRef" :model="userForm" :rules="userRules" label-width="80px">
        <el-form-item label="用户名" prop="username">
          <el-input v-model="userForm.username" />
        </el-form-item>
        <el-form-item label="邮箱" prop="email">
          <el-input v-model="userForm.email" />
        </el-form-item>
        <el-form-item label="手机号" prop="phone">
          <el-input v-model="userForm.phone" />
        </el-form-item>
        <el-form-item label="角色" prop="roles">
        <el-select v-model="userForm.roles" placeholder="请选择角色" multiple collapse-tags style="width: 100%">
          <el-option
            v-for="role in availableRoles"
            :key="role.value"
            :label="role.label"
            :value="role.value"
          />
        </el-select>
        <div class="form-tip">提示：可选择多个角色，用户将拥有所有选定角色的权限</div>
      </el-form-item>
        <el-form-item label="状态" prop="status">
          <el-select v-model="userForm.status" placeholder="请选择状态">
            <el-option label="待激活" value="Pending" />
            <el-option label="活跃" value="Active" />
            <el-option label="禁用" value="Disabled" />
            <el-option label="锁定" value="Locked" />
          </el-select>
        </el-form-item>
      </el-form>
      
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="dialogLoading" @click="handleSubmit">
          确定
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, computed } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { useAuthStore } from '@/stores/auth'
import type { Role } from '@/types/api'
import { User, UserRole, UserStatus } from '@/types/api'
import { userService, roleService, permissionService } from '@/services'
import { useRouter } from 'vue-router'
import { api } from '@/services/api'
import VirtualScrollTable from '@/components/VirtualScrollTable.vue'
import EasySkeleton from '@/components/EasySkeleton.vue'

const authStore = useAuthStore()

const loading = ref(false)
const dialogVisible = ref(false)
const dialogLoading = ref(false)
const isEdit = ref(false)
const userFormRef = ref<FormInstance>()

const searchForm = reactive({
  username: '',
  email: '',
  status: '' as UserStatus | ''
})

const userForm = reactive({
  id: '',
  username: '',
  email: '',
  password: '',
  confirmPassword: '',
  status: UserStatus.Pending,
  role: UserRole.User,
  roles: [] as string[], // 支持多角色
  phone: ''
})

// 角色列表（用于选择）
const availableRoles = ref([
  { value: UserRole.User, label: '普通用户' },
  { value: UserRole.Admin, label: '管理员' },
  { value: UserRole.SystemAdmin, label: '系统管理员' }
])

// 获取所有角色
const fetchRoles = async () => {
  try {
    const response = await roleService.getRoles({ page: 1, size: 100 })
    
    if (response.data && response.data.items) {
      availableRoles.value = response.data.items.map((role: Role) => ({
        value: role.id,
        label: role.name
      }))
    }
  } catch (error) {
    console.error('获取角色列表失败:', error)
    ElMessage.warning('获取角色列表失败，显示默认角色')
  }
}

const pagination = reactive({
  current: 1,
  size: 10,
  total: 0
})

const userList = ref<User[]>([])

const dialogTitle = computed(() => isEdit.value ? '编辑用户' : '新增用户')

const userRules: FormRules = {
  username: [
    { required: true, message: '请输入用户名', trigger: 'blur' },
    { min: 3, max: 20, message: '用户名长度在3-20个字符', trigger: 'blur' },
    { pattern: /^[a-zA-Z0-9_]+$/, message: '用户名只能包含字母、数字和下划线', trigger: 'blur' }
  ],
  email: [
    { required: true, message: '请输入邮箱', trigger: 'blur' },
    { type: 'email', message: '请输入正确的邮箱格式', trigger: 'blur' }
  ],
  password: [
    { required: !isEdit.value, message: '请输入密码', trigger: 'blur' },
    { min: 6, max: 20, message: '密码长度在6-20个字符', trigger: 'blur' }
  ],
  confirmPassword: [
    { required: () => !isEdit.value, message: '请确认密码', trigger: 'blur' },
    { 
      validator: (rule, value, callback) => {
        if (value !== userForm.password) {
          callback(new Error('两次输入的密码不一致'))
        } else {
          callback()
        }
      }, 
      trigger: 'blur' 
    }
  ],
  roles: [
    { required: true, message: '请至少选择一个角色', trigger: 'change' },
    {
      validator: (rule, value, callback) => {
        if (!Array.isArray(value) || value.length === 0) {
          callback(new Error('请至少选择一个角色'))
        } else {
          callback()
        }
      },
      trigger: 'change'
    }
  ],
  status: [
    { required: true, message: '请选择状态', trigger: 'change' }
  ]
}

// 获取角色标签类型
const getRoleType = (role: UserRole) => {
  const types = {
    [UserRole.User]: '',
    [UserRole.Admin]: 'warning',
    [UserRole.SystemAdmin]: 'danger'
  }
  return types[role] || ''
}

// 获取角色标签文本
const getRoleLabel = (role: UserRole) => {
  const labels = {
    [UserRole.User]: '普通用户',
    [UserRole.Admin]: '管理员',
    [UserRole.SystemAdmin]: '系统管理员'
  }
  return labels[role] || role
}

// 获取状态标签类型
const getStatusType = (status: UserStatus) => {
  const types = {
    [UserStatus.Pending]: 'info',
    [UserStatus.Active]: 'success',
    [UserStatus.Disabled]: 'warning',
    [UserStatus.Locked]: 'danger'
  }
  return types[status] || ''
}

// 格式化日期
const formatDate = (date: string) => {
  return new Date(date).toLocaleString('zh-CN')
}

// 获取用户列表
  const fetchUsers = async () => {
    loading.value = true
    try {
      const response = await api.get('/api/admin/users', {
        params: {
          username: searchForm.username,
          email: searchForm.email,
          status: searchForm.status
        }
      })
      if (response.data && response.data.success) {
        userList.value = response.data.users
        pagination.total = response.data.users.length
      }
    } 
    catch (error) 
    {
      console.error('获取用户列表失败:', error)
      ElMessage.error('获取用户列表失败')
      
      // 显示模拟数据作为备选
      pagination.total = userList.value.length
    } finally {
      loading.value = false
    }
  }

// 搜索
const handleSearch = () => {
  pagination.current = 1
  // 调用获取用户列表方法，将搜索参数通过searchForm传递给fetchUsers
  fetchUsers()
}

// 重置搜索
const handleReset = () => {
  Object.assign(searchForm, {
    username: '',
    email: '',
    status: ''
  })
  pagination.current = 1
  // 重置后重新获取数据
  fetchUsers()
}

// 分页大小改变
const handleSizeChange = (size: number) => {
  pagination.size = size
  pagination.current = 1
  fetchUsers()
}

// 当前页改变
const handleCurrentChange = (current: number) => {
  pagination.current = current
  fetchUsers()
}

// 创建用户
const handleCreate = () => {
  isEdit.value = false
  Object.assign(userForm, {
    id: '',
    username: '',
    email: '',
    password: '',
    confirmPassword: '',
    status: UserStatus.Pending,
    role: UserRole.User,
    roles: [UserRole.User], // 默认普通用户角色
    phone: ''
  })
  dialogVisible.value = true
}

// 编辑用户
const handleEdit = (user: User) => {
  isEdit.value = true
  // 直接使用表格中的数据填充表单
  Object.assign(userForm, {
    ...user,
    roles: (user as any).roles || [user.role], // 如果没有roles属性，使用role作为回退
    phone: user.phone || ''
  })
  dialogVisible.value = true
}

// 删除用户
const handleDelete = async (user: User) => {
  try {
    await ElMessageBox.confirm(`确定要删除用户 "${user.username}" 吗？`, '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    
    // 调用删除用户API
    const res = await api.delete(`/api/admin/users/${user.id}`)
    if (res.data.success) {
      ElMessage.success('删除成功')
      fetchUsers()
    } else {
      ElMessage.error(res.data.message || '删除失败')
    }
  } catch (error: any) {
    if (error !== 'cancel' && !error.response) {
      console.error('删除失败:', error)
      ElMessage.error('删除失败，请稍后重试')
    }
    // 用户取消删除不做处理
  }
}

// 提交表单
const handleSubmit = async () => {
  if (!userFormRef.value) return

  try {
    await userFormRef.value.validate()
    dialogLoading.value = true
    
    const userData = {
      username: userForm.username,
      email: userForm.email,
      phone: userForm.phone,
      status: userForm.status,
      roles: userForm.roles,
      role: userForm.roles.length > 0 ? userForm.roles[0] : UserRole.User // 保留role字段以兼容
    }
    
    if (!isEdit.value) {
      // 创建用户，添加密码字段
      await userService.createUser({
        ...userData,
        password: userForm.password,
        confirmPassword: userForm.confirmPassword
      })
      ElMessage.success('创建成功')
    } else {
      // 更新用户基本信息，密码为空时不更新
      const updateData = { ...userData }
      if (userForm.password) {
        updateData.password = userForm.password
        updateData.confirmPassword = userForm.confirmPassword
      }
      
      // 更新用户 - 调用管理员更新用户API
      const res = await api.put(`/api/Admin/users/${userForm.id}`, updateData)
      if (res.data.success) {
        ElMessage.success('更新成功')
      } else {
        ElMessage.error(res.data.message || '更新失败')
        throw new Error('更新失败')
      }
    }
    
    dialogVisible.value = false
    fetchUsers()
  } catch (error) {
    console.error('提交失败:', error)
    ElMessage.error(isEdit.value ? '更新失败' : '创建失败')
  } finally {
    dialogLoading.value = false
  }
}

// 对话框关闭
const handleDialogClose = () => {
  userFormRef.value?.resetFields()
  Object.assign(userForm, {
    id: '',
    username: '',
    email: '',
    phone: '',
    role: 'User',
    status: 'Pending'
  })
}

onMounted(async () => {
  // 先获取角色列表
  await fetchRoles()
  // 再获取用户列表
  fetchUsers()
})
</script>

<style scoped lang="scss">
.users-container {
  padding: 0;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
  
  h2 {
    margin: 0;
    color: var(--el-text-color-primary);
  }
}

.search-card {
  margin-bottom: 20px;
}

.pagination-container {
  display: flex;
  justify-content: flex-end;
  margin-top: 20px;
}

.role-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
}

.role-tag {
  margin-bottom: 4px;
}

.form-tip {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  margin-top: 4px;
}

/* 骨架屏样式 */
.skeleton-container {
  background: #fff;
  border-radius: 8px;
  padding: 16px;
}

.skeleton-user-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 0;
  border-bottom: 1px solid #f0f0f0;
}

.skeleton-user-item:last-child {
  border-bottom: none;
}

.skeleton-user-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.skeleton-user-actions {
  display: flex;
  gap: 8px;
}
</style>