# Orleans SqlSugar 代码生成器

这是一个专为 Orleans 微服务项目设计的智能代码生成器，能够根据实体类自动生成完整的 CRUD 操作代码，包括 Interface、Grain、DTO、Service 和 Controller 层。

## 功能特性

- 🚀 **全栈代码生成**：一键生成 Interface、Grain、DTO、Service、Controller 完整代码
- 📝 **智能模板引擎**：基于现有项目结构自动适配代码风格
- 🔧 **灵活配置**：支持自定义命名空间、输出路径、作者信息等
- 🎯 **Orleans 最佳实践**：严格遵循 Orleans 框架开发规范
- 💾 **SqlSugar 集成**：完美集成 SqlSugar ORM 仓储模式
- 🛡️ **类型安全**：生成强类型代码，支持异步操作

## 支持的代码类型

| 类型 | 描述 | 生成位置 |
|------|------|----------|
| Interface | Orleans Grain 接口定义 | `FakeMicro.Interfaces` |
| Grain | Orleans Grain 实现 | `FakeMicro.Grains` |
| Dto | 数据传输对象 | `FakeMicro.Entities` |
| ServiceInterface | 服务接口 | `FakeMicro.Interfaces` |
| ServiceGrain | 服务实现 | `FakeMicro.Grains` |
| Controller | API 控制器 | `FakeMicro.Api` |

## 快速开始

### 1. 命令行使用

```bash
# 生成单个实体的所有代码
dotnet run --project . FakeMicro.Utilities/CodeGenerator/CodeGeneratorCLI.cs -- generate Product --all

# 生成特定类型的代码
dotnet run --project . FakeMicro.Utilities/CodeGenerator/CodeGeneratorCLI.cs -- generate User --type Interface --type Grain

# 使用自定义配置
dotnet run --project . FakeMicro.Utilities/CodeGenerator/CodeGeneratorCLI.cs -- generate Order --config custom-config.json

# 批量生成多个实体
dotnet run --project . FakeMicro.Utilities/CodeGenerator/CodeGeneratorCLI.cs -- generate Product,User,Order --all
```

### 2. 程序化调用

```csharp
// 基本使用
var generator = new CodeGenerator();
var result = await generator.GenerateCodeAsync("Product", GenerationType.All);

if (result.IsSuccess)
{
    Console.WriteLine($"成功生成 {result.GeneratedFiles.Count} 个文件");
}
else
{
    Console.WriteLine($"生成失败: {result.ErrorMessage}");
}

// 自定义配置
var config = new CodeGeneratorConfiguration
{
    BaseNamespace = "MyCompany.MyApp",
    AuthorName = "开发者",
    OutputDirectories = new Dictionary<string, string>
    {
        ["Interface"] = @"C:\MyProject\Interfaces",
        ["Grain"] = @"C:\MyProject\Grains"
    }
};

var generator = new CodeGenerator(config);
await generator.GenerateCodeAsync("Customer", GenerationType.Interface | GenerationType.Grain);
```

## 生成的代码示例

### Interface 示例
```csharp
using FakeMicro.Interfaces;
using FakeMicro.Entities;
using Orleans;

namespace FakeMicro.Interfaces
{
    public interface IProductGrain : IGrainWithIntegerKey
    {
        Task<ProductDto> CreateProductAsync(CreateProductDto dto);
        Task<ProductDto?> GetProductAsync();
        Task<ProductDto> UpdateProductAsync(UpdateProductDto dto);
        Task<bool> DeleteProductAsync();
    }
}
```

### Grain 实现示例
```csharp
using FakeMicro.Interfaces;
using FakeMicro.Entities;
using FakeMicro.DatabaseAccess;
using Orleans;

namespace FakeMicro.Grains
{
    public class ProductGrain : Grain, IProductGrain
    {
        private readonly SqlSugarRepository<Product, int> _repository;
        
        public ProductGrain(SqlSugarRepository<Product, int> repository)
        {
            _repository = repository;
        }

        public async Task<ProductDto> CreateProductAsync(CreateProductDto dto)
        {
            var entity = new Product
            {
                // 自动映射属性
            };
            
            await _repository.InsertAsync(entity);
            return entity.Adapt<ProductDto>();
        }

        public async Task<ProductDto?> GetProductAsync()
        {
            var entity = await _repository.GetByIdAsync(this.GetPrimaryKey());
            return entity?.Adapt<ProductDto>();
        }
        // ... 其他 CRUD 方法
    }
}
```

## 配置文件

代码生成器支持 JSON 配置文件，示例配置：

```json
{
  "BaseNamespace": "FakeMicro",
  "AuthorName": "代码生成器",
  "OutputDirectories": {
    "Interface": "src/FakeMicro.Interfaces",
    "Grain": "src/FakeMicro.Grains",
    "Dto": "src/FakeMicro.Entities",
    "ServiceInterface": "src/FakeMicro.Interfaces",
    "ServiceGrain": "src/FakeMicro.Grains",
    "Controller": "src/FakeMicro.Api"
  },
  "EntityNamespace": "FakeMicro.Entities",
  "UseUtcTime": true,
  "GenerateServiceClasses": true
}
```

## 最佳实践建议

### 1. 实体设计规范
- 使用 `[SugarTable]` 注解指定表名
- 包含 `Id` 作为主键
- 添加审计字段 `CreatedAt`、`UpdatedAt`

### 2. 代码生成时机
- 在设计数据库表结构后先生成实体类
- 然后使用代码生成器生成完整的 CRUD 代码
- 根据业务需求自定义生成的代码

### 3. 自定义扩展
生成的基础代码可以作为起点，根据具体业务需求进行扩展：
- 添加业务验证逻辑
- 实现复杂查询方法
- 集成缓存机制
- 添加事件处理

## 错误处理

代码生成器提供完善的错误处理机制：

```csharp
var result = await generator.GenerateCodeAsync("Product", GenerationType.All);

if (!result.IsSuccess)
{
    switch (result.ErrorType)
    {
        case GeneratorErrorType.EntityNotFound:
            Console.WriteLine("实体类不存在");
            break;
        case GeneratorErrorType.FileExists:
            Console.WriteLine("目标文件已存在");
            break;
        case GeneratorErrorType.PermissionDenied:
            Console.WriteLine("没有文件写入权限");
            break;
    }
}
```

## 技术特点

- ✅ 支持 Orleans Grain 生命周期
- ✅ 集成 SqlSugar 异步操作
- ✅ 自动对象映射（Mapster）
- ✅ 完整的错误处理
- ✅ 支持依赖注入
- ✅ 异步编程模式
- ✅ 类型安全设计

这个代码生成器可以显著提高开发效率，确保代码的一致性和质量，让开发者专注于业务逻辑的实现。