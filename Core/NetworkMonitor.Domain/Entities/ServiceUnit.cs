using System;

namespace NetworkMonitor.Domain;

public class ServiceUnit
{
    public int Id { get; set; }

    public int DeviceId { get; set; }

    public string UnitName { get; set; } = null!;

    public string ActiveState { get; set; } = null!;

    public string? SubState { get; set; }

    public string Source { get; set; } = null!;

    public DateTime ReportedAt { get; set; }

    public virtual Device Device { get; set; } = null!;
}
