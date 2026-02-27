using System;
using System.Collections.Generic;

namespace NetworkMonitor.Domain;

public partial class MonitoringJob
{
    public int Id { get; set; }

    public int DeviceId { get; set; }

    public int Type { get; set; }

    public int IntervalSeconds { get; set; }

    public string? ConfigurationJson { get; set; }

    public virtual Device Device { get; set; } = null!;

    public virtual ICollection<RawMetric> RawMetrics { get; set; } = new List<RawMetric>();
}
