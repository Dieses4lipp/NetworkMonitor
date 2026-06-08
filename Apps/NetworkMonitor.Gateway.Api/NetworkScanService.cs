using NetworkMonitor.Domain;
using NetworkMonitor.Infrastructure.Data.Context;
using NetworkMonitor.Services;

namespace NetworkMonitor.Gateway.Api
{
    public interface INetworkScanService
    {
        Task<NetworkScan> RunScanAsync(CancellationToken cancellationToken);
    }

    public class NetworkScanService : INetworkScanService
    {
        private readonly ILogger<NetworkScanService> _logger;
        private readonly INetworkDiscoveryService _discoveryService;
        private readonly NetworkMonitorDbContext _dbContext;

        public NetworkScanService(
            ILogger<NetworkScanService> logger,
            INetworkDiscoveryService discoveryService,
            NetworkMonitorDbContext dbContext)
        {
            _logger = logger;
            _discoveryService = discoveryService;
            _dbContext = dbContext;
        }

        public async Task<NetworkScan> RunScanAsync(CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;
            var status = "Completed";
            List<DiscoveredDevice> discovered;

            try
            {
                discovered = await _discoveryService.ScanNetworkAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Network discovery failed.");
                status = "Failed";
                discovered = new List<DiscoveredDevice>();
            }

            var activeIps = discovered.Select(d => d.IPAddress).ToList();
            _logger.LogInformation("Scan complete. Found {DeviceCount} active devices.", activeIps.Count);

            var scan = new NetworkScan
            {
                StartTime = startTime,
                EndTime = DateTime.UtcNow,
                DevicesFound = activeIps.Count,
                Status = status
            };
            _dbContext.NetworkScans.Add(scan);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var existingDevices = _dbContext.Devices
                .Where(d => d.AgentId == SystemConstants.BuiltInAgentId)
                .ToList();

            foreach (var ip in activeIps)
            {
                var device = existingDevices.FirstOrDefault(d => d.IpAddress == ip);

                if (device == null)
                {
                    _dbContext.Devices.Add(new Device
                    {
                        AgentId = SystemConstants.BuiltInAgentId,
                        DisplayName = $"Unknown Device ({ip})",
                        IpAddress = ip,
                        Status = 1
                    });
                }
                else
                {
                    device.Status = 1;
                    _dbContext.DeviceHistories.Add(new DeviceHistory
                    {
                        DeviceId = device.Id,
                        ScanId = scan.Id,
                        Timestamp = DateTime.UtcNow,
                        Status = "Online"
                    });
                }
            }

            var missingDevices = existingDevices.Where(d => !activeIps.Contains(d.IpAddress));
            foreach (var missing in missingDevices)
            {
                missing.Status = 0;
                _dbContext.DeviceHistories.Add(new DeviceHistory
                {
                    DeviceId = missing.Id,
                    ScanId = scan.Id,
                    Timestamp = DateTime.UtcNow,
                    Status = "Offline"
                });
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            return scan;
        }
    }
}