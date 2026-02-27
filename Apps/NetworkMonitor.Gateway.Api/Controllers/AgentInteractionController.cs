using Microsoft.AspNetCore.Mvc;

namespace NetworkMonitor.Gateway.Api.Controllers
{
    [ApiController]
    [Route("agent")]
    public class AgentInteractionController : ControllerBase
    {
        [HttpGet]
        [Route("jobs")]
        public void GetAgentJobs()
        {
            Console.WriteLine("agent/jobs hit");
        }
        [HttpPost]
        [Route("check-in")]
        public void CheckIn()
        {
            Console.WriteLine("agent/check-in hit");
        }

        [HttpPost]
        [Route("results")]
        public void PostResults()
        {
            Console.WriteLine("agent/results hit");
        }
    }
}
