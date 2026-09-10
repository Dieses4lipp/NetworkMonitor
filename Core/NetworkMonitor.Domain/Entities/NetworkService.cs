using System;

namespace NetworkMonitor.Domain;

public class NetworkService
{
    public int Id { get; set; }

    public int DeviceId { get; set; }

    public int Port { get; set; }

    public string Name { get; set; } = null!;

    public string? Banner { get; set; }

    public DateTime FirstSeenAt { get; set; }

    public DateTime LastSeenAt { get; set; }

    public virtual Device Device { get; set; } = null!;
}
