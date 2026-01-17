using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RS.Fritz.Manager.API;

namespace NetworkMonitor.Controllers 
{
    [ApiController] 
    [Route("[controller]")] 
    public class GetConnectedDevicesController : ControllerBase 
    {
        // service for device discovery
        private readonly IDeviceSearchService _deviceSearchService;
        // service for host retrieval
        private readonly IDeviceHostsService _deviceHostsService; 

        private readonly IConfiguration _configuration;

        public GetConnectedDevicesController(
            // injected discovery service
            IDeviceSearchService deviceSearchService,
            // injected hosts service
            IDeviceHostsService deviceHostsService,
            // injected configuration for secrets
            IConfiguration configuration)
        {
            // assign discovery service
            _deviceSearchService = deviceSearchService;
            // assign hosts service
            _deviceHostsService = deviceHostsService;
            // assign configuration
            _configuration = configuration;
        }

        [HttpGet("scan", Name = "GetConnectedDevicesWithIpScan")]
        public void Get()
        {
            Console.WriteLine("hit");
            // Find the first IPv4 gateway address on any network interface
            IPAddress gateway = NetworkInterface
                .GetAllNetworkInterfaces()
                .SelectMany(ni => ni.GetIPProperties().GatewayAddresses)
                .Select(g => g?.Address)
                .FirstOrDefault(a => a != null && a.AddressFamily == AddressFamily.InterNetwork);

            Console.WriteLine($"Gateway IP: {gateway}");
            if (gateway == null)
            {
                Console.WriteLine("No IPv4 gateway found.");
                return;
            }
            string gatewayString = gateway.ToString();
            int lastdot = gatewayString.LastIndexOf('.');
            Console.WriteLine($"Last dot position: {lastdot}");
            string gatewayPrefix;
            if (lastdot >= 0)
            {
                gatewayPrefix = gatewayString.Substring(0, lastdot + 1);
            }else
            {
                gatewayPrefix = gatewayString;
            }
            Console.WriteLine($"DefaultGateway: {gatewayString}\n GatewayPrefix: {gatewayPrefix}");
            for (int i = 2; i < 245; i++)
            {
                Console.WriteLine(gatewayPrefix + i);
                PingReply ping = new Ping().Send(gatewayPrefix + i);
                Console.WriteLine($"{ping.Status}");

            }
        }


        [HttpGet("fritz", Name = "GetConnectedDevices")]
        // async action returning HTTP result
        public async Task<IActionResult> GetDevices() 
        {
            try
            {
                // log starting search
                Console.WriteLine("Searching for Fritz!Box devices..."); 

                // Search for routers and take the first one
                var devices = await _deviceSearchService.GetInternetGatewayDevicesAsync();
                // pick first group
                var groupedDevice = devices.FirstOrDefault();
                // no device found
                if (groupedDevice == null) 
                {
                    return NotFound("No Fritz!Box device found on the network"); 
                }

                // Select the router's internal AVM (FritzBox) device
                InternetGatewayDevice device = groupedDevice.Devices.FirstOrDefault(q => q.IsAvm);
                // no AVM device
                if (device == null) 
                {
                    return NotFound("No AVM device found"); 
                }

                Console.WriteLine($"Found device: {device.UPnPDescription?.Device?.ModelDescription}"); 

                // Initialize the device for TR-064
                await device.InitializeAsync();

                // Set credentials
                string username = _configuration["fritzboxuser"] ?? string.Empty;
                string password = _configuration["fritzboxpassword"] ?? string.Empty;

                // apply credentials
                device.NetworkCredential = new NetworkCredential(username, password);
                // log retrieval start
                Console.WriteLine("Retrieving device hosts...");

                // Get all connected devices
                DeviceHostInfo deviceHostInfo = await _deviceHostsService.GetDeviceHostsAsync(device);
                
                var onlineDevices = deviceHostInfo.DeviceHosts 
                    // filter active hosts
                    .Where(h => h.Active) 
                    .Select(h => new 
                    {
                        hostname = h.HostName,
                        ip = h.IpAddress, 
                        mac = h.MacAddress, 
                        interfaceType = h.InterfaceType,
                        active = h.Active,
                        speed = h.Speed

                    })
                    .ToList();

                Console.WriteLine($"Found {onlineDevices.Count} online devices out of {deviceHostInfo.DeviceHosts.Count()} total");

                foreach (var dev in onlineDevices) 
                {
                    Console.WriteLine($"{dev.hostname} | IP: {dev.ip} | MAC: {dev.mac} | Speed: {dev.speed}"); 
                }

                return Ok(new
                {
                    totalDevices = deviceHostInfo.DeviceHosts.Count(),
                    onlineDevices = onlineDevices.Count,
                    devices = onlineDevices 
                });
            }
            catch (Exception ex) 
            {
                Console.WriteLine($"GetDevices error: {ex}"); 
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace }); 
            }
        }


        [HttpGet("mesh")]
        public async Task<IActionResult> GetMeshTopology()
        {
            

            return Ok();
        }
    }
}