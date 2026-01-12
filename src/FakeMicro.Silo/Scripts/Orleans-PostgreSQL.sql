-- ========================================
-- Orleans PostgreSQL Database Schema
-- For Production Clustering Support
-- ========================================

-- Drop existing tables if needed (for development only)
-- DROP TABLE IF EXISTS OrleansMembershipTable CASCADE;
-- DROP TABLE IF EXISTS OrleansMembershipVersionTable CASCADE;
-- DROP TABLE IF EXISTS OrleansRemindersTable CASCADE;
-- DROP TABLE IF EXISTS OrleansStatisticsTable CASCADE;
-- DROP TABLE IF EXISTS OrleansStorage CASCADE;

-- ========================================
-- 1. Membership Table (Clustering)
-- ========================================
CREATE TABLE IF NOT EXISTS OrleansMembershipTable
(
    DeploymentId VARCHAR(150) NOT NULL,
    Address VARCHAR(45) NOT NULL,
    Port INT NOT NULL,
    Generation INT NOT NULL,
    SiloName VARCHAR(150) NOT NULL,
    HostName VARCHAR(150) NOT NULL,
    Status INT NOT NULL,
    ProxyPort INT NULL,
    SuspectTimes VARCHAR(8000) NULL,
    StartTime TIMESTAMP NOT NULL,
    IAmAliveTime TIMESTAMP NOT NULL,

    CONSTRAINT PK_OrleansMembershipTable PRIMARY KEY (DeploymentId, Address, Port, Generation)
);

CREATE INDEX IF NOT EXISTS IX_OrleansMembershipTable_Status 
    ON OrleansMembershipTable (DeploymentId, Status);
CREATE INDEX IF NOT EXISTS IX_OrleansMembershipTable_IAmAliveTime 
    ON OrleansMembershipTable (DeploymentId, IAmAliveTime);

-- ========================================
-- 2. Membership Version Table
-- ========================================
CREATE TABLE IF NOT EXISTS OrleansMembershipVersionTable
(
    DeploymentId VARCHAR(150) NOT NULL,
    Timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
    Version INT DEFAULT 0 NOT NULL,

    CONSTRAINT PK_OrleansMembershipVersionTable PRIMARY KEY (DeploymentId)
);

-- Insert initial version
INSERT INTO OrleansMembershipVersionTable (DeploymentId, Version)
VALUES ('FakeMicroCluster', 0)
ON CONFLICT (DeploymentId) DO NOTHING;

-- ========================================
-- 3. Reminders Table
-- ========================================
CREATE TABLE IF NOT EXISTS OrleansRemindersTable
(
    ServiceId VARCHAR(150) NOT NULL,
    GrainId VARCHAR(150) NOT NULL,
    ReminderName VARCHAR(150) NOT NULL,
    StartTime TIMESTAMP NOT NULL,
    Period INT NOT NULL,
    GrainHash INT NOT NULL,
    Version INT NOT NULL,

    CONSTRAINT PK_OrleansRemindersTable PRIMARY KEY (ServiceId, GrainId, ReminderName)
);

CREATE INDEX IF NOT EXISTS IX_OrleansRemindersTable_ServiceId_GrainHash
    ON OrleansRemindersTable (ServiceId, GrainHash);

-- ========================================
-- 4. Grain Storage Table
-- ========================================
CREATE TABLE IF NOT EXISTS OrleansStorage
(
    GrainIdHash INT NOT NULL,
    GrainIdN0 BIGINT NOT NULL,
    GrainIdN1 BIGINT NOT NULL,
    GrainTypeHash INT NOT NULL,
    GrainTypeString VARCHAR(512) NOT NULL,
    GrainIdExtensionString VARCHAR(512) NULL,
    ServiceId VARCHAR(150) NOT NULL,
    PayloadBinary BYTEA NULL,
    PayloadXml VARCHAR(MAX) NULL,
    PayloadJson TEXT NULL,
    ModifiedOn TIMESTAMP NOT NULL,
    Version INT NULL,

    CONSTRAINT PK_OrleansStorage PRIMARY KEY (GrainIdHash, GrainTypeHash)
);

CREATE INDEX IF NOT EXISTS IX_OrleansStorage_GrainIdHash
    ON OrleansStorage (GrainIdHash);
CREATE INDEX IF NOT EXISTS IX_OrleansStorage_GrainTypeHash
    ON OrleansStorage (GrainTypeHash);

-- ========================================
-- 5. Statistics Table (Optional)
-- ========================================
CREATE TABLE IF NOT EXISTS OrleansStatisticsTable
(
    OrleansStatisticsTableId SERIAL PRIMARY KEY,
    DeploymentId VARCHAR(150) NOT NULL,
    Timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
    Id VARCHAR(250) NOT NULL,
    HostName VARCHAR(150) NOT NULL,
    Name VARCHAR(150) NOT NULL,
    IsValueDelta BOOLEAN NOT NULL,
    StatValue VARCHAR(250) NOT NULL,
    Statistic VARCHAR(250) NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_OrleansStatisticsTable_Timestamp
    ON OrleansStatisticsTable (Timestamp);

-- ========================================
-- Grant Permissions
-- ========================================
-- GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO postgres;
-- GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO postgres;

COMMENT ON TABLE OrleansMembershipTable IS 'Orleans cluster membership table for silo discovery';
COMMENT ON TABLE OrleansMembershipVersionTable IS 'Orleans cluster membership version tracking';
COMMENT ON TABLE OrleansRemindersTable IS 'Orleans persistent reminders storage';
COMMENT ON TABLE OrleansStorage IS 'Orleans grain state storage';
COMMENT ON TABLE OrleansStatisticsTable IS 'Orleans runtime statistics';

-- ========================================
-- Verification Query
-- ========================================
-- SELECT 'Membership' as TableName, COUNT(*) as Count FROM OrleansMembershipTable
-- UNION ALL
-- SELECT 'Reminders', COUNT(*) FROM OrleansRemindersTable
-- UNION ALL
-- SELECT 'Storage', COUNT(*) FROM OrleansStorage;
