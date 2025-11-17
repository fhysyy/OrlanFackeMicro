-- Orleans PostgreSQL 数据库表创建脚本
-- 适用于 Orleans 9.x 版本

-- 创建 Orleans 存储表
CREATE TABLE IF NOT EXISTS orleansstorage (
    grainidhash integer NOT NULL,
    grainidn0 bigint NOT NULL,
    grainidn1 bigint NOT NULL,
    graintypehash integer NOT NULL,
    graintypestring character varying(512) NOT NULL,
    grainidextensionstring character varying(512),
    serviceid character varying(150) NOT NULL,
    payloadbinary bytea,
    payloadjson text,
    payloadxml text,
    etag integer NOT NULL DEFAULT 0,
    version integer NOT NULL DEFAULT 0,
    modifiedon timestamp without time zone NOT NULL DEFAULT now(),
    createdon timestamp without time zone NOT NULL DEFAULT now(),
    PRIMARY KEY (grainidhash, grainidn0, grainidn1, graintypehash)
);

-- 创建 Orleans 查询表
CREATE TABLE IF NOT EXISTS orleansquery (
    querykey character varying(512) NOT NULL,
    querytext text NOT NULL,
    createdon timestamp without time zone NOT NULL DEFAULT now(),
    modifiedon timestamp without time zone NOT NULL DEFAULT now(),
    PRIMARY KEY (querykey)
);

-- 创建 Orleans 成员表（用于集群成员管理）
CREATE TABLE IF NOT EXISTS orleansmembershiptable (
    deploymentid character varying(150) NOT NULL,
    address character varying(45) NOT NULL,
    port integer NOT NULL,
    generation integer NOT NULL,
    siloname character varying(150) NOT NULL,
    hostname character varying(150) NOT NULL,
    status integer NOT NULL,
    proxyport integer,
    suspecttimes character varying(8000),
    starttime timestamp without time zone NOT NULL,
    iamalivetime timestamp without time zone NOT NULL,
    PRIMARY KEY (deploymentid, address, port, generation)
);

-- 创建 Orleans 成员版本表
CREATE TABLE IF NOT EXISTS orleansmembershipversiontable (
    deploymentid character varying(150) NOT NULL,
    timestamp timestamp without time zone NOT NULL DEFAULT now(),
    version integer NOT NULL,
    PRIMARY KEY (deploymentid)
);

-- 创建 Orleans 重映射表
CREATE TABLE IF NOT EXISTS orleansreminderservice (
    serviceid character varying(150) NOT NULL,
    grainid character varying(150) NOT NULL,
    remintername character varying(150) NOT NULL,
    starttime timestamp without time zone NOT NULL,
    period bigint NOT NULL,
    PRIMARY KEY (serviceid, grainid, remintername)
);

-- 创建索引以优化查询性能
CREATE INDEX IF NOT EXISTS idx_orleansstorage_serviceid ON orleansstorage(serviceid);
CREATE INDEX IF NOT EXISTS idx_orleansstorage_graintypehash ON orleansstorage(graintypehash);
CREATE INDEX IF NOT EXISTS idx_orleansstorage_grainidhash ON orleansstorage(grainidhash);
CREATE INDEX IF NOT EXISTS idx_orleansstorage_modifiedon ON orleansstorage(modifiedon);
CREATE INDEX IF NOT EXISTS idx_orleansmembership_deployment ON orleansmembershiptable(deploymentid);
CREATE INDEX IF NOT EXISTS idx_orleansmembership_iamalive ON orleansmembershiptable(iamalivetime);

-- 插入默认的 Orleans 查询
INSERT INTO orleansquery (querykey, querytext) VALUES 
('UpsertGrainStateKey', 'INSERT INTO orleansstorage (grainidhash, grainidn0, grainidn1, graintypehash, graintypestring, grainidextensionstring, serviceid, payloadbinary, payloadjson, payloadxml, etag, version, modifiedon, createdon) VALUES (@grainidhash, @grainidn0, @grainidn1, @graintypehash, @graintypestring, @grainidextensionstring, @serviceid, @payloadbinary, @payloadjson, @payloadxml, @etag, @version, @modifiedon, @createdon) ON CONFLICT (grainidhash, grainidn0, grainidn1, graintypehash) DO UPDATE SET payloadbinary = EXCLUDED.payloadbinary, payloadjson = EXCLUDED.payloadjson, payloadxml = EXCLUDED.payloadxml, etag = EXCLUDED.etag, version = EXCLUDED.version, modifiedon = EXCLUDED.modifiedon'),
('ReadGrainStateKey', 'SELECT payloadbinary, payloadjson, payloadxml, etag, version FROM orleansstorage WHERE grainidhash = @grainidhash AND grainidn0 = @grainidn0 AND grainidn1 = @grainidn1 AND graintypehash = @graintypehash AND serviceid = @serviceid'),
('ClearGrainStateKey', 'DELETE FROM orleansstorage WHERE grainidhash = @grainidhash AND grainidn0 = @grainidn0 AND grainidn1 = @grainidn1 AND graintypehash = @graintypehash AND serviceid = @serviceid'),
('WriteToStorageKey', 'INSERT INTO orleansstorage (grainidhash, grainidn0, grainidn1, graintypehash, graintypestring, grainidextensionstring, serviceid, payloadbinary, payloadjson, payloadxml, etag, version, modifiedon, createdon) VALUES (@grainidhash, @grainidn0, @grainidn1, @graintypehash, @graintypestring, @grainidextensionstring, @serviceid, @payloadbinary, @payloadjson, @payloadxml, @etag, @version, @modifiedon, @createdon) ON CONFLICT (grainidhash, grainidn0, grainidn1, graintypehash) DO UPDATE SET payloadbinary = EXCLUDED.payloadbinary, payloadjson = EXCLUDED.payloadjson, payloadxml = EXCLUDED.payloadxml, etag = EXCLUDED.etag, version = EXCLUDED.version, modifiedon = EXCLUDED.modifiedon')
ON CONFLICT (querykey) DO UPDATE SET querytext = EXCLUDED.querytext;

-- 显示创建的表信息
SELECT 'Orleans 数据库表创建完成' as status;
SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_name LIKE 'orleans%' ORDER BY table_name;