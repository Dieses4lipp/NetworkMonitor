namespace NetworkMonitor
{
    public class NetworkDevice
    {
        public string Name { get; set; }
        public string IPAddress { get; set; }
        public string MACAddress { get; set; }
        public bool IsActive { get; set; }
        public string InterfaceType { get; set; }
        public string ParentMacAddress { get; set; }

    }
}
