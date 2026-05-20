using NetworkMonitor.Data;

namespace NetworkMonitor.Services
{
    public interface IDeviceTrackingService
    {
        Task<List<NetworkDevice>> UpdateDevicesFromScanAsync(List<DiscoveredDevice> discoveredDevices);
        Task<NetworkDevice> GetOrCreateDeviceAsync(DiscoveredDevice discoveredDevice);
        Task RecordDeviceHistoryAsync(int deviceId, int scanId, string status);
        Task<List<NetworkDevice>> GetAllDevicesAsync();
        Task<NetworkDevice> GetDeviceByIdAsync(int id);
        Task<List<DeviceHistory>> GetDeviceHistoryAsync(int deviceId, int limit = 100);
    }

    public class DeviceTrackingService : IDeviceTrackingService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<DeviceTrackingService> _logger;

        public DeviceTrackingService(ApplicationDbContext dbContext, ILogger<DeviceTrackingService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<List<NetworkDevice>> UpdateDevicesFromScanAsync(List<DiscoveredDevice> discoveredDevices)
        {
            var updatedDevices = new List<NetworkDevice>();

            foreach (var discovered in discoveredDevices)
            {
                var device = await GetOrCreateDeviceAsync(discovered);
                device.Status = DeviceStatus.Online;
                device.LastSeen = DateTime.UtcNow;
                device.ScanCount++;

                _dbContext.Devices.Update(device);
                updatedDevices.Add(device);
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation($"Updated {updatedDevices.Count} devices from scan");
            return updatedDevices;
        }

        public async Task<NetworkDevice> GetOrCreateDeviceAsync(DiscoveredDevice discoveredDevice)
        {
            var existingDevice = _dbContext.Devices
                .FirstOrDefault(d => d.MACAddress == discoveredDevice.MACAddress);

            if (existingDevice != null)
            {
                existingDevice.IPAddress = discoveredDevice.IPAddress;
                existingDevice.Name = discoveredDevice.HostName;
                existingDevice.InterfaceType = discoveredDevice.InterfaceType;
                return existingDevice;
            }

            var newDevice = new NetworkDevice
            {
                Name = discoveredDevice.HostName,
                IPAddress = discoveredDevice.IPAddress,
                MACAddress = discoveredDevice.MACAddress,
                InterfaceType = discoveredDevice.InterfaceType,
                Status = DeviceStatus.Online,
                FirstSeen = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow,
                ScanCount = 1
            };

            _dbContext.Devices.Add(newDevice);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation($"Created new device: {newDevice.Name} ({newDevice.MACAddress})");
            return newDevice;
        }

        public async Task RecordDeviceHistoryAsync(int deviceId, int scanId, string status)
        {
            var history = new DeviceHistory
            {
                DeviceId = deviceId,
                ScanId = scanId,
                Timestamp = DateTime.UtcNow,
                Status = status
            };

            _dbContext.DeviceHistories.Add(history);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<NetworkDevice>> GetAllDevicesAsync()
        {
            return await Task.FromResult(_dbContext.Devices.OrderByDescending(d => d.LastSeen).ToList());
        }

        public async Task<NetworkDevice> GetDeviceByIdAsync(int id)
        {
            return await Task.FromResult(_dbContext.Devices.FirstOrDefault(d => d.Id == id));
        }

        public async Task<List<DeviceHistory>> GetDeviceHistoryAsync(int deviceId, int limit = 100)
        {
            return await Task.FromResult(
                _dbContext.DeviceHistories
                    .Where(h => h.DeviceId == deviceId)
                    .OrderByDescending(h => h.Timestamp)
                    .Take(limit)
                    .ToList()
            );
        }
    }
}
