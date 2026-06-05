using System.Net.NetworkInformation;
using NetworkMonitor.Domain;
using NetworkMonitor.Infrastructure.Data.Context;
using NetworkMonitor.Services;

namespace NetworkMonitor.Gateway.Api
{
    public class PeriodicNetworkScanWorker : BackgroundService
    {
        private readonly ILogger<PeriodicNetworkScanWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly INetworkDiscoveryService _discoveryService;

        public PeriodicNetworkScanWorker(ILogger<PeriodicNetworkScanWorker> logger, IServiceScopeFactory scopeFactory, INetworkDiscoveryService discoveryService)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _discoveryService = discoveryService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Starting built-in local network ping sweep...");

                try
                {
                    await RunScanAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during local network scan.");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        private async Task RunScanAsync(CancellationToken stoppingToken)
        {
            var discovered = await _discoveryService.ScanNetworkAsync(stoppingToken);
            var activeIps = discovered.Select(d => d.IPAddress).ToList();

            _logger.LogInformation("Scan complete. Found {DeviceCount} active devices.", activeIps.Count);

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NetworkMonitorDbContext>();

            var existingDevices = dbContext.Devices
                .Where(d => d.AgentId == SystemConstants.BuiltInAgentId)
                .ToList();

            foreach (var ip in activeIps)
            {
                var device = existingDevices.FirstOrDefault(d => d.IpAddress == ip);

                if (device == null)
                {
                    dbContext.Devices.Add(new Device
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
                }
            }

            var missingDevices = existingDevices.Where(d => !activeIps.Contains(d.IpAddress));
            foreach (var missing in missingDevices)
                missing.Status = 0;

            await dbContext.SaveChangesAsync(stoppingToken);
        }
    }
}