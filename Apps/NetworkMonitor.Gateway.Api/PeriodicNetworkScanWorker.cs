using Microsoft.EntityFrameworkCore;
using NetworkMonitor.Domain;
using NetworkMonitor.Infrastructure.Data.Context;
using NetworkMonitor.Services;
using System.Net.NetworkInformation;

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
            var discovered = new List<DiscoveredDevice>();
            var status = "Completed";
            var starttime = DateTime.UtcNow;
            try { discovered = await _discoveryService.ScanNetworkAsync(stoppingToken); }
            catch (Exception ex)
            {
                status = "Failed";
                _logger.LogError(ex, "Network discovery failed.");
                return;
            }
            var activeIps = discovered.Select(d => d.IPAddress).ToList();
            _logger.LogInformation("Scan complete. Found {DeviceCount} active devices.", activeIps.Count);

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NetworkMonitorDbContext>();
            var scan = new NetworkScan
            {
                StartTime = starttime,
                EndTime = DateTime.UtcNow,
                DevicesFound = activeIps.Count,
                Status = status
            };
            dbContext.NetworkScans.Add(scan);
            await dbContext.SaveChangesAsync(stoppingToken);

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
                    dbContext.DeviceHistories.Add(new DeviceHistory
                    {
                        DeviceId = device.Id,
                        ScanId = scan.Id,
                        Timestamp = DateTime.UtcNow,
                        Status = "Online"
                    });
                }

            }
            var missingDevices = existingDevices.Where(d => !activeIps.Contains(d.IpAddress));
            foreach (var missing in missingDevices) { 
                missing.Status = 0;
            dbContext.DeviceHistories.Add(new DeviceHistory
            {
                DeviceId = missing.Id,
                ScanId = scan.Id,
                Timestamp = DateTime.UtcNow,
                Status = "Offline"
            });
        }
            await dbContext.SaveChangesAsync(stoppingToken);
        }
    }
}