# PowerShell脚本用于批量重构Grain文件

# 定义要重构的Grain文件列表
$grainFiles = @(
    @{File="AuthGrain.cs"; BaseClass="BaseGrain"; NewBaseClass="OrleansGrainBase"},
    @{File="StudentGrain.cs"; BaseClass="BaseGrain"; NewBaseClass="OrleansGrainBase"},
    @{File="ScoreGrain.cs"; BaseClass="BaseGrain"; NewBaseClass="OrleansGrainBase"},
    @{File="SysOpenGrain.cs"; BaseClass="BaseGrain"; NewBaseClass="OrleansGrainBase"},
    @{File="ClassManagerGrain.cs"; BaseClass="BaseGrain"; NewBaseClass="OrleansGrainBase"},
    @{File="FileGrain.cs"; BaseClass="OrleansOptimizedBaseGrain"; NewBaseClass="OrleansGrainBase"}
)

# 根目录
$srcPath = "f:\Orleans\Orleans\src\FakeMicro.Grains"

foreach ($grain in $grainFiles) {
    $filePath = Join-Path $srcPath $grain.File
    
    if (Test-Path $filePath) {
        Write-Host "重构文件: $($grain.File)" -ForegroundColor Yellow
        
        # 读取文件内容
        $content = Get-Content $filePath -Raw
        
        # 替换基类
        $content = $content -replace "$($grain.BaseClass)", "$($grain.NewBaseClass)"
        
        # 更新构造函数（从ILoggerService到ILogger<T>）
        $content = $content -replace "ILoggerService", "ILogger<$($grain.File.Replace('.cs',''))>"
        
        # 写入文件
        Set-Content -Path $filePath -Value $content
        
        Write-Host "✅ 完成: $($grain.File)" -ForegroundColor Green
    } else {
        Write-Host "❌ 文件不存在: $($grain.File)" -ForegroundColor Red
    }
}

Write-Host "所有Grain重构完成！" -ForegroundColor Cyan