using FakeMicro.DatabaseAccess;
using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.Interfaces;
using FakeMicro.Utilities.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace FakeMicro.Grains
{
    public class SysOpenGrain : OrleansGrainBase, ISysOpenGrain
    {
        private readonly ISysOpenRepository _sysOpenRepository;
        private readonly JwtSettings _jwtSettings;

        public SysOpenGrain(ISysOpenRepository sysOpenRepository, ILogger<SysOpenGrain> logger, IOptions<JwtSettings> jwtSettings)
            : base(logger)
        {
            _sysOpenRepository = sysOpenRepository ?? throw new ArgumentNullException(nameof(sysOpenRepository));
            _jwtSettings = jwtSettings?.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
        }


        public Task<string> GetSysOpenSetting(string data)
        {
            // 修复 CS0029: 返回类型应为 string，而不是匿名类型
            // 修复 CS1002: 缺少分号
            return Task.FromResult(DateTime.Now.ToString("yyyyMMdd")+"雪花UI");
        }
        
        // GetMachineId() 和 GenerateSnowflakeId() 方法现在由 BaseGrain 基类提供
    }
}
