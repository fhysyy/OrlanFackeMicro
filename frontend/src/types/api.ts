// API响应类型定义
/**
 * API响应基础格式
 */
export interface ApiResponse<T = any> {
  success: boolean
  data?: T
  message?: string
  error?: string
  code?: string
  timestamp?: number
}

/**
 * 分页响应格式
 */
export interface PaginatedResponse<T = any> extends ApiResponse<T[]> {
  pagination: {
    page: number
    size: number
    total: number
    pages: number
  }
}

/**
 * 用户角色枚举
 */
export enum UserRole {
  User = 'User',
  Admin = 'Admin',
  SystemAdmin = 'SystemAdmin'
}

/**
 * 用户状态枚举
 */
export enum UserStatus {
  Active = 'Active',
  Inactive = 'Inactive',
  Locked = 'Locked',
  Pending = 'Pending'
}

/**
 * 用户信息接口
 */
export interface User {
  id: string
  username: string
  email: string
  phone?: string
  role: UserRole
  status: UserStatus
  createdAt: string
  updatedAt: string
  lastLoginAt?: string
  avatar?: string
  name?: string
  department?: string
  position?: string
}

/**
 * 认证响应接口
 */
export interface AuthResponse {
  token: string
  refreshToken?: string
  user: User
}

/**
 * 登录请求接口
 */
export interface LoginRequest {
  username: string
  usernameOrEmail: string
  password: string
  rememberMe?: boolean
}

/**
 * 注册请求接口
 */
export interface RegisterRequest {
  username: string
  email: string
  password: string
  confirmPassword: string
  name?: string
}

/**
 * 角色接口
 */
export interface Role {
  id: string
  name: string
  description?: string
  permissions: string[]
  createdAt: string
  updatedAt: string
  isSystem?: boolean
}

/**
 * 权限接口
 */
export interface Permission {
  id: string
  name: string
  code: string
  description?: string
  group?: string
  category?: string
  createdAt: string
  updatedAt: string
}

/**
 * 权限组接口
 */
export interface PermissionGroup {
  id: string
  name: string
  permissions: Permission[]
  description?: string
}

// 用户相关类型
export interface User {
  id: string
  username: string
  email: string
  phone?: string
  role: UserRole
  status: UserStatus
  createdAt: string
  updatedAt: string
}

export enum UserRole {
  User = 'User',
  Admin = 'Admin',
  SystemAdmin = 'SystemAdmin'
}

export enum UserStatus {
  Pending = 'Pending',
  Active = 'Active',
  Disabled = 'Disabled',
  Locked = 'Locked'
}

export interface LoginRequest {
  username: string
  password: string
}

export interface RegisterRequest {
  username: string
  email: string
  password: string
  phone?: string
}

export interface AuthResponse {
  success: boolean
  token?: string
  refreshToken?: string
  expiresAt?: string
  user?: User
  errorMessage?: string
}

// 消息相关类型
export interface Message {
  id: string
  senderId: string
  receiverId?: string
  receiverEmail?: string
  receiverPhone?: string
  title: string
  content: string
  messageType: MessageType
  channel: MessageChannel
  status: MessageStatus
  sentAt?: string
  deliveredAt?: string
  readAt?: string
  failedAt?: string
  retryCount: number
  errorMessage?: string
  metadata?: any
  scheduledAt?: string
  expiresAt?: string
  createdAt: string
  updatedAt: string
}

export enum MessageType {
  System = 'System',
  Notification = 'Notification',
  Reminder = 'Reminder',
  Marketing = 'Marketing',
  Verification = 'Verification',
  Warning = 'Warning',
  Error = 'Error',
  Success = 'Success',
  Info = 'Info',
  Custom = 'Custom'
}

export enum MessageChannel {
  InApp = 'InApp',
  Email = 'Email',
  SMS = 'SMS',
  Push = 'Push',
  WeChat = 'WeChat',
  DingTalk = 'DingTalk',
  Webhook = 'Webhook',
  MultiChannel = 'MultiChannel'
}

export enum MessageStatus {
  Draft = 'Draft',
  Pending = 'Pending',
  Sending = 'Sending',
  Sent = 'Sent',
  Delivered = 'Delivered',
  Read = 'Read',
  Failed = 'Failed',
  Cancelled = 'Cancelled',
  Expired = 'Expired'
}

// 文件相关类型
export interface FileInfo {
  id: string
  fileName: string
  filePath: string
  fileSize: number
  mimeType?: string
  uploaderId?: string
  isPublic: boolean
  createdAt: string
  updatedAt: string
}

// 系统监控类型
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

// 角色相关类型
export interface Role {
  id: string
  name: string
  description: string
  permissions: string[]
  createdAt: string
  updatedAt: string
  isSystem?: boolean
}

// 权限相关类型
export interface Permission {
  id: string
  name: string
  description: string
  code: string
  category: string
  parentId?: string
  createdAt: string
  updatedAt: string
}

// 权限分组
export interface PermissionGroup {
  id: string
  name: string
  description: string
  permissions: Permission[]
}