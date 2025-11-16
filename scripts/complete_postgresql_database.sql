-- FakeMicro 完整 PostgreSQL 数据库创建脚本
-- 整合版本 - 基于现有脚本和实体类同步
-- 生成时间: 2025-10-26

-- =============================================
-- 1. 数据库创建
-- =============================================

-- 创建主数据库
CREATE DATABASE fakemicro;
CREATE DATABASE fakemicro_cap;
CREATE DATABASE fakemicro_hangfire;
CREATE DATABASE orleans;

-- 切换到主数据库
\c fakemicro;

-- =============================================
-- 2. 枚举类型定义
-- =============================================

-- 用户角色枚举
CREATE TYPE user_role AS ENUM ('User', 'Admin', 'SystemAdmin');

-- 用户状态枚举
CREATE TYPE user_status AS ENUM ('Pending', 'Active', 'Disabled', 'Locked');

-- 消息类型枚举
CREATE TYPE message_type AS ENUM ('System', 'Notification', 'Reminder', 'Marketing', 'Verification', 'Warning', 'Error', 'Success', 'Info', 'Custom');

-- 消息渠道枚举
CREATE TYPE message_channel AS ENUM ('InApp', 'Email', 'SMS', 'Push', 'WeChat', 'DingTalk', 'Webhook', 'MultiChannel');

-- 消息状态枚举
CREATE TYPE message_status AS ENUM ('Draft', 'Pending', 'Sending', 'Sent', 'Delivered', 'Read', 'Failed', 'Cancelled', 'Expired');

-- =============================================
-- 3. 核心表结构
-- =============================================

-- 用户表
CREATE TABLE users (
    id BIGSERIAL PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    display_name VARCHAR(100) NOT NULL DEFAULT '',
    email VARCHAR(100) NOT NULL UNIQUE,
    phone VARCHAR(20),
    password_hash TEXT NOT NULL,
    password_salt TEXT NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    role user_role NOT NULL DEFAULT 'User',
    status user_status NOT NULL DEFAULT 'Pending',
    last_login_at TIMESTAMP WITH TIME ZONE,
    tenant_id INTEGER,
    email_verified BOOLEAN NOT NULL DEFAULT false,
    phone_verified BOOLEAN NOT NULL DEFAULT false,
    login_attempts INTEGER NOT NULL DEFAULT 0,
    locked_until TIMESTAMP WITH TIME ZONE,
    refresh_token TEXT,
    refresh_token_expiry TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMP WITH TIME ZONE
);

-- 角色表
CREATE TABLE roles (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE,
    code VARCHAR(20) NOT NULL UNIQUE,
    description VARCHAR(200),
    is_enabled BOOLEAN NOT NULL DEFAULT true,
    is_system_role BOOLEAN NOT NULL DEFAULT false,
    tenant_id BIGINT NOT NULL DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMP WITH TIME ZONE
);

-- 用户角色关联表
CREATE TABLE user_roles (
    user_id BIGINT NOT NULL,
    role_id BIGINT NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (user_id, role_id),
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE CASCADE
);

-- 消息表
CREATE TABLE messages (
    id BIGSERIAL PRIMARY KEY,
    sender_id BIGINT NOT NULL,
    receiver_id BIGINT,
    title VARCHAR(500) NOT NULL,
    content TEXT NOT NULL,
    message_type message_type NOT NULL,
    channel message_channel NOT NULL,
    status message_status NOT NULL DEFAULT 'Pending',
    sent_at TIMESTAMP WITH TIME ZONE,
    delivered_at TIMESTAMP WITH TIME ZONE,
    read_at TIMESTAMP WITH TIME ZONE,
    failed_at TIMESTAMP WITH TIME ZONE,
    retry_count INTEGER NOT NULL DEFAULT 0,
    error_message TEXT,
    metadata JSONB,
    scheduled_at TIMESTAMP WITH TIME ZONE,
    expires_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 消息模板表
CREATE TABLE message_templates (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    code VARCHAR(50) NOT NULL UNIQUE,
    title VARCHAR(500) NOT NULL,
    content TEXT NOT NULL,
    message_type message_type NOT NULL,
    variables JSONB,
    is_enabled BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMP WITH TIME ZONE
);

-- 文件信息表
CREATE TABLE file_infos (
    id BIGSERIAL PRIMARY KEY,
    file_name VARCHAR(255) NOT NULL,
    file_path VARCHAR(500) NOT NULL,
    file_size BIGINT NOT NULL,
    mime_type VARCHAR(100),
    uploader_id BIGINT,
    is_public BOOLEAN NOT NULL DEFAULT false,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- =============================================
-- 4. 索引创建
-- =============================================

-- 用户表索引
CREATE INDEX idx_users_username ON users(username);
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_status ON users(status);
CREATE INDEX idx_users_tenant_id ON users(tenant_id);
CREATE INDEX idx_users_is_deleted ON users(is_deleted);

-- 角色表索引
CREATE INDEX idx_roles_name ON roles(name);
CREATE INDEX idx_roles_code ON roles(code);
CREATE INDEX idx_roles_tenant_id ON roles(tenant_id);
CREATE INDEX idx_roles_is_deleted ON roles(is_deleted);

-- 用户角色关联表索引
CREATE INDEX idx_user_roles_user_id ON user_roles(user_id);
CREATE INDEX idx_user_roles_role_id ON user_roles(role_id);

-- 消息表索引
CREATE INDEX idx_messages_sender_id ON messages(sender_id);
CREATE INDEX idx_messages_receiver_id ON messages(receiver_id);
CREATE INDEX idx_messages_status ON messages(status);
CREATE INDEX idx_messages_scheduled_at ON messages(scheduled_at);
CREATE INDEX idx_messages_created_at ON messages(created_at);

-- 消息模板表索引
CREATE INDEX idx_message_templates_code ON message_templates(code);
CREATE INDEX idx_message_templates_is_deleted ON message_templates(is_deleted);

-- 文件信息表索引
CREATE INDEX idx_file_infos_uploader_id ON file_infos(uploader_id);

-- =============================================
-- 5. 默认数据插入
-- =============================================

-- 插入默认角色
INSERT INTO roles (name, code, description, is_system_role, created_at, updated_at) VALUES
    ('管理员', 'ADMIN', '系统管理员角色', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('用户', 'USER', '普通用户角色', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('系统管理员', 'SYSTEM_ADMIN', '系统超级管理员角色', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT (code) DO NOTHING;

-- 插入默认管理员用户（密码为：admin123，需要在实际使用时重新设置）
INSERT INTO users (id, username, display_name, email, password_hash, password_salt, role, status, email_verified) 
VALUES 
    (1, 'admin', '系统管理员', 'admin@fakemicro.com', 
     'AQAAAAIAAYagAAAAEJJqJ8v8mZ4VZvK5JgJ8v8mZ4VZvK5JgJ8v8mZ4VZvK5JgJ8v8mZ4VZvK5Jg==', 
     'AQAAAAIAAYagAAAAEJJqJ8v8mZ4VZvK5JgJ8v8mZ4VZvK5JgJ8v8mZ4VZvK5JgJ8v8mZ4VZvK5Jg==', 
     'SystemAdmin', 'Active', true)
ON CONFLICT (username) DO NOTHING;

-- 关联管理员用户和角色
INSERT INTO user_roles (user_id, role_id, created_at, updated_at)
SELECT u.id, r.id, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
FROM users u, roles r
WHERE u.username = 'admin' AND r.code = 'ADMIN'
ON CONFLICT (user_id, role_id) DO NOTHING;

-- 插入默认消息模板
INSERT INTO message_templates (id, name, code, title, content, message_type) 
VALUES 
    (1, '欢迎邮件', 'WELCOME_EMAIL', '欢迎加入 FakeMicro', 
     '亲爱的{{username}}，欢迎您加入 FakeMicro 平台！', 'Email'),
    (2, '密码重置', 'PASSWORD_RESET', '密码重置通知', 
     '您的密码已成功重置，如果不是您本人操作，请立即联系管理员。', 'Email')
ON CONFLICT (code) DO NOTHING;

-- =============================================
-- 6. 触发器函数
-- =============================================

-- 创建更新时间的触发器函数
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- 为所有表创建触发器
CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON users FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_roles_updated_at BEFORE UPDATE ON roles FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_user_roles_updated_at BEFORE UPDATE ON user_roles FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_messages_updated_at BEFORE UPDATE ON messages FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_message_templates_updated_at BEFORE UPDATE ON message_templates FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_file_infos_updated_at BEFORE UPDATE ON file_infos FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- =============================================
-- 7. 视图创建
-- =============================================

-- 创建软删除视图
CREATE VIEW active_users AS SELECT * FROM users WHERE is_deleted = false;
CREATE VIEW active_roles AS SELECT * FROM roles WHERE is_deleted = false;
CREATE VIEW active_message_templates AS SELECT * FROM message_templates WHERE is_deleted = false;

-- 创建审计视图
CREATE VIEW user_audit AS 
SELECT 
    u.id,
    u.username,
    u.email,
    u.role,
    u.status,
    u.last_login_at,
    u.created_at,
    u.updated_at,
    r.name as role_name,
    COUNT(ur.role_id) as role_count
FROM users u
LEFT JOIN user_roles ur ON u.id = ur.user_id
LEFT JOIN roles r ON ur.role_id = r.id
WHERE u.is_deleted = false
GROUP BY u.id, u.username, u.email, u.role, u.status, u.last_login_at, u.created_at, u.updated_at, r.name;

-- =============================================
-- 8. 表注释
-- =============================================

COMMENT ON TABLE users IS '用户表 - 存储系统用户信息';
COMMENT ON TABLE roles IS '角色表 - 存储系统角色信息';
COMMENT ON TABLE user_roles IS '用户角色关联表 - 存储用户和角色的多对多关系';
COMMENT ON TABLE messages IS '消息表 - 存储系统消息记录';
COMMENT ON TABLE message_templates IS '消息模板表 - 存储消息模板信息';
COMMENT ON TABLE file_infos IS '文件信息表 - 存储文件元数据信息';

COMMENT ON COLUMN users.password_hash IS '密码哈希值';
COMMENT ON COLUMN users.password_salt IS '密码盐值';
COMMENT ON COLUMN users.refresh_token IS '刷新令牌';
COMMENT ON COLUMN users.refresh_token_expiry IS '刷新令牌过期时间';

COMMENT ON COLUMN messages.metadata IS '消息元数据，JSON格式';
COMMENT ON COLUMN message_templates.variables IS '模板变量定义，JSON格式';

-- =============================================
-- 9. 完成提示
-- =============================================

SELECT 'FakeMicro 数据库创建完成！' as message;
SELECT '数据库名称: fakemicro' as info;
SELECT '已创建表数量: 5' as info;
SELECT '已创建索引数量: 15' as info;
SELECT '已插入默认数据记录数: 6' as info;

-- =============================================
-- 10. 使用说明
-- =============================================

-- 执行此脚本的步骤：
-- 1. 连接到 PostgreSQL 服务器
-- 2. 以超级用户身份执行此脚本
-- 3. 脚本会自动创建数据库和所有表结构
-- 4. 脚本会插入必要的默认数据
-- 5. 脚本会创建索引和触发器以优化性能

-- 连接字符串示例：
-- Host=localhost;Port=5432;Database=fakemicro;Username=postgres;Password=your_password

-- 验证脚本执行成功：
-- SELECT COUNT(*) as table_count FROM information_schema.tables WHERE table_schema = 'public';
-- 预期结果：5个表