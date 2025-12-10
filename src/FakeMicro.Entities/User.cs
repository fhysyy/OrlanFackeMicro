using FakeMicro.Entities.Enums;
using Orleans;
using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace FakeMicro.Entities
{
    /// <summary>
    /// 用户实体
    /// </summary>
    [GenerateSerializer]
    [SugarTable("users")]
    public class User : IAuditable, ISoftDeletable
    {
        /// <summary>
        /// 用户ID (雪花ID)
        /// </summary>
        [SqlSugar.SugarColumn(IsPrimaryKey = true, ColumnName = "id")]
        [Id(0)]
        public long id { get; set; }
        
        /// <summary>
        /// 用户名
        /// </summary>
        [Id(1)]
        [SugarColumn(IsNullable = true, ColumnName = "username")]
        public string username { get; set; } = string.Empty;
        
        /// <summary>
        /// 显示名称
        /// </summary>
        [Id(2)]
        [SugarColumn(IsNullable = true, ColumnName = "display_name")]
        public string display_name { get; set; } = string.Empty;
        
        /// <summary>
        /// 电子邮箱
        /// </summary>
        [Id(3)]
        [SugarColumn(IsNullable = true, ColumnName = "email")]
        public string email { get; set; } = string.Empty;
        
        /// <summary>
        /// 手机号码
        /// </summary>
        [Id(4)]
        [SugarColumn(IsNullable = true, ColumnName = "phone")]
        public string? phone { get; set; }
        
        /// <summary>
        /// 密码哈希
        /// </summary>
        [Id(5)]
        [SugarColumn(IsNullable = true, ColumnName = "password_hash")]
        public string password_hash { get; set; }
        
        /// <summary>
        /// 密码盐值
        /// </summary>
        [Id(6)]
        [SugarColumn(IsNullable = true, ColumnName = "password_salt")]
        public string password_salt { get; set; }
        
        /// <summary>
        /// 是否激活
        /// </summary>
        [Id(7)]
        [SugarColumn(IsNullable = true, ColumnName = "is_active")]
        public bool is_active { get; set; } = true;
        
        /// <summary>
        /// 用户角色
        /// </summary>
        [MaxLength(20)]
        [Id(8)]
        [SugarColumn(IsNullable = true, ColumnName = "role")]
        public string role { get; set; } = "User";
        
        /// <summary>
        /// 用户状态
        /// </summary>
        [MaxLength(20)]
        [Id(9)]
        [SugarColumn(IsNullable = true, ColumnName = "status")]
        public string status { get; set; } = "Pending";
        
        /// <summary>
        /// 最后登录时间
        /// </summary>
        [Id(10)]
        [SugarColumn(IsNullable = true, ColumnName = "last_login_at")]
        public DateTime? last_login_at { get; set; }
        
        /// <summary>
        /// 租户ID
        /// </summary>
        [Id(11)]
        [SugarColumn(IsNullable = true, ColumnName = "tenant_id")]
        public int? tenant_id { get; set; }
        
        /// <summary>
        /// 邮箱验证状态
        /// </summary>
        [Id(12)]
        [SugarColumn(IsNullable = true, ColumnName = "email_verified")]
        public bool email_verified { get; set; } = false;
        
        /// <summary>
        /// 手机验证状态
        /// </summary>
        [Id(13)]
        [SugarColumn(IsNullable = true, ColumnName = "phone_verified")]
        public bool phone_verified { get; set; } = false;
        
        /// <summary>
        /// 登录尝试次数
        /// </summary>
        [Id(14)]
        [SugarColumn(IsNullable = true, ColumnName = "login_attempts")]
        public int login_attempts { get; set; } = 0;
        
        /// <summary>
        /// 锁定时间
        /// </summary>
        [Id(15)]
        [SugarColumn(IsNullable = true, ColumnName = "locked_until")]
        public DateTime? locked_until { get; set; }
        
        /// <summary>
        /// 刷新令牌
        /// </summary>
        [Id(16)]
        [SugarColumn(IsNullable = true, ColumnName = "refresh_token")]
        public string? refresh_token { get; set; }
        
        /// <summary>
        /// 刷新令牌过期时间
        /// </summary>
        [Id(17)]
        [SugarColumn(IsNullable = true, ColumnName = "refresh_token_expiry")]
        public DateTime? refresh_token_expiry { get; set; }
        
        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        [Id(18)]
        [SugarColumn(IsNullable = true, ColumnName = "CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// 更新时间
        /// </summary>
        [Id(19)]
        [SugarColumn(IsNullable = true, ColumnName = "UpdatedAt")]
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// 是否删除
        /// </summary>
        [Id(20)]
        [SugarColumn(IsNullable = true, ColumnName = "is_deleted")]
        public bool is_deleted { get; set; } = false;
        
        /// <summary>
        /// 删除时间
        /// </summary>
        [Id(21)]
        [SugarColumn(IsNullable = true, ColumnName = "deleted_at")]
        public DateTime? deleted_at { get; set; }

        [Id(22)]
        [SqlSugar.SugarColumn(IsNullable = true, ColumnName = "CreatedBy")]
        public string CreatedBy { get; set; }

        [Id(23)]

        [SqlSugar.SugarColumn(IsNullable = true, ColumnName = "UpdatedBy")]
        public string UpdatedBy { get; set; }
 

    }

    
    /// <summary>
    /// 软删除接口
    /// </summary>
    public interface ISoftDeletable
    {
        bool is_deleted { get; set; }
        DateTime? deleted_at { get; set; }
    }
}