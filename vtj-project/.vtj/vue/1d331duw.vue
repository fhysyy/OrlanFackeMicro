<template>
  <div class="launch-page">
    <header class="header">
      <img
        src="https://picsum.photos/120/120?random=1"
        alt="App Logo"
        class="logo" />
      <h1 class="title">欢迎使用我们的应用</h1>
      <p class="subtitle">让工作更高效，让生活更简单</p>
    </header>
    <component is="div"> 组件文本内容示例</component>
    <main class="main">
      <div class="action-buttons">
        <ElButton
          type="primary"
          size="large"
          class="action-button"
          @click="handleLogin">
          立即登录</ElButton
        >
        <ElButton size="large" class="action-button" @click="handleRegister">
          注册账号</ElButton
        >
      </div>
      <div class="quick-actions">
        <div
          v-for="(action, index) in state.quickActions"
          :key="index"
          class="quick-action"
          @click="(...args: any[]) => click_b8neu6te({ action, index }, args)">
          <XIcon :icon="action.icon" :size="24" class="action-icon"></XIcon>
          <span> {{ action.label }}</span>
        </div>
      </div>
    </main>
    <footer class="footer">
      <p>© 2023 我们的应用. 保留所有权利.</p>
      <div class="footer-links">
        <a href="#" @click.prevent="handleAbout"> 关于我们</a>
        <a href="#" @click.prevent="handleTerms"> 服务条款</a>
        <a href="#" @click.prevent="handlePrivacy"> 隐私政策</a>
      </div>
    </footer>
  </div>
</template>
<script lang="ts">
  // @ts-nocheck
  import { defineComponent, reactive } from 'vue';
  import { ElButton, ElMessage } from 'element-plus';
  import { XIcon } from '@vtj/ui';
  import { HomeFilled, Setting, Help } from '@vtj/icons';
  import { useProvider } from '@vtj/renderer';
  export default defineComponent({
    name: 'LaunchPage',
    components: { ElButton, XIcon },
    setup(props) {
      const provider = useProvider({ id: '1d331duw', version: '1764154654906' });
      const state = reactive({
        quickActions: [
          { icon: HomeFilled, label: '首页', action: 'home' },
          { icon: Setting, label: '设置', action: 'settings' },
          { icon: Help, label: '帮助', action: 'help' }
        ]
      });
      return { state, props, provider, HomeFilled, Setting, Help, ElMessage };
    },
    methods: {
      click_b8neu6te({ action, index }, args) {
        return (($event) => {
          this.handleQuickAction(action);
        }).apply(this, args);
      },
      handleLogin() {
        ElMessage({ message: 'login' });
      },
      handleRegister() {
        ElMessage({ message: 'register' });
      },
      handleQuickAction(action) {
        ElMessage({ message: action.action });
      },
      handleAbout() {
        ElMessage({ message: 'about' });
      },
      handleTerms() {
        ElMessage({ message: 'terms' });
      },
      handlePrivacy() {
        ElMessage({ message: 'privacy' });
      }
    }
  })
</script>
<style lang="css" scoped>
  .launch-page {
    display: flex;
    flex-direction: column;
    height: 100%;
    background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
    padding: 20px;
    text-align: center;
    box-sizing: border-box;
  }

  .header {
    margin-top: 80px;
    margin-bottom: 60px;
  }

  .logo {
    border-radius: 50%;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
    margin-bottom: 20px;
  }

  .title {
    font-size: 2.5rem;
    color: #333;
    margin-bottom: 10px;
  }

  .subtitle {
    font-size: 1.2rem;
    color: #666;
  }

  .main {
    flex: 1;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
  }

  .action-buttons {
    display: flex;
    gap: 20px;
    margin-bottom: 60px;
  }

  .action-button {
    min-width: 150px;
  }

  .quick-actions {
    display: flex;
    gap: 40px;
    margin-bottom: 40px;
  }

  .quick-action {
    display: flex;
    flex-direction: column;
    align-items: center;
    cursor: pointer;
    transition: all 0.3s ease;
    color: #555;
  }

  .quick-action:hover {
    transform: translateY(-5px);
    color: #409eff;
  }

  .action-icon {
    margin-bottom: 8px;
  }

  .footer {
    margin-top: auto;
    padding: 20px 0;
    color: #666;
    font-size: 0.9rem;
  }

  .footer-links {
    display: flex;
    justify-content: center;
    gap: 20px;
    margin-top: 10px;
  }

  .footer-links a {
    color: #666;
    text-decoration: none;
  }

  .footer-links a:hover {
    color: #409eff;
    text-decoration: underline;
  }
</style>
