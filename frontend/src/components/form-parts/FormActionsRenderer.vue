<template>
  <el-form-item class="form-actions">
    <el-button
      type="primary"
      :loading="submitting"
      @click="handleSubmit"
    >
      {{ submitButtonText || '提交' }}
    </el-button>
    <el-button
      v-if="showResetButton !== false"
      @click="handleReset"
    >
      {{ resetButtonText || '重置' }}
    </el-button>
    <!-- 插槽：自定义操作按钮 -->
    <slot name="actions" />
  </el-form-item>
</template>

<script setup lang="ts">
// Props
const props = defineProps<{
  submitting?: boolean
  submitButtonText?: string
  resetButtonText?: string
  showResetButton?: boolean
}>()

// Emits
const emit = defineEmits<{
  (e: 'submit'): void
  (e: 'reset'): void
}>()

// 处理提交
function handleSubmit() {
  emit('submit')
}

// 处理重置
function handleReset() {
  emit('reset')
}
</script>

<style scoped>
.form-actions {
  margin-top: 20px;
  text-align: center;
}
</style>