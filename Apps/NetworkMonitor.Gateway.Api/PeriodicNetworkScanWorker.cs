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
                    using var scope = _scopeFactory.CreateScope();
                    var networkScanService = scope.ServiceProvider.GetRequiredService<INetworkScanService>();
                    await networkScanService.RunScanAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during local network scan.");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}