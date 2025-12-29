# Orleans 项目快速启动指南

## 问题说明

当前遇到的问题是 Orleans 数据库表结构的大小写敏感性错误。PostgreSQL 在处理未加引号的标识符时会自动转换为小写，而 Orleans 的 AdoNet 提供程序期望列名是小写的。

## 解决方案

项目已经包含数据库修复工具，按照以下步骤操作：

### 方式一：使用修复工具（推荐）

#### Windows 批处理：
```bash
cd f:/Orleans/OrlanFackeMicro/src
run-db-fixer.bat
```

#### PowerShell：
```powershell
cd f:/Orleans/OrlanFackeMicro/src
.\fix-orleans-db-simple.ps1
```

修复完成后，启动项目：
```bash
start-api.bat
```

### 方式二：手动修复数据库

如果您有 PostgreSQL 命令行工具（psql），可以手动执行：

1. 执行 SQL 脚本：
```bash
psql -h localhost -U postgres -d fakemicro -f fix-orleans-tables.sql
```

2. 然后启动 API 项目：
```bash
cd FakeMicro.Api
dotnet run
```

### 方式三：让程序自动修复

FakeMicro.Api 项目启动时会自动调用 `FixOrleansDatabaseTablesAsync` 方法来修复数据库表结构。

直接运行：
```bash
cd FakeMicro.Api
dotnet run
```

## 前置条件

确保以下服务正在运行：

1. **PostgreSQL 数据库**
   - 主机: localhost
   - 端口: 5432
   - 数据库名: fakemicro
   - 用户名: postgres
   - 密码: 123456

2. **创建数据库**（如果尚未创建）
```sql
CREATE DATABASE fakemicro;
CREATE DATABASE fakemicro_cap;
CREATE DATABASE fakemicro_hangfire;
```

## 验证修复

修复成功后，应该看到以下输出：
```
✅ 数据库连接成功
✅ 删除现有 Orleans 表 成功
✅ 创建存储表 成功
✅ 创建查询表 成功
✅ 创建成员表 成功
✅ 创建成员版本表 成功
✅ 创建提醒服务表 成功
✅ 插入默认查询 成功
✅ Orleans 数据库表结构修复完成!
```

## 启动项目

### 启动 API（包含 Silo 功能）：
```bash
cd FakeMicro.Api
dotnet run
```

### 或者使用启动脚本：
```bash
start-api.bat
```

## 访问点

启动成功后：
- API 文档: http://localhost:5000/swagger
- 健康检查: http://localhost:5000/health
- Hangfire 仪表板: http://localhost:5000/hangfire

## 常见问题

### 问题1：PostgreSQL 连接失败
```
Npgsql.PostgresException: connection refused
```
**解决方案**：确保 PostgreSQL 服务正在运行，端口 5432 可用。

### 问题2：表名不存在错误
```
42703: 字段 "lastupdated" 不存在
```
**解决方案**：运行数据库修复工具删除并重新创建表。

### 问题3：端口被占用
```
Failed to bind to address http://[::]:5000
```
**解决方案**：
- 检查其他程序是否占用 5000 端口
- 或修改 launchSettings.json 中的端口号

## 技术细节

修复工具会执行以下操作：
1. 删除所有现有的 Orleans 表（orleansstorage, orleansquery, orleansmembershiptable, orleansmembershipversiontable, orleansreminderservice）
2. 使用小写列名重新创建所有表
3. 插入 Orleans 所需的默认查询
4. 验证表结构是否正确

所有列名都使用小写，以避免 PostgreSQL 的大小写敏感性问题。
