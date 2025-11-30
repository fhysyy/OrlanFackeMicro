<template>
  <div class="roles-container">
    <div class="page-header">
      <h2>角色管理</h2>
      <el-button
        type="primary"
        @click="handleCreate"
      >
        新增角色
      </el-button>
    </div>

    <!-- 搜索和筛选 -->
    <el-card class="search-card">
      <el-form
        :model="searchForm"
        inline
      >
        <el-form-item label="角色名称">
          <el-input
            v-model="searchForm.name"
            placeholder="请输入角色名称"
            clearable
          />
        </el-form-item>
        <el-form-item>
          <el-button
            type="primary"
            @click="handleSearch"
          >
            搜索
          </el-button>
          <el-button @click="handleReset">
            重置
          </el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <!-- 角色列表 -->
    <el-card>
      <el-table
        v-loading="loading"
        :data="roleList"
      >
        <el-table-column
          prop="id"
          label="ID"
          width="80"
        />
        <el-table-column
          prop="name"
          label="角色名称"
        />
        <el-table-column
          prop="description"
          label="角色描述"
          show-overflow-tooltip
        />
        <el-table-column label="权限数量">
          <template #default="{ row }">
            {{ row.permissions.length }}
          </template>
        </el-table-column>
        <el-table-column
          prop="isSystem"
          label="系统角色"
        >
          <template #default="{ row }">
            <el-tag
              v-if="row.isSystem"
              type="success"
            >
              是
            </el-tag>
            <el-tag v-else>
              否
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column
          prop="createdAt"
          label="创建时间"
          width="180"
        >
          <template #default="{ row }">
            {{ formatDate(row.createdAt) }}
          </template>
        </el-table-column>
        <el-table-column
          label="操作"
          width="200"
        >
          <template #default="{ row }">
            <el-button 
              size="small" 
              :disabled="row.isSystem"
              :tooltip="row.isSystem ? '系统角色不可编辑' : ''"
              @click="handleEdit(row)"
            >
              编辑
            </el-button>
            <el-button 
              size="small" 
              type="danger" 
              :disabled="row.isSystem"
              :tooltip="row.isSystem ? '系统角色不可删除' : ''"
              @click="handleDelete(row)"
            >
              删除
            </el-button>
          </template>
        </el-table-column>
      </el-table>

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

    <!-- 角色编辑对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="dialogTitle"
      width="700px"
      @close="handleDialogClose"
    >
      <el-form
        ref="roleFormRef"
        :model="roleForm"
        :rules="roleRules"
        label-width="80px"
      >
        <el-form-item
          label="角色名称"
          prop="name"
        >
          <el-input
            v-model="roleForm.name"
            :disabled="roleForm.isSystem"
          />
          <el-input
            v-if="roleForm.isSystem"
            v-model="roleForm.name"
            disabled
            style="display: none;"
          />
        </el-form-item>
        <el-form-item
          label="角色描述"
          prop="description"
        >
          <el-input
            v-model="roleForm.description"
            type="textarea"
            :rows="3"
          />
        </el-form-item>
        <el-form-item
          label="权限配置"
          prop="permissions"
        >
          <div
            v-if="permissionGroups.length > 0"
            class="permission-container"
          >
            <div 
              v-for="group in permissionGroups" 
              :key="group.id" 
              class="permission-group"
            >
              <el-checkbox
                :label="group.id"
                :indeterminate="isGroupIndeterminate(group.id)"
                @change="handleGroupChange(group.id, $event)"
              >
                {{ group.name }}
              </el-checkbox>
              <div class="permission-list">
                <el-checkbox
                  v-for="permission in group.permissions"
                  :key="permission.id"
                  v-model="roleForm.permissions"
                  :label="permission.id"
                >
                  {{ permission.name }} - {{ permission.description }}
                </el-checkbox>
              </div>
            </div>
          </div>
          <div
            v-else
            class="no-data"
          >
            暂无权限数据
          </div>
        </el-form-item>
      </el-form>
      
      <template #footer>
        <el-button @click="dialogVisible = false">
          取消
        </el-button>
        <el-button 
          type="primary" 
          :loading="dialogLoading" 
          :disabled="roleForm.isSystem"
          @click="handleSubmit"
        >
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
import type { Role, Permission, PermissionGroup } from '@/types/api'
import { roleService, permissionService } from '@/services'

const authStore = useAuthStore()

const loading = ref(false)
const dialogVisible = ref(false)
const dialogLoading = ref(false)
const isEdit = ref(false)
const roleFormRef = ref<FormInstance>()

const searchForm = reactive({
  name: ''
})

const roleForm = reactive({
  id: '',
  name: '',
  description: '',
  permissions: [] as string[],
  isSystem: false
})

const pagination = reactive({
  current: 1,
  size: 10,
  total: 0
})

const roleList = ref<Role[]>([])
const permissionGroups = ref<PermissionGroup[]>([])

const dialogTitle = computed(() => isEdit.value ? '编辑角色' : '新增角色')

const roleRules: FormRules = {
  name: [
    { required: true, message: '请输入角色名称', trigger: 'blur' },
    { min: 2, max: 20, message: '角色名称长度在2-20个字符', trigger: 'blur' }
  ],
  description: [
    { required: true, message: '请输入角色描述', trigger: 'blur' },
    { max: 100, message: '角色描述不能超过100个字符', trigger: 'blur' }
  ],
  permissions: [
    { required: true, message: '请至少选择一个权限', trigger: 'change' },
    {
      validator: (rule, value, callback) => {
        if (value.length === 0) {
          callback(new Error('请至少选择一个权限'))
        } else {
          callback()
        }
      },
      trigger: 'change'
    }
  ]
}

// 格式化日期
const formatDate = (date: string) => {
  return new Date(date).toLocaleString('zh-CN')
}

// 获取角色列表
const fetchRoles = async () => {
  loading.value = true
  try {
    const response = await roleService.getRoles({
      page: pagination.current,
      size: pagination.size,
      keyword: searchForm.name
    })
   
    if (response.data) {
      roleList.value = response.data
      pagination.total = response.data.length
    }
  } catch (error) {
    console.error('获取角色列表失败:', error)
    ElMessage.error('获取角色列表失败')
    
    // 显示模拟数据作为备选
    roleList.value = [
      {
        id: '1',
        name: '系统管理员',
        description: '拥有系统所有权限',
        permissions: ['user:create', 'user:read', 'user:update', 'user:delete', 'role:create', 'role:read', 'role:update', 'role:delete', 'permission:all'],
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: '2024-01-01T00:00:00Z',
        isSystem: true
      },
      {
        id: '2',
        name: '管理员',
        description: '管理用户和角色',
        permissions: ['user:create', 'user:read', 'user:update', 'user:delete', 'role:read', 'role:update'],
        createdAt: '2024-01-02T00:00:00Z',
        updatedAt: '2024-01-02T00:00:00Z'
      },
      {
        id: '3',
        name: '普通用户',
        description: '基础功能权限',
        permissions: ['user:read', 'message:read', 'file:read'],
        createdAt: '2024-01-03T00:00:00Z',
        updatedAt: '2024-01-03T00:00:00Z'
      }
    ]
    pagination.total = roleList.value.length
  } finally {
    loading.value = false
  }
}

// 获取权限并分组
const fetchPermissions = async () => {
  try {
    const response = await permissionService.getPermissions()
    
    if (response.data) {
      // 按category分组权限
      const permissionsByCategory = response.data.reduce((groups, permission) => {
        const category = permission.category || '未分类'
        if (!groups[category]) {
          groups[category] = []
        }
        groups[category].push(permission)
        return groups
      }, {} as Record<string, Permission[]>)
      
      // 转换为分组格式
      permissionGroups.value = Object.entries(permissionsByCategory).map(([category, permissions], index) => ({
        id: `group-${index}`,
        name: category,
        description: `${category}相关权限`,
        permissions
      }))
    }
  } catch (error) {
    console.error('获取权限列表失败:', error)
    ElMessage.warning('获取权限列表失败，显示模拟数据')
    
    // 显示模拟数据作为备选
    permissionGroups.value = [
      {
        id: 'user',
        name: '用户管理',
        description: '用户相关权限',
        permissions: [
          { id: 'user:create', name: '创建用户', description: '允许创建新用户', code: 'user:create', category: '用户管理' },
          { id: 'user:read', name: '查看用户', description: '允许查看用户信息', code: 'user:read', category: '用户管理' },
          { id: 'user:update', name: '更新用户', description: '允许更新用户信息', code: 'user:update', category: '用户管理' },
          { id: 'user:delete', name: '删除用户', description: '允许删除用户', code: 'user:delete', category: '用户管理' }
        ]
      },
      {
        id: 'role',
        name: '角色管理',
        description: '角色相关权限',
        permissions: [
          { id: 'role:create', name: '创建角色', description: '允许创建新角色', code: 'role:create', category: '角色管理' },
          { id: 'role:read', name: '查看角色', description: '允许查看角色信息', code: 'role:read', category: '角色管理' },
          { id: 'role:update', name: '更新角色', description: '允许更新角色信息', code: 'role:update', category: '角色管理' },
          { id: 'role:delete', name: '删除角色', description: '允许删除角色', code: 'role:delete', category: '角色管理' }
        ]
      },
      {
        id: 'message',
        name: '消息管理',
        description: '消息相关权限',
        permissions: [
          { id: 'message:send', name: '发送消息', description: '允许发送消息', code: 'message:send', category: '消息管理' },
          { id: 'message:read', name: '查看消息', description: '允许查看消息', code: 'message:read', category: '消息管理' }
        ]
      },
      {
        id: 'file',
        name: '文件管理',
        description: '文件相关权限',
        permissions: [
          { id: 'file:upload', name: '上传文件', description: '允许上传文件', code: 'file:upload', category: '文件管理' },
          { id: 'file:download', name: '下载文件', description: '允许下载文件', code: 'file:download', category: '文件管理' },
          { id: 'file:delete', name: '删除文件', description: '允许删除文件', code: 'file:delete', category: '文件管理' },
          { id: 'file:read', name: '查看文件', description: '允许查看文件', code: 'file:read', category: '文件管理' }
        ]
      }
    ]
  }
}

// 检查分组是否部分选中
const isGroupIndeterminate = (groupId: string) => {
  const group = permissionGroups.value.find(g => g.id === groupId)
  if (!group) return false
  
  const groupPermissionIds = group.permissions.map(p => p.id)
  const checkedCount = groupPermissionIds.filter(id => roleForm.permissions.includes(id)).length
  
  return checkedCount > 0 && checkedCount < groupPermissionIds.length
}

// 处理分组选中状态改变
const handleGroupChange = (groupId: string, checked: boolean) => {
  const group = permissionGroups.value.find(g => g.id === groupId)
  if (!group) return
  
  const groupPermissionIds = group.permissions.map(p => p.id)
  
  if (checked) {
    // 添加分组内所有权限
    groupPermissionIds.forEach(id => {
      if (!roleForm.permissions.includes(id)) {
        roleForm.permissions.push(id)
      }
    })
  } else {
    // 移除分组内所有权限
    roleForm.permissions = roleForm.permissions.filter(id => !groupPermissionIds.includes(id))
  }
}

// 搜索
const handleSearch = () => {
  pagination.current = 1
  fetchRoles()
}

// 重置搜索
const handleReset = () => {
  Object.assign(searchForm, {
    name: ''
  })
  pagination.current = 1
  fetchRoles()
}

// 分页大小改变
const handleSizeChange = (size: number) => {
  pagination.size = size
  pagination.current = 1
  fetchRoles()
}

// 当前页改变
const handleCurrentChange = (current: number) => {
  pagination.current = current
  fetchRoles()
}

// 创建角色
const handleCreate = () => {
  isEdit.value = false
  Object.assign(roleForm, {
    id: '',
    name: '',
    description: '',
    permissions: [],
    isSystem: false
  })
  dialogVisible.value = true
}

// 编辑角色
const handleEdit = (role: Role) => {
  isEdit.value = true
  Object.assign(roleForm, role)
  dialogVisible.value = true
}

// 删除角色
const handleDelete = async (role: Role) => {
  try {
    await ElMessageBox.confirm(`确定要删除角色 "${role.name}" 吗？`, '提示', {
      type: 'warning'
    })
    
    await roleService.deleteRole(role.id)
    ElMessage.success('删除成功')
    fetchRoles()
  } catch (error) {
    if (error !== 'cancel') {
      console.error('删除失败:', error)
      ElMessage.error('删除失败')
    }
    // 用户取消删除不做处理
  }
}

// 提交表单
const handleSubmit = async () => {
  if (!roleFormRef.value) return

  try {
    await roleFormRef.value.validate()
    dialogLoading.value = true
    
    if (isEdit.value) {
      // 更新角色
      await roleService.updateRole(roleForm.id, {
        name: roleForm.name,
        description: roleForm.description,
        permissions: roleForm.permissions
      })
      ElMessage.success('更新成功')
    } else {
      // 创建角色
      await roleService.createRole({
        name: roleForm.name,
        description: roleForm.description,
        permissions: roleForm.permissions
      })
      ElMessage.success('创建成功')
    }
    
    dialogVisible.value = false
    fetchRoles()
  } catch (error) {
    console.error('提交失败:', error)
    ElMessage.error(isEdit.value ? '更新失败' : '创建失败')
  } finally {
    dialogLoading.value = false
  }
}

// 对话框关闭
const handleDialogClose = () => {
  roleFormRef.value?.resetFields()
  Object.assign(roleForm, {
    id: '',
    name: '',
    description: '',
    permissions: [],
    isSystem: false
  })
}

onMounted(async () => {
  await fetchPermissions()
  await fetchRoles()
})
</script>

<style scoped lang="scss">
.roles-container {
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

.permission-container {
  max-height: 400px;
  overflow-y: auto;
}

.permission-group {
  margin-bottom: 16px;
  padding: 12px;
  background-color: var(--el-bg-color-secondary);
  border-radius: 4px;
}

.permission-list {
  margin-top: 8px;
  padding-left: 24px;
}

.no-data {
  text-align: center;
  padding: 20px;
  color: var(--el-text-color-secondary);
}
</style>