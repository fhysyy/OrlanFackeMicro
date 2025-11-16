# CAP 事件总线集成指南

## 概述
FakeMicro项目已集成CAP（DotNetCore.CAP）事件总线，提供分布式事务和事件驱动的微服务架构支持。

## 功能特性
- ✅ 分布式事务支持
- ✅ 事件发布/订阅模式
- ✅ PostgreSQL持久化存储
- ✅ RabbitMQ消息队列
- ✅ CAP仪表板监控
- ✅ 失败重试机制

## 配置说明

### 数据库配置
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=fakemicro;Username=postgres;Password=password"
  }
}
```

### RabbitMQ配置
```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "UserName": "guest",
    "Password": "guest",
    "Port": "5672"
  }
}
```

## 使用方法

### 1. 发布事件
```csharp
public class MyService
{
    private readonly EventPublisherService _eventPublisher;
    
    public async Task CreateUserAsync(User user)
    {
        // 业务逻辑...
        
        // 发布用户创建事件
        await _eventPublisher.PublishUserCreatedAsync(
            user.Id, user.Username, user.Email);
    }
}
```

### 2. 订阅事件
```csharp
public class UserEventHandler : ICapSubscribe
{
    [CapSubscribe("user.created")]
    public async Task HandleUserCreated(UserCreatedEvent @event)
    {
        // 处理用户创建事件
        // 可以在这里发送邮件、更新缓存等
    }
}
```

### 3. API端点
- `POST /api/events/user-created` - 发布用户创建事件
- `POST /api/events/user-updated` - 发布用户更新事件  
- `POST /api/events/user-deleted` - 发布用户删除事件
- `GET /api/events/test` - 测试事件总线连接

## CAP仪表板
访问 `http://localhost:5000/cap` 查看事件监控仪表板。

## 依赖服务

### 必需服务
1. **PostgreSQL** - 事件持久化存储
2. **RabbitMQ** - 消息队列服务

### 启动命令
```bash
# 启动PostgreSQL (Docker)
docker run -d --name postgres -p 5432:5432 -e POSTGRES_PASSWORD=password postgres:latest

# 启动RabbitMQ (Docker)
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:management
```

## 事件类型

### 用户相关事件
- `user.created` - 用户创建事件
- `user.updated` - 用户更新事件  
- `user.deleted` - 用户删除事件

### 自定义事件
可以扩展更多事件类型来满足业务需求。

## 故障排除

### 常见问题
1. **连接失败**：检查PostgreSQL和RabbitMQ服务是否正常运行
2. **权限问题**：确认数据库用户有足够的权限
3. **网络问题**：确保服务间网络连通性

### 日志查看
事件处理日志会在控制台输出，也可以通过CAP仪表板查看详细状态。