using FakeMicro.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orleans;

using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
namespace FakeMicro.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SysOpenController : ControllerBase
    {
        private readonly IClusterClient _clusterClient;
        private readonly ILogger<SysOpenController> _logger;
        public SysOpenController(IClusterClient clusterClient, ILogger<SysOpenController> logger)
        {
            _clusterClient = clusterClient;
            _logger = logger;
        }
        [Authorize(Policy = "SystemAdmin")]
        [HttpPost("setting")]
        public async Task<IActionResult> SearchSetting([FromBody] object data)
        {
            try
            {
                var sysOpenGrain = _clusterClient.GetGrain<ISysOpenGrain>(Guid.NewGuid().ToString());
                var setting = await sysOpenGrain.GetSysOpenSetting(JsonConvert.SerializeObject(data));
                return Ok(setting);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取系统开放设置失败"); 
                return StatusCode(500, "服务器内部错误");
            }
        }

        // GET api/<SysOpenController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<SysOpenController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<SysOpenController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<SysOpenController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
