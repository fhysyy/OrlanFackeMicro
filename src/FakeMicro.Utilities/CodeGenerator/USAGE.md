# Orleans SqlSugar 代码生成器使用指南

## 🚀 快速开始

### 1. 项目集成

代码生成器已集成到 `FakeMicro.Utilities` 项目中，通过依赖注入自动注册。

```csharp
// 在 Program.cs 或 Startup.cs 中
services.AddCodeGenerator(configuration);
```

### 2. 基本使用

#### 程序化调用

```csharp
// 注入代码生成器
public class MyService
{
    private readonly CodeGenerator _codeGenerator;
    
    public MyService(CodeGenerator codeGenerator)
    {
        _codeGenerator = codeGenerator;
    }
    
    public async Task GenerateProductCode()
    {
        var result = await _codeGenerator.GenerateCodeAsync(
            "Product", 
            GenerationType.All
        );
        
        if (result.IsSuccess)
        {
            Console.WriteLine($"生成了 {result.GeneratedFiles.Count} 个文件");
        }
        else
        {
            Console.WriteLine($"生成失败: {result.ErrorMessage}");
        }
    }
}
```

#### 命令行使用

```bash
# 生成所有类型的代码
dotnet run --project CodeGeneratorDemo.csproj generate Product

# 生成特定类型
dotnet run --project CodeGeneratorDemo.csproj generate User Interface Grain

# 列出所有可用实体
dotnet run --project CodeGeneratorDemo.csproj list

# 预览生成的代码
dotnet run --project CodeGeneratorDemo.csproj preview Product
```

## 📋 支持的生成类型

| 类型 | 说明 | 生成位置 | 功能 |
|------|------|----------|------|
| `Interface` | Orleans Grain 接口 | `FakeMicro.Interfaces` | 定义Grain操作契约 |
| `Grain` | Orleans Grain 实现 | `FakeMicro.Grains` | 实现业务逻辑 |
| `Dto` | 数据传输对象 | `FakeMicro.Entities` | API数据模型 |
| `Controller` | Web API 控制器 | `FakeMicro.Api` | HTTP接口层 |
| `All` | 所有类型 | 上述所有位置 | 完整CRUD功能 |

## 🔧 配置选项

### appsettings.codegen.json

```json
{
  "BaseNamespace": "FakeMicro",
  "AuthorName": "代码生成器",
  "UseUtcTime": true,
  "OverwriteExisting": false,
  "GenerateServiceClasses": true,
  "OutputDirectories": {
    "Interface": "src/FakeMicro.Interfaces",
    "Grain": "src/FakeMicro.Grains", 
    "Dto": "src/FakeMicro.Entities",
    "Controller": "src/FakeMicro.Api"
  },
  "EntityNamespace": "FakeMicro.Entities",
  "IncludeNamespaceMappings": {
    "System": "global::System",
    "System.Collections.Generic": "global::System.Collections.Generic"
  },
  "GeneratedFileHeader": {
    "IncludeTimestamp": true,
    "IncludeAuthor": true,
    "IncludeGeneratorInfo": true,
    "CustomHeader": "// 此文件由代码生成器自动生成\n// 请勿手动修改"
  }
}
```

### 编程式配置

```csharp
var config = new CodeGeneratorConfiguration
{
    BaseNamespace = "MyCompany.MyApp",
    AuthorName = "开发团队",
    UseUtcTime = true,
    OverwriteExisting = true,
    OutputDirectories = new Dictionary<string, string>
    {
        ["Interface"] = @"C:\MyProject\Interfaces",
        ["Grain"] = @"C:\MyProject\Grains"
    }
};

var generator = new CodeGenerator(config);
```

## 📝 实体设计规范

为了获得最佳的代码生成效果，实体类应遵循以下规范：

### 基本要求

```csharp
using SqlSugar;
using Orleans.Concurrency;
using System.ComponentModel.DataAnnotations;

namespace FakeMicro.Entities
{
    [SugarTable("Products")]
    [GenerateSerializer]
    public class Product : BaseEntity
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        [GenerateSerializerMember(Id = 0)]
        public int Id { get; set; }

        [SugarColumn(Length = 200, IsNullable = false)]
        [Required]
        [GenerateSerializerMember(Id = 1)]
        public string Name { get; set; } = string.Empty;

        [SugarColumn(ColumnDataType = "text", IsNullable = true)]
        [GenerateSerializerMember(Id = 2)]
        public string? Description { get; set; }

        [SugarColumn(DecimalDigits = 2, LengthDigits = 18, IsNullable = false)]
        [Required]
        [GenerateSerializerMember(Id = 3)]
        public decimal Price { get; set; }

        [SugarColumn(IsNullable = false)]
        [Required]
        [GenerateSerializerMember(Id = 4)]
        public bool IsActive { get; set; } = true;
    }
}
```

### 最佳实践

1. **主键命名**: 使用 `Id` 作为主键属性名
2. **审计字段**: 包含 `CreatedAt`、`UpdatedAt` 等审计字段
3. **序列化注解**: 使用 `[GenerateSerializerMember]` 指定序列化ID
4. **表名注解**: 使用 `[SugarTable]` 指定数据库表名
5. **字段注解**: 使用 `[SugarColumn]]` 指定数据库列属性

## 🎯 生成的代码特性

### Interface 特性

- ✅ 支持异步操作
- ✅ 包含完整的CRUD方法
- ✅ 支持批量操作
- ✅ 提供搜索和统计功能
- ✅ Orleans 特性注解 (`[ReadOnly]`, `[AlwaysInterleave]`)
- ✅ 详细的XML文档注释

### Grain 特性

- ✅ 依赖注入支持
- ✅ SqlSugar 仓储集成
- ✅ 完整的错误处理
- ✅ 结构化日志记录
- ✅ 异步编程模式
- ✅ 性能优化

### Dto 特性

- ✅ Orleans 序列化支持
- ✅ 数据验证注解
- ✅ 不同的Dto类型（Create、Update、Response）
- ✅ 自动对象映射支持

### Controller 特性

- ✅ RESTful API 设计
- ✅ 完整的HTTP状态码处理
- ✅ 输入验证
- ✅ 异常处理
- ✅ API文档注释

## 🔄 高级用法

### 自定义模板

```csharp
public class CustomTemplate : ITemplate
{
    public string Generate(EntityMetadata metadata)
    {
        // 自定义代码生成逻辑
        return $"// 自定义 {metadata.EntityName} 代码";
    }
}

// 注册自定义模板
services.AddSingleton<ITemplate, CustomTemplate>();
```

### 批量生成多个实体

```csharp
var entities = new[] { "Product", "User", "Order" };
foreach (var entity in entities)
{
    var result = await _codeGenerator.GenerateCodeAsync(entity, GenerationType.All);
    // 处理结果...
}
```

### 代码预览

```csharp
// 预览而不实际生成文件
var previews = await _codeGenerator.PreviewCodeAsync("Product");
foreach (var (type, code) in previews)
{
    Console.WriteLine($"=== {type} ===");
    Console.WriteLine(code);
}
```

## 🧪 测试和验证

### 单元测试

项目包含完整的单元测试：

```bash
# 运行所有测试
dotnet test FakeMicro.Utilities.Tests

# 运行特定测试
dotnet test --filter "CodeGeneratorTests"
```

### 集成测试

```csharp
[Fact]
public async Task GenerateProductCode_ShouldCreateAllFiles()
{
    var result = await _codeGenerator.GenerateCodeAsync("Product", GenerationType.All);
    
    Assert.True(result.IsSuccess);
    Assert.Equal(4, result.GeneratedFiles.Count); // Interface, Grain, Dto, Controller
}
```

## 🚨 注意事项

1. **文件覆盖**: 默认不覆盖已存在的文件，可在配置中修改
2. **实体依赖**: 确保实体类在 `FakeMicro.Entities` 项目中
3. **命名规范**: 遵循 C# 命名约定，避免特殊字符
4. **权限要求**: 确保有输出目录的写入权限
5. **数据库同步**: 生成的代码不包含数据库迁移，需要手动处理

## 🐛 常见问题

### Q: 生成的代码编译错误
A: 检查实体类是否正确配置，特别是序列化注解

### Q: 找不到实体类型
A: 确保实体类在 `FakeMicro.Entities` 项目中，并且是公共类

### Q: 文件生成失败
A: 检查文件权限和输出路径配置

### Q: 生成的代码格式问题
A: 可以通过配置自定义代码模板或格式化工具

## 📞 技术支持

- 📚 查看 `README.md` 获取详细信息
- 🔍 运行单元测试了解预期行为
- 🛠️ 使用 `preview` 命令检查生成的代码
- 📝 提交Issue反馈问题和建议

---

**代码生成器版本**: 1.0.0  
**支持的框架**: Orleans 9.x + SqlSugar  
**目标平台**: .NET 9.0