<template>
  <XContainer class="home" fit>
    <h1>欢迎使用VTJ项目</h1>
    <p>这是项目的首页，您可以在这里添加您的内容。</p>
    <div class="card">
      <h2>项目信息</h2>
      <ul>
        <li>技术栈：Vue 3 + TypeScript + Vite</li>
        <li>UI库：@vtj/web + element-plus</li>
        <li>状态管理：pinia</li>
        <li>路由：vue-router</li>
        <li>国际化：vue-i18n</li>
      </ul>
    </div>
  </XContainer>
</template>
<script lang="ts" setup>
import { XContainer } from '@vtj/web';
</script>
<style lang="scss" scoped>
.home {
  padding: 20px;
  
  h1 {
    color: #333;
    margin-bottom: 20px;
  }
  
  p {
    color: #666;
    margin-bottom: 30px;
  }
  
  .card {
    background-color: #f5f7fa;
    border-radius: 8px;
    padding: 20px;
    box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
    
    h2 {
      color: #333;
      margin-bottom: 15px;
    }
    
    ul {
      list-style-type: disc;
      padding-left: 20px;
      
      li {
        color: #666;
        margin-bottom: 10px;
      }
    }
  }
}
</style>