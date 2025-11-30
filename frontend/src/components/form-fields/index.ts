// 导入所有表单字段组件
import FormFieldInput from './FormFieldInput.vue'
import FormFieldTextarea from './FormFieldTextarea.vue'
import FormFieldSelect from './FormFieldSelect.vue'
import FormFieldRadio from './FormFieldRadio.vue'
import FormFieldCheckbox from './FormFieldCheckbox.vue'
import FormFieldSwitch from './FormFieldSwitch.vue'
import FormFieldDatePicker from './FormFieldDatePicker.vue'
import FormFieldTimePicker from './FormFieldTimePicker.vue'
import FormFieldTime from './FormFieldTime.vue'
import FormFieldDatetimePicker from './FormFieldDatetimePicker.vue'
import FormFieldInputNumber from './FormFieldInputNumber.vue'
import FormFieldSlider from './FormFieldSlider.vue'
import FormFieldRate from './FormFieldRate.vue'
import FormFieldUpload from './FormFieldUpload.vue'
import FormLayout from './FormLayout.vue'

// 导出所有表单字段组件
export { FormFieldInput, FormFieldTextarea, FormFieldSelect, FormFieldRadio, FormFieldCheckbox, FormFieldSwitch, FormFieldDatePicker, FormFieldTimePicker, FormFieldTime, FormFieldDatetimePicker, FormFieldInputNumber, FormFieldSlider, FormFieldRate, FormFieldUpload, FormLayout }

// 直接导入并重新导出类型定义
import type { BaseFormFieldProps, FormLayout, FormLayoutConfig } from './types'
import { FormFieldComponents } from './types'

export type { BaseFormFieldProps, FormLayoutConfig }
export { FormFieldComponents }
export type { FormLayout as FormLayoutType }

// 导出所有组件的映射
export const AllFormFieldComponents = {
  FormFieldInput,
  FormFieldTextarea,
  FormFieldSelect,
  FormFieldRadio,
  FormFieldCheckbox,
  FormFieldSwitch,
  FormFieldDatePicker,
  FormFieldTimePicker,
  FormFieldTime,
  FormFieldDatetimePicker,
  FormFieldInputNumber,
  FormFieldSlider,
  FormFieldRate,
  FormFieldUpload,
  FormLayout
}
