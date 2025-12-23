using System.Collections.Generic;
using Orleans;

namespace FakeMicro.Interfaces
{
    /// <summary>
    /// 分页结果接口
    /// </summary>
    /// <typeparam name="T">数据项类型</typeparam>
    public interface IPaginatedResult<T>
    {
        /// <summary>
        /// 当前页码
        /// </summary>
        long Page { get; }

        /// <summary>
        /// 每页大小
        /// </summary>
        long PageSize { get; }

        /// <summary>
        /// 总记录数
        /// </summary>
        long TotalCount { get; }

        /// <summary>
        /// 总页数
        /// </summary>
        long TotalPages { get; }

        /// <summary>
        /// 数据项集合
        /// </summary>
        IReadOnlyCollection<T> Items { get; }

        /// <summary>
        /// 是否有上一页
        /// </summary>
        bool HasPreviousPage { get; }

        /// <summary>
        /// 是否有下一页
        /// </summary>
        bool HasNextPage { get; }
    }

    /// <summary>
    /// 分页结果实现类
    /// </summary>
    /// <typeparam name="T">数据项类型</typeparam>
    [GenerateSerializer]
    public class PaginatedResult<T> : IPaginatedResult<T>
    {
        /// <summary>
        /// 当前页码
        /// </summary>
        [Id(0)]
        public long Page { get; private set; }

        /// <summary>
        /// 每页大小
        /// </summary>
        [Id(1)]
        public long PageSize { get; private set; }

        /// <summary>
        /// 总记录数
        /// </summary>
        [Id(2)]
        public long TotalCount { get; private set; }

        /// <summary>
        /// 总页数
        /// </summary>
        [Id(3)]
        public long TotalPages { get; private set; }

        /// <summary>
        /// 数据项集合
        /// </summary>
        [Id(4)]
        public IReadOnlyCollection<T> Items { get; private set; }

        /// <summary>
        /// 是否有上一页
        /// </summary>
        public bool HasPreviousPage => Page > 1;

        /// <summary>
        /// 是否有下一页
        /// </summary>
        public bool HasNextPage => Page < TotalPages;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="items">数据项集合</param>
        /// <param name="page">当前页码</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="totalCount">总记录数</param>
        public PaginatedResult(IReadOnlyCollection<T> items, long page, long pageSize, long totalCount)
        {
            Items = items;
            Page = page;
            PageSize = pageSize;
            TotalCount = totalCount;
            TotalPages = (long)System.Math.Ceiling(totalCount / (double)pageSize);
        }
    }
}