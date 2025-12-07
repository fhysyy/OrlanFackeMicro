using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeMicro.Interfaces.Models
{
    /// <summary>
    /// 分页查询模型
    /// </summary>
    /// 
    [GenerateSerializer]
    public class PageQueryModel
    {
        /// <summary>
        /// 页码
        /// </summary>
        [Id(0)]
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// 每页大小
        /// </summary>
        [Id(1)]
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// 查询条件
        /// </summary>
        [Id(2)]
        public Dictionary<string, object> Filter { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 排序字段
        /// </summary>
        [Id(3)]
        public string SortField { get; set; }

        /// <summary>
        /// 排序方向
        /// </summary>
        [Id(4)]
        public bool IsAscending { get; set; } = true;

    }
    [GenerateSerializer]
    /// <summary>
    /// 查询过滤条件
    /// </summary>
    public class QueryFilter
    {
        /// <summary>
        /// 字段名
        /// </summary>
        [Id(0)]
        public string Field { get; set; }

        /// <summary>
        /// 操作符
        /// </summary>
        [Id(1)]
        public string Operator { get; set; }

        /// <summary>
        /// 值
        /// </summary>
        [Id(2)]
        public object Value { get; set; }
    }
}
