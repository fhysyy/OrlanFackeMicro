namespace FakeMicro.Entities.Enums
{
    /// <summary>
    /// 消息类型枚举
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        /// 系统消息
        /// </summary>
        System = 1,
        
        /// <summary>
        /// 通知消息
        /// </summary>
        Notification = 2,
        
        /// <summary>
        /// 提醒消息
        /// </summary>
        Reminder = 3,
        
        /// <summary>
        /// 营销消息
        /// </summary>
        Marketing = 4,
        
        /// <summary>
        /// 验证消息
        /// </summary>
        Verification = 5,
        
        /// <summary>
        /// 警告消息
        /// </summary>
        Warning = 6,
        
        /// <summary>
        /// 错误消息
        /// </summary>
        Error = 7,
        
        /// <summary>
        /// 成功消息
        /// </summary>
        Success = 8,
        
        /// <summary>
        /// 信息消息
        /// </summary>
        Info = 9,
        
        /// <summary>
        /// 自定义消息
        /// </summary>
        Custom = 10
    }

    /// <summary>
    /// 消息通道枚举
    /// </summary>
    public enum MessageChannel
    {
        /// <summary>
        /// 站内信
        /// </summary>
        InApp = 1,
        
        /// <summary>
        /// 电子邮件
        /// </summary>
        Email = 2,
        
        /// <summary>
        /// 短信
        /// </summary>
        SMS = 3,
        
        /// <summary>
        /// 推送通知
        /// </summary>
        Push = 4,
        
        /// <summary>
        /// 微信
        /// </summary>
        WeChat = 5,
        
        /// <summary>
        /// 钉钉
        /// </summary>
        DingTalk = 6,
        
        /// <summary>
        /// Webhook
        /// </summary>
        Webhook = 7,
        
        /// <summary>
        /// 多通道
        /// </summary>
        MultiChannel = 8
    }

    /// <summary>
    /// 消息状态枚举
    /// </summary>
    public enum MessageStatus
    {
        /// <summary>
        /// 草稿
        /// </summary>
        Draft = 1,
        
        /// <summary>
        /// 待发送
        /// </summary>
        Pending = 2,
        
        /// <summary>
        /// 发送中
        /// </summary>
        Sending = 3,
        
        /// <summary>
        /// 已发送
        /// </summary>
        Sent = 4,
        
        /// <summary>
        /// 已送达
        /// </summary>
        Delivered = 5,
        
        /// <summary>
        /// 已阅读
        /// </summary>
        Read = 6,
        
        /// <summary>
        /// 发送失败
        /// </summary>
        Failed = 7,
        
        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled = 8,
        
        /// <summary>
        /// 已过期
        /// </summary>
        Expired = 9
    }

    /// <summary>
    /// 消息优先级枚举
    /// </summary>
    public enum MessagePriority
    {
        /// <summary>
        /// 低优先级
        /// </summary>
        Low = 1,
        
        /// <summary>
        /// 普通优先级
        /// </summary>
        Normal = 2,
        
        /// <summary>
        /// 高优先级
        /// </summary>
        High = 3,
        
        /// <summary>
        /// 紧急优先级
        /// </summary>
        Urgent = 4
    }
}