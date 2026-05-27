namespace NetworkMonitor.Domain;

public class NetworkScan
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int DevicesFound { get; set; }
    public string Status { get; set; } = "Completed";
}
