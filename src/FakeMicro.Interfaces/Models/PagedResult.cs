using FakeMicro.Interfaces.FakeMicro.Interfaces;
using Orleans;
using System;
using System.Collections.Generic;

namespace FakeMicro.Interfaces.Models
{
    /// <summary>
    /// 统一的分页结果模型
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    [GenerateSerializer]
    public class PagedResult<T> : BaseResultModel<List<T>>
    {
        /// <summary>
        /// 当前页码
        /// </summary>
        [Id(5)]
        public int PageIndex { get; set; } = 1;
        
        /// <summary>
        /// 每页大小
        /// </summary>
        [Id(6)]
        public int PageSize { get; set; } = 100;
        
        /// <summary>
        /// 总记录数
        /// </summary>
        [Id(7)]
        public int TotalCount { get; set; } = 0;
        
        /// <summary>
        /// 总页数
        /// </summary>
        [Id(8)]
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        
        /// <summary>
        /// 是否有上一页
        /// </summary>
        public bool HasPrevious => PageIndex > 1;
        
        /// <summary>
        /// 是否有下一页
        /// </summary>
        public bool HasNext => PageIndex < TotalPages;
        
        /// <summary>
        /// 创建成功的分页结果
        /// </summary>
        public static new PagedResult<T> SuccessResult(List<T> data, int totalCount, int pageIndex, int pageSize, string message = "")
        {
            return new PagedResult<T>
            {
                Success = true,
                Code = 200,
                TraceId = Guid.NewGuid().ToString(),
                Message = message,
                Data = data,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }
    }
}