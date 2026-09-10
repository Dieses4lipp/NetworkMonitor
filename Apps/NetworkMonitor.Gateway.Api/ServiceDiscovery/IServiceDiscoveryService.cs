namespace NetworkMonitor.Gateway.Api.ServiceDiscovery
{
    public interface IServiceDiscoveryService
    {
        Task<IReadOnlyList<DiscoveredService>> ScanAsync(string ipAddress, CancellationToken cancellationToken = default);
    }
}
