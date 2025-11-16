namespace FakeMicro.DatabaseAccess
{
    /// <summary>
    /// 软删除接口，标记实体支持软删除功能
    /// </summary>
    public interface ISoftDelete
    {
        /// <summary>
        /// 是否已删除
        /// </summary>
        bool is_deleted { get; set; }
    }
}