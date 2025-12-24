using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace FakeMicro.Api.Controllers
{
    /// <summary>
    /// 幂等性测试控制器
    /// 用于验证幂等性中间件的功能
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class IdempotencyTestController : ControllerBase
    {
        // 静态计数器，用于验证幂等性
        private static int _counter = 0;
        private static readonly object _lock = new object();

        /// <summary>
        /// 测试幂等性的接口
        /// 每次调用会增加计数器的值
        /// 如果幂等性中间件正常工作，重复调用应该返回相同的结果
        /// </summary>
        [HttpPost("test")]
        public async Task<IActionResult> TestIdempotency()
        {
            // 模拟一些处理时间
            await Task.Delay(100);

            // 增加计数器
            int currentCount;
            lock (_lock)
            {
                _counter++;
                currentCount = _counter;
            }

            return Ok(new {
                Message = "请求处理成功",
                Counter = currentCount,
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// 获取当前计数器的值
        /// 用于验证幂等性是否生效
        /// </summary>
        [HttpGet("counter")]
        public IActionResult GetCounter()
        {
            return Ok(new {
                CurrentCounter = _counter
            });
        }

        /// <summary>
        /// 重置计数器
        /// </summary>
        [HttpPost("reset")]
        public IActionResult ResetCounter()
        {
            lock (_lock)
            {
                _counter = 0;
            }
            return Ok(new {
                Message = "计数器已重置"
            });
        }
    }
}
