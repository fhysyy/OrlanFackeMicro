// 前端类型定义主文件
export * from './api'
export * from './dictionary'
export * from './class'
export * from './student'
export * from './score'

// 用户相关类型
export interface User {
  id: string
  username: string
  email: string
  phone?: string
  role: UserRole
  status: UserStatus
  createdAt: string
  lastLoginAt?: string
  avatar?: string
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

// 消息相关类型
export interface Message {
  id: string
  title: string
  content: string
  type: MessageType
  status: MessageStatus
  senderId: string
  receiverId: string
  createdAt: string
  sentAt?: string
  readAt?: string
}

export enum MessageType {
  System = 'System',
  Notification = 'Notification',
  Alert = 'Alert',
  Marketing = 'Marketing'
}

export enum MessageStatus {
  Draft = 'Draft',
  Pending = 'Pending',
  Sent = 'Sent',
  Failed = 'Failed',
  Read = 'Read'
}

// 文件相关类型
export interface FileInfo {
  id: string
  name: string
  size: number
  type: string
  url: string
  uploaderId: string
  createdAt: string
  updatedAt: string
}

// 系统统计类型
export interface SystemStats {
  userCount: number
  activeUserCount: number
  messageCount: number
  fileCount: number
  totalFileSize: number
  systemLoad: number
  memoryUsage: number
  diskUsage: number
}

// 分页参数类型
export interface PaginationParams {
  page: number
  size: number
  total: number
}

export interface SearchParams {
  keyword?: string
  status?: string
  type?: string
  startDate?: string
  endDate?: string
}

// 表单验证类型
export interface LoginForm {
  username: string
  password: string
  rememberMe: boolean
}

export interface RegisterForm {
  username: string
  email: string
  password: string
  confirmPassword: string
  phone?: string
}

export interface ChangePasswordForm {
  currentPassword: string
  newPassword: string
  confirmPassword: string
}

// 路由元信息类型
export interface RouteMeta {
  title: string
  requiresAuth: boolean
  roles?: UserRole[]
  icon?: string
  keepAlive?: boolean
}

// 菜单项类型
export interface MenuItem {
  key: string
  label: string
  icon?: string
  path?: string
  children?: MenuItem[]
  roles?: UserRole[]
}

// 表格列配置类型
export interface TableColumn {
  prop: string
  label: string
  width?: number
  minWidth?: number
  align?: 'left' | 'center' | 'right'
  sortable?: boolean | 'custom'
  fixed?: boolean | 'left' | 'right'
  showOverflowTooltip?: boolean
  formatter?: (row: any, column: any, cellValue: any, index: number) => string
  tag?: boolean
  tagMap?: Record<string | number, { type: string; text: string }>
  tagEffect?: 'dark' | 'light' | 'plain'
}

// 图表数据类型
export interface ChartData {
  labels: string[]
  datasets: {
    label: string
    data: number[]
    backgroundColor: string | string[]
    borderColor?: string | string[]
    borderWidth?: number
  }[]
}

export interface PieChartData {
  labels: string[]
  datasets: {
    data: number[]
    backgroundColor: string[]
    borderColor?: string[]
    borderWidth?: number
  }[]
}

// 通知类型
export interface Notification {
  id: string
  type: 'success' | 'warning' | 'error' | 'info'
  title: string
  message: string
  timestamp: number
  read: boolean
}

// 上传文件类型
export interface UploadFile {
  uid: string
  name: string
  status: 'uploading' | 'done' | 'error' | 'removed'
  percent?: number
  response?: any
  url?: string
}

// 响应式断点类型
export interface Breakpoints {
  xs: number
  sm: number
  md: number
  lg: number
  xl: number
  xxl: number
}

export const breakpoints: Breakpoints = {
  xs: 480,
  sm: 576,
  md: 768,
  lg: 992,
  xl: 1200,
  xxl: 1600
}

// 主题配置类型
export interface ThemeConfig {
  primaryColor: string
  successColor: string
  warningColor: string
  errorColor: string
  infoColor: string
  textColor: string
  textColorSecondary: string
  borderColor: string
  backgroundColor: string
}

export const defaultTheme: ThemeConfig = {
  primaryColor: '#409EFF',
  successColor: '#67C23A',
  warningColor: '#E6A23C',
  errorColor: '#F56C6C',
  infoColor: '#909399',
  textColor: '#303133',
  textColorSecondary: '#606266',
  borderColor: '#DCDFE6',
  backgroundColor: '#FFFFFF'
}

// 导出所有类型
export type {
  ApiResponse,
  PaginatedResponse,
  LoginResponse,
  UserResponse,
  MessageResponse,
  FileResponse,
  StatsResponse
} from './api'