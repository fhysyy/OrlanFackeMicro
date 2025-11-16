# FakeMicro 数据库设置指南

本文档提供了在PostgreSQL数据库中设置FakeMicro项目所需的步骤。

## 前提条件

- 已安装PostgreSQL数据库（建议版本12或更高）
- 已创建一个名为`fakemicro`的数据库（或其他您希望使用的名称）
- 已安装`psql`命令行工具或其他PostgreSQL客户端

## 执行步骤

### 方法1：使用psql命令行工具

1. 打开命令行终端
2. 执行以下命令连接到PostgreSQL并执行脚本：

```bash
# 连接到PostgreSQL并指定数据库名称
sudo -u postgres psql -d fakemicro -f create_tables_postgresql.sql
```

或者（如果使用Windows）：

```cmd
psql -U postgres -d fakemicro -f create_tables_postgresql.sql
```

然后输入您的PostgreSQL密码。

### 方法2：使用pgAdmin图形界面

1. 打开pgAdmin并连接到您的PostgreSQL服务器
2. 选择您的数据库（例如`fakemicro`）
3. 右键点击数据库，选择"查询工具"
4. 打开`create_tables_postgresql.sql`文件
5. 点击执行按钮运行脚本

## 数据库连接配置

在运行应用程序之前，您需要配置数据库连接信息。请根据您的环境修改以下配置文件：

1. `src/FakeMicro.Silo/appsettings.json` - Silo服务的数据库配置
2. `src/FakeMicro.Api/appsettings.json` - API服务的数据库配置

配置示例：

```json
"Database": {
  "Type": "PostgreSql",
  "Host": "localhost",
  "Port": 5432,
  "Database": "fakemicro",
  "Username": "postgres",
  "Password": "your_password"
}
```

## 初始管理员账户

脚本创建了一个默认的管理员账户：

- **用户名**：admin
- **密码**：admin123456 （请在首次登录后修改此密码）
- **邮箱**：admin@example.com

## 注意事项

1. 生产环境中，请务必修改默认的管理员密码
2. 生产环境建议使用强密码策略和HTTPS连接
3. 请定期备份您的数据库
4. 脚本中的密码哈希仅用于演示，生产环境应使用安全的密码哈希方法

## 故障排除

如果在执行脚本时遇到问题，请检查：

1. PostgreSQL服务是否正在运行
2. 您是否有足够的权限创建表和枚举类型
3. 数据库是否已正确创建
4. 连接字符串中的凭据是否正确

如需进一步帮助，请参考PostgreSQL官方文档或提交issue。