namespace FakeMicro.DatabaseAccess
{
    /// <summary>
    /// 连接字符串选项 - 强类型配置类
    /// </summary>
    public class ConnectionStringsOptions
    {
        /// <summary>
        /// 默认连接字符串
        /// </summary>
        public string? DefaultConnection { get; set; }
        
        /// <summary>
        /// MongoDB连接字符串
        /// </summary>
        public string? MongoDBConnection { get; set; }
        
        /// <summary>
        /// Hangfire连接字符串
        /// </summary>
        public string? HangfireConnection { get; set; }
        
        /// <summary>
        /// Orleans连接字符串
        /// </summary>
        public string? OrleansConnection { get; set; }
        
        /// <summary>
        /// CAP事件总线连接字符串
        /// </summary>
        public string? CAP { get; set; }
    }
}