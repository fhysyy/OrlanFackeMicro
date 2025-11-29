<template>
  <div id="app">
     <Suspense>
    <router-view />
     </Suspense>

  </div>
</template>

<script setup lang="ts">
// 主应用组件
 import { Suspense } from 'vue';
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