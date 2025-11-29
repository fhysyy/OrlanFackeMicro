// 字典类型
interface DictionaryType {
  id: string;
  code: string;
  name: string;
  description?: string;
  isEnabled: boolean;
  sortOrder: number;
  createdAt: string;
  updatedAt: string;
}

// 字典项
interface DictionaryItem {
  id: string;
  dictionaryTypeId: string;
  value: string;
  text: string;
  description?: string;
  isEnabled: boolean;
  sortOrder: number;
  extraData?: string;
  createdAt: string;
  updatedAt: string;
}

// 创建字典类型请求
interface DictionaryTypeCreateRequest {
  code: string;
  name: string;
  description?: string;
  isEnabled?: boolean;
  sortOrder?: number;
}

// 更新字典类型请求
interface DictionaryTypeUpdateRequest {
  code: string;
  name: string;
  description?: string;
  isEnabled: boolean;
  sortOrder: number;
}

// 创建字典项请求
interface DictionaryItemCreateRequest {
  dictionaryTypeId: string;
  value: string;
  text: string;
  description?: string;
  isEnabled?: boolean;
  sortOrder?: number;
  extraData?: string;
}

// 更新字典项请求
interface DictionaryItemUpdateRequest {
  value: string;
  text: string;
  description?: string;
  isEnabled: boolean;
  sortOrder: number;
  extraData?: string;
}

export type {
  DictionaryType,
  DictionaryItem,
  DictionaryTypeCreateRequest,
  DictionaryTypeUpdateRequest,
  DictionaryItemCreateRequest,
  DictionaryItemUpdateRequest
}