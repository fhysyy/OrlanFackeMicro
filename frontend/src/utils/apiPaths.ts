// API路径常量管理文件
// 统一管理所有API端点，确保路径一致性和易于维护

/**
 * 基础API前缀
 */
export const API_PREFIX = '/api';

/**
 * 认证相关API路径
 */
export const AUTH_API = {
  LOGIN: `${API_PREFIX}/auth/login`,
  REGISTER: `${API_PREFIX}/auth/register`,
  LOGOUT: `${API_PREFIX}/auth/logout`,
  REFRESH_TOKEN: `${API_PREFIX}/auth/refresh`,
  VERIFY_EMAIL: `${API_PREFIX}/auth/verify-email`,
  RESET_PASSWORD: `${API_PREFIX}/auth/reset-password`
};

/**
 * 用户相关API路径
 */
export const USER_API = {
  ME: `${API_PREFIX}/admin/user/me`,
  LIST: `${API_PREFIX}/admin/users`,
  GET: (id: string) => `${API_PREFIX}/admin/user/${id}`,
  CREATE: `${API_PREFIX}/auth/register`, // 使用Auth控制器注册用户
  UPDATE: (id: string) => `${API_PREFIX}/admin/user/${id}`,
  DELETE: (id: string) => `${API_PREFIX}/admin/user/${id}`,
  UPDATE_STATUS: (id: string) => `${API_PREFIX}/admin/user/${id}/status`,
  UPDATE_ROLES: (id: string) => `${API_PREFIX}/admin/user/${id}/roles`
};

/**
 * 角色相关API路径
 */
export const ROLE_API = {
  LIST: `${API_PREFIX}/permission/role`,
  GET: (id: string) => `${API_PREFIX}/permission/role/${id}`,
  CREATE: `${API_PREFIX}/permission/role`,
  UPDATE: (id: string) => `${API_PREFIX}/permission/role/${id}`,
  DELETE: (id: string) => `${API_PREFIX}/permission/role/${id}`,
  UPDATE_PERMISSIONS: (id: string) => `${API_PREFIX}/permission/role/${id}/permissions`
};

/**
 * 权限相关API路径
 */
export const PERMISSION_API = {
  LIST: `${API_PREFIX}/permission`,
  GROUPS: `${API_PREFIX}/permission/groups`
};

/**
 * 班级相关API路径
 */
export const CLASS_API = {
  LIST: `${API_PREFIX}/admin/classes`,
  GET: (id: string) => `${API_PREFIX}/admin/classes/${id}`,
  CREATE: `${API_PREFIX}/admin/classes`,
  UPDATE: (id: string) => `${API_PREFIX}/admin/classes/${id}`,
  DELETE: (id: string) => `${API_PREFIX}/admin/classes/${id}`
};

/**
 * 学生相关API路径
 */
export const STUDENT_API = {
  LIST: `${API_PREFIX}/admin/students`,
  GET: (id: string) => `${API_PREFIX}/admin/students/${id}`,
  CREATE: `${API_PREFIX}/admin/students`,
  UPDATE: (id: string) => `${API_PREFIX}/admin/students/${id}`,
  DELETE: (id: string) => `${API_PREFIX}/admin/students/${id}`,
  BY_CLASS: (classId: string) => `${API_PREFIX}/admin/classes/${classId}/students`
};

/**
 * 成绩相关API路径
 */
export const SCORE_API = {
  LIST: `${API_PREFIX}/admin/scores`,
  GET: (id: string) => `${API_PREFIX}/admin/scores/${id}`,
  CREATE: `${API_PREFIX}/admin/scores`,
  UPDATE: (id: string) => `${API_PREFIX}/admin/scores/${id}`,
  DELETE: (id: string) => `${API_PREFIX}/admin/scores/${id}`,
  BY_STUDENT: (studentId: string) => `${API_PREFIX}/admin/students/${studentId}/scores`,
  BY_CLASS: (classId: string) => `${API_PREFIX}/admin/classes/${classId}/scores`,
  ANALYSIS: `${API_PREFIX}/admin/scores/analysis`
};

/**
 * 消息相关API路径
 */
export const MESSAGE_API = {
  LIST: `${API_PREFIX}/messages`,
  GET: (id: string) => `${API_PREFIX}/messages/${id}`,
  CREATE: `${API_PREFIX}/messages`,
  MARK_READ: (id: string) => `${API_PREFIX}/messages/${id}/read`,
  MARK_ALL_READ: `${API_PREFIX}/messages/read-all`,
  UNREAD_COUNT: `${API_PREFIX}/messages/unread-count`
};

/**
 * 文件相关API路径
 */
export const FILE_API = {
  UPLOAD: `${API_PREFIX}/files/upload`,
  LIST: `${API_PREFIX}/files`,
  GET: (id: string) => `${API_PREFIX}/files/${id}`,
  DELETE: (id: string) => `${API_PREFIX}/files/${id}`,
  DOWNLOAD: (id: string) => `${API_PREFIX}/files/${id}/download`
};

/**
 * 系统监控相关API路径
 */
export const SYSTEM_API = {
  DASHBOARD: `${API_PREFIX}/system/dashboard`,
  LOGS: `${API_PREFIX}/system/logs`,
  METRICS: `${API_PREFIX}/system/metrics`,
  HEALTH: `${API_PREFIX}/system/health`
};

/**
 * 字典相关API路径
 */
export const DICTIONARY_API = {
  TYPES: `${API_PREFIX}/dictionary/types`,
  ITEMS: (typeId: string) => `${API_PREFIX}/dictionary/types/${typeId}/items`
};
