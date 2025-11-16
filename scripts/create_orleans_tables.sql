-- Orleans表初始化脚本
-- 用于创建Orleans集群所需的数据库表

-- 创建Orleans数据库（如果不存在）
CREATE DATABASE IF NOT EXISTS orleans;

-- 切换到Orleans数据库
USE orleans;

-- 创建Membership表
CREATE TABLE IF NOT EXISTS OrleansMembershipTable
(
    DeploymentId varchar(150) NOT NULL,
    Address varchar(45) NOT NULL,
    Port int NOT NULL,
    Generation int NOT NULL,
    SiloName varchar(150) NOT NULL,
    HostName varchar(150) NOT NULL,
    Status int NOT NULL,
    ProxyPort int NULL,
    SuspectTimes varchar(8000) NULL,
    StartTime timestamp NOT NULL,
    IAmAliveTime timestamp NOT NULL,
    PRIMARY KEY (DeploymentId, Address, Port, Generation),
    INDEX OrleansMembershipTable_DeploymentId (DeploymentId),
    INDEX OrleansMembershipTable_Status (Status)
);

-- 创建MembershipVersion表
CREATE TABLE IF NOT EXISTS OrleansMembershipVersionTable
(
    DeploymentId varchar(150) NOT NULL,
    Timestamp timestamp NOT NULL,
    Version int NOT NULL,
    PRIMARY KEY (DeploymentId)
);

-- 创建Reminders表
CREATE TABLE IF NOT EXISTS OrleansRemindersTable
(
    ServiceId varchar(150) NOT NULL,
    GrainId varchar(150) NOT NULL,
    ReminderName varchar(150) NOT NULL,
    StartTime timestamp NOT NULL,
    Period bigint NOT NULL,
    GrainHash int NOT NULL,
    Version int NOT NULL,
    PRIMARY KEY (ServiceId, GrainId, ReminderName),
    INDEX OrleansRemindersTable_ServiceId_GrainHash (ServiceId, GrainHash)
);

-- 创建Storage表
CREATE TABLE IF NOT EXISTS OrleansStorage
(
    GrainIdHash int NOT NULL,
    GrainIdN0 bigint NOT NULL,
    GrainIdN1 bigint NOT NULL,
    GrainTypeHash int NOT NULL,
    GrainTypeString varchar(512) NOT NULL,
    GrainIdExtensionString varchar(512) NULL,
    ServiceId varchar(150) NOT NULL,
    GrainStateVersion int NOT NULL,
    PayloadBinary bytea NULL,
    ModifiedOn timestamp NOT NULL,
    PRIMARY KEY (GrainIdHash, GrainIdN0, GrainIdN1, GrainTypeHash),
    INDEX OrleansStorage_ServiceId (ServiceId),
    INDEX OrleansStorage_GrainTypeString (GrainTypeString),
    INDEX OrleansStorage_GrainIdExtensionString (GrainIdExtensionString)
);

-- 创建Orleans数据库用户（如果不存在）
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'orleans_user') THEN
        CREATE USER orleans_user WITH PASSWORD 'orleans_password';
    END IF;
END
$$;

-- 授予权限
GRANT CONNECT ON DATABASE orleans TO orleans_user;
GRANT USAGE ON SCHEMA public TO orleans_user;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO orleans_user;
GRANT SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA public TO orleans_user;

-- 为未来创建的表自动授予权限
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO orleans_user;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, UPDATE ON SEQUENCES TO orleans_user;

-- 创建索引优化查询性能
CREATE INDEX IF NOT EXISTS idx_orleans_membership_status ON OrleansMembershipTable(Status);
CREATE INDEX IF NOT EXISTS idx_orleans_reminders_grainhash ON OrleansRemindersTable(GrainHash);
CREATE INDEX IF NOT EXISTS idx_orleans_storage_modified ON OrleansStorage(ModifiedOn);

-- 插入初始集群配置（可选）
INSERT INTO OrleansMembershipVersionTable (DeploymentId, Timestamp, Version)
VALUES ('FakeMicroCluster', NOW(), 1)
ON CONFLICT (DeploymentId) DO NOTHING;