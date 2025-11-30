<template>
  <div class="layout-container">
    <!-- 移动端遮罩层 -->
    <div 
      v-if="isMobile && !isCollapse" 
      class="mobile-mask"
      @click="toggleSidebar"
    ></div>
    
    <!-- 侧边栏 -->
    <el-aside 
      :width="isCollapse ? '64px' : sidebarWidth" 
      class="sidebar"
      :class="{ 'mobile-sidebar': isMobile }"
    >
      <div class="logo">
        <h2 v-show="!isCollapse">FakeMicro</h2>
        <h2 v-show="isCollapse">FM</h2>
      </div>
      
      <el-menu
        :default-active="activeMenu"
        :collapse="isCollapse"
        router
        class="sidebar-menu"
      >
        <el-menu-item index="/dashboard">
          <el-icon><Monitor /></el-icon>
          <span>仪表板</span>
        </el-menu-item>
        <!-- v-if="hasPermission('Admin')" -->
        <el-menu-item index="/users" >
          <el-icon><User /></el-icon>
          <span>用户管理</span>
        </el-menu-item>
      <!-- v-if="hasPermission('Admin')" -->
        <el-menu-item index="/roles" >
          <el-icon><UserFilled /></el-icon>
          <span>角色管理</span>
        </el-menu-item>
        <!-- v-if="hasPermission('SystemAdmin')" -->
        <el-menu-item index="/permissions" >
          <el-icon><Key /></el-icon>
          <span>权限管理</span>
        </el-menu-item>
        
        <el-menu-item index="/messages">
          <el-icon><Message /></el-icon>
          <span>消息管理</span>
        </el-menu-item>
        
        <el-menu-item index="/files">
          <el-icon><Folder /></el-icon>
          <span>文件管理</span>
        </el-menu-item>
        <!-- v-if="hasPermission('SystemAdmin')" -->
        <el-menu-item index="/system" >
          <el-icon><Setting /></el-icon>
          <span>系统监控</span>
        </el-menu-item>
        
        <!-- 低代码设计器 - 仅管理员和系统管理员可见v-if="hasPermission('Admin')" -->
        <el-menu-item index="/__vtj__/#/" >
          <el-icon><Edit /></el-icon>
          <span>低代码设计器</span>
        </el-menu-item>
        <!-- v-if="hasPermission('Admin')" -->
        <el-sub-menu  index="/dictionary">
          <template #title>
            <el-icon><Key /></el-icon>
            <span>字典管理</span>
          </template>
          <el-menu-item index="/dictionary-types">字典类型</el-menu-item>
          <el-menu-item index="/dictionary-items">字典项</el-menu-item>
        </el-sub-menu>
        
        <!-- 班级学生成绩管理 -->
        <el-sub-menu index="/management">
          <template #title>
            <el-icon><School /></el-icon>
            <span>教学管理</span>
          </template>
          <el-menu-item index="/class-management">班级管理</el-menu-item>
          <el-menu-item index="/student-management">学生管理</el-menu-item>
          <el-menu-item index="/score-management">成绩管理</el-menu-item>
          <el-menu-item index="/exam-management">考试管理</el-menu-item>
        </el-sub-menu>
        
        <!-- 可配置表单 -->
        <el-sub-menu index="/configurable-form">
          <template #title>
            <el-icon><Document /></el-icon>
            <span>表单管理</span>
          </template>
          <el-menu-item index="/form-config-management">表单配置管理</el-menu-item>
          <el-menu-item index="/configurable-form">创建/编辑表单</el-menu-item>
        </el-sub-menu>
      </el-menu>
    </el-aside>

    <!-- 主内容区 -->
    <div class="main-content">
      <!-- 顶部导航 -->
      <el-header class="header">
        <div class="header-left">
          <el-button
            :icon="isCollapse ? 'Expand' : 'Fold'"
            @click="toggleSidebar"
            text
            v-if="!isMobile"
          />
          <el-button
            icon="Menu"
            @click="toggleSidebar"
            text
            v-else
          />
          <el-breadcrumb separator="/">
            <template v-for="(item, index) in breadcrumbItems" :key="index">
              <el-breadcrumb-item 
                :to="item.path ? { path: item.path } : undefined"
                :class="{ 'last-item': index === breadcrumbItems.length - 1 }"
              >
                {{ item.title }}
              </el-breadcrumb-item>
            </template>
          </el-breadcrumb>
        </div>
        
        <div class="header-right">
          <el-dropdown @command="handleCommand">
            <span class="user-info">
              <el-avatar :size="32" :src="authStore.user?.avatar" />
              <span class="username">{{ authStore.user?.username }}</span>
              <el-icon><ArrowDown /></el-icon>
            </span>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item command="profile">个人资料</el-dropdown-item>
                <el-dropdown-item command="settings">设置</el-dropdown-item>
                <el-dropdown-item divided command="logout">退出登录</el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </el-header>

      <!-- 页面内容 -->
      <main class="content">
        <router-view v-slot="{ Component }">
          <transition name="fade" mode="out-in">
            <component :is="Component" />
          </transition>
        </router-view>
      </main>
    </div>

    <!-- 设置对话框 -->
    <el-dialog
      v-model="settingsDialogVisible"
      title="系统设置"
      width="500px"
      :close-on-click-modal="false"
    >
      <div class="settings-content">
        <el-form label-width="100px">
          <el-form-item label="主题模式">
            <el-radio-group v-model="themeMode">
              <el-radio label="light">浅色</el-radio>
              <el-radio label="dark">深色</el-radio>
              <el-radio label="auto">自动</el-radio>
            </el-radio-group>
          </el-form-item>
          <el-form-item label="语言">
            <el-select v-model="language" placeholder="选择语言">
              <el-option label="中文" value="zh-CN" />
              <el-option label="English" value="en-US" />
            </el-select>
          </el-form-item>
        </el-form>
      </div>
      <template #footer>
        <el-button @click="settingsDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="saveSettings">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { ElMessageBox, ElMessage } from 'element-plus'
import { Monitor, User, UserFilled, Key, Message, Folder, Setting, Menu, Expand, Fold, ArrowDown, School, Edit, Document } from '@element-plus/icons-vue'

interface BreadcrumbItem {
  title: string
  path?: string
}

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

// 响应式状态
const isCollapse = ref(false)
const isMobile = ref(false)
const settingsDialogVisible = ref(false)
const themeMode = ref('light')
const language = ref('zh-CN')

// 计算属性
const sidebarWidth = computed(() => isMobile.value ? '240px' : '200px')
const activeMenu = computed(() => route.path)

const breadcrumbItems = computed((): BreadcrumbItem[] => {
  const items: BreadcrumbItem[] = []
  const matchedRoutes = route.matched.filter(record => record.meta?.title)
  
  // 添加首页
  items.push({ title: '首页', path: '/dashboard' })
  
  // 添加匹配的路由
  matchedRoutes.forEach(record => {
    items.push({
      title: record.meta.title as string,
      path: record.path === route.path ? undefined : record.path
    })
  })
  
  return items
})

// 检查屏幕尺寸
const checkScreenSize = () => {
  isMobile.value = window.innerWidth < 768
  if (isMobile.value) {
    isCollapse.value = true
  }
}

// 侧边栏切换
const toggleSidebar = () => {
  isCollapse.value = !isCollapse.value
}

// 权限检查
const hasPermission = (requiredRole: string): boolean => {
  return true;
  try {
    return authStore.hasPermission(requiredRole)
  } catch (error) {
    console.error('权限检查失败:', error)
    return false
  }
}

// 处理下拉菜单命令
const handleCommand = async (command: string) => {
  try {
    switch (command) {
      case 'profile':
        router.push('/profile')
        break
      case 'settings':
        settingsDialogVisible.value = true
        break
      case 'logout':
        await handleLogout()
        break
    }
  } catch (error) {
    console.error('处理命令失败:', error)
    ElMessage.error('操作失败，请重试')
  }
}

// 处理退出登录
const handleLogout = async () => {
  try {
    await ElMessageBox.confirm('确定要退出登录吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    
    await authStore.logout()
    ElMessage.success('退出登录成功')
    router.push('/login')
  } catch (error) {
    if (error !== 'cancel') {
      console.error('退出登录失败:', error)
      ElMessage.error('退出登录失败')
    }
  }
}

// 保存设置
const saveSettings = () => {
  // 这里可以添加保存设置的逻辑
  ElMessage.success('设置已保存')
  settingsDialogVisible.value = false
}

// 生命周期
onMounted(() => {
  checkScreenSize()
  window.addEventListener('resize', checkScreenSize)
  
  // 从本地存储恢复侧边栏状态
  const savedState = localStorage.getItem('sidebar-collapsed')
  if (savedState) {
    isCollapse.value = JSON.parse(savedState)
  }
})

onUnmounted(() => {
  window.removeEventListener('resize', checkScreenSize)
})

// 监听侧边栏状态变化，保存到本地存储
watch(isCollapse, (newValue) => {
  localStorage.setItem('sidebar-collapsed', JSON.stringify(newValue))
})
</script>

<style scoped lang="scss">
.layout-container {
  display: flex;
  height: 100vh;
  position: relative;
}

// 移动端遮罩层
.mobile-mask {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.5);
  z-index: 1999;
}

.sidebar {
  background-color: var(--el-bg-color);
  border-right: 1px solid var(--el-border-color);
  transition: width 0.3s, transform 0.3s;
  z-index: 2000;
  
  &.mobile-sidebar {
    position: fixed;
    height: 100vh;
    transform: translateX(-100%);
    
    &:not(.el-aside--collapse) {
      transform: translateX(0);
    }
  }
  
  .logo {
    height: 60px;
    display: flex;
    align-items: center;
    justify-content: center;
    border-bottom: 1px solid var(--el-border-color);
    
    h2 {
      margin: 0;
      color: var(--el-color-primary);
      font-size: 18px;
      font-weight: 600;
    }
  }
  
  .sidebar-menu {
    border: none;
    height: calc(100vh - 60px);
    
    :deep(.el-menu-item) {
      &.is-active {
        background-color: var(--el-color-primary-light-9);
        color: var(--el-color-primary);
      }
    }
  }
}

.main-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  min-width: 0; // 防止内容溢出
}

.header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 20px;
  background-color: var(--el-bg-color);
  border-bottom: 1px solid var(--el-border-color);
  height: 60px;
  flex-shrink: 0;
  
  .header-left {
    display: flex;
    align-items: center;
    flex: 1;
    min-width: 0;
    
    .el-button {
      margin-right: 15px;
      flex-shrink: 0;
    }
    
    .el-breadcrumb {
      flex: 1;
      min-width: 0;
      
      :deep(.el-breadcrumb__item) {
        .el-breadcrumb__inner {
          color: var(--el-text-color-regular);
          
          &.is-link {
            color: var(--el-color-primary);
            font-weight: normal;
          }
        }
        
        &.last-item {
          .el-breadcrumb__inner {
            color: var(--el-text-color-primary);
            font-weight: 600;
          }
        }
      }
    }
  }
  
  .header-right {
    flex-shrink: 0;
    
    .user-info {
      display: flex;
      align-items: center;
      cursor: pointer;
      padding: 5px 10px;
      border-radius: 4px;
      transition: background-color 0.3s;
      
      &:hover {
        background-color: var(--el-fill-color-light);
      }
      
      .username {
        margin: 0 8px;
        font-size: 14px;
        font-weight: 500;
        
        @media (max-width: 480px) {
          display: none;
        }
      }
    }
  }
}

.content {
  flex: 1;
  padding: 20px;
  overflow: auto;
  background-color: var(--el-bg-color-page);
  
  @media (max-width: 768px) {
    padding: 15px;
  }
  
  @media (max-width: 480px) {
    padding: 10px;
  }
}

// 页面切换动画
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

// 设置对话框样式
.settings-content {
  .el-form-item {
    margin-bottom: 20px;
    
    &:last-child {
      margin-bottom: 0;
    }
  }
}

// 响应式设计
@media (max-width: 768px) {
  .sidebar:not(.mobile-sidebar) {
    display: none;
  }
  
  .main-content {
    width: 100%;
  }
}

@media (min-width: 769px) {
  .mobile-mask {
    display: none;
  }
  
  .sidebar.mobile-sidebar {
    position: relative;
    transform: translateX(0);
  }
}
</style>