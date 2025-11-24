import api from './api';
import { ElMessage } from 'element-plus';
import type { CrudConfig, ApiConfig } from '../types/crud';
import type { TableColumn } from '../types/table';
import type { FormConfig, FormField } from '../types/form';

/**
 * CRUD服务类
 */
export class CrudService<T = any> {
  private config: CrudConfig;
  
  constructor(config: CrudConfig) {
    this.config = config;
  }
  
  /**
   * 获取列表数据
   * @param params 查询参数
   * @returns 列表数据
   */
  async getList(params: any = {}): Promise<any> {
    try {
      const response = await api.get(this.config.api.list, { params });
      return response.data;
    } catch (error) {
      console.error('获取列表数据失败:', error);
      ElMessage.error('获取数据失败');
      throw error;
    }
  }
  
  /**
   * 获取详情数据
   * @param id 数据ID
   * @returns 详情数据
   */
  async getDetail(id: any): Promise<T> {
    if (!this.config.api.detail) {
      throw new Error('未配置详情API');
    }
    
    try {
      const response = await api.get(this.config.api.detail!.replace(':id', String(id)), { params: { id } });
      return response.data;
    } catch (error) {
      console.error('获取详情数据失败:', error);
      ElMessage.error('获取详情失败');
      throw error;
    }
  }
  
  /**
   * 创建数据
   * @param data 创建数据
   * @returns 创建结果
   */
  async create(data: Partial<T>): Promise<T> {
    if (!this.config.api.create) {
      throw new Error('未配置创建API');
    }
    
    try {
      const response = await api.post(this.config.api.create!, data);
      ElMessage.success('创建成功');
      return response.data;
    } catch (error) {
      console.error('创建数据失败:', error);
      ElMessage.error('创建失败');
      throw error;
    }
  }
  
  /**
   * 更新数据
   * @param id 数据ID
   * @param data 更新数据
   * @returns 更新结果
   */
  async update(id: any, data: Partial<T>): Promise<T> {
    if (!this.config.api.update) {
      throw new Error('未配置更新API');
    }
    
    try {
      const response = await api.put(this.config.api.update!.replace(':id', String(id)), data);
      ElMessage.success('更新成功');
      return response.data;
    } catch (error) {
      console.error('更新数据失败:', error);
      ElMessage.error('更新失败');
      throw error;
    }
  }
  
  /**
   * 删除数据
   * @param id 数据ID
   * @returns 删除结果
   */
  async delete(id: any): Promise<void> {
    if (!this.config.api.delete) {
      throw new Error('未配置删除API');
    }
    
    try {
      await api.delete(this.config.api.delete!.replace(':id', String(id)), { params: { id } });
      ElMessage.success('删除成功');
    } catch (error) {
      console.error('删除数据失败:', error);
      ElMessage.error('删除失败');
      throw error;
    }
  }
  
  /**
   * 批量删除数据
   * @param ids 数据ID数组
   * @returns 删除结果
   */
  async batchDelete(ids: any[]): Promise<void> {
    const deleteApi = this.config.api.batchDelete || this.config.api.delete;
    if (!deleteApi) {
      throw new Error('未配置删除API');
    }
    
    try {
      await api.post(deleteApi, { ids });
      ElMessage.success('批量删除成功');
    } catch (error) {
      console.error('批量删除数据失败:', error);
      ElMessage.error('批量删除失败');
      throw error;
    }
  }
  
  /**
   * 导出数据
   * @param params 导出参数
   */
  async export(params: any = {}): Promise<void> {
    if (!this.config.api.export) {
      throw new Error('未配置导出API');
    }
    
    try {
      const response = await api.get(this.config.api.export!, {
        params,
        responseType: 'blob'
      });
      
      // 处理文件下载
      const blob = new Blob([response.data], {
        type: 'application/vnd.ms-excel'
      });
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `${this.config.entityName}列表.xlsx`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);
      
      ElMessage.success('导出成功');
    } catch (error) {
      console.error('导出数据失败:', error);
      ElMessage.error('导出失败');
      throw error;
    }
  }
  
  /**
   * 导入数据
   * @param file 文件对象
   * @returns 导入结果
   */
  async import(file: File): Promise<any> {
    if (!this.config.api.import) {
      throw new Error('未配置导入API');
    }
    
    try {
      const formData = new FormData();
      formData.append('file', file);
      
      const response = await api.post(this.config.api.import!, formData, {
        headers: {
          'Content-Type': 'multipart/form-data'
        }
      });
      
      ElMessage.success('导入成功');
      return response.data;
    } catch (error) {
      console.error('导入数据失败:', error);
      ElMessage.error('导入失败');
      throw error;
    }
  }
  
  /**
   * 获取配置
   */
  getConfig(): CrudConfig {
    return this.config;
  }
  
  /**
   * 更新配置
   */
  updateConfig(config: Partial<CrudConfig>): void {
    this.config = { ...this.config, ...config };
  }
}

/**
 * 创建CRUD服务实例
 */
export function createCrudService<T = any>(config: CrudConfig): CrudService<T> {
  return new CrudService<T>(config);
}

/**
 * 生成默认的CRUD配置
 */
export function generateDefaultCrudConfig(options: {
  entityName: string;
  apiBaseUrl: string;
  tableColumns: TableColumn[];
  formFields?: FormField[];
  showActions?: boolean[];
}): CrudConfig {
  const { entityName, apiBaseUrl, tableColumns, formFields = [], showActions = [] } = options;
  
  // 生成API配置
  const api: ApiConfig = {
    list: `${apiBaseUrl}/list`,
    detail: `${apiBaseUrl}/detail/:id`,
    create: `${apiBaseUrl}/create`,
    update: `${apiBaseUrl}/update/:id`,
    delete: `${apiBaseUrl}/delete/:id`,
    batchDelete: `${apiBaseUrl}/batchDelete`,
    import: `${apiBaseUrl}/import`,
    export: `${apiBaseUrl}/export`
  };
  
  // 生成表单配置
  const formConfig: FormConfig = {
    fields: formFields
  };
  
  // 生成操作配置
  const enabledActions = [
    ...(showActions.includes(true) || showActions.includes('create') ? ['create'] : []),
    ...(showActions.includes(true) || showActions.includes('update') ? ['update'] : []),
    ...(showActions.includes(true) || showActions.includes('delete') ? ['delete', 'batch-delete'] : []),
    ...(showActions.includes(true) || showActions.includes('view') ? ['view'] : []),
    ...(showActions.includes(true) || showActions.includes('import') ? ['import'] : []),
    ...(showActions.includes(true) || showActions.includes('export') ? ['export'] : [])
  ] as any[];
  
  return {
    entityName,
    api,
    table: {
      columns: tableColumns,
      showPagination: true,
      showSelection: enabledActions.includes('batch-delete'),
      showIndex: true,
      showSearch: true,
      showActions: enabledActions.length > 0,
      rowKey: 'id'
    },
    form: {
      createFormConfig: formConfig,
      updateFormConfig: formConfig,
      viewFormConfig: formConfig,
      dialogWidth: '50%',
      fullscreen: false
    },
    actions: {
      toolbar: [
        ...(enabledActions.includes('create') ? [{ name: '新增', type: 'primary', icon: 'Plus' }] : []),
        ...(enabledActions.includes('import') ? [{ name: '导入', type: 'success', icon: 'Upload' }] : []),
        ...(enabledActions.includes('export') ? [{ name: '导出', type: 'warning', icon: 'Download' }] : []),
        { name: '刷新', type: 'info', icon: 'Refresh' }
      ],
      tableRow: [
        ...(enabledActions.includes('view') ? [{ name: '查看', type: 'info', icon: 'View' }] : []),
        ...(enabledActions.includes('update') ? [{ name: '编辑', type: 'primary', icon: 'Edit' }] : []),
        ...(enabledActions.includes('delete') ? [{ 
          name: '删除', 
          type: 'danger', 
          icon: 'Delete',
          confirm: {
            title: '确认删除',
            message: `确定要删除该${entityName}吗？`,
            type: 'warning'
          }
        }] : [])
      ],
      batchActions: enabledActions.filter(action => 
        ['batch-delete', 'export'].includes(action)
      ),
      enabledActions
    },
    data: {
      responseMap: {
        list: 'data.list',
        total: 'data.total',
        pageNum: 'data.pageNum',
        pageSize: 'data.pageSize'
      },
      requestMap: {
        pageNum: 'pageNum',
        pageSize: 'pageSize',
        sortField: 'sortField',
        sortOrder: 'sortOrder'
      }
    }
  };
}

/**
 * 从数据模型生成CRUD配置
 */
export function generateCrudConfigFromModel(model: any, options?: Partial<{
  entityName: string;
  apiBaseUrl: string;
  excludedFields?: string[];
  includedFields?: string[];
  fieldOptions?: Record<string, any>;
  showActions?: boolean[];
}>): CrudConfig {
  const defaultOptions = {
    entityName: '实体',
    apiBaseUrl: '/api',
    excludedFields: ['id', 'createdAt', 'updatedAt', 'deletedAt'],
    includedFields: [],
    fieldOptions: {},
    showActions: [true]
  };
  
  const mergedOptions = { ...defaultOptions, ...options };
  const { entityName, apiBaseUrl, excludedFields, includedFields, fieldOptions, showActions } = mergedOptions;
  
  // 生成表格列和表单字段
  const tableColumns: TableColumn[] = [];
  const formFields: FormField[] = [];
  
  // 获取模型字段
  const modelKeys = includedFields.length > 0 
    ? includedFields 
    : Object.keys(model).filter(key => !excludedFields.includes(key));
  
  // 为每个字段生成配置
  modelKeys.forEach(key => {
    const fieldOption = fieldOptions[key] || {};
    const fieldType = getFieldType(model[key]);
    
    // 生成表格列
    tableColumns.push({
      title: fieldOption.label || camelToLabel(key),
      prop: key,
      width: fieldOption.width || 180,
      sortable: fieldOption.sortable !== false,
      align: fieldOption.align || 'left',
      formatter: fieldOption.formatter
    });
    
    // 生成表单字段
    formFields.push({
      name: key,
      label: fieldOption.label || camelToLabel(key),
      type: fieldOption.type || fieldType,
      required: fieldOption.required || false,
      placeholder: `请输入${camelToLabel(key)}`,
      default: model[key],
      ...fieldOption
    } as FormField);
  });
  
  // 添加ID列
  if (!excludedFields.includes('id')) {
    tableColumns.unshift({
      title: 'ID',
      prop: 'id',
      width: 80,
      fixed: 'left'
    });
  }
  
  // 添加时间列
  if (!excludedFields.includes('createdAt')) {
    tableColumns.push({
      title: '创建时间',
      prop: 'createdAt',
      width: 180,
      formatter: (row: any) => formatDate(row.createdAt)
    });
  }
  
  return generateDefaultCrudConfig({
    entityName,
    apiBaseUrl,
    tableColumns,
    formFields,
    showActions
  });
}

/**
 * 获取字段类型
 */
function getFieldType(value: any): string {
  if (value === null || value === undefined) return 'input';
  
  const type = typeof value;
  switch (type) {
    case 'string':
      // 判断是否为日期格式
      if (value.match(/^\d{4}-\d{2}-\d{2}/)) {
        return 'date';
      }
      return 'input';
    case 'number':
      return 'number';
    case 'boolean':
      return 'switch';
    case 'object':
      if (Array.isArray(value)) {
        return 'select';
      }
      if (value instanceof Date) {
        return 'datetime';
      }
      return 'input';
    default:
      return 'input';
  }
}

/**
 * 驼峰命名转中文标签
 */
function camelToLabel(str: string): string {
  return str.replace(/([A-Z])/g, ' $1')
    .replace(/^./, str => str.toUpperCase())
    .replace(/^Id$/, 'ID');
}

/**
 * 格式化日期
 */
function formatDate(date: any): string {
  if (!date) return '';
  
  const d = new Date(date);
  const year = d.getFullYear();
  const month = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  const hours = String(d.getHours()).padStart(2, '0');
  const minutes = String(d.getMinutes()).padStart(2, '0');
  const seconds = String(d.getSeconds()).padStart(2, '0');
  
  return `${year}-${month}-${day} ${hours}:${minutes}:${seconds}`;
}