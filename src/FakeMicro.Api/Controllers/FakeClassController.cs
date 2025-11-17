using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orleans;
namespace FakeMicro.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FakeClassController : ControllerBase
    {
        private readonly IClusterClient clusterClient;
        private readonly ILogger<FakeClassController> _logger;
        public FakeClassController(IClusterClient _clusterClient, 
            ILogger<FakeClassController> logger) {
            clusterClient= _clusterClient;
            _logger=logger;
        }
    }
}
