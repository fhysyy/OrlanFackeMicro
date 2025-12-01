namespace FakeMicro.DatabaseAccess
{
    /// <summary>
    /// 数据库类型枚举
    /// </summary>
    public enum DatabaseType
    {
        /// <summary>
        /// MySQL数据库
        /// </summary>
        MySQL,
        
        /// <summary>
        /// PostgreSQL数据库
        /// </summary>
        PostgreSQL,
        
        /// <summary>
        /// MariaDB数据库
        /// </summary>
        MariaDB,
        
        /// <summary>
        /// SQL Server数据库
        /// </summary>
        SQLServer,
        /// <summary>
        /// MongoDB数据库
        /// </summary>
        MongoDB,

        /// <summary>
        /// SQLite数据库
        /// </summary>
        SQLite
    }
}