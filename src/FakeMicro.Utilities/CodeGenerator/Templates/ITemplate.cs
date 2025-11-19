using FakeMicro.Utilities.CodeGenerator.Entities;

namespace FakeMicro.Utilities.CodeGenerator.Templates
{
    /// <summary>
    /// 代码模板接口
    /// </summary>
    public interface ITemplate
    {
        /// <summary>
        /// 生成代码
        /// </summary>
        /// <param name="metadata">实体元数据</param>
        /// <returns>生成的代码</returns>
        string Generate(EntityMetadata metadata);
    }
}