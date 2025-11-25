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
  ],
  
  resolve: {
    alias: {
      '@': resolve(__dirname, 'src'),
      // 为低代码引擎添加别名，提高兼容性
      '@alilc/lowcode-engine': resolve(__dirname, 'node_modules/@alilc/lowcode-engine')
    },
    // 配置扩展，确保模块解析正确
    extensions: ['.mjs', '.js', '.ts', '.jsx', '.tsx', '.json', '.vue'],
    // 为低代码引擎依赖的Node.js模块添加浏览器兼容处理
    fallback: {
      events: 'events'
    }
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
          'chart-vendor': ['echarts', 'vue-echarts']
        },
        // 确保输出格式兼容性
        format: 'esm',
        // 解决循环依赖问题
        preserveModules: false
      },
      // 配置外部依赖，避免打包问题
      external: [
        '@alilc/lowcode-engine',
        '@alilc/lowcode-materials'
      ]
    }
  },
  
  css: {
    preprocessorOptions: {
      scss: {
        additionalData: '@use "@/styles/variables.scss" as *;'
      }
    },
    // 增加 CSS 配置以支持低代码引擎
    modules: {
      localsConvention: 'camelCaseOnly'
    }
  },
  
  optimizeDeps: {
    include: [
      'vue',
      'vue-router',
      'pinia',
      'axios',
      'dayjs',
      'lodash'
    ],
    // 排除低代码引擎相关依赖，避免构建问题
    exclude: [
      '@alilc/lowcode-engine',
      '@alilc/lowcode-materials',
      '@alifd/next'
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
  
  // 配置 worker 以支持低代码引擎
  worker: {
    format: 'es',
    plugins: () => [vue()],
    rollupOptions: {
      output: {
        inlineDynamicImports: true
      }
    }
  },
  
  // 移除可能导致HMR冲突的实验性配置
  // 简化配置以确保热模块替换正常工作
  experimental: {
    // 移除hmrPartialAccept以避免冲突
  },
  
  // 配置环境变量
  define: {
    'process.env': {},
    // 低代码引擎需要的全局变量
    '__VUE_PROD_HYDRATION_MISMATCH_DETAILS__': false
  }
})