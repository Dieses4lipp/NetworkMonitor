
using Microsoft.AspNetCore.Mvc;

namespace NetworkMonitor.Gateway.Api.Controllers
{
    [ApiController]
    [Route("jobs")]
    public class MonitoringJobController : ControllerBase
    {
        [HttpPost]
        public void CreateNewJob()
        {
            Console.WriteLine("Create New Job hit");
        }

        [HttpPatch]
        [Route("{id}")]
        public void UpdateJobById([FromRoute] Guid id)
        {
            Console.WriteLine($"Update Job by Id hit with {id}");
        }
        [HttpGet]
        [Route("types")]
        public void GetJobTypes()
        {
            Console.WriteLine("Get Job Types hit");
        }
    }
}
