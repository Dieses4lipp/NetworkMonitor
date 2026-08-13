using Microsoft.EntityFrameworkCore;
using NetworkMonitor.Domain;
using NetworkMonitor.Infrastructure.Data.Context;

namespace NetworkMonitor.Application.Devices;

public class DeviceService : IDeviceService
{
    private readonly NetworkMonitorDbContext _dbContext;

    public DeviceService(NetworkMonitorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Device>> GetAllDevicesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Devices
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<DeviceDetailsDto?> GetDeviceByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var device = await _dbContext.Devices
            .Include(d => d.MonitoringJobs)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (device == null)
            return null;

        return new DeviceDetailsDto(
            device.Id,
            device.AgentId,
            device.DisplayName,
            device.IpAddress,
            device.Status,
            device.OperatingSystem,
            device.MonitoringJobs.Count);
    }

    public async Task<List<NetworkScan>> GetScanHistoryAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.NetworkScans
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
