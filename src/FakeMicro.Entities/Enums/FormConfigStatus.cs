using Orleans;

namespace FakeMicro.Entities.Enums
{
    /// <summary>
    /// 表单配置状态枚举
    /// </summary>
    [GenerateSerializer]
    public enum FormConfigStatus
    {
        /// <summary>
        /// 草稿状态
        /// </summary>
        [Id(0)]
        Draft = 0,

        /// <summary>
        /// 已发布
        /// </summary>
        [Id(1)]
        Published = 1,

        /// <summary>
        /// 已下线
        /// </summary>
        [Id(2)]
        Offline = 2,

        /// <summary>
        /// 已归档
        /// </summary>
        [Id(3)]
        Archived = 3
    }
}