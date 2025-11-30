<template>
  <div class="data-table-container">
    <!-- 搜索和操作区域 -->
    <div
      v-if="showHeader"
      class="table-header"
    >
      <div class="header-left">
        <slot name="header-left">
          <el-input
            v-if="showSearch"
            v-model="searchValue"
            placeholder="请输入搜索内容"
            clearable
            style="width: 300px"
            @input="handleSearch"
          >
            <template #append>
              <el-button
                :icon="Search"
                @click="handleSearch"
              />
            </template>
          </el-input>
        </slot>
      </div>
      
      <div class="header-right">
        <slot name="header-right">
          <el-button
            v-if="showRefresh"
            :icon="Refresh"
            :loading="loading"
            @click="handleRefresh"
          >
            刷新
          </el-button>
          
          <el-button
            v-if="showAdd"
            type="primary"
            :icon="Plus"
            @click="handleAdd"
          >
            新增
          </el-button>
        </slot>
      </div>
    </div>

    <!-- 数据表格 -->
    <el-table
      v-loading="loading"
      :data="tableData"
      :border="border"
      :stripe="stripe"
      :height="height"
      :max-height="maxHeight"
      :row-key="rowKey"
      style="width: 100%"
      @selection-change="handleSelectionChange"
      @sort-change="handleSortChange"
    >
      <!-- 选择列 -->
      <el-table-column
        v-if="showSelection"
        type="selection"
        width="55"
        align="center"
      />

      <!-- 序号列 -->
      <el-table-column
        v-if="showIndex"
        type="index"
        label="序号"
        width="80"
        align="center"
      />

      <!-- 数据列 -->
      <template
        v-for="column in columns"
        :key="column.prop"
      >
        <el-table-column
          :prop="column.prop"
          :label="column.label"
          :width="column.width"
          :min-width="column.minWidth"
          :align="column.align || 'center'"
          :sortable="column.sortable"
          :fixed="column.fixed"
          :show-overflow-tooltip="column.showOverflowTooltip"
        >
          <template #default="scope">
            <slot
              :name="column.prop"
              :row="scope.row"
              :$index="scope.$index"
            >
              <!-- 格式化显示 -->
              <template v-if="column.formatter">
                {{ column.formatter(scope.row, scope.column, scope.row[column.prop], scope.$index) }}
              </template>
              
              <!-- 状态标签 -->
              <template v-else-if="column.tag">
                <el-tag
                  :type="getTagType(scope.row[column.prop], column.tagMap)"
                  :effect="column.tagEffect || 'light'"
                >
                  {{ getTagText(scope.row[column.prop], column.tagMap) }}
                </el-tag>
              </template>
              
              <!-- 默认显示 -->
              <template v-else>
                {{ scope.row[column.prop] }}
              </template>
            </slot>
          </template>
        </el-table-column>
      </template>

      <!-- 操作列 -->
      <el-table-column
        v-if="showActions"
        label="操作"
        :width="actionsWidth"
        :fixed="actionsFixed"
        align="center"
      >
        <template #default="scope">
          <slot
            name="actions"
            :row="scope.row"
            :$index="scope.$index"
          >
            <el-button
              v-if="showEdit"
              link
              type="primary"
              size="small"
              @click="handleEdit(scope.row)"
            >
              编辑
            </el-button>
            
            <el-button
              v-if="showDelete"
              link
              type="danger"
              size="small"
              @click="handleDelete(scope.row)"
            >
              删除
            </el-button>
            
            <el-button
              v-if="showView"
              link
              type="info"
              size="small"
              @click="handleView(scope.row)"
            >
              查看
            </el-button>
          </slot>
        </template>
      </el-table-column>
    </el-table>

    <!-- 分页 -->
    <div
      v-if="showPagination"
      class="table-pagination"
    >
      <el-pagination
        v-model:current-page="pagination.current"
        v-model:page-size="pagination.size"
        :total="pagination.total"
        :page-sizes="pageSizes"
        :layout="paginationLayout"
        style="margin-top: 16px; justify-content: flex-end"
        @size-change="handleSizeChange"
        @current-change="handleCurrentChange"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import { Search, Refresh, Plus } from '@element-plus/icons-vue'
import type { TableColumnCtx } from 'element-plus'

export interface TableColumn {
  prop: string
  label: string
  width?: number
  minWidth?: number
  align?: 'left' | 'center' | 'right'
  sortable?: boolean | 'custom'
  fixed?: boolean | 'left' | 'right'
  showOverflowTooltip?: boolean
  formatter?: (row: any, column: TableColumnCtx<any>, cellValue: any, index: number) => string
  tag?: boolean
  tagMap?: Record<string | number, { type: string; text: string }>
  tagEffect?: 'dark' | 'light' | 'plain'
}

export interface PaginationConfig {
  current: number
  size: number
  total: number
}

interface Props {
  // 数据相关
  data: any[]
  loading?: boolean
  rowKey?: string
  
  // 列配置
  columns: TableColumn[]
  
  // 功能开关
  showHeader?: boolean
  showSearch?: boolean
  showRefresh?: boolean
  showAdd?: boolean
  showSelection?: boolean
  showIndex?: boolean
  showActions?: boolean
  showEdit?: boolean
  showDelete?: boolean
  showView?: boolean
  showPagination?: boolean
  
  // 表格样式
  border?: boolean
  stripe?: boolean
  height?: string | number
  maxHeight?: string | number
  
  // 分页配置
  pagination?: PaginationConfig
  pageSizes?: number[]
  paginationLayout?: string
  
  // 操作列配置
  actionsWidth?: string | number
  actionsFixed?: boolean | 'left' | 'right'
}

const props = withDefaults(defineProps<Props>(), {
  // 数据相关
  data: () => [],
  loading: false,
  rowKey: 'id',
  
  // 功能开关
  showHeader: true,
  showSearch: true,
  showRefresh: true,
  showAdd: true,
  showSelection: false,
  showIndex: false,
  showActions: true,
  showEdit: true,
  showDelete: true,
  showView: false,
  showPagination: true,
  
  // 表格样式
  border: true,
  stripe: true,
  height: undefined,
  maxHeight: undefined,
  
  // 分页配置
  pagination: () => ({
    current: 1,
    size: 10,
    total: 0
  }),
  pageSizes: () => [10, 20, 50, 100],
  paginationLayout: 'total, sizes, prev, pager, next, jumper',
  
  // 操作列配置
  actionsWidth: 200,
  actionsFixed: 'right'
})

const emit = defineEmits<{
  search: [value: string]
  refresh: []
  add: []
  edit: [row: any]
  delete: [row: any]
  view: [row: any]
  selectionChange: [selection: any[]]
  sortChange: [sort: any]
  sizeChange: [size: number]
  currentChange: [current: number]
}>()

const searchValue = ref('')
const tableData = ref<any[]>([])
const selection = ref<any[]>([])

// 计算属性
const pagination = computed(() => props.pagination)

// 监听数据变化
watch(() => props.data, (newData) => {
  tableData.value = newData
}, { immediate: true })

// 事件处理
const handleSearch = () => {
  emit('search', searchValue.value)
}

const handleRefresh = () => {
  emit('refresh')
}

const handleAdd = () => {
  emit('add')
}

const handleEdit = (row: any) => {
  emit('edit', row)
}

const handleDelete = (row: any) => {
  emit('delete', row)
}

const handleView = (row: any) => {
  emit('view', row)
}

const handleSelectionChange = (val: any[]) => {
  selection.value = val
  emit('selectionChange', val)
}

const handleSortChange = (sort: any) => {
  emit('sortChange', sort)
}

const handleSizeChange = (size: number) => {
  emit('sizeChange', size)
}

const handleCurrentChange = (current: number) => {
  emit('currentChange', current)
}

// 工具函数
const getTagType = (value: any, tagMap?: Record<string | number, { type: string; text: string }>) => {
  if (!tagMap) return ''
  return tagMap[value]?.type || ''
}

const getTagText = (value: any, tagMap?: Record<string | number, { type: string; text: string }>) => {
  if (!tagMap) return value
  return tagMap[value]?.text || value
}

// 暴露方法给父组件
defineExpose({
  clearSelection: () => {
    selection.value = []
  },
  getSelection: () => selection.value
})
</script>

<style scoped lang="scss">
.data-table-container {
  width: 100%;
}

.table-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
  flex-wrap: wrap;
  gap: 16px;

  .header-left,
  .header-right {
    display: flex;
    align-items: center;
    gap: 12px;
    flex-wrap: wrap;
  }
}

.table-pagination {
  display: flex;
  justify-content: flex-end;
}

@media (max-width: 768px) {
  .table-header {
    flex-direction: column;
    align-items: stretch;
    gap: 12px;

    .header-left,
    .header-right {
      justify-content: center;
    }
  }
}
</style>