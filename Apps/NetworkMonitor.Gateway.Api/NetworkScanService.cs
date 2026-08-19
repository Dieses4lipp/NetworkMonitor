using NetworkMonitor.Domain;
using NetworkMonitor.Gateway.Api.PlatformInspection;
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
        private readonly IHostFingerprintService _fingerprintService;
        private readonly IEnumerable<IPlatformInspector> _platformInspectors;
        private readonly NetworkMonitorDbContext _dbContext;
        private readonly TimeSpan _fingerprintInterval;

        public NetworkScanService(
            ILogger<NetworkScanService> logger,
            INetworkDiscoveryService discoveryService,
            IPlatformClassificationService classificationService,
            IHostFingerprintService fingerprintService,
            IEnumerable<IPlatformInspector> platformInspectors,
            NetworkMonitorDbContext dbContext,
            IConfiguration configuration)
        {
            _logger = logger;
            _discoveryService = discoveryService;
            _classificationService = classificationService;
            _fingerprintService = fingerprintService;
            _platformInspectors = platformInspectors;
            _dbContext = dbContext;
            var fingerprintMinutes = configuration.GetValue<int?>("NetworkMonitor:FingerprintIntervalMinutes") ?? 30;
            _fingerprintInterval = TimeSpan.FromMinutes(fingerprintMinutes);
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

            await FingerprintDueDevicesAsync(activeIps, cancellationToken);

            return scan;
        }

        private async Task FingerprintDueDevicesAsync(List<string> activeIps, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var dueDevices = _dbContext.Devices
                .Where(d => d.AgentId == SystemConstants.BuiltInAgentId && activeIps.Contains(d.IpAddress))
                .ToList()
                .Where(d => d.LastFingerprintedAt == null || (now - d.LastFingerprintedAt.Value) > _fingerprintInterval)
                .ToList();

            foreach (var device in dueDevices)
            {
                try
                {
                    var fingerprint = await _fingerprintService.FingerprintAsync(device.IpAddress, cancellationToken);

                    device.PlatformType = _classificationService.Classify(new PlatformClassificationInput(
                        Vendor: device.Vendor,
                        Hostname: device.DisplayName,
                        OperatingSystemGuess: device.OperatingSystem,
                        SshBanner: fingerprint.SshBanner,
                        RespondedPorts: fingerprint.RespondedPorts));
                    device.LastFingerprintedAt = now;

                    var inspector = _platformInspectors.FirstOrDefault(i => i.SupportedPlatform == device.PlatformType);
                    if (inspector != null)
                        await InspectAndPersistAsync(device, inspector, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Fingerprinting failed for device {Ip}", device.IpAddress);
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task InspectAndPersistAsync(Device device, IPlatformInspector inspector, CancellationToken cancellationToken)
        {
            var result = await inspector.InspectAsync(device, cancellationToken);
            if (!result.WasSuccessful)
            {
                _logger.LogWarning("Platform inspection failed for {Ip}: {Error}", device.IpAddress, result.ErrorMessage);
                return;
            }

            var now = DateTime.UtcNow;

            var existingWorkloads = _dbContext.HostedWorkloads.Where(w => w.DeviceId == device.Id && w.Source == inspector.Source);
            _dbContext.HostedWorkloads.RemoveRange(existingWorkloads);
            foreach (var w in result.Workloads)
            {
                _dbContext.HostedWorkloads.Add(new HostedWorkload
                {
                    DeviceId = device.Id,
                    ExternalId = w.ExternalId,
                    Name = w.Name,
                    Kind = w.Kind,
                    Status = w.Status,
                    Source = inspector.Source,
                    ReportedAt = now
                });
            }

            var existingServices = _dbContext.ServiceUnits.Where(s => s.DeviceId == device.Id && s.Source == inspector.Source);
            _dbContext.ServiceUnits.RemoveRange(existingServices);
            foreach (var s in result.Services)
            {
                _dbContext.ServiceUnits.Add(new ServiceUnit
                {
                    DeviceId = device.Id,
                    UnitName = s.UnitName,
                    ActiveState = s.ActiveState,
                    SubState = s.SubState,
                    Source = inspector.Source,
                    ReportedAt = now
                });
            }
        }
    }
}