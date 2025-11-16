using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace FakeMicro.OrleansDiagnostics
{
    public class OrleansConnectionDiagnostics
    {
        private static readonly string ConnectionString = "Host=localhost;Port=5432;Database=fakemicro;Username=postgres;Password=123456";
        private const int DefaultGatewayPort = 30000;
        private const int DefaultSiloPort = 11111;
        
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== Orleans 连接诊断与修复工具 ===");
            
            try
            {
                // 1. 检查防火墙设置
                CheckFirewallSettings();
                
                // 2. 检查PostgreSQL服务状态
                CheckPostgreSqlServiceStatus();
                
                // 3. 检查网络连接和端口
                await CheckNetworkConnectivity();
                
                // 4. 检查和修复数据库
                await CheckAndFixDatabase();
                
                // 5. 清理旧的集群数据
                await CleanupClusterData();
                
                // 6. 打印启动建议
                PrintStartupInstructions();
                
                Console.WriteLine("\n诊断与修复完成！请按照上述建议重新启动Orleans服务。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n错误: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
            
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
        
        private static void CheckFirewallSettings()
        {
            Console.WriteLine("\n1. 检查防火墙设置:");
            
            try
            {
                // 使用netsh命令检查防火墙规则
                var gatewayRule = ExecuteCommand($"netsh advfirewall firewall show rule name=\"Orleans Gateway Port 30000\" dir=in");
                var siloRule = ExecuteCommand($"netsh advfirewall firewall show rule name=\"Orleans Silo Port 11111\" dir=in");
                
                bool gatewayRuleExists = gatewayRule.Contains("规则信息:");
                bool siloRuleExists = siloRule.Contains("规则信息:");
                
                Console.WriteLine($"   网关端口 30000 防火墙规则: {(gatewayRuleExists ? "存在" : "不存在")}");
                Console.WriteLine($"   Silo端口 11111 防火墙规则: {(siloRuleExists ? "存在" : "不存在")}");
                
                if (!gatewayRuleExists || !siloRuleExists)
                {
                    Console.WriteLine("   建议: 创建防火墙规则以允许Orleans端口通信");
                    Console.WriteLine("   执行以下命令添加防火墙规则:");
                    Console.WriteLine("   netsh advfirewall firewall add rule name=\"Orleans Gateway Port 30000\" dir=in action=allow protocol=TCP localport=30000 remoteip=any");
                    Console.WriteLine("   netsh advfirewall firewall add rule name=\"Orleans Silo Port 11111\" dir=in action=allow protocol=TCP localport=11111 remoteip=any");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   检查防火墙规则时出错: {ex.Message}");
                Console.WriteLine("   请手动检查防火墙设置");
            }
        }
        
        private static void CheckPostgreSqlServiceStatus()
        {
            Console.WriteLine("\n2. 检查PostgreSQL服务状态:");
            
            try
            {
                var output = ExecuteCommand("sc query PostgreSQL");
                bool serviceRunning = output.Contains("STATE              : 4  RUNNING") || 
                                      output.Contains("状态              : 4  运行中");
                
                Console.WriteLine($"   PostgreSQL服务: {(serviceRunning ? "正在运行" : "未运行")}");
                
                if (!serviceRunning)
                {
                    Console.WriteLine("   建议: 启动PostgreSQL服务");
                    Console.WriteLine("   执行以下命令启动服务:");
                    Console.WriteLine("   net start PostgreSQL");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   检查PostgreSQL服务时出错: {ex.Message}");
                Console.WriteLine("   请手动检查PostgreSQL服务状态");
            }
        }
        
        private static async Task CleanupClusterData()
        {
            Console.WriteLine("\n5. 清理集群数据:");
            
            try
            {
                using (var conn = new NpgsqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();
                    await CleanupOldClusterData(conn);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   清理集群数据时出错: {ex.Message}");
            }
        }
        
        private static string ExecuteCommand(string command)
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.Arguments = $"/c {command}";
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    
                    return output + error;
                }
            }
            catch (Exception ex)
            {
                return $"执行命令时出错: {ex.Message}";
            }
        }
        
        private static async Task CheckNetworkConnectivity()
        {
            Console.WriteLine("\n3. 检查网络连接:");
            
            // 检查端口30000 (Gateway)
            bool gatewayPortOpen = await CheckPort(30000);
            Console.WriteLine($"   网关端口 30000: {(gatewayPortOpen ? "开放" : "未开放")}");
            
            // 检查端口11111 (Silo)
            bool siloPortOpen = await CheckPort(11111);
            Console.WriteLine($"   Silo端口 11111: {(siloPortOpen ? "开放" : "未开放")}");
            
            // 检查PostgreSQL端口5432
            bool pgPortOpen = await CheckPort(5432);
            Console.WriteLine($"   PostgreSQL端口 5432: {(pgPortOpen ? "开放" : "未开放")}");
            
            if (!gatewayPortOpen && !siloPortOpen)
            {
                Console.WriteLine("   警告: 未发现Orleans Silo正在运行");
                Console.WriteLine("   请确保先启动Orleans Silo，再启动API服务");
            }
        }
        
        private static async Task CheckAndFixDatabase()
        {
            Console.WriteLine("\n2. 检查和修复数据库表结构...");
            
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                Console.WriteLine("成功连接到数据库: fakemicro");
                
                // 检查OrleansMembershipTable
                bool membershipTableExists = await TableExists(conn, "orleansmembershiptable");
                Console.WriteLine($"OrleansMembershipTable: {(membershipTableExists ? "存在" : "不存在")}");
                
                // 检查OrleansMembershipVersionTable
                bool versionTableExists = await TableExists(conn, "orleansmembershipversiontable");
                Console.WriteLine($"OrleansMembershipVersionTable: {(versionTableExists ? "存在" : "不存在")}");
                
                // 如果表不存在，创建它们
                if (!versionTableExists)
                {
                    Console.WriteLine("创建OrleansMembershipVersionTable...");
                    await CreateMembershipVersionTable(conn);
                }
                
                // 确保字段都是小写
                await EnsureLowerCaseColumns(conn, "orleansmembershipversiontable");
                await EnsureLowerCaseColumns(conn, "orleansmembershiptable");
                
                // 清理旧的集群数据（如果需要）
                await CleanupOldClusterData(conn);
            }
        }
        
        private static async Task<bool> CheckPort(int port)
        {
            // 异步端口检查方法
            try
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    // 超时设置为1秒
                    socket.ReceiveTimeout = 1000;
                    socket.SendTimeout = 1000;
                    
                    var result = await Task.Run(() => 
                    {
                        try
                        {
                            IAsyncResult ar = socket.BeginConnect(IPAddress.Loopback, port, null, null);
                            bool success = ar.AsyncWaitHandle.WaitOne(1000);
                            return success && socket.Connected;
                        }
                        catch
                        {
                            return false;
                        }
                        finally
                        {
                            if (socket.Connected)
                                socket.Disconnect(false);
                        }
                    });
                    
                    return result;
                }
            }
            catch
            {
                return false;
            }
        }
        
        private static async Task<bool> TableExists(NpgsqlConnection conn, string tableName)
        {
            var cmd = new NpgsqlCommand(
                "SELECT EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = @tableName);", conn);
            cmd.Parameters.AddWithValue("tableName", tableName);
            
            return Convert.ToBoolean(await cmd.ExecuteScalarAsync());
        }
        
        private static async Task CreateMembershipVersionTable(NpgsqlConnection conn)
        {
            var createTableSql = @"
            CREATE TABLE orleansmembershipversiontable (
                deploymentid VARCHAR(150) NOT NULL,
                timestamp BIGINT NOT NULL,
                version INT NOT NULL,
                PRIMARY KEY (deploymentid)
            );
            
            INSERT INTO orleansmembershipversiontable (deploymentid, timestamp, version) 
            VALUES ('FakeMicroCluster', 0, 0);
            
            -- 创建OrleansMembershipTable（如果不存在）
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
            
            -- 创建必要的索引
            CREATE INDEX IF NOT EXISTS idx_orleansmembershiptable_deployment_status 
            ON orleansmembershiptable (deploymentid, status);
            
            CREATE INDEX IF NOT EXISTS idx_orleansmembershiptable_iomalivetime 
            ON orleansmembershiptable (iamalivetime);
            
            -- 创建OrleansQueryTable（如果不存在）
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
            ('GatewaysQueryKey', 'SELECT address, proxyport FROM orleansmembershiptable WHERE deploymentid = @deploymentid AND status = @status;');
            
            -- 创建OrleansRemindersTable（如果不存在）
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
            ON orleansreminderstable (grainid);";
            
            await conn.ExecuteNonQueryAsync(createTableSql);
            Console.WriteLine("数据库表创建/更新成功");
        }
        
        private static async Task EnsureLowerCaseColumns(NpgsqlConnection conn, string tableName)
        {
            Console.WriteLine($"检查并修复 {tableName} 的字段名大小写...");
            
            // 获取表的所有列
            var columnsCmd = new NpgsqlCommand(@"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_schema = 'public' AND table_name = @tableName;", conn);
            columnsCmd.Parameters.AddWithValue("tableName", tableName);
            
            var reader = await columnsCmd.ExecuteReaderAsync();
            var columns = new List<string>();
            
            while (await reader.ReadAsync())
            {
                columns.Add(reader.GetString(0));
            }
            await reader.CloseAsync();
            
            // 检查是否需要修复大写字段
            bool hasUpperCaseColumns = columns.Any(col => col.Any(char.IsUpper));
            
            if (hasUpperCaseColumns)
            {
                Console.WriteLine($"发现大写字段，正在修复 {tableName}...");
                
                // 备份表数据
                string backupTableName = $"{tableName}_backup_{DateTime.Now.Ticks}";
                await conn.ExecuteNonQueryAsync($"CREATE TABLE {backupTableName} AS SELECT * FROM {tableName};");
                
                try
                {
                    // 对于OrleansMembershipVersionTable的特殊处理
                    if (tableName == "orleansmembershipversiontable")
                    {
                        // 重创建表使用小写字段名
                        await conn.ExecuteNonQueryAsync($"DROP TABLE IF EXISTS {tableName} CASCADE;");
                        await CreateMembershipVersionTable(conn);
                    }
                    else
                    {
                        // 其他表的处理逻辑
                        Console.WriteLine($"注意: {tableName} 表包含大写字段，可能需要手动修复。");
                    }
                    
                    Console.WriteLine($"{tableName} 表修复完成");
                }
                catch (Exception ex)
                {
                    // 如果出错，恢复数据
                    Console.WriteLine($"修复 {tableName} 时出错: {ex.Message}");
                    Console.WriteLine("正在恢复原始表...");
                    await conn.ExecuteNonQueryAsync($"DROP TABLE IF EXISTS {tableName};");
                    await conn.ExecuteNonQueryAsync($"ALTER TABLE {backupTableName} RENAME TO {tableName};");
                }
            }
            else
            {
                Console.WriteLine($"{tableName} 字段名已经是小写，无需修复");
            }
        }
        
        private static async Task CleanupOldClusterData(NpgsqlConnection conn)
        {
            Console.WriteLine("清理旧的集群数据...");
            
            try
            {
                // 检查并清理OrleansMembershipTable中的旧数据
                var countCmd = new NpgsqlCommand("SELECT COUNT(*) FROM orleansmembershiptable WHERE deploymentid = 'FakeMicroCluster';", conn);
                int oldRecordsCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
                
                if (oldRecordsCount > 0)
                {
                    Console.WriteLine($"发现 {oldRecordsCount} 条旧的集群记录，正在清理...");
                    await conn.ExecuteNonQueryAsync("DELETE FROM orleansmembershiptable WHERE deploymentid = 'FakeMicroCluster';");
                    // 重置版本表
                    await conn.ExecuteNonQueryAsync("UPDATE orleansmembershipversiontable SET timestamp = 0, version = 0 WHERE deploymentid = 'FakeMicroCluster';");
                    Console.WriteLine("旧数据清理完成");
                }
                else
                {
                    Console.WriteLine("没有发现旧的集群数据，无需清理");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清理旧数据时出错: {ex.Message}");
            }
        }
        
        private static void PrintStartupInstructions()
        {
            Console.WriteLine("\n3. 启动建议:");
            Console.WriteLine("   a. 首先启动Orleans Silo:");
            Console.WriteLine("      - 确保使用正确的端口配置 (SiloPort=11111, GatewayPort=30000)");
            Console.WriteLine("      - 确认PostgreSQL数据库连接正常");
            Console.WriteLine("   b. 等待Silo完全启动后，再启动API网关");
            Console.WriteLine("   c. 如果仍然出现连接问题:");
            Console.WriteLine("      - 检查防火墙是否阻止了端口30000和11111");
            Console.WriteLine("      - 确认Silo和API使用相同的ClusterId和ServiceId");
            Console.WriteLine("      - 尝试重启PostgreSQL服务");
        }
    }
    
    // 扩展方法类
    public static class NpgsqlExtensions
    {
        public static async Task ExecuteNonQueryAsync(this NpgsqlConnection conn, string sql)
        {
            using (var cmd = new NpgsqlCommand(sql, conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
    
    // 简化的List<T>实现
    public class List<T>
    {
        private readonly System.Collections.Generic.List<T> _items = new System.Collections.Generic.List<T>();
        
        public void Add(T item)
        {
            _items.Add(item);
        }
        
        public bool Any(Func<T, bool> predicate)
        {
            return _items.Any(predicate);
        }
        
        public IEnumerable<T> Where(Func<T, bool> predicate)
        {
            return _items.Where(predicate);
        }
    }
    
    // 简化的IEnumerable<T>扩展
    public static class EnumerableExtensions
    {
        public static bool Any<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            foreach (var item in source)
            {
                if (predicate(item))
                    return true;
            }
            return false;
        }
        
        public static IEnumerable<T> Where<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            foreach (var item in source)
            {
                if (predicate(item))
                    yield return item;
            }
        }
        
        public static T First<T>(this IEnumerable<T> source)
        {
            foreach (var item in source)
            {
                return item;
            }
            throw new InvalidOperationException("序列不包含任何元素");
        }
    }
}