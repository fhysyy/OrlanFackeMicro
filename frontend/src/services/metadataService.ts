import type { PageMetadata } from '../types/page';
import api from './api';

/**
 * 页面元数据管理服务
 */
export class MetadataService {
  private static readonly STORAGE_KEY_PREFIX = 'page_metadata_';
  private static readonly STORAGE_LIST_KEY = 'page_metadata_list';

  /**
   * 保存页面元数据
   */
  static async saveMetadata(metadata: PageMetadata): Promise<PageMetadata> {
    try {
      // 更新时间戳
      metadata.updatedAt = new Date().toISOString();
      
      // 如果是新页面，设置创建时间
      if (!metadata.createdAt) {
        metadata.createdAt = metadata.updatedAt;
      }

      // 尝试通过API保存到后端
      try {
        const response = await api.post('/api/metadata/pages', metadata);
        
        // 更新本地存储
        this.updateLocalStorage(metadata);
        
        return response.data;
      } catch (apiError) {
        console.warn('后端保存失败，使用本地存储', apiError);
        
        // 保存到本地存储
        this.updateLocalStorage(metadata);
        
        return metadata;
      }
    } catch (error) {
      console.error('保存页面元数据失败:', error);
      throw new Error('保存页面元数据失败');
    }
  }

  /**
   * 获取页面元数据
   */
  static async getMetadata(pageId: string): Promise<PageMetadata | null> {
    try {
      // 尝试从后端获取
      try {
        const response = await api.get(`/api/metadata/pages/${pageId}`);
        const metadata = response.data;
        
        // 更新本地缓存
        this.updateLocalStorage(metadata);
        
        return metadata;
      } catch (apiError) {
        console.warn('后端获取失败，尝试从本地存储获取', apiError);
        
        // 从本地存储获取
        return this.getFromLocalStorage(pageId);
      }
    } catch (error) {
      console.error('获取页面元数据失败:', error);
      
      // 兜底从本地存储获取
      return this.getFromLocalStorage(pageId);
    }
  }

  /**
   * 获取页面元数据列表
   */
  static async getMetadataList(): Promise<PageMetadata[]> {
    try {
      // 尝试从后端获取
      try {
        const response = await api.get('/api/metadata/pages');
        const metadataList = response.data;
        
        // 更新本地缓存
        this.updateLocalStorageList(metadataList);
        
        return metadataList;
      } catch (apiError) {
        console.warn('后端获取失败，从本地存储获取列表', apiError);
        
        // 从本地存储获取
        return this.getListFromLocalStorage();
      }
    } catch (error) {
      console.error('获取页面元数据列表失败:', error);
      
      // 兜底从本地存储获取
      return this.getListFromLocalStorage();
    }
  }

  /**
   * 删除页面元数据
   */
  static async deleteMetadata(pageId: string): Promise<void> {
    try {
      // 尝试从后端删除
      try {
        await api.delete(`/api/metadata/pages/${pageId}`);
      } catch (apiError) {
        console.warn('后端删除失败，仅删除本地存储', apiError);
      }
      
      // 从本地存储删除
      this.removeFromLocalStorage(pageId);
    } catch (error) {
      console.error('删除页面元数据失败:', error);
      throw new Error('删除页面元数据失败');
    }
  }

  /**
   * 导出页面元数据
   */
  static exportMetadata(metadata: PageMetadata): string {
    try {
      const content = JSON.stringify(metadata, null, 2);
      return content;
    } catch (error) {
      console.error('导出页面元数据失败:', error);
      throw new Error('导出页面元数据失败');
    }
  }

  /**
   * 导入页面元数据
   */
  static importMetadata(content: string): PageMetadata {
    try {
      const metadata = JSON.parse(content) as PageMetadata;
      
      // 验证导入的数据
      this.validateMetadata(metadata);
      
      // 生成新的ID，避免覆盖
      metadata.id = `${metadata.id || 'imported'}_${Date.now()}`;
      
      // 更新时间戳
      metadata.createdAt = new Date().toISOString();
      metadata.updatedAt = metadata.createdAt;
      
      return metadata;
    } catch (error) {
      console.error('导入页面元数据失败:', error);
      throw new Error('导入页面元数据格式错误或无效');
    }
  }

  /**
   * 复制页面元数据
   */
  static duplicateMetadata(metadata: PageMetadata, newName: string, newPath: string): PageMetadata {
    // 深拷贝
    const newMetadata = JSON.parse(JSON.stringify(metadata));
    
    // 更新基本信息
    newMetadata.id = `${metadata.id || 'page'}_copy_${Date.now()}`;
    newMetadata.name = newName;
    newMetadata.createdAt = new Date().toISOString();
    newMetadata.updatedAt = newMetadata.createdAt;
    
    // 更新路由信息
    if (newMetadata.route) {
      newMetadata.route.path = newPath;
      newMetadata.route.name = newMetadata.id;
      if (newMetadata.route.meta) {
        newMetadata.route.meta.title = newName;
      }
    }
    
    return newMetadata;
  }

  /**
   * 获取页面元数据模板
   */
  static getMetadataTemplates(): PageMetadata[] {
    return [
      {
        id: 'template_dashboard',
        name: '仪表盘模板',
        description: '包含图表和统计卡片的仪表盘',
        version: '1.0.0',
        layout: {
          type: 'dashboard'
        },
        components: [
          {
            type: 'row',
            children: [
              {
                type: 'stat-card',
                props: {
                  title: '总用户数',
                  value: '{{userCount}}',
                  icon: 'el-icon-user-solid',
                  color: 'primary'
                }
              },
              {
                type: 'stat-card',
                props: {
                  title: '今日访问',
                  value: '{{todayVisits}}',
                  icon: 'el-icon-visit',
                  color: 'success'
                }
              },
              {
                type: 'stat-card',
                props: {
                  title: '转化率',
                  value: '{{conversionRate}}%',
                  icon: 'el-icon-data-line',
                  color: 'warning'
                }
              },
              {
                type: 'stat-card',
                props: {
                  title: '销售额',
                  value: '￥{{salesAmount}}',
                  icon: 'el-icon-money',
                  color: 'danger'
                }
              }
            ]
          },
          {
            type: 'row',
            children: [
              {
                type: 'chart',
                props: {
                  title: '销售趋势',
                  type: 'line',
                  data: '{{salesData}}'
                }
              },
              {
                type: 'chart',
                props: {
                  title: '用户分布',
                  type: 'pie',
                  data: '{{userDistributionData}}'
                }
              }
            ]
          }
        ],
        state: [
          { name: 'userCount', initialValue: 0 },
          { name: 'todayVisits', initialValue: 0 },
          { name: 'conversionRate', initialValue: 0 },
          { name: 'salesAmount', initialValue: 0 },
          { name: 'salesData', initialValue: [] },
          { name: 'userDistributionData', initialValue: [] }
        ],
        methods: [
          {
            name: 'loadDashboardData',
            body: `
              api.get('/api/dashboard/data').then(response => {
                const data = response.data;
                userCount.value = data.userCount;
                todayVisits.value = data.todayVisits;
                conversionRate.value = data.conversionRate;
                salesAmount.value = data.salesAmount;
                salesData.value = data.salesData;
                userDistributionData.value = data.userDistributionData;
              }).catch(error => {
                console.error('加载仪表盘数据失败:', error);
              });
            `
          }
        ],
        lifecycle: {
          created: `
            this.loadDashboardData();
          `
        }
      },
      {
        id: 'template_list',
        name: '列表模板',
        description: '包含搜索、表格和分页的列表页面',
        version: '1.0.0',
        layout: {
          type: 'single-column'
        },
        components: [
          {
            type: 'search-form',
            props: {
              model: '{{searchForm}}',
              onSearch: 'handleSearch',
              onReset: 'handleReset',
              fields: [
                {
                  prop: 'keyword',
                  label: '关键字',
                  type: 'input'
                },
                {
                  prop: 'status',
                  label: '状态',
                  type: 'select',
                  options: ['全部', '启用', '禁用']
                }
              ]
            }
          },
          {
            type: 'table',
            props: {
              data: '{{tableData}}',
              columns: [
                {
                  prop: 'id',
                  label: 'ID'
                },
                {
                  prop: 'name',
                  label: '名称'
                },
                {
                  prop: 'createdAt',
                  label: '创建时间'
                },
                {
                  prop: 'status',
                  label: '状态'
                },
                {
                  prop: 'action',
                  label: '操作',
                  fixed: 'right',
                  width: 180,
                  buttons: [
                    { text: '编辑', onClick: 'handleEdit' },
                    { text: '删除', onClick: 'handleDelete' }
                  ]
                }
              ],
              pagination: {
                currentPage: '{{currentPage}}',
                pageSize: '{{pageSize}}',
                total: '{{total}}',
                onCurrentChange: 'handleCurrentChange',
                onSizeChange: 'handleSizeChange'
              }
            }
          }
        ],
        state: [
          { name: 'searchForm', initialValue: { keyword: '', status: '全部' } },
          { name: 'tableData', initialValue: [] },
          { name: 'currentPage', initialValue: 1 },
          { name: 'pageSize', initialValue: 10 },
          { name: 'total', initialValue: 0 }
        ],
        methods: [
          {
            name: 'loadData',
            body: `
              const params = {
                ...searchForm.value,
                page: currentPage.value,
                pageSize: pageSize.value
              };
              
              api.get('/api/data/list', { params }).then(response => {
                tableData.value = response.data.items;
                total.value = response.data.total;
              }).catch(error => {
                console.error('加载数据失败:', error);
              });
            `
          },
          {
            name: 'handleSearch',
            body: `
              currentPage.value = 1;
              this.loadData();
            `
          },
          {
            name: 'handleReset',
            body: `
              searchForm.value = { keyword: '', status: '全部' };
              currentPage.value = 1;
              this.loadData();
            `
          },
          {
            name: 'handleCurrentChange',
            params: ['page'],
            body: `
              currentPage.value = page;
              this.loadData();
            `
          },
          {
            name: 'handleSizeChange',
            params: ['size'],
            body: `
              pageSize.value = size;
              currentPage.value = 1;
              this.loadData();
            `
          },
          {
            name: 'handleEdit',
            params: ['row'],
            body: `
              console.log('编辑:', row);
              // 这里可以打开编辑对话框
            `
          },
          {
            name: 'handleDelete',
            params: ['row'],
            body: `
              if (confirm('确定要删除这条记录吗？')) {
                api.delete('/api/data/' + row.id).then(() => {
                  this.loadData();
                }).catch(error => {
                  console.error('删除失败:', error);
                });
              }
            `
          }
        ],
        lifecycle: {
          created: `
            this.loadData();
          `
        }
      }
    ];
  }

  /**
   * 验证页面元数据
   */
  private static validateMetadata(metadata: any): void {
    if (!metadata || typeof metadata !== 'object') {
      throw new Error('页面元数据必须是有效的对象');
    }
    
    if (!metadata.name || typeof metadata.name !== 'string') {
      throw new Error('页面名称必须是有效的字符串');
    }
    
    if (!metadata.components || !Array.isArray(metadata.components)) {
      throw new Error('页面组件配置必须是有效的数组');
    }
  }

  /**
   * 更新本地存储
   */
  private static updateLocalStorage(metadata: PageMetadata): void {
    try {
      localStorage.setItem(`${this.STORAGE_KEY_PREFIX}${metadata.id}`, JSON.stringify(metadata));
      
      // 更新列表
      const list = this.getListFromLocalStorage();
      const existingIndex = list.findIndex(item => item.id === metadata.id);
      
      if (existingIndex >= 0) {
        list[existingIndex] = { ...metadata };
      } else {
        list.push({ ...metadata });
      }
      
      localStorage.setItem(this.STORAGE_LIST_KEY, JSON.stringify(list));
    } catch (error) {
      console.error('更新本地存储失败:', error);
    }
  }

  /**
   * 从本地存储获取
   */
  private static getFromLocalStorage(pageId: string): PageMetadata | null {
    try {
      const content = localStorage.getItem(`${this.STORAGE_KEY_PREFIX}${pageId}`);
      return content ? JSON.parse(content) : null;
    } catch (error) {
      console.error('从本地存储获取失败:', error);
      return null;
    }
  }

  /**
   * 更新本地存储列表
   */
  private static updateLocalStorageList(metadataList: PageMetadata[]): void {
    try {
      localStorage.setItem(this.STORAGE_LIST_KEY, JSON.stringify(metadataList));
      
      // 更新每个页面的单独存储
      metadataList.forEach(metadata => {
        localStorage.setItem(`${this.STORAGE_KEY_PREFIX}${metadata.id}`, JSON.stringify(metadata));
      });
    } catch (error) {
      console.error('更新本地存储列表失败:', error);
    }
  }

  /**
   * 从本地存储获取列表
   */
  private static getListFromLocalStorage(): PageMetadata[] {
    try {
      const content = localStorage.getItem(this.STORAGE_LIST_KEY);
      return content ? JSON.parse(content) : [];
    } catch (error) {
      console.error('从本地存储获取列表失败:', error);
      return [];
    }
  }

  /**
   * 从本地存储删除
   */
  private static removeFromLocalStorage(pageId: string): void {
    try {
      localStorage.removeItem(`${this.STORAGE_KEY_PREFIX}${pageId}`);
      
      // 更新列表
      const list = this.getListFromLocalStorage();
      const filteredList = list.filter(item => item.id !== pageId);
      localStorage.setItem(this.STORAGE_LIST_KEY, JSON.stringify(filteredList));
    } catch (error) {
      console.error('从本地存储删除失败:', error);
    }
  }
}