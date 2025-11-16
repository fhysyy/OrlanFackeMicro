using System;
using System.Threading.Tasks;
using FakeMicro.Interfaces.Attributes;
using FakeMicro.Interfaces.Versioning;
using Xunit;

namespace FakeMicro.Interfaces.Tests
{
    public class VersionCompatibilityTests
    {
        /// <summary>
        /// 测试版本兼容性验证逻辑
        /// </summary>
        [Fact]
        public void TestVersionCompatibilityCheck()
        {
            // 测试场景1: 客户端版本完全匹配
            var result1 = VersionCompatibilityManager.IsCompatible(1, 1, 1, 1);
            Assert.True(result1.IsCompatible);
            Assert.Null(result1.CompatibilityIssue);

            // 测试场景2: 客户端版本低于服务端当前版本但高于最低兼容版本
            var result2 = VersionCompatibilityManager.IsCompatible(1, 2, 1, 1);
            Assert.True(result2.IsCompatible);
            Assert.Contains("客户端版本低于服务端", result2.CompatibilityIssue);

            // 测试场景3: 客户端版本高于服务端版本
            var result3 = VersionCompatibilityManager.IsCompatible(2, 1, 1, 1);
            Assert.False(result3.IsCompatible);
            Assert.Contains("客户端版本高于服务端", result3.CompatibilityIssue);

            // 测试场景4: 客户端版本低于最低兼容版本
            var result4 = VersionCompatibilityManager.IsCompatible(1, 0, 2, 1);
            Assert.False(result4.IsCompatible);
            Assert.Contains("客户端版本低于最低兼容版本", result4.CompatibilityIssue);
        }

        /// <summary>
        /// 测试从类型获取版本信息
        /// </summary>
        [Fact]
        public void TestGetVersionFromType()
        {
            // 获取IAuthGrain的版本信息
            var authGrainVersion = VersionCompatibilityManager.GetInterfaceVersion(typeof(IAuthGrain));
            Assert.NotNull(authGrainVersion);
            Assert.Equal(1, authGrainVersion.Value.Version);
            Assert.Equal(1, authGrainVersion.Value.MinCompatibleVersion);

            // 获取IDictionaryItemGrain的版本信息
            var dictGrainVersion = VersionCompatibilityManager.GetInterfaceVersion(typeof(IDictionaryItemGrain));
            Assert.NotNull(dictGrainVersion);
            Assert.Equal(1, dictGrainVersion.Value.Version);
            Assert.Equal(1, dictGrainVersion.Value.MinCompatibleVersion);
        }

        /// <summary>
        /// 测试VersionAttribute属性验证
        /// </summary>
        [Fact]
        public void TestVersionAttributeValidation()
        {
            // 正常版本属性
            var validAttribute = new VersionAttribute(2, 1);
            Assert.Equal(2, validAttribute.Version);
            Assert.Equal(1, validAttribute.MinCompatibleVersion);

            // 最低版本不能大于当前版本
            Assert.Throws<ArgumentException>(() => new VersionAttribute(1, 2));

            // 版本号不能为负数
            Assert.Throws<ArgumentException>(() => new VersionAttribute(-1, 0));
            Assert.Throws<ArgumentException>(() => new VersionAttribute(1, -1));
        }
    }

    // 用于测试的版本标记接口
    [Version(3, 2)]
    public interface IVersionedTestInterface
    {
        Task<string> GetDataAsync();
    }
}
