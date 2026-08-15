using NetworkMonitor.Domain;
using NetworkMonitor.Infrastructure.Data.Context;

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
        private readonly IPlatformClassificationService _classificationService;
        private readonly NetworkMonitorDbContext _dbContext;

        public NetworkScanService(
            ILogger<NetworkScanService> logger,
            INetworkDiscoveryService discoveryService,
            IPlatformClassificationService classificationService,
            NetworkMonitorDbContext dbContext)
        {
            _logger = logger;
            _discoveryService = discoveryService;
            _classificationService = classificationService;
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
                var info = discovered.First(d => d.IPAddress == ip);
                var platformType = _classificationService.Classify(new PlatformClassificationInput(
                    Vendor: info.Vendor,
                    Hostname: info.HostName,
                    OperatingSystemGuess: info.OperatingSystem));

                if (device == null)
                {
                    _dbContext.Devices.Add(new Device
                    {
                        AgentId = SystemConstants.BuiltInAgentId,
                        DisplayName = $"Unknown Device ({ip})",
                        IpAddress = ip,
                        Status = 1,
                        OperatingSystem = info.OperatingSystem,
                        Vendor = info.Vendor,
                        PlatformType = platformType,
                        LastFingerprintedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    device.Status = 1;
                    if (info.OperatingSystem != "Unknown")
                        device.OperatingSystem = info.OperatingSystem;
                    device.Vendor = info.Vendor;
                    device.PlatformType = platformType;
                    device.LastFingerprintedAt = DateTime.UtcNow;

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