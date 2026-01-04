# 测试项目文档

## 概述

本项目包含完整的测试套件，用于验证 Orleans 应用程序的功能正确性和稳定性。测试分为三个层级：

- **单元测试**：测试单个组件的逻辑，使用 Mock 对象隔离依赖
- **集成测试**：测试组件之间的交互，使用 Orleans.TestingHost 模拟集群环境
- **API 测试**：测试完整的 HTTP API 端点，验证端到端功能

## 项目结构

```
FakeMicro.Tests/
├── UnitTests/              # 单元测试
│   ├── UserGrainTests.cs
│   └── MessageGrainTests.cs
├── IntegrationTests/       # 集成测试
│   ├── TestClusterConfigurator.cs
│   ├── UserGrainIntegrationTests.cs
│   └── MessageGrainIntegrationTests.cs
├── ApiTests/               # API 测试
│   ├── TestWebApplicationFactory.cs
│   ├── UserControllerTests.cs
│   └── MessagesControllerTests.cs
├── TestHelpers/            # 测试辅助工具
│   ├── TestDataGenerator.cs
│   ├── TestExtensions.cs
│   ├── GrainTestBase.cs
│   └── TestBase.cs
├── xunit.runner.json       # xUnit 配置文件
└── FakeMicro.Tests.csproj  # 项目文件
```

## 快速开始

### 运行所有测试

使用 PowerShell 脚本：

```powershell
.\Run-Tests.ps1
```

使用批处理文件：

```cmd
Run-Tests.bat
```

### 运行特定类型的测试

#### 运行单元测试

```powershell
.\Run-Tests.ps1 -TestType unit
```

```cmd
Run-Tests.bat unit
```

#### 运行集成测试

```powershell
.\Run-Tests.ps1 -TestType integration
```

```cmd
Run-Tests.bat integration
```

#### 运行 API 测试

```powershell
.\Run-Tests.ps1 -TestType api
```

```cmd
Run-Tests.bat api
```

### 使用过滤器运行特定测试

```powershell
.\Run-Tests.ps1 -Filter "FullyQualifiedName~UserGrainTests"
```

### 详细输出模式

```powershell
.\Run-Tests.ps1 -Verbose
```

### 跳过构建步骤

```powershell
.\Run-Tests.ps1 -NoBuild
```

## 测试类型详解

### 单元测试

单元测试专注于测试单个类或方法的功能，使用 Mock 对象来隔离外部依赖。

**特点：**
- 执行速度快
- 不依赖外部系统（数据库、网络等）
- 使用 Moq 框架创建 Mock 对象
- 继承自 `GrainTestBase` 基类

**示例：**

```csharp
[Fact]
public async Task UserGrain_SetAndGetNickname_Success()
{
    var grain = new UserGrain(
        LoggerMock.Object,
        GrainFactoryMock.Object,
        GrainIdentityMock.Object
    );

    await grain.SetNicknameAsync("Test Nickname");
    var nickname = await grain.GetNicknameAsync();

    Assert.Equal("Test Nickname", nickname);
}
```

### 集成测试

集成测试测试组件之间的交互，使用 Orleans.TestingHost 创建模拟的集群环境。

**特点：**
- 测试真实的 Orleans 集群行为
- 使用内存存储（不依赖真实数据库）
- 测试 Grain 之间的交互
- 继承自 `TestClusterFixture` 基类

**示例：**

```csharp
[Fact]
public async Task UserGrain_SetAndGetNickname_Success()
{
    var grain = _fixture.Cluster.GrainFactory.GetGrain<IUserGrain>("test-user-1");

    await grain.SetNicknameAsync("Test Nickname");
    var nickname = await grain.GetNicknameAsync();

    Assert.Equal("Test Nickname", nickname);
}
```

### API 测试

API 测试测试完整的 HTTP API 端点，验证从 HTTP 请求到 Grain 调用的完整流程。

**特点：**
- 测试完整的 HTTP 请求/响应
- 使用 WebApplicationFactory 创建测试主机
- 集成 Orleans 测试集群
- 测试认证、授权和错误处理

**示例：**

```csharp
[Fact]
public async Task GetUser_ReturnsUser_WhenUserExists()
{
    var userId = "test-user-1";
    var clusterClient = _factory.Services.GetRequiredService<IClusterClient>();
    var userGrain = clusterClient.GetGrain<IUserGrain>(userId);

    await userGrain.SetNicknameAsync("Test User");

    var response = await _client.GetAsync($"/api/User/{userId}");

    response.EnsureSuccessStatusCode();
    var user = await response.Content.ReadFromJsonAsync<UserDto>();

    Assert.NotNull(user);
    Assert.Equal(userId, user.Id.ToString());
}
```

## 测试辅助工具

### TestDataGenerator

提供测试数据生成方法：

```csharp
var user = TestDataGenerator.CreateTestUser(id: 1, username: "testuser");
var message = TestDataGenerator.CreateTestMessage(id: 1, senderId: 1, receiverId: 2);
```

### TestExtensions

提供扩展方法用于测试：

```csharp
var loggerMock = TestExtensions.CreateLoggerMock<UserGrain>();
var grainFactoryMock = TestExtensions.CreateGrainFactoryMock();
loggerMock.VerifyLog(LogLevel.Information, "Expected message", Times.Once());
```

### GrainTestBase

单元测试的基类，提供通用的 Mock 设置：

```csharp
public class UserGrainTests : GrainTestBase
{
    protected Mock<ILogger> LoggerMock => base.LoggerMock;
    protected Mock<IGrainFactory> GrainFactoryMock => base.GrainFactoryMock;
}
```

### TestBase

集成测试的基类，提供测试集群设置：

```csharp
public class UserGrainIntegrationTests : IClassFixture<TestClusterFixture>
{
    private readonly TestClusterFixture _fixture;

    public UserGrainIntegrationTests(TestClusterFixture fixture)
    {
        _fixture = fixture;
    }
}
```

## 配置

### xUnit 配置

xUnit 配置文件位于 `xunit.runner.json`：

```json
{
  "maxParallelThreads": 8,
  "parallelizeAssembly": true,
  "parallelizeTestCollections": true,
  "diagnosticMessages": true,
  "longRunningTestSeconds": 300
}
```

### 测试集群配置

测试集群配置位于 `TestClusterConfigurator.cs`：

```csharp
public class TestClusterConfigurator : ISiloConfigurator, IClientBuilderConfigurator
{
    public void Configure(ISiloBuilder siloBuilder)
    {
        siloBuilder.AddMemoryGrainStorageAsDefault();
        siloBuilder.Configure<ClusterOptions>(options =>
        {
            options.ClusterId = "TestCluster";
            options.ServiceId = "TestService";
        });
    }
}
```

## CI/CD 集成

项目包含 GitHub Actions 工作流配置，自动运行测试：

```yaml
name: Run Tests

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main, develop ]

jobs:
  test:
    runs-on: windows-latest
    steps:
      - name: Checkout code
        uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
      - name: Run tests
        run: dotnet test ./FakeMicro.Tests/FakeMicro.Tests.csproj
```

## 测试报告

测试运行后会生成以下报告：

- **TestResults.trx**：Visual Studio 格式的测试报告
- **TestResults.html**：HTML 格式的测试报告

报告文件位于测试项目的 `TestResults` 目录中。

## 最佳实践

1. **编写可读的测试**：使用描述性的测试名称，遵循 AAA 模式（Arrange-Act-Assert）

2. **保持测试独立**：每个测试应该独立运行，不依赖其他测试的状态

3. **使用适当的测试类型**：
   - 单元测试用于快速验证逻辑
   - 集成测试用于验证组件交互
   - API 测试用于验证端到端功能

4. **Mock 外部依赖**：在单元测试中使用 Mock 对象隔离数据库、网络等外部依赖

5. **测试边界情况**：除了正常流程，还要测试错误处理和边界条件

6. **定期运行测试**：在提交代码前运行测试，确保不会引入回归

## 故障排除

### 测试失败

如果测试失败，检查以下内容：

1. 查看详细的错误信息（使用 `-Verbose` 参数）
2. 检查测试报告文件
3. 确保所有依赖项已正确安装
4. 验证测试数据是否正确

### 构建失败

如果构建失败，检查以下内容：

1. 确保 .NET SDK 版本正确（需要 .NET 9.0）
2. 检查项目依赖项
3. 验证代码编译错误

### 性能问题

如果测试运行缓慢：

1. 检查测试是否正确使用 Mock 对象
2. 减少不必要的数据库访问
3. 使用 `--no-build` 参数跳过构建步骤

## 依赖项

测试项目依赖以下 NuGet 包：

- **xunit**：测试框架
- **xunit.runner.visualstudio**：Visual Studio 测试运行器
- **Microsoft.NET.Test.Sdk**：.NET 测试 SDK
- **Moq**：Mock 框架
- **FluentAssertions**：断言库
- **Microsoft.AspNetCore.Mvc.Testing**：API 测试支持
- **Orleans.TestingHost**：Orleans 测试集群
- **coverlet.collector**：代码覆盖率收集器

## 贡献指南

添加新测试时：

1. 确定测试类型（单元/集成/API）
2. 在相应的目录中创建测试文件
3. 遵循现有的命名约定和代码风格
4. 使用适当的基类和辅助工具
5. 编写描述性的测试名称
6. 运行所有测试确保没有回归
7. 更新文档（如果添加了新的测试类别）

## 联系方式

如有问题或建议，请联系项目维护者。
