using System;
using System.Collections.Generic;

namespace NetworkMonitor.Domain;

public partial class RawMetric
{
    public long Id { get; set; }

    public int JobId { get; set; }

    public DateTime Timestamp { get; set; }

    public double Value { get; set; }

    public bool IsSuccess { get; set; }

    public string? ErrorMessage { get; set; }

    public virtual MonitoringJob Job { get; set; } = null!;
}
