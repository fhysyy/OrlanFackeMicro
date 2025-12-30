# FakeMicro - Orleans微服务示例项目

## 项目概述

FakeMicro是一个基于Orleans框架的微服务示例项目，展示了如何构建高性能、可扩展的分布式应用程序。该项目包含完整的监控、告警、异常处理和配置管理功能，适合作为Orleans应用开发的参考模板。

## 系统架构

### 核心组件

- **Orleans Silo集群**：负责运行Grain实例，处理业务逻辑
- **FakeMicro.Api**：RESTful API网关，提供外部访问接口
- **PostgreSQL**：作为Orleans的持久化存储和CAP消息队列的存储
- **监控系统**：提供实时性能指标和告警功能

### 项目结构

```
FakeMicro/
├── src/
│   ├── FakeMicro.Api/          # RESTful API网关
│   ├── FakeMicro.Configuration/ # 配置管理
│   ├── FakeMicro.DatabaseAccess/ # 数据库访问层
│   ├── FakeMicro.Entities/     # 实体定义
│   ├── FakeMicro.Grains/       # Orleans Grains实现
│   ├── FakeMicro.Interfaces/   # 接口定义
│   ├── FakeMicro.Shared/       # 共享工具和扩展
│   └── FakeMicro.Silo/         # Orleans Silo节点
├── tests/
│   ├── FakeMicro.Api.Tests/    # API测试
│   ├── FakeMicro.Grains.Tests/  # Grain测试
│   └── FakeMicro.Shared.Tests/  # 共享组件测试
├── FakeMicro.sln               # 解决方案文件
└── README.md                   # 项目文档
```

## 环境要求

- .NET 9.0 SDK
- PostgreSQL 15+（用于Orleans持久化和CAP消息队列）
- Windows Server 2019+/Linux（用于生产环境部署）
- Docker（可选，用于容器化部署）

## 部署步骤

### 1. 数据库准备

#### 创建PostgreSQL数据库

```sql
-- 创建Orleans持久化数据库
CREATE DATABASE orleans_db;

-- 创建CAP消息队列数据库
CREATE DATABASE cap_db;

-- 创建用户并授予权限
CREATE USER orleans_user WITH PASSWORD 'your_secure_password';
GRANT ALL PRIVILEGES ON DATABASE orleans_db TO orleans_user;
GRANT ALL PRIVILEGES ON DATABASE cap_db TO orleans_user;
```

#### 配置PostgreSQL

确保PostgreSQL配置文件（postgresql.conf）中启用了以下设置：

```
listen_addresses = '*'  # 允许远程连接
max_connections = 200  # 根据需要调整
shared_buffers = 1GB   # 建议为系统内存的25%
work_mem = 8MB         # 根据需要调整
maintenance_work_mem = 256MB
```

### 2. Orleans Silo部署

#### 配置Silo节点

编辑`FakeMicro.Silo/appsettings.Production.json`文件：

```json
{
  "Orleans": {
    "ClusterId": "fake_micro_cluster",
    "ServiceId": "FakeMicroService",
    "SiloName": "silo-01",
    "Persistence": {
      "ConnectionString": "Host=db_server;Port=5432;Database=orleans_db;Username=orleans_user;Password=your_secure_password;"
    }
  },
  "CAP": {
    "UsePostgreSql": true,
    "PostgreSql": {
      "ConnectionString": "Host=db_server;Port=5432;Database=cap_db;Username=orleans_user;Password=your_secure_password;"
    }
  },
  "Kestrel": {
    "Endpoints": {
      "http": {
        "Url": "http://0.0.0.0:3000"
      },
      "https": {
        "Url": "https://0.0.0.0:3001",
        "Certificate": {
          "Path": "/path/to/certificate.pfx",
          "Password": "certificate_password"
        }
      }
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "AllowedHosts": "*"
}
```

#### 部署Silo节点

**使用dotnet命令部署：**

```bash
cd FakeMicro.Silo
ASPNETCORE_ENVIRONMENT=Production dotnet run
```

**使用Docker部署（可选）：**

创建Dockerfile：

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY bin/Release/net9.0/publish .
EXPOSE 3000
EXPOSE 3001
ENTRYPOINT ["dotnet", "FakeMicro.Silo.dll"]
```

构建并运行容器：

```bash
docker build -t fake_micro_silo .
docker run -d --name fake_micro_silo -p 3000:3000 -p 3001:3001 fake_micro_silo
```

### 3. API网关部署

#### 配置API网关

编辑`FakeMicro.Api/appsettings.Production.json`文件：

```json
{
  "Orleans": {
    "ClusterId": "fake_micro_cluster",
    "GatewayEndpoint": "https://silo-01:3001"
  },
  "Kestrel": {
    "Endpoints": {
      "https": {
        "Url": "https://0.0.0.0:5001",
        "Certificate": {
          "Path": "/path/to/certificate.pfx",
          "Password": "certificate_password"
        }
      }
    }
  },
  "Swagger": {
    "Enabled": true,
    "RequireAuth": true,
    "Username": "swagger_user",
    "Password": "your_secure_password"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "AllowedHosts": "*"
}
```

#### 部署API网关

**使用dotnet命令部署：**

```bash
cd FakeMicro.Api
ASPNETCORE_ENVIRONMENT=Production dotnet run
```

**使用Docker部署（可选）：**

```bash
docker build -t fake_micro_api .
docker run -d --name fake_micro_api -p 5001:5001 fake_micro_api
```

## 配置管理

### 环境变量

项目支持使用环境变量覆盖配置文件中的设置：

| 环境变量 | 描述 | 默认值 |
|---------|------|-------|
| `ASPNETCORE_ENVIRONMENT` | 运行环境 | `Development` |
| `ORLEANS_CLUSTER_ID` | Orleans集群ID | `fake_micro_cluster` |
| `ORLEANS_PERSISTENCE_CONNECTION` | Orleans持久化连接字符串 | - |
| `CAP_POSTGRESQL_CONNECTION` | CAP消息队列连接字符串 | - |
| `KESTREL_HTTPS_URL` | HTTPS监听地址 | - |
| `KESTREL_CERT_PATH` | HTTPS证书路径 | - |
| `KESTREL_CERT_PASSWORD` | HTTPS证书密码 | - |

### 配置文件

项目使用分层配置文件，优先级从高到低：

1. `appsettings.Production.json`（生产环境）
2. `appsettings.Staging.json`（测试环境）
3. `appsettings.Development.json`（开发环境）
4. `appsettings.json`（通用配置）

## 监控和告警

### 监控API

项目提供以下监控端点：

- `GET /api/monitoring/health` - 获取系统健康状态
- `GET /api/monitoring/metrics` - 获取性能指标报告
- `GET /api/monitoring/stats` - 获取系统统计信息
- `POST /api/monitoring/reset` - 重置监控指标

### 告警管理

- `POST /api/monitoring/alerts/configure` - 配置告警规则
- `GET /api/monitoring/alerts/configurations` - 获取告警配置
- `GET /api/monitoring/alerts/active` - 获取当前活动告警

### 关键指标

- **响应时间**：平均响应时间、P95、P99延迟
- **错误率**：请求错误率、异常统计
- **内存使用**：应用程序内存占用
- **Grain活动**：活跃Grain类型和数量
- **请求统计**：总请求数、错误请求数

## 运维最佳实践

### 1. Orleans集群管理

- **集群规模**：根据负载至少部署3个Silo节点以确保高可用性
- **Silo命名**：使用有意义的名称（如`silo-01`, `silo-02`）便于识别
- **持久化**：定期备份PostgreSQL数据库，确保数据安全
- **版本升级**：使用滚动升级方式更新Silo节点，避免服务中断

### 2. 性能优化

- **Grain设计**：
  - 避免大Grain状态，保持Grain轻量化
  - 使用合适的Grain键策略（Guid、String或Long）
  - 实现流式处理处理大量数据

- **配置优化**：
  ```json
  "Orleans": {
    "Silo": {
      "Dispatcher": {
        "MaxThreads": 1000
      },
      "Messaging": {
        "MaxMessageBodySize": 1048576, // 1MB
        "SlowConsumingWarningThreshold": 5000
      }
    }
  }
  ```

### 3. 安全管理

- **HTTPS配置**：在生产环境中强制使用HTTPS
- **API认证**：为Swagger文档和敏感API添加认证
- **密码策略**：使用强密码并定期更换
- **最小权限原则**：数据库用户仅授予必要的权限

### 4. 日志管理

- **日志级别**：生产环境使用`Information`级别，避免过多日志
- **日志存储**：考虑使用ELK Stack或Graylog集中管理日志
- **日志保留**：根据法规要求设置适当的日志保留期

## 故障排查

### 常见问题

1. **Silo无法启动**
   - 检查PostgreSQL连接字符串是否正确
   - 确保端口未被占用
   - 查看日志文件了解具体错误

2. **API无法连接到Silo**
   - 检查GatewayEndpoint配置是否正确
   - 确保Silo的HTTPS证书有效
   - 检查网络防火墙设置

3. **高内存使用率**
   - 检查Grain状态是否过大
   - 调整Orleans的内存限制
   - 分析内存转储文件定位内存泄漏

4. **高错误率**
   - 查看监控系统的错误统计
   - 检查数据库连接池设置
   - 分析Grain中的异常处理逻辑

### 诊断工具

- **Orleans Dashboard**：提供可视化的集群监控
- **dotnet-counters**：实时监控.NET应用性能指标
- **dotnet-dump**：生成和分析内存转储
- **PostgreSQL pg_stat_statements**：分析数据库查询性能

## 开发指南

### 代码规范

- 使用C# 12语法特性
- 遵循SOLID原则
- 为所有公共方法添加XML注释
- 实现适当的异常处理和日志记录

### 测试

项目使用xUnit进行单元测试和集成测试：

```bash
# 运行所有测试
dotnet test

# 运行特定项目的测试
dotnet test tests/FakeMicro.Grains.Tests
```

### 调试

1. **本地Silo调试**：直接运行`FakeMicro.Silo`项目
2. **API调试**：运行`FakeMicro.Api`项目，连接到本地Silo
3. **Grain调试**：使用Orleans的调试支持，在Visual Studio中设置断点

## 版本控制

项目使用Git进行版本控制，遵循以下分支策略：

- `main`：稳定的生产版本
- `develop`：主要开发分支
- `feature/xxx`：新功能开发
- `hotfix/xxx`：紧急修复

## 贡献指南

欢迎提交Issue和Pull Request。在提交代码前，请确保：

1. 所有测试通过
2. 代码遵循项目规范
3. 添加适当的文档和注释
4. 提交消息清晰描述变更内容

## 许可证

MIT License

## 联系方式

如有问题或建议，请通过以下方式联系：

- 项目地址：https://github.com/yourusername/FakeMicro
- 邮箱：your.email@example.com

---

**最后更新：** 2024-09-30
