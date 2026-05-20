using Microsoft.AspNetCore.Mvc;

namespace NetworkMonitor.Gateway.Api.Controllers
{
    [ApiController]
    [Route("devices")]
    public class DeviceController : ControllerBase
    {
        [HttpGet]
        public void Get()
        {
            Console.WriteLine("devices hit");
        }
        [HttpPost]
        public void AddDevice()
        {
            Console.WriteLine("Add Device hit");
        }
        [HttpGet]
        [Route("{id}")]
        public void GetById([FromRoute] Guid id)
        {
            Console.WriteLine($"Get Device by Id hit with {id}");
        }
        [HttpPut]
        [Route("{id}")]
        public void UpdateById([FromRoute] Guid id)
        {
            Console.WriteLine($"Update Device by Id hit with {id}");
        }
        [HttpDelete]
        [Route("{id}")]
        public void DeleteById([FromRoute] Guid id)
        {
            Console.WriteLine($"Delete Device by Id hit with {id}");
        }
    }
}
