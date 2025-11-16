-- Orleans 数据库初始化脚本 for FakeMicro
-- 在fakemicro数据库中创建Orleans所需的表结构

-- 创建Orleans集群表结构
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
    PRIMARY KEY (DeploymentId, Address, Port, Generation)
);

-- 创建索引
CREATE INDEX IF NOT EXISTS OrleansMembershipTable_DeploymentId ON OrleansMembershipTable(DeploymentId);
CREATE INDEX IF NOT EXISTS OrleansMembershipTable_Status ON OrleansMembershipTable(Status);

-- 创建MembershipVersion表
CREATE TABLE IF NOT EXISTS OrleansMembershipVersionTable
(
    DeploymentId varchar(150) NOT NULL,
    "Timestamp" timestamp NOT NULL,
    "Version" int NOT NULL,
    PRIMARY KEY (DeploymentId)
);

-- 创建Reminders表
CREATE TABLE IF NOT EXISTS OrleansRemindersTable
(
    ServiceId varchar(150) NOT NULL,
    GrainId varchar(150) NOT NULL,
    ReminderName varchar(150) NOT NULL,
    StartTime timestamp NOT NULL,
    "Period" bigint NOT NULL,
    GrainHash int NOT NULL,
    "Version" int NOT NULL,
    PRIMARY KEY (ServiceId, GrainId, ReminderName)
);

-- 创建索引
CREATE INDEX IF NOT EXISTS OrleansRemindersTable_ServiceId_GrainHash ON OrleansRemindersTable(ServiceId, GrainHash);

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
    PRIMARY KEY (GrainIdHash, GrainIdN0, GrainIdN1, GrainTypeHash)
);

-- 创建索引
CREATE INDEX IF NOT EXISTS OrleansStorage_ServiceId ON OrleansStorage(ServiceId);
CREATE INDEX IF NOT EXISTS OrleansStorage_GrainTypeString ON OrleansStorage(GrainTypeString);
CREATE INDEX IF NOT EXISTS OrleansStorage_GrainIdExtensionString ON OrleansStorage(GrainIdExtensionString);

-- 创建Orleans查询表（解决42P01错误）
CREATE TABLE IF NOT EXISTS orleansquery
(
    QueryKey VARCHAR(255) NOT NULL PRIMARY KEY,
    QueryText TEXT,
    ResultCount INT,
    ExecutionTime BIGINT,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 插入Orleans集群必需的查询语句
INSERT INTO orleansquery (QueryKey, QueryText) VALUES
('GatewaysQueryKey', 'SELECT Address, ProxyPort FROM OrleansMembershipTable WHERE DeploymentId = @DeploymentId AND Status = @Status;'),
('MembershipReadRowKey', 'SELECT * FROM OrleansMembershipTable WHERE DeploymentId = @DeploymentId AND Address = @Address AND Port = @Port AND Generation = @Generation;'),
('MembershipReadAllKey', 'SELECT * FROM OrleansMembershipTable WHERE DeploymentId = @DeploymentId;'),
('InsertMembershipVersionKey', 'INSERT INTO OrleansMembershipVersionTable (DeploymentId, "Timestamp", "Version") VALUES (@DeploymentId, @Timestamp, @Version) ON CONFLICT (DeploymentId) DO UPDATE SET "Timestamp" = @Timestamp, "Version" = @Version;'),
('UpdateIAmAlivetimeKey', 'UPDATE OrleansMembershipTable SET IAmAliveTime = @IAmAliveTime WHERE DeploymentId = @DeploymentId AND Address = @Address AND Port = @Port AND Generation = @Generation;'),
('InsertMembershipKey', 'INSERT INTO OrleansMembershipTable (DeploymentId, Address, Port, Generation, SiloName, HostName, Status, ProxyPort, SuspectTimes, StartTime, IAmAliveTime) VALUES (@DeploymentId, @Address, @Port, @Generation, @SiloName, @HostName, @Status, @ProxyPort, @SuspectTimes, @StartTime, @IAmAliveTime);'),
('UpdateMembershipKey', 'UPDATE OrleansMembershipTable SET Status = @Status, SuspectTimes = @SuspectTimes, IAmAliveTime = @IAmAliveTime WHERE DeploymentId = @DeploymentId AND Address = @Address AND Port = @Port AND Generation = @Generation;'),
('DeleteMembershipTableEntriesKey', 'DELETE FROM OrleansMembershipTable WHERE DeploymentId = @DeploymentId;'),
('CleanupDefunctSiloEntriesKey', 'DELETE FROM OrleansMembershipTable WHERE DeploymentId = @DeploymentId AND Status = @Status AND IAmAliveTime < @IAmAliveTime;'),

-- 额外的集群管理查询
('MembershipReadVersionKey', 'SELECT "Version", "Timestamp" FROM OrleansMembershipVersionTable WHERE DeploymentId = @DeploymentId;'),
('InsertReminderRowKey', 'INSERT INTO OrleansRemindersTable (ServiceId, GrainId, ReminderName, StartTime, "Period", GrainHash, "Version") VALUES (@ServiceId, @GrainId, @ReminderName, @StartTime, @Period, @GrainHash, @Version);'),
('ReadReminderRowsKey', 'SELECT * FROM OrleansRemindersTable WHERE ServiceId = @ServiceId AND GrainId = @GrainId;'),
('ReadReminderRowKey', 'SELECT * FROM OrleansRemindersTable WHERE ServiceId = @ServiceId AND GrainId = @GrainId AND ReminderName = @ReminderName;'),
('UpdateReminderRowKey', 'UPDATE OrleansRemindersTable SET StartTime = @StartTime, "Period" = @Period, "Version" = @Version WHERE ServiceId = @ServiceId AND GrainId = @GrainId AND ReminderName = @ReminderName;'),
('DeleteReminderRowKey', 'DELETE FROM OrleansRemindersTable WHERE ServiceId = @ServiceId AND GrainId = @GrainId AND ReminderName = @ReminderName;'),
('ReadRangeRows1Key', 'SELECT * FROM OrleansRemindersTable WHERE ServiceId = @ServiceId AND GrainHash > @BeginHash AND GrainHash <= @EndHash;'),
('ReadRangeRows2Key', 'SELECT * FROM OrleansRemindersTable WHERE ServiceId = @ServiceId AND ((GrainHash > @BeginHash AND GrainHash <= @EndHash) OR GrainHash <= @EndHash AND GrainHash > @BeginHash);'),

-- 存储相关的查询
('WriteToStorageKey', 'INSERT INTO OrleansStorage (GrainIdHash, GrainIdN0, GrainIdN1, GrainTypeHash, GrainTypeString, GrainIdExtensionString, ServiceId, GrainStateVersion, PayloadBinary, ModifiedOn) VALUES (@GrainIdHash, @GrainIdN0, @GrainIdN1, @GrainTypeHash, @GrainTypeString, @GrainIdExtensionString, @ServiceId, @GrainStateVersion, @PayloadBinary, @ModifiedOn) ON CONFLICT (GrainIdHash, GrainIdN0, GrainIdN1, GrainTypeHash) DO UPDATE SET PayloadBinary = @PayloadBinary, GrainStateVersion = @GrainStateVersion, ModifiedOn = @ModifiedOn;'),
('ReadFromStorageKey', 'SELECT GrainIdHash, GrainIdN0, GrainIdN1, GrainTypeHash, GrainTypeString, GrainIdExtensionString, ServiceId, GrainStateVersion, PayloadBinary, ModifiedOn FROM OrleansStorage WHERE GrainIdHash = @GrainIdHash AND GrainIdN0 = @GrainIdN0 AND GrainIdN1 = @GrainIdN1 AND GrainTypeHash = @GrainTypeHash;'),
('ClearStorageKey', 'DELETE FROM OrleansStorage WHERE GrainIdHash = @GrainIdHash AND GrainIdN0 = @GrainIdN0 AND GrainIdN1 = @GrainIdN1 AND GrainTypeHash = @GrainTypeHash;'),
('ReadMultiFromStorageKey', 'SELECT GrainIdHash, GrainIdN0, GrainIdN1, GrainTypeHash, GrainTypeString, GrainIdExtensionString, ServiceId, GrainStateVersion, PayloadBinary, ModifiedOn FROM OrleansStorage WHERE GrainIdHash = @GrainIdHash AND GrainIdN0 = @GrainIdN0 AND GrainIdN1 = @GrainIdN1 AND GrainTypeHash = @GrainTypeHash;')

ON CONFLICT (QueryKey) DO UPDATE SET QueryText = EXCLUDED.QueryText;

-- 创建额外索引优化查询性能
CREATE INDEX IF NOT EXISTS idx_orleans_membership_status ON OrleansMembershipTable(Status);
CREATE INDEX IF NOT EXISTS idx_orleans_reminders_grainhash ON OrleansRemindersTable(GrainHash);
CREATE INDEX IF NOT EXISTS idx_orleans_storage_modified ON OrleansStorage(ModifiedOn);

-- 插入初始集群配置
INSERT INTO OrleansMembershipVersionTable (DeploymentId, "Timestamp", "Version")
VALUES ('FakeMicroCluster', NOW(), 1)
ON CONFLICT (DeploymentId) DO NOTHING;

-- 查询创建结果
SELECT 'FakeMicro Orleans表创建完成！' as message;