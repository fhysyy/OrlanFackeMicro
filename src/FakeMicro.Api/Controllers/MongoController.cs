using FakeMicro.Interfaces;
using FakeMicro.Interfaces.FakeMicro.Interfaces;
using FakeMicro.Interfaces.Models.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Orleans;
using Orleans;
using System;
using System.Text.Json;
using System.Threading.Tasks;
namespace FakeMicro.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MongoController : ControllerBase
    {
        private readonly IClusterClient clusterClient;
        public MongoController(IClusterClient _clusterClient) {
            clusterClient = _clusterClient;
        }
        [HttpPost("insert")]
        public async Task<IActionResult> InsertData([FromBody]object data)
        {
            var mongoGrain = clusterClient.GetGrain<IMongoGrain>("MongoGrain");
            var para= JsonConvert.SerializeObject(data);
            var result = await mongoGrain.InsertData(para);
            return Ok(JsonConvert.DeserializeObject(result));
            //return  Ok(JsonConvert.DeserializeObject<BaseResultModel>(result));
        }
        [HttpPost("info/{id}")]
        public async Task<IActionResult> info(string id)
        {
            var mongoGrain = clusterClient.GetGrain<IMongoGrain>("MongoGrain");
           // var para = JsonConvert.SerializeObject(data);
            var result = await mongoGrain.DataInfo(id);
            return Ok(JsonConvert.DeserializeObject(result));
            //return  Ok(JsonConvert.DeserializeObject<BaseResultModel>(result));
        }
    }
}
