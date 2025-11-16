# FakeMicro 项目贡献指南

感谢您对 FakeMicro 项目的关注！我们欢迎任何形式的贡献，包括但不限于代码提交、文档改进、问题报告和功能建议。

## 🎯 贡献方式

### 1. 报告问题
如果您发现 bug 或有功能建议，请通过以下方式报告：
- 创建 [GitHub Issue](issue-link)
- 描述清晰的问题现象和复现步骤
- 提供相关日志或截图

### 2. 代码贡献
如果您想贡献代码，请遵循以下流程：

#### 开发环境搭建
```bash
# 1. 克隆项目
git clone <repository-url>
cd FakeMicro

# 2. 启动开发环境
docker-compose -f docker-compose.dev.yml up -d

# 3. 前端开发
cd frontend
npm install
npm run dev

# 4. 后端开发
# API 服务运行在 http://localhost:5000
# Silo 服务运行在 http://localhost:8080
```

#### 代码提交规范
我们使用 Conventional Commits 规范：
- `feat`: 新功能
- `fix`: bug 修复
- `docs`: 文档更新
- `style`: 代码格式调整
- `refactor`: 代码重构
- `test`: 测试相关
- `chore`: 构建工具或依赖更新

示例：
```bash
git commit -m "feat: 添加用户管理功能"
git commit -m "fix: 修复登录页面样式问题"
```

### 3. 文档改进
文档是项目的重要组成部分，欢迎改进：
- 修正拼写和语法错误
- 补充使用示例
- 添加架构说明
- 完善 API 文档

## 📋 开发规范

### 代码风格
- **后端**: 遵循 .NET 编码规范
- **前端**: 使用 ESLint + Prettier
- **TypeScript**: 启用严格模式
- **命名**: 使用有意义的英文名称

### 提交前检查
在提交代码前，请确保：
```bash
# 前端代码检查
cd frontend
npm run lint
npm run build

# 后端代码检查
dotnet build
dotnet test
```

### 分支管理
- `main`: 主分支，稳定版本
- `develop`: 开发分支
- `feature/*`: 功能分支
- `hotfix/*`: 热修复分支
- `release/*`: 发布分支

## 🧪 测试要求

### 单元测试
- 后端: 使用 xUnit 框架
- 前端: 使用 Vitest 框架
- 覆盖率要求: > 80%

### 集成测试
- API 接口测试
- 数据库操作测试
- 端到端测试

## 🔧 技术栈要求

### 后端技术栈
- .NET 9.0
- Orleans 9.2.1
- Entity Framework Core
- PostgreSQL
- Redis
- JWT 认证

### 前端技术栈
- Vue 3 + TypeScript
- Element Plus
- Pinia + Vue Router
- Vite + ESLint
- Axios

### 基础设施
- Docker + Docker Compose
- ELK Stack (日志)
- Hangfire (任务调度)
- CAP (事件总线)

## 📖 文档规范

### 代码注释
```csharp
/// <summary>
/// 用户服务接口
/// </summary>
public interface IUserService
{
    /// <summary>
    /// 根据ID获取用户信息
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <returns>用户信息</returns>
    Task<User> GetUserByIdAsync(string id);
}
```

```typescript
/**
 * 用户登录函数
 * @param username - 用户名
 * @param password - 密码
 * @returns 登录结果
 */
async function login(username: string, password: string): Promise<LoginResult> {
  // 实现代码
}
```

### README 文档
每个模块都应包含：
- 功能描述
- 使用示例
- API 说明
- 配置说明

## 🚀 发布流程

### 版本管理
我们使用语义化版本控制：
- `主版本.次版本.修订版本`
- 示例: `1.2.3`

### 发布检查清单
- [ ] 所有测试通过
- [ ] 代码审查完成
- [ ] 文档更新完成
- [ ] 版本号更新
- [ ] 变更日志更新

## 🤝 行为准则

### 沟通准则
- 保持友好和尊重的态度
- 使用清晰明确的语言
- 及时回复问题和评论
- 尊重不同的观点和经验

### 代码审查
- 审查者应提供建设性反馈
- 提交者应积极回应审查意见
- 重点关注代码质量和可维护性

## 🎁 贡献者权益

### 贡献者名单
所有贡献者将被记录在 [CONTRIBUTORS.md](CONTRIBUTORS.md) 文件中。

### 特殊贡献
对于重大贡献，我们将：
- 在发布说明中特别感谢
- 邀请参与项目决策
- 提供技术支持和指导

## 📞 联系方式

### 技术讨论
- **GitHub Discussions**: [讨论区链接]
- **Slack 频道**: [Slack 邀请链接]
- **邮件列表**: dev@fakemicro.com

### 核心团队
- **项目负责人**: [负责人姓名]
- **技术负责人**: [技术负责人姓名]
- **社区经理**: [社区经理姓名]

## 🙏 致谢

感谢所有为 FakeMicro 项目做出贡献的开发者！您的每一份贡献都让项目变得更好。

---

**最后更新**: 2024-10-19  
**版本**: v1.0.0