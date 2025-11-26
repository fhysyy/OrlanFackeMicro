// Ali LowCode Engine 服务封装
import { ElMessage } from 'element-plus'
import { logger } from '@/utils/logger'

// 定义schema类型
export interface LowCodeSchema {
  version: string;
  componentsTree: any[];
  componentsMap: Record<string, any>;
}

// 默认schema
export const defaultSchema: LowCodeSchema = {
  version: '1.0.0',
  componentsTree: [
    {
      componentName: 'Page',
      props: {
        title: '我的低代码页面',
        style: {
          padding: '20px',
          backgroundColor: '#f0f2f5',
        },
      },
      children: [
        {
          componentName: 'Button',
          props: {
            type: 'primary',
            children: '点击我',
          },
          style: {
            margin: '10px 0',
          },
        },
      ],
    },
  ],
  componentsMap: {
    Page: {
      componentName: 'Page',
      title: '页面',
      icon: 'icon-page',
    },
    Button: {
      componentName: 'Button',
      title: '按钮',
      icon: 'icon-button',
    },
  },
}

/**
 * 动态导入低代码引擎
 */
async function importLowCodeEngine() {
  try {
    logger.log('尝试导入低代码引擎...');
    
    // 只尝试导入低代码引擎核心
    let engine = null;
    try {
      engine = await import('@alilc/lowcode-engine');
      logger.log('成功导入低代码引擎核心');
    } catch (engineError) {
      logger.error('导入低代码引擎核心失败:', engineError);
      return null;
    }
    
    return {
      engine,
      version: '1.0.0',
      vueAdapter: {
        register: () => {
          logger.log('注册Vue适配器');
        }
      },
      init: async (container: HTMLElement, options: any) => {
        logger.log('初始化低代码引擎实例', { container, options });
        return Promise.resolve();
      }
    };
  } catch (error) {
    logger.error('导入低代码引擎失败:', error);
    return null;
  }
}

class AliLowCodeService {
  private engine: any = null
  private container: HTMLElement | null = null
  private currentSchema: LowCodeSchema = { ...defaultSchema }
  private isInitialized: boolean = false

  /**
   * 初始化低代码引擎
   * @param container 容器DOM元素
   * @param options 初始化选项
   */
  async initialize(container: HTMLElement, options: any = {}) {
    try {
      if (!container) {
        throw new Error('容器元素不存在')
      }

      this.container = container
      
      // 动态导入低代码引擎
      const lowCodeModules = await importLowCodeEngine();
      
      if (lowCodeModules) {
        const { engine, vueAdapter } = lowCodeModules;
        
        // 注册 Vue 适配器
        vueAdapter.register();
        
        // 初始化引擎
        this.engine = await engine.init({
          // 设计器初始化配置
          designMode: 'design',
          mountNode: container,
          enableCondition: true,
          enableCanvasLock: true,
          // 传入组件面板的初始化数据
          componentsTree: this.currentSchema.componentsTree,
          componentsMap: this.currentSchema.componentsMap,
          device: 'default',
          ...options
        });
        
        logger.info('低代码引擎初始化成功');
      } else {
        logger.info('使用模拟模式运行低代码引擎');
        // 模拟模式下的初始化逻辑
        console.log('初始化Ali LowCode Engine服务(模拟模式)', options)
      }
      
      this.isInitialized = true
      
      // 返回初始化结果
      return {
        success: true,
        message: '低代码引擎服务初始化成功'
      }
    } catch (error) {
      logger.error('初始化低代码引擎服务失败:', error)
      throw error
    }
  }

  /**
   * 加载Schema
   * @param schema 页面配置
   */
  async loadSchema(schema: LowCodeSchema) {
    try {
      this.currentSchema = { ...schema };
      
      if (this.engine) {
        // 使用真实引擎加载配置
        await this.engine.setComponentsTree(schema.componentsTree);
        await this.engine.setComponentsMap(schema.componentsMap);
        logger.info('低代码引擎配置加载成功');
      } else {
        // 模拟加载配置
        console.log('模拟加载低代码配置:', schema);
      }
      
      return { success: true }
    } catch (error) {
      logger.error('加载schema失败:', error)
      ElMessage.error('加载页面配置失败')
      return { success: false, error }
    }
  }

  /**
   * 导出Schema
   */
  async exportSchema(): Promise<LowCodeSchema> {
    try {
      if (this.engine) {
        // 从真实引擎获取配置
        try {
          const componentsTree = this.engine.getComponentsTree();
          const componentsMap = this.engine.getComponentsMap();
          
          return {
            version: '1.0.0',
            componentsTree,
            componentsMap
          };
        } catch (error) {
          logger.error('获取引擎配置失败，返回当前缓存配置:', error);
        }
      }
      
      // 返回缓存的配置或默认配置
      return { ...this.currentSchema }
    } catch (error) {
      logger.error('导出schema失败:', error)
      ElMessage.error('导出页面配置失败')
      throw error
    }
  }

  /**
   * 注册自定义物料
   * @param materials 物料配置
   */
  async registerMaterials(materials: any[]) {
    try {
      if (this.engine) {
        // 使用真实引擎注册物料
        materials.forEach(material => {
          if (material.componentName && material.component) {
            this.engine.registerComponent(material.componentName, material.component);
          }
        });
        logger.info('自定义物料注册成功');
      } else {
        // 模拟注册物料
        console.log('模拟注册自定义物料:', materials);
      }
      
      return { success: true }
    } catch (error) {
      logger.error('注册物料失败:', error)
      return { success: false, error }
    }
  }

  /**
   * 预览当前页面
   */
  async preview() {
    try {
      if (this.engine) {
        // 使用真实引擎预览
        this.engine.switchDesignMode('preview');
      } else {
        // 模拟预览功能
        console.log('模拟预览当前设计');
        ElMessage.info('预览功能开发中(模拟模式)')
      }
      return { success: true }
    } catch (error) {
      logger.error('预览失败:', error)
      ElMessage.error('预览失败')
      return { success: false, error }
    }
  }

  /**
   * 返回设计模式
   */
  async design() {
    try {
      if (this.engine) {
        // 使用真实引擎切换到设计模式
        this.engine.switchDesignMode('design');
      } else {
        console.log('切换到设计模式(模拟)');
      }
      return { success: true }
    } catch (error) {
      logger.error('切换到设计模式失败:', error);
      return { success: false, error };
    }
  }

  /**
   * 销毁低代码引擎实例
   */
  destroy() {
    try {
      logger.info('销毁低代码引擎实例');
      
      if (this.engine) {
        // 销毁真实引擎
        this.engine.destroy();
        this.engine = null;
      }
      
      this.container = null;
      this.isInitialized = false;
    } catch (error) {
      logger.error('销毁低代码引擎失败:', error)
    }
  }

  /**
   * 获取引擎状态
   */
  getStatus(): { initialized: boolean; hasEngine: boolean } {
    return {
      initialized: this.isInitialized,
      hasEngine: !!this.engine
    };
  }
}

// 导出服务实例和类型
export const aliLowCodeService = new AliLowCodeService();
export default aliLowCodeService;