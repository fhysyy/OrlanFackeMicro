using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FakeMicro.Entities;
using FakeMicro.DatabaseAccess;
using FakeMicro.Utilities;
using Microsoft.Extensions.DependencyInjection;
using FakeMicro.DatabaseAccess.Entities;
using UserRole = FakeMicro.Entities.UserRole;
using Message = FakeMicro.Entities.Message;
using FileInfo = FakeMicro.Entities.FileInfo;

namespace FakeMicro.DatabaseAccess.Services
{
    /// <summary>
    /// 数据库初始化托管服务
    /// 遵循Orleans框架最佳实践，使用SqlSugar ORM进行数据库初始化
    /// </summary>
    public class DatabaseInitializerHostedService : IHostedService
    {
        private readonly ILogger<DatabaseInitializerHostedService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly SqlSugarConfig.SqlSugarOptions _sqlSugarOptions;
        private readonly DatabaseInitializerOptions _initializerOptions;

        public DatabaseInitializerHostedService(
            ILogger<DatabaseInitializerHostedService> logger,
            IServiceProvider serviceProvider,
            IOptions<SqlSugarConfig.SqlSugarOptions> sqlSugarOptions,
            IOptions<DatabaseInitializerOptions> initializerOptions)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _sqlSugarOptions = sqlSugarOptions?.Value ?? throw new ArgumentNullException(nameof(sqlSugarOptions));
            _initializerOptions = initializerOptions?.Value ?? new DatabaseInitializerOptions();
        }

        /// <summary>
        /// 启动时执行数据库初始化
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始数据库初始化服务...");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var sqlSugarClient = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

                // 检查数据库连接
                await TestDatabaseConnection(sqlSugarClient);

                // 创建数据库表结构
                await CreateDatabaseTablesAsync(sqlSugarClient);

                // 初始化种子数据
                await SeedDataAsync(sqlSugarClient);

                // 创建数据库索引
                await CreateIndexesAsync(sqlSugarClient);

                _logger.LogInformation("数据库初始化服务完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据库初始化失败");
                throw;
            }
        }

        /// <summary>
        /// 停止服务（无需特殊处理）
        /// </summary>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("数据库初始化服务停止");
            return Task.CompletedTask;
        }

        /// <summary>
        /// 测试数据库连接
        /// </summary>
        private async Task TestDatabaseConnection(ISqlSugarClient db)
        {
            _logger.LogInformation("测试数据库连接...");

            try
            {
                var isConnected = db.Ado.IsValidConnection();
                if (!isConnected)
                {
                    throw new InvalidOperationException("数据库连接无效");
                }

                // 执行简单查询测试连接
                await db.Ado.ExecuteCommandAsync("SELECT 1");
                
                _logger.LogInformation("数据库连接测试成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据库连接测试失败");
                throw;
            }
        }

        /// <summary>
        /// 创建数据库表结构
        /// </summary>
        private async Task CreateDatabaseTablesAsync(ISqlSugarClient db)
        {
            _logger.LogInformation("开始创建数据库表结构...");

            try
            {
                db.CodeFirst.InitTables<User>();
                db.CodeFirst.InitTables<Role>();
                db.CodeFirst.InitTables<DictionaryType>();
                db.CodeFirst.InitTables<DictionaryItem>();
                db.CodeFirst.InitTables<AuditLog>();
                db.CodeFirst.InitTables<Tenant>();
                db.CodeFirst.InitTables<UserRole>();
                db.CodeFirst.InitTables<Subject>();
                db.CodeFirst.InitTables<Message>();
                db.CodeFirst.InitTables<FileInfo>();
                // 获取所有实体类型
                //var entityTypes = GetEntityTypes();

                //foreach (var entityType in entityTypes)
                //{
                //    _logger.LogInformation($"正在创建表: {entityType.Name}");

                //    // 使用SqlSugar的CodeFirst功能创建表
                //    db.CodeFirst.InitTables(entityType);
                //    db.CodeFirst.InitTables<User>();
                //    // 确保User表创建

                //    _logger.LogInformation($"表 {entityType.Name} 创建成功");
                //}

                // 创建Orleans相关表
                await CreateOrleansTablesAsync(db);

                _logger.LogInformation("数据库表结构创建完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建数据库表结构失败");
                throw;
            }
        }

        /// <summary>
        /// 获取所有需要创建表的实体类型
        /// </summary>
        //private IEnumerable<Type> GetEntityTypes()
        //{
        //    var assembly = Assembly.GetAssembly(typeof(User));
        //    if (assembly == null)
        //    {
        //        throw new InvalidOperationException("无法找到实体程序集");
        //    }

        //    //return assembly.GetTypes()
        //    //    .Where(type => type.GetCustomAttributes(typeof(SugarTableAttribute), false).Length > 0)
        //    //    .ToList();
        //}

        /// <summary>
        /// 创建Orleans框架相关表
        /// </summary>
        private async Task CreateOrleansTablesAsync(ISqlSugarClient db)
        {
            _logger.LogInformation("创建Orleans框架相关表...");

            try
            {
                // Orleans提醒表
                var remindersTable = @"
                    CREATE TABLE IF NOT EXISTS OrleansReminders (
                        ServiceId VARCHAR(150) NOT NULL,
                        GrainId VARCHAR(150) NOT NULL,
                        ReminderName VARCHAR(150) NOT NULL,
                        StartTime TIMESTAMP NOT NULL,
                        Period VARCHAR(150) NOT NULL,
                        GrainIdHash INT NOT NULL,
                        ReminderHash INT NOT NULL,
                        PRIMARY KEY (ServiceId, GrainId, ReminderName)
                    );";

                // Orleans状态表
                var stateTable = @"
                    CREATE TABLE IF NOT EXISTS OrleansState (
                        GrainIdHash INT NOT NULL,
                        GrainType VARCHAR(512) NOT NULL,
                        GrainId VARCHAR(512) NOT NULL,
                        State VARCHAR(512) NOT NULL,
                        Payload VARCHAR(512) NULL,
                        Version INT NOT NULL,
                        DispatchCounter INT NOT NULL,
                        Status INT NOT NULL,
                        ModifiedOn TIMESTAMP NOT NULL,
                        PRIMARY KEY (GrainIdHash)
                    );";

                // Orleans查询表
                var queryTable = @"
                    CREATE TABLE IF NOT EXISTS OrleansQuery (
                        GrainHash INT NOT NULL,
                        GrainId VARCHAR(512) NOT NULL,
                        Status INT NOT NULL,
                        PRIMARY KEY (GrainHash)
                    );";

                await db.Ado.ExecuteCommandAsync(remindersTable);
                await db.Ado.ExecuteCommandAsync(stateTable);
                await db.Ado.ExecuteCommandAsync(queryTable);

                _logger.LogInformation("Orleans框架相关表创建完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建Orleans框架相关表失败");
                throw;
            }
        }

        /// <summary>
        /// 初始化种子数据
        /// </summary>
        private async Task SeedDataAsync(ISqlSugarClient db)
        {
            if (!_initializerOptions.EnableSeedData)
            {
                _logger.LogInformation("种子数据初始化已禁用");
                return;
            }

            _logger.LogInformation("开始初始化种子数据...");

            try
            {
                // 初始化字典类型数据
                //await SeedDictionaryTypesAsync(db);

                // 初始化角色数据
                //await SeedRolesAsync(db);

                //// 初始化管理员用户
                await SeedAdminUserAsync(db);

                _logger.LogInformation("种子数据初始化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "种子数据初始化失败");
                throw;
            }
        }

        /// <summary>
        /// 初始化字典类型数据
        /// </summary>
        private async Task SeedDictionaryTypesAsync(ISqlSugarClient db)
        {
            var dictionaryTypes = new List<DictionaryType>
            {
                new DictionaryType
                {
                    Code = "USER_STATUS",
                    Name = "用户状态",
                    Description = "用户账户状态枚举",
                    IsEnabled = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    SortOrder = 1
                },
                new DictionaryType
                {
                    Code = "MESSAGE_TYPE",
                    Name = "消息类型",
                    Description = "系统消息类型枚举",
                    IsEnabled = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    SortOrder = 2
                }
            };

            foreach (var dictType in dictionaryTypes)
            {
                var exists = await db.Queryable<DictionaryType>()
                    .Where(x => x.Code == dictType.Code)
                    .AnyAsync();

                if (!exists)
                {
                    await db.Insertable(dictType).ExecuteCommandAsync();
                    _logger.LogInformation($"创建字典类型: {dictType.Code}");
                }
            }
        }

        /// <summary>
        /// 初始化角色数据
        /// </summary>
        private async Task SeedRolesAsync(ISqlSugarClient db)
        {
            var roles = new List<Role>
            {
                new Role
                {
                    name = "超级管理员",
                    code = "SUPER_ADMIN",
                    description = "系统超级管理员，拥有所有权限",
                    is_enabled = true,
                    is_system_role = true,
                    tenant_id = 0,
                    created_at = DateTime.UtcNow,
                    updated_at = DateTime.UtcNow
                },
                new Role
                {
                    name = "管理员",
                    code = "ADMIN",
                    description = "系统管理员，拥有大部分管理权限",
                    is_enabled = true,
                    is_system_role = true,
                    tenant_id = 0,
                    created_at = DateTime.UtcNow,
                    updated_at = DateTime.UtcNow
                },
                new Role
                {
                    name = "普通用户",
                    code = "USER",
                    description = "普通用户，拥有基本权限",
                    is_enabled = true,
                    is_system_role = true,
                    tenant_id = 0,
                    created_at = DateTime.UtcNow,
                    updated_at = DateTime.UtcNow
                }
            };

            foreach (var role in roles)
            {
                var exists = await db.Queryable<Role>()
                    .Where(x => x.code == role.code)
                    .AnyAsync();

                if (!exists)
                {
                    await db.Insertable(role).ExecuteCommandAsync();
                    _logger.LogInformation($"创建角色: {role.code}");
                }
            }
        }

        /// <summary>
        /// 初始化管理员用户
        /// </summary>
        private async Task SeedAdminUserAsync(ISqlSugarClient db)
        {
            var adminExists = await db.Queryable<User>()
                .Where(x => x.username == "admin")
                .AnyAsync();

            if (!adminExists)
            {
                // 生成密码哈希
                var passwordHash = CryptoHelper.GeneratePasswordHash("admin123");
                
                var snowflakeGenerator = new SnowflakeIdGenerator(1); // 机器ID为1

                var adminUser = new User
                {
                    id = snowflakeGenerator.NextId(),
                    username = "admin",
                    display_name = "系统管理员",
                    email = "admin@fakemicro.com",
                    password_hash = passwordHash,
                    password_salt = "123456789",
                    is_active = true,
                    role = "SUPER_ADMIN",
                    status = "Active",
                    phone = "15810010011",
                    email_verified = true,
                    phone_verified = false,
                    login_attempts = 0,
                    created_at = DateTime.UtcNow,
                    updated_at = DateTime.UtcNow,
                    last_login_at = DateTime.UtcNow,
                    tenant_id = 100,
                    locked_until = DateTime.UtcNow,
                    refresh_token=Guid.NewGuid().ToString(),
                    refresh_token_expiry=DateTime.UtcNow.AddDays(1),
                    deleted_at = DateTime.UtcNow,
                    is_deleted = false,
                };

                await db.Insertable(adminUser).ExecuteCommandAsync();
                _logger.LogInformation("创建默认管理员用户: admin (密码: admin123)");
            }
        }

        /// <summary>
        /// 创建数据库索引
        /// </summary>
        private async Task CreateIndexesAsync(ISqlSugarClient db)
        {
            if (!_initializerOptions.EnableIndexCreation)
            {
                _logger.LogInformation("索引创建已禁用");
                return;
            }

            _logger.LogInformation("开始创建数据库索引...");

            try
            {
                // 用户表索引
                await CreateIndexIfNotExists(db, "users", "idx_users_username", "username");
                await CreateIndexIfNotExists(db, "users", "idx_users_email", "email");
                await CreateIndexIfNotExists(db, "users", "idx_users_tenant_id", "tenant_id");
                await CreateIndexIfNotExists(db, "users", "idx_users_is_deleted", "is_deleted");

                // 角色表索引
                await CreateIndexIfNotExists(db, "roles", "idx_roles_code", "code");
                await CreateIndexIfNotExists(db, "roles", "idx_roles_tenant_id", "tenant_id");
                await CreateIndexIfNotExists(db, "roles", "idx_roles_is_deleted", "is_deleted");

                _logger.LogInformation("数据库索引创建完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建数据库索引失败");
                throw;
            }
        }

        /// <summary>
        /// 创建索引（如果不存在）
        /// </summary>
        private async Task CreateIndexIfNotExists(ISqlSugarClient db, string tableName, string indexName, string columnName)
        {
            var checkIndexSql = _sqlSugarOptions.DbType switch
            {
               DatabaseType.PostgreSQL => $"SELECT 1 FROM pg_indexes WHERE tablename = '{tableName.ToLower()}' AND indexname = '{indexName.ToLower()}'",
               DatabaseType.MySQL => $"SHOW INDEX FROM {tableName} WHERE Key_name = '{indexName}'",
               DatabaseType.SQLServer => $"SELECT 1 FROM sys.indexes WHERE name = '{indexName}'",
                _ => throw new NotSupportedException($"不支持的数据库类型: {_sqlSugarOptions.DbType}")
            };

            var indexExists = await db.Ado.GetScalarAsync(checkIndexSql);
            
            if (indexExists == null || indexExists == DBNull.Value)
            {
                var createIndexSql = $"CREATE INDEX {indexName} ON {tableName} ({columnName})";
                await db.Ado.ExecuteCommandAsync(createIndexSql);
                _logger.LogInformation($"创建索引: {indexName}");
            }
        }
    }

    /// <summary>
    /// 数据库初始化选项配置
    /// </summary>
    public class DatabaseInitializerOptions
    {
        /// <summary>
        /// 是否启用种子数据初始化
        /// </summary>
        public bool EnableSeedData { get; set; } = true;

        /// <summary>
        /// 是否启用索引创建
        /// </summary>
        public bool EnableIndexCreation { get; set; } = true;

        /// <summary>
        /// 是否在开发环境重新创建表
        /// </summary>
        public bool RecreateTablesInDevelopment { get; set; } = false;

        /// <summary>
        /// 数据库迁移版本
        /// </summary>
        public string MigrationVersion { get; set; } = "1.0.0";
    }
}