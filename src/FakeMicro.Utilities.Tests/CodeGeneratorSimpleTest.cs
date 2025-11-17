using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FakeMicro.Utilities.CodeGenerator;
using Microsoft.Extensions.Configuration;

// 为解决命名空间冲突，使用别名
using CodeGeneratorClass = FakeMicro.Utilities.CodeGenerator.CodeGenerator;

namespace FakeMicro.Utilities.Tests
{
    /// <summary>
    /// 代码生成器简单功能测试
    /// </summary>
    public class CodeGeneratorSimpleTest
    {
        private readonly CodeGeneratorClass _codeGenerator;
        private readonly CodeGeneratorConfiguration _configuration;

        public CodeGeneratorSimpleTest()
        {
            _configuration = new CodeGeneratorConfiguration();
            _codeGenerator = new CodeGeneratorClass(_configuration);
        }

        /// <summary>
        /// 测试CodeGenerator实例创建
        /// </summary>
        [Fact]
        public void CodeGenerator_CanBeCreated()
        {
            // Arrange & Act
            var generator = new CodeGeneratorClass(_configuration);

            // Assert
            Assert.NotNull(generator);
        }

        /// <summary>
        /// 测试获取可用实体类型
        /// </summary>
        [Fact]
        public async Task GetAvailableEntityTypes_ReturnsValidTypes()
        {
            // Arrange
            var generator = _codeGenerator;

            // Act
            var types = await generator.GetAvailableEntityTypesAsync();

            // Assert
            Assert.NotNull(types);
            Assert.NotEmpty(types);
            Assert.Contains("User", types);
            Assert.Contains("Product", types);
        }

        /// <summary>
        /// 测试创建实体元数据
        /// </summary>
        [Fact]
        public void CreateEntityMetadata_ReturnsValidMetadata()
        {
            // Arrange
            var generator = _codeGenerator;
            var properties = new List<PropertyMetadata>
            {
                new PropertyMetadata { Name = "Id", Type = "Guid", IsPrimaryKey = true, IsRequired = true },
                new PropertyMetadata { Name = "Name", Type = "string", IsRequired = true },
                new PropertyMetadata { Name = "Email", Type = "string?" }
            };

            // Act
            var metadata = generator.CreateEntityMetadata("User", properties);

            // Assert
            Assert.NotNull(metadata);
            Assert.Equal("User", metadata.EntityName);
            Assert.Equal(3, metadata.Properties.Count);
            Assert.True(metadata.Properties.First().IsPrimaryKey);
        }

        /// <summary>
        /// 测试代码预览功能
        /// </summary>
        [Fact]
        public async Task PreviewCode_ReturnsPreviewContent()
        {
            // Arrange
            var generator = _codeGenerator;
            var properties = new List<PropertyMetadata>
            {
                new PropertyMetadata { Name = "Id", Type = "Guid", IsPrimaryKey = true, IsRequired = true },
                new PropertyMetadata { Name = "Name", Type = "string", IsRequired = true }
            };
            var metadata = generator.CreateEntityMetadata("User", properties);

            // Act
            var preview = await generator.PreviewCodeAsync(metadata, GenerationType.All);

            // Assert
            Assert.NotNull(preview);
            Assert.Equal(4, preview.Count); // All types: Interface, Grain, Dto, Controller
            Assert.True(preview.ContainsKey(GenerationType.Interface));
            Assert.True(preview.ContainsKey(GenerationType.Grain));
            Assert.True(preview.ContainsKey(GenerationType.Dto));
            Assert.True(preview.ContainsKey(GenerationType.Controller));

            // Check that each content is not empty
            foreach (var kvp in preview)
            {
                Assert.NotEmpty(kvp.Value);
                Assert.Contains("User", kvp.Value);
            }
        }
    }
}