using System;

namespace FakeMicro.Utilities.CodeGenerator
{
    /// <summary>
    /// 代码生成类型枚举
    /// </summary>
    [Flags]
    public enum GenerationType
    {
        None = 0,
        Interface = 1,
        Grain = 2,
        Dto = 4,
        Controller = 8,
        All = Interface | Grain | Dto | Controller
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
    }
}