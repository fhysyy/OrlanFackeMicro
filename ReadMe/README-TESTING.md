# FakeMicro 测试指南

## 📋 测试概述

FakeMicro 项目包含完整的测试套件，涵盖单元测试、集成测试和API测试。

## 🏗️ 测试架构

### 测试项目结构
```
src/FakeMicro.Tests/
├── UnitTests/           # 单元测试
│   ├── UserGrainTests.cs
│   └── MessageGrainTests.cs
├── IntegrationTests/    # 集成测试
│   └── DatabaseIntegrationTests.cs
├── ApiTests/           # API测试
│   └── AuthApiTests.cs
├── TestHelpers/        # 测试辅助工具
│   ├── TestDataGenerator.cs
│   └── TestExtensions.cs
└── xunit.runner.json   # 测试配置
```

## 🚀 快速开始

### 运行所有测试
```bash
# Windows
scripts\run-tests.bat

# Linux/Mac
./scripts/run-tests.sh
```

### 运行特定测试类型
```bash
# 仅运行单元测试
dotnet test --filter "Category=Unit"

# 仅运行集成测试
dotnet test --filter "Category=Integration"

# 仅运行API测试
dotnet test --filter "Category=Api"
```

## 📊 测试类型

### 1. 单元测试 (Unit Tests)
- **位置**: `UnitTests/`
- **特点**: 隔离测试，使用Mock对象
- **依赖**: 无外部依赖
- **运行速度**: 快速

**测试内容**:
- Grain业务逻辑
- 数据验证
- 异常处理

### 2. 集成测试 (Integration Tests)
- **位置**: `IntegrationTests/`
- **特点**: 测试真实数据库交互
- **依赖**: Docker + PostgreSQL
- **运行速度**: 中等

**测试内容**:
- 数据库CRUD操作
- 仓储层集成
- 数据一致性

### 3. API测试 (API Tests)
- **位置**: `ApiTests/`
- **特点**: 测试控制器和API端点
- **依赖**: Mock仓储层
- **运行速度**: 快速

**测试内容**:
- 认证授权
- API响应格式
- 错误处理

## 🔧 测试配置

### xUnit 配置 (`xunit.runner.json`)
```json
{
  "parallelizeTestCollections": true,
  "maxParallelThreads": 4,
  "diagnosticMessages": true,
  "shadowCopy": false
}
```

### 测试数据生成
使用 `TestDataGenerator` 类生成测试数据：
```csharp
var user = TestDataGenerator.CreateTestUser(1, "testuser");
var message = TestDataGenerator.CreateTestMessage(1, 1, 2);
```

## 🐳 集成测试环境

### Docker 要求
集成测试需要运行 PostgreSQL 容器：
```bash
# 启动测试数据库容器
docker run -d --name fakemicro-test-db \
  -e POSTGRES_DB=fakemicro_test \
  -e POSTGRES_USER=testuser \
  -e POSTGRES_PASSWORD=testpassword \
  -p 5432:5432 postgres:15
```

### 测试容器配置
集成测试使用 Testcontainers 自动管理容器：
```csharp
private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder()
    .WithImage("postgres:15")
    .WithDatabase("fakemicro_test")
    .Build();
```

## 📈 测试覆盖率

### 生成覆盖率报告
```bash
# 安装覆盖率工具
dotnet tool install -g coverlet.console

# 生成覆盖率报告
coverlet src/FakeMicro.Tests/bin/Debug/net8.0/FakeMicro.Tests.dll \
  --target "dotnet" \
  --targetargs "test src/FakeMicro.Tests --no-build" \
  --format opencover \
  --output ./coverage.xml
```

## 🔍 调试测试

### Visual Studio
1. 打开测试资源管理器
2. 右键点击测试方法
3. 选择"调试测试"

### 命令行调试
```bash
dotnet test --debug
```

## 🛠️ 测试最佳实践

### 1. 命名约定
- 测试类: `[被测类名]Tests`
- 测试方法: `[场景]_[预期结果]`

### 2. 测试结构 (AAA模式)
```csharp
[Fact]
public void Method_ShouldReturnResult_WhenCondition()
{
    // Arrange - 设置测试环境
    var input = "test";
    
    // Act - 执行被测方法
    var result = MethodUnderTest(input);
    
    // Assert - 验证结果
    Assert.Equal(expected, result);
}
```

### 3. 测试隔离
- 每个测试独立运行
- 使用 `IAsyncLifetime` 管理测试生命周期
- 清理测试数据

## 🚨 常见问题

### Q: 集成测试失败怎么办？
A: 检查Docker是否运行，确保5432端口未被占用。

### Q: Mock对象设置失败？
A: 确保Mock设置与实际调用匹配，检查参数类型。

### Q: 测试运行缓慢？
A: 分离单元测试和集成测试，并行运行单元测试。

## 📚 相关文档

- [xUnit 文档](https://xunit.net/)
- [Moq 文档](https://github.com/moq/moq)
- [Testcontainers 文档](https://testcontainers.com/)

---

**注意**: 运行集成测试前请确保Docker已安装并运行。