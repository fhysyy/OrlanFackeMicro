using FakeMicro.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orleans;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
namespace FakeMicro.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ManagerVersionController : ControllerBase
    {
        private readonly IClusterClient _clusterClient;
        private readonly ILogger<ManagerVersionController> _logger;
        public ManagerVersionController(IClusterClient clusterClient, ILogger<ManagerVersionController> logger)
        {
            _clusterClient = clusterClient;
            _logger = logger;
        }
        [HttpPost("insert")]
        public async Task<IActionResult> InsertData([FromBody] object request)
        {
            var _client = _clusterClient.GetGrain<IManagerVersion>(Guid.NewGuid().ToString());
            var result = await _client.InsertData(JsonConvert.SerializeObject(request));
            return Ok(result);
        }

        [HttpPost("update")]
        public async Task<IActionResult> Update([FromBody] object request)
        {
            var _client = _clusterClient.GetGrain<IManagerVersion>(Guid.NewGuid().ToString());
            var result = await _client.UpdateData(JsonConvert.SerializeObject(request));
            return Ok(result);
        }
        [HttpDelete("delete/{id}")] 
        public async Task<IActionResult> Delete(string id)
        {
            var _client = _clusterClient.GetGrain<IManagerVersion>(Guid.NewGuid().ToString());
            var result = await _client.DeleteData(id);
            return Ok(result);
        }


        [HttpPost("search")]
        public async Task<IActionResult> SearchData([FromBody] object request)
        {
            var _client = _clusterClient.GetGrain<IManagerVersion>(Guid.NewGuid().ToString());
            var result = await _client.SearchData(JsonConvert.SerializeObject(request));
            return Ok(result);
        }
        [HttpPost("info/{id}")]
        public async Task<IActionResult> DataInfo([FromBody] object request)
        {
            var _client = _clusterClient.GetGrain<IManagerVersion>(Guid.NewGuid().ToString());
            var result = await _client.DataInfo(JsonConvert.SerializeObject(request));
            return Ok(result);
        }

    }
}
