using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using FakeMicro.Utilities.Configuration;
using FakeMicro.DatabaseAccess;

namespace FakeMicro.Silo.Services
{
    /// <summary>
    /// Orleans 数据库初始化服务
    /// </summary>
    public class OrleansDatabaseInitializer
    {
        private readonly ILogger<OrleansDatabaseInitializer> _logger;
        private readonly string _connectionString;

        public OrleansDatabaseInitializer(
            ILogger<OrleansDatabaseInitializer> logger,
            IConfiguration configuration,
            IOptions<SqlSugarConfig.SqlSugarOptions> sqlSugarOptions = null)
        {
            _logger = logger;
            
            // 尝试从SqlSugarOptions获取连接字符串
            _connectionString = sqlSugarOptions?.Value?.ConnectionString;
            
            // 如果SqlSugarOptions为空，从配置直接读取
            if (string.IsNullOrEmpty(_connectionString))
            {
                _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                                   configuration["ConnectionStrings:DefaultConnection"] ??
                                   configuration["SqlSugar:ConnectionString"] ??
                                   configuration.GetSection("SqlSugar")?["ConnectionString"];
            }
            
            // 如果仍然为空，使用默认的PostgreSQL连接字符串
            if (string.IsNullOrEmpty(_connectionString))
            {
                _connectionString = "Host=localhost;Database=fakemicro;Username=postgres;Password=123456;Port=5432";
            }
            
            _logger.LogInformation($"数据库初始化器连接字符串: {_connectionString}");
        }

        /// <summary>
        /// 初始化 Orleans 数据库表结构
        /// </summary>
        public async Task InitializeOrleansTablesAsync()
        {
            if (string.IsNullOrEmpty(_connectionString))
            {
                _logger.LogError("数据库连接字符串为空，无法初始化 Orleans 表结构");
                return;
            }

            try
            {
                _logger.LogInformation("开始初始化 Orleans 数据库表结构...");

                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 创建 Orleans 存储表
                await CreateOrleansStorageTable(connection);
                
                // 创建 Orleans 查询表
                await CreateOrleansQueryTable(connection);
                
                // 创建 Orleans 成员表
                await CreateOrleansMembershipTable(connection);
                
                // 创建 Orleans 成员版本表
                await CreateOrleansMembershipVersionTable(connection);
                
                // 创建 Orleans 提醒服务表
                await CreateOrleansReminderTable(connection);
                
                // 插入默认查询
                await InsertDefaultQueries(connection);

                _logger.LogInformation("Orleans 数据库表结构初始化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化 Orleans 数据库表结构时发生错误");
                throw;
            }
        }

        private async Task CreateOrleansStorageTable(NpgsqlConnection connection)
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS orleansstorage (
                    grainidhash integer NOT NULL,
                    grainidn0 bigint NOT NULL,
                    grainidn1 bigint NOT NULL,
                    graintypehash integer NOT NULL,
                    graintypestring character varying(512) NOT NULL,
                    grainidextensionstring character varying(512),
                    serviceid character varying(150) NOT NULL,
                    payloadbinary bytea,
                    payloadjson text,
                    payloadxml text,
                    etag integer NOT NULL DEFAULT 0,
                    version integer NOT NULL DEFAULT 0,
                    modifiedon timestamp without time zone NOT NULL DEFAULT now(),
                    createdon timestamp without time zone NOT NULL DEFAULT now(),
                    PRIMARY KEY (grainidhash, grainidn0, grainidn1, graintypehash)
                );

                CREATE INDEX IF NOT EXISTS idx_orleansstorage_serviceid ON orleansstorage(serviceid);
                CREATE INDEX IF NOT EXISTS idx_orleansstorage_graintypehash ON orleansstorage(graintypehash);
                CREATE INDEX IF NOT EXISTS idx_orleansstorage_grainidhash ON orleansstorage(grainidhash);
                CREATE INDEX IF NOT EXISTS idx_orleansstorage_modifiedon ON orleansstorage(modifiedon);";

            await ExecuteSqlCommand(connection, sql, "OrleansStorage 表");
        }

        private async Task CreateOrleansQueryTable(NpgsqlConnection connection)
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS orleansquery (
                    querykey character varying(512) NOT NULL,
                    querytext text NOT NULL,
                    createdon timestamp without time zone NOT NULL DEFAULT now(),
                    modifiedon timestamp without time zone NOT NULL DEFAULT now(),
                    PRIMARY KEY (querykey)
                );";

            await ExecuteSqlCommand(connection, sql, "OrleansQuery 表");
        }

        private async Task CreateOrleansMembershipTable(NpgsqlConnection connection)
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS orleansmembershiptable (
                    deploymentid character varying(150) NOT NULL,
                    address character varying(45) NOT NULL,
                    port integer NOT NULL,
                    generation integer NOT NULL,
                    siloname character varying(150) NOT NULL,
                    hostname character varying(150) NOT NULL,
                    status integer NOT NULL,
                    proxyport integer,
                    suspecttimes character varying(8000),
                    starttime timestamp without time zone NOT NULL,
                    iamalivetime timestamp without time zone NOT NULL,
                    PRIMARY KEY (deploymentid, address, port, generation)
                );

                CREATE INDEX IF NOT EXISTS idx_orleansmembership_deployment ON orleansmembershiptable(deploymentid);
                CREATE INDEX IF NOT EXISTS idx_orleansmembership_iamalive ON orleansmembershiptable(iamalivetime);";

            await ExecuteSqlCommand(connection, sql, "OrleansMembership 表");
        }

        private async Task CreateOrleansMembershipVersionTable(NpgsqlConnection connection)
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS orleansmembershipversiontable (
                    deploymentid character varying(150) NOT NULL,
                    timestamp timestamp without time zone NOT NULL DEFAULT now(),
                    version integer NOT NULL,
                    PRIMARY KEY (deploymentid)
                );";

            await ExecuteSqlCommand(connection, sql, "OrleansMembershipVersion 表");
        }

        private async Task CreateOrleansReminderTable(NpgsqlConnection connection)
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS orleansreminderservice (
                    serviceid character varying(150) NOT NULL,
                    grainid character varying(150) NOT NULL,
                    remintername character varying(150) NOT NULL,
                    starttime timestamp without time zone NOT NULL,
                    period bigint NOT NULL,
                    PRIMARY KEY (serviceid, grainid, remintername)
                );";

            await ExecuteSqlCommand(connection, sql, "OrleansReminder 表");
        }

        private async Task InsertDefaultQueries(NpgsqlConnection connection)
        {
            const string sql = @"
                INSERT INTO orleansquery (querykey, querytext) VALUES 
                ('UpsertGrainStateKey', 'INSERT INTO orleansstorage (grainidhash, grainidn0, grainidn1, graintypehash, graintypestring, grainidextensionstring, serviceid, payloadbinary, payloadjson, payloadxml, etag, version, modifiedon, createdon) VALUES (@grainidhash, @grainidn0, @grainidn1, @graintypehash, @graintypestring, @grainidextensionstring, @serviceid, @payloadbinary, @payloadjson, @payloadxml, @etag, @version, @modifiedon, @createdon) ON CONFLICT (grainidhash, grainidn0, grainidn1, graintypehash) DO UPDATE SET payloadbinary = EXCLUDED.payloadbinary, payloadjson = EXCLUDED.payloadjson, payloadxml = EXCLUDED.payloadxml, etag = EXCLUDED.etag, version = EXCLUDED.version, modifiedon = EXCLUDED.modifiedon'),
                ('ReadGrainStateKey', 'SELECT payloadbinary, payloadjson, payloadxml, etag, version FROM orleansstorage WHERE grainidhash = @grainidhash AND grainidn0 = @grainidn0 AND grainidn1 = @grainidn1 AND graintypehash = @graintypehash AND serviceid = @serviceid'),
                ('ClearGrainStateKey', 'DELETE FROM orleansstorage WHERE grainidhash = @grainidhash AND grainidn0 = @grainidn0 AND grainidn1 = @grainidn1 AND graintypehash = @graintypehash AND serviceid = @serviceid'),
                ('WriteToStorageKey', 'INSERT INTO orleansstorage (grainidhash, grainidn0, grainidn1, graintypehash, graintypestring, grainidextensionstring, serviceid, payloadbinary, payloadjson, payloadxml, etag, version, modifiedon, createdon) VALUES (@grainidhash, @grainidn0, @grainidn1, @graintypehash, @graintypestring, @grainidextensionstring, @serviceid, @payloadbinary, @payloadjson, @payloadxml, @etag, @version, @modifiedon, @createdon) ON CONFLICT (grainidhash, grainidn0, grainidn1, graintypehash) DO UPDATE SET payloadbinary = EXCLUDED.payloadbinary, payloadjson = EXCLUDED.payloadjson, payloadxml = EXCLUDED.payloadxml, etag = EXCLUDED.etag, version = EXCLUDED.version, modifiedon = EXCLUDED.modifiedon')
                ON CONFLICT (querykey) DO UPDATE SET querytext = EXCLUDED.querytext;";

            await ExecuteSqlCommand(connection, sql, "默认查询");
        }

        private async Task ExecuteSqlCommand(NpgsqlConnection connection, string sql, string tableName)
        {
            try
            {
                await using var command = new NpgsqlCommand(sql, connection);
                await command.ExecuteNonQueryAsync();
                _logger.LogInformation($"成功创建/更新 {tableName}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"创建/更新 {tableName} 时发生警告: {ex.Message}");
                // 对于表创建，如果表已存在，这可能是正常情况
            }
        }

        /// <summary>
        /// 检查 Orleans 表是否存在
        /// </summary>
        public async Task<bool> CheckOrleansTablesExistAsync()
        {
            if (string.IsNullOrEmpty(_connectionString))
            {
                return false;
            }

            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql = @"
                    SELECT COUNT(*) 
                    FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    AND table_name IN ('orleansstorage', 'orleansquery', 'orleansmembershiptable', 'orleansmembershipversiontable', 'orleansreminderservice')";

                await using var command = new NpgsqlCommand(sql, connection);
                var result = await command.ExecuteScalarAsync();
                var count = Convert.ToInt32(result);

                return count >= 5; // 所有5个表都应该存在
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查 Orleans 表是否存在时发生错误");
                return false;
            }
        }
    }
}