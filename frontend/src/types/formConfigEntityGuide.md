# 表单配置实体类设计指南

## 设计背景

在分析现有的表单配置系统后，发现了以下问题需要解决：

1. **类型不一致**：前端使用的接口类型与后端存储的数据模型存在差异
2. **字段命名混淆**：如之前出现的 `id` 和 `formId` 混淆使用问题
3. **层级结构复杂**：现有表单配置嵌套层级过深，不便于数据库存储和查询
4. **可扩展性不足**：缺乏对表单字段、分组和条件逻辑的独立管理

## 新增实体类设计思路

### 1. 规范化数据模型

- **统一ID命名**：使用 `id` 作为所有实体的主键，使用 `formConfigId` 表示关联关系
- **使用枚举类型**：定义明确的 `FormConfigStatus` 和 `FormType` 枚举
- **清晰的层级关系**：将表单配置拆分为多个独立实体，通过外键关联

### 2. 数据库友好设计

- **扁平化复杂配置**：将复杂的配置对象存储为 JSON 格式
- **支持关联查询**：添加适当的外键关系支持级联操作
- **添加排序字段**：增加 `order` 字段支持自定义排序

### 3. 前后端分离适配

- **提供 DTO 类型**：创建专门的请求和响应类型
- **保留扩展字段**：使用 `Record<string, any>` 支持未来扩展
- **时间戳管理**：统一的 `createdAt` 和 `updatedAt` 字段

## 核心实体类说明

### FormConfigEntity

**主要功能**：表单配置的核心实体，包含表单的基本信息和元数据。

**设计亮点**：
- 移除了之前存在的 `formId` 字段，统一使用 `id`
- 将复杂配置（如布局、提交等）改为 JSON 格式存储
- 添加了与字段和分组的关联关系

**适用场景**：
- 表单配置的创建、更新和查询
- 作为表单设计器的数据基础

### FormFieldEntity

**主要功能**：表示表单中的单个字段，支持各种字段类型。

**设计亮点**：
- 所有特定类型字段的配置统一存储在 `config` JSON 字段中
- 明确的 `formConfigId` 外键关联
- 支持字段级别的验证规则存储
- 添加了 `order` 字段支持自定义排序

**适用场景**：
- 动态生成表单字段
- 字段级别的权限控制和验证

### FormGroupEntity

**主要功能**：用于对表单字段进行分组管理。

**设计亮点**：
- 独立的实体设计，便于管理分组关系
- 支持分组的折叠/展开状态
- 添加了排序支持

**适用场景**：
- 复杂表单的分组展示
- 多步骤表单的页面划分

### FormConditionalLogicEntity

**主要功能**：管理表单的条件逻辑规则。

**设计亮点**：
- 独立存储条件逻辑，便于维护和扩展
- 支持多种触发条件和执行动作
- 可针对多个目标字段执行动作

**适用场景**：
- 表单的动态显示/隐藏逻辑
- 基于用户输入的动态验证规则

## 与现有代码的集成建议

### 1. 服务层改造

```typescript
// 示例：formConfigService.ts 改造
import type { FormConfigEntity, FormConfigCreateEntity, FormConfigUpdateEntity } from '../types/formConfigEntity';

export const formConfigService = {
  // 创建表单配置
  async createFormConfig(data: FormConfigCreateEntity): Promise<FormConfigEntity> {
    // 实现逻辑
  },
  
  // 其他方法类似改造
}
```

### 2. 组件层适配

```typescript
// 示例：FormConfigManagement.vue 适配
import type { FormConfigEntity, FormConfigListItemEntity } from '../types/formConfigEntity';

// 使用新的实体类型替换原有类型
```

### 3. 数据转换工具

建议创建数据转换工具，用于在新旧类型之间进行转换：

```typescript
// formConfigConverter.ts
export const formConfigConverter = {
  // 将旧类型转换为新实体类型
  toEntity(legacyConfig: any): FormConfigEntity {
    // 转换逻辑
  },
  
  // 将新实体类型转换为前端展示需要的格式
  toViewFormat(entity: FormConfigEntity): any {
    // 转换逻辑
  }
}
```

## 数据库设计建议

针对 PostgreSQL 数据库，建议以下表结构设计：

### 1. form_configs 表

```sql
CREATE TABLE form_configs (
  id VARCHAR(36) PRIMARY KEY,
  name VARCHAR(255) NOT NULL,
  description TEXT,
  type VARCHAR(50) DEFAULT 'default',
  version VARCHAR(50) NOT NULL,
  status VARCHAR(50) DEFAULT 'draft',
  enabled BOOLEAN DEFAULT TRUE,
  module VARCHAR(100),
  tags JSONB,
  layout_config JSONB,
  submit_config JSONB,
  validation_config JSONB,
  style_config JSONB,
  advanced_config JSONB,
  created_by VARCHAR(100),
  updated_by VARCHAR(100),
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(name, version)
);

CREATE INDEX idx_form_configs_status ON form_configs(status);
CREATE INDEX idx_form_configs_module ON form_configs(module);
CREATE INDEX idx_form_configs_created_by ON form_configs(created_by);
```

### 2. form_fields 表

```sql
CREATE TABLE form_fields (
  id VARCHAR(36) PRIMARY KEY,
  form_config_id VARCHAR(36) NOT NULL REFERENCES form_configs(id) ON DELETE CASCADE,
  group_id VARCHAR(36),
  prop VARCHAR(100) NOT NULL,
  label VARCHAR(255) NOT NULL,
  type VARCHAR(50) NOT NULL,
  initial_value JSONB,
  placeholder VARCHAR(255),
  required BOOLEAN DEFAULT FALSE,
  disabled BOOLEAN DEFAULT FALSE,
  hidden BOOLEAN DEFAULT FALSE,
  help_text TEXT,
  span INTEGER DEFAULT 24,
  "order" INTEGER DEFAULT 0,
  config JSONB NOT NULL DEFAULT '{}',
  validation_rules JSONB,
  options JSONB,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(form_config_id, prop)
);

CREATE INDEX idx_form_fields_form_config_id ON form_fields(form_config_id);
CREATE INDEX idx_form_fields_group_id ON form_fields(group_id);
```

### 3. form_groups 表

```sql
CREATE TABLE form_groups (
  id VARCHAR(36) PRIMARY KEY,
  form_config_id VARCHAR(36) NOT NULL REFERENCES form_configs(id) ON DELETE CASCADE,
  title VARCHAR(255) NOT NULL,
  description TEXT,
  collapsible BOOLEAN DEFAULT FALSE,
  expanded BOOLEAN DEFAULT TRUE,
  "order" INTEGER DEFAULT 0,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_form_groups_form_config_id ON form_groups(form_config_id);
```

### 4. form_conditional_logics 表

```sql
CREATE TABLE form_conditional_logics (
  id VARCHAR(36) PRIMARY KEY,
  form_config_id VARCHAR(36) NOT NULL REFERENCES form_configs(id) ON DELETE CASCADE,
  trigger_field VARCHAR(100) NOT NULL,
  trigger_value JSONB,
  condition VARCHAR(50) NOT NULL,
  action VARCHAR(50) NOT NULL,
  target_fields JSONB NOT NULL,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_form_conditional_logics_form_config_id ON form_conditional_logics(form_config_id);
CREATE INDEX idx_form_conditional_logics_trigger_field ON form_conditional_logics(trigger_field);
```

## 最佳实践建议

1. **使用 UUID 作为主键**：确保分布式系统中的唯一性
2. **事务处理**：创建或更新表单配置时，使用事务确保数据一致性
3. **版本管理**：实现表单配置的版本控制，避免直接修改已发布的表单
4. **缓存优化**：对常用的表单配置实施缓存策略，提高性能
5. **权限控制**：基于 `createdBy` 和 `module` 字段实现细粒度的权限控制
6. **数据迁移**：提供从旧数据模型到新实体类的迁移工具

## 总结

新增的实体类设计解决了之前存在的类型不一致和字段混淆问题，提供了更加清晰、规范的数据模型，同时保持了良好的扩展性和数据库兼容性。通过合理的表结构设计和索引优化，可以确保表单配置系统的高性能和稳定性。