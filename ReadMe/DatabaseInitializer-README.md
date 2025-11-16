# DatabaseInitializerHostedService 使用指南

## 概述

`DatabaseInitializerHostedService` 是一个基于 Orleans 框架最佳实践设计的数据库初始化托管服务，使用 SqlSugar 作为 ORM 框架。该服务在应用程序启动时自动执行数据库初始化操作，包括表结构创建、种子数据初始化和索引创建。

## 特性

- ✅ **自动表结构创建**：基于实体类自动创建数据库表
- ✅ **种子数据初始化**：自动创建系统必需的基础数据
- ✅ **索引优化**：自动创建性能优化索引
- ✅ **Orleans 集成**：支持 Orleans 框架的持久化表
- ✅ **错误处理**：完善的错误处理和日志记录
- ✅ **配置灵活**：支持多种配置方式
- ✅ **数据库兼容**：支持 PostgreSQL、MySQL、SQL Server

## 快速开始

### 1. 配置文件设置

在 `appsettings.json` 中添加以下配置：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=fakemicro;Username=postgres;Password=your_password"
  },
  "SqlSugar": {
    "DbType": "PostgreSQL",
    "ConnectionString": "Host=localhost;Port=5432;Database=fakemicro;Username=postgres;Password=your_password",
    "EnableSqlLog": true,
    "SlowQueryThreshold": 2000,
    "EnableAop": true,
    "ConnectionPoolSize": 50,
    "ConnectionTimeout": 30
  },
  "DatabaseInitializer": {
    "EnableSeedData": true,
    "EnableIndexCreation": true,
    "RecreateTablesInDevelopment": false,
    "MigrationVersion": "1.0.0"
  }
}
```

### 2. 服务注册

在 Orleans Silo 的 `Program.cs` 中注册服务：

```csharp
using FakeMicro.DatabaseAccess.Extensions;

// 在 ConfigureServices 中添加
services.AddDatabaseInitializer(context.Configuration);
```

### 3. 完整示例

```csharp
public static async Task Main(string[] args)
{
    var hostBuilder = Host.CreateDefaultBuilder(args);
    
    hostBuilder.ConfigureServices((context, services) =>
    {
        // 添加配置服务
        services.AddConfigurationServices(context.Configuration);
        
        // 添加数据库服务
        services.AddDatabaseServices(context.Configuration);
        
        // 添加数据库初始化服务
        services.AddDatabaseInitializer(context.Configuration);
        
        // 其他服务注册...
    });
    
    // 配置 Orleans
    hostBuilder.UseOrleans((context, siloBuilder) =>
    {
        // Orleans 配置...
    });
    
    var host = hostBuilder.Build();
    await host.StartAsync();
    
    Console.WriteLine("应用程序启动成功！");
    await Task.Delay(-1);
}
```

## 配置选项

### DatabaseInitializerOptions

| 属性 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| `EnableSeedData` | bool | true | 是否启用种子数据初始化 |
| `EnableIndexCreation` | bool | true | 是否启用索引创建 |
| `RecreateTablesInDevelopment` | bool | false | 是否在开发环境重新创建表 |
| `MigrationVersion` | string | "1.0.0" | 数据库迁移版本 |

### SqlSugarOptions

| 属性 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| `DbType` | DatabaseType | PostgreSQL | 数据库类型 |
| `ConnectionString` | string | - | 数据库连接字符串 |
| `EnableSqlLog` | bool | false | 是否启用 SQL 日志 |
| `SlowQueryThreshold` | int | 2000 | 慢查询阈值（毫秒） |
| `EnableAop` | bool | true | 是否启用 AOP |
| `ConnectionPoolSize` | int | 50 | 连接池大小 |
| `ConnectionTimeout` | int | 30 | 连接超时时间（秒） |

## 自动初始化内容

### 1. 数据库表结构

服务会自动扫描所有标记了 `[SugarTable]` 特性的实体类并创建对应的数据库表：

- `users` - 用户表
- `roles` - 角色表
- `messages` - 消息表
- `message_templates` - 消息模板表
- `DictionaryTypes` - 字典类型表
- `DictionaryItems` - 字典项表

### 2. Orleans 框架表

自动创建 Orleans 框架所需的持久化表：

- `OrleansReminders` - 提醒表
- `OrleansState` - 状态表
- `OrleansQuery` - 查询表

### 3. 种子数据

自动初始化系统基础数据：

#### 字典类型
- `USER_STATUS` - 用户状态
- `MESSAGE_TYPE` - 消息类型

#### 角色
- `SUPER_ADMIN` - 超级管理员
- `ADMIN` - 管理员  
- `USER` - 普通用户

#### 管理员用户
- 用户名：`admin`
- 密码：`admin123`
- 角色：`SUPER_ADMIN`

### 4. 性能索引

自动创建性能优化索引：

#### 用户表索引
- `idx_users_username` - 用户名索引
- `idx_users_email` - 邮箱索引
- `idx_users_tenant_id` - 租户ID索引
- `idx_users_is_deleted` - 软删除索引

#### 消息表索引
- `idx_messages_sender_id` - 发送者ID索引
- `idx_messages_receiver_id` - 接收者ID索引
- `idx_messages_status` - 状态索引
- `idx_messages_created_at` - 创建时间索引

## 高级用法

### 自定义配置

```csharp
// 使用自定义配置
services.AddDatabaseInitializer(options =>
{
    options.EnableSeedData = false;  // 禁用种子数据
    options.EnableIndexCreation = true;
    options.MigrationVersion = "2.0.0";
});
```

### 条件性初始化

```csharp
// 仅在开发环境启用
if (context.HostingEnvironment.IsDevelopment())
{
    services.AddDatabaseInitializer(context.Configuration, options =>
    {
        options.RecreateTablesInDevelopment = true;
    });
}
```

## 日志记录

服务提供详细的日志记录，可通过配置控制日志级别：

```json
{
  "Logging": {
    "LogLevel": {
      "FakeMicro.DatabaseAccess.Services.DatabaseInitializerHostedService": "Information"
    }
  }
}
```

日志级别：
- `Information` - 正常操作日志
- `Warning` - 警告信息
- `Error` - 错误信息
- `Debug` - 详细调试信息

## 错误处理

服务包含完善的错误处理机制：

1. **数据库连接错误** - 在启动时检测并报告连接问题
2. **表创建错误** - 详细记录表创建过程中的错误
3. **种子数据错误** - 单个种子数据失败不影响其他数据初始化
4. **索引创建错误** - 记录索引创建失败但不中断服务

## 最佳实践

### 1. 生产环境配置

```json
{
  "DatabaseInitializer": {
    "EnableSeedData": false,
    "EnableIndexCreation": true,
    "RecreateTablesInDevelopment": false
  }
}
```

### 2. 开发环境配置

```json
{
  "DatabaseInitializer": {
    "EnableSeedData": true,
    "EnableIndexCreation": true,
    "RecreateTablesInDevelopment": true
  }
}
```

### 3. 数据库权限

确保数据库用户具有以下权限：
- `CREATE TABLE` - 创建表
- `CREATE INDEX` - 创建索引
- `INSERT` - 插入数据
- `SELECT` - 查询数据
- `ALTER TABLE` - 修改表结构

## 故障排除

### 常见问题

1. **连接字符串错误**
   ```
   错误: 数据库连接测试失败
   解决: 检查 appsettings.json 中的连接字符串配置
   ```

2. **权限不足**
   ```
   错误: 权限被拒绝
   解决: 确保数据库用户具有足够的权限
   ```

3. **表已存在**
   ```
   警告: 表已存在，跳过创建
   说明: 正常行为，服务会自动跳过已存在的表
   ```

### 调试模式

启用详细日志进行调试：

```json
{
  "Logging": {
    "LogLevel": {
      "FakeMicro.DatabaseAccess.Services.DatabaseInitializerHostedService": "Debug",
      "SqlSugar": "Debug"
    }
  }
}
```

## 版本历史

- **v1.0.0** - 初始版本，支持基本的数据库初始化功能
- 支持 PostgreSQL、MySQL、SQL Server
- 自动种子数据初始化
- 性能索引创建
- Orleans 框架集成

## 依赖项

- `Microsoft.Extensions.Hosting`
- `Microsoft.Extensions.Logging`
- `Microsoft.Extensions.Options`
- `SqlSugar`
- `FakeMicro.Entities`
- `FakeMicro.DatabaseAccess`

## 许可证

本项目遵循 MIT 许可证。