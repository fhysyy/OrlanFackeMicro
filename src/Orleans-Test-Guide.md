# Orleans Silo 和 API 测试指南

## 测试步骤

### 1. 启动 Orleans Silo

```powershell
cd F:\ProjectCode\Orleans\src\FakeMicro.Silo
dotnet run
```

Silo 应该在端口 11111 上启动，网关端口为 30000。

### 2. 启动 API 服务

```powershell
cd F:\ProjectCode\Orleans\src\FakeMicro.Api
dotnet run
```

API 服务应该默认在端口 5000 或 5001 上启动。

### 3. 测试 API 连接

打开浏览器或使用 Postman 测试以下端点：

- API 健康检查：`http://localhost:5000/health`
- Orleans 连接测试：`http://localhost:5000/api/test/orleans`
- Swagger UI：`http://localhost:5000/swagger`

### 4. 预期结果

1. Silo 启动应该显示 "Orleans Silo启动成功！"
2. API 健康检查应返回 `{"Status": "OK"}`
3. Orleans 连接测试应返回类似：
   ```json
   {
     "Success": true,
     "Message": "Orleans客户端已连接",
     "Details": "客户端可用"
   }
   ```

## 常见问题及解决方案

### Silo 启动失败

1. 检查端口 11111 和 30000 是否被占用
2. 确保 appsettings.json 配置正确
3. 检查 Orleans 包版本一致性

### API 连接 Silo 失败

1. 确保 Silo 已成功启动
2. 检查 API 中的 Orleans 配置是否与 Silo 匹配
3. 确认防火墙设置允许本地连接

### 序列化错误

如果遇到序列化相关错误，可能需要在项目中添加以下 NuGet 包：
- Microsoft.Orleans.Serialization
- Microsoft.Orleans.Serialization.SystemTextJson