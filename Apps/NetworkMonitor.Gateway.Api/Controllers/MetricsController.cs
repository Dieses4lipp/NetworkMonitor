using Microsoft.AspNetCore.Mvc;

namespace NetworkMonitor.Gateway.Api.Controllers
{
    [ApiController]
    [Route("metrics")]
    public class MetricsController : ControllerBase
    {
        [HttpGet]
        [Route("latest/{id}")]
        public void Get([FromRoute] Guid id)
        {
            Console.WriteLine($"metrics hit wiht {id}");
        }
        [HttpGet]
        [Route("history")]
        public void GetHistory()
        {
            Console.WriteLine($"metrics hit with history");
        }
        [HttpGet]
        [Route("export")]
        public void GetExport()
        {
            Console.WriteLine($"metrics hit with export");
        }
    }
}
