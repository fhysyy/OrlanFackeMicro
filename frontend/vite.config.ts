import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { resolve } from 'path'
import Components from 'unplugin-vue-components/vite'
import { ElementPlusResolver } from 'unplugin-vue-components/resolvers'
import AutoImport from 'unplugin-auto-import/vite'
import { createDevTools } from '@vtj/pro/vite'
// https://vitejs.dev/config/
export default defineConfig({
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
   createDevTools({ linkOptions: { href: "/__vtj__/#/" } }),
  
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
    sourcemap: process.env.NODE_ENV !== 'production',
    chunkSizeWarningLimit: 2000,
    minify: 'terser',
    terserOptions: {
      compress: {
        drop_console: process.env.NODE_ENV === 'production',
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
    'process.env': process.env,
    '__VUE_PROD_HYDRATION_MISMATCH_DETAILS__': false
  }
})