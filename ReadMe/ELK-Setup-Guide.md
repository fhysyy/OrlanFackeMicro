# ELK Stack 搭建指南

## 当前状态
项目已成功集成ELK Stack日志聚合功能，但需要搭建Elasticsearch服务端才能正常工作。

## 方法一：使用Docker快速搭建（推荐）

### 1. 安装Docker Desktop
- 下载地址：https://www.docker.com/products/docker-desktop
- 安装后启动Docker服务

### 2. 创建Docker Compose文件
创建 `docker-compose.yml` 文件：

```yaml
version: '3.8'
services:
  elasticsearch:
    image: docker.elastic.co/elasticsearch/elasticsearch:8.11.0
    container_name: elasticsearch
    environment:
      - discovery.type=single-node
      - xpack.security.enabled=false
      - "ES_JAVA_OPTS=-Xms512m -Xmx512m"
    ports:
      - "9200:9200"
    volumes:
      - elasticsearch_data:/usr/share/elasticsearch/data

  kibana:
    image: docker.elastic.co/kibana/kibana:8.11.0
    container_name: kibana
    ports:
      - "5601:5601"
    environment:
      - ELASTICSEARCH_HOSTS=http://elasticsearch:9200
    depends_on:
      - elasticsearch

volumes:
  elasticsearch_data:
```

### 3. 启动ELK Stack
```bash
# 在项目根目录执行
docker-compose up -d
```

### 4. 验证服务
- Elasticsearch: http://localhost:9200
- Kibana: http://localhost:5601

## 方法二：Windows本地安装

### 1. 下载Elasticsearch
```bash
# 下载Elasticsearch 8.11.0
Invoke-WebRequest -Uri "https://artifacts.elastic.co/downloads/elasticsearch/elasticsearch-8.11.0-windows-x86_64.zip" -OutFile "elasticsearch-8.11.0.zip"

# 解压到当前目录
Expand-Archive -Path "elasticsearch-8.11.0.zip" -DestinationPath "."
```

### 2. 配置Elasticsearch
编辑 `config/elasticsearch.yml`：
```yaml
cluster.name: fakemicro-cluster
node.name: fakemicro-node
network.host: localhost
http.port: 9200
discovery.type: single-node
xpack.security.enabled: false
```

### 3. 启动Elasticsearch
```bash
cd elasticsearch-8.11.0/bin
.\elasticsearch.bat
```

### 4. 下载Kibana
```bash
# 下载Kibana 8.11.0
Invoke-WebRequest -Uri "https://artifacts.elastic.co/downloads/kibana/kibana-8.11.0-windows-x86_64.zip" -OutFile "kibana-8.11.0.zip"

# 解压
Expand-Archive -Path "kibana-8.11.0.zip" -DestinationPath "."
```

### 5. 配置Kibana
编辑 `config/kibana.yml`：
```yaml
server.port: 5601
server.host: "localhost"
elasticsearch.hosts: ["http://localhost:9200"]
```

### 6. 启动Kibana
```bash
cd kibana-8.11.0/bin
.\kibana.bat
```

## 方法三：使用Chocolatey安装

### 1. 安装Chocolatey（如果未安装）
```powershell
Set-ExecutionPolicy Bypass -Scope Process -Force
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
```

### 2. 安装Elasticsearch和Kibana
```powershell
choco install elasticsearch -y
choco install kibana -y
```

### 3. 配置服务
```powershell
# 启动Elasticsearch服务
Start-Service elasticsearch

# 启动Kibana服务  
Start-Service kibana
```

## 验证ELK Stack集成

### 1. 检查Elasticsearch连接
访问 http://localhost:9200 应该返回：
```json
{
  "name" : "fakemicro-node",
  "cluster_name" : "fakemicro-cluster",
  "version" : {
    "number" : "8.11.0"
  }
}
```

### 2. 检查Kibana
访问 http://localhost:5601 应该显示Kibana界面

### 3. 查看日志索引
在Kibana中创建索引模式：
- 索引模式：`fakemicro-*-logs-*`
- 时间字段：`@timestamp`

### 4. 测试日志记录
重启FakeMicro项目，日志将自动发送到Elasticsearch。

## 故障排除

### 常见问题1：端口冲突
如果9200或5601端口被占用：
```bash
# 查看占用端口的进程
netstat -ano | findstr :9200

# 修改docker-compose.yml中的端口映射
ports:
  - "9201:9200"  # 外部端口:内部端口
```

### 常见问题2：内存不足
Elasticsearch需要足够内存，如果遇到内存错误：
```yaml
# 在docker-compose.yml中调整内存限制
environment:
  - "ES_JAVA_OPTS=-Xms1g -Xmx1g"
```

### 常见问题3：权限问题
在Windows上运行Docker时可能需要管理员权限。

## 下一步操作

1. 选择上述任一方法搭建ELK Stack
2. 重启FakeMicro项目
3. 在Kibana中查看和分析日志
4. 配置日志报警和监控仪表板

搭建完成后，FakeMicro项目的ELK Stack日志聚合功能将完全正常工作。


Changed password for user apm_system
PASSWORD apm_system = lra9g9hjiwlilhH6WaFh

Changed password for user kibana_system
PASSWORD kibana_system = nTDd3oYG45JYWRCHOK8t

Changed password for user kibana
PASSWORD kibana = nTDd3oYG45JYWRCHOK8t

Changed password for user logstash_system
PASSWORD logstash_system = He4ah6X8jLfdYUGj9ZdW

Changed password for user beats_system
PASSWORD beats_system = NzgNcslYujqHGcRc0Ide

Changed password for user remote_monitoring_user
PASSWORD remote_monitoring_user = 4oL39FuNQABiXumUNDHq

Changed password for user elastic
PASSWORD elastic = gIpq8FPihYmSvlwlPOSL