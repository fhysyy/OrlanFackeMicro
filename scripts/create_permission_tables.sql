-- FakeMicro 权限系统 PostgreSQL 数据库创建脚本
-- 基于 Permission 和 RolePermission 实体类生成
-- 生成时间: 2025-10-26

-- =============================================
-- 权限表
-- =============================================
CREATE TABLE permissions (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    code VARCHAR(50) NOT NULL UNIQUE,
    resource VARCHAR(50) NOT NULL,
    type VARCHAR(20) NOT NULL,
    description VARCHAR(500),
    is_enabled BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    tenant_id VARCHAR(50) -- 多租户支持
);

-- 为权限表创建索引
CREATE INDEX idx_permissions_code ON permissions(code);
CREATE INDEX idx_permissions_resource ON permissions(resource);
CREATE INDEX idx_permissions_type ON permissions(type);
CREATE INDEX idx_permissions_tenant_id ON permissions(tenant_id);
CREATE INDEX idx_permissions_is_enabled ON permissions(is_enabled);

-- =============================================
-- 角色权限关联表
-- =============================================
CREATE TABLE role_permissions (
    id BIGSERIAL PRIMARY KEY,
    role_id BIGINT NOT NULL,
    permission_id BIGINT NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    tenant_id VARCHAR(50), -- 多租户支持
    
    -- 注意：在Orleans分布式环境中，外键约束可能会影响性能
    -- 以下外键约束仅作为参考，实际使用时请根据性能需求考虑是否保留
    -- FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE CASCADE,
    -- FOREIGN KEY (permission_id) REFERENCES permissions(id) ON DELETE CASCADE,
    
    -- 添加唯一约束，确保一个角色不能重复拥有同一个权限
    UNIQUE(role_id, permission_id)
);

-- 为角色权限关联表创建索引
CREATE INDEX idx_role_permissions_role_id ON role_permissions(role_id);
CREATE INDEX idx_role_permissions_permission_id ON role_permissions(permission_id);
CREATE INDEX idx_role_permissions_tenant_id ON role_permissions(tenant_id);

-- =============================================
-- 可选：插入默认权限数据
-- =============================================
INSERT INTO permissions (name, code, resource, type, description, is_enabled)
VALUES 
    ('查看用户', 'user:read', 'user', 'read', '允许查看用户信息', true),
    ('创建用户', 'user:create', 'user', 'create', '允许创建新用户', true),
    ('更新用户', 'user:update', 'user', 'update', '允许更新用户信息', true),
    ('删除用户', 'user:delete', 'user', 'delete', '允许删除用户', true),
    ('查看角色', 'role:read', 'role', 'read', '允许查看角色信息', true),
    ('创建角色', 'role:create', 'role', 'create', '允许创建新角色', true),
    ('更新角色', 'role:update', 'role', 'update', '允许更新角色信息', true),
    ('删除角色', 'role:delete', 'role', 'delete', '允许删除角色', true),
    ('查看权限', 'permission:read', 'permission', 'read', '允许查看权限信息', true),
    ('分配权限', 'permission:assign', 'permission', 'assign', '允许为角色分配权限', true);

-- =============================================
-- 权限表注释
-- =============================================
COMMENT ON TABLE permissions IS '系统权限定义表，存储所有可用权限';
COMMENT ON COLUMN permissions.id IS '权限ID';
COMMENT ON COLUMN permissions.name IS '权限名称';
COMMENT ON COLUMN permissions.code IS '权限代码，唯一标识符';
COMMENT ON COLUMN permissions.resource IS '资源类型，如user、role等';
COMMENT ON COLUMN permissions.type IS '权限类型，如read、create、update、delete等';
COMMENT ON COLUMN permissions.description IS '权限描述';
COMMENT ON COLUMN permissions.is_enabled IS '是否启用';
COMMENT ON COLUMN permissions.created_at IS '创建时间';
COMMENT ON COLUMN permissions.updated_at IS '更新时间';
COMMENT ON COLUMN permissions.tenant_id IS '租户ID，用于多租户隔离';

COMMENT ON TABLE role_permissions IS '角色权限关联表，存储角色与权限的多对多关系';
COMMENT ON COLUMN role_permissions.id IS '关联ID';
COMMENT ON COLUMN role_permissions.role_id IS '角色ID';
COMMENT ON COLUMN role_permissions.permission_id IS '权限ID';
COMMENT ON COLUMN role_permissions.created_at IS '创建时间';
COMMENT ON COLUMN role_permissions.tenant_id IS '租户ID，用于多租户隔离';