// API 响应通用类型
export interface ApiResponse<T = unknown> {
  success: boolean
  data?: T
  message?: string
  errorMessage?: string
  code?: string | number
}

// 用户相关类型
export interface User {
  id: string
  username: string
  email: string
  role: UserRole
  avatar?: string
  createdAt: string
  updatedAt: string
}

export enum UserRole {
  User = 'User',
  Admin = 'Admin',
  SystemAdmin = 'SystemAdmin'
}

export enum UserStatus {
  Active = 'Active',
  Inactive = 'Inactive',
  Suspended = 'Suspended'
}

// 认证相关类型
export interface LoginRequest {
  username: string
  password: string
}

export interface RegisterRequest {
  username: string
  email: string
  password: string
  confirmPassword: string
}

export interface AuthResponse {
  success: boolean
  token: string
  refreshToken?: string
  user: User
  message?: string
  errorMessage?: string
}

// 分页相关类型
export interface PaginationParams {
  page: number
  pageSize: number
  search?: string
  sortBy?: string
  sortOrder?: 'asc' | 'desc'
}

export interface PaginationResponse<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
  totalPages: number
}

// 错误类型
export interface ApiError {
  code: string
  message: string
  details?: Record<string, unknown>
}

// HTTP 状态码类型
export type HttpStatus = 
  | 200 | 201 | 204 // 成功
  | 400 | 401 | 403 | 404 // 客户端错误
  | 500 | 502 | 503 // 服务器错误

// 请求方法类型
export type HttpMethod = 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH'

// 通用请求配置
export interface RequestConfig {
  method?: HttpMethod
  url: string
  params?: Record<string, string | number>
  data?: Record<string, unknown>
  headers?: Record<string, string>
  timeout?: number
}

// 系统统计相关类型
export interface SystemStats {
  userCount: number
  activeUsers: number
  messageCount: number
  pendingMessages: number
  fileCount: number
  totalFileSize: number
  systemUptime: string
  memoryUsage: number
  cpuUsage: number
}

// 消息相关类型
export interface Message {
  id: string
  title: string
  content: string
  type: MessageType
  status: MessageStatus
  recipient: string
  sender: string
  createdAt: string
  updatedAt: string
}

export enum MessageType {
  Email = 'Email',
  SMS = 'SMS',
  InApp = 'InApp'
}

export enum MessageStatus {
  Pending = 'Pending',
  Sent = 'Sent',
  Delivered = 'Delivered',
  Failed = 'Failed'
}

// 文件相关类型
export interface FileInfo {
  id: string
  name: string
  originalName: string
  size: number
  type: string
  path: string
  url?: string
  uploadedBy: string
  createdAt: string
}

// 活动日志类型
export interface Activity {
  id: string
  action: string
  details: string
  userId: string
  username: string
  timestamp: string
  type: ActivityType
}

export enum ActivityType {
  Login = 'Login',
  Logout = 'Logout',
  Create = 'Create',
  Update = 'Update',
  Delete = 'Delete',
  Error = 'Error'
}