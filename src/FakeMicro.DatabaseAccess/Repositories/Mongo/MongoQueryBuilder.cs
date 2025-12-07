using FakeMicro.Interfaces.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace FakeMicro.DatabaseAccess.Repositories.Mongo
{
    /// <summary>
    /// MongoDB查询构建器
    /// </summary>
    public static class MongoQueryBuilder
    {
        /// <summary>
        /// 根据查询过滤条件构建MongoDB过滤条件
        /// </summary>
        /// <param name="filters">查询过滤条件列表</param>
        /// <returns>MongoDB过滤条件</returns>
        public static FilterDefinition<BsonDocument> BuildFilter(IEnumerable<QueryFilter> filters)
        {
            if (filters == null || !filters.Any())
            {
                return FilterDefinition<BsonDocument>.Empty;
            }

            var filterList = new List<FilterDefinition<BsonDocument>>();

            foreach (var filter in filters)
            {
                var singleFilter = BuildSingleFilter(filter);
                if (singleFilter != null)
                {
                    filterList.Add(singleFilter);
                }
            }

            return Builders<BsonDocument>.Filter.And(filterList);
        }

        /// <summary>
        /// 根据查询过滤条件构建表达式（兼容旧接口）
        /// </summary>
        /// <param name="filters">查询过滤条件列表</param>
        /// <returns>查询表达式</returns>
        public static Expression<Func<object, bool>> BuildExpression(IEnumerable<QueryFilter> filters)
        {
            // 对于MongoDB，我们不需要实际的表达式，因为我们将在仓储中使用FilterDefinition
            // 这里返回一个始终为true的表达式，实际过滤将由BuildFilter方法完成
            return obj => true;
        }

        /// <summary>
        /// 构建单个过滤条件
        /// </summary>
        /// <param name="filter">过滤条件</param>
        /// <returns>MongoDB过滤条件</returns>
        private static FilterDefinition<BsonDocument> BuildSingleFilter(QueryFilter filter)
        {
            if (string.IsNullOrEmpty(filter.Field) || filter.Operator == null)
                return null;

            // 处理不同的操作符
            switch (filter.Operator.ToLower())
            {
                case "eq":
                case "==":
                    return Builders<BsonDocument>.Filter.Eq(filter.Field, filter.Value);
                case "neq":
                case "!=":
                    return Builders<BsonDocument>.Filter.Ne(filter.Field, filter.Value);
                case "contains":
                    return Builders<BsonDocument>.Filter.Regex(filter.Field, new BsonRegularExpression(filter.Value?.ToString() ?? "", "i"));
                case "gt":
                case ">":
                    return Builders<BsonDocument>.Filter.Gt(filter.Field, filter.Value);
                case "gte":
                case ">=":
                    return Builders<BsonDocument>.Filter.Gte(filter.Field, filter.Value);
                case "lt":
                case "<":
                    return Builders<BsonDocument>.Filter.Lt(filter.Field, filter.Value);
                case "lte":
                case "<=":
                    return Builders<BsonDocument>.Filter.Lte(filter.Field, filter.Value);
                case "in":
                    var inValues = GetInValues(filter.Value);
                    if (inValues != null && inValues.Any())
                    {
                        return Builders<BsonDocument>.Filter.In(filter.Field, inValues);
                    }
                    return null;
                case "nin":
                case "not in":
                    var ninValues = GetInValues(filter.Value);
                    if (ninValues != null && ninValues.Any())
                    {
                        return Builders<BsonDocument>.Filter.Nin(filter.Field, ninValues);
                    }
                    return null;
                default:
                    return null;
            }
        }

        /// <summary>
        /// 获取IN操作的值列表
        /// </summary>
        private static IEnumerable<object> GetInValues(object value)
        {
            if (value == null)
                return null;

            var list = value as IEnumerable<object>;
            if (list == null)
            {
                try
                {
                    // 尝试将value转换为IEnumerable
                    list = ((JArray)JToken.FromObject(value)).ToObject<IEnumerable<object>>();
                }
                catch
                {
                    return null;
                }
            }

            return list;
        }
    }
}