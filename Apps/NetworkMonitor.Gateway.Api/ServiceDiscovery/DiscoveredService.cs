namespace NetworkMonitor.Gateway.Api.ServiceDiscovery
{
    public record DiscoveredService(int Port, string Name, string? Banner);
}
