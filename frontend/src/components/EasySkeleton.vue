<template>
  <div class="easy-skeleton">
    <!-- 简单的骨架屏实现 -->
    <div class="skeleton-line" :style="{ width, height }"></div>
  </div>
</template>

<script setup lang="ts">
interface Props {
  type?: 'line' | 'circle' | 'rect'
  width?: string
  height?: string
  animated?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  type: 'line',
  width: '100%',
  height: '20px',
  animated: true
})
</script>

<style scoped>
.easy-skeleton {
  display: inline-block;
}

.skeleton-line {
  background: #f0f0f0;
  border-radius: 4px;
  animation: skeleton-loading 1.5s ease-in-out infinite;
}

.skeleton-line.animate-pulse {
  animation: skeleton-pulse 1.5s ease-in-out infinite;
}

@keyframes skeleton-loading {
  0% { opacity: 0.7; }
  50% { opacity: 0.9; }
  100% { opacity: 0.7; }
}

@keyframes skeleton-pulse {
  0% { background-color: #f0f0f0; }
  50% { background-color: #e0e0e0; }
  100% { background-color: #f0f0f0; }
}
</style>