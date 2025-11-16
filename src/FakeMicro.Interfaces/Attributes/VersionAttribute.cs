using System;

namespace FakeMicro.Interfaces.Attributes
{
    /// <summary>
    /// 版本属性，用于标记Orleans Grain接口的版本号
    /// 实现版本兼容性控制
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class VersionAttribute : Attribute
    {
        /// <summary>
        /// 获取接口版本号
        /// </summary>
        public int Version { get; }
        
        /// <summary>
        /// 获取最低兼容版本号
        /// </summary>
        public int MinCompatibleVersion { get; }
        
        /// <summary>
        /// 初始化版本属性
        /// </summary>
        /// <param name="version">当前版本号</param>
        /// <param name="minCompatibleVersion">最低兼容版本号，默认为1</param>
        public VersionAttribute(int version, int minCompatibleVersion = 1)
        {
            if (version < 1)
                throw new ArgumentException("版本号必须大于或等于1", nameof(version));
            
            if (minCompatibleVersion < 1 || minCompatibleVersion > version)
                throw new ArgumentException("最低兼容版本号必须大于或等于1且小于或等于当前版本号", nameof(minCompatibleVersion));
            
            Version = version;
            MinCompatibleVersion = minCompatibleVersion;
        }
    }
}
