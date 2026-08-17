using NetworkMonitor.Domain;

namespace NetworkMonitor.Application.Devices;

public record DeviceDetailsDto(
    int Id,
    Guid AgentId,
    string DisplayName,
    string IpAddress,
    int Status,
    string? OperatingSystem,
    int JobCount);

public interface IDeviceService
{
    Task<List<Device>> GetAllDevicesAsync(CancellationToken cancellationToken = default);

    Task<DeviceDetailsDto?> GetDeviceByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<List<NetworkScan>> GetScanHistoryAsync(CancellationToken cancellationToken = default);

    Task<List<HostedWorkload>> GetWorkloadsAsync(int deviceId, CancellationToken cancellationToken = default);

    Task<List<ServiceUnit>> GetServiceUnitsAsync(int deviceId, CancellationToken cancellationToken = default);
}
