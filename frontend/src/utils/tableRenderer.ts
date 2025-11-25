import { ref, computed, watch, nextTick } from 'vue'

/**
 * 表格渲染性能优化工具
 */
export class TableRenderer {
  /**
   * 创建响应式的表格数据处理对象
   * @param initialData 初始数据
   * @param options 配置选项
   * @returns 表格数据处理对象
   */
  static createTableData<T extends Record<string, any>>(initialData: T[] = [], options: TableDataOptions = {}) {
    const { 
      pageSize = 10, 
      enablePagination = true,
      enableSorting = true,
      enableFiltering = true,
      defaultSortField = '',
      defaultSortOrder = 'asc'
    } = options

    // 响应式数据
    const rawData = ref<T[]>(initialData)
    const currentPage = ref(1)
    const pageSizeRef = ref(pageSize)
    const sortField = ref(defaultSortField)
    const sortOrder = ref<'asc' | 'desc'>(defaultSortOrder)
    const searchKeyword = ref('')
    const filters = ref<Record<string, any>>({})
    const loading = ref(false)

    // 计算属性
    const filteredData = computed(() => {
      let data = [...rawData.value]

      // 搜索过滤
      if (enableFiltering && searchKeyword.value) {
        const keyword = searchKeyword.value.toLowerCase()
        data = data.filter(item => {
          // 默认搜索所有可搜索字段
          return Object.values(item).some(value => {
            if (value === null || value === undefined) return false
            return String(value).toLowerCase().includes(keyword)
          })
        })
      }

      // 自定义过滤
      if (enableFiltering && Object.keys(filters.value).length > 0) {
        data = data.filter(item => {
          return Object.entries(filters.value).every(([key, filterValue]) => {
            // 如果过滤值为null或undefined，则不过滤该字段
            if (filterValue === null || filterValue === undefined) return true
            
            // 如果过滤值是数组，则检查是否包含
            if (Array.isArray(filterValue)) {
              return filterValue.includes(item[key])
            }
            
            // 直接比较
            return item[key] === filterValue
          })
        })
      }

      return data
    })

    const sortedData = computed(() => {
      if (!enableSorting || !sortField.value) return filteredData.value

      const data = [...filteredData.value]
      return data.sort((a, b) => {
        const aValue = a[sortField.value]
        const bValue = b[sortField.value]

        // 处理null和undefined
        if (aValue === null || aValue === undefined) return 1
        if (bValue === null || bValue === undefined) return -1

        // 根据类型进行排序
        if (typeof aValue === 'string' && typeof bValue === 'string') {
          return sortOrder.value === 'asc' 
            ? aValue.localeCompare(bValue) 
            : bValue.localeCompare(aValue)
        } else if (typeof aValue === 'number' && typeof bValue === 'number') {
          return sortOrder.value === 'asc' 
            ? aValue - bValue 
            : bValue - aValue
        } else if (aValue instanceof Date && bValue instanceof Date) {
          return sortOrder.value === 'asc' 
            ? aValue.getTime() - bValue.getTime() 
            : bValue.getTime() - aValue.getTime()
        }

        // 默认比较
        return 0
      })
    })

    const paginatedData = computed(() => {
      if (!enablePagination) return sortedData.value

      const start = (currentPage.value - 1) * pageSizeRef.value
      const end = start + pageSizeRef.value
      return sortedData.value.slice(start, end)
    })

    const total = computed(() => sortedData.value.length)
    const totalPages = computed(() => {
      if (!enablePagination) return 1
      return Math.ceil(total.value / pageSizeRef.value)
    })

    // 方法
    const updateData = (newData: T[]) => {
      // 使用批量赋值而不是逐个修改，提高性能
      rawData.value = [...newData]
      // 重置到第一页
      currentPage.value = 1
    }

    const setSearchKeyword = (keyword: string) => {
      searchKeyword.value = keyword
      currentPage.value = 1 // 搜索时重置到第一页
    }

    const setFilter = (key: string, value: any) => {
      filters.value[key] = value
      currentPage.value = 1 // 过滤时重置到第一页
    }

    const clearFilters = () => {
      filters.value = {}
      currentPage.value = 1
    }

    const setSort = (field: string, order: 'asc' | 'desc') => {
      sortField.value = field
      sortOrder.value = order
    }

    const setPageSize = (size: number) => {
      pageSizeRef.value = size
      currentPage.value = 1 // 更改页大小时重置到第一页
    }

    const setCurrentPage = (page: number) => {
      currentPage.value = Math.max(1, Math.min(page, totalPages.value))
    }

    const prevPage = () => {
      if (currentPage.value > 1) {
        currentPage.value--
      }
    }

    const nextPage = () => {
      if (currentPage.value < totalPages.value) {
        currentPage.value++
      }
    }

    const reset = () => {
      currentPage.value = 1
      searchKeyword.value = ''
      filters.value = {}
      sortField.value = defaultSortField
      sortOrder.value = defaultSortOrder
    }

    return {
      // 数据
      data: paginatedData,
      rawData,
      loading,
      
      // 分页信息
      currentPage,
      pageSize: pageSizeRef,
      total,
      totalPages,
      
      // 排序信息
      sortField,
      sortOrder,
      
      // 过滤信息
      searchKeyword,
      filters,
      
      // 方法
      updateData,
      setSearchKeyword,
      setFilter,
      clearFilters,
      setSort,
      setPageSize,
      setCurrentPage,
      prevPage,
      nextPage,
      reset
    }
  }

  /**
   * 创建表格列配置优化器
   * @param columns 原始列配置
   * @returns 优化后的列配置和操作方法
   */
  static createColumnOptimizer<T extends TableColumn>(columns: T[]) {
    const columnState = ref<Record<string, { visible: boolean; width: number | null }>>({})
    const visibleColumns = ref<T[]>([...columns])

    // 初始化列状态
    columns.forEach(col => {
      columnState.value[col.prop] = {
        visible: col.visible !== false,
        width: col.width || null
      }
    })

    // 更新可见列
    const updateVisibleColumns = () => {
      visibleColumns.value = columns.filter(col => 
        columnState.value[col.prop]?.visible !== false
      )
    }

    const toggleColumnVisibility = (prop: string) => {
      const state = columnState.value[prop]
      if (state) {
        state.visible = !state.visible
        updateVisibleColumns()
      }
    }

    const setColumnWidth = (prop: string, width: number) => {
      const state = columnState.value[prop]
      if (state) {
        state.width = width
      }
    }

    const resetColumns = () => {
      columns.forEach(col => {
        columnState.value[col.prop] = {
          visible: col.visible !== false,
          width: col.width || null
        }
      })
      updateVisibleColumns()
    }

    return {
      columns: visibleColumns,
      columnState,
      toggleColumnVisibility,
      setColumnWidth,
      resetColumns
    }
  }

  /**
   * 创建表格行数据缓存器
   * 用于缓存行数据的计算结果，避免重复计算
   */
  static createRowCache<T extends Record<string, any>>() {
    const cache = new Map<string, any>()
    const cacheSize = ref(0)
    const maxCacheSize = 1000 // 最大缓存条数

    const getCacheKey = (row: T, key: string) => {
      return `${row.id || row._uid || JSON.stringify(row)}:${key}`
    }

    const get = (row: T, key: string): any | null => {
      const cacheKey = getCacheKey(row, key)
      return cache.get(cacheKey)
    }

    const set = (row: T, key: string, value: any) => {
      // 如果缓存过大，清除部分缓存
      if (cacheSize.value >= maxCacheSize) {
        // 清除20%的缓存
        const deleteCount = Math.floor(cacheSize.value * 0.2)
        let deleted = 0
        
        // 遍历并删除前N个条目
        for (const [cacheKey] of cache.entries()) {
          cache.delete(cacheKey)
          cacheSize.value--
          deleted++
          if (deleted >= deleteCount) break
        }
      }

      const cacheKey = getCacheKey(row, key)
      cache.set(cacheKey, value)
      cacheSize.value = cache.size
    }

    const has = (row: T, key: string): boolean => {
      const cacheKey = getCacheKey(row, key)
      return cache.has(cacheKey)
    }

    const clear = () => {
      cache.clear()
      cacheSize.value = 0
    }

    const clearRow = (row: T) => {
      const rowId = row.id || row._uid || JSON.stringify(row)
      
      // 清除与该行相关的所有缓存
      for (const [cacheKey] of cache.entries()) {
        if (cacheKey.startsWith(rowId + ':')) {
          cache.delete(cacheKey)
          cacheSize.value--
        }
      }
    }

    return {
      get,
      set,
      has,
      clear,
      clearRow,
      cacheSize
    }
  }

  /**
   * 批量格式化表格数据
   * @param data 原始数据
   * @param formatters 格式化器配置
   * @returns 格式化后的数据
   */
  static formatTableData<T extends Record<string, any>>(
    data: T[],
    formatters: Record<string, (value: any, row: T, index: number) => any>
  ): T[] {
    // 使用map创建新数组而不是修改原数组
    return data.map((row, index) => {
      const formattedRow = { ...row }
      
      // 批量应用格式化器
      Object.entries(formatters).forEach(([field, formatter]) => {
        if (field in row) {
          formattedRow[field] = formatter(row[field], row, index)
        }
      })
      
      return formattedRow
    })
  }

  /**
   * 防抖处理搜索输入
   * @param callback 搜索回调
   * @param delay 延迟时间（毫秒）
   * @returns 防抖后的函数
   */
  static debounceSearch(callback: (keyword: string) => void, delay: number = 300) {
    let timeoutId: number | null = null

    return (keyword: string) => {
      if (timeoutId !== null) {
        clearTimeout(timeoutId)
      }
      
      timeoutId = window.setTimeout(() => {
        callback(keyword)
      }, delay)
    }
  }
}

// 类型定义
export interface TableDataOptions {
  pageSize?: number
  enablePagination?: boolean
  enableSorting?: boolean
  enableFiltering?: boolean
  defaultSortField?: string
  defaultSortOrder?: 'asc' | 'desc'
}

export interface TableColumn {
  prop: string
  label: string
  width?: number
  minWidth?: number
  align?: 'left' | 'center' | 'right'
  sortable?: boolean | 'custom'
  fixed?: boolean | 'left' | 'right'
  showOverflowTooltip?: boolean
  visible?: boolean
  [key: string]: any
}

/**
 * 创建高效的表格行key生成器
 * 用于优化表格行渲染性能
 */
export const createRowKeyGenerator = () => {
  const keyMap = new Map<any, string>()
  let counter = 0

  return (row: any): string => {
    // 如果有唯一标识，则优先使用
    if (row.id !== undefined && row.id !== null) {
      return `row_${row.id}`
    }
    
    // 如果有_uid属性（Vue内部使用），则使用它
    if (row._uid !== undefined) {
      return `row_uid_${row._uid}`
    }
    
    // 对于没有明确标识的对象，使用缓存映射
    // 注意：这不是理想做法，但在无法获取唯一ID时作为备选
    if (!keyMap.has(row)) {
      keyMap.set(row, `row_${++counter}`)
    }
    
    return keyMap.get(row)!
  }
}

/**
 * 优化表格列渲染，避免不必要的重渲染
 * @param columns 原始列配置
 * @returns 优化后的列配置
 */
export const optimizeColumns = <T extends TableColumn>(columns: T[]): T[] => {
  // 移除空的列配置
  return columns.filter(col => col && col.prop)
    // 优化宽度配置
    .map(col => {
      // 确保宽度配置合理
      if (col.width && (typeof col.width !== 'number' || col.width < 50)) {
        return { ...col, width: 120 }
      }
      return col
    })
}

/**
 * 计算表格理想的列宽
 * @param content 单元格内容
 * @param minWidth 最小宽度
 * @param maxWidth 最大宽度
 * @returns 建议的列宽
 */
export const calculateOptimalColumnWidth = (
  content: string, 
  minWidth: number = 80,
  maxWidth: number = 500
): number => {
  // 创建临时canvas用于测量文本宽度
  const canvas = document.createElement('canvas')
  const context = canvas.getContext('2d')
  
  if (!context) return minWidth
  
  // 设置与表格相同的字体样式
  context.font = '14px -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif'
  
  // 测量文本宽度
  const textWidth = context.measureText(content).width
  
  // 添加一些padding
  const width = textWidth + 24 // 12px padding on each side
  
  // 限制在min和max之间
  return Math.max(minWidth, Math.min(width, maxWidth))
}