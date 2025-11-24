import type { FormConfig } from './form';
import type { TableColumn } from './table';

/**
 * CRUD操作类型
 */
export enum CrudAction {
  CREATE = 'create',
  READ = 'read',
  UPDATE = 'update',
  DELETE = 'delete',
  BATCH_DELETE = 'batch-delete',
  IMPORT = 'import',
  EXPORT = 'export',
  VIEW = 'view'
}

/**
 * API配置接口
 */
export interface ApiConfig {
  // 获取列表接口
  list: string;
  // 获取详情接口
  detail?: string;
  // 创建接口
  create?: string;
  // 更新接口
  update?: string;
  // 删除接口
  delete?: string;
  // 批量删除接口
  batchDelete?: string;
  // 导入接口
  import?: string;
  // 导出接口
  export?: string;
}

/**
 * 操作按钮配置接口
 */
export interface ActionButtonConfig {
  // 按钮名称
  name: string;
  // 按钮类型
  type: string;
  // 按钮图标
  icon?: string;
  // 是否显示
  show?: boolean | ((row?: any) => boolean);
  // 是否禁用
  disabled?: boolean | ((row?: any) => boolean);
  // 点击事件处理函数
  onClick?: (row?: any) => void;
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
 * CRUD配置接口
 */
export interface CrudConfig {
  // 实体名称
  entityName: string;
  // API配置
  api: ApiConfig;
  // 表格配置
  table: {
    // 表格列配置
    columns: TableColumn[];
    // 是否显示分页
    showPagination?: boolean;
    // 是否显示选择框
    showSelection?: boolean;
    // 是否显示序号
    showIndex?: boolean;
    // 是否显示搜索框
    showSearch?: boolean;
    // 是否显示操作列
    showActions?: boolean;
    // 行唯一标识字段
    rowKey?: string;
    // 搜索表单配置
    searchFormConfig?: FormConfig;
    // 初始排序配置
    initialSort?: {
      prop: string;
      order: 'ascending' | 'descending';
    };
  };
  // 表单配置
  form: {
    // 创建表单配置
    createFormConfig?: FormConfig;
    // 编辑表单配置
    updateFormConfig?: FormConfig;
    // 查看表单配置
    viewFormConfig?: FormConfig;
    // 对话框宽度
    dialogWidth?: string;
    // 是否全屏显示对话框
    fullscreen?: boolean;
  };
  // 操作按钮配置
  actions: {
    // 工具栏按钮配置
    toolbar: ActionButtonConfig[];
    // 操作列按钮配置
    tableRow: ActionButtonConfig[];
    // 支持的批量操作
    batchActions: string[];
    // 启用的标准操作
    enabledActions: CrudAction[];
  };
  // 数据处理配置
  data: {
    // 响应数据映射
    responseMap?: {
      // 列表字段名
      list?: string;
      // 总数字段名
      total?: string;
      // 页码字段名
      pageNum?: string;
      // 页大小字段名
      pageSize?: string;
    };
    // 请求参数映射
    requestMap?: {
      // 页码参数名
      pageNum?: string;
      // 页大小参数名
      pageSize?: string;
      // 排序字段参数名
      sortField?: string;
      // 排序方向参数名
      sortOrder?: string;
    };
    // 格式化函数
    formatters?: {
      // 请求参数格式化
      requestFormatter?: (params: any) => any;
      // 响应数据格式化
      responseFormatter?: (data: any) => any;
    };
  };
}

/**
 * CRUD页面上下文接口
 */
export interface CrudContext {
  // 加载状态
  loading: boolean;
  // 数据列表
  dataList: any[];
  // 搜索参数
  searchParams: any;
  // 分页参数
  pagination: {
    currentPage: number;
    pageSize: number;
    total: number;
  };
  // 排序参数
  sort: {
    prop: string;
    order: string;
  };
  // 选中的行
  selectedRows: any[];
  // 对话框显示状态
  dialogVisible: {
    create: boolean;
    update: boolean;
    view: boolean;
    import: boolean;
  };
  // 当前操作的数据
  currentData: any;
  // 表单实例引用
  formRef: any;
  // 表格实例引用
  tableRef: any;
}

/**
 * CRUD操作结果接口
 */
export interface CrudResult {
  success: boolean;
  message?: string;
  data?: any;
  error?: any;
}