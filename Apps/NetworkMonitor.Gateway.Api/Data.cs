namespace NetworkMonitor.Data
{
    public class NetworkScan
    {
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int DevicesFound { get; set; }
        public string Status { get; set; } = "Completed";
    }

    public class DeviceHistory
    {
        public int Id { get; set; }
        public int DeviceId { get; set; }
        public int ScanId { get; set; }
        public DateTime Timestamp { get; set; }
        public string Status { get; set; }
    }
}
