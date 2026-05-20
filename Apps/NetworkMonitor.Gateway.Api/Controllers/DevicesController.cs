using Microsoft.AspNetCore.Mvc;
using NetworkMonitor.Data;
using NetworkMonitor.Services;

namespace NetworkMonitor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DevicesController : ControllerBase
    {
        private readonly IDeviceTrackingService _trackingService;
        private readonly INetworkDiscoveryService _discoveryService;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<DevicesController> _logger;

        public DevicesController(
            IDeviceTrackingService trackingService,
            INetworkDiscoveryService discoveryService,
            ApplicationDbContext dbContext,
            ILogger<DevicesController> logger)
        {
            _trackingService = trackingService;
            _discoveryService = discoveryService;
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllDevices()
        {
            try
            {
                var devices = await _trackingService.GetAllDevicesAsync();
                var onlineCount = devices.Count(d => d.Status == DeviceStatus.Online);

                return Ok(new
                {
                    totalDevices = devices.Count,
                    onlineDevices = onlineCount,
                    offlineDevices = devices.Count - onlineCount,
                    devices = devices.Select(d => new
                    {
                        d.Id,
                        d.Name,
                        d.IPAddress,
                        d.MACAddress,
                        d.Status,
                        d.InterfaceType,
                        d.FirstSeen,
                        d.LastSeen,
                        d.ScanCount
                    })
                });
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
                var device = await _trackingService.GetDeviceByIdAsync(id);
                if (device == null)
                    return NotFound("Device not found");

                var history = await _trackingService.GetDeviceHistoryAsync(id, 50);

                return Ok(new
                {
                    device = new
                    {
                        device.Id,
                        device.Name,
                        device.IPAddress,
                        device.MACAddress,
                        device.Status,
                        device.InterfaceType,
                        device.FirstSeen,
                        device.LastSeen,
                        device.ScanCount
                    },
                    recentHistory = history.Select(h => new { h.Timestamp, h.Status })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving device");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("scan")]
        public async Task<IActionResult> TriggerNetworkScan()
        {
            try
            {
                _logger.LogInformation("Manual network scan triggered");
                var startTime = DateTime.UtcNow;

                var discoveredDevices = await _discoveryService.ScanNetworkAsync();
                var endTime = DateTime.UtcNow;

                var scan = new NetworkScan
                {
                    StartTime = startTime,
                    EndTime = endTime,
                    DevicesFound = discoveredDevices.Count,
                    Status = "Completed"
                };

                _dbContext.Scans.Add(scan);
                await _dbContext.SaveChangesAsync();

                var updatedDevices = await _trackingService.UpdateDevicesFromScanAsync(discoveredDevices);

                foreach (var device in updatedDevices)
                {
                    await _trackingService.RecordDeviceHistoryAsync(device.Id, scan.Id, "Online");
                }

                return Ok(new
                {
                    scanDuration = (endTime - startTime).TotalSeconds,
                    devicesFound = discoveredDevices.Count,
                    devicesUpdated = updatedDevices.Count,
                    devices = updatedDevices.Select(d => new { d.Id, d.Name, d.IPAddress, d.MACAddress })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during manual scan");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("scans/history")]
        public async Task<IActionResult> GetScanHistory([FromQuery] int limit = 20)
        {
            try
            {
                var scans = _dbContext.Scans
                    .OrderByDescending(s => s.StartTime)
                    .Take(limit)
                    .Select(s => new
                    {
                        s.Id,
                        s.StartTime,
                        s.EndTime,
                        s.DevicesFound,
                        s.Status,
                        Duration = s.EndTime.HasValue ? (s.EndTime.Value - s.StartTime).TotalSeconds : 0
                    })
                    .ToList();

                return Ok(new { scans });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving scan history");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
