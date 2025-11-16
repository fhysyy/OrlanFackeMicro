using System;
using System.IO;
using Npgsql;

namespace FakeMicro.DatabaseInitializer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("开始初始化FakeMicro数据库...");
            
            try
            {
                // PostgreSQL连接字符串 - 根据您的配置调整密码
                string connectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=123456";
                
                // 创建数据库连接
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    Console.WriteLine("成功连接到PostgreSQL数据库");
                    
                    // 读取完整数据库初始化脚本
                    string scriptPath = "F:\\Orleans\\Orleans\\scripts\\init-db.sql";
                    if (!File.Exists(scriptPath))
                    {
                        scriptPath = "..\\scripts\\init-db.sql";
                        if (!File.Exists(scriptPath))
                        {
                            scriptPath = ".\\scripts\\init-db.sql";
                        }
                    }
                    
                    if (!File.Exists(scriptPath))
                    {
                        Console.WriteLine($"找不到初始化脚本: {scriptPath}");
                        // 备用方案：手动创建基本表结构
                        CreateBasicTables(connection);
                        return;
                    }
                    
                    string sqlScript = File.ReadAllText(scriptPath);
                    Console.WriteLine($"读取SQL脚本文件: {scriptPath}");
                    
                    // 按分号分割SQL语句并执行
                    var sqlStatements = sqlScript.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    int executedStatements = 0;
                    
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            foreach (string sqlStatement in sqlStatements)
                            {
                                string trimmedStatement = sqlStatement.Trim();
                                if (!string.IsNullOrWhiteSpace(trimmedStatement) && 
                                    !trimmedStatement.StartsWith("--") && 
                                    !trimmedStatement.StartsWith("/*"))
                                {
                                    using (var command = new NpgsqlCommand(trimmedStatement, connection, transaction))
                                    {
                                        command.ExecuteNonQuery();
                                        executedStatements++;
                                    }
                                }
                            }
                            
                            transaction.Commit();
                            Console.WriteLine($"成功执行 {executedStatements} 条SQL语句！");
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            Console.WriteLine($"执行SQL时发生错误，已回滚: {ex.Message}");
                            throw;
                        }
                    }
                    
                    Console.WriteLine("FakeMicro数据库初始化成功！");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"初始化数据库时发生错误: {ex.Message}");
                Console.WriteLine($"错误详情: {ex.StackTrace}");
            }
            
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
        
        private static void CreateBasicTables(NpgsqlConnection connection)
        {
            Console.WriteLine("使用备用方案创建基本表结构...");
            
            // 创建基本的Orleans表
            var createTablesSql = @"
                -- 创建fakemicro数据库
                CREATE DATABASE fakemicro;
                
                -- 切换到fakemicro数据库（这需要单独连接）
                -- 这里只是演示，实际需要在fakemicro数据库中执行
            ";
            
            try
            {
                // 首先创建数据库
                using (var command = new NpgsqlCommand("CREATE DATABASE fakemicro", connection))
                {
                    command.ExecuteNonQuery();
                    Console.WriteLine("创建数据库fakemicro成功");
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("already exists"))
                {
                    Console.WriteLine("数据库fakemicro已存在");
                }
                else
                {
                    Console.WriteLine($"创建数据库时出错: {ex.Message}");
                }
            }
            
            // 连接fakemicro数据库并创建表
            string fakemicroConnectionString = "Host=localhost;Port=5432;Database=fakemicro;Username=postgres;Password=123456";
            using (var fakemicroConnection = new NpgsqlConnection(fakemicroConnectionString))
            {
                fakemicroConnection.Open();
                Console.WriteLine("已连接到fakemicro数据库");
                
                // 创建基本Orleans表
                var basicTablesSql = @"
                    CREATE TABLE IF NOT EXISTS OrleansMembershipVersionTable (
                        DeploymentId varchar(150) NOT NULL PRIMARY KEY,
                        timestamp timestamp NOT NULL,
                        version int NOT NULL
                    );
                    
                    INSERT INTO OrleansMembershipVersionTable (DeploymentId, timestamp, version) 
                    VALUES ('FakeMicroCluster', NOW(), 1)
                    ON CONFLICT (DeploymentId) DO NOTHING;
                    
                    CREATE TABLE IF NOT EXISTS OrleansRemindersTable (
                        ServiceId varchar(150) NOT NULL,
                        GrainId varchar(150) NOT NULL,
                        ReminderName varchar(150) NOT NULL,
                        StartTime timestamp NOT NULL,
                        period bigint NOT NULL,
                        GrainHash int NOT NULL,
                        version int NOT NULL,
                        PRIMARY KEY (ServiceId, GrainId, ReminderName)
                    );
                    
                    CREATE TABLE IF NOT EXISTS orleansquery (
                        QueryKey varchar(150) NOT NULL PRIMARY KEY,
                        QueryText text NOT NULL
                    );
                    
                    INSERT INTO orleansquery (QueryKey, QueryText) VALUES
                    ('INSERT_ORLEANS_MEMBERSHIP_VERSION_ENTRY', 'INSERT INTO OrleansMembershipVersionTable (DeploymentId, timestamp, version) VALUES (@DeploymentId, @Timestamp, @Version)'),
                    ('UPDATE_ORLEANS_MEMBERSHIP_VERSION_ENTRY', 'UPDATE OrleansMembershipVersionTable SET timestamp = @Timestamp, version = @Version WHERE DeploymentId = @DeploymentId')
                    ON CONFLICT (QueryKey) DO NOTHING;
                ";
                
                using (var command = new NpgsqlCommand(basicTablesSql, fakemicroConnection))
                {
                    command.ExecuteNonQuery();
                    Console.WriteLine("创建基本Orleans表成功");
                }
            }
        }
    }
}