using Npgsql;
using System;
using System.Threading.Tasks;

namespace FakeMicro.OrleansDiagnostics
{
    class DbFix
    {
        private static readonly string ConnectionString = "Host=localhost;Port=5432;Database=fakemicro;Username=postgres;Password=123456";
        
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== 数据库修复工具 ===");
            
            try
            {
                using (var conn = new NpgsqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();
                    Console.WriteLine("成功连接到数据库: fakemicro");
                    Console.WriteLine();
                    
                    // 修复数据库
                    await FixDatabase(conn);
                    
                    Console.WriteLine("数据库修复完成！");
                    Console.WriteLine();
                    
                    // 验证修复结果
                    await VerifyDatabase(conn);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
            }
            
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
        
        static async Task FixDatabase(NpgsqlConnection conn)
        {
            Console.WriteLine("开始修复数据库...");

            try
            {
                // 1. 添加缺失的字段
                Console.WriteLine("修复表结构...");
                await AddMissingColumns(conn);

                // 2. 创建枚举类型
                Console.WriteLine("创建枚举类型...");
                await CreateEnums(conn);

                // 3. 修复roles表约束
                Console.WriteLine("修复roles表约束...");
                await FixRolesConstraints(conn);

                // 4. 修复users表约束
                Console.WriteLine("修复users表约束...");
                await FixUsersConstraints(conn);

                // 5. 创建sessions表
                Console.WriteLine("创建sessions表...");
                await CreateSessionsTable(conn);

                // 6. 插入默认角色
                Console.WriteLine("插入默认角色...");
                await InsertDefaultRoles(conn);

                // 7. 创建管理员用户
                Console.WriteLine("创建管理员用户...");
                await CreateAdminUser(conn);

                // 8. 创建索引
                Console.WriteLine("创建索引...");
                await CreateIndexes(conn);

                Console.WriteLine("数据库修复成功完成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"修复数据库时出错: {ex.Message}");
                throw;
            }
        }

        static async Task AddMissingColumns(NpgsqlConnection conn)
        {
            var alterTableSqls = new[]
            {
                "ALTER TABLE users ADD COLUMN IF NOT EXISTS full_name VARCHAR(255);",
                "ALTER TABLE messages ADD COLUMN IF NOT EXISTS recipient_id INTEGER;"
            };
            
            foreach (var sql in alterTableSqls)
            {
                try
                {
                    await ExecuteSql(conn, sql);
                    Console.WriteLine($"✓ 执行成功: {sql}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ 字段添加警告: {ex.Message}");
                }
            }
        }

        static async Task CreateEnums(NpgsqlConnection conn)
        {
            var enumsSql = @"
            DO $$ BEGIN
                CREATE TYPE user_status AS ENUM ('active', 'inactive', 'suspended');
            EXCEPTION
                WHEN duplicate_object THEN null;
            END $$;
            
            DO $$ BEGIN
                CREATE TYPE message_status AS ENUM ('sent', 'delivered', 'read');
            EXCEPTION
                WHEN duplicate_object THEN null;
            END $$;";
            
            await ExecuteSql(conn, enumsSql);
        }

        static async Task FixRolesConstraints(NpgsqlConnection conn)
        {
            try
            {
                // 为roles表的name字段添加唯一约束
                var constraintSql = "ALTER TABLE roles ADD CONSTRAINT roles_name_unique UNIQUE (name);";
                await ExecuteSql(conn, constraintSql);
                Console.WriteLine("✓ roles表的name字段唯一约束添加成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ 约束添加警告: {ex.Message}");
            }
        }

        static async Task FixUsersConstraints(NpgsqlConnection conn)
        {
            try
            {
                // 为username字段添加唯一约束
                var usernameSql = "ALTER TABLE users ADD CONSTRAINT users_username_unique UNIQUE (username);";
                await ExecuteSql(conn, usernameSql);
                Console.WriteLine("✓ users表username字段唯一约束添加成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ username约束添加警告: {ex.Message}");
            }

            try
            {
                // 为email字段添加唯一约束
                var emailSql = "ALTER TABLE users ADD CONSTRAINT users_email_unique UNIQUE (email);";
                await ExecuteSql(conn, emailSql);
                Console.WriteLine("✓ users表email字段唯一约束添加成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ email约束添加警告: {ex.Message}");
            }
        }

        static async Task CreateSessionsTable(NpgsqlConnection conn)
        {
            var sessionsSql = @"
            CREATE TABLE IF NOT EXISTS sessions (
                id SERIAL PRIMARY KEY,
                user_id INTEGER REFERENCES users(id) ON DELETE CASCADE,
                token VARCHAR(255) UNIQUE NOT NULL,
                expires_at TIMESTAMP NOT NULL,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );";
            
            await ExecuteSql(conn, sessionsSql);
        }

        static async Task InsertDefaultRoles(NpgsqlConnection conn)
        {
            try
            {
                // 检查并修复id字段的默认值设置
                var checkIdColumn = "SELECT column_name, column_default, is_nullable FROM information_schema.columns WHERE table_name = 'roles' AND column_name = 'id';";
                using (var cmd = new NpgsqlCommand(checkIdColumn, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        var columnDefault = reader["column_default"]?.ToString();
                        if (string.IsNullOrEmpty(columnDefault))
                        {
                            // 如果id字段没有默认值，创建序列并设置默认值
                            await reader.CloseAsync();
                            
                            // 创建序列
                            try 
                            {
                                var createSeqSql = "CREATE SEQUENCE IF NOT EXISTS roles_id_seq START 1 OWNED BY roles.id;";
                                await ExecuteSql(conn, createSeqSql);
                                Console.WriteLine("✓ 序列创建成功");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"⚠️ 序列创建警告: {ex.Message}");
                            }
                            
                            // 设置id字段默认值
                            try
                            {
                                var alterIdSql = "ALTER TABLE roles ALTER COLUMN id SET DEFAULT nextval('roles_id_seq'::regclass);";
                                await ExecuteSql(conn, alterIdSql);
                                Console.WriteLine("✓ id字段默认值设置成功");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"⚠️ id字段设置警告: {ex.Message}");
                            }
                        }
                    }
                    await reader.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ 检查角色表结构警告: {ex.Message}");
            }

            // 插入默认角色，为id和is_deleted提供明确值
            var rolesSql = @"
            INSERT INTO roles (id, name, description, is_deleted) VALUES 
            (1, 'admin', 'Administrator with full access', false),
            (2, 'user', 'Regular user', false),
            (3, 'moderator', 'Content moderator', false)
            ON CONFLICT (name) DO NOTHING;";
            
            await ExecuteSql(conn, rolesSql);
        }

        static async Task CreateAdminUser(NpgsqlConnection conn)
        {
            var adminSql = @"
            INSERT INTO users (username, email, password_hash, full_name, status) VALUES 
            ('admin', 'admin@fakemicro.com', '$2a$11$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewfhP3Xo0x3rd9e6', 'System Administrator', 'active')
            ON CONFLICT (username) DO NOTHING;";
            
            await ExecuteSql(conn, adminSql);
        }

        static async Task CreateIndexes(NpgsqlConnection conn)
        {
            var indexesSql = new[]
            {
                "CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);",
                "CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);",
                "CREATE INDEX IF NOT EXISTS idx_messages_sender ON messages(sender_id);",
                "CREATE INDEX IF NOT EXISTS idx_messages_recipient ON messages(recipient_id);",
                "CREATE INDEX IF NOT EXISTS idx_messages_status ON messages(status);",
                "CREATE INDEX IF NOT EXISTS idx_sessions_token ON sessions(token);",
                "CREATE INDEX IF NOT EXISTS idx_sessions_user ON sessions(user_id);"
            };
            
            foreach (var sql in indexesSql)
            {
                try
                {
                    await ExecuteSql(conn, sql);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ 索引创建警告: {ex.Message}");
                }
            }
        }

        static async Task VerifyDatabase(NpgsqlConnection conn)
        {
            Console.WriteLine("=== 验证修复结果 ===");
            
            // 检查所有表
            await CheckAllTables(conn);
            
            // 检查管理员用户
            await CheckAdminUser(conn);
        }

        static async Task CheckAllTables(NpgsqlConnection conn)
        {
            Console.WriteLine("=== 表状态检查 ===");
            
            var allTables = new[] {
                "orleansmembershipversiontable",
                "orleansmembershiptable",
                "orleansquerytable", 
                "orleansreminderstable",
                "users",
                "roles",
                "user_roles",
                "messages",
                "sessions"
            };
            
            foreach (var table in allTables)
            {
                try
                {
                    var sql = $"SELECT COUNT(*) FROM {table};";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        var count = await cmd.ExecuteScalarAsync();
                        Console.WriteLine($"✓ {table}: 存在, 行数: {count}");
                    }
                }
                catch
                {
                    Console.WriteLine($"✗ {table}: 不存在或无法访问");
                }
            }
            
            Console.WriteLine();
        }

        static async Task CheckAdminUser(NpgsqlConnection conn)
        {
            Console.WriteLine("=== 检查管理员用户 ===");
            try
            {
                var sql = "SELECT username, email, full_name FROM users WHERE username = 'admin';";
                using (var cmd = new NpgsqlCommand(sql, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        Console.WriteLine($"✓ 管理员用户存在: {reader["username"]} ({reader["email"]}) - {reader["full_name"]}");
                    }
                    else
                    {
                        Console.WriteLine("✗ 管理员用户不存在");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 检查管理员用户失败: {ex.Message}");
            }
        }

        static async Task ExecuteSql(NpgsqlConnection conn, string sql)
        {
            try
            {
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"执行SQL时出错: {ex.Message}");
                Console.WriteLine($"SQL: {sql}");
                throw;
            }
        }
    }
}