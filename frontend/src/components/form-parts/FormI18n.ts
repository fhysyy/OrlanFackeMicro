import { ref, computed } from 'vue'
import { ElMessage } from 'element-plus'

// 默认语言
const DEFAULT_LOCALE = 'zh-CN'

// 可用语言列表
export const SUPPORTED_LOCALES = [
  { value: 'zh-CN', label: '简体中文' },
  { value: 'en-US', label: 'English' },
  { value: 'ja-JP', label: '日本語' }
]

// 当前语言
const currentLocale = ref(DEFAULT_LOCALE)

// 语言包
const messages: Record<string, Record<string, string>> = {
  'zh-CN': {
    // 表单通用
    'form.submit': '提交',
    'form.reset': '重置',
    'form.required': '请输入{field}',
    'form.loading': '提交中...',
    
    // 验证消息
    'validation.required': '{field}是必填项',
    'validation.min': '{field}长度不能少于{min}个字符',
    'validation.max': '{field}长度不能超过{max}个字符',
    'validation.email': '请输入正确的邮箱地址',
    'validation.phone': '请输入正确的手机号码',
    'validation.idcard': '请输入正确的身份证号码',
    'validation.noChinese': '{field}不能包含中文字符',
    'validation.hasNumber': '{field}必须包含数字',
    'validation.hasSpecialChar': '{field}必须包含特殊字符',
    'validation.pattern': '{field}格式不正确',
    
    // 错误处理
    'error.unknown': '发生了未知错误',
    'error.network': '网络连接失败，请检查网络设置',
    'error.server': '服务器错误，请稍后重试',
    'error.validation': '表单验证失败，请检查输入内容',
    'error.fileSize': '文件大小不能超过{size}',
    'error.fileType': '不支持的文件类型',
    
    // 成功消息
    'success.submit': '表单提交成功',
    'success.save': '保存成功',
    'success.reset': '表单已重置',
    'success.recover': '表单已恢复',
    
    // 操作提示
    'action.upload': '上传文件',
    'action.delete': '删除',
    'action.edit': '编辑',
    'action.add': '添加',
    'action.cancel': '取消',
    'action.confirm': '确认',
    'action.retry': '重试',
    'action.back': '返回',
    'action.continue': '继续',
    
    // 字段类型
    'field.input': '单行文本',
    'field.textarea': '多行文本',
    'field.select': '下拉选择',
    'field.radio': '单选框',
    'field.checkbox': '复选框',
    'field.switch': '开关',
    'field.date': '日期',
    'field.time': '时间',
    'field.datetime': '日期时间',
    'field.number': '数字',
    'field.slider': '滑块',
    'field.rate': '评分',
    'field.upload': '文件上传',
    
    // 占位符
    'placeholder.input': '请输入{field}',
    'placeholder.select': '请选择{field}',
    'placeholder.date': '请选择日期',
    'placeholder.time': '请选择时间',
    
    // 帮助文本
    'help.required': '此字段为必填项',
    'help.optional': '此字段为可选项',
    'help.format': '请按照指定格式输入'
  },
  'en-US': {
    // Form common
    'form.submit': 'Submit',
    'form.reset': 'Reset',
    'form.required': 'Please enter {field}',
    'form.loading': 'Submitting...',
    
    // Validation messages
    'validation.required': '{field} is required',
    'validation.min': '{field} must be at least {min} characters',
    'validation.max': '{field} must be no more than {max} characters',
    'validation.email': 'Please enter a valid email address',
    'validation.phone': 'Please enter a valid phone number',
    'validation.idcard': 'Please enter a valid ID card number',
    'validation.noChinese': '{field} cannot contain Chinese characters',
    'validation.hasNumber': '{field} must contain numbers',
    'validation.hasSpecialChar': '{field} must contain special characters',
    'validation.pattern': '{field} format is incorrect',
    
    // Error handling
    'error.unknown': 'An unknown error occurred',
    'error.network': 'Network connection failed, please check your network settings',
    'error.server': 'Server error, please try again later',
    'error.validation': 'Form validation failed, please check your input',
    'error.fileSize': 'File size cannot exceed {size}',
    'error.fileType': 'Unsupported file type',
    
    // Success messages
    'success.submit': 'Form submitted successfully',
    'success.save': 'Saved successfully',
    'success.reset': 'Form has been reset',
    'success.recover': 'Form has been recovered',
    
    // Action prompts
    'action.upload': 'Upload File',
    'action.delete': 'Delete',
    'action.edit': 'Edit',
    'action.add': 'Add',
    'action.cancel': 'Cancel',
    'action.confirm': 'Confirm',
    'action.retry': 'Retry',
    'action.back': 'Back',
    'action.continue': 'Continue',
    
    // Field types
    'field.input': 'Single Line Text',
    'field.textarea': 'Multi-line Text',
    'field.select': 'Dropdown Select',
    'field.radio': 'Radio Button',
    'field.checkbox': 'Checkbox',
    'field.switch': 'Switch',
    'field.date': 'Date',
    'field.time': 'Time',
    'field.datetime': 'Date Time',
    'field.number': 'Number',
    'field.slider': 'Slider',
    'field.rate': 'Rating',
    'field.upload': 'File Upload',
    
    // Placeholders
    'placeholder.input': 'Please enter {field}',
    'placeholder.select': 'Please select {field}',
    'placeholder.date': 'Please select date',
    'placeholder.time': 'Please select time',
    
    // Help text
    'help.required': 'This field is required',
    'help.optional': 'This field is optional',
    'help.format': 'Please enter in the specified format'
  },
  'ja-JP': {
    // フォーム共通
    'form.submit': '送信',
    'form.reset': 'リセット',
    'form.required': '{field}を入力してください',
    'form.loading': '送信中...',
    
    // 検証メッセージ
    'validation.required': '{field}は必須項目です',
    'validation.min': '{field}は{min}文字以上である必要があります',
    'validation.max': '{field}は{max}文字以下である必要があります',
    'validation.email': '有効なメールアドレスを入力してください',
    'validation.phone': '有効な電話番号を入力してください',
    'validation.idcard': '有効なIDカード番号を入力してください',
    'validation.noChinese': '{field}に中国語文字を含めることはできません',
    'validation.hasNumber': '{field}には数字を含める必要があります',
    'validation.hasSpecialChar': '{field}には特殊文字を含める必要があります',
    'validation.pattern': '{field}の形式が正しくありません',
    
    // エラー処理
    'error.unknown': '不明なエラーが発生しました',
    'error.network': 'ネットワーク接続に失敗しました。ネットワーク設定を確認してください',
    'error.server': 'サーバーエラーです。後でもう一度お試しください',
    'error.validation': 'フォーム検証に失敗しました。入力内容を確認してください',
    'error.fileSize': 'ファイルサイズは{size}を超えることはできません',
    'error.fileType': 'サポートされていないファイルタイプです',
    
    // 成功メッセージ
    'success.submit': 'フォームが正常に送信されました',
    'success.save': '正常に保存されました',
    'success.reset': 'フォームがリセットされました',
    'success.recover': 'フォームが復元されました',
    
    // 操作プロンプト
    'action.upload': 'ファイルアップロード',
    'action.delete': '削除',
    'action.edit': '編集',
    'action.add': '追加',
    'action.cancel': 'キャンセル',
    'action.confirm': '確認',
    'action.retry': '再試行',
    'action.back': '戻る',
    'action.continue': '続行',
    
    // フィールドタイプ
    'field.input': '単一行テキスト',
    'field.textarea': '複数行テキスト',
    'field.select': 'ドロップダウン選択',
    'field.radio': 'ラジオボタン',
    'field.checkbox': 'チェックボックス',
    'field.switch': 'スイッチ',
    'field.date': '日付',
    'field.time': '時間',
    'field.datetime': '日付時間',
    'field.number': '数値',
    'field.slider': 'スライダー',
    'field.rate': '評価',
    'field.upload': 'ファイルアップロード',
    
    // プレースホルダー
    'placeholder.input': '{field}を入力してください',
    'placeholder.select': '{field}を選択してください',
    'placeholder.date': '日付を選択してください',
    'placeholder.time': '時間を選択してください',
    
    // ヘルプテキスト
    'help.required': 'この項目は必須です',
    'help.optional': 'この項目はオプションです',
    'help.format': '指定された形式で入力してください'
  }
}

/**
 * 国际化管理器
 */
export class FormI18nManager {
  /**
   * 设置当前语言
   */
  static setLocale(locale: string) {
    if (!messages[locale]) {
      console.warn(`不支持的 locale: ${locale}`)
      return
    }
    
    currentLocale.value = locale
    localStorage.setItem('form-locale', locale)
  }

  /**
   * 获取当前语言
   */
  static getCurrentLocale(): string {
    return currentLocale.value
  }

  /**
   * 获取支持的语言列表
   */
  static getSupportedLocales() {
    return SUPPORTED_LOCALES
  }

  /**
   * 翻译文本
   */
  static t(key: string, params?: Record<string, any>): string {
    const locale = currentLocale.value
    const message = messages[locale]?.[key] || messages[DEFAULT_LOCALE]?.[key] || key
    
    // 参数替换
    if (params) {
      return message.replace(/{(\w+)}/g, (match, param) => {
        return params[param] !== undefined ? String(params[param]) : match
      })
    }
    
    return message
  }

  /**
   * 添加语言包
   */
  static addMessages(locale: string, newMessages: Record<string, string>) {
    if (!messages[locale]) {
      messages[locale] = {}
    }
    
    Object.assign(messages[locale], newMessages)
  }

  /**
   * 初始化国际化
   */
  static init() {
    // 从本地存储获取语言设置
    const savedLocale = localStorage.getItem('form-locale')
    if (savedLocale && messages[savedLocale]) {
      currentLocale.value = savedLocale
    } else {
      // 尝试从浏览器获取语言设置
      const browserLocale = navigator.language || (navigator as any).userLanguage
      const matchedLocale = SUPPORTED_LOCALES.find(item => item.value.startsWith(browserLocale))?.value
      if (matchedLocale && messages[matchedLocale]) {
        currentLocale.value = matchedLocale
      }
    }
  }
}

// 计算属性：当前语言的消息
const currentMessages = computed(() => {
  return messages[currentLocale.value] || messages[DEFAULT_LOCALE] || {}
})

// 导出翻译函数
export const t = FormI18nManager.t

// 初始化国际化
FormI18nManager.init()