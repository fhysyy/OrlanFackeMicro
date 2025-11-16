# FakeMicro 项目部署指南

## 项目概述

FakeMicro 是一个基于 Microsoft Orleans 框架的微服务管理平台，采用现代化的微服务架构设计，支持容器化部署。

## 技术栈

- **后端框架**: .NET 9.0 + Orleans 9.2.1
- **前端框架**: Vue 3 + TypeScript + Element Plus
- **数据库**: PostgreSQL
- **消息队列**: RabbitMQ
- **缓存**: Redis
- **容器化**: Docker + Docker Compose
- **监控**: ELK Stack (Elasticsearch + Logstash + Kibana)

## 部署方式

### 1. 开发环境部署

#### 使用 Docker Compose（推荐）

```bash
# 克隆项目
git clone <repository-url>
cd FakeMicro

# 构建并启动开发环境
./scripts/deploy.sh -e dev -a up

# 或者使用docker-compose直接启动
docker-compose -f docker-compose.dev.yml up -d
```

#### 手动部署

```bash
# 启动依赖服务
docker-compose -f docker-compose.dev.yml up postgres redis rabbitmq -d

# 构建项目
dotnet build

# 启动Silo服务
cd src/FakeMicro.Silo
dotnet run

# 启动API网关（新终端）
cd src/FakeMicro.Api  
dotnet run

# 启动前端（新终端）
cd frontend
npm install
npm run dev
```

### 2. 生产环境部署

#### 使用 Docker Compose

```bash
# 构建生产环境镜像
./scripts/deploy.sh -e prod -a build

# 启动生产环境
./scripts/deploy.sh -e prod -a up

# 或者使用docker-compose
docker-compose up -d
```

#### Kubernetes 部署（可选）

```bash
# 应用Kubernetes配置
kubectl apply -f k8s/

# 查看部署状态
kubectl get pods,svc,ingress
```

## 环境配置

### 开发环境配置

编辑 `docker-compose.dev.yml` 文件：

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Development
  - ConnectionStrings:DefaultConnection=Host=postgres;Port=5432;Database=fakemicro;Username=postgres;Password=postgres
```

### 生产环境配置

编辑 `docker-compose.yml` 文件：

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Production
  - ConnectionStrings:DefaultConnection=${DATABASE_URL}
```

设置环境变量：

```bash
export DATABASE_URL=postgresql://username:password@host:5432/fakemicro
export RABBITMQ_URL=amqp://username:password@host:5672
export REDIS_URL=redis://host:6379
```

## 服务访问地址

### 开发环境
- **API网关**: http://localhost:5001
- **前端界面**: http://localhost:3001
- **数据库**: localhost:5432
- **RabbitMQ管理**: http://localhost:15673
- **Redis**: localhost:6379

### 生产环境
- **API网关**: http://localhost:5000
- **前端界面**: http://localhost:3000
- **数据库**: localhost:5432
- **RabbitMQ管理**: http://localhost:15672
- **Redis**: localhost:6379
- **Kibana**: http://localhost:5601

## 数据库初始化

项目启动时会自动执行数据库迁移和初始化脚本：

```sql
-- 手动执行初始化脚本（如果需要）
psql -h localhost -U postgres -f scripts/init-db.sql
```

## 健康检查

所有服务都包含健康检查端点：

- **API健康检查**: `GET http://localhost:5000/health`
- **Silo健康检查**: `GET http://localhost:8080/health`

## 监控和日志

### 日志聚合

项目集成了 ELK Stack 进行日志聚合：

```bash
# 启动ELK服务
docker-compose -f docker-compose.yml up elasticsearch logstash kibana -d

# 查看日志
docker-compose logs -f fakemicro-api
```

### 性能监控

- **应用指标**: 通过 `/metrics` 端点暴露
- **数据库监控**: PostgreSQL 性能指标
- **系统监控**: 通过系统监控页面查看

## 故障排除

### 常见问题

1. **端口冲突**
   ```bash
   # 检查端口占用
   netstat -tulpn | grep :5000
   
   # 修改端口配置
   docker-compose.yml 中的 ports 配置
   ```

2. **数据库连接失败**
   ```bash
   # 检查数据库服务
   docker-compose ps postgres
   
   # 检查连接字符串
   echo $DATABASE_URL
   ```

3. **内存不足**
   ```bash
   # 调整Docker内存限制
   docker-compose down
   docker system prune -f
   docker-compose up -d
   ```

### 日志查看

```bash
# 查看所有服务日志
docker-compose logs

# 查看特定服务日志
docker-compose logs fakemicro-api

# 实时日志
docker-compose logs -f fakemicro-silo
```

## 备份和恢复

### 数据库备份

```bash
# 备份数据库
docker-compose exec postgres pg_dump -U postgres fakemicro > backup.sql

# 恢复数据库
docker-compose exec -T postgres psql -U postgres fakemicro < backup.sql
```

### 文件备份

```bash
# 备份上传的文件
tar -czf files-backup.tar.gz uploads/

# 恢复文件
tar -xzf files-backup.tar.gz -C ./
```

## 安全配置

### 环境变量安全

```bash
# 创建.env文件（不要提交到版本控制）
cat > .env << EOF
DATABASE_URL=postgresql://user:pass@host:5432/db
JWT_SECRET=your-super-secret-jwt-key
RABBITMQ_URL=amqp://user:pass@host:5672
REDIS_URL=redis://:password@host:6379
EOF
```

### 防火墙配置

```bash
# 只开放必要端口
ufw allow 22    # SSH
ufw allow 80    # HTTP
ufw allow 443   # HTTPS
ufw enable
```

## 扩展部署

### 水平扩展

```yaml
# docker-compose.scale.yml
services:
  fakemicro-api:
    deploy:
      replicas: 3
    environment:
      - ORLEANS_CLUSTERING=AdoNet
      - ORLEANS_ADONET_INVARIANT=Npgsql
```

### 负载均衡

```yaml
services:
  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
    depends_on:
      - fakemicro-api
```

## 更新部署

```bash
# 拉取最新代码
git pull origin main

# 重新构建和部署
./scripts/deploy.sh -e prod -a down
./scripts/deploy.sh -e prod -a build
./scripts/deploy.sh -e prod -a up

# 或者使用滚动更新
docker-compose up -d --force-recreate --build
```

## 联系方式

如有部署问题，请联系：
- 项目维护者: [your-email@example.com]
- 文档更新: 提交 Pull Request
- 问题反馈: 创建 Issue

---

**注意**: 生产环境部署前请务必进行充分测试，并确保所有安全配置正确设置。