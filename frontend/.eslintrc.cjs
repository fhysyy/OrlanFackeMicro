/* eslint-env node */

module.exports = {
  root: true,
  extends: [
    'eslint:recommended',
    'plugin:vue/vue3-recommended'
  ],
  parser: 'vue-eslint-parser',
  parserOptions: {
    ecmaVersion: 'latest',
    parser: {
      ts: '@typescript-eslint/parser',
      js: '@typescript-eslint/parser',
      '<template>': 'espree'
    },
    sourceType: 'module'
  },
  plugins: [
    '@typescript-eslint'
  ],
  rules: {
    // Vue 相关规则
    'vue/multi-word-component-names': 'off',
    'vue/no-v-model-argument': 'off',
    'vue/no-unused-vars': 'error',
    
    // TypeScript 相关规则
    '@typescript-eslint/no-unused-vars': 'error',
    '@typescript-eslint/no-explicit-any': 'warn',
    '@typescript-eslint/explicit-function-return-type': 'off',
    '@typescript-eslint/explicit-module-boundary-types': 'off',
    
    // JavaScript 相关规则
    'no-console': process.env.NODE_ENV === 'production' ? 'warn' : 'off',
    'no-debugger': process.env.NODE_ENV === 'production' ? 'warn' : 'off',
    'no-unused-vars': 'error',
    'prefer-const': 'error',
    'no-var': 'error',
    
    // 代码风格
    'quotes': ['error', 'single'],
    'semi': ['error', 'never'],
    'indent': ['error', 2],
    'comma-dangle': ['error', 'never'],
    'object-curly-spacing': ['error', 'always']
  }
}