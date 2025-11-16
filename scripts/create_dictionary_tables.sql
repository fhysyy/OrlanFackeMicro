-- 创建字典类型表
CREATE TABLE IF NOT EXISTS "DictionaryTypes" (
    "Id" BIGSERIAL PRIMARY KEY,
    "Code" VARCHAR(50) NOT NULL UNIQUE,
    "Name" VARCHAR(100) NOT NULL,
    "Description" VARCHAR(500),
    "IsEnabled" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "SortOrder" INTEGER NOT NULL DEFAULT 0
);

-- 创建字典项表
CREATE TABLE IF NOT EXISTS "DictionaryItems" (
    "Id" BIGSERIAL PRIMARY KEY,
    "DictionaryTypeId" BIGINT NOT NULL REFERENCES "DictionaryTypes"("Id") ON DELETE CASCADE,
    "Value" VARCHAR(100) NOT NULL,
    "Text" VARCHAR(100) NOT NULL,
    "Description" VARCHAR(500),
    "IsEnabled" BOOLEAN NOT NULL DEFAULT TRUE,
    "SortOrder" INTEGER NOT NULL DEFAULT 0,
    "ExtraData" VARCHAR(1000),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 创建字典项唯一索引（同一字典类型下值唯一）
CREATE UNIQUE INDEX IF NOT EXISTS "IX_DictionaryItems_DictionaryTypeId_Value" 
ON "DictionaryItems"("DictionaryTypeId", "Value");

-- 创建字典类型索引
CREATE INDEX IF NOT EXISTS "IX_DictionaryTypes_Code" ON "DictionaryTypes"("Code");
CREATE INDEX IF NOT EXISTS "IX_DictionaryTypes_IsEnabled" ON "DictionaryTypes"("IsEnabled");

-- 创建字典项索引
CREATE INDEX IF NOT EXISTS "IX_DictionaryItems_DictionaryTypeId" ON "DictionaryItems"("DictionaryTypeId");
CREATE INDEX IF NOT EXISTS "IX_DictionaryItems_IsEnabled" ON "DictionaryItems"("IsEnabled");

-- 插入一些示例数据
INSERT INTO "DictionaryTypes" ("Code", "Name", "Description", "IsEnabled", "SortOrder")
VALUES 
    ('USER_STATUS', '用户状态', '系统用户状态字典', TRUE, 1),
    ('MESSAGE_TYPE', '消息类型', '系统消息类型字典', TRUE, 2),
    ('SEX', '性别', '性别类型字典', TRUE, 3)
ON CONFLICT ("Code") DO NOTHING;

-- 插入用户状态字典项
INSERT INTO "DictionaryItems" ("DictionaryTypeId", "Value", "Text", "Description", "IsEnabled", "SortOrder")
SELECT dt."Id", 'Active', '激活', '用户账户处于激活状态', TRUE, 1
FROM "DictionaryTypes" dt
WHERE dt."Code" = 'USER_STATUS'
ON CONFLICT ("DictionaryTypeId", "Value") DO NOTHING;

INSERT INTO "DictionaryItems" ("DictionaryTypeId", "Value", "Text", "Description", "IsEnabled", "SortOrder")
SELECT dt."Id", 'Inactive', '未激活', '用户账户未激活', TRUE, 2
FROM "DictionaryTypes" dt
WHERE dt."Code" = 'USER_STATUS'
ON CONFLICT ("DictionaryTypeId", "Value") DO NOTHING;

INSERT INTO "DictionaryItems" ("DictionaryTypeId", "Value", "Text", "Description", "IsEnabled", "SortOrder")
SELECT dt."Id", 'Locked', '锁定', '用户账户已锁定', TRUE, 3
FROM "DictionaryTypes" dt
WHERE dt."Code" = 'USER_STATUS'
ON CONFLICT ("DictionaryTypeId", "Value") DO NOTHING;

-- 插入性别字典项
INSERT INTO "DictionaryItems" ("DictionaryTypeId", "Value", "Text", "Description", "IsEnabled", "SortOrder")
SELECT dt."Id", 'Male', '男', '男性', TRUE, 1
FROM "DictionaryTypes" dt
WHERE dt."Code" = 'SEX'
ON CONFLICT ("DictionaryTypeId", "Value") DO NOTHING;

INSERT INTO "DictionaryItems" ("DictionaryTypeId", "Value", "Text", "Description", "IsEnabled", "SortOrder")
SELECT dt."Id", 'Female', '女', '女性', TRUE, 2
FROM "DictionaryTypes" dt
WHERE dt."Code" = 'SEX'
ON CONFLICT ("DictionaryTypeId", "Value") DO NOTHING;