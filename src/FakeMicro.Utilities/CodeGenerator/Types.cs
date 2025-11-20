using System;
using System.Collections.Generic;

namespace FakeMicro.Utilities.CodeGenerator
{
    /// <summary>
    /// 代码生成类型枚举
    /// </summary>
    [Flags]
    public enum GenerationType
    {
        None = 0,
        Entity = 1,
        Interface = 2,
        Result = 4,
        Request = 8,
        Grain = 16,
        Dto = 32,
        Controller = 64,
        Repository = 128,           // 仓储接口
        RepositoryImplementation = 256, // 仓储实现
        All = Entity | Interface | Result | Request | Grain | Dto | Controller | Repository | RepositoryImplementation
    }

    /// <summary>
    /// 代码生成错误类型
    /// </summary>
    public enum GeneratorErrorType
    {
        None,
        EntityNotFound,
        FileExists,
        PermissionDenied,
        Unknown
    }

    /// <summary>
    /// 代码生成结果
    /// </summary>
    public class CodeGenerationResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public GeneratorErrorType ErrorType { get; set; } = GeneratorErrorType.None;
        public List<string> GeneratedFiles { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        /// <summary>
        /// 实际使用的输出路径
        /// </summary>
        public string? OutputPath { get; set; }
    }
}