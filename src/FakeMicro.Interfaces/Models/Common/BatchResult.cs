using System;
using System.Collections.Generic;
using Orleans;

namespace FakeMicro.Interfaces.Models.Common
{
    /// <summary>
    /// 批量操作结果模型
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    [GenerateSerializer]
    public class BatchResult<T>
    {
        /// <summary>
        /// 成功项集合
        /// </summary>
        [Id(0)]
        public List<T> SuccessItems { get; set; } = new List<T>();

        /// <summary>
        /// 错误信息集合
        /// </summary>
        [Id(1)]
        public List<string> ErrorMessages { get; set; } = new List<string>();

        /// <summary>
        /// 总处理项数
        /// </summary>
        [Id(2)]
        public int TotalItems { get; set; }

        /// <summary>
        /// 成功项数
        /// </summary>
        [Id(3)]
        public int SuccessCount { get; set; }

        /// <summary>
        /// 失败项数
        /// </summary>
        [Id(4)]
        public int FailedCount { get; set; }

        /// <summary>
        /// 是否全部成功
        /// </summary>
        [Id(5)]
        public bool IsAllSuccess { get; set; }
    }
}