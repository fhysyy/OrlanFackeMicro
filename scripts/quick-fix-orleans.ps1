# Orleans 数据库快速修复脚本
# 适用于没有Docker的环境，直接连接到本地PostgreSQL

Write-Host "=== Orleans 数据库快速修复 ===" -ForegroundColor Green

# 检查 PostgreSQL 是否在运行
Write-Host "检查 PostgreSQL 服务状态..." -ForegroundColor Cyan

# 尝试连接到PostgreSQL
try {
    # 使用 .NET 的 Npgsql 连接
    Add-Type -Path "Npgsql.dll" -ErrorAction SilentlyContinue
    
    # 尝试连接字符串
    $connectionString = "Server=localhost;Port=5432;Database=postgres;User Id=postgres;Password=123456"
    
    Write-Host "尝试连接到 PostgreSQL..." -ForegroundColor Yellow
    
    # 使用 psql 命令行工具检查连接
    $psqlTest = & "psql" "-h" "localhost" "-p" "5432" "-U" "postgres" "-d" "postgres" "-c" "SELECT version();" 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "PostgreSQL 连接成功！" -ForegroundColor Green
        Write-Host $psqlTest -ForegroundColor Cyan
    } else {
        Write-Host "PostgreSQL 连接失败，请检查服务是否启动" -ForegroundColor Red
        Write-Host "错误信息: $psqlTest" -ForegroundColor Yellow
        
        # 提供手动启动PostgreSQL的指南
        Write-Host "`n=== 手动启动 PostgreSQL 指南 ===" -ForegroundColor Yellow
        Write-Host "1. 下载并安装 PostgreSQL: https://www.postgresql.org/download/" -ForegroundColor White
        Write-Host "2. 启动 PostgreSQL 服务" -ForegroundColor White
        Write-Host "3. 创建 orleans 数据库: CREATE DATABASE orleans;" -ForegroundColor White
        Write-Host "4. 运行 init-orleans-db.sql 脚本初始化表结构" -ForegroundColor White
        exit 1
    }
    
} catch {
    Write-Host "连接测试失败: $($_.Exception.Message)" -ForegroundColor Red
}

# 检查 orleans 数据库是否存在
Write-Host "检查 orleans 数据库..." -ForegroundColor Cyan
$dbCheck = & "psql" "-h" "localhost" "-p" "5432" "-U" "postgres" "-d" "postgres" "-c" "SELECT 1 FROM pg_database WHERE datname = 'orleans';" 2>&1

if ($LASTEXITCODE -eq 0 -and $dbCheck -match "1") {
    Write-Host "orleans 数据库存在" -ForegroundColor Green
    
    # 运行修复脚本
    Write-Host "执行字段名修复..." -ForegroundColor Yellow
    
    # 修复 OrleansMembershipVersionTable 字段名
    $fixTimestamp = & "psql" "-h" "localhost" "-p" "5432" "-U" "postgres" "-d" "orleans" "-c" "ALTER TABLE IF EXISTS OrleansMembershipVersionTable RENAME COLUMN \"Timestamp\" TO timestamp;" 2>&1
    $fixVersion = & "psql" "-h" "localhost" "-p" "5432" "-U" "postgres" "-d" "orleans" "-c" "ALTER TABLE IF EXISTS OrleansMembershipVersionTable RENAME COLUMN \"Version\" TO version;" 2>&1
    
    Write-Host "字段名修复完成" -ForegroundColor Green
    
} else {
    Write-Host "orleans 数据库不存在，需要初始化" -ForegroundColor Yellow
    
    # 创建数据库
    Write-Host "创建 orleans 数据库..." -ForegroundColor Yellow
    $createDb = & "psql" "-h" "localhost" "-p" "5432" "-U" "postgres" "-d" "postgres" "-c" "CREATE DATABASE orleans;" 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "数据库创建成功" -ForegroundColor Green
        
        # 运行初始化脚本
        Write-Host "初始化 Orleans 表结构..." -ForegroundColor Yellow
        $initScript = Get-Content "scripts/init-orleans-db.sql" -Raw
        $initResult = & "psql" "-h" "localhost" "-p" "5432" "-U" "postgres" "-d" "orleans" "-c" $initScript 2>&1
        
        Write-Host "Orleans 数据库初始化完成" -ForegroundColor Green
    } else {
        Write-Host "数据库创建失败: $createDb" -ForegroundColor Red
    }
}

# 提供下一步操作指南
Write-Host "`n=== 修复完成，下一步操作 ===" -ForegroundColor Green
Write-Host "1. Orleans Silo 现在使用内存存储，可以正常启动" -ForegroundColor White
Write-Host "2. 要使用 PostgreSQL 存储，请确保字段名修复完成" -ForegroundColor White
Write-Host "3. 启动 Orleans Silo: dotnet run --project src/FakeMicro.Silo" -ForegroundColor White
Write-Host "4. 启动 API: dotnet run --project src/FakeMicro.Api" -ForegroundColor White

Write-Host "`n=== 快速启动命令 ===" -ForegroundColor Yellow
Write-Host "# 启动 Orleans Silo" -ForegroundColor Cyan
Write-Host "cd src/FakeMicro.Silo && dotnet run" -ForegroundColor White

Write-Host "# 启动 API (新终端)" -ForegroundColor Cyan  
Write-Host "cd src/FakeMicro.Api && dotnet run" -ForegroundColor White

Write-Host "`n=== 完成 ===" -ForegroundColor Green