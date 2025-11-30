<template>
  <div class="profile-container">
    <div class="page-header">
      <h2>个人资料</h2>
    </div>

    <el-row :gutter="20">
      <!-- 个人信息 -->
      <el-col :span="16">
        <el-card header="基本信息">
          <el-form
            ref="profileFormRef"
            :model="profileForm"
            :rules="profileRules"
            label-width="100px"
          >
            <el-form-item
              label="用户名"
              prop="username"
            >
              <el-input
                v-model="profileForm.username"
                disabled
              />
            </el-form-item>
            
            <el-form-item
              label="邮箱"
              prop="email"
            >
              <el-input v-model="profileForm.email" />
            </el-form-item>
            
            <el-form-item
              label="手机号"
              prop="phone"
            >
              <el-input v-model="profileForm.phone" />
            </el-form-item>
            
            <el-form-item label="角色">
              <el-tag :type="getRoleType(profileForm.role)">
                {{ profileForm.role }}
              </el-tag>
            </el-form-item>
            
            <el-form-item label="状态">
              <el-tag :type="getStatusType(profileForm.status)">
                {{ profileForm.status }}
              </el-tag>
            </el-form-item>
            
            <el-form-item label="注册时间">
              <span>{{ formatDate(profileForm.createdAt) }}</span>
            </el-form-item>
            
            <el-form-item label="最后登录">
              <span>{{ formatDate(profileForm.lastLoginAt) }}</span>
            </el-form-item>
            
            <el-form-item>
              <el-button
                type="primary"
                :loading="saving"
                @click="handleSaveProfile"
              >
                保存修改
              </el-button>
            </el-form-item>
          </el-form>
        </el-card>
      </el-col>

      <!-- 头像和密码 -->
      <el-col :span="8">
        <!-- 头像设置 -->
        <el-card
          header="头像设置"
          class="avatar-card"
        >
          <div class="avatar-section">
            <el-avatar
              :size="100"
              :src="avatarUrl"
            />
            <div class="avatar-actions">
              <el-button
                size="small"
                @click="handleUploadAvatar"
              >
                上传头像
              </el-button>
              <el-button
                size="small"
                type="danger"
                @click="handleRemoveAvatar"
              >
                移除头像
              </el-button>
            </div>
          </div>
        </el-card>

        <!-- 密码修改 -->
        <el-card
          header="修改密码"
          class="password-card"
        >
          <el-form
            ref="passwordFormRef"
            :model="passwordForm"
            :rules="passwordRules"
            label-width="100px"
          >
            <el-form-item
              label="当前密码"
              prop="currentPassword"
            >
              <el-input
                v-model="passwordForm.currentPassword"
                type="password"
                show-password
              />
            </el-form-item>
            
            <el-form-item
              label="新密码"
              prop="newPassword"
            >
              <el-input
                v-model="passwordForm.newPassword"
                type="password"
                show-password
              />
            </el-form-item>
            
            <el-form-item
              label="确认密码"
              prop="confirmPassword"
            >
              <el-input
                v-model="passwordForm.confirmPassword"
                type="password"
                show-password
              />
            </el-form-item>
            
            <el-form-item>
              <el-button
                type="primary"
                :loading="changingPassword"
                @click="handleChangePassword"
              >
                修改密码
              </el-button>
            </el-form-item>
          </el-form>
        </el-card>

        <!-- 安全设置 -->
        <el-card
          header="安全设置"
          class="security-card"
        >
          <div class="security-item">
            <div class="security-info">
              <h4>双重认证</h4>
              <p>启用双重认证提高账户安全性</p>
            </div>
            <el-switch
              v-model="twoFactorEnabled"
              @change="handleToggleTwoFactor"
            />
          </div>
          
          <div class="security-item">
            <div class="security-info">
              <h4>登录通知</h4>
              <p>有新设备登录时发送邮件通知</p>
            </div>
            <el-switch
              v-model="loginNotificationEnabled"
              @change="handleToggleLoginNotification"
            />
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 头像上传对话框 -->
    <el-dialog
      v-model="avatarDialogVisible"
      title="上传头像"
      width="400px"
    >
      <el-upload
        class="avatar-uploader"
        action="/api/files/upload"
        :show-file-list="false"
        :before-upload="beforeAvatarUpload"
        :on-success="handleAvatarSuccess"
      >
        <img
          v-if="avatarImageUrl"
          :src="avatarImageUrl"
          class="avatar"
        >
        <el-icon
          v-else
          class="avatar-uploader-icon"
        >
          <Plus />
        </el-icon>
      </el-upload>
      
      <template #footer>
        <el-button @click="avatarDialogVisible = false">
          取消
        </el-button>
        <el-button
          type="primary"
          @click="confirmAvatar"
        >
          确定
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules, UploadProps } from 'element-plus'
import { useAuthStore } from '@/stores/auth'
import type { User, UserRole, UserStatus } from '@/types/api'

const authStore = useAuthStore()
const profileFormRef = ref<FormInstance>()
const passwordFormRef = ref<FormInstance>()

const saving = ref(false)
const changingPassword = ref(false)
const avatarDialogVisible = ref(false)
const twoFactorEnabled = ref(false)
const loginNotificationEnabled = ref(true)
const avatarImageUrl = ref('')
const avatarUrl = ref('')

const profileForm = reactive({
  username: '',
  email: '',
  phone: '',
  role: 'User' as UserRole,
  status: 'Active' as UserStatus,
  createdAt: '',
  lastLoginAt: ''
})

const passwordForm = reactive({
  currentPassword: '',
  newPassword: '',
  confirmPassword: ''
})

const profileRules: FormRules = {
  email: [
    { required: true, message: '请输入邮箱地址', trigger: 'blur' },
    { type: 'email', message: '请输入正确的邮箱地址', trigger: 'blur' }
  ],
  phone: [
    { pattern: /^1[3-9]\d{9}$/, message: '请输入正确的手机号码', trigger: 'blur' }
  ]
}

const passwordRules: FormRules = {
  currentPassword: [
    { required: true, message: '请输入当前密码', trigger: 'blur' },
    { min: 6, message: '密码长度不能少于6位', trigger: 'blur' }
  ],
  newPassword: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    { min: 6, message: '密码长度不能少于6位', trigger: 'blur' }
  ],
  confirmPassword: [
    { required: true, message: '请确认新密码', trigger: 'blur' },
    { validator: validateConfirmPassword, trigger: 'blur' }
  ]
}

// 验证确认密码
function validateConfirmPassword(rule: any, value: string, callback: any) {
  if (value !== passwordForm.newPassword) {
    callback(new Error('两次输入的密码不一致'))
  } else {
    callback()
  }
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
  return date ? new Date(date).toLocaleString('zh-CN') : '-'
}

// 加载用户信息
const loadUserInfo = () => {
  if (authStore.user) {
    Object.assign(profileForm, {
      username: authStore.user.username,
      email: authStore.user.email,
      phone: authStore.user.phone || '',
      role: authStore.user.role,
      status: authStore.user.status,
      createdAt: authStore.user.createdAt,
      lastLoginAt: authStore.user.lastLoginAt || ''
    })
  }
}

// 保存个人信息
const handleSaveProfile = async () => {
  if (!profileFormRef.value) return

  try {
    await profileFormRef.value.validate()
    saving.value = true
    
    // 模拟API调用
    await new Promise(resolve => setTimeout(resolve, 1000))
    
    ElMessage.success('个人信息更新成功')
    
    // 更新store中的用户信息
    if (authStore.user) {
      authStore.user.email = profileForm.email
      authStore.user.phone = profileForm.phone
    }
  } catch (error) {
    ElMessage.error('个人信息更新失败')
  } finally {
    saving.value = false
  }
}

// 修改密码
const handleChangePassword = async () => {
  if (!passwordFormRef.value) return

  try {
    await passwordFormRef.value.validate()
    changingPassword.value = true
    
    // 模拟API调用
    await new Promise(resolve => setTimeout(resolve, 1000))
    
    ElMessage.success('密码修改成功')
    
    // 清空密码表单
    Object.assign(passwordForm, {
      currentPassword: '',
      newPassword: '',
      confirmPassword: ''
    })
    
    passwordFormRef.value.resetFields()
  } catch (error) {
    ElMessage.error('密码修改失败')
  } finally {
    changingPassword.value = false
  }
}

// 上传头像
const handleUploadAvatar = () => {
  avatarDialogVisible.value = true
}

// 移除头像
const handleRemoveAvatar = async () => {
  try {
    await ElMessageBox.confirm('确定要移除当前头像吗？', '提示', {
      type: 'warning'
    })
    
    // 模拟API调用
    await new Promise(resolve => setTimeout(resolve, 500))
    
    avatarUrl.value = ''
    ElMessage.success('头像移除成功')
  } catch {
    // 用户取消操作
  }
}

// 头像上传前验证
const beforeAvatarUpload: UploadProps['beforeUpload'] = (rawFile) => {
  const isJPGOrPNG = rawFile.type === 'image/jpeg' || rawFile.type === 'image/png'
  const isLt2M = rawFile.size / 1024 / 1024 < 2

  if (!isJPGOrPNG) {
    ElMessage.error('头像必须是 JPG 或 PNG 格式!')
    return false
  }
  if (!isLt2M) {
    ElMessage.error('头像大小不能超过 2MB!')
    return false
  }
  return true
}

// 头像上传成功
const handleAvatarSuccess: UploadProps['onSuccess'] = (response, uploadFile) => {
  avatarImageUrl.value = URL.createObjectURL(uploadFile.raw!)
}

// 确认头像
const confirmAvatar = async () => {
  if (avatarImageUrl.value) {
    // 模拟API调用
    await new Promise(resolve => setTimeout(resolve, 500))
    
    avatarUrl.value = avatarImageUrl.value
    ElMessage.success('头像上传成功')
    avatarDialogVisible.value = false
    avatarImageUrl.value = ''
  } else {
    ElMessage.warning('请先选择头像图片')
  }
}

// 切换双重认证
const handleToggleTwoFactor = async (enabled: boolean) => {
  try {
    // 模拟API调用
    await new Promise(resolve => setTimeout(resolve, 500))
    ElMessage.success(enabled ? '双重认证已启用' : '双重认证已禁用')
  } catch (error) {
    twoFactorEnabled.value = !enabled
    ElMessage.error('操作失败，请重试')
  }
}

// 切换登录通知
const handleToggleLoginNotification = async (enabled: boolean) => {
  try {
    // 模拟API调用
    await new Promise(resolve => setTimeout(resolve, 500))
    ElMessage.success(enabled ? '登录通知已启用' : '登录通知已禁用')
  } catch (error) {
    loginNotificationEnabled.value = !enabled
    ElMessage.error('操作失败，请重试')
  }
}

onMounted(() => {
  loadUserInfo()
})
</script>

<style scoped lang="scss">
.profile-container {
  padding: 0;
}

.page-header {
  margin-bottom: 20px;
  
  h2 {
    margin: 0;
    color: var(--el-text-color-primary);
  }
}

.avatar-card,
.password-card,
.security-card {
  margin-bottom: 20px;
}

.avatar-section {
  text-align: center;
  
  .avatar-actions {
    margin-top: 15px;
    display: flex;
    justify-content: center;
    gap: 10px;
  }
}

.security-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 15px 0;
  border-bottom: 1px solid var(--el-border-color-light);
  
  &:last-child {
    border-bottom: none;
  }
  
  .security-info {
    h4 {
      margin: 0 0 5px 0;
      color: var(--el-text-color-primary);
    }
    
    p {
      margin: 0;
      color: var(--el-text-color-secondary);
      font-size: 12px;
    }
  }
}

.avatar-uploader {
  :deep(.el-upload) {
    border: 1px dashed var(--el-border-color);
    border-radius: 6px;
    cursor: pointer;
    position: relative;
    overflow: hidden;
    transition: var(--el-transition-duration-fast);
  }

  :deep(.el-upload:hover) {
    border-color: var(--el-color-primary);
  }

  .avatar-uploader-icon {
    font-size: 28px;
    color: #8c939d;
    width: 178px;
    height: 178px;
    text-align: center;
    line-height: 178px;
  }

  .avatar {
    width: 178px;
    height: 178px;
    display: block;
  }
}
</style>