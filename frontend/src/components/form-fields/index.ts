// 导出所有表单字段组件
export { default as FormFieldInput } from './FormFieldInput.vue';
export { default as FormFieldTextarea } from './FormFieldTextarea.vue';
export { default as FormFieldSelect } from './FormFieldSelect.vue';
export { default as FormFieldRadio } from './FormFieldRadio.vue';
export { default as FormFieldCheckbox } from './FormFieldCheckbox.vue';
export { default as FormFieldSwitch } from './FormFieldSwitch.vue';
export { default as FormFieldDatePicker } from './FormFieldDatePicker.vue';
export { default as FormFieldTimePicker } from './FormFieldTimePicker.vue';
export { default as FormFieldTime } from './FormFieldTime.vue';
export { default as FormFieldDatetimePicker } from './FormFieldDatetimePicker.vue';
export { default as FormFieldInputNumber } from './FormFieldInputNumber.vue';
export { default as FormFieldSlider } from './FormFieldSlider.vue';
export { default as FormFieldRate } from './FormFieldRate.vue';
export { default as FormFieldUpload } from './FormFieldUpload.vue';
export { default as FormLayout } from './FormLayout.vue';

// 重新导出类型定义 - 确保BaseFormFieldProps被正确导出
export { BaseFormFieldProps, FormFieldComponents, FormLayoutConfig } from './types';
export { FormLayout as FormLayoutType } from './types';

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
};
