using Microsoft.AspNetCore.Mvc;
using NetworkMonitor.Application.Devices;
using NetworkMonitor.Gateway.Api;

namespace NetworkMonitor.Gateway.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DevicesController : ControllerBase
    {
        private readonly IDeviceService _deviceService;
        private readonly ILogger<DevicesController> _logger;
        private readonly INetworkScanService _networkScanService;

        public DevicesController(
            IDeviceService deviceService,
            ILogger<DevicesController> logger,
            INetworkScanService networkScanService)
        {
            _networkScanService = networkScanService;
            _deviceService = deviceService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllDevices()
        {
            try
            {
                var devices = await _deviceService.GetAllDevicesAsync();
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
                var device = await _deviceService.GetDeviceByIdAsync(id);

                if (device == null)
                    return NotFound("Device not found");

                return Ok(device);
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
                var scans = await _deviceService.GetScanHistoryAsync();
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
