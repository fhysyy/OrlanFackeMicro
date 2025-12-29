using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace OrleansDatabaseInitialization
{
    public class OrleansPostgresInitializer
    {
        private readonly string _connectionString;
        private readonly ILogger<OrleansPostgresInitializer> _logger;

        public OrleansPostgresInitializer(IConfiguration configuration, ILogger<OrleansPostgresInitializer> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                                ?? "Host=localhost;Database=fakemicro;Username=postgres;Password=123456;Port=5432";
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            _logger.LogInformation("开始初始化 Orleans PostgreSQL 表...");

            await CreateTables(connection);
            await InsertDefaultQueryKeys(connection);
            await InsertDefaultMembershipVersion(connection);

            _logger.LogInformation("Orleans PostgreSQL 初始化完成.");
        }

        private async Task CreateTables(NpgsqlConnection conn)
        {
            var sql = @"
-- MembershipVersion 表
CREATE TABLE IF NOT EXISTS orleansmembershipversion (
    deploymentid VARCHAR(150) NOT NULL,
    version INT NOT NULL,
    CONSTRAINT pk_orleansmembershipversion PRIMARY KEY (deploymentid)
);

-- Membership 表
CREATE TABLE IF NOT EXISTS orleansmembership (
    deploymentid VARCHAR(150) NOT NULL,
    address VARCHAR(45) NOT NULL,
    port INT NOT NULL,
    generation INT NOT NULL,
    silo_name VARCHAR(150) NOT NULL,
    hostname VARCHAR(150) NOT NULL,
    status INT NOT NULL,
    proxy_port INT NULL,
    start_time TIMESTAMP NOT NULL,
    i_am_alive_time TIMESTAMP NOT NULL,
    CONSTRAINT pk_orleansmembership PRIMARY KEY (deploymentid, address, port, generation)
);
CREATE INDEX IF NOT EXISTS ix_orleansmembership_status
    ON orleansmembership (deploymentid, status);

-- Storage 表
CREATE TABLE IF NOT EXISTS orleansstorage (
    serviceid VARCHAR(150) NOT NULL,
    grainid VARCHAR(150) NOT NULL,
    graintype VARCHAR(150) NOT NULL,
    payloadbinary BYTEA,
    payloadjson TEXT,
    modifiedon TIMESTAMP NOT NULL DEFAULT now(),
    version INT NOT NULL,
    CONSTRAINT pk_orleansstorage PRIMARY KEY (serviceid, grainid, graintype)
);
CREATE INDEX IF NOT EXISTS ix_orleansstorage_grainid
    ON orleansstorage (grainid);

-- Reminders 表
CREATE TABLE IF NOT EXISTS orleansreminderstable (
    serviceid VARCHAR(150) NOT NULL,
    grainid VARCHAR(150) NOT NULL,
    remindername VARCHAR(150) NOT NULL,
    starttime TIMESTAMP NOT NULL,
    period BIGINT NOT NULL,
    grainhash INT NOT NULL,
    CONSTRAINT pk_orleansreminderstable PRIMARY KEY (serviceid, grainid, remindername)
);
CREATE INDEX IF NOT EXISTS ix_orleansreminderstable_hash
    ON orleansreminderstable (grainhash);

-- Query 表
CREATE TABLE IF NOT EXISTS orleansquery (
    querykey VARCHAR(64) NOT NULL,
    querytext TEXT NOT NULL,
    CONSTRAINT pk_orleansquery PRIMARY KEY (querykey)
);
";
            await using var cmd = new NpgsqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task InsertDefaultQueryKeys(NpgsqlConnection conn)
        {
            var sql = @"
INSERT INTO orleansquery (querykey, querytext) VALUES
('InsertMembershipVersionKey', 'INSERT INTO orleansmembershipversion (deploymentid, version) VALUES (@DeploymentId, 0)'),
('UpdateMembershipVersionKey', 'UPDATE orleansmembershipversion SET version = version + 1 WHERE deploymentid = @DeploymentId AND version = @Version'),
('InsertMembershipKey', 'INSERT INTO orleansmembership (deploymentid, address, port, generation, silo_name, hostname, status, proxy_port, start_time, i_am_alive_time) VALUES (@DeploymentId, @Address, @Port, @Generation, @SiloName, @HostName, @Status, @ProxyPort, @StartTime, @IAmAliveTime)'),
('UpdateIAmAlivetimeKey', 'UPDATE orleansmembership SET i_am_alive_time = @IAmAliveTime WHERE deploymentid = @DeploymentId AND address = @Address AND port = @Port AND generation = @Generation'),
('UpdateMembershipKey', 'UPDATE orleansmembership SET status = @Status, proxy_port = @ProxyPort, i_am_alive_time = @IAmAliveTime WHERE deploymentid = @DeploymentId AND address = @Address AND port = @Port AND generation = @Generation'),
('DeleteMembershipKey', 'DELETE FROM orleansmembership WHERE deploymentid = @DeploymentId AND address = @Address AND port = @Port AND generation = @Generation'),
('SelectMembershipKey', 'SELECT deploymentid, address, port, generation, silo_name, hostname, status, proxy_port, start_time, i_am_alive_time FROM orleansmembership WHERE deploymentid = @DeploymentId'),
('UpsertGrainStateKey', 'INSERT INTO orleansstorage (serviceid, grainid, graintype, payloadbinary, payloadjson, modifiedon, version) VALUES (@ServiceId, @GrainId, @GrainType, @PayloadBinary, @PayloadJson, @ModifiedOn, @Version) ON CONFLICT (serviceid, grainid, graintype) DO UPDATE SET payloadbinary = EXCLUDED.payloadbinary, payloadjson = EXCLUDED.payloadjson, modifiedon = EXCLUDED.modifiedon, version = EXCLUDED.version'),
('ReadGrainStateKey', 'SELECT payloadbinary, payloadjson, version FROM orleansstorage WHERE serviceid = @ServiceId AND grainid = @GrainId AND graintype = @GrainType'),
('ClearGrainStateKey', 'DELETE FROM orleansstorage WHERE serviceid = @ServiceId AND grainid = @GrainId AND graintype = @GrainType'),
('UpsertReminderKey', 'INSERT INTO orleansreminderstable (serviceid, grainid, remindername, starttime, period, grainhash) VALUES (@ServiceId, @GrainId, @ReminderName, @StartTime, @Period, @GrainHash) ON CONFLICT (serviceid, grainid, remindername) DO UPDATE SET starttime = EXCLUDED.starttime, period = EXCLUDED.period')
ON CONFLICT (querykey) DO NOTHING;
";
            await using var cmd = new NpgsqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task InsertDefaultMembershipVersion(NpgsqlConnection conn)
        {
            var sql = @"
INSERT INTO orleansmembershipversion (deploymentid, version)
VALUES ('default', 0)
ON CONFLICT (deploymentid) DO NOTHING;
";
            await using var cmd = new NpgsqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
