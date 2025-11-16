using FakeMicro.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using System;
using System.Threading.Tasks;

namespace FakeMicro.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CounterController : ControllerBase
    {
        private readonly IClusterClient _clusterClient;

        public CounterController(IClusterClient clusterClient)
        {
            _clusterClient = clusterClient;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get([FromRoute] string id)
        {
            try
            {
                var counterGrain = _clusterClient.GetGrain<ICounterGrain>(id);
                var count = await counterGrain.GetCountAsync();
                return Ok(new { CounterId = id, Count = count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("{id}/increment")]
        public async Task<IActionResult> Increment([FromRoute] string id)
        {
            try
            {
                var counterGrain = _clusterClient.GetGrain<ICounterGrain>(id);
                var newCount = await counterGrain.IncrementAsync();
                return Ok(new { CounterId = id, Count = newCount });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("{id}/decrement")]
        public async Task<IActionResult> Decrement([FromRoute] string id)
        {
            try
            {
                var counterGrain = _clusterClient.GetGrain<ICounterGrain>(id);
                var newCount = await counterGrain.DecrementAsync();
                return Ok(new { CounterId = id, Count = newCount });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("{id}/reset")]
        public async Task<IActionResult> Reset([FromRoute] string id)
        {
            try
            {
                var counterGrain = _clusterClient.GetGrain<ICounterGrain>(id);
                await counterGrain.ResetAsync();
                return Ok(new { CounterId = id, Count = 0 });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}