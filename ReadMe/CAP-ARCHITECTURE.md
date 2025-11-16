# CAP事件总线架构设计

## 架构概述
FakeMicro项目采用分层的事件总线架构，遵循微服务最佳实践，将CAP事件总线正确集成到各个层级。

## 架构层级

### 1. Interfaces层 - 事件契约定义
**位置**: `src/FakeMicro.Interfaces/Events/`
**职责**: 定义事件契约和接口
- `IUserEvents` - 用户事件处理接口
- `IEventPublisher` - 事件发布服务接口
- 事件数据类 (`UserCreatedEvent`, `UserUpdatedEvent`, `UserDeletedEvent`)

### 2. API层 - 事件发布
**位置**: `src/FakeMicro.Api/`
**职责**: 接收HTTP请求并发布事件
- 控制器接收外部请求
- 使用`IEventPublisher`发布事件
- 不处理业务逻辑，只负责事件发布

### 3. Grains层 - 事件订阅和处理
**位置**: `src/FakeMicro.Grains/`
**职责**: 订阅和处理事件，执行业务逻辑
- `UserEventHandler` - 用户事件处理器
- `EventPublisherService` - 事件发布服务实现
- 包含实际的业务逻辑处理

### 4. 其他服务层 - 可选事件订阅
**扩展性**: 其他微服务可以订阅相同的事件

## 设计原则

### 1. 单一职责原则
- **API层**: 只负责接收请求和发布事件
- **Grains层**: 只负责业务逻辑和事件处理
- **Interfaces层**: 只负责契约定义

### 2. 依赖倒置原则
- 高层模块不依赖低层模块，都依赖抽象
- 使用接口进行解耦

### 3. 开闭原则
- 易于扩展新的事件类型
- 易于添加新的事件处理器

## 事件流

### 发布事件流程
```
HTTP请求 → API控制器 → IEventPublisher → CAP事件总线
```

### 订阅事件流程
```
CAP事件总线 → UserEventHandler → 业务逻辑处理
```

## 配置说明

### CAP配置 (API层)
```csharp
builder.Services.AddCap(options =>
{
    options.UsePostgreSql(conf => { /* PostgreSQL配置 */ });
    options.FailedRetryCount = 5;
    options.FailedRetryInterval = 30;
});
```

### 服务注册
```csharp
// API层 - 注册发布服务
builder.Services.AddScoped<IEventPublisher, EventPublisherService>();

// Grains层 - 注册事件处理器
builder.Services.AddScoped<IUserEvents, UserEventHandler>();
```

## 优势

### 1. 解耦性
- API层和业务逻辑完全解耦
- 事件发布者和订阅者互不依赖

### 2. 可扩展性
- 易于添加新的事件类型
- 多个服务可以订阅相同事件

### 3. 可靠性
- CAP提供事务性消息保证
- 失败重试机制

### 4. 可观测性
- CAP仪表板提供事件监控
- 完整的日志记录

## 使用示例

### 发布事件
```csharp
// 在API控制器中
await _eventPublisher.PublishUserCreatedAsync(userId, username, email);
```

### 订阅事件
```csharp
// 在Grain事件处理器中
[CapSubscribe("user.created")]
public async Task HandleUserCreatedAsync(UserCreatedEvent @event)
{
    // 业务逻辑处理
}
```

这种架构确保了事件总线的正确使用，遵循了微服务架构的最佳实践。