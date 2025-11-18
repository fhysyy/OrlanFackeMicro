# 快速入门指南

## 5分钟快速开始

### 1. 安装依赖

```bash
# 安装必要的NuGet包
dotnet add package SqlSugar
dotnet add package Microsoft.Extensions.Caching.Memory
dotnet add package Microsoft.Extensions.Logging
dotnet add package Orleans.Core
dotnet add package Orleans.Persistence
```

### 2. 配置服务

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// 添加SqlSugar数据访问服务
builder.Services.AddSqlSugarDataAccess(
    connectionString: builder.Configuration.GetConnectionString("Default"),
    databaseType: DbType.SqlServer);

// 添加Orleans服务
builder.Services.AddOrleansGrains(options =>
{
    options.UseDevelopmentCluster = true;
    options.ApplicationAssembly = typeof(Program).Assembly;
});

// 添加缓存
builder.Services.AddMemoryCache();
builder.Services.AddScoped(typeof(IRepository<,>), typeof(SqlSugarRepository<,>));

var app = builder.Build();
```

### 3. 创建实体

```csharp
public class Product
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public decimal Price { get; set; }
    
    [SugarColumn(IsNullable = true)]
    public string? Description { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [SugarColumn(IsNullable = true)]
    public DateTime? UpdatedAt { get; set; }
    
    [SugarColumn(IsDelete = true)]
    public bool IsDeleted { get; set; } = false;
}
```

### 4. 使用仓储

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly ILogger<ProductsController> _logger;
    
    public ProductsController(
        IRepository<Product, Guid> productRepository,
        ILogger<ProductsController> logger)
    {
        _productRepository = productRepository;
        _logger = logger;
    }
    
    // 获取所有产品（分页）
    [HttpGet]
    public async Task<ActionResult<IPagedResult<Product>>> GetProducts(
        [FromQuery] int pageIndex = 0, 
        [FromQuery] int pageSize = 10)
    {
        var result = await _productRepository.GetPagedAsync(pageIndex, pageSize);
        return Ok(result);
    }
    
    // 根据ID获取产品
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Product>> GetProduct(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
            return NotFound();
        
        return Ok(product);
    }
    
    // 创建产品
    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(Product product)
    {
        product.Id = Guid.NewGuid();
        product.CreatedAt = DateTime.UtcNow;
        
        await _productRepository.AddAsync(product);
        await _productRepository.SaveChangesAsync();
        
        _logger.LogInformation("创建产品成功: {ProductId}", product.Id);
        
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }
    
    // 更新产品
    [HttpPut("{id:guid}")]
    public async Task<ActionResult> UpdateProduct(Guid id, Product product)
    {
        if (id != product.Id)
            return BadRequest();
        
        var existingProduct = await _productRepository.GetByIdAsync(id);
        if (existingProduct == null)
            return NotFound();
        
        product.UpdatedAt = DateTime.UtcNow;
        await _productRepository.UpdateAsync(product);
        await _productRepository.SaveChangesAsync();
        
        _logger.LogInformation("更新产品成功: {ProductId}", product.Id);
        
        return NoContent();
    }
    
    // 删除产品（软删除）
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteProduct(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
            return NotFound();
        
        await _productRepository.DeleteAsync(product);
        await _productRepository.SaveChangesAsync();
        
        _logger.LogInformation("删除产品成功: {ProductId}", product.Id);
        
        return NoContent();
    }
}
```

### 5. 使用Orleans Grain

```csharp
// Grain接口
public interface IProductGrain : IGrainWithGuidKey
{
    Task<Product?> GetAsync();
    Task<Product?> CreateAsync(Product product);
    Task<Product?> UpdateAsync(Product product);
    Task<bool> DeleteAsync();
}

// Grain实现
public class ProductGrain : Grain, IProductGrain
{
    private readonly IRepository<Product, Guid> _repository;
    private readonly ILogger<ProductGrain> _logger;
    
    public ProductGrain(IRepository<Product, Guid> repository, ILogger<ProductGrain> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    
    public async Task<Product?> GetAsync()
    {
        var productId = this.GetPrimaryKey();
        return await _repository.GetByIdAsync(productId);
    }
    
    public async Task<Product?> CreateAsync(Product product)
    {
        product.Id = this.GetPrimaryKey();
        product.CreatedAt = DateTime.UtcNow;
        
        await _repository.AddAsync(product);
        await _repository.SaveChangesAsync();
        
        _logger.LogInformation("Grain创建产品成功: {ProductId}", product.Id);
        return product;
    }
    
    public async Task<Product?> UpdateAsync(Product product)
    {
        product.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(product);
        await _repository.SaveChangesAsync();
        
        _logger.LogInformation("Grain更新产品成功: {ProductId}", product.Id);
        return product;
    }
    
    public async Task<bool> DeleteAsync()
    {
        var product = await _repository.GetByIdAsync(this.GetPrimaryKey());
        if (product == null)
            return false;
        
        await _repository.DeleteAsync(product);
        await _repository.SaveChangesAsync();
        
        _logger.LogInformation("Grain删除产品成功: {ProductId}", product.Id);
        return true;
    }
}
```

### 6. 事务管理示例

```csharp
public class OrderService
{
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IRepository<OrderItem, Guid> _orderItemRepository;
    private readonly ITransactionManager _transactionManager;
    
    public async Task<Order> CreateOrderWithItemsAsync(Order order, IEnumerable<OrderItem> items)
    {
        await _transactionManager.ExecuteInTransactionAsync(async () =>
        {
            // 创建订单
            await _orderRepository.AddAsync(order);
            
            // 添加订单项
            foreach (var item in items)
            {
                item.OrderId = order.Id;
                await _orderItemRepository.AddAsync(item);
            }
            
            // 保存所有更改
            await _orderRepository.SaveChangesAsync();
        });
        
        return order;
    }
}
```

### 7. 性能优化示例

```csharp
public class ProductService
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly ICacheManager _cacheManager;
    
    public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(Guid categoryId)
    {
        var cacheKey = $"products_category_{categoryId}";
        
        // 尝试从缓存获取
        var cachedProducts = await _cacheManager.GetAsync<IEnumerable<Product>>(cacheKey);
        if (cachedProducts != null)
            return cachedProducts;
        
        // 从数据库查询
        var products = await _productRepository.GetByConditionAsync(p => p.CategoryId == categoryId);
        
        // 缓存结果（30分钟）
        await _cacheManager.SetAsync(cacheKey, products, TimeSpan.FromMinutes(30));
        
        return products;
    }
    
    public async Task<IPagedResult<Product>> GetProductsWithPagingAsync(int pageIndex, int pageSize)
    {
        return await _productRepository.GetPagedAsync(pageIndex, pageSize);
    }
    
    public async Task BulkInsertProductsAsync(IEnumerable<Product> products)
    {
        // 批量插入提高性能
        await _productRepository.AddBatchedAsync(products, 1000);
    }
}
```

## 常用配置

### 数据库连接配置

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=(local);Initial Catalog=YourDatabase;Integrated Security=True;"
  },
  "SqlSugarOptions": {
    "ConnectionString": "Data Source=(local);Initial Catalog=YourDatabase;Integrated Security=True;",
    "DbType": "SqlServer",
    "IsAutoCloseConnection": true,
    "MoreSettings": {
      "EnableOracleBracket": false,
      "EnableDiffLogEvent": true,
      "EnableUnderLineWords": false,
      "IsJoinRemoveRepeat": false
    },
    "ConfigureExternalServices": {
      "DataInfoCacheService": "FakeMicro.DatabaseAccess.Caching.SqlSugarCacheService"
    }
  }
}
```

### Orleans配置

```json
{
  "Orleans": {
    "ClusterOptions": {
      "ClusterId": "FakeMicroCluster",
      "ServiceId": "FakeMicroService"
    },
    "SiloOptions": {
      "SiloName": "FakeMicroSilo"
    },
    "OrleansDashboard": {
      "EmbeddedDashboard": true,
      "DashboardPort": 8080
    }
  }
}
```

### 缓存配置

```json
{
  "CacheOptions": {
    "EnableCaching": true,
    "DefaultExpirationMinutes": 30,
    "Providers": {
      "Memory": {
        "Type": "Memory",
        "ExpirationMinutes": 30
      },
      "Redis": {
        "Type": "Redis",
        "Configuration": "localhost:6379",
        "ExpirationMinutes": 60
      }
    }
  }
}
```

## 测试API

### 1. 创建产品

```bash
curl -X POST "https://localhost:5001/api/products" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "测试产品",
    "price": 99.99,
    "description": "这是一个测试产品"
  }'
```

### 2. 获取产品列表

```bash
curl "https://localhost:5001/api/products?pageIndex=0&pageSize=10"
```

### 3. 获取单个产品

```bash
curl "https://localhost:5001/api/products/{product-id}"
```

### 4. 更新产品

```bash
curl -X PUT "https://localhost:5001/api/products/{product-id}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "更新后的产品名称",
    "price": 149.99,
    "description": "更新后的描述"
  }'
```

### 5. 删除产品

```bash
curl -X DELETE "https://localhost:5001/api/products/{product-id}"
```

## 故障排除

### 常见问题解决

1. **连接字符串错误**
   ```
   解决：检查连接字符串格式和数据库服务状态
   ```

2. **实体属性映射错误**
   ```
   解决：确保实体属性上的SugarColumn特性正确配置
   ```

3. **事务冲突**
   ```
   解决：缩短事务持续时间，避免长时间锁定
   ```

4. **性能问题**
   ```
   解决：添加适当索引，使用分页查询，启用缓存
   ```

### 调试技巧

```csharp
// 启用SQL日志
SqlSugarScope.Client!.Aop.OnLogExecuting = (sql, pars) => 
{
    Console.WriteLine("SQL: " + sql);
    Console.WriteLine("Params: " + JsonConvert.SerializeObject(pars));
};
```

---

**下一步**: 查看完整的 [README.md](README.md) 获取详细的架构信息和最佳实践。