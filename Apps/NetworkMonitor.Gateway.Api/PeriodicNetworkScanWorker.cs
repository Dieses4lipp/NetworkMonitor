using System.Net.NetworkInformation;
using NetworkMonitor.Domain;
using NetworkMonitor.Infrastructure.Data.Context;

namespace NetworkMonitor.Gateway.Api
{
    public class PeriodicNetworkScanWorker : BackgroundService
    {
        private readonly ILogger<PeriodicNetworkScanWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly string _subnetPrefix = "192.168.178.";

        public PeriodicNetworkScanWorker(ILogger<PeriodicNetworkScanWorker> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
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
            var activeIps = new List<string>();
            var pingTasks = new List<Task<PingReply?>>();

            for (int i = 1; i < 255; i++)
            {
                var ip = $"{_subnetPrefix}{i}";
                pingTasks.Add(PingAsync(ip));
            }

            var replies = await Task.WhenAll(pingTasks);

            foreach (var reply in replies.Where(r => r != null && r.Status == IPStatus.Success))
            {
                activeIps.Add(reply!.Address.ToString());
            }

            _logger.LogInformation($"Scan complete. Found {activeIps.Count} active devices.");

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NetworkMonitorDbContext>();

            var builtInAgent = await dbContext.Agents.FindAsync(SystemConstants.BuiltInAgentId);

            var existingDevices = dbContext.Devices
                .Where(d => d.AgentId == SystemConstants.BuiltInAgentId)
                .ToList();

            foreach (var ip in activeIps)
            {
                var device = existingDevices.FirstOrDefault(d => d.IpAddress == ip);

                if (device == null)
                {
                    // Map exactly to your updated Device class
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
            {
                missing.Status = 0; 
            }

            await dbContext.SaveChangesAsync(stoppingToken);
        }

        private async Task<PingReply?> PingAsync(string ipAddress)
        {
            try
            {
                using var ping = new Ping();
                return await ping.SendPingAsync(ipAddress, 1000);
            }
            catch
            {
                return null;
            }
        }
    }
}