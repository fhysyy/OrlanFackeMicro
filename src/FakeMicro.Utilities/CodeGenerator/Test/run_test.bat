@echo off
echo === 编译测试程序 ===
cd /d "f:\ProjectCode\OrlanFackeMicro\src\FakeMicro.Utilities\CodeGenerator"

echo 正在编译...
csc /target:exe /out:ManualTest.exe /reference:System.dll /reference:System.Core.dll Test\ManualTest.cs Templates\EntityTemplate.cs Templates\InterfaceTemplate.cs Templates\ControllerTemplate.cs EntityMetadata.cs Types.cs CodeGeneratorConfiguration.cs 2>nul

if %errorlevel% equ 0 (
    echo ✅ 编译成功!
    echo.
    echo === 运行测试 ===
    ManualTest.exe
) else (
    echo ❌ 编译失败，请检查代码错误
)

pause