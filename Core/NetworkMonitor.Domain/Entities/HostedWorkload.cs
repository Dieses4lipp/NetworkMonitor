using System;

namespace NetworkMonitor.Domain;

public class HostedWorkload
{
    public int Id { get; set; }

    public int DeviceId { get; set; }

    public string ExternalId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Kind { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string Source { get; set; } = null!;

    public DateTime ReportedAt { get; set; }

    public virtual Device Device { get; set; } = null!;
}
