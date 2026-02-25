using System;
using System.Collections.Generic;

namespace NetworkMonitor.Domain;

public partial class Agent
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string SecretKey { get; set; } = null!;

    public DateTime? LastSeen { get; set; }

    public string? Version { get; set; }

    public virtual ICollection<Device> Devices { get; set; } = new List<Device>();
}
