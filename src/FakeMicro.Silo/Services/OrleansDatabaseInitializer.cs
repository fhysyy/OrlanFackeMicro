using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Threading.Tasks;
using FakeMicro.Utilities.Configuration;

namespace FakeMicro.Silo.Services
{
    /// <summary>
    /// Orleans数据库初始化器
    /// 负责创建Orleans所需的数据库和表结构
    /// </summary>
    public class OrleansDatabaseInitializer : IHostedService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<OrleansDatabaseInitializer> _logger;

        public OrleansDatabaseInitializer(IConfiguration configuration, ILogger<OrleansDatabaseInitializer> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// 启动服务
        /// </summary>
        public async Task StartAsync(System.Threading.CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting Orleans database initialization...");

                // 获取应用配置
                var appSettings = _configuration.Get<AppSettings>() ?? new AppSettings();
                
                // 使用数据库配置获取连接字符串
                string connectionString = appSettings.Database.GetConnectionString();

                // 确保数据库存在
                await EnsureDatabaseExistsAsync(connectionString);

                _logger.LogInformation("Orleans database initialization completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Orleans database.");
                // 不抛出异常，允许服务继续启动
            }
        }

        /// <summary>
        /// 停止服务
        /// </summary>
        public Task StopAsync(System.Threading.CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 确保数据库存在
        /// </summary>
        private async Task EnsureDatabaseExistsAsync(string connectionString)
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            var databaseName = builder.Database;

            // 移除数据库名称，连接到默认的postgres数据库
            builder.Database = "postgres";

            using var connection = new NpgsqlConnection(builder.ConnectionString);
            await connection.OpenAsync();

            // 检查数据库是否存在
            using var command = new NpgsqlCommand($"SELECT 1 FROM pg_database WHERE datname = @databaseName", connection);
            command.Parameters.AddWithValue("@databaseName", databaseName);

            var result = await command.ExecuteScalarAsync();
            if (result == null)
            {
                // 创建数据库
                _logger.LogInformation($"Creating database '{databaseName}'...");
                using var createCommand = new NpgsqlCommand($"CREATE DATABASE {databaseName}", connection);
                await createCommand.ExecuteNonQueryAsync();
                _logger.LogInformation($"Database '{databaseName}' created successfully.");
            }
            else
            {
                _logger.LogInformation($"Database '{databaseName}' already exists.");
            }
        }
    }
}