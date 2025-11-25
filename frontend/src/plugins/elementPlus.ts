import type { App } from 'vue'
import { ElMessage } from 'element-plus'

// 优化样式加载：使用统一的样式文件，减少网络请求数量
import 'element-plus/dist/index.css'

// Element Plus插件配置 - 简化为仅处理全局方法注册
// 组件和图标将通过unplugin-vue-components自动导入
export function registerElementPlus(app: App) {
  // 注册全局消息方法（ElMessage不是组件，需要手动处理）
  app.config.globalProperties.$message = ElMessage
  
  // 提供给Composition API
  app.provide('message', ElMessage)
}

// 导出ElMessage供特殊情况使用
export { ElMessage }
