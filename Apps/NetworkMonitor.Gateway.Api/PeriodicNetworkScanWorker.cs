using Microsoft.EntityFrameworkCore;
using NetworkMonitor.Domain;
using NetworkMonitor.Infrastructure.Data.Context;
using System.Net.NetworkInformation;

namespace NetworkMonitor.Gateway.Api
{
    public class PeriodicNetworkScanWorker : BackgroundService
    {
        private readonly ILogger<PeriodicNetworkScanWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TimeSpan _scanInterval;
        public PeriodicNetworkScanWorker(ILogger<PeriodicNetworkScanWorker> logger, IServiceScopeFactory scopeFactory, IConfiguration configuration)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            var scanIntervalSeconds = configuration.GetValue<int?>("NetworkMonitor:ScanIntervalSeconds") ?? 300;
            _scanInterval = TimeSpan.FromSeconds(scanIntervalSeconds);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Starting built-in local network ping sweep...");

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var networkScanService = scope.ServiceProvider.GetRequiredService<INetworkScanService>();
                    await networkScanService.RunScanAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during local network scan.");
                }

                await Task.Delay(_scanInterval, stoppingToken);
            }
        }
    }
}