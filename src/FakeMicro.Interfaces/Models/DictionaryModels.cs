using Orleans;

namespace FakeMicro.Interfaces.Models
{
    /// <summary>
    /// 创建字典类型请求
    /// </summary>
    [GenerateSerializer]
    public class DictionaryTypeCreateRequest
    {
        [Id(0)]
        public string Code { get; set; } = string.Empty;
        [Id(1)]
        public string Name { get; set; } = string.Empty;
        [Id(2)]
        public string? Description { get; set; }
        [Id(3)]
        public bool IsEnabled { get; set; } = true;
        [Id(4)]
        public int SortOrder { get; set; }
    }

    /// <summary>
    /// 更新字典类型请求
    /// </summary>
    [GenerateSerializer]
    public class DictionaryTypeUpdateRequest
    {
        [Id(0)]
        public string Code { get; set; } = string.Empty;
        [Id(1)]
        public string Name { get; set; } = string.Empty;
        [Id(2)]
        public string? Description { get; set; }
        [Id(3)]
        public bool IsEnabled { get; set; }
        [Id(4)]
        public int SortOrder { get; set; }
    }

    /// <summary>
    /// 创建字典项请求
    /// </summary>
    [GenerateSerializer]
    public class DictionaryItemCreateRequest
    {
        [Id(0)]
        public long DictionaryTypeId { get; set; }
        [Id(1)]
        public string Value { get; set; } = string.Empty;
        [Id(2)]
        public string Text { get; set; } = string.Empty;
        [Id(3)]
        public string? Description { get; set; }
        [Id(4)]
        public bool IsEnabled { get; set; } = true;
        [Id(5)]
        public int SortOrder { get; set; }
        [Id(6)]
        public string? ExtraData { get; set; }
    }

    /// <summary>
    /// 更新字典项请求
    /// </summary>
    [GenerateSerializer]
    public class DictionaryItemUpdateRequest
    {
        [Id(0)]
        public string Value { get; set; } = string.Empty;
        [Id(1)]
        public string Text { get; set; } = string.Empty;
        [Id(2)]
        public string? Description { get; set; }
        [Id(3)]
        public bool IsEnabled { get; set; }
        [Id(4)]
        public int SortOrder { get; set; }
        [Id(5)]
        public string? ExtraData { get; set; }
    }
}