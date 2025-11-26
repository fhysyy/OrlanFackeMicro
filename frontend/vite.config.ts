import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { resolve } from 'path'
import Components from 'unplugin-vue-components/vite'
import { ElementPlusResolver } from 'unplugin-vue-components/resolvers'
import AutoImport from 'unplugin-auto-import/vite'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    Components({
      resolvers: [
        ElementPlusResolver({
          importStyle: 'css',
        }),
      ],
      dts: true, // 生成类型声明文件
    }),
    AutoImport({
      resolvers: [ElementPlusResolver()],
      imports: ['vue', 'vue-router', 'pinia'],
      dts: true, // 生成类型声明文件
      vueTemplate: true,
    }),
    // VTJ 开发工具插件 - 用于设计器功能
    // createDevTools()
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
    // 增加服务器配置以支持大文件
    maxPayloadSize: 50,
    // 配置静态资源目录
    watch: {
      usePolling: true,
      interval: 100,
    },
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
    sourcemap: process.env.NODE_ENV !== 'production',
    chunkSizeWarningLimit: 2000,
    minify: 'terser',
    terserOptions: {
      compress: {
        drop_console: process.env.NODE_ENV === 'production',
        drop_debugger: true
      }
    },
    rollupOptions: {
      output: {
        manualChunks: {
          // Vue 生态系统
          'vue-vendor': ['vue', 'vue-router', 'pinia'],
          
          // UI 组件库
          'ui-vendor': ['element-plus', '@element-plus/icons-vue'],
          
          // 工具库
          'utils-vendor': ['axios', 'dayjs', 'lodash'],
          
          // 图表库
          'chart-vendor': ['echarts', 'vue-echarts'],
          
          // VTJ 设计器
          'vtj-vendor': ['@vtj/designer', '@vtj/core', '@vtj/renderer']
        },
        // 确保输出格式兼容性
        format: 'esm',
        // 解决循环依赖问题
        preserveModules: false
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
  
  optimizeDeps: {
    include: [
      'vue',
      'vue-router',
      'pinia',
      'axios',
      'dayjs',
      'lodash',
      '@vtj/designer',
      '@vtj/core',
      '@vtj/renderer'
    ],
    // 增加优化配置
    esbuildOptions: {
      target: 'es2020',
      supported: {
        bigint: true
      }
    },
    // 增加依赖优化并发数
    maxConcurrentWorkers: 8
  },
  
  // 配置环境变量
  define: {
    'process.env': {},
    '__VUE_PROD_HYDRATION_MISMATCH_DETAILS__': false
  }
})