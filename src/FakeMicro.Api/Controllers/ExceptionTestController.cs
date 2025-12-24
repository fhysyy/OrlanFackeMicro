using FakeMicro.Shared.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System;

namespace FakeMicro.Api.Controllers
{
    /// <summary>
    /// 异常测试控制器
    /// 用于验证全局异常处理机制
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ExceptionTestController : ControllerBase
    {
        /// <summary>
        /// 测试业务异常
        /// </summary>
        [HttpGet("business")]
        public IActionResult TestBusinessException()
        {
            throw new BusinessException("测试业务规则违反");
        }

        /// <summary>
        /// 测试未找到异常
        /// </summary>
        [HttpGet("not-found")]
        public IActionResult TestNotFoundException()
        {
            throw new NotFoundException("User", "123");
        }

        /// <summary>
        /// 测试验证异常
        /// </summary>
        [HttpGet("validation")]
        public IActionResult TestValidationException()
        {
            throw new ValidationException("字段验证失败", new System.Collections.Generic.List<ValidationError>
            {
                new ValidationError("Username", "用户名不能为空"),
                new ValidationError("Email", "邮箱格式不正确")
            });
        }

        /// <summary>
        /// 测试通用自定义异常
        /// </summary>
        [HttpGet("custom")]
        public IActionResult TestCustomException()
        {
            throw new CustomException("自定义异常测试", "custom_test", 418, new System.Collections.Generic.Dictionary<string, object>
            {
                { "key1", "value1" },
                { "key2", 123 }
            });
        }

        /// <summary>
        /// 测试系统异常
        /// </summary>
        [HttpGet("system")]
        public IActionResult TestSystemException()
        {
            throw new ArgumentNullException("参数不能为空");
        }

        /// <summary>
        /// 测试内部服务器错误
        /// </summary>
        [HttpGet("internal")]
        public IActionResult TestInternalServerError()
        {
            throw new Exception("内部服务器错误测试");
        }
    }
}
