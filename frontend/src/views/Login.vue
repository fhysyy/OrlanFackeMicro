<template>
  <div class="login-container">
    <div class="login-form">
      <div class="login-header">
        <!-- <h1>FakeMicro</h1>
        <p>微服务管理平台</p> -->
      </div>

      <el-form
        ref="loginFormRef"
        :model="loginForm"
        :rules="loginRules"
        class="login-form-content"
        @submit.prevent="handleLogin"
      >
        <el-form-item prop="usernameOrEmail">
          <el-input
            v-model="loginForm.usernameOrEmail"
            placeholder="用户名或邮箱"
            size="large"
            prefix-icon="User"
          />
        </el-form-item>

        <el-form-item prop="password">
          <el-input
            v-model="loginForm.password"
            type="password"
            placeholder="密码"
            size="large"
            prefix-icon="Lock"
            show-password
          />
        </el-form-item>

        <el-form-item>
          <el-button
            type="primary"
            size="large"
            class="login-button"
            :loading="loading"
            @click="handleLogin"
          >
            {{ loading ? '登录中...' : '登录' }}
          </el-button>
        </el-form-item>

        <div class="login-links">
          <el-link
            type="primary"
            @click="showRegister = true"
          >
            注册账号
          </el-link>
          <el-link type="info">
            忘记密码？
          </el-link>
        </div>
      </el-form>
    </div>

    <!-- 注册对话框 -->
    <el-dialog
      v-model="showRegister"
      title="注册账号"
      width="400px"
      :before-close="handleCloseRegister"
    >
      <el-form
        ref="registerFormRef"
        :model="registerForm"
        :rules="registerRules"
        label-width="80px"
      >
        <el-form-item
          label="用户名"
          prop="username"
        >
          <el-input v-model="registerForm.username" />
        </el-form-item>

        <el-form-item
          label="邮箱"
          prop="email"
        >
          <el-input v-model="registerForm.email" />
        </el-form-item>

        <el-form-item
          label="密码"
          prop="password"
        >
          <el-input
            v-model="registerForm.password"
            type="password"
            show-password
          />
        </el-form-item>

        <el-form-item
          label="手机号"
          prop="phone"
        >
          <el-input v-model="registerForm.phone" />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="showRegister = false">
          取消
        </el-button>
        <el-button
          type="primary"
          :loading="registerLoading"
          @click="handleRegister"
        >
          注册
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { useAuthStore } from '@/stores/auth'
import type { LoginRequest, RegisterRequest } from '@/types/api'

const router = useRouter()
const authStore = useAuthStore()

const loginFormRef = ref<FormInstance>()
const registerFormRef = ref<FormInstance>()

const loading = ref(false)
const registerLoading = ref(false)
const showRegister = ref(false)

const loginForm = reactive({
  usernameOrEmail: '',
  password: ''
})

const registerForm = reactive<RegisterRequest>({
  username: '',
  email: '',
  confirmPassword: '',
  password: '',
  phone: ''
})

const loginRules: FormRules = {
  usernameOrEmail: [
    { required: true, message: '请输入用户名或邮箱', trigger: 'blur' }
  ],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' },
    { min: 6, message: '密码长度不能少于6位', trigger: 'blur' }
  ]
}

const registerRules: FormRules = {
  username: [
    { required: true, message: '请输入用户名', trigger: 'blur' },
    { min: 3, max: 20, message: '用户名长度在3-20个字符', trigger: 'blur' }
  ],
  email: [
    { required: true, message: '请输入邮箱', trigger: 'blur' },
    { type: 'email', message: '请输入正确的邮箱地址', trigger: 'blur' }
  ],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' },
    { min: 6, message: '密码长度不能少于6位', trigger: 'blur' }
  ]
}

const handleLogin = async () => {
  if (!loginFormRef.value) return

  try {
    await loginFormRef.value.validate()
    loading.value = true

    const result = await authStore.login(loginForm)

    if (result.success) {
      ElMessage.success('登录成功')
      // 检查URL中是否有redirect参数
      const urlParams = new URLSearchParams(window.location.search)
      const redirectUrl = urlParams.get('redirect')
      // 如果有redirect参数且不为空，则跳转到该地址，否则跳转到默认的dashboard
      if (redirectUrl) {
        try {
          // 解码并重定向
          router.push(decodeURIComponent(redirectUrl))
        } catch {
          // 如果解码失败，则跳转到默认页面
          router.push('/dashboard')
        }
      } else {
        router.push('/dashboard')
      }
    } else {
      ElMessage.error(result.errorMessage || '登录失败')
    }
  } catch (error: any) {
    ElMessage.error(error.message || '登录失败')
  } finally {
    loading.value = false
  }
}

const handleRegister = async () => {
  if (!registerFormRef.value) return

  try {
    await registerFormRef.value.validate()
    registerLoading.value = true

    const result = await authStore.register(registerForm)

    if (result.success) {
      ElMessage.success('注册成功，请登录')
      showRegister.value = false
      // 清空注册表单
      Object.assign(registerForm, {
        username: '',
        email: '',
        password: '',
        confirmPassword: '',
        phone: ''
      })
    } else {
      ElMessage.error(result.errorMessage || '注册失败')
    }
  } catch (error: any) {
    ElMessage.error(error.message || '注册失败')
  } finally {
    registerLoading.value = false
  }
}

const handleCloseRegister = () => {
  showRegister.value = false
  if (registerFormRef.value) {
    registerFormRef.value.resetFields()
  }
}
</script>

<style scoped lang="scss">
@use '@/styles/variables.scss' as *;

.login-container {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.login-form {
  background: white;
  padding: 40px;
  border-radius: 8px;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);
  width: 400px;
}

.login-header {
  text-align: center;
  margin-bottom: 30px;

  h1 {
    color: #409eff;
    margin: 0 0 10px 0;
    font-size: 28px;
  }

  p {
    color: #666;
    margin: 0;
    font-size: 14px;
  }
}

.login-form-content {
  .login-button {
    width: 100%;
    margin-top: 10px;
  }
}

.login-links {
  display: flex;
  justify-content: space-between;
  margin-top: 20px;
}
</style>