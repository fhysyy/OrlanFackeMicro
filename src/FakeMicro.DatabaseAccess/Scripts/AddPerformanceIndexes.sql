-- ========================================
-- Performance Optimization Indexes
-- For FakeMicro PostgreSQL Database
-- ========================================

-- ========================================
-- 1. Users Table Indexes
-- ========================================
-- Username lookup (登录、查询最常用)
CREATE INDEX IF NOT EXISTS idx_users_username 
    ON users (username) 
    WHERE deleted_at IS NULL;

-- Email lookup (邮箱登录、密码重置)
CREATE INDEX IF NOT EXISTS idx_users_email 
    ON users (email) 
    WHERE deleted_at IS NULL;

-- Phone lookup
CREATE INDEX IF NOT EXISTS idx_users_phone 
    ON users (phone) 
    WHERE deleted_at IS NULL AND phone IS NOT NULL AND phone != '';

-- Status + Active filter (管理员查询活跃用户)
CREATE INDEX IF NOT EXISTS idx_users_status_active 
    ON users (status, is_active) 
    WHERE deleted_at IS NULL;

-- CreatedAt for statistics (新用户统计、时间范围查询)
CREATE INDEX IF NOT EXISTS idx_users_createdat 
    ON users (createdat DESC) 
    WHERE deleted_at IS NULL;

-- Last login time (活跃度分析)
CREATE INDEX IF NOT EXISTS idx_users_last_login 
    ON users (last_login_at DESC NULLS LAST) 
    WHERE deleted_at IS NULL;

-- Composite index for common admin queries
CREATE INDEX IF NOT EXISTS idx_users_role_status_createdat 
    ON users (role, status, createdat DESC) 
    WHERE deleted_at IS NULL;

-- ========================================
-- 2. Roles Table Indexes
-- ========================================
CREATE INDEX IF NOT EXISTS idx_roles_name 
    ON roles (name) 
    WHERE deleted_at IS NULL;

CREATE INDEX IF NOT EXISTS idx_roles_status 
    ON roles (status) 
    WHERE deleted_at IS NULL;

CREATE INDEX IF NOT EXISTS idx_roles_createdat 
    ON roles (createdat DESC);

-- ========================================
-- 3. Permissions Table Indexes
-- ========================================
CREATE INDEX IF NOT EXISTS idx_permissions_resource 
    ON permissions (resource) 
    WHERE deleted_at IS NULL;

CREATE INDEX IF NOT EXISTS idx_permissions_type 
    ON permissions (type) 
    WHERE deleted_at IS NULL;

-- Composite for permission checks
CREATE INDEX IF NOT EXISTS idx_permissions_resource_type 
    ON permissions (resource, type) 
    WHERE deleted_at IS NULL;

-- ========================================
-- 4. User_Roles Table Indexes
-- ========================================
CREATE INDEX IF NOT EXISTS idx_user_roles_userid 
    ON user_roles (user_id) 
    WHERE deleted_at IS NULL;

CREATE INDEX IF NOT EXISTS idx_user_roles_roleid 
    ON user_roles (role_id) 
    WHERE deleted_at IS NULL;

-- Composite for role lookups
CREATE INDEX IF NOT EXISTS idx_user_roles_userid_roleid 
    ON user_roles (user_id, role_id) 
    WHERE deleted_at IS NULL;

CREATE INDEX IF NOT EXISTS idx_user_roles_createdat 
    ON user_roles (createdat DESC);

-- ========================================
-- 5. Role_Permissions Table Indexes
-- ========================================
CREATE INDEX IF NOT EXISTS idx_role_permissions_roleid 
    ON role_permissions (role_id) 
    WHERE deleted_at IS NULL;

CREATE INDEX IF NOT EXISTS idx_role_permissions_permissionid 
    ON role_permissions (permission_id) 
    WHERE deleted_at IS NULL;

-- Composite for permission checks
CREATE INDEX IF NOT EXISTS idx_role_permissions_roleid_permissionid 
    ON role_permissions (role_id, permission_id) 
    WHERE deleted_at IS NULL;

-- ========================================
-- 6. User_Permissions Table Indexes
-- ========================================
CREATE INDEX IF NOT EXISTS idx_user_permissions_userid 
    ON user_permissions (user_id) 
    WHERE deleted_at IS NULL;

CREATE INDEX IF NOT EXISTS idx_user_permissions_permissionid 
    ON user_permissions (permission_id) 
    WHERE deleted_at IS NULL;

-- Composite for direct permission checks
CREATE INDEX IF NOT EXISTS idx_user_permissions_userid_permissionid 
    ON user_permissions (user_id, permission_id) 
    WHERE deleted_at IS NULL;

-- ========================================
-- 7. Sessions Table Indexes (如果存在)
-- ========================================
-- CREATE INDEX IF NOT EXISTS idx_sessions_userid 
--     ON sessions (user_id) 
--     WHERE deleted_at IS NULL;

-- CREATE INDEX IF NOT EXISTS idx_sessions_token 
--     ON sessions (refresh_token) 
--     WHERE deleted_at IS NULL AND refresh_token IS NOT NULL;

-- CREATE INDEX IF NOT EXISTS idx_sessions_expiry 
--     ON sessions (expires_at) 
--     WHERE deleted_at IS NULL;

-- ========================================
-- 8. Audit Logs Indexes (如果存在)
-- ========================================
-- CREATE INDEX IF NOT EXISTS idx_audit_logs_userid 
--     ON audit_logs (user_id);

-- CREATE INDEX IF NOT EXISTS idx_audit_logs_createdat 
--     ON audit_logs (createdat DESC);

-- CREATE INDEX IF NOT EXISTS idx_audit_logs_action 
--     ON audit_logs (action);

-- ========================================
-- Analyze Tables for Statistics
-- ========================================
ANALYZE users;
ANALYZE roles;
ANALYZE permissions;
ANALYZE user_roles;
ANALYZE role_permissions;
ANALYZE user_permissions;

-- ========================================
-- Verification Query
-- ========================================
-- Check index usage
SELECT 
    schemaname,
    tablename,
    indexname,
    idx_scan as index_scans,
    idx_tup_read as tuples_read,
    idx_tup_fetch as tuples_fetched
FROM pg_stat_user_indexes
WHERE schemaname = 'public'
ORDER BY idx_scan DESC;

-- Check table sizes
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS total_size,
    pg_size_pretty(pg_relation_size(schemaname||'.'||tablename)) AS table_size,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename) - pg_relation_size(schemaname||'.'||tablename)) AS index_size
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
