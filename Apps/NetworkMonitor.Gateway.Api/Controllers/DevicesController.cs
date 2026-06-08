using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetworkMonitor.Domain;
using NetworkMonitor.Gateway.Api;
using NetworkMonitor.Infrastructure.Data.Context;

namespace NetworkMonitor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DevicesController : ControllerBase
    {
        private readonly NetworkMonitorDbContext _dbContext;
        private readonly ILogger<DevicesController> _logger;
        private readonly INetworkScanService _networkScanService;

        public DevicesController(
            NetworkMonitorDbContext dbContext,
            ILogger<DevicesController> logger,
            INetworkScanService networkScanService)
        {
            _networkScanService = networkScanService;
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

        [HttpPost("scan")]
        public async Task<IActionResult> ScanDevice(CancellationToken cancellationToken)
        {
            try
            {
                var scan = await _networkScanService.RunScanAsync(cancellationToken);
                return Ok(scan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning the network");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("scans")]
        public async Task<IActionResult> GetScanHistory()
        {
            try
            {
                var scans = await _dbContext.NetworkScans
                    .AsNoTracking()
                    .ToListAsync();

                return Ok(scans);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving scan history");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}