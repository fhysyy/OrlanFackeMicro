# FakeMicro 项目扩展文档

## 项目概述

FakeMicro 是一个基于 Microsoft Orleans 框架的现代化微服务项目，现已扩展了完整的容器化部署方案和前端管理界面。

## 🚀 新增功能特性

### 1. 容器化部署方案
- **多阶段 Docker 构建**：优化镜像大小和构建效率
- **Docker Compose 编排**：支持开发、测试、生产环境
- **数据库初始化**：自动化的数据库迁移和初始化脚本
- **健康检查**：完整的服务健康监控体系

### 2. 前端管理界面
- **现代化技术栈**：Vue 3 + TypeScript + Element Plus
- **响应式设计**：完美适配桌面和移动设备
- **完整功能模块**：
  - 用户管理（增删改查、权限控制）
  - 消息系统管理
  - 文件管理
  - 系统监控和统计
  - 个人资料设置

### 3. 部署和运维工具
- **自动化构建脚本**：一键构建和部署
- **环境配置管理**：多环境配置文件
- **监控和日志**：集成 ELK Stack 日志系统

## 📁 项目结构（扩展后）

```
FakeMicro/
├── src/                          # 后端源代码
│   ├── FakeMicro.Api/           # API 网关层
│   ├── FakeMicro.Grains/        # Orleans Grains
│   ├── FakeMicro.Silo/          # Orleans Silo
│   ├── FakeMicro.Interfaces/    # 接口定义
│   ├── FakeMicro.Entities/      # 数据实体
│   ├── FakeMicro.DatabaseAccess/# 数据访问层
│   └── FakeMicro.Utilities/     # 工具类库
├── frontend/                    # 前端管理界面
│   ├── src/
│   │   ├── components/         # 可复用组件
│   │   ├── views/              # 页面组件
│   │   ├── stores/             # 状态管理
│   │   ├── services/           # API 服务
│   │   ├── router/             # 路由配置
│   │   ├── types/              # TypeScript 类型定义
│   │   └── utils/              # 工具函数
│   ├── public/                 # 静态资源
│   └── dist/                   # 构建输出
├── scripts/                    # 部署和运维脚本
│   ├── deploy.sh              # 完整部署脚本
│   ├── build-frontend.sh      # 前端构建脚本
│   └── init-db.sql            # 数据库初始化
├── docker-compose.yml         # 生产环境编排
├── docker-compose.dev.yml     # 开发环境编排
└── Dockerfile                 # 多阶段构建配置
```

## 🛠 快速开始

### 1. 环境要求
- Docker & Docker Compose
- Node.js 16+ & npm
- .NET 9.0 SDK

### 2. 一键启动（开发环境）
```bash
# 克隆项目
git clone <repository-url>
cd FakeMicro

# 启动所有服务
docker-compose -f docker-compose.dev.yml up -d

# 访问服务
# 前端管理界面: http://localhost:3000
# API 文档: http://localhost:5000/swagger
# 数据库管理: http://localhost:8080 (Adminer)
```

### 3. 生产环境部署
```bash
# 构建和部署
./scripts/deploy.sh production

# 或者使用 Docker Compose
docker-compose up -d
```

## 🔧 详细配置

### 前端配置
```bash
cd frontend

# 安装依赖
npm install

# 开发环境运行
npm run dev

# 构建生产版本
npm run build

# 代码检查
npm run lint
```

### 后端配置
```bash
# 恢复 NuGet 包
dotnet restore

# 运行 API 服务
cd src/FakeMicro.Api
dotnet run

# 运行 Silo 服务  
cd src/FakeMicro.Silo
dotnet run
```

## 📊 功能模块详解

### 用户管理模块
- **用户列表**：分页显示、搜索过滤
- **用户操作**：创建、编辑、删除、启用/禁用
- **权限管理**：角色分配、权限控制
- **登录历史**：登录时间、IP 地址记录

### 消息系统模块
- **消息列表**：按状态筛选、批量操作
- **消息发送**：模板选择、变量替换
- **发送记录**：成功/失败统计、重试机制
- **模板管理**：消息模板的增删改查

### 文件管理模块
- **文件列表**：按类型、大小、时间排序
- **文件操作**：上传、下载、删除、预览
- **存储统计**：空间使用情况、文件类型分布
- **权限控制**：文件访问权限管理

### 系统监控模块
- **实时统计**：用户活跃度、消息发送量
- **性能监控**：API 响应时间、错误率
- **资源使用**：CPU、内存、磁盘使用情况
- **日志查看**：实时日志流、错误追踪

## 🔒 安全特性

### 认证授权
- JWT Token 认证
- 角色基于权限控制
- Token 自动刷新机制
- 登录失败限制

### 数据安全
- 密码加密存储（HMACSHA512）
- API 请求签名验证
- SQL 注入防护
- XSS 攻击防护

### 网络安全
- HTTPS 强制启用
- CORS 跨域配置
- 请求频率限制
- IP 白名单控制

## 📈 监控和日志

### 应用监控
- 健康检查端点：`/health`
- 性能指标：响应时间、吞吐量
- 错误率监控：异常捕获和统计

### 日志系统
- 结构化日志记录
- ELK Stack 集成
- 日志级别控制
- 实时日志查询

## 🚀 性能优化

### 前端优化
- 代码分割和懒加载
- 图片压缩和缓存
- API 请求合并
- 本地存储优化

### 后端优化
- Orleans 分布式缓存
- 数据库连接池
- 异步编程模型
- 响应压缩

## 🤝 贡献指南

### 开发流程
1. Fork 项目仓库
2. 创建功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 创建 Pull Request

### 代码规范
- 使用 TypeScript 严格模式
- 遵循 ESLint 规则
- 编写单元测试
- 更新相关文档

## 📞 支持与反馈

### 问题报告
如果您遇到问题或有建议，请通过以下方式联系我们：
- [创建 Issue](<issue-link>)
- 发送邮件至：support@fakemicro.com

### 社区交流
- 官方文档：<docs-link>
- 技术博客：<blog-link>
- 社区论坛：<forum-link>

## 📄 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。

## 🙏 致谢

感谢以下开源项目的支持：
- [Microsoft Orleans](https://github.com/dotnet/orleans)
- [Vue.js](https://vuejs.org/)
- [Element Plus](https://element-plus.org/)
- [Docker](https://www.docker.com/)

---

**FakeMicro Team** © 2024