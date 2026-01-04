param(
    [string]$TestType = "all",
    [string]$Filter = "",
    [switch]$Verbose,
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
$projectPath = "f:\Orleans\OrlanFackeMicro\src\FakeMicro.Tests\FakeMicro.Tests.csproj"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Orleans 测试运行器" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if (-not $NoBuild) {
    Write-Host "正在构建测试项目..." -ForegroundColor Yellow
    dotnet build $projectPath --configuration Release
    if ($LASTEXITCODE -ne 0) {
        Write-Host "构建失败!" -ForegroundColor Red
        exit 1
    }
    Write-Host "构建成功!" -ForegroundColor Green
    Write-Host ""
}

$testCommand = "dotnet test $projectPath --configuration Release --no-build"

switch ($TestType.ToLower()) {
    "unit" {
        Write-Host "运行单元测试..." -ForegroundColor Yellow
        $testCommand += " --filter ""FullyQualifiedName~UnitTests"""
    }
    "integration" {
        Write-Host "运行集成测试..." -ForegroundColor Yellow
        $testCommand += " --filter ""FullyQualifiedName~IntegrationTests"""
    }
    "api" {
        Write-Host "运行 API 测试..." -ForegroundColor Yellow
        $testCommand += " --filter ""FullyQualifiedName~ApiTests"""
    }
    "all" {
        Write-Host "运行所有测试..." -ForegroundColor Yellow
    }
    default {
        Write-Host "未知的测试类型: $TestType" -ForegroundColor Red
        Write-Host "支持的类型: unit, integration, api, all" -ForegroundColor Yellow
        exit 1
    }
}

if ($Filter) {
    $testCommand += " --filter ""$Filter"""
}

if ($Verbose) {
    $testCommand += " --logger ""console;verbosity=detailed"""
} else {
    $testCommand += " --logger ""console;verbosity=normal"""
}

$testCommand += " --logger ""trx;LogFileName=TestResults.trx"""
$testCommand += " --logger ""html;LogFileName=TestResults.html"""

Write-Host ""
Write-Host "执行命令: $testCommand" -ForegroundColor Gray
Write-Host ""

Invoke-Expression $testCommand

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "测试成功!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "测试报告已生成:" -ForegroundColor Cyan
    Write-Host "  - TestResults.trx (Visual Studio 格式)" -ForegroundColor White
    Write-Host "  - TestResults.html (HTML 格式)" -ForegroundColor White
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "测试失败!" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    exit 1
}
