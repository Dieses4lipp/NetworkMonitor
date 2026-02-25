using System;
using System.Collections.Generic;

namespace NetworkMonitor.Domain;

public partial class Device
{
    public int Id { get; set; }

    public Guid AgentId { get; set; }

    public string DisplayName { get; set; } = null!;

    public string IpAddress { get; set; } = null!;

    public int Status { get; set; }

    public virtual Agent Agent { get; set; } = null!;

    public virtual ICollection<MonitoringJob> MonitoringJobs { get; set; } = new List<MonitoringJob>();
}
