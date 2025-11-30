<template>
  <div class="virtual-scroll-table-container">
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

    <!-- 虚拟滚动容器 -->
    <div 
      ref="scrollWrapperRef"
      class="virtual-scroll-wrapper"
      :style="{ height: tableHeight }"
      @scroll="handleScroll"
    >
      <!-- 表头固定 -->
      <div class="table-header-wrapper">
        <el-table 
          :data="[]" 
          :border="border"
          style="width: 100%"
          :header-row-style="{ backgroundColor: '#f5f7fa' }"
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
            />
          </template>

          <!-- 操作列 -->
          <el-table-column
            v-if="showActions"
            label="操作"
            :width="actionsWidth"
            :fixed="actionsFixed"
            align="center"
          />
        </el-table>
      </div>

      <!-- 虚拟滚动内容区域 -->
      <div 
        class="scroll-content"
        :style="{ height: totalHeight + 'px' }"
      >
        <!-- 占位区域 -->
        <div :style="{ height: startOffset + 'px' }" />

        <!-- 可见行 -->
        <div class="visible-data">
          <table
            class="virtual-table"
            :style="{ width: tableWidth }"
          >
            <tbody>
              <tr 
                v-for="(row, index) in visibleData" 
                :key="getRowKey(row)"
                :class="{ 'hover-row': hoveredRowIndex === startIndex + index }"
                @mouseenter="hoveredRowIndex = startIndex + index"
                @mouseleave="hoveredRowIndex = -1"
              >
                <!-- 选择列 -->
                <td
                  v-if="showSelection"
                  class="selection-cell"
                  style="width: 55px; text-align: center;"
                >
                  <el-checkbox 
                    v-model="row._selected" 
                    :indeterminate="row._indeterminate"
                    @change="handleRowSelection(row, $event)"
                  />
                </td>

                <!-- 序号列 -->
                <td
                  v-if="showIndex"
                  class="index-cell"
                  style="width: 80px; text-align: center;"
                >
                  {{ startIndex + index + 1 }}
                </td>

                <!-- 数据列 -->
                <template
                  v-for="column in columns"
                  :key="column.prop"
                >
                  <td 
                    :style="getCellStyle(column)"
                    class="data-cell"
                  >
                    <slot
                      :name="column.prop"
                      :row="row"
                      :$index="startIndex + index"
                    >
                      <!-- 格式化显示 -->
                      <template v-if="column.formatter">
                        {{ column.formatter(row, null, row[column.prop], startIndex + index) }}
                      </template>
                       
                      <!-- 状态标签 -->
                      <template v-else-if="column.tag">
                        <el-tag
                          :type="getTagType(row[column.prop], column.tagMap)"
                          :effect="column.tagEffect || 'light'"
                          size="small"
                        >
                          {{ getTagText(row[column.prop], column.tagMap) }}
                        </el-tag>
                      </template>
                       
                      <!-- 默认显示 -->
                      <template v-else>
                        <div
                          v-if="column.showOverflowTooltip"
                          class="tooltip-cell"
                        >
                          {{ row[column.prop] }}
                        </div>
                        <template v-else>
                          {{ row[column.prop] }}
                        </template>
                      </template>
                    </slot>
                  </td>
                </template>

                <!-- 操作列 -->
                <td 
                  v-if="showActions" 
                  class="action-cell" 
                  :style="{ width: actionsWidth + 'px', textAlign: 'center' }"
                >
                  <slot
                    name="actions"
                    :row="row"
                    :$index="startIndex + index"
                  >
                    <el-button
                      v-if="showEdit"
                      link
                      type="primary"
                      size="small"
                      @click="handleEdit(row)"
                    >
                      编辑
                    </el-button>
                    
                    <el-button
                      v-if="showDelete"
                      link
                      type="danger"
                      size="small"
                      @click="handleDelete(row)"
                    >
                      删除
                    </el-button>
                    
                    <el-button
                      v-if="showView"
                      link
                      type="info"
                      size="small"
                      @click="handleView(row)"
                    >
                      查看
                    </el-button>
                  </slot>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- 加载状态 -->
    <div
      v-if="loading"
      class="loading-overlay"
    >
      <el-loading
        target=".table-data-wrapper"
        text="加载中..."
      />
    </div>

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
import { ref, watch, computed, nextTick, onMounted, onUnmounted } from 'vue'
import { Search, Refresh, Plus } from '@element-plus/icons-vue'

// 类型定义
export interface TableColumn {
  prop: string
  label: string
  width?: number
  minWidth?: number
  align?: 'left' | 'center' | 'right'
  sortable?: boolean | 'custom'
  fixed?: boolean | 'left' | 'right'
  showOverflowTooltip?: boolean
  formatter?: (row: any, column: any, cellValue: any, index: number) => string
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
  
  // 虚拟滚动配置
  rowHeight?: number
  bufferRows?: number
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
  height: '400px',
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
  actionsFixed: 'right',
  
  // 虚拟滚动配置
  rowHeight: 48,
  bufferRows: 5
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

// 响应式数据
const searchValue = ref('')
const tableData = ref<any[]>([])
const selection = ref<any[]>([])
const scrollTop = ref(0)
const startIndex = ref(0)
const endIndex = ref(0)
const hoveredRowIndex = ref(-1)
const scrollWrapperRef = ref<HTMLElement>()
const isComputing = ref(false)
const selectionMap = ref<Map<string, boolean>>(new Map())

// 计算属性
const pagination = computed(() => props.pagination)

const tableHeight = computed(() => {
  if (props.height) return props.height
  if (props.maxHeight) return props.maxHeight
  return '400px'
})

const tableWidth = computed(() => {
  // 计算表格总宽度
  let totalWidth = 0
  if (props.showSelection) totalWidth += 55
  if (props.showIndex) totalWidth += 80
  
  // 计算列宽
  props.columns.forEach(column => {
    if (column.width) {
      totalWidth += column.width
    } else if (column.minWidth) {
      totalWidth += column.minWidth
    } else {
      totalWidth += 180 // 默认宽度
    }
  })
  
  if (props.showActions) {
    totalWidth += typeof props.actionsWidth === 'string' ? parseInt(props.actionsWidth) : props.actionsWidth
  }
  
  return totalWidth + 'px'
})

const totalHeight = computed(() => {
  return tableData.value.length * props.rowHeight
})

const visibleRowCount = computed(() => {
  if (!scrollWrapperRef.value) return 10
  // 计算可见行数
  const wrapperHeight = scrollWrapperRef.value.clientHeight - 48 // 减去表头高度
  return Math.ceil(wrapperHeight / props.rowHeight)
})

const startOffset = computed(() => {
  return startIndex.value * props.rowHeight
})

const visibleData = computed(() => {
  return tableData.value.slice(startIndex.value, endIndex.value)
})

// 监听数据变化
watch(() => props.data, (newData) => {
  // 深拷贝数据并添加选中状态属性
  tableData.value = newData.map(item => ({
    ...item,
    _selected: false,
    _indeterminate: false
  }))
  
  // 更新选中状态
  updateSelectionStatus()
  
  // 重新计算虚拟滚动
  nextTick(() => {
    updateVisibleRange()
  })
}, { immediate: true })

// 方法
const getRowKey = (row: any) => {
  return row[props.rowKey] || Math.random()
}

const getCellStyle = (column: TableColumn) => {
  const style: any = {
    textAlign: column.align || 'center'
  }
  
  if (column.width) {
    style.width = column.width + 'px'
  } else if (column.minWidth) {
    style.minWidth = column.minWidth + 'px'
  }
  
  return style
}

const getTagType = (value: any, tagMap?: Record<string | number, { type: string; text: string }>) => {
  if (!tagMap) return ''
  return tagMap[value]?.type || ''
}

const getTagText = (value: any, tagMap?: Record<string | number, { type: string; text: string }>) => {
  if (!tagMap) return value
  return tagMap[value]?.text || value
}

const updateVisibleRange = () => {
  if (isComputing.value) return
  isComputing.value = true
  
  try {
    const scrollTop = scrollWrapperRef.value?.scrollTop || 0
    const newStartIndex = Math.max(0, Math.floor(scrollTop / props.rowHeight) - props.bufferRows)
    const newEndIndex = Math.min(
      tableData.value.length,
      newStartIndex + visibleRowCount.value + props.bufferRows * 2
    )
    
    startIndex.value = newStartIndex
    endIndex.value = newEndIndex
  } finally {
    isComputing.value = false
  }
}

const handleScroll = () => {
  requestAnimationFrame(() => {
    updateVisibleRange()
  })
}

const handleRowSelection = (row: any, selected: boolean) => {
  row._selected = selected
  const rowKey = getRowKey(row)
  
  if (selected) {
    selectionMap.value.set(rowKey, true)
  } else {
    selectionMap.value.delete(rowKey)
  }
  
  updateSelectionArray()
}

const updateSelectionStatus = () => {
  tableData.value.forEach(row => {
    const rowKey = getRowKey(row)
    row._selected = selectionMap.value.has(rowKey)
  })
}

const updateSelectionArray = () => {
  selection.value = tableData.value.filter(row => row._selected)
  emit('selectionChange', selection.value)
}

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

const handleSortChange = (sort: any) => {
  emit('sortChange', sort)
}

const handleSizeChange = (size: number) => {
  emit('sizeChange', size)
}

const handleCurrentChange = (current: number) => {
  emit('currentChange', current)
}

// 生命周期
onMounted(() => {
  nextTick(() => {
    updateVisibleRange()
    // 添加窗口大小变化监听，重新计算可见范围
    window.addEventListener('resize', updateVisibleRange)
  })
})

onUnmounted(() => {
  window.removeEventListener('resize', updateVisibleRange)
})

// 暴露方法给父组件
defineExpose({
  clearSelection: () => {
    selectionMap.value.clear()
    updateSelectionStatus()
    updateSelectionArray()
  },
  getSelection: () => selection.value,
  updateVisibleRange
})
</script>

<style scoped lang="scss">
.virtual-scroll-table-container {
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

.virtual-scroll-wrapper {
  overflow: auto;
  border: 1px solid #ebeef5;
  border-radius: 4px;
  position: relative;
}

.table-header-wrapper {
  background-color: #f5f7fa;
  border-bottom: 1px solid #ebeef5;
  position: sticky;
  top: 0;
  z-index: 10;
}

.scroll-content {
  position: relative;
}

.visible-data {
  position: relative;
}

.virtual-table {
  width: 100%;
  border-collapse: collapse;
  table-layout: fixed;
}

.virtual-table tr {
  height: v-bind('props.rowHeight + "px"');
  border-bottom: 1px solid #ebeef5;
  transition: background-color 0.2s;
}

.virtual-table tr:hover,
.virtual-table tr.hover-row {
  background-color: #f5f7fa;
}

.virtual-table td {
  padding: 12px;
  border-right: 1px solid #ebeef5;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.virtual-table td:last-child {
  border-right: none;
}

.tooltip-cell {
  position: relative;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.loading-overlay {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  z-index: 20;
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
  
  .virtual-table td {
    padding: 8px;
    font-size: 14px;
  }
}
</style>