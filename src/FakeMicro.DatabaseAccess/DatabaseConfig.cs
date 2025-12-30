using System.ComponentModel.DataAnnotations;

namespace FakeMicro.DatabaseAccess
{
    /// <summary>
    /// 数据库配置类
    /// </summary>
    public class DatabaseConfig
    {
        /// <summary>
        /// 数据库类型
        /// </summary>
        public string Type { get; set; } = "PostgreSQL";

        /// <summary>
        /// 服务器地址
        /// </summary>
        public string Server { get; set; } = "localhost";

        /// <summary>
        /// 端口号
        /// </summary>
        public int Port { get; set; } = 5432;

        /// <summary>
        /// 数据库名称
        /// </summary>
        public string DatabaseName { get; set; } = "fakemicro";

        /// <summary>
        /// 用户名
        /// </summary>
        public string Username { get; set; } = "postgres";

        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; } = string.Empty; // 不再使用硬编码默认密码

        /// <summary>
        /// 是否信任服务器证书
        /// </summary>
        public bool TrustServerCertificate { get; set; } = false;

        /// <summary>
        /// 连接超时时间（秒）
        /// </summary>
        public int ConnectionTimeout { get; set; } = 30;

        /// <summary>
        /// 最小连接池大小
        /// </summary>
        public int MinPoolSize { get; set; } = 5;

        /// <summary>
        /// 最大连接池大小
        /// </summary>
        public int MaxPoolSize { get; set; } = 100;

        /// <summary>
        /// 获取连接字符串
        /// </summary>
        /// <returns>连接字符串</returns>
        public string GetConnectionString()
        {
            // 直接使用appsettings.json中的配置值
            var server = Server;
            var port = Port;
            var databaseName = DatabaseName;
            var username = Username;
            var password = Password;

            // 根据数据库类型生成连接字符串
            switch (Type?.ToLower())
            {
                case "postgresql":
                    return $"Host={server};Port={port};Database={databaseName};Username={username};Password={password};" +
                           $"Trust Server Certificate={TrustServerCertificate};Timeout={ConnectionTimeout};" +
                           $"MinPoolSize={MinPoolSize};MaxPoolSize={MaxPoolSize};";
                case "sqlserver":
                    return $"Server={server},{port};Database={databaseName};User Id={username};Password={password};" +
                           $"TrustServerCertificate={TrustServerCertificate};Connection Timeout={ConnectionTimeout};" +
                           $"Min Pool Size={MinPoolSize};Max Pool Size={MaxPoolSize};";
                default:
                    throw new NotSupportedException($"Database type '{Type}' is not supported.");
            }
        }
    }
}