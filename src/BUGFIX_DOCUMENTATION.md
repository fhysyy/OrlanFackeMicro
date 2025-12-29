# Orleans 数据库表大小写敏感性错误修复文档

## 问题描述

错误信息：
```
42703: 字段 "lastupdated" 不存在
DETAIL: 表"orleansmembershipversiontable"中存在一列,名为"lastupdated", 但是这个表名并不能从这部分查询里引用.
```

## 根本原因分析

### 1. PostgreSQL 大小写敏感性规则

在 PostgreSQL 中，未加引号的标识符（表名、列名）会被自动转换为**小写**。例如：
```sql
CREATE TABLE MyTable (
    MyColumn INT
);
```

实际创建的是：
```sql
CREATE TABLE mytable (
    mycolumn INT
);
```

### 2. Orleans AdoNet 提供程序的行为

Orleans 的 AdoNet 提供程序在以下场景会查询表结构：
- **初始化阶段**：检查表是否存在，验证表结构
- **运行时**：执行查询语句

问题在于：
- 如果表创建时使用了 `CREATE TABLE IF NOT EXISTS`，且表已存在，则不会重建
- 旧的表可能包含带引号的列名（如 `"LastUpdated"`），而 Orleans 查询时使用小写列名（如 `lastupdated`）
- 导致列名不匹配

### 3. 具体的错误场景

1. **旧表残留**：之前的表创建可能使用了不同的命名规则
2. **表结构不匹配**：Orleans 期望的列名是全小写的，但实际的列名可能大小写不一致
3. **查询引用问题**：Orleans 的内部查询可能使用了带引号的列名，导致无法找到列

## 解决方案

### 1. 修改 Orleans 配置（OrleansConfigurationExtensions.cs）

**修改位置**：`FakeMicro.Utilities/Configuration/OrleansConfigurationExtensions.cs`

**修改内容**：
```csharp
// 在 ConfigureClustering 和 ConfigureStorage 方法中
builder.UseAdoNetClustering(options =>
{
    options.Invariant = "Npgsql";
    options.ConnectionString = connectionString;
    // 新增：确保使用 JSON 格式
    options.UseJsonFormat = true;
    options.UseXmlFormat = false;
    options.UseBinaryFormat = false;
});
```

**作用**：
- 明确指定 Orleans 使用 JSON 格式存储数据
- 避免格式混淆导致的查询问题

### 2. 修改表创建逻辑（OrleansDatabaseInitializer.cs）

**修改位置**：`FakeMicro.Silo/Services/OrleansDatabaseInitializer.cs`

**修改内容**：
```csharp
// 所有表创建方法都改为：
// 1. 先删除表（DROP TABLE IF EXISTS ... CASCADE）
// 2. 然后创建表（不带 IF NOT EXISTS）
// 3. 对于关键字表，添加列验证

private async Task CreateOrleansMembershipVersionTable(NpgsqlConnection connection)
{
    // 先删除表以确保完全重建
    const string dropSql = "DROP TABLE IF EXISTS orleansmembershipversiontable CASCADE;";
    await ExecuteSqlCommand(connection, dropSql, "删除旧的成员版本表");

    const string sql = @"
        CREATE TABLE orleansmembershipversiontable (
            deploymentid character varying(150) NOT NULL,
            lastupdated timestamp without time zone NOT NULL DEFAULT now(),
            version integer NOT NULL,
            PRIMARY KEY (deploymentid)
        );";

    await ExecuteSqlCommand(connection, sql, "OrleansMembershipVersion 表");

    // 验证列是否创建成功
    const string verifySql = @"
        SELECT column_name
        FROM information_schema.columns
        WHERE table_schema = 'public'
        AND table_name = 'orleansmembershipversiontable'
        ORDER BY ordinal_position;";

    // ... 验证逻辑
}
```

**关键改动**：
1. **所有表都先删除再创建**：确保没有旧表残留
2. **移除 IF NOT EXISTS**：强制重建表结构
3. **添加列验证**：确保列名正确创建
4. **所有列名使用小写**：符合 PostgreSQL 默认规则

### 3. 修复数据类型（orleansstorage 表）

**问题**：原代码中 `grainidhash` 使用 `integer`，但 Orleans 期望 `bigint`

**修复**：
```csharp
CREATE TABLE orleansstorage (
    grainidhash bigint NOT NULL,        // 改为 bigint
    grainidn0 bigint NOT NULL,
    grainidn1 bigint NOT NULL,
    graintypehash bigint NOT NULL,      // 改为 bigint
    // ... 其他字段
);
```

## 表结构规范

所有 Orleans 表都必须遵循以下规则：

1. **表名全小写**：如 `orleansstorage`, `orleansmembershiptable`
2. **列名全小写**：如 `deploymentid`, `lastupdated`, `version`
3. **不使用引号**：创建表时不要在列名周围加引号
4. ** CASCADE 删除**：删除表时使用 `CASCADE` 确保删除所有依赖对象

## 完整的表结构

### orleansstorage
```sql
CREATE TABLE orleansstorage (
    grainidhash bigint NOT NULL,
    grainidn0 bigint NOT NULL,
    grainidn1 bigint NOT NULL,
    graintypehash bigint NOT NULL,
    graintypestring character varying(512) NOT NULL,
    grainidextensionstring character varying(512),
    serviceid character varying(150) NOT NULL,
    payloadbinary bytea,
    payloadjson text,
    payloadxml text,
    etag character varying(50) NOT NULL,
    version integer NOT NULL,
    modifiedon timestamp without time zone NOT NULL DEFAULT now(),
    createdon timestamp without time zone NOT NULL DEFAULT now(),
    PRIMARY KEY (grainidhash, grainidn0, grainidn1, graintypehash)
);
```

### orleansmembershiptable
```sql
CREATE TABLE orleansmembershiptable (
    deploymentid character varying(150) NOT NULL,
    address character varying(45) NOT NULL,
    port integer NOT NULL,
    generation integer NOT NULL,
    siloname character varying(150) NOT NULL,
    hostname character varying(150) NOT NULL,
    status integer NOT NULL,
    proxyport integer,
    suspecttimes character varying(8000),
    starttime timestamp without time zone NOT NULL,
    iamalivetime timestamp without time zone NOT NULL,
    PRIMARY KEY (deploymentid, address, port, generation)
);
```

### orleansmembershipversiontable（关键修复表）
```sql
CREATE TABLE orleansmembershipversiontable (
    deploymentid character varying(150) NOT NULL,
    lastupdated timestamp without time zone NOT NULL DEFAULT now(),
    version integer NOT NULL,
    PRIMARY KEY (deploymentid)
);
```

### orleansquery
```sql
CREATE TABLE orleansquery (
    querykey character varying(512) NOT NULL,
    querytext text NOT NULL,
    createdon timestamp without time zone NOT NULL DEFAULT now(),
    modifiedon timestamp without time zone NOT NULL DEFAULT now(),
    PRIMARY KEY (querykey)
);
```

### orleansreminderservice
```sql
CREATE TABLE orleansreminderservice (
    serviceid character varying(150) NOT NULL,
    grainid character varying(150) NOT NULL,
    remintername character varying(150) NOT NULL,
    starttime timestamp without time zone NOT NULL,
    period bigint NOT NULL,
    PRIMARY KEY (serviceid, grainid, remintername)
);
```

## 验证步骤

### 1. 启动 Silo 项目

```bash
cd FakeMicro.Silo
dotnet run
```

### 2. 检查日志输出

预期输出：
```
正在初始化 Orleans 数据库表结构...
删除现有 Orleans 表
  ✅ 删除现有Orleans表 成功
  ✅ OrleansStorage 表 成功
  ✅ OrleansQuery 表 成功
  ✅ OrleansMembership 表 成功
  ✅ OrleansMembershipVersion 表 成功
  ✅ orleansmembershipversiontable 表结构验证成功
  ✅ OrleansReminder 表 成功
  ✅ 默认查询 成功
✅ Orleans 数据库表结构初始化完成
Orleans Silo运行中
```

### 3. 验证表结构

在 PostgreSQL 中执行：
```sql
SELECT table_name, column_name, data_type
FROM information_schema.columns
WHERE table_schema = 'public'
AND table_name LIKE 'orleans%'
ORDER BY table_name, ordinal_position;
```

确保：
- 所有表名和列名都是小写
- 数据类型正确（特别是 bigint vs integer）
- `orleansmembershipversiontable` 包含 `lastupdated` 列

## 常见错误排查

### 错误1：42703: 字段 "lastupdated" 不存在

**原因**：列名大小写不匹配

**解决方案**：
1. 检查实际列名：
   ```sql
   SELECT column_name FROM information_schema.columns
   WHERE table_name = 'orleansmembershipversiontable';
   ```
2. 如果列名不是小写，重新创建表

### 错误2：表已存在但结构不正确

**原因**：使用了 `CREATE TABLE IF NOT EXISTS` 导致旧表未被重建

**解决方案**：先 `DROP TABLE` 再 `CREATE TABLE`

### 错误3：查询超时或连接失败

**原因**：连接字符串配置不正确

**解决方案**：确保连接字符串包含：
```
Host=localhost;Port=5432;Database=fakemicro;Username=postgres;Password=123456;SearchPath=public;
```

## 最佳实践

1. **开发环境**：使用 `UseLocalhostClustering = true`，避免依赖外部数据库
2. **生产环境**：使用 PostgreSQL 集群存储，确保高可用
3. **表结构管理**：定期检查表结构是否与 Orleans 期望的一致
4. **日志监控**：关注 Orleans 启动时的表初始化日志
5. **备份策略**：在重建表前备份重要数据

## 相关文件

- `FakeMicro.Utilities/Configuration/OrleansConfigurationExtensions.cs`：Orleans 配置
- `FakeMicro.Silo/Services/OrleansDatabaseInitializer.cs`：数据库初始化服务
- `FakeMicro.Api/Program.cs`：API 项目的数据库修复逻辑
- `FakeMicro.Silo/Program.cs`：Silo 项目的启动逻辑

## 参考资料

- Orleans 文档：https://docs.microsoft.com/en-us/dotnet/orleans/
- PostgreSQL 大小写敏感性：https://www.postgresql.org/docs/current/sql-syntax-lexical.html#SQL-SYNTAX-IDENTIFIERS
- Npgsql 文档：https://www.npgsql.org/doc/
