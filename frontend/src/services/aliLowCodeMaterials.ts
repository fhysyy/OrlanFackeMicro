// Ali LowCode Engine 物料配置

// 物料分类类型
export interface MaterialCategory {
  id: string
  name: string
  icon?: string
}

// 物料组件类型
export interface MaterialComponent {
  componentName: string
  title: string
  category: string
  icon?: string
  description?: string
  groupName?: string
  props?: Record<string, any>
}

// 物料分类配置
export const materialCategories: MaterialCategory[] = [
  {
    id: 'layout',
    name: '布局组件',
    icon: 'el-icon-s-grid'
  },
  {
    id: 'basic',
    name: '基础组件',
    icon: 'el-icon-box'
  },
  {
    id: 'form',
    name: '表单组件',
    icon: 'el-icon-edit-outline'
  },
  {
    id: 'data',
    name: '数据展示',
    icon: 'el-icon-data-line'
  },
  {
    id: 'feedback',
    name: '反馈组件',
    icon: 'el-icon-bell'
  }
]

// 基础物料组件配置
export const basicMaterials: MaterialComponent[] = [
  // 布局组件
  {
    componentName: 'div',
    title: '容器',
    category: 'layout',
    description: '通用容器组件',
    props: {
      style: {
        padding: '10px',
        background: '#fff'
      }
    }
  },
  {
    componentName: 'Row',
    title: '行布局',
    category: 'layout',
    description: '栅格行组件',
    props: {
      gutter: 16
    }
  },
  {
    componentName: 'Col',
    title: '列布局',
    category: 'layout',
    description: '栅格列组件',
    props: {
      span: 12
    }
  },
  
  // 基础组件
  {
    componentName: 'Text',
    title: '文本',
    category: 'basic',
    description: '文本展示组件',
    props: {
      content: '文本内容'
    }
  },
  {
    componentName: 'Button',
    title: '按钮',
    category: 'basic',
    description: '按钮组件',
    props: {
      type: 'primary',
      text: '按钮'
    }
  },
  {
    componentName: 'Image',
    title: '图片',
    category: 'basic',
    description: '图片组件',
    props: {
      src: '',
      alt: '图片'
    }
  },
  
  // 表单组件
  {
    componentName: 'Input',
    title: '输入框',
    category: 'form',
    description: '文本输入框',
    props: {
      placeholder: '请输入'
    }
  },
  {
    componentName: 'Select',
    title: '选择器',
    category: 'form',
    description: '下拉选择器',
    props: {
      placeholder: '请选择'
    }
  },
  {
    componentName: 'Switch',
    title: '开关',
    category: 'form',
    description: '开关组件'
  },
  
  // 数据展示
  {
    componentName: 'Table',
    title: '表格',
    category: 'data',
    description: '数据表格组件',
    props: {
      columns: [],
      dataSource: []
    }
  },
  {
    componentName: 'List',
    title: '列表',
    category: 'data',
    description: '列表组件',
    props: {
      dataSource: []
    }
  },
  
  // 反馈组件
  {
    componentName: 'Dialog',
    title: '对话框',
    category: 'feedback',
    description: '弹窗组件',
    props: {
      title: '提示',
      visible: false
    }
  },
  {
    componentName: 'Loading',
    title: '加载',
    category: 'feedback',
    description: '加载组件',
    props: {
      text: '加载中'
    }
  }
]

// 物料加载器
export const loadMaterials = async () => {
  try {
    // 这里将在依赖安装后替换为真实的物料加载逻辑
    console.log('加载物料配置:', basicMaterials)
    
    // 模拟加载成功
    return {
      categories: materialCategories,
      components: basicMaterials
    }
  } catch (error) {
    console.error('加载物料失败:', error)
    return {
      categories: materialCategories,
      components: []
    }
  }
}

// 注册自定义物料
export const registerCustomMaterials = async (customMaterials: MaterialComponent[]) => {
  try {
    // 这里将在依赖安装后实现真实的物料注册逻辑
    console.log('注册自定义物料:', customMaterials)
    return { success: true }
  } catch (error) {
    console.error('注册自定义物料失败:', error)
    return { success: false, error }
  }
}