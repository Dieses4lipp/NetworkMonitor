using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetworkMonitor.Domain;
using NetworkMonitor.Infrastructure.Data.Context;

namespace NetworkMonitor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DevicesController : ControllerBase
    {
        private readonly NetworkMonitorDbContext _dbContext;
        private readonly ILogger<DevicesController> _logger;

        public DevicesController(
            NetworkMonitorDbContext dbContext,
            ILogger<DevicesController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllDevices()
        {
            try
            {
                var devices = await _dbContext.Devices
                    .AsNoTracking()
                    .ToListAsync();

                return Ok(devices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving devices");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDeviceById(int id)
        {
            try
            {
                var device = await _dbContext.Devices
                    .Include(d => d.MonitoringJobs)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (device == null)
                    return NotFound("Device not found");

                return Ok(new
                {
                    device.Id,
                    device.AgentId,
                    device.DisplayName,
                    device.IpAddress,
                    device.Status,
                    jobCount = device.MonitoringJobs.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving device");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}