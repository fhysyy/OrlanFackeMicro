# 🚀 FakeMicro 生产环境部署检查清单

## 📋 部署前检查 (Pre-Deployment)

### ✅ 环境配置

- [ ] 设置所有必需的环境变量
  ```bash
  export JWT_SECRET_KEY="..."
  export DB_PASSWORD="..."
  export REDIS_PASSWORD="..."
  ```

- [ ] 验证配置文件
  - [ ] `appsettings.Production.json` 存在
  - [ ] `UseLocalhostClustering = false`
  - [ ] 数据库连接字符串正确
  - [ ] Redis 连接字符串正确

- [ ] 创建 `.env` 文件（从 `.env.example` 复制）

### ✅ 数据库准备

- [ ] 执行 Orleans 数据库脚本
  ```bash
  psql -U postgres -d fakemicro -f FakeMicro.Silo/Scripts/Orleans-PostgreSQL.sql
  ```

- [ ] 执行性能索引脚本
  ```bash
  psql -U postgres -d fakemicro -f FakeMicro.DatabaseAccess/Scripts/AddPerformanceIndexes.sql
  ```

- [ ] 验证表和索引创建
  ```sql
  SELECT tablename FROM pg_tables WHERE schemaname = 'public' AND tablename LIKE 'Orleans%';
  SELECT indexname FROM pg_indexes WHERE schemaname = 'public';
  ```

- [ ] 执行数据库备份
  ```bash
  pg_dump -U postgres fakemicro > backup_$(date +%Y%m%d).sql
  ```

### ✅ 依赖服务

- [ ] PostgreSQL 运行正常
  ```bash
  pg_isready -h localhost -p 5432
  ```

- [ ] Redis 运行正常
  ```bash
  redis-cli PING
  ```

- [ ] MongoDB 运行正常（如果使用）
  ```bash
  mongosh --eval "db.runCommand({ping: 1})"
  ```

### ✅ 代码质量

- [ ] 所有单元测试通过
  ```bash
  dotnet test
  ```

- [ ] 没有编译警告
  ```bash
  dotnet build --no-incremental
  ```

- [ ] 代码扫描通过（SonarQube/CodeQL）

- [ ] 依赖安全检查
  ```bash
  dotnet list package --vulnerable
  ```

---

## 🏗️ 部署步骤 (Deployment)

### Step 1: 构建应用

```bash
# 1. 清理旧构建
dotnet clean

# 2. 发布应用
dotnet publish FakeMicro.Api/FakeMicro.Api.csproj \
  -c Release \
  -o ./publish/api \
  --self-contained false

dotnet publish FakeMicro.Silo/FakeMicro.Silo.csproj \
  -c Release \
  -o ./publish/silo \
  --self-contained false

# 3. 验证输出
ls -lh ./publish/api
ls -lh ./publish/silo
```

### Step 2: 部署到服务器

```bash
# 1. 停止旧服务
systemctl stop fakemicro-api
systemctl stop fakemicro-silo

# 2. 备份当前版本
mv /opt/fakemicro /opt/fakemicro.backup.$(date +%Y%m%d_%H%M%S)

# 3. 部署新版本
cp -r ./publish/* /opt/fakemicro/

# 4. 设置权限
chown -R fakemicro:fakemicro /opt/fakemicro
chmod +x /opt/fakemicro/api/FakeMicro.Api
chmod +x /opt/fakemicro/silo/FakeMicro.Silo

# 5. 启动服务
systemctl start fakemicro-silo
sleep 10  # 等待Silo启动
systemctl start fakemicro-api
```

### Step 3: 验证部署

```bash
# 1. 检查服务状态
systemctl status fakemicro-api
systemctl status fakemicro-silo

# 2. 检查健康端点
curl http://localhost:5000/health
curl http://localhost:5000/health/ready
curl http://localhost:5000/health/detailed

# 3. 检查日志
journalctl -u fakemicro-api -n 50
journalctl -u fakemicro-silo -n 50

# 4. 检查 Orleans 集群
curl http://localhost:8080/dashboard
```

---

## ✅ 部署后验证 (Post-Deployment)

### 功能测试

- [ ] 用户注册功能正常
  ```bash
  curl -X POST http://localhost:5000/api/auth/register \
    -H "Content-Type: application/json" \
    -d '{"username":"testuser","email":"test@example.com","password":"Test123!"}'
  ```

- [ ] 用户登录功能正常
  ```bash
  curl -X POST http://localhost:5000/api/auth/login \
    -H "Content-Type: application/json" \
    -d '{"usernameOrEmail":"testuser","password":"Test123!"}'
  ```

- [ ] JWT 令牌刷新正常

- [ ] API 限流生效
  ```bash
  # 发送150个请求，应该有部分返回429
  for i in {1..150}; do curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5000/api/auth/login; done
  ```

### 性能验证

- [ ] API 响应时间 < 100ms (P95)
  ```bash
  ab -n 1000 -c 10 http://localhost:5000/api/users
  ```

- [ ] 数据库查询性能正常
  ```sql
  SELECT * FROM pg_stat_statements ORDER BY total_exec_time DESC LIMIT 10;
  ```

- [ ] Redis 命中率 > 80%
  ```bash
  redis-cli INFO stats | grep keyspace_hits
  ```

- [ ] Orleans Grain 激活时间 < 10ms

### 监控告警

- [ ] Prometheus 指标导出正常
  ```bash
  curl http://localhost:5000/metrics
  ```

- [ ] 日志聚合正常（ELK/Loki）

- [ ] APM 追踪正常（Jaeger/Zipkin）
  ```bash
  curl http://localhost:16686/api/services
  ```

- [ ] 告警规则配置完成
  - [ ] API 错误率 > 5%
  - [ ] API 响应时间 > 500ms
  - [ ] 数据库连接失败
  - [ ] Redis 连接失败
  - [ ] Orleans Silo 离线

---

## 🔄 回滚计划 (Rollback)

### 快速回滚步骤

```bash
# 1. 停止新版本服务
systemctl stop fakemicro-api
systemctl stop fakemicro-silo

# 2. 恢复备份版本
rm -rf /opt/fakemicro
mv /opt/fakemicro.backup.YYYYMMDD_HHMMSS /opt/fakemicro

# 3. 启动旧版本服务
systemctl start fakemicro-silo
sleep 10
systemctl start fakemicro-api

# 4. 验证
curl http://localhost:5000/health
```

### 数据库回滚（如有必要）

```bash
# 恢复数据库备份
psql -U postgres -d fakemicro < backup_YYYYMMDD.sql
```

---

## 📊 监控指标

### 关键指标

| 指标 | 正常范围 | 告警阈值 |
|------|---------|---------|
| API 响应时间 (P95) | < 100ms | > 500ms |
| API 错误率 | < 1% | > 5% |
| CPU 使用率 | < 70% | > 90% |
| 内存使用率 | < 80% | > 95% |
| 数据库连接数 | < 30 | > 80 |
| Redis 命中率 | > 80% | < 50% |
| Orleans Grain 数量 | < 10k | > 100k |

### 监控查询

```bash
# CPU & 内存
top -b -n 1 | head -n 20

# 网络连接
netstat -an | grep :5000 | wc -l

# 磁盘空间
df -h

# 数据库连接
psql -U postgres -c "SELECT count(*) FROM pg_stat_activity WHERE datname='fakemicro';"

# Redis 内存
redis-cli INFO memory | grep used_memory_human
```

---

## 🔧 故障排查

### API 无法启动

1. 检查端口占用
   ```bash
   netstat -tulpn | grep :5000
   ```

2. 检查环境变量
   ```bash
   env | grep JWT_SECRET_KEY
   ```

3. 检查日志
   ```bash
   journalctl -u fakemicro-api -n 100 --no-pager
   ```

### Orleans Silo 无法加入集群

1. 检查数据库表
   ```sql
   SELECT * FROM OrleansMembershipTable;
   ```

2. 检查网络连通性
   ```bash
   telnet localhost 11111
   telnet localhost 30000
   ```

3. 清理旧成员
   ```sql
   DELETE FROM OrleansMembershipTable WHERE Status != 1;
   ```

### Redis 连接失败

1. 检查 Redis 服务
   ```bash
   systemctl status redis
   redis-cli PING
   ```

2. 检查防火墙
   ```bash
   iptables -L | grep 6379
   ```

---

## 📝 部署记录

| 日期 | 版本 | 部署人 | 结果 | 备注 |
|------|------|--------|------|------|
| 2026-01-12 | 1.0.0 | Admin | ✅ 成功 | 初始生产部署 |
| | | | | |

---

## 📞 联系人

- **开发团队:** dev-team@fakemicro.com
- **运维团队:** ops-team@fakemicro.com
- **紧急联系:** +86-xxx-xxxx-xxxx

---

**文档版本:** 1.0.0  
**最后更新:** 2026-01-12
