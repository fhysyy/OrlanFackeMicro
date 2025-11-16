-- FakeMicro 完整数据库初始化脚本
-- 整合Orleans + FakeMicro应用数据库结构

-- =============================================
-- 1. 数据库创建
-- =============================================

-- 创建主数据库
CREATE DATABASE fakemicro;
CREATE DATABASE fakemicro_cap;
CREATE DATABASE fakemicro_hangfire;

-- 切换到主数据库
\c fakemicro;

-- =============================================
-- 2. Orleans表结构
-- =============================================

-- Orleans成员资格表
CREATE TABLE OrleansMembershipTable (
    DeploymentId varchar(150) NOT NULL,
    Address varchar(45) NOT NULL,
    Port int NOT NULL,
    Generation int NOT NULL,
    SiloName varchar(150),
    Hostname varchar(150),
    Status int NOT NULL,
    ProxyPort int,
    SiloWeight int NOT NULL,
    UpdateZone int,
    SubZone int,
    StartTime timestamp NOT NULL,
    IAmAliveTime timestamp NOT NULL,
    PRIMARY KEY (DeploymentId, Address, Port, Generation)
);

-- Orleans成员资格版本表
CREATE TABLE OrleansMembershipVersionTable (
    DeploymentId varchar(150) NOT NULL,
    timestamp timestamp NOT NULL,
    version int NOT NULL,
    PRIMARY KEY (DeploymentId)
);

-- Orleans提醒表
CREATE TABLE OrleansRemindersTable (
    ServiceId varchar(150) NOT NULL,
    GrainId varchar(150) NOT NULL,
    ReminderName varchar(150) NOT NULL,
    StartTime timestamp NOT NULL,
    period bigint NOT NULL,
    GrainHash int NOT NULL,
    version int NOT NULL,
    PRIMARY KEY (ServiceId, GrainId, ReminderName)
);

-- Orleans存储表
CREATE TABLE OrleansStorage (
    GrainId varchar(150) NOT NULL,
    GrainType varchar(150) NOT NULL,
    GrainStateBinary bytea,
    GrainStateJson text,
    Version bigint NOT NULL,
    ModifiedOn timestamp NOT NULL,
    ETag varchar(50),
    PRIMARY KEY (GrainId, GrainType)
);

-- Orleans查询表
CREATE TABLE orleansquery (
    QueryKey varchar(150) NOT NULL,
    QueryText text NOT NULL,
    PRIMARY KEY (QueryKey)
);

-- Orleans索引
CREATE INDEX idx_OrleansMembershipTable_DeploymentId ON OrleansMembershipTable(DeploymentId);
CREATE INDEX idx_OrleansMembershipVersionTable_DeploymentId ON OrleansMembershipVersionTable(DeploymentId);
CREATE INDEX idx_OrleansRemindersTable_ServiceId_GrainHash ON OrleansRemindersTable(ServiceId, GrainHash);
CREATE INDEX idx_OrleansRemindersTable_GrainHash ON OrleansRemindersTable(GrainHash);

-- 插入Orleans查询语句
INSERT INTO orleansquery (QueryKey, QueryText) VALUES
('INSERT_ORLEANS_MEMBERSHIP_VERSION_ENTRY', 'INSERT INTO OrleansMembershipVersionTable (DeploymentId, timestamp, version) VALUES (@DeploymentId, @Timestamp, @Version)'),
('UPDATE_ORLEANS_MEMBERSHIP_VERSION_ENTRY', 'UPDATE OrleansMembershipVersionTable SET timestamp = @Timestamp, version = @Version WHERE DeploymentId = @DeploymentId'),
('UPDATE_ORLEANS_I_AM_ALIVE_TIME', 'UPDATE OrleansMembershipTable SET IAmAliveTime = @IAmAliveTime WHERE DeploymentId = @DeploymentId AND Address = @Address AND Port = @Port AND Generation = @Generation'),
('INSERT_ORLEANS_MEMBERSHIP_ENTRY', 'INSERT INTO OrleansMembershipTable (DeploymentId, Address, Port, Generation, SiloName, Hostname, Status, ProxyPort, SiloWeight, UpdateZone, SubZone, StartTime, IAmAliveTime) VALUES (@DeploymentId, @Address, @Port, @Generation, @SiloName, @Hostname, @Status, @ProxyPort, @SiloWeight, @UpdateZone, @SubZone, @StartTime, @IAmAliveTime)'),
('UPDATE_ORLEANS_MEMBERSHIP_ENTRY', 'UPDATE OrleansMembershipTable SET SiloName = @SiloName, Hostname = @Hostname, Status = @Status, ProxyPort = @ProxyPort, SiloWeight = @SiloWeight, UpdateZone = @UpdateZone, SubZone = @SubZone, IAmAliveTime = @IAmAliveTime WHERE DeploymentId = @DeploymentId AND Address = @Address AND Port = @Port AND Generation = @Generation'),
('DELETE_ORLEANS_MEMBERSHIP_ENTRY', 'DELETE FROM OrleansMembershipTable WHERE DeploymentId = @DeploymentId AND Address = @Address AND Port = @Port AND Generation = @Generation'),
('GET_ORLEANS_MEMBERSHIP_TABLE', 'SELECT DeploymentId, Address, Port, Generation, SiloName, Hostname, Status, ProxyPort, SiloWeight, UpdateZone, SubZone, StartTime, IAmAliveTime FROM OrleansMembershipTable WHERE DeploymentId = @DeploymentId'),
('GET_ORLEANS_MEMBERSHIP_VERSION', 'SELECT DeploymentId, timestamp, version FROM OrleansMembershipVersionTable WHERE DeploymentId = @DeploymentId'),
('GET_ORLEANS_I_AM_ALIVE_TIME', 'SELECT DeploymentId, Address, Port, Generation, IAmAliveTime FROM OrleansMembershipTable WHERE DeploymentId = @DeploymentId AND Address = @Address AND Port = @Port AND Generation = @Generation'),
('INSERT_ORLEANS_REMINDER', 'INSERT INTO OrleansRemindersTable (ServiceId, GrainId, ReminderName, StartTime, period, GrainHash, version) VALUES (@ServiceId, @GrainId, @ReminderName, @StartTime, @Period, @GrainHash, @Version)'),
('UPDATE_ORLEANS_REMINDER', 'UPDATE OrleansRemindersTable SET StartTime = @StartTime, period = @Period, GrainHash = @GrainHash, version = @Version WHERE ServiceId = @ServiceId AND GrainId = @GrainId AND ReminderName = @ReminderName'),
('DELETE_ORLEANS_REMINDER', 'DELETE FROM OrleansRemindersTable WHERE ServiceId = @ServiceId AND GrainId = @GrainId AND ReminderName = @ReminderName'),
('GET_ORLEANS_REMINDER_ROWS', 'SELECT ServiceId, GrainId, ReminderName, StartTime, period, GrainHash, version FROM OrleansRemindersTable WHERE ServiceId = @ServiceId AND GrainHash = @GrainHash'),
('GET_ORLEANS_REMINDER_ROW', 'SELECT ServiceId, GrainId, ReminderName, StartTime, period, GrainHash, version FROM OrleansRemindersTable WHERE ServiceId = @ServiceId AND GrainId = @GrainId AND ReminderName = @ReminderName');

-- 插入初始集群配置
INSERT INTO OrleansMembershipVersionTable (DeploymentId, timestamp, version) VALUES ('FakeMicroCluster', NOW(), 1);

-- =============================================
-- 3. 枚举类型定义
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
-- 4. FakeMicro核心表结构
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
-- 5. 索引创建
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
-- 6. 默认数据插入
-- =============================================

-- 插入默认角色
INSERT INTO roles (name, code, description, is_system_role, created_at, updated_at) VALUES
    ('管理员', 'ADMIN', '系统管理员角色', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('用户', 'USER', '普通用户角色', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('系统管理员', 'SYSTEM_ADMIN', '系统超级管理员角色', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT (code) DO NOTHING;

-- 插入默认管理员用户（密码为：admin123，需要在实际使用时重新设置）
INSERT INTO users (id, username, display_name, email, password_hash, password_salt, role, status, email_verified, created_at, updated_at) 
VALUES 
    (1, 'admin', '系统管理员', 'admin@fakemicro.com', 
     'AQAAAAIAAYagAAAAEJJqJ8v8mZ4VZvK5JgJ8v8mZ4VZvK5JgJ8v8mZ4VZvK5JgJ8v8mZ4VZvK5Jg==', 
     'AQAAAAIAAYagAAAAEJJqJ8v8mZ4VZvK5JgJ8v8mZ4VZvK5JgJ8v8mZ4VZvK5JgJ8v8mZ4VZvK5Jg==', 
     'SystemAdmin', 'Active', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT (username) DO NOTHING;

-- 关联管理员用户和角色
INSERT INTO user_roles (user_id, role_id, created_at, updated_at) 
SELECT 1, id, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP 
FROM roles 
WHERE code IN ('ADMIN', 'SYSTEM_ADMIN')
ON CONFLICT (user_id, role_id) DO NOTHING;

-- =============================================
-- 7. 权限和用户设置
-- =============================================

-- 创建orleans用户（如果需要）
CREATE USER orleans_user WITH PASSWORD 'orleans_password';
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO orleans_user;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO orleans_user;

-- 确保所有表的权限正确
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO postgres;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO postgres;

-- 初始化完成提示
DO $$
BEGIN
    RAISE NOTICE 'FakeMicro数据库初始化完成！';
    RAISE NOTICE 'Orleans表结构已创建完成';
    RAISE NOTICE '默认管理员账户: admin / admin123';
    RAISE NOTICE '请在生产环境中修改默认密码';
END $$;