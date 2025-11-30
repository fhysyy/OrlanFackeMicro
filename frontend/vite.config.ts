import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { resolve } from 'path'
import { fileURLToPath } from 'url'
import Components from 'unplugin-vue-components/vite'
import { ElementPlusResolver } from 'unplugin-vue-components/resolvers'
import AutoImport from 'unplugin-auto-import/vite'
import { createDevTools } from '@vtj/pro/vite'

// 获取当前文件路径
const __filename = fileURLToPath(import.meta.url)
// 获取当前目录路径
const __dirname = resolve(__filename, '..')
// https://vitejs.dev/config/
export default defineConfig(({ command }) => ({
  plugins: [
    vue(),
    Components({
      resolvers: [
        ElementPlusResolver({
          importStyle: 'css'
        })
      ],
      dts: true // 生成类型声明文件
    }),
    AutoImport({
      resolvers: [ElementPlusResolver()],
      imports: ['vue', 'vue-router', 'pinia'],
      dts: true, // 生成类型声明文件
      vueTemplate: true
    }),
    // 条件性添加VTJ开发工具插件
    // 只有在开发环境中才使用该插件，并且使用简单配置
    // 使用process.env.NODE_ENV代替import.meta.env.MODE在配置文件中
    ...(process.env.NODE_ENV === 'development' ? [createDevTools({
      linkOptions: { href: '/_vtj_/#' }
    })] : [])
  ],
  
  resolve: {
    alias: {
      '@': resolve(__dirname, 'src')
    },
    // 配置扩展，确保模块解析正确
    extensions: ['.mjs', '.js', '.ts', '.json', '.vue']
  },
  
  server: {
    port: 3000,
    host: true,
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        secure: false
      }
    }
  },
  
  build: {
    outDir: 'dist',
    sourcemap: command !== 'build',
    chunkSizeWarningLimit: 2000,
    minify: 'terser',
    terserOptions: {
      compress: {
        drop_console: command === 'build',
        drop_debugger: true
      },
      // 保留类名，避免类构造函数调用错误
      keep_classnames: true,
      keep_fnames: true
    },
    // 确保构建目标支持ES6类
    target: 'es2020',
    rollupOptions: {
      output: {
        manualChunks: {
          vendor: ['vue', 'vue-router', 'pinia'],
          ui: ['element-plus', '@element-plus/icons-vue'],
          utils: ['axios', 'dayjs', 'lodash'],
          charts: ['echarts', 'vue-echarts']
        }
      }
    }
  },
  
  css: {
    preprocessorOptions: {
      scss: {
        additionalData: '@use "@/styles/variables.scss" as *;'
      }
    }
  },
  
  // 优化配置，确保正确处理ES6类
  optimizeDeps: {
    include: [
      '@vtj/designer', 
      '@vtj/web',
      'vue',
      'vue-router',
      'pinia',
      'axios',
      'dayjs',
      'lodash'
    ],
    esbuildOptions: {
      // 确保ES6类正确编译
      target: 'es2020',
      // 避免对@vtj相关包进行过度优化
      keepNames: true
    }
  },
  
  // 配置环境变量
  define: {
    '__VUE_PROD_HYDRATION_MISMATCH_DETAILS__': false
  }
}))