using FakeMicro.Interfaces;
using FakeMicro.Interfaces.FakeMicro.Interfaces;
using FakeMicro.Interfaces.Models;
using FakeMicro.Interfaces.Models.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Orleans;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
namespace FakeMicro.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MongoController : ControllerBase
    {
        private readonly IClusterClient clusterClient;
        public MongoController(IClusterClient _clusterClient)
        {
            clusterClient = _clusterClient;
        }
        [HttpPost("insert/{formName}")]
        public async Task<IActionResult> InsertData(string formName, [FromBody] object data)
        {
            var mongoGrain = clusterClient.GetGrain<IMongoGrain>("FakeMicroDB");
            var result = await mongoGrain.InsertData(formName, JsonConvert.SerializeObject(data));
            return Ok(result);
        }
        [HttpPost("info/{formName}/{id}")]
        public async Task<IActionResult> info(string formName, string id)
        {
            var mongoGrain = clusterClient.GetGrain<IMongoGrain>("FakeMicroDB");

            var result = await mongoGrain.DataInfo(formName, id);
            return Ok(result);

        }
        [HttpPost("delete/{formName}/{id}")]
        public async Task<IActionResult> delete(string formName, string id)
        {
            var mongoGrain = clusterClient.GetGrain<IMongoGrain>("FakeMicroDB");
            var result = await mongoGrain.DeleteData(id);
            return Ok(result);

        }
        [HttpPost("update/{formName}/{id}")]
        public async Task<IActionResult> Update(string id, string formName, [FromBody] Dictionary<string, object> data)
        {
            var mongoGrain = clusterClient.GetGrain<IMongoGrain>("FakeMicroDB");
            var result = await mongoGrain.UpdateData(id, data);
            return Ok(result);
        
        }
        [HttpPost("search")]
        public async Task<IActionResult> searchData([FromBody] PageQueryModel data)
        {
            var mongoGrain = clusterClient.GetGrain<IMongoGrain>("FakeMicroDB");
            var result = await mongoGrain.SearchData(data);
            return Ok(result);

        }

    }
}
