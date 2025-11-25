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

// 考试相关类型
export interface Exam {
  id: string
  title: string
  description: string
  duration: number // 考试时长（分钟）
  totalScore: number
  passScore: number
  status: ExamStatus
  createdAt: string
  updatedAt: string
  startTime?: string
  endTime?: string
}

export enum ExamStatus {
  Draft = 'Draft',
  Published = 'Published',
  InProgress = 'InProgress',
  Completed = 'Completed',
  Cancelled = 'Cancelled'
}

export interface ExamQuestion {
  id: string
  examId: string
  type: QuestionType
  content: string
  options?: string[]
  correctAnswer: string | string[]
  score: number
  order: number
}

export enum QuestionType {
  SingleChoice = 'SingleChoice',
  MultipleChoice = 'MultipleChoice',
  TrueFalse = 'TrueFalse',
  ShortAnswer = 'ShortAnswer',
  Essay = 'Essay'
}

export interface ExamParticipant {
  id: string
  examId: string
  userId: string
  username: string
  startTime?: string
  endTime?: string
  score?: number
  status: ParticipantStatus
}

export enum ParticipantStatus {
  NotStarted = 'NotStarted',
  InProgress = 'InProgress',
  Submitted = 'Submitted',
  Graded = 'Graded'
}