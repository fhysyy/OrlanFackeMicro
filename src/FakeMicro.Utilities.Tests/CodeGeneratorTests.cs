using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FakeMicro.Entities;
using FakeMicro.Utilities.CodeGenerator;
using Microsoft.Extensions.Logging;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

// 为解决命名空间冲突，使用别名
using CodeGeneratorClass = FakeMicro.Utilities.CodeGenerator.CodeGenerator;

namespace FakeMicro.Utilities.Tests
{
    /// <summary>
    /// 代码生成器单元测试
    /// </summary>
    public class CodeGeneratorTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly CodeGeneratorClass _codeGenerator;
        private readonly string _testOutputPath;

        public CodeGeneratorTests(ITestOutputHelper output)
        {
            _output = output;
            
            // 创建测试输出路径
            _testOutputPath = Path.Combine(Path.GetTempPath(), "TestOutput");
            
            // 创建代码生成器实例，使用默认配置
            _codeGenerator = new CodeGeneratorClass(
                configuration: new CodeGeneratorConfiguration(),
                outputPath: _testOutputPath,
                overwriteStrategy: OverwriteStrategy.Backup
            );
        }

        [Fact]
        public async Task GenerateCodeAsync_WithValidEntity_ShouldGenerateAllCodeTypes()
        {
            // Arrange
            var properties = new List<PropertyMetadata>
            {
                new PropertyMetadata { Name = "Id", Type = "int", IsPrimaryKey = true },
                new PropertyMetadata { Name = "Name", Type = "string" }
            };
            var entity = _codeGenerator.CreateEntityMetadata("Product", properties);
            var entities = new List<EntityMetadata> { entity };
            var types = GenerationType.All;

            // Act
            var result = await _codeGenerator.GenerateCodeAsync(entities, types);

            // Assert
            Assert.True(result.IsSuccess, $"代码生成失败: {result.ErrorMessage}");
            Assert.Equal(4, result.GeneratedFiles.Count); // Interface, Grain, Dto, Controller
            Assert.Empty(result.Warnings);
            Assert.Equal(GeneratorErrorType.None, result.ErrorType);

            // 验证文件内容
            await VerifyGeneratedFiles(result, "Product");
        }

        [Fact]
        public async Task GenerateCodeAsync_WithSpecificTypes_ShouldGenerateOnlySpecifiedTypes()
        {
            // Arrange
            var properties = new List<PropertyMetadata>
            {
                new PropertyMetadata { Name = "Id", Type = "int", IsPrimaryKey = true },
                new PropertyMetadata { Name = "Name", Type = "string" }
            };
            var entity = _codeGenerator.CreateEntityMetadata("User", properties);
            var entities = new List<EntityMetadata> { entity };
            var types = GenerationType.Interface | GenerationType.Grain;

            // Act
            var result = await _codeGenerator.GenerateCodeAsync(entities, types);

            // Assert
            Assert.True(result.IsSuccess, $"代码生成失败: {result.ErrorMessage}");
            Assert.Equal(2, result.GeneratedFiles.Count);
            
            var interfaceFile = result.GeneratedFiles.FirstOrDefault(f => f.Contains("IUserGrain.cs"));
            var grainFile = result.GeneratedFiles.FirstOrDefault(f => f.Contains("UserGrain.cs"));
            
            Assert.NotNull(interfaceFile);
            Assert.NotNull(grainFile);
            Assert.True(File.Exists(interfaceFile));
            Assert.True(File.Exists(grainFile));
        }

        [Fact]
        public async Task GenerateCodeAsync_WithInvalidEntityName_ShouldNotReturnError()
        {
            // Arrange
            var properties = new List<PropertyMetadata>
            {
                new PropertyMetadata { Name = "Id", Type = "int", IsPrimaryKey = true }
            };
            var entity = _codeGenerator.CreateEntityMetadata("NonExistentEntity", properties);
            var entities = new List<EntityMetadata> { entity };
            var types = GenerationType.All;

            // Act
            var result = await _codeGenerator.GenerateCodeAsync(entities, types);

            // Assert
            // 在实际的代码生成器中，只要提供了EntityMetadata，即使实体名称不存在也会生成代码
            Assert.True(result.IsSuccess);
            Assert.Equal(4, result.GeneratedFiles.Count);
        }

        [Fact]
        public async Task GenerateCodeAsync_WithEmptyEntityList_ShouldReturnEmpty()
        {
            // Arrange
            var entities = new List<EntityMetadata>();

            // Act
            var result = await _codeGenerator.GenerateCodeAsync(entities, GenerationType.All);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.GeneratedFiles);
        }

        [Fact]
        public async Task PreviewCodeAsync_WithValidEntity_ShouldReturnValidPreview()
        {
            // Arrange
            var properties = new List<PropertyMetadata>
            {
                new PropertyMetadata { Name = "Id", Type = "int", IsPrimaryKey = true },
                new PropertyMetadata { Name = "Name", Type = "string" }
            };
            var entity = _codeGenerator.CreateEntityMetadata("Product", properties);
            var previewType = GenerationType.Grain;

            // Act
            var preview = await _codeGenerator.PreviewCodeAsync(entity, previewType);

            // Assert
            Assert.NotNull(preview);
            Assert.Contains(GenerationType.Grain, preview);
            Assert.NotEmpty(preview[GenerationType.Grain]);
            Assert.Contains("ProductGrain", preview[GenerationType.Grain]);
            Assert.Contains("IGrainWithGuidKey", preview[GenerationType.Grain]);
            Assert.Contains("Product", preview[GenerationType.Grain]);
        }

        [Theory]
        [InlineData("Product", "产品")]
        [InlineData("User", "用户")]
        public async Task GenerateCodeAsync_WithDifferentEntities_ShouldGenerateCorrectCode(string entityName, string expectedDescription)
        {
            // Arrange
            var properties = new List<PropertyMetadata>
            {
                new PropertyMetadata { Name = "Id", Type = "int", IsPrimaryKey = true }
            };
            var entity = _codeGenerator.CreateEntityMetadata("NonExistentEntity", properties);
            var entities = new List<EntityMetadata> { entity };

            // Act
            var result = await _codeGenerator.GenerateCodeAsync(entities, GenerationType.All);

            // Assert
            Assert.True(result.IsSuccess);
            
            var interfaceFile = result.GeneratedFiles.FirstOrDefault(f => f.Contains($"I{entityName}Grain.cs"));
            Assert.NotNull(interfaceFile);
            
            var interfaceContent = await File.ReadAllTextAsync(interfaceFile);
            Assert.Contains(expectedDescription, interfaceContent);
        }

        [Fact]
        public async Task GenerateCodeAsync_WithMultipleEntities_ShouldGenerateForAll()
        {
            // Arrange
            var properties1 = new List<PropertyMetadata>
            {
                new PropertyMetadata { Name = "Id", Type = "int", IsPrimaryKey = true },
                new PropertyMetadata { Name = "Name", Type = "string" }
            };
            var entity1 = _codeGenerator.CreateEntityMetadata("Product", properties1);
            
            var properties2 = new List<PropertyMetadata>
            {
                new PropertyMetadata { Name = "Id", Type = "int", IsPrimaryKey = true },
                new PropertyMetadata { Name = "UserName", Type = "string" }
            };
            var entity2 = _codeGenerator.CreateEntityMetadata("User", properties2);
            
            var entities = new List<EntityMetadata> { entity1, entity2 };

            // Act
            var result = await _codeGenerator.GenerateCodeAsync(entities, GenerationType.All);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(8, result.GeneratedFiles.Count); // 2 entities × 4 file types each
            Assert.Contains("ProductGrain", string.Join(" ", result.GeneratedFiles));
            Assert.Contains("UserGrain", string.Join(" ", result.GeneratedFiles));
        }

        private async Task VerifyGeneratedFiles(CodeGenerationResult result, string entityName)
        {
            foreach (var filePath in result.GeneratedFiles)
            {
                Assert.True(File.Exists(filePath), $"文件不存在: {filePath}");
                
                var content = await File.ReadAllTextAsync(filePath);
                Assert.NotEmpty(content);
                Assert.Contains(entityName, content);

                _output.WriteLine($"✓ 验证文件: {Path.GetFileName(filePath)}");
                _output.WriteLine($"  大小: {content.Length} 字符");
                _output.WriteLine($"  包含实体名: {content.Contains(entityName)}");
            }
        }

        void IDisposable.Dispose()
        {
            // 清理测试文件
            var tempDir = Path.Combine(Path.GetTempPath(), "TestOutput");
            if (Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                    // 忽略清理错误
                }
            }
        }
    }

    /// <summary>
    /// 模板生成器测试
    /// </summary>
    public class TemplateTests
    {
        private readonly ITestOutputHelper _output;
        private readonly EntityMetadata _testMetadata;

        public TemplateTests(ITestOutputHelper output)
        {
            _output = output;
            _testMetadata = new EntityMetadata
            {
                EntityName = "TestEntity",
                EntityDescription = "测试实体",
                Namespace = "Test.Namespace",
                PrimaryKeyProperty = "Id",
                PrimaryKeyType = "int",
                Properties = new()
                {
                    new() { Name = "Id", Type = "int", IsPrimaryKey = true },
                    new() { Name = "Name", Type = "string" },
                    new() { Name = "CreatedAt", Type = "DateTime" }
                },
                IsAuditable = true,
                TableName = "TestEntities"
            };
        }

        [Fact]
        public void InterfaceTemplate_ShouldGenerateCorrectInterface()
        {
            // Act
            var code = global::FakeMicro.Utilities.CodeGenerator.Templates.InterfaceTemplate.Generate(_testMetadata);

            // Assert
            Assert.Contains("interface ITestEntityGrain", code);
            Assert.Contains("Task<TestEntityDto> CreateTestEntityAsync", code);
            Assert.Contains("Task<TestEntityDto?> GetTestEntityAsync", code);
            Assert.Contains("Task<TestEntityDto> UpdateTestEntityAsync", code);
            Assert.Contains("Task<bool> DeleteTestEntityAsync", code);
        }

        [Fact]
        public void GrainTemplate_ShouldGenerateCorrectGrain()
        {
            // Arrange
            var template = new global::FakeMicro.Utilities.CodeGenerator.Templates.GrainTemplate();

            // Act
            var code = template.Generate(_testMetadata);

            // Assert
            Assert.Contains("class TestEntityGrain", code);
            Assert.Contains("ITestEntityGrain", code);
            Assert.Contains("TestEntity", code);
        }

        [Fact]
        public void DtoTemplate_ShouldGenerateCorrectDtos()
        {
            // Act
            var code = global::FakeMicro.Utilities.CodeGenerator.Templates.DtoTemplate.Generate(_testMetadata);

            // Assert
            Assert.Contains("class TestEntityDto", code);
            Assert.Contains("TestEntity", code);
            Assert.Contains("public int Id", code);
            Assert.Contains("public string Name", code);
        }

        [Fact]
        public void ControllerTemplate_ShouldGenerateCorrectController()
        {
            // Arrange
            var template = new global::FakeMicro.Utilities.CodeGenerator.Templates.ControllerTemplate();

            // Act
            var code = template.Generate(_testMetadata);

            // Assert
            Assert.Contains("class TestEntityController", code);
            Assert.Contains("TestEntity", code);
            Assert.Contains("ControllerBase", code);
        }
    }
}