using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FakeMicro.Silo.Services
{
    /// <summary>
    /// Orleans PostgreSQL 数据库初始化器（官方 Provider 版本）
    /// 仅负责创建 Orleans 必需表结构，不存 SQL、不做旧模型兼容
    /// </summary>
    public sealed class OrleansDatabaseInitializer
    {
        private readonly ILogger<OrleansDatabaseInitializer> _logger;
        private readonly string _connectionString;

        public OrleansDatabaseInitializer(
            ILogger<OrleansDatabaseInitializer> logger,
            IConfiguration configuration)
        {
            _logger = logger;

            _connectionString =
                configuration.GetConnectionString("DefaultConnection") ??
                configuration["ConnectionStrings:DefaultConnection"] ??
                configuration["PostgreSQL:ConnectionString"] ??
                throw new InvalidOperationException("未配置 PostgreSQL 连接字符串");

            _logger.LogInformation("Orleans 数据库初始化器使用连接字符串：{ConnectionString}", _connectionString);
        }

        /// <summary>
        /// 初始化 Orleans 所需的 PostgreSQL 表结构（幂等）
        /// </summary>
        public async Task InitializeAsync()
        {
            _logger.LogInformation("开始初始化 Orleans PostgreSQL 表结构...");

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await CreateMembershipVersionTableAsync(connection);
            await CreateMembershipTableAsync(connection);
            await CreateStorageTableAsync(connection);
            await CreateReminderTableAsync(connection);

            _logger.LogInformation("✅ Orleans PostgreSQL 表结构初始化完成");
        }

        #region Table Creation

        private async Task CreateMembershipVersionTableAsync(NpgsqlConnection connection)
        {
            const string sql = @"
CREATE TABLE IF NOT EXISTS orleansmembershipversiontable
(
    deploymentid varchar(150) NOT NULL,
    version      integer      NOT NULL,
    CONSTRAINT pk_orleansmembershipversiontable
        PRIMARY KEY (deploymentid)
);";

            await ExecuteAsync(connection, sql, "orleansmembershipversiontable");
        }

        private async Task CreateMembershipTableAsync(NpgsqlConnection connection)
        {
            const string sql = @"
CREATE TABLE IF NOT EXISTS orleansmembershiptable
(
    deploymentid     varchar(150) NOT NULL,
    address          varchar(45)  NOT NULL,
    port             integer      NOT NULL,
    generation       integer      NOT NULL,
    silo_name        varchar(150) NOT NULL,
    host_name        varchar(150) NOT NULL,
    status           integer      NOT NULL,
    proxy_port       integer,
    suspecting_silos varchar(8000),
    start_time       timestamp    NOT NULL,
    iamalivetime     timestamp    NOT NULL,

    CONSTRAINT pk_orleansmembershiptable
        PRIMARY KEY (deploymentid, address, port, generation)
);

CREATE INDEX IF NOT EXISTS ix_orleansmembershiptable_status
    ON orleansmembershiptable (deploymentid, status);";

            await ExecuteAsync(connection, sql, "orleansmembershiptable");
        }

        private async Task CreateStorageTableAsync(NpgsqlConnection connection)
        {
            const string sql = @"
CREATE TABLE IF NOT EXISTS orleansstorage
(
    serviceid     varchar(150) NOT NULL,
    grainid       varchar(150) NOT NULL,
    graintype     varchar(150) NOT NULL,
    payloadbinary bytea,
    payloadjson   text,
    modifiedon    timestamp    NOT NULL,
    version       integer      NOT NULL,

    CONSTRAINT pk_orleansstorage
        PRIMARY KEY (serviceid, grainid, graintype)
);

CREATE INDEX IF NOT EXISTS ix_orleansstorage_grainid
    ON orleansstorage (grainid);";

            await ExecuteAsync(connection, sql, "orleansstorage");
        }

        private async Task CreateReminderTableAsync(NpgsqlConnection connection)
        {
            const string sql = @"
CREATE TABLE IF NOT EXISTS orleansreminderstable
(
    serviceid    varchar(150) NOT NULL,
    grainid      varchar(150) NOT NULL,
    remindername varchar(150) NOT NULL,
    starttime    timestamp    NOT NULL,
    period       bigint       NOT NULL,
    grainhash    integer      NOT NULL,

    CONSTRAINT pk_orleansreminderstable
        PRIMARY KEY (serviceid, grainid, remindername)
);

CREATE INDEX IF NOT EXISTS ix_orleansreminderstable_hash
    ON orleansreminderstable (grainhash);";

            await ExecuteAsync(connection, sql, "orleansreminderstable");
        }

        #endregion

        #region Helpers

        private async Task ExecuteAsync(NpgsqlConnection connection, string sql, string tableName)
        {
            try
            {
                await using var cmd = new NpgsqlCommand(sql, connection);
                await cmd.ExecuteNonQueryAsync();
                _logger.LogInformation("✔ 表已就绪：{Table}", tableName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 创建表失败：{Table}", tableName);
                throw;
            }
        }

        #endregion
    }
}
