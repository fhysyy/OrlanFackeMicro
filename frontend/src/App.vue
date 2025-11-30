<template>
  <div id="app">
    <!-- 移除Suspense组件，直接使用router-view避免实验性功能警告 -->
    <router-view />
  </div>
</template>

<script setup lang="ts">
// 主应用组件
// 移除Suspense导入
</script>

<style lang="scss">
@use '@/styles/variables.scss' as *;

#app {
  height: 100vh;
  font-family: 'Helvetica Neue', Helvetica, 'PingFang SC', 'Hiragino Sans GB', 'Microsoft YaHei', '微软雅黑', Arial, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  color: var(--el-text-color-primary);
  background-color: var(--el-bg-color);
}
</style>