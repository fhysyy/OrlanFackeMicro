<template>
  <div class="language-switcher">
    <el-select
      :model-value="currentLocale"
      placeholder="选择语言"
      @change="handleLanguageChange"
    >
      <el-option
        v-for="locale in supportedLocales"
        :key="locale.value"
        :label="locale.label"
        :value="locale.value"
      />
    </el-select>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { FormI18nManager, SUPPORTED_LOCALES } from './FormI18n'

// 当前语言
const currentLocale = ref(FormI18nManager.getCurrentLocale())

// 支持的语言列表
const supportedLocales = ref(SUPPORTED_LOCALES)

// 处理语言切换
const handleLanguageChange = (locale: string) => {
  FormI18nManager.setLocale(locale)
  currentLocale.value = locale
  ElMessage.success(`语言已切换到 ${supportedLocales.value.find(l => l.value === locale)?.label}`)
}

// 组件挂载时初始化
onMounted(() => {
  currentLocale.value = FormI18nManager.getCurrentLocale()
})
</script>

<style scoped>
.language-switcher {
  display: inline-block;
  min-width: 120px;
}
</style>