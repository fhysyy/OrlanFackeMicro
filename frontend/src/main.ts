import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { registerElementPlus } from './plugins/elementPlus'
import '@/styles/index.scss'
import { createProvider,LocalService,createModules,NodeEnv } from '@vtj/web'
import './style.css'
import App from './App.vue'
import router from './router'
import { useAuthStore } from './stores/auth'


// 导入错误处理工具
import { setupGlobalErrorHandler } from './utils/errorHandler'
import { logger } from './utils/logger'


const app = createApp(App)
const service = new LocalService()

const { provider, onReady } = createProvider({
  // 设置运行环境
  nodeEnv: process.env.NODE_ENV as NodeEnv,

  // 注册应用模块
  modules: createModules(),

  // 注入服务实例
  service,

  // 注入路由实例
  router
})
// 按需注册Element Plus组件和图标
registerElementPlus(app)

// 设置全局错误处理
setupGlobalErrorHandler(app)
onReady(async () => {
  app.use(router)
  app.use(createPinia())
  const authStore = useAuthStore()
  await authStore.initialize()
  app.use(provider)
  app.mount('#app')
})




// // 初始化应用
// const initApp = async () => {
//   try {
//     // 初始化认证状态（必须在挂载前进行）
//     const authStore = useAuthStore()
//     await authStore.initialize()
    

    
//     // 挂载应用
//     app.mount('#app')
    
//     logger.log('项目启动成功')
//   } catch (error) {
//     logger.error('项目启动失败:', error)
    
//     // 显示错误提示
//     if (window && window.document) {
//       const errorElement = document.createElement('div')
//       errorElement.style.cssText = `
//         position: fixed;
//         top: 50%;
//         left: 50%;
//         transform: translate(-50%, -50%);
//         background: #f56c6c;
//         color: white;
//         padding: 20px;
//         border-radius: 8px;
//         z-index: 9999;
//         box-shadow: 0 4px 12px rgba(245, 108, 108, 0.3);
//       `
//       errorElement.textContent = '应用启动失败，请刷新页面重试'
//       document.body.appendChild(errorElement)
//     }
//   }
// }

// // 启动应用
// initApp()