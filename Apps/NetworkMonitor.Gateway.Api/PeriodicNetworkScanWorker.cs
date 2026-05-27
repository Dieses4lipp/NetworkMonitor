using NetworkMonitor.Domain;
using NetworkMonitor.Infrastructure.Data.Context;
using NetworkMonitor.Services;

namespace NetworkMonitor.Services
{
    public class PeriodicNetworkScanWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PeriodicNetworkScanWorker> _logger;
        private readonly IConfiguration _configuration;
        private int _scanIntervalSeconds = 300; // Default 5 minutes

        public PeriodicNetworkScanWorker(IServiceProvider serviceProvider, ILogger<PeriodicNetworkScanWorker> logger, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
            
            if (int.TryParse(_configuration["NetworkMonitor:ScanIntervalSeconds"], out var interval))
                _scanIntervalSeconds = interval;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PeriodicNetworkScanWorker started with {interval}s interval", _scanIntervalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_scanIntervalSeconds * 1000, stoppingToken);
                    await PerformNetworkScanAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in periodic network scan worker");
                }
            }

            _logger.LogInformation("PeriodicNetworkScanWorker stopped");
        }

        private async Task PerformNetworkScanAsync(CancellationToken cancellationToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var discoveryService = scope.ServiceProvider.GetRequiredService<INetworkDiscoveryService>();
                var trackingService = scope.ServiceProvider.GetRequiredService<IDeviceTrackingService>();
                var dbContext = scope.ServiceProvider.GetRequiredService<NetworkMonitorDbContext>();

                try
                {
                    _logger.LogInformation("Starting periodic network scan");
                    var startTime = DateTime.UtcNow;

                    var discoveredDevices = await discoveryService.ScanNetworkAsync(cancellationToken);
                    var endTime = DateTime.UtcNow;

                    var scan = new NetworkScan
                    {
                        StartTime = startTime,
                        EndTime = endTime,
                        DevicesFound = discoveredDevices.Count,
                        Status = "Completed"
                    };

                    dbContext.NetworkScans.Add(scan);
                    await dbContext.SaveChangesAsync(cancellationToken);

                    var updatedDevices = await trackingService.UpdateDevicesFromScanAsync(discoveredDevices);

                    foreach (var device in updatedDevices)
                    {
                        await trackingService.RecordDeviceHistoryAsync(device.Id, scan.Id, "Online");
                    }

                    _logger.LogInformation($"Periodic scan completed: {discoveredDevices.Count} devices found in {(endTime - startTime).TotalSeconds:F2}s");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing periodic network scan");
                }
            }
        }
    }
}
