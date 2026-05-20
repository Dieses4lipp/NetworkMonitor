namespace NetworkMonitor
{
    public class NetworkDevice
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string IPAddress { get; set; }
        public string MACAddress { get; set; }
        public DeviceStatus Status { get; set; }
        public string InterfaceType { get; set; }
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
        public int ScanCount { get; set; }
    }

    public enum DeviceStatus
    {
        Online,
        Offline,
        Unknown
    }
}
