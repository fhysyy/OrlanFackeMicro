<template>
  <div class="crud-pagination" v-if="showPagination">
    <el-pagination
      v-model:current-page="pagination.currentPage"
      v-model:page-size="pagination.pageSize"
      :page-sizes="[10, 20, 50, 100]"
      layout="total, sizes, prev, pager, next, jumper"
      :total="pagination.total"
      @size-change="handleSizeChange"
      @current-change="handleCurrentChange"
    />
  </div>
</template>

<script setup lang="ts">
import { reactive, watch } from 'vue';

// Props定义
const props = defineProps<{
  showPagination: boolean;
  initialCurrentPage?: number;
  initialPageSize?: number;
  initialTotal?: number;
}>();

// Emits定义
const emit = defineEmits<{
  (e: 'size-change', size: number): void;
  (e: 'current-change', current: number): void;
  (e: 'pagination-change', pagination: { currentPage: number; pageSize: number; total: number }): void;
}>();

// 响应式数据
const pagination = reactive({
  currentPage: props.initialCurrentPage || 1,
  pageSize: props.initialPageSize || 10,
  total: props.initialTotal || 0
});

// 处理分页大小变化
const handleSizeChange = (size: number) => {
  pagination.pageSize = size;
  pagination.currentPage = 1; // 重置为第一页
  emit('size-change', size);
  emit('pagination-change', { ...pagination });
};

// 处理当前页变化
const handleCurrentChange = (current: number) => {
  pagination.currentPage = current;
  emit('current-change', current);
  emit('pagination-change', { ...pagination });
};

// 监听外部属性变化
watch(() => props.initialCurrentPage, (newVal) => {
  if (newVal !== undefined && newVal !== pagination.currentPage) {
    pagination.currentPage = newVal;
  }
});

watch(() => props.initialPageSize, (newVal) => {
  if (newVal !== undefined && newVal !== pagination.pageSize) {
    pagination.pageSize = newVal;
  }
});

watch(() => props.initialTotal, (newVal) => {
  if (newVal !== undefined && newVal !== pagination.total) {
    pagination.total = newVal;
  }
});

// 暴露方法给父组件
defineExpose({
  getPagination: () => ({ ...pagination }),
  setPagination: (newPagination: Partial<typeof pagination>) => {
    Object.assign(pagination, newPagination);
  },
  resetPagination: () => {
    pagination.currentPage = 1;
    pagination.pageSize = props.initialPageSize || 10;
    pagination.total = 0;
    emit('pagination-change', { ...pagination });
  }
});
</script>

<style scoped>
.crud-pagination {
  margin-top: 16px;
  display: flex;
  justify-content: flex-end;
}
</style>