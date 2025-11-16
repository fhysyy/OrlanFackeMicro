using Npgsql;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FakeMicro.OrleansDiagnostics
{
    class DirectDbInit
    {
        private static readonly string ConnectionString = "Host=localhost;Port=5432;Database=fakemicro;Username=postgres;Password=123456";
        
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== 直接数据库初始化工具 ===");
            
            try
            {
                using (var conn = new NpgsqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();
                    Console.WriteLine("成功连接到数据库: fakemicro");
                    
                    // 执行Orleans表结构创建
                    await CreateOrleansTables(conn);
                    
                    // 执行FakeMicro业务表创建  
                    await CreateFakeMicroTables(conn);
                    
                    Console.WriteLine("数据库初始化完成！");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
            
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
        
        static async Task CreateOrleansTables(NpgsqlConnection conn)
        {
            Console.WriteLine("创建Orleans表结构...");
            
            var orleansTablesSql = @"
            -- 创建OrleansMembershipVersionTable
            CREATE TABLE IF NOT EXISTS orleansmembershipversiontable (
                deploymentid VARCHAR(150) NOT NULL,
                timestamp BIGINT NOT NULL,
                version INT NOT NULL,
                PRIMARY KEY (deploymentid)
            );
            
            -- 插入初始版本数据
            INSERT INTO orleansmembershipversiontable (deploymentid, timestamp, version) 
            VALUES ('FakeMicroCluster', 0, 0)
            ON CONFLICT (deploymentid) DO NOTHING;
            
            -- 创建OrleansMembershipTable
            CREATE TABLE IF NOT EXISTS orleansmembershiptable (
                deploymentid VARCHAR(150) NOT NULL,
                address VARCHAR(45) NOT NULL,
                port INTEGER NOT NULL,
                generation INTEGER NOT NULL,
                siloname VARCHAR(150) NOT NULL,
                hostname VARCHAR(150) NOT NULL,
                status INTEGER NOT NULL,
                proxyport INTEGER NOT NULL,
                suspecttimes TEXT NULL,
                starttime BIGINT NOT NULL,
                iamalivetime BIGINT NOT NULL,
                PRIMARY KEY (deploymentid, address, port, generation)
            );
            
            -- 创建索引
            CREATE INDEX IF NOT EXISTS idx_orleansmembershiptable_deployment_status 
            ON orleansmembershiptable (deploymentid, status);
            
            CREATE INDEX IF NOT EXISTS idx_orleansmembershiptable_iomalivetime 
            ON orleansmembershiptable (iamalivetime);
            
            -- 创建OrleansQueryTable
            CREATE TABLE IF NOT EXISTS orleansquerytable (
                querykey VARCHAR(150) NOT NULL,
                querytext TEXT NOT NULL,
                PRIMARY KEY (querykey)
            );
            
            -- 插入必要的查询
            INSERT INTO orleansquerytable (querykey, querytext)
            VALUES
            ('MembershipReadRowKey', 'SELECT * FROM orleansmembershiptable WHERE deploymentid = @deploymentid AND address = @address AND port = @port AND generation = @generation;'),
            ('MembershipReadAllKey', 'SELECT * FROM orleansmembershiptable WHERE deploymentid = @deploymentid;'),
            ('InsertMembershipVersionKey', 'INSERT INTO orleansmembershipversiontable (deploymentid, timestamp, version) VALUES (@deploymentid, @timestamp, @version) ON CONFLICT (deploymentid) DO UPDATE SET timestamp = @timestamp, version = @version;'),
            ('UpdateIAmAlivetimeKey', 'UPDATE orleansmembershiptable SET iamalivetime = @iamalivetime WHERE deploymentid = @deploymentid AND address = @address AND port = @port AND generation = @generation;'),
            ('InsertMembershipKey', 'INSERT INTO orleansmembershiptable (deploymentid, address, port, generation, siloname, hostname, status, proxyport, suspecttimes, starttime, iamalivetime) VALUES (@deploymentid, @address, @port, @generation, @siloname, @hostname, @status, @proxyport, @suspecttimes, @starttime, @iamalivetime);'),
            ('UpdateMembershipKey', 'UPDATE orleansmembershiptable SET status = @status, suspecttimes = @suspecttimes, iamalivetime = @iamalivetime WHERE deploymentid = @deploymentid AND address = @address AND port = @port AND generation = @generation;'),
            ('DeleteMembershipTableEntriesKey', 'DELETE FROM orleansmembershiptable WHERE deploymentid = @deploymentid;'),
            ('GatewaysQueryKey', 'SELECT address, proxyport FROM orleansmembershiptable WHERE deploymentid = @deploymentid AND status = @status;')
            ON CONFLICT (querykey) DO NOTHING;
            
            -- 创建OrleansRemindersTable
            CREATE TABLE IF NOT EXISTS orleansreminderstable (
                serviceid VARCHAR(150) NOT NULL,
                grainid VARCHAR(150) NOT NULL,
                reminderkey VARCHAR(150) NOT NULL,
                starttime BIGINT NOT NULL,
                period BIGINT NOT NULL,
                PRIMARY KEY (serviceid, grainid, reminderkey)
            );
            
            -- 创建索引
            CREATE INDEX IF NOT EXISTS idx_orleansreminderstable_grainid 
            ON orleansreminderstable (grainid);
            ";
            
            await ExecuteSqlCommands(conn, orleansTablesSql);
            Console.WriteLine("Orleans表结构创建完成");
        }
        
        static async Task CreateFakeMicroTables(NpgsqlConnection conn)
        {
            Console.WriteLine("创建FakeMicro业务表结构...");
            
            var fakeMicroSql = @"
            -- 创建枚举类型
            CREATE TYPE user_role AS ENUM ('admin', 'user', 'moderator');
            CREATE TYPE message_status AS ENUM ('active', 'deleted', 'archived');
            CREATE TYPE user_status AS ENUM ('active', 'inactive', 'banned');
            
            -- 创建用户表
            CREATE TABLE IF NOT EXISTS users (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                username VARCHAR(50) UNIQUE NOT NULL,
                email VARCHAR(100) UNIQUE NOT NULL,
                password_hash VARCHAR(255) NOT NULL,
                full_name VARCHAR(100),
                status user_status DEFAULT 'active',
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );
            
            -- 创建角色表
            CREATE TABLE IF NOT EXISTS roles (
                id SERIAL PRIMARY KEY,
                name user_role UNIQUE NOT NULL,
                description TEXT,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );
            
            -- 创建用户角色关联表
            CREATE TABLE IF NOT EXISTS user_roles (
                user_id UUID REFERENCES users(id) ON DELETE CASCADE,
                role_id INTEGER REFERENCES roles(id) ON DELETE CASCADE,
                assigned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (user_id, role_id)
            );
            
            -- 创建消息表
            CREATE TABLE IF NOT EXISTS messages (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                sender_id UUID REFERENCES users(id),
                recipient_id UUID REFERENCES users(id),
                subject VARCHAR(255),
                content TEXT NOT NULL,
                status message_status DEFAULT 'active',
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                read_at TIMESTAMP NULL
            );
            
            -- 创建会话表
            CREATE TABLE IF NOT EXISTS sessions (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                user_id UUID REFERENCES users(id) ON DELETE CASCADE,
                token VARCHAR(255) UNIQUE NOT NULL,
                expires_at TIMESTAMP NOT NULL,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );
            
            -- 插入默认角色
            INSERT INTO roles (name, description) VALUES 
            ('admin', 'Administrator with full access'),
            ('user', 'Regular user'),
            ('moderator', 'Content moderator')
            ON CONFLICT (name) DO NOTHING;
            
            -- 创建管理员用户（如果不存在）
            INSERT INTO users (username, email, password_hash, full_name) VALUES 
            ('admin', 'admin@fakemicro.com', '$2a$11$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewfhP3Xo0x3rd9e6', 'System Administrator')
            ON CONFLICT (username) DO NOTHING;
            
            -- 为管理员分配admin角色
            INSERT INTO user_roles (user_id, role_id)
            SELECT u.id, r.id FROM users u, roles r 
            WHERE u.username = 'admin' AND r.name = 'admin'
            ON CONFLICT (user_id, role_id) DO NOTHING;
            
            -- 创建索引
            CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);
            CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
            CREATE INDEX IF NOT EXISTS idx_messages_sender ON messages(sender_id);
            CREATE INDEX IF NOT EXISTS idx_messages_recipient ON messages(recipient_id);
            CREATE INDEX IF NOT EXISTS idx_messages_status ON messages(status);
            CREATE INDEX IF NOT EXISTS idx_sessions_token ON sessions(token);
            CREATE INDEX IF NOT EXISTS idx_sessions_user ON sessions(user_id);
            ";
            
            await ExecuteSqlCommands(conn, fakeMicroSql);
            Console.WriteLine("FakeMicro业务表结构创建完成");
        }
        
        static async Task ExecuteSqlCommands(NpgsqlConnection conn, string sql)
        {
            // 按分号分割SQL语句
            var commands = sql.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var command in commands)
            {
                var trimmedCommand = command.Trim();
                if (!string.IsNullOrWhiteSpace(trimmedCommand))
                {
                    try
                    {
                        using (var cmd = new NpgsqlCommand(trimmedCommand, conn))
                        {
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"执行SQL命令时出错: {ex.Message}");
                        Console.WriteLine($"命令: {trimmedCommand}");
                    }
                }
            }
        }
    }
}