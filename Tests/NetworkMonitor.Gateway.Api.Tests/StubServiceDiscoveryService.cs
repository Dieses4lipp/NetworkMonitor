using NetworkMonitor.Gateway.Api.ServiceDiscovery;

namespace NetworkMonitor.Gateway.Api.Tests;

/// <summary>Returns a fixed set of open ports per scan, so no TCP connects leave the test host.</summary>
public sealed class StubServiceDiscoveryService : IServiceDiscoveryService
{
    public List<DiscoveredService> Services { get; set; } = new();

    public int ScanCount { get; private set; }

    public Task<IReadOnlyList<DiscoveredService>> ScanAsync(
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        ScanCount++;
        return Task.FromResult<IReadOnlyList<DiscoveredService>>(Services.ToList());
    }
}
