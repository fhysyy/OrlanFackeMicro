using System.Data.Common;
using System;

namespace FakeMicro.DatabaseAccess;

/// <summary>
/// MariaDB数据库连接管理器
/// </summary>
public class MariaDbConnectionManager : BaseDatabaseConnectionManager
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="config">数据库配置</param>
    public MariaDbConnectionManager(DatabaseConfig config) : base(config)
    {
        if (config.Type != DatabaseType.MariaDB)
        {
            throw new ArgumentException("数据库配置类型必须是MariaDB");
        }
        
        // 如果端口未设置，使用默认端口
        if (config.Port <= 0)
        {
            config.Port = DatabaseConfig.GetDefaultPort(DatabaseType.MariaDB);
        }
    }
    
    /// <summary>
    /// 创建MariaDB数据库连接
    /// </summary>
    /// <returns>MariaDB数据库连接对象</returns>
    public override DbConnection CreateConnection()
    {
        // 注意：在实际应用中，这里应该返回MariaDB连接对象
        // 但由于我们已经在csproj文件中注释掉了MySQL/MariaDB驱动程序依赖
        throw new NotImplementedException("MariaDB连接创建功能需要MySql.Data包");
    }
    

    
    protected override string BuildConnectionStringInternal(DatabaseConfig config)
    {
        // 注意：在实际应用中，这里应该构建MariaDB连接字符串
        // 但由于我们已经在csproj文件中注释掉了MariaDB驱动程序依赖
        throw new NotImplementedException("MariaDB连接字符串构建功能需要MySql.Data包");
    }
}