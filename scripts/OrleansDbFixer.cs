using Npgsql;

namespace FakeMicro.OrleansDbFixer
{
    class OrleansDatabaseFixer
    {
        static void Main(string[] args)
        {
            Console.WriteLine("开始修复Orleans数据库字段名...");
            
            // 连接到fakemicro数据库，使用正确的密码
            string connectionString = "Host=localhost;Port=5432;Database=fakemicro;Username=postgres;Password=123456";
            
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    Console.WriteLine("已连接到数据库");
                    
                    // 直接创建或修复OrleansMembershipVersionTable
                    CreateOrFixMembershipVersionTable(connection);
                    
                    // 直接创建或修复OrleansRemindersTable
                    CreateOrFixRemindersTable(connection);
                }
                
                Console.WriteLine("\n修复完成！Orleans数据库表已准备就绪。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
                Console.WriteLine($"错误详情: {ex.StackTrace}");
            }
        }
        
        private static void CreateOrFixMembershipVersionTable(NpgsqlConnection connection)
        {
            try
            {
                // 首先尝试删除表（如果存在）以避免字段名冲突
                string dropTableSql = "DROP TABLE IF EXISTS OrleansMembershipVersionTable CASCADE;";
                using (var cmd = new NpgsqlCommand(dropTableSql, connection))
                {
                    cmd.ExecuteNonQuery();
                    Console.WriteLine("已删除旧的OrleansMembershipVersionTable表（如果存在）");
                }
                
                // 创建新表，使用小写字段名
                string createTableSql = @"
                CREATE TABLE IF NOT EXISTS OrleansMembershipVersionTable
                (
                    DeploymentId varchar(150) NOT NULL,
                    timestamp timestamp NOT NULL,
                    version int NOT NULL,
                    PRIMARY KEY (DeploymentId)
                );
                
                -- 插入初始数据
                INSERT INTO OrleansMembershipVersionTable (DeploymentId, timestamp, version)
                VALUES ('FakeMicroCluster', NOW(), 1);";
                
                using (var cmd = new NpgsqlCommand(createTableSql, connection))
                {
                    cmd.ExecuteNonQuery();
                    Console.WriteLine("成功创建OrleansMembershipVersionTable表并插入初始数据");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建OrleansMembershipVersionTable时出错: {ex.Message}");
            }
        }
        
        private static void CreateOrFixRemindersTable(NpgsqlConnection connection)
        {
            try
            {
                // 首先尝试删除表（如果存在）以避免字段名冲突
                string dropTableSql = "DROP TABLE IF EXISTS OrleansRemindersTable CASCADE;";
                using (var cmd = new NpgsqlCommand(dropTableSql, connection))
                {
                    cmd.ExecuteNonQuery();
                    Console.WriteLine("已删除旧的OrleansRemindersTable表（如果存在）");
                }
                
                // 创建新表，使用小写字段名
                string createTableSql = @"
                CREATE TABLE IF NOT EXISTS OrleansRemindersTable
                (
                    ServiceId varchar(150) NOT NULL,
                    GrainId varchar(150) NOT NULL,
                    ReminderName varchar(150) NOT NULL,
                    StartTime timestamp NOT NULL,
                    period bigint NOT NULL,
                    GrainHash int NOT NULL,
                    version int NOT NULL,
                    PRIMARY KEY (ServiceId, GrainId, ReminderName)
                );
                
                -- 创建索引
                CREATE INDEX IF NOT EXISTS OrleansRemindersTable_ServiceId_GrainHash ON OrleansRemindersTable(ServiceId, GrainHash);
                CREATE INDEX IF NOT EXISTS idx_orleans_reminders_grainhash ON OrleansRemindersTable(GrainHash);";
                
                using (var cmd = new NpgsqlCommand(createTableSql, connection))
                {
                    cmd.ExecuteNonQuery();
                    Console.WriteLine("成功创建OrleansRemindersTable表和索引");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建OrleansRemindersTable时出错: {ex.Message}");
            }
        }
    }
}