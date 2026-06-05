using System;
using System.Collections.Generic;
using System.Text;

namespace NetworkMonitor.Domain
{
    public enum MonitoringJobType
    {
        IcmpPing = 1,
        Http = 2,
        TcpPort = 3,
        Snmp = 4
    }
}
