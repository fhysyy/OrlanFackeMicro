using System;
using System.Collections.Generic;
using System.Linq;

namespace FakeMicro.Utilities
{
    /// <summary>
    /// 集合扩展方法
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// 检查集合是否为null或空
        /// </summary>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T>? collection)
        {
            return collection == null || !collection.Any();
        }

        /// <summary>
        /// 安全地获取集合中的元素，如果索引超出范围返回默认值
        /// </summary>
        public static T? SafeGet<T>(this IList<T>? list, int index)
        {
            if (list == null || index < 0 || index >= list.Count)
                return default;
            return list[index];
        }

        /// <summary>
        /// 安全地获取集合中的元素，如果索引超出范围返回默认值
        /// </summary>
        public static T? SafeGet<T>(this T[]? array, int index)
        {
            if (array == null || index < 0 || index >= array.Length)
                return default;
            return array[index];
        }

        /// <summary>
        /// 将集合转换为分页结果
        /// </summary>
        public static PaginatedResult<T> ToPaginatedResult<T>(this IEnumerable<T> source, int pageNumber, int pageSize, int totalCount)
        {
            return new PaginatedResult<T>
            {
                Items = source,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        /// <summary>
        /// 对集合进行分页
        /// </summary>
        public static IEnumerable<T> Paginate<T>(this IEnumerable<T> source, int pageNumber, int pageSize)
        {
            if (source == null) return Enumerable.Empty<T>();
            return source.Skip((pageNumber - 1) * pageSize).Take(pageSize);
        }

        /// <summary>
        /// 将集合转换为逗号分隔的字符串
        /// </summary>
        public static string ToCommaSeparatedString<T>(this IEnumerable<T> source, string separator = ", ")
        {
            if (source == null) return string.Empty;
            return string.Join(separator, source);
        }

        /// <summary>
        /// 批量处理集合（分批处理）
        /// </summary>
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
        {
            if (source == null) yield break;
            
            var batch = new List<T>(batchSize);
            foreach (var item in source)
            {
                batch.Add(item);
                if (batch.Count >= batchSize)
                {
                    yield return batch;
                    batch = new List<T>(batchSize);
                }
            }
            
            if (batch.Count > 0)
                yield return batch;
        }

        /// <summary>
        /// 去重（根据指定的键选择器）
        /// </summary>
        public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
        {
            if (source == null) return Enumerable.Empty<T>();
            return source.GroupBy(keySelector).Select(x => x.First());
        }

        /// <summary>
        /// 安全地添加元素到集合（如果元素不为null）
        /// </summary>
        public static void AddIfNotNull<T>(this ICollection<T> collection, T? item) where T : class
        {
            if (item != null)
                collection.Add(item);
        }

        /// <summary>
        /// 安全地添加元素到集合（如果元素不为null且满足条件）
        /// </summary>
        public static void AddIf<T>(this ICollection<T> collection, T? item, Func<T, bool> condition) where T : class
        {
            if (item != null && condition(item))
                collection.Add(item);
        }

        /// <summary>
        /// 将集合转换为字典（处理重复键）
        /// </summary>
        public static Dictionary<TKey, TValue> ToDictionarySafe<T, TKey, TValue>(
            this IEnumerable<T> source,
            Func<T, TKey> keySelector,
            Func<T, TValue> valueSelector,
            IEqualityComparer<TKey>? comparer = null) where TKey : notnull
        {
            var dictionary = new Dictionary<TKey, TValue>(comparer);
            if (source == null) return dictionary;

            foreach (var item in source)
            {
                var key = keySelector(item);
                if (key != null && !dictionary.ContainsKey(key))
                {
                    dictionary[key] = valueSelector(item);
                }
            }

            return dictionary;
        }

        /// <summary>
        /// 随机打乱集合顺序
        /// </summary>
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        {
            if (source == null) return Enumerable.Empty<T>();
            var random = new Random();
            return source.OrderBy(x => random.Next());
        }

        /// <summary>
        /// 检查集合中是否包含任何指定的元素
        /// </summary>
        public static bool ContainsAny<T>(this IEnumerable<T> source, params T[] items)
        {
            if (source == null || items == null) return false;
            return items.Any(item => source.Contains(item));
        }

        /// <summary>
        /// 检查集合中是否包含所有指定的元素
        /// </summary>
        public static bool ContainsAll<T>(this IEnumerable<T> source, params T[] items)
        {
            if (source == null || items == null) return false;
            return items.All(item => source.Contains(item));
        }
    }

    /// <summary>
    /// 分页结果
    /// </summary>
    public class PaginatedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}