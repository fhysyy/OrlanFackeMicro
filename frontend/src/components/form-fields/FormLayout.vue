<template>
  <div class="form-layout" :class="`form-layout-${layout}`">
    <!-- 栅格布局 -->
    <template v-if="layout === 'grid' || layout === 'grid-right'">
      <el-row
        :gutter="gutter || 20"
        :justify="layout === 'grid-right' ? 'end' : 'start'"
        class="form-row"
      >
        <el-col
          v-for="(item, index) in children"
          :key="index"
          :xs="xs || 12"
          :sm="sm || 8"
          :md="md || 6"
          :lg="lg || 6"
          :xl="xl || 6"
          :class="['form-col', item.className]"
          :style="{ minWidth: minColWidth }"
        >
          <slot :name="`item-${index}`" :item="item">
            {{ item }}
          </slot>
        </el-col>
      </el-row>
    </template>

    <!-- 水平布局 -->
    <div v-else-if="layout === 'horizontal'" class="form-horizontal">
      <div
        v-for="(item, index) in children"
        :key="index"
        class="form-horizontal-item"
        :class="item.className"
      >
        <slot :name="`item-${index}`" :item="item">
          {{ item }}
        </slot>
      </div>
    </div>

    <!-- 垂直布局 -->
    <div v-else-if="layout === 'vertical'" class="form-vertical">
      <div
        v-for="(item, index) in children"
        :key="index"
        class="form-vertical-item"
        :class="item.className"
      >
        <slot :name="`item-${index}`" :item="item">
          {{ item }}
        </slot>
      </div>
    </div>

    <!-- 内联布局 -->
    <div v-else-if="layout === 'inline'" class="form-inline">
      <div
        v-for="(item, index) in children"
        :key="index"
        class="form-inline-item"
        :class="item.className"
      >
        <slot :name="`item-${index}`" :item="item">
          {{ item }}
        </slot>
      </div>
    </div>

    <!-- 默认布局 -->
    <div v-else class="form-default">
      <slot></slot>
    </div>
  </div>
</template>

<script setup lang="ts">
import { defineProps } from 'vue';
import { FormLayout } from './types';

// Props
const props = defineProps<{
  // 布局类型
  layout?: FormLayout;
  // 子项数据
  children?: any[];
  // 栅格间距
  gutter?: number;
  // 响应式断点
  xs?: number;
  sm?: number;
  md?: number;
  lg?: number;
  xl?: number;
  // 最小列宽
  minColWidth?: string;
}>();
</script>

<style scoped>
.form-layout {
  width: 100%;
}

/* 栅格布局样式 */
.form-layout-grid,
.form-layout-grid-right {
  width: 100%;
}

.form-row {
  margin-bottom: 12px;
}

.form-col {
  margin-bottom: 0;
}

/* 水平布局样式 */
.form-horizontal {
  display: flex;
  flex-wrap: wrap;
  gap: 16px;
  align-items: center;
}

.form-horizontal-item {
  display: flex;
  align-items: center;
}

/* 垂直布局样式 */
.form-vertical {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.form-vertical-item {
  display: flex;
  flex-direction: column;
}

/* 内联布局样式 */
.form-inline {
  display: inline-flex;
  flex-wrap: wrap;
  gap: 8px;
  align-items: center;
}

.form-inline-item {
  display: inline-flex;
  align-items: center;
}

/* 默认布局样式 */
.form-default {
  width: 100%;
}
</style>
