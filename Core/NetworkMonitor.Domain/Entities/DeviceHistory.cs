namespace NetworkMonitor.Domain;

public class DeviceHistory
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public int ScanId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Status { get; set; }
}
