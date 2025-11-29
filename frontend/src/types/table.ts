import type { ColumnProps } from 'element-plus'

/**
 * 表格排序方向
 */
export type SortDirection = 'ascending' | 'descending' | null;

/**
 * 表格列类型
 */
export interface TableColumn extends Partial<ColumnProps> {
  // 列唯一标识
  key?: string;
  // 列标题
  title: string;
  // 数据字段
  prop?: string;
  // 列宽
  width?: number | string;
  // 最小宽度
  minWidth?: number | string;
  // 是否可排序
  sortable?: boolean;
  // 是否固定
  fixed?: boolean | 'left' | 'right';
  // 对齐方式
  align?: 'left' | 'center' | 'right';
  // 格式化函数，用于自定义单元格显示内容
  formatter?: (row: any, column: any, cellValue: any, index: number) => string | VNode;
  // 自定义渲染函数
  renderCell?: (row: any, column: any, index: number) => string | VNode;
  // 是否可筛选
  filterable?: boolean;
  // 筛选选项配置
  filters?: Array<{
    text: string;
    value: any;
  }>;
  // 筛选方法
  filterMethod?: (value: any, row: any, column: any) => boolean;
  // 筛选重置方法
  filterResetMethod?: () => void;
  // 是否隐藏列
  hidden?: boolean;
  // 权限码，用于控制列的显示
  permission?: string;
  // 是否可编辑
  editable?: boolean;
  // 自定义组件配置
  component?: {
    // 组件名称
    name: string;
    // 组件属性
    props?: Record<string, any>;
    // 组件事件
    events?: Record<string, Function>;
  };
  // 子列配置
  children?: TableColumn[];
  // 列操作配置
  actions?: Array<{
    // 操作名称
    name: string;
    // 操作类型
    type?: 'primary' | 'success' | 'warning' | 'danger' | 'info';
    // 操作图标
    icon?: string;
    // 是否显示
    show?: boolean | ((row?: any) => boolean);
    // 是否禁用
    disabled?: boolean | ((row?: any) => boolean);
    // 点击事件处理函数
    onClick?: (row: any, index: number) => void;
    // 权限码
    permission?: string;
  }>;
}

/**
 * 表格分页配置
 */
export interface TablePagination {
  // 当前页码
  currentPage: number;
  // 每页条数
  pageSize: number;
  // 总条数
  total: number;
  // 可切换的每页条数
  pageSizes?: number[];
  // 是否显示总数
  showTotal?: boolean;
  // 是否显示分页器
  showPagination?: boolean;
}

/**
 * 表格选择配置
 */
export interface TableSelection {
  // 是否显示复选框
  showSelection?: boolean;
  // 是否支持全选
  selectable?: boolean | ((row: any, index: number) => boolean);
  // 是否支持多选
  multiple?: boolean;
}

/**
 * 表格操作配置
 */
export interface TableAction {
  // 操作名称
  name: string;
  // 操作类型
  type?: 'primary' | 'success' | 'warning' | 'danger' | 'info';
  // 操作图标
  icon?: string;
  // 是否显示
  show?: boolean | ((row?: any) => boolean);
  // 是否禁用
  disabled?: boolean | ((row?: any) => boolean);
  // 点击事件处理函数
  onClick?: (row: any, index: number) => void;
  // 权限码
  permission?: string;
  // 确认对话框配置
  confirm?: {
    title?: string;
    message?: string;
    type?: 'success' | 'warning' | 'info' | 'error';
  };
}

/**
 * 表格配置
 */
export interface TableConfig {
  // 表格列配置
  columns: TableColumn[];
  // 表格属性配置
  tableProps?: Partial<{
    // 数据加载状态
    loading: boolean;
    // 表格高度
    height: string | number;
    // 是否显示斑马纹
    stripe: boolean;
    // 是否显示边框
    border: boolean;
    // 行唯一标识
    rowKey: string | ((row: any) => string);
    // 空状态文本
    emptyText: string;
    // 最大高度
    maxHeight: string | number;
  }>;
  // 分页配置
  pagination?: TablePagination;
  // 选择配置
  selection?: TableSelection;
  // 操作按钮配置
  actions?: TableAction[];
  // 是否显示序号
  showIndex?: boolean;
  // 序号列标题
  indexHeader?: string;
}

// 为了TypeScript兼容，声明VNode类型
declare global {
  interface VNode {
    // Vue VNode接口的最小声明
  }
}