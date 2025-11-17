using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Orleans;
using System.Text.Json.Serialization;
using Orleans.Serialization;
using SqlSugar;

namespace FakeMicro.Entities
{
    /// <summary>
    /// 文件信息实体
    /// </summary>
    [SugarTable("file_infos")]
    [GenerateSerializer]
    public class FileInfo : IAuditable
    {
        /// <summary>
        /// 文件ID
        /// </summary>
        [SugarColumn(IsPrimaryKey = true, ColumnName = "id")]
        [JsonPropertyOrder(0)]
        [Id(0)]
        public long id { get; set; }
        
        /// <summary>
        /// 文件名
        /// </summary>
        [Required]
        [MaxLength(255)]
        [JsonPropertyOrder(1)]
        [SugarColumn(IsNullable = true, ColumnName = "file_name")]
        [Id(1)]
        public string file_name { get; set; } = string.Empty;
        
        /// <summary>
        /// 文件路径
        /// </summary>
        [Required]
        [MaxLength(500)]
        [JsonPropertyOrder(2)]
        [Id(2)]
        public string file_path { get; set; } = string.Empty;
        
        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        [JsonPropertyOrder(3)]
        [Id(3)]
        [SugarColumn(IsNullable = true, ColumnName = "file_size")]
        public long file_size { get; set; }
        
        /// <summary>
        /// MIME类型（内容类型）
        /// </summary>
        [MaxLength(100)]
        [JsonPropertyOrder(4)]
        [Id(4)]
        [SugarColumn(IsNullable = true, ColumnName = "mime_type")]
        public string mime_type { get; set; } = string.Empty;
        
        /// <summary>
        /// 上传者ID
        /// </summary>
        [JsonPropertyOrder(5)]
        [Id(5)]
        [SugarColumn(IsNullable = true, ColumnName = "uploader_id")]
        public long uploader_id { get; set; }
        
        /// <summary>
        /// 是否公开访问
        /// </summary>
        [JsonPropertyOrder(6)]
        [Id(6)]
        [SugarColumn(IsNullable = true, ColumnName = "is_public")]
        public bool is_public { get; set; } = false;
        
        /// <summary>
        /// 文件哈希值（用于重复文件检测）
        /// </summary>
        [MaxLength(64)]
        [JsonPropertyOrder(7)]
        [Id(7)]
        [SugarColumn(IsNullable = true, ColumnName = "file_hash")]
        public string file_hash { get; set; } = string.Empty;
        
        /// <summary>
        /// 文件描述
        /// </summary>
        [MaxLength(1000)]
        [JsonPropertyOrder(8)]
        [Id(8)]
        [SugarColumn(IsNullable = true, ColumnName = "description")]
        public string description { get; set; } = string.Empty;
        
        /// <summary>
        /// 创建时间
        /// </summary>
        [JsonPropertyOrder(9)]
        [Id(9)]
        [SugarColumn(IsNullable = true, ColumnName = "created_at")]
        public DateTime created_at { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// 更新时间
        /// </summary>
        [JsonPropertyOrder(10)]
        [Id(10)]
        [SugarColumn(IsNullable = true, ColumnName = "updated_at")]
        public DateTime? updated_at { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// 是否已删除（软删除标记）
        /// </summary>
        [JsonPropertyOrder(11)]
        [Id(11)]
        [SugarColumn(IsNullable = true, ColumnName = "is_deleted")]
        public bool is_deleted { get; set; } = false;
        
        /// <summary>
        /// 删除时间（软删除时记录）
        /// </summary>
        [JsonPropertyOrder(12)]
        [Id(12)]
        [SugarColumn(IsNullable = true, ColumnName = "deleted_at")]
        public DateTime? deleted_at { get; set; }
        
        /// <summary>
        /// 兼容旧版本属性（上传时间）
        /// </summary>
        [NotMapped]
        [Id(13)]
        public DateTime upload_time 
        { 
            get => created_at; 
            set => created_at = value; 
        }
        
        /// <summary>
        /// 兼容旧版本属性（内容类型）
        /// </summary>
        [NotMapped]
        [Id(14)]
        public string content_type 
        { 
            get => mime_type; 
            set => mime_type = value; 
        }
        [Id(16)]
        [SqlSugar.SugarColumn(IsNullable = true, ColumnName = "created_by")]
        public string created_by { get; set; }

        [Id(15)]

        [SqlSugar.SugarColumn(IsNullable = true, ColumnName = "updated_by")]
        public string updated_by { get; set; }


    }
}