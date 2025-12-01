<template>
  <div class="form-i18n-provider">
    <slot :t="t" :locale="currentLocale" />
  </div>
</template>

<script setup lang="ts">
import { provide, ref, watch, onMounted, onUnmounted } from 'vue'
import { FormI18nManager, SUPPORTED_LOCALES } from './FormI18n'

// Props
const props = defineProps<{
  locale?: string
  fallbackLocale?: string
  messages?: Record<string, Record<string, string>>
}>()

// 当前语言
const currentLocale = ref(props.locale || FormI18nManager.getCurrentLocale())

// 提供翻译函数
const t = (key: string, params?: Record<string, any>) => {
  return FormI18nManager.t(key, params)
}

// 提供上下文
provide('formI18n', {
  t,
  locale: currentLocale,
  setLocale: (locale: string) => {
    FormI18nManager.setLocale(locale)
    currentLocale.value = locale
  },
  supportedLocales: SUPPORTED_LOCALES
})

// 监听语言变化
watch(
  () => currentLocale.value,
  (newLocale) => {
    FormI18nManager.setLocale(newLocale)
  }
)

// 监听自定义消息变化
watch(
  () => props.messages,
  (newMessages) => {
    if (newMessages) {
      Object.entries(newMessages).forEach(([locale, messages]) => {
        FormI18nManager.addMessages(locale, messages)
      })
    }
  },
  { deep: true, immediate: true }
)

// 组件挂载时设置语言
onMounted(() => {
  if (props.locale) {
    FormI18nManager.setLocale(props.locale)
    currentLocale.value = props.locale
  }
})

// 组件卸载时重置语言
onUnmounted(() => {
  if (props.locale) {
    FormI18nManager.setLocale(FormI18nManager.getCurrentLocale())
  }
})
</script>

<style scoped>
.form-i18n-provider {
  width: 100%;
  height: 100%;
}
</style>