<template>
  <div class="permissions-container">
    <div class="page-header">
      <h2>权限管理</h2>
      <div>
        <el-button
          type="primary"
          @click="handleCreatePermission"
        >
          新增权限
        </el-button>
        <el-button
          type="success"
          style="margin-left: 10px;"
          @click="handleCreateGroup"
        >
          新增分组
        </el-button>
      </div>
    </div>

    <!-- 搜索和筛选 -->
    <el-card class="search-card">
      <el-form
        :model="searchForm"
        inline
      >
        <el-form-item label="权限名称">
          <el-input
            v-model="searchForm.name"
            placeholder="请输入权限名称"
            clearable
          />
        </el-form-item>
        <el-form-item label="权限编码">
          <el-input
            v-model="searchForm.code"
            placeholder="请输入权限编码"
            clearable
          />
        </el-form-item>
        <el-form-item label="权限分组">
          <el-select
            v-model="searchForm.groupId"
            placeholder="请选择权限分组"
            clearable
          >
            <el-option 
              v-for="group in permissionGroups" 
              :key="group.id" 
              :label="group.name" 
              :value="group.id"
            />
          </el-select>
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

    <!-- 权限列表 -->
    <el-card>
      <div class="permissions-tree">
        <el-tree
          v-loading="loading"
          :data="permissionsTreeData"
          :props="treeProps"
          node-key="id"
          default-expand-all
          :expand-on-click-node="false"
        >
          <template #default="{ node, data }">
            <div
              class="tree-node-content"
              :class="{ 'group-node': data.type === 'group' }"
            >
              <span
                v-if="data.type === 'group'"
                class="node-title group-title"
              >{{ data.name }}</span>
              <span
                v-else
                class="node-title permission-title"
              >
                <span>{{ data.name }}</span>
                <el-tag
                  size="small"
                  type="info"
                  class="code-tag"
                >{{ data.code }}</el-tag>
              </span>
              
              <div class="node-actions">
                <span
                  v-if="data.type === 'group'"
                  class="node-description"
                >
                  {{ data.description || '暂无描述' }}
                </span>
                <span
                  v-else
                  class="node-description"
                >
                  {{ data.description || '暂无描述' }}
                </span>
                
                <template v-if="!data.isSystem">
                  <el-button 
                    v-if="data.type === 'group'" 
                    size="small"
                    @click="() => handleEdit(data.type === 'group' ? 'group' : 'permission', data)"
                  >
                    编辑分组
                  </el-button>
                  <el-button 
                    v-else 
                    size="small"
                    @click="() => handleEdit('permission', data)"
                  >
                    编辑
                  </el-button>
                  <el-button 
                    size="small" 
                    type="danger" 
                    @click="() => handleDelete(data.type === 'group' ? 'group' : 'permission', data)"
                  >
                    删除
                  </el-button>
                </template>
              </div>
            </div>
          </template>
        </el-tree>
      </div>
    </el-card>

    <!-- 权限编辑对话框 -->
    <el-dialog
      v-model="permissionDialogVisible"
      :title="permissionDialogTitle"
      width="600px"
      @close="handlePermissionDialogClose"
    >
      <el-form
        ref="permissionFormRef"
        :model="permissionForm"
        :rules="permissionRules"
        label-width="100px"
      >
        <el-form-item
          label="权限名称"
          prop="name"
        >
          <el-input v-model="permissionForm.name" />
        </el-form-item>
        <el-form-item
          label="权限编码"
          prop="code"
        >
          <el-input
            v-model="permissionForm.code"
            :disabled="isPermissionEdit"
          />
        </el-form-item>
        <el-form-item
          label="权限描述"
          prop="description"
        >
          <el-input
            v-model="permissionForm.description"
            type="textarea"
            :rows="3"
          />
        </el-form-item>
        <el-form-item
          label="所属分组"
          prop="groupId"
        >
          <el-select
            v-model="permissionForm.groupId"
            placeholder="请选择权限分组"
          >
            <el-option 
              v-for="group in permissionGroups" 
              :key="group.id" 
              :label="group.name" 
              :value="group.id"
            />
          </el-select>
        </el-form-item>
      </el-form>
      
      <template #footer>
        <el-button @click="permissionDialogVisible = false">
          取消
        </el-button>
        <el-button 
          type="primary" 
          :loading="permissionDialogLoading" 
          @click="handlePermissionSubmit"
        >
          确定
        </el-button>
      </template>
    </el-dialog>

    <!-- 分组编辑对话框 -->
    <el-dialog
      v-model="groupDialogVisible"
      :title="groupDialogTitle"
      width="500px"
      @close="handleGroupDialogClose"
    >
      <el-form
        ref="groupFormRef"
        :model="groupForm"
        :rules="groupRules"
        label-width="100px"
      >
        <el-form-item
          label="分组名称"
          prop="name"
        >
          <el-input v-model="groupForm.name" />
        </el-form-item>
        <el-form-item
          label="分组ID"
          prop="id"
        >
          <el-input
            v-model="groupForm.id"
            :disabled="isGroupEdit"
            placeholder="仅允许使用字母、数字、下划线"
          />
          <div
            v-if="!isGroupEdit"
            class="form-tip"
          >
            提示：分组ID一旦创建不可修改，建议使用有意义的标识符
          </div>
        </el-form-item>
        <el-form-item
          label="分组描述"
          prop="description"
        >
          <el-input
            v-model="groupForm.description"
            type="textarea"
            :rows="3"
          />
        </el-form-item>
      </el-form>
      
      <template #footer>
        <el-button @click="groupDialogVisible = false">
          取消
        </el-button>
        <el-button 
          type="primary" 
          :loading="groupDialogLoading" 
          @click="handleGroupSubmit"
        >
          确定
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import type { Permission, PermissionGroup } from '@/types/api'
import { permissionService } from '@/services'

const loading = ref(false)
const permissionDialogVisible = ref(false)
const groupDialogVisible = ref(false)
const permissionDialogLoading = ref(false)
const groupDialogLoading = ref(false)
const isPermissionEdit = ref(false)
const isGroupEdit = ref(false)
const permissionFormRef = ref<FormInstance>()
const groupFormRef = ref<FormInstance>()

const searchForm = reactive({
  name: '',
  code: '',
  groupId: ''
})

const permissionForm = reactive({
  id: '',
  name: '',
  code: '',
  description: '',
  groupId: ''
})

const groupForm = reactive({
  id: '',
  name: '',
  description: ''
})

const permissions = ref<Permission[]>([])
const permissionGroups = ref<PermissionGroup[]>([])

const permissionDialogTitle = computed(() => isPermissionEdit.value ? '编辑权限' : '新增权限')
const groupDialogTitle = computed(() => isGroupEdit.value ? '编辑分组' : '新增分组')

const treeProps = {
  children: 'children',
  label: 'name'
}

const permissionsTreeData = computed(() => {
  // 将权限按分组组织成树形结构
  const tree: any[] = []
  
  // 过滤出符合搜索条件的权限
  const filteredPermissions = permissions.value.filter(permission => {
    const matchName = !searchForm.name || permission.name.includes(searchForm.name)
    const matchCode = !searchForm.code || permission.code.includes(searchForm.code)
    const matchGroup = !searchForm.groupId || permission.category === searchForm.groupId
    return matchName && matchCode && matchGroup
  })
  
  // 过滤出符合搜索条件的分组
  const filteredGroups = permissionGroups.value.filter(group => {
    // 如果按分组筛选，则只显示选中的分组
    if (searchForm.groupId && group.id !== searchForm.groupId) return false
    
    // 如果按权限名称或编码搜索，则只显示包含匹配权限的分组
    if (searchForm.name || searchForm.code) {
      const hasMatchedPermission = filteredPermissions.some(p => p.category === group.id)
      return hasMatchedPermission
    }
    
    return true
  })
  
  // 构建树状数据
  filteredGroups.forEach(group => {
    const groupNode = {
      id: group.id,
      name: group.name,
      description: group.description,
      type: 'group',
      isSystem: group.id === 'system',
      children: filteredPermissions
        .filter(permission => permission.category === group.id)
        .map(permission => ({
          ...permission,
          type: 'permission',
          isSystem: permission.id.includes('system:')
        }))
    }
    tree.push(groupNode)
  })
  
  return tree
})

const permissionRules: FormRules = {
  name: [
    { required: true, message: '请输入权限名称', trigger: 'blur' },
    { min: 2, max: 30, message: '权限名称长度在2-30个字符', trigger: 'blur' }
  ],
  code: [
    { required: true, message: '请输入权限编码', trigger: 'blur' },
    { 
      pattern: /^[a-z0-9_:]+$/, 
      message: '权限编码只能包含小写字母、数字、下划线和冒号', 
      trigger: 'blur' 
    },
    { min: 3, max: 50, message: '权限编码长度在3-50个字符', trigger: 'blur' }
  ],
  description: [
    { max: 100, message: '权限描述不能超过100个字符', trigger: 'blur' }
  ],
  groupId: [
    { required: true, message: '请选择所属分组', trigger: 'change' }
  ]
}

const groupRules: FormRules = {
  name: [
    { required: true, message: '请输入分组名称', trigger: 'blur' },
    { min: 2, max: 20, message: '分组名称长度在2-20个字符', trigger: 'blur' }
  ],
  id: [
    { required: true, message: '请输入分组ID', trigger: 'blur' },
    { 
      pattern: /^[a-z0-9_]+$/, 
      message: '分组ID只能包含小写字母、数字和下划线', 
      trigger: 'blur' 
    },
    { min: 2, max: 20, message: '分组ID长度在2-20个字符', trigger: 'blur' },
    {
      validator: (rule, value, callback) => {
        if (!value) {
          callback(new Error('请输入分组ID'))
          return
        }
        
        // 检查分组ID是否重复
        if (!isGroupEdit.value) {
          const exists = permissionGroups.value.some(group => group.id === value)
          if (exists) {
            callback(new Error('分组ID已存在'))
            return
          }
        }
        
        callback()
      },
      trigger: 'blur'
    }
  ],
  description: [
    { max: 100, message: '分组描述不能超过100个字符', trigger: 'blur' }
  ]
}

// 获取权限列表并生成分组
const fetchPermissions = async () => {
  loading.value = true
  try {
    const permissionsResponse = await permissionService.getAllPermissions()
    permissions.value = permissionsResponse.data || []
    
    // 从权限中提取唯一的分类作为分组
    const uniqueCategories = [...new Set(permissions.value.map(p => p.category).filter(c => c))]
    
    // 创建分组信息
    permissionGroups.value = uniqueCategories.map(category => ({
      id: category,
      name: category.charAt(0).toUpperCase() + category.slice(1),
      description: `${category}相关权限`,
      isSystem: category === 'system'
    }))
  } catch (error) {
    console.error('获取权限列表失败:', error)
    ElMessage.error('获取权限列表失败')
    
    // 显示模拟数据作为备选
    permissions.value = [
      { id: 'user:create', name: '创建用户', description: '允许创建新用户', code: 'user:create', category: 'user' },
      { id: 'user:read', name: '查看用户', description: '允许查看用户信息', code: 'user:read', category: 'user' },
      { id: 'user:update', name: '更新用户', description: '允许更新用户信息', code: 'user:update', category: 'user' },
      { id: 'user:delete', name: '删除用户', description: '允许删除用户', code: 'user:delete', category: 'user' },
      { id: 'role:create', name: '创建角色', description: '允许创建新角色', code: 'role:create', category: 'role' },
      { id: 'role:read', name: '查看角色', description: '允许查看角色信息', code: 'role:read', category: 'role' },
      { id: 'role:update', name: '更新角色', description: '允许更新角色信息', code: 'role:update', category: 'role' },
      { id: 'role:delete', name: '删除角色', description: '允许删除角色', code: 'role:delete', category: 'role' },
      { id: 'system:config', name: '系统配置', description: '允许修改系统配置', code: 'system:config', category: 'system' }
    ]
    
    permissionGroups.value = [
      { id: 'user', name: '用户管理', description: '用户相关权限' },
      { id: 'role', name: '角色管理', description: '角色相关权限' },
      { id: 'system', name: '系统管理', description: '系统配置相关权限', isSystem: true }
    ]
  } finally {
    loading.value = false
  }
}

// 搜索
const handleSearch = () => {
  // 搜索逻辑已经通过计算属性实现
}

// 重置搜索
const handleReset = () => {
  Object.assign(searchForm, {
    name: '',
    code: '',
    groupId: ''
  })
}

// 创建权限
const handleCreatePermission = () => {
  isPermissionEdit.value = false
  Object.assign(permissionForm, {
    id: '',
    name: '',
    code: '',
    description: '',
    groupId: permissionGroups.value.length > 0 ? permissionGroups.value[0].id : ''
  })
  permissionDialogVisible.value = true
}

// 编辑权限
const handleEdit = (type: 'permission' | 'group', data: any) => {
  if (type === 'permission') {
    isPermissionEdit.value = true
    Object.assign(permissionForm, data)
    permissionForm.groupId = data.category
    permissionDialogVisible.value = true
  } else {
    // 分组编辑功能暂时禁用，因为分组是基于权限分类动态生成的
    ElMessage.warning('分组是基于权限分类动态生成的，无法直接编辑')
  }
}

// 创建分组
const handleCreateGroup = () => {
  isGroupEdit.value = false
  Object.assign(groupForm, {
    id: '',
    name: '',
    description: ''
  })
  groupDialogVisible.value = true
}

// 删除权限
const handleDelete = async (type: 'permission' | 'group', data: any) => {
  try {
    // 目前只支持删除权限，因为分组是基于权限的分类动态生成的
    if (type === 'group') {
      ElMessage.warning('分组是基于权限分类动态生成的，无法直接删除')
      return
    }
    
    const confirmMsg = `确定要删除权限 "${data.name}" 吗？`
    await ElMessageBox.confirm(confirmMsg, '提示', {
      type: 'warning'
    })
    
    await permissionService.deletePermission(data.id)
    
    ElMessage.success('删除成功')
    fetchPermissions()
  } catch (error) {
    if (error !== 'cancel') {
      console.error('删除失败:', error)
      ElMessage.error('删除失败')
    }
    // 用户取消删除不做处理
  }
}

// 提交权限表单
const handlePermissionSubmit = async () => {
  if (!permissionFormRef.value) return

  try {
    await permissionFormRef.value.validate()
    permissionDialogLoading.value = true
    
    if (isPermissionEdit.value) {
      // 更新权限
      await permissionService.updatePermission(permissionForm.id, {
        name: permissionForm.name,
        description: permissionForm.description,
        code: permissionForm.code,
        category: permissionForm.groupId
      })
      ElMessage.success('更新成功')
    } else {
      // 创建新权限
      await permissionService.createPermission({
        name: permissionForm.name,
        description: permissionForm.description,
        code: permissionForm.code,
        category: permissionForm.groupId
      })
      ElMessage.success('创建成功')
    }
    
    permissionDialogVisible.value = false
    fetchPermissions()
  } catch (error) {
    console.error('提交失败:', error)
    ElMessage.error(isPermissionEdit.value ? '更新失败' : '创建失败')
  } finally {
    permissionDialogLoading.value = false
  }
}

// 提交分组表单（分组是基于权限分类的，这里提供UI但实际实现需要通过创建带新分类的权限来实现）
const handleGroupSubmit = async () => {
  if (!groupFormRef.value) return

  try {
    await groupFormRef.value.validate()
    groupDialogLoading.value = true
    
    // 由于分组是基于权限分类动态生成的，我们需要创建一个带有新分类的占位权限
    // 这样分组就会在下次加载时自动显示
    await permissionService.createPermission({
      name: `${groupForm.name} - 占位权限`,
      description: `这是${groupForm.description}的占位权限，用于创建新的权限分组`,
      code: `${groupForm.id}:placeholder`,
      category: groupForm.id
    })
    
    ElMessage.success('分组创建成功（通过创建占位权限实现）')
    groupDialogVisible.value = false
    fetchPermissions()
  } catch (error) {
    console.error('提交失败:', error)
    ElMessage.error('创建失败')
  } finally {
    groupDialogLoading.value = false
  }
}

// 权限对话框关闭
const handlePermissionDialogClose = () => {
  permissionFormRef.value?.resetFields()
}

// 分组对话框关闭
const handleGroupDialogClose = () => {
  groupFormRef.value?.resetFields()
}

onMounted(() => {
  fetchPermissions()
})
</script>

<style scoped lang="scss">
.permissions-container {
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

.permissions-tree {
  max-height: 600px;
  overflow-y: auto;
}

.tree-node-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 4px 0;
  
  &.group-node {
    background-color: var(--el-bg-color-secondary);
    padding: 8px 12px;
    margin: 8px 0;
    border-radius: 4px;
  }
}

.node-title {
  display: flex;
  align-items: center;
  flex: 1;
  
  &.group-title {
    font-weight: 600;
    font-size: 16px;
  }
  
  &.permission-title {
    display: flex;
    align-items: center;
    gap: 10px;
  }
}

.code-tag {
  margin-left: 8px;
}

.node-description {
  color: var(--el-text-color-secondary);
  font-size: 12px;
  margin-right: 16px;
}

.node-actions {
  display: flex;
  align-items: center;
  gap: 8px;
}

.form-tip {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  margin-top: 4px;
}
</style>