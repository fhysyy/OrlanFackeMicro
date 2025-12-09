# GetByConditionAsync方法入参样例

下面是针对MongoRepository.cs中GetByConditionAsync方法三个重载版本的具体入参示例：

## 1. 基本重载版本（仅查询条件）
```csharp
// 导入必要的命名空间
using FakeMicro.Entities;
using System.Linq.Expressions;
using System.Threading;

// 创建MongoRepository实例（假设已注入依赖）
var userRepository = new MongoRepository<User, long>(db, logger);

// 定义查询条件：查找用户名包含"admin"的用户
Expression<Func<User, bool>> predicate = u => u.username.Contains("admin");

// 创建取消令牌（可选）
var cancellationToken = new CancellationToken();

// 调用方法
var adminUsers = await userRepository.GetByConditionAsync(predicate, cancellationToken);
```

## 2. 指定数据库重载版本
```csharp
// 导入必要的命名空间
using FakeMicro.Entities;
using System.Linq.Expressions;
using System.Threading;

// 创建MongoRepository实例（假设已注入依赖）
var userRepository = new MongoRepository<User, long>(db, logger);

// 定义查询条件：查找邮箱以"@example.com"结尾的用户
Expression<Func<User, bool>> predicate = u => u.email.EndsWith("@example.com");

// 指定数据库名称
string? databaseName = "FakeMicroDB_Production";

// 创建取消令牌（可选）
var cancellationToken = new CancellationToken();

// 调用方法
var exampleUsers = await userRepository.GetByConditionAsync(predicate, databaseName, cancellationToken);
```

## 3. 指定数据库和集合重载版本
```csharp
// 导入必要的命名空间
using FakeMicro.Entities;
using System.Linq.Expressions;
using System.Threading;

// 创建MongoRepository实例（假设已注入依赖）
var userRepository = new MongoRepository<User, long>(db, logger);

// 定义查询条件：查找手机号不为空且显示名称包含"张"的用户
Expression<Func<User, bool>> predicate = u => u.phone != null && u.display_name.Contains("张");

// 指定数据库名称
string? databaseName = "FakeMicroDB_Production";

// 指定集合名称（默认使用实体类名"User"，这里自定义）
string? collectionName = "system_users";

// 创建取消令牌（可选）
var cancellationToken = new CancellationToken();

// 调用方法
var chineseUsersWithPhone = await userRepository.GetByConditionAsync(predicate, databaseName, collectionName, cancellationToken);
```

# 入参说明

1. **predicate**：Lambda表达式，用于指定查询条件
   - 必须返回bool类型
   - 可以使用实体的任何属性进行条件组合
   - 支持常见的LINQ操作符（Contains, StartsWith, EndsWith, ==, !=, >, <等）

2. **databaseName**（可选）：
   - 字符串类型，可以为null
   - 指定要查询的数据库名称
   - 不提供时使用默认数据库配置

3. **collectionName**（可选）：
   - 字符串类型，可以为null
   - 指定要查询的集合名称
   - 不提供时使用实体类名作为集合名

4. **cancellationToken**（可选）：
   - CancellationToken类型
   - 用于取消异步操作
   - 不提供时使用默认值

这些示例展示了GetByConditionAsync方法的灵活性，可以根据实际需求选择合适的重载版本。