namespace NetworkMonitor.Gateway.Api.Tests;

/// <summary>Returns a fixed set of discovered devices, so no packets leave the test host.</summary>
public sealed class StubNetworkDiscoveryService : INetworkDiscoveryService
{
    public List<DiscoveredDevice> Devices { get; set; } = new();

    public Task<List<DiscoveredDevice>> ScanNetworkAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Devices.ToList());

    public Task<List<DiscoveredDevice>> PingRangeAsync(
        string gatewayPrefix,
        int startIp = 1,
        int endIp = 254,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}
